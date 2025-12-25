using DynamicAllowListingLib.Helpers;
using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using static DynamicAllowListingLib.Models.VNets;

namespace DynamicAllowListingLib.Services
{
  public interface IIpRestrictionRuleGeneratorService
  {
    HashSet<IpSecurityRestrictionRule> GenerateIpRestrictionRules(List<IAzureResource> resourceIds,List<string> subnetIdsToGenerateRulesFrom,List<string> allSubnetIds);
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
      FunctionLogger.MethodStart(_logger, nameof(GenerateIpRestrictionRules));
      var generatedIpRules = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        // Step 1: Generate rules from Azure resources
        if (resourceIds != null && resourceIds.Any())
        {
          foreach (var resource in resourceIds)
          {
            FunctionLogger.MethodInformation(_logger, $"Generating IP restriction rules for resource: {resource.Name}");
            generatedIpRules.UnionWith(resource.GenerateIpRestrictionRules(_logger));
          }
        }
        else
        {
          FunctionLogger.MethodWarning(_logger, "No Azure resources provided for generating IP restriction rules.");
        }

        // Step 2: Generate rules for specific subnets
        if (subnetIdsToGenerateRulesFrom != null && subnetIdsToGenerateRulesFrom.Any())
        {
          foreach (var subnetId in subnetIdsToGenerateRulesFrom)
          {
            FunctionLogger.MethodInformation(_logger, $"Generating IP restriction rules for subnet ID: {subnetId}");

            string resourceName = StringHelper.GetResourceName(subnetId);
            var rule = IpRulesHelper.GenerateDynamicAllowListingRuleForVnet(resourceName, subnetId);
            generatedIpRules.Add(rule);
          }
        }
        else
        {
          FunctionLogger.MethodWarning(_logger, "No subnet IDs provided for generating specific IP restriction rules.");
        }

        //generate ip rules for vnet per naming convention
        HashSet<IpSecurityRestrictionRule> vnetIpRulesPerNamingConvention = GenerateIpRulesBySubnetNamingConvention(resourceIds!, allSubnetIds);
        generatedIpRules.UnionWith(vnetIpRulesPerNamingConvention);
      }
      catch (Exception ex) 
      {
        FunctionLogger.MethodException(_logger, ex, "Exception Occured in Generating IP restriction rules");
      }
      FunctionLogger.MethodInformation(_logger, $"Total IP restriction rules generated are: {generatedIpRules.Count}");

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
      FunctionLogger.MethodStart(_logger, nameof(GenerateIpRulesBySubnetNamingConvention));
      var generatedIpRules = new HashSet<IpSecurityRestrictionRule>();
      // Validate input: Check if existing resources are provided
      if (existingResources == null || !existingResources.Any())
      {
        FunctionLogger.MethodWarning(_logger, "No existing resources provided; returning an empty rule set.");
        return generatedIpRules; // Return early if there are no resources
      }

      try
      {
        var intersectedSubnetIds = FindIntersectedSubnetIds(existingResources, allSubnetIds);
        if (!intersectedSubnetIds.Any())
        {
          FunctionLogger.MethodInformation(_logger, "No intersected subnet IDs found; no IP rules will be generated.");
          return generatedIpRules; // Return early if no intersected subnets are found
        }

        foreach (var subnetId in intersectedSubnetIds)
        {
          generatedIpRules.Add(IpRulesHelper.GenerateDynamicAllowListingRuleForVnet(
            subnetId.Key,   //resource name
            subnetId.Value  //subnet Id
            ));

          FunctionLogger.MethodInformation(_logger, $"Generated IP rule for VNet: {subnetId.Key}, Subnet ID: {subnetId.Value}");
        }
        FunctionLogger.MethodInformation(
            _logger,
            $"Successfully generated {generatedIpRules.Count} IP rules based on subnet naming convention."
        );
        return generatedIpRules;
      }
      catch(Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw; // Re-throw the exception after logging it
      }
    }

    internal List<KeyValuePair<string, string>> FindIntersectedSubnetIds(List<IAzureResource> existingResources,
      List<string> allSubnetIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(FindIntersectedSubnetIds));
      var intersectedSubnetIds = new List<KeyValuePair<string, string>>();
      
      // Validate input: Check if subnet IDs are provided
      if (allSubnetIds == null || !allSubnetIds.Any())
      {
        FunctionLogger.MethodWarning(_logger, "No subnet IDs provided; returning an empty list of intersected subnet IDs.");
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

        FunctionLogger.MethodInformation(_logger, $"Expected subnet names: {string.Join(", ", expectedSubnetNameList)}");

        // Identify intersected subnet names
        var intersectedNames = expectedSubnetNameList.Intersect(allSubnetIdTuple.Select(x => x.Key)).ToList();

        // Build the result for intersected subnet IDs
        intersectedSubnetIds = allSubnetIdTuple
           .Where(x => x.Key != null && intersectedNames.Contains(x.Key))
           .Select(x => new KeyValuePair<string, string>
           (
             x.Key.Split('-').Last(), //name
             x.Value                  //subnetId
           )).ToList();

        // Log the found intersected subnet IDs
        if (intersectedSubnetIds.Any())
        {
            FunctionLogger.MethodInformation(
                _logger,
                $"Found intersected subnet IDs: {string.Join(", ", intersectedSubnetIds.Select(id => id.Value))}"
            );
        }
        else
        {
            FunctionLogger.MethodInformation(_logger, "No intersected subnet IDs found.");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw; // Re-throw the exception after logging it
      }
      return intersectedSubnetIds;
    }
  }
}