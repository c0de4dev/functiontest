using DynamicAllowListingLib.Helpers;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static DynamicAllowListingLib.Models.VNets;

namespace DynamicAllowListingLib.Services
{
  public interface IIpRestrictionRuleGeneratorService
  {
    HashSet<IpSecurityRestrictionRule> GenerateIpRestrictionRules(List<IAzureResource> resourceIds, List<string> subnetIdsToGenerateRulesFrom, List<string> allSubnetIds);
  }

  public class IpRestrictionRuleGeneratorService : IIpRestrictionRuleGeneratorService
  {
    private readonly ILogger<IpRestrictionRuleGeneratorService> _logger;

    public IpRestrictionRuleGeneratorService(ILogger<IpRestrictionRuleGeneratorService> logger)
    {
      _logger = logger;
    }

    public HashSet<IpSecurityRestrictionRule> GenerateIpRestrictionRules(List<IAzureResource> resourceIds,
      List<string> subnetIdsToGenerateRulesFrom,
      List<string> allSubnetIds)
    {
      var generatedIpRules = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        // Step 1: Generate rules from Azure resources
        if (resourceIds != null && resourceIds.Any())
        {
          foreach (var resource in resourceIds)
          {
            _logger.LogGeneratingRulesForResource(resource.Name ?? "Unknown");
            generatedIpRules.UnionWith(resource.GenerateIpRestrictionRules(_logger));
          }
        }
        else
        {
          _logger.LogNoAzureResourcesProvided();
        }

        // Step 2: Generate rules for specific subnets
        if (subnetIdsToGenerateRulesFrom != null && subnetIdsToGenerateRulesFrom.Any())
        {
          foreach (var subnetId in subnetIdsToGenerateRulesFrom)
          {
            _logger.LogGeneratingRulesForSubnet(subnetId);

            string resourceName = StringHelper.GetResourceName(subnetId);
            var rule = IpRulesHelper.GenerateDynamicAllowListingRuleForVnet(resourceName, subnetId);
            generatedIpRules.Add(rule);
          }
        }
        else
        {
          _logger.LogNoSubnetIdsProvided();
        }

        // Generate ip rules for vnet per naming convention
        HashSet<IpSecurityRestrictionRule> vnetIpRulesPerNamingConvention = GenerateIpRulesBySubnetNamingConvention(resourceIds!, allSubnetIds);
        generatedIpRules.UnionWith(vnetIpRulesPerNamingConvention);
      }
      catch (Exception ex)
      {
        _logger.LogGenerateRulesException(ex);
      }
      _logger.LogTotalRulesGenerated(generatedIpRules.Count);

      return generatedIpRules;
    }

    /// <summary>
    /// Find all SubnetIds and generate subnet restriction rules for intersected names
    /// </summary>
    /// <param name="existingResources">rules to be generated for</param>
    /// <param name="allSubnetIds">All vnetSubnet ids in current azure subscription.</param>
    /// <returns></returns>
    internal HashSet<IpSecurityRestrictionRule> GenerateIpRulesBySubnetNamingConvention(List<IAzureResource> existingResources, List<string> allSubnetIds)
    {

      var generatedIpRules = new HashSet<IpSecurityRestrictionRule>();

      // Validate input: Check if existing resources are provided
      if (existingResources == null || !existingResources.Any())
      {
        _logger.LogNoExistingResourcesProvided();
        return generatedIpRules; // Return early if there are no resources
      }

      try
      {
        var intersectedSubnetIds = FindIntersectedSubnetIds(existingResources, allSubnetIds);
        if (!intersectedSubnetIds.Any())
        {
          _logger.LogNoIntersectedSubnetIds();
          return generatedIpRules; // Return early if no intersected subnets are found
        }

        foreach (var subnetId in intersectedSubnetIds)
        {
          generatedIpRules.Add(IpRulesHelper.GenerateDynamicAllowListingRuleForVnet(
            subnetId.Key,   // resource name
            subnetId.Value  // subnet Id
            ));

          _logger.LogGeneratedVnetRule(subnetId.Key, subnetId.Value);
        }
        _logger.LogNamingConventionRulesGenerated(generatedIpRules.Count);

        return generatedIpRules;
      }
      catch (Exception ex)
      {
        _logger.LogNamingConventionException(ex);
        throw; // Re-throw the exception after logging it
      }
    }

    internal List<KeyValuePair<string, string>> FindIntersectedSubnetIds(List<IAzureResource> existingResources,
      List<string> allSubnetIds)
    {
      _logger.LogMethodStart(nameof(FindIntersectedSubnetIds));

      var intersectedSubnetIds = new List<KeyValuePair<string, string>>();

      // Validate input: Check if subnet IDs are provided
      if (allSubnetIds == null || !allSubnetIds.Any())
      {
        _logger.LogNoSubnetIdsForIntersection();
        return intersectedSubnetIds; // Early return if no subnet IDs
      }

      try
      {
        // Build a list of subnet name and ID tuples
        var allSubnetIdTuple = allSubnetIds.Where(x => !string.IsNullOrEmpty(x))
          .Select(x => new KeyValuePair<string, string>(x.Split('/').Last(), x)) // there are subnetIds which have same name but in different vnet!
          .ToList();

        // Generate the expected subnet names based on the resource names
        var expectedSubnetNameList = existingResources.Select(x => $"nsbnt-{x.Name}").ToList();

        _logger.LogExpectedSubnetNames(string.Join(", ", expectedSubnetNameList));

        // Identify intersected subnet names
        var intersectedNames = expectedSubnetNameList.Intersect(allSubnetIdTuple.Select(x => x.Key)).ToList();

        // Build the result for intersected subnet IDs
        intersectedSubnetIds = allSubnetIdTuple
           .Where(x => x.Key != null && intersectedNames.Contains(x.Key))
           .Select(x => new KeyValuePair<string, string>
           (
             x.Key.Split('-').Last(), // name
             x.Value                  // subnetId
           )).ToList();

        // Log the found intersected subnet IDs
        if (intersectedSubnetIds.Any())
        {
          _logger.LogFoundIntersectedSubnetIds(
            string.Join(", ", intersectedSubnetIds.Select(id => id.Value)),
            intersectedSubnetIds.Count);
        }
        else
        {
          _logger.LogNoIntersectedSubnetIdsFound();
        }
      }
      catch (Exception ex)
      {
        _logger.LogFindIntersectedSubnetIdsException(ex);
        throw; // Re-throw the exception after logging it
      }

      return intersectedSubnetIds;
    }
  }

  /// <summary>
  /// High-performance structured logging extensions for IpRestrictionRuleGeneratorService.
  /// Uses LoggerMessage source generators for optimal performance.
  /// </summary>
  public static partial class IpRestrictionRuleGeneratorLoggerExtensions
  {
    // ============================================================
    // Method Lifecycle (EventIds 5000-5009)
    // ============================================================

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Information,
        Message = "Starting method: {MethodName}")]
    public static partial void LogMethodStart(
        this ILogger logger,
        string methodName);

    // ============================================================
    // GenerateIpRestrictionRules (EventIds 5010-5029)
    // ============================================================

    [LoggerMessage(
        EventId = 5010,
        Level = LogLevel.Information,
        Message = "Generating IP restriction rules for resource: {ResourceName}")]
    public static partial void LogGeneratingRulesForResource(
        this ILogger logger,
        string resourceName);

    [LoggerMessage(
        EventId = 5011,
        Level = LogLevel.Warning,
        Message = "No Azure resources provided for generating IP restriction rules")]
    public static partial void LogNoAzureResourcesProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 5012,
        Level = LogLevel.Information,
        Message = "Generating IP restriction rules for subnet ID: {SubnetId}")]
    public static partial void LogGeneratingRulesForSubnet(
        this ILogger logger,
        string subnetId);

    [LoggerMessage(
        EventId = 5013,
        Level = LogLevel.Warning,
        Message = "No subnet IDs provided for generating specific IP restriction rules")]
    public static partial void LogNoSubnetIdsProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 5014,
        Level = LogLevel.Error,
        Message = "Exception occurred in generating IP restriction rules")]
    public static partial void LogGenerateRulesException(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 5015,
        Level = LogLevel.Information,
        Message = "Total IP restriction rules generated: {RuleCount}")]
    public static partial void LogTotalRulesGenerated(
        this ILogger logger,
        int ruleCount);

    // ============================================================
    // GenerateIpRulesBySubnetNamingConvention (EventIds 5030-5049)
    // ============================================================

    [LoggerMessage(
        EventId = 5030,
        Level = LogLevel.Warning,
        Message = "No existing resources provided; returning an empty rule set")]
    public static partial void LogNoExistingResourcesProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 5031,
        Level = LogLevel.Information,
        Message = "No intersected subnet IDs found; no IP rules will be generated")]
    public static partial void LogNoIntersectedSubnetIds(this ILogger logger);

    [LoggerMessage(
        EventId = 5032,
        Level = LogLevel.Information,
        Message = "Generated IP rule for VNet: {ResourceName}, Subnet ID: {SubnetId}")]
    public static partial void LogGeneratedVnetRule(
        this ILogger logger,
        string resourceName,
        string subnetId);

    [LoggerMessage(
        EventId = 5033,
        Level = LogLevel.Information,
        Message = "Successfully generated {RuleCount} IP rules based on subnet naming convention")]
    public static partial void LogNamingConventionRulesGenerated(
        this ILogger logger,
        int ruleCount);

    [LoggerMessage(
        EventId = 5034,
        Level = LogLevel.Error,
        Message = "Exception occurred in GenerateIpRulesBySubnetNamingConvention")]
    public static partial void LogNamingConventionException(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // FindIntersectedSubnetIds (EventIds 5050-5069)
    // ============================================================

    [LoggerMessage(
        EventId = 5050,
        Level = LogLevel.Warning,
        Message = "No subnet IDs provided; returning an empty list of intersected subnet IDs")]
    public static partial void LogNoSubnetIdsForIntersection(this ILogger logger);

    [LoggerMessage(
        EventId = 5051,
        Level = LogLevel.Information,
        Message = "Expected subnet names: {SubnetNames}")]
    public static partial void LogExpectedSubnetNames(
        this ILogger logger,
        string subnetNames);

    [LoggerMessage(
        EventId = 5052,
        Level = LogLevel.Information,
        Message = "Found intersected subnet IDs: {SubnetIds} | Count: {Count}")]
    public static partial void LogFoundIntersectedSubnetIds(
        this ILogger logger,
        string subnetIds,
        int count);

    [LoggerMessage(
        EventId = 5053,
        Level = LogLevel.Information,
        Message = "No intersected subnet IDs found")]
    public static partial void LogNoIntersectedSubnetIdsFound(
        this ILogger logger);

    [LoggerMessage(
        EventId = 5054,
        Level = LogLevel.Error,
        Message = "Exception occurred in FindIntersectedSubnetIds")]
    public static partial void LogFindIntersectedSubnetIdsException(
        this ILogger logger,
        Exception exception);
  }
}