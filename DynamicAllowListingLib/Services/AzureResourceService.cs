using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.ResourceGraphResponses;
using DynamicAllowListingLib.ServiceTagManagers;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DynamicAllowListingLib.Models.VNets;

namespace DynamicAllowListingLib.Services
{
  public interface IAzureResourceServiceFactory
  {
    IAzureResourceService CreateInstance(ResourceDependencyInformation resourceDependencyInformation);
  }

  public class AzureResourceServiceFactory : IAzureResourceServiceFactory
  {
    private readonly IServiceProvider _serviceProvider;
    public AzureResourceServiceFactory(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }
    public IAzureResourceService CreateInstance(ResourceDependencyInformation resourceDependencyInformation)
    {
      return ActivatorUtilities.CreateInstance<AzureResourceService>(_serviceProvider, resourceDependencyInformation);
    }
  }

  public interface IAzureResourceService : IDisposable
  {
    Task<IAzureResource?> GetAzureResource(string resourceId);
    Task<HashSet<IAzureResource>> GetAzureResources();
    Task<NetworkRestrictionSettings> GetUpdateNetworkRestrictionSettingsForMainResource();
    Task<HashSet<NetworkRestrictionSettings>> GetAppendNetworkRestrictionSettings();
    ResultObject ResultObject { get; }
  }

  public class AzureResourceService : IAzureResourceService
  {
    private HashSet<IAzureResource>? _azureResources;
    private List<string>? _subnets;
    private string[]? _resourceIdsWhereMainResourceOutbound;

    private readonly ResourceDependencyInformation _resourceDependencyInformation;
    private readonly IResourceDependencyInformationPersistenceService _dependencyInformationPersistenceService;
    private readonly IResourceGraphExplorerService _resourceGraphExplorerService;
    private readonly IRestHelper _restHelper;
    private readonly IIpRestrictionRuleGeneratorService _ipRulesService;
    private readonly IServiceTagManagerProvider _serviceTagManagerProvider;
    private readonly ILogger<AzureResourceService> _logger;
    private readonly PerformanceLogger _performanceLogger;

    private ResultObject? _resultObject;

    public ResultObject ResultObject => _resultObject ??= new ResultObject();

    public AzureResourceService(ResourceDependencyInformation resourceDependencyInformation,
      IRestHelper restHelper,
      IResourceDependencyInformationPersistenceService dependencyInformationPersistenceService,
      IResourceGraphExplorerService resourceGraphExplorerService,
      IIpRestrictionRuleGeneratorService ipRulesService,
      IServiceTagManagerProvider serviceTagManagerProvider,
      ILogger<AzureResourceService> logger)
    {
      _resourceDependencyInformation = resourceDependencyInformation;
      _dependencyInformationPersistenceService = dependencyInformationPersistenceService;
      _logger = logger;
      _ipRulesService = ipRulesService;
      _serviceTagManagerProvider = serviceTagManagerProvider;
      _resourceGraphExplorerService = resourceGraphExplorerService;
      _restHelper = restHelper;
      _performanceLogger = new PerformanceLogger(logger);
    }

    public async Task<HashSet<NetworkRestrictionSettings>> GetAppendNetworkRestrictionSettings()
    {
      using (_performanceLogger.TrackPerformance(
        nameof(GetAppendNetworkRestrictionSettings),
        new Dictionary<string, object>
        {
          ["ResourceId"] = _resourceDependencyInformation.ResourceId ?? "Unknown",
          ["CorrelationId"] = CorrelationContext.CorrelationId
        }))
      {
        if (!string.IsNullOrEmpty(_resourceDependencyInformation.ResourceId))
        {
          if (_resourceDependencyInformation.AllowOutbound?.ResourceIds != null &&
              _resourceDependencyInformation.AllowOutbound?.ResourceIds.Length > 0)
          {
            var allowOutboundResourceIds = _resourceDependencyInformation.AllowOutbound.ResourceIds;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
              ["ResourceId"] = _resourceDependencyInformation.ResourceId,
              ["OutboundResourceCount"] = allowOutboundResourceIds.Length,
              ["CorrelationId"] = CorrelationContext.CorrelationId
            }))
            {
              _logger.LogInformation(
                "Getting append network restriction settings | ResourceId: {ResourceId} | OutboundCount: {Count}",
                _resourceDependencyInformation.ResourceId,
                allowOutboundResourceIds.Length);

              return await GetAppendNetworkRestrictionSettings(
                _resourceDependencyInformation.ResourceId,
                allowOutboundResourceIds);
            }
          }
        }
        return new HashSet<NetworkRestrictionSettings>();
      }
    }

    private async Task<HashSet<NetworkRestrictionSettings>> GetAppendNetworkRestrictionSettings(
      string resourceIdToAllow,
      IEnumerable<string> resourceIdsToApplyRuleTo)
    {
      var networkRestrictionSettingsHashSet = new HashSet<NetworkRestrictionSettings>();

      try
      {
        var resourceRules = await GetResourceRules(new[] { resourceIdToAllow });

        if (resourceRules.Count <= 0)
        {
          _logger.LogWarning(
            "No rules found for ResourceIdToAllow: {ResourceId}",
            resourceIdToAllow);
          return networkRestrictionSettingsHashSet;
        }

        foreach (var resourceId in resourceIdsToApplyRuleTo)
        {
          var nrs = new NetworkRestrictionSettings
          {
            ResourceId = resourceId,
            IpSecRules = resourceRules
          };
          networkRestrictionSettingsHashSet.Add(nrs);

          _logger.LogInformation(
            "Added network restriction settings | TargetResourceId: {ResourceId} | RuleCount: {RuleCount}",
            resourceId,
            resourceRules.Count);
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GetAppendNetworkRestrictionSettings),
          _resourceDependencyInformation,
          new Dictionary<string, object>
          {
            ["ResourceIdToAllow"] = resourceIdToAllow,
            ["TargetResourceCount"] = resourceIdsToApplyRuleTo.Count()
          });
        throw;
      }

      return networkRestrictionSettingsHashSet;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetResourceRules(string[] resourceIds)
    {
      using (_performanceLogger.TrackPerformance(
        nameof(GetResourceRules),
        new Dictionary<string, object>
        {
          ["ResourceCount"] = resourceIds.Length,
          ["CorrelationId"] = CorrelationContext.CorrelationId
        }))
      {
        var ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();

        if (!resourceIds.Any())
        {
          _logger.LogWarning("No resource IDs provided to GetResourceRules");
          return ipSecurityRestrictionRules;
        }

        try
        {
          string resourceIDs = string.Join(", ", resourceIds);

          using (_logger.BeginScope(new Dictionary<string, object>
          {
            ["ResourceIds"] = resourceIDs,
            ["ResourceCount"] = resourceIds.Length,
            ["CorrelationId"] = CorrelationContext.CorrelationId
          }))
          {
            _logger.LogInformation(
              "Getting resource rules | ResourceCount: {Count}",
              resourceIds.Length);

            List<IAzureResource> azureResources = await GetAzureResources(resourceIds);

            var subscriptionIds = StringHelper.GetSubscriptionIds(resourceIds);
            var subnetIds = await GetAllSubnetIds(subscriptionIds);
            List<string> subnetIdsFromConfig = FilterValidSubnetIds(resourceIds, subnetIds);

            ipSecurityRestrictionRules = _ipRulesService.GenerateIpRestrictionRules(
              azureResources,
              subnetIdsFromConfig,
              subnetIds);

            LogVnetSubnetIntegrationWarnings(ipSecurityRestrictionRules);

            _logger.LogInformation(
              "Generated IP security restriction rules | RuleCount: {Count}",
              ipSecurityRestrictionRules.Count);
          }
        }
        catch (Exception ex)
        {
          _logger.LogOperationException(
            ex,
            nameof(GetResourceRules),
            null,
            new Dictionary<string, object>
            {
              ["ResourceIds"] = string.Join(", ", resourceIds)
            });
          throw;
        }

        return ipSecurityRestrictionRules;
      }
    }

    internal async Task<List<IAzureResource>> GetAzureResources(IEnumerable<string> resourceIds)
    {
      List<IAzureResource> azureResources = new List<IAzureResource>();

      try
      {
        var initializedAzureResources = await GetAzureResources();

        foreach (string resourceId in resourceIds)
        {
          var matchingResources = initializedAzureResources
            .Where(azureResource => azureResource.Id != null &&
                   azureResource.Id.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

          if (matchingResources.Any())
          {
            azureResources.AddRange(matchingResources);
            _logger.LogDebug("Found resource for ResourceId: {ResourceId}", resourceId);
          }
          else
          {
            string errorMessage = $"No matching resource found for Resource ID: {resourceId}";
            ResultObject.Errors.Add(errorMessage);
            _logger.LogWarning(errorMessage);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GetAzureResources),
          null,
          new Dictionary<string, object>
          {
            ["ResourceIds"] = string.Join(", ", resourceIds)
          });
        throw;
      }

      return azureResources;
    }

    public async Task<IAzureResource?> GetAzureResource(string resourceId)
    {
      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogWarning("Provided ResourceId is null or empty");
        return null;
      }

      try
      {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
          ["ResourceId"] = resourceId,
          ["CorrelationId"] = CorrelationContext.CorrelationId
        }))
        {
          _logger.LogInformation("Fetching resource | ResourceId: {ResourceId}", resourceId);

          var azureResources = await GetAzureResources();
          var matchedResource = azureResources.FirstOrDefault(azureResource =>
            azureResource.Id != null &&
            azureResource.Id.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase));

          if (matchedResource != null)
          {
            _logger.LogInformation(
              "Found resource | ResourceName: {ResourceName} | ResourceId: {ResourceId}",
              matchedResource.Name,
              resourceId);
          }
          else
          {
            string errorMessage = $"No matching resource found for Resource ID: {resourceId}";
            ResultObject.Errors.Add(errorMessage);
            _logger.LogWarning(errorMessage);
          }

          return matchedResource;
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GetAzureResource),
          null,
          new Dictionary<string, object>
          {
            ["ResourceId"] = resourceId
          });
        throw;
      }
    }

    public async Task<HashSet<IAzureResource>> GetAzureResources()
    {
      try
      {
        if (_azureResources == null)
        {
          using (_performanceLogger.TrackPerformance(
            "InitializeAzureResources",
            new Dictionary<string, object>
            {
              ["MainResourceId"] = _resourceDependencyInformation.ResourceId ?? "Unknown",
              ["CorrelationId"] = CorrelationContext.CorrelationId
            }))
          {
            _logger.LogInformation("Initializing Azure resources");
            _azureResources = await InitializeAzureResources(_resourceDependencyInformation);

            if (_azureResources != null && _azureResources.Any())
            {
              _logger.LogInformation(
                "Initialized Azure resources | Count: {Count}",
                _azureResources.Count);
            }
            else
            {
              string errorMessage = "No Azure resources found during initialization";
              ResultObject.Errors.Add(errorMessage);
              _logger.LogWarning(errorMessage);
            }
          }
        }
        else
        {
          _logger.LogDebug(
            "Returning cached Azure resources | Count: {Count}",
            _azureResources.Count);
        }

        return _azureResources ?? new HashSet<IAzureResource>();
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GetAzureResources),
          _resourceDependencyInformation);
        return new HashSet<IAzureResource>();
      }
    }

    private async Task<HashSet<IAzureResource>> InitializeAzureResources(
      ResourceDependencyInformation resourceDependencyInformation)
    {
      var azureResources = new HashSet<IAzureResource>();

      try
      {
        if (string.IsNullOrEmpty(resourceDependencyInformation.ResourceId))
        {
          var ex = new Exception($"The main resourceId was found to be null or empty for Resource: {resourceDependencyInformation.ResourceName}");
          _logger.LogError(ex, "Invalid resource configuration");
          throw ex;
        }

        List<string> azureResourceIds = new List<string> { resourceDependencyInformation.ResourceId };

        _resourceIdsWhereMainResourceOutbound = await _dependencyInformationPersistenceService
          .GetResourceIdsWhereOutbound(resourceDependencyInformation.ResourceId);
        azureResourceIds.AddRange(_resourceIdsWhereMainResourceOutbound);

        if (resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds != null &&
            resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds.Length > 0)
        {
          _logger.LogDebug("Adding AllowInbound SecurityRestrictions ResourceIds");
          azureResourceIds.AddRange(resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds ?? Array.Empty<string>());
        }

        if (resourceDependencyInformation.AllowOutbound?.ResourceIds != null &&
            resourceDependencyInformation.AllowOutbound?.ResourceIds.Length > 0)
        {
          _logger.LogDebug("Adding AllowOutbound ResourceIds");
          azureResourceIds.AddRange(resourceDependencyInformation.AllowOutbound?.ResourceIds ?? Array.Empty<string>());
        }

        string resourceIDs = string.Join(",", azureResourceIds);

        _logger.LogInformation(
          "Instantiating resources | ResourceCount: {Count}",
          azureResourceIds.Count);

        var subscriptionIds = StringHelper.GetSubscriptionIds(azureResourceIds.ToArray());
        var azureResourceInstances = await _resourceGraphExplorerService
          .GetResourceInstances(subscriptionIds, azureResourceIds);

        if (azureResourceInstances != null)
        {
          foreach (var azureResource in azureResourceInstances)
          {
            _logger.LogDebug(
              "Initialized Azure resource | ResourceName: {Name}",
              azureResource.Name);
            azureResources.Add(azureResource);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(InitializeAzureResources),
          resourceDependencyInformation);
        throw;
      }

      return azureResources;
    }

    private async Task<string> GetAzureResourceType(string resourceId)
    {
      try
      {
        var azureResources = await GetAzureResources();
        var azureResource = azureResources.FirstOrDefault(a => a.Id!.ToLower().Equals(resourceId.ToLower()));

        if (azureResource == null)
        {
          _logger.LogWarning(
            "Resource ID not found in Azure resources | ResourceId: {ResourceId}",
            resourceId);
          return string.Empty;
        }

        if (string.IsNullOrEmpty(azureResource.Type))
        {
          _logger.LogWarning(
            "Resource found but no type available | ResourceId: {ResourceId}",
            resourceId);
          return string.Empty;
        }

        _logger.LogDebug(
          "Found Azure ResourceType | ResourceType: {Type} | ResourceId: {ResourceId}",
          azureResource.Type,
          resourceId);

        return azureResource.Type;
      }
      catch (Exception ex)
      {
        _logger.LogError(
          ex,
          "Error getting Azure resource type | ResourceId: {ResourceId}",
          resourceId);
        return string.Empty;
      }
    }

    public async Task<NetworkRestrictionSettings> GetUpdateNetworkRestrictionSettingsForMainResource()
    {
      using (_performanceLogger.TrackPerformance(
        nameof(GetUpdateNetworkRestrictionSettingsForMainResource),
        new Dictionary<string, object>
        {
          ["ResourceId"] = _resourceDependencyInformation.ResourceId ?? "Unknown",
          ["ResourceName"] = _resourceDependencyInformation.ResourceName ?? "Unknown",
          ["CorrelationId"] = CorrelationContext.CorrelationId
        }))
      {
        var nrs = new NetworkRestrictionSettings();

        try
        {
          var resourceName = _resourceDependencyInformation.ResourceName;

          using (_logger.BeginScope(new Dictionary<string, object>
          {
            ["ResourceId"] = _resourceDependencyInformation.ResourceId ?? "Unknown",
            ["ResourceName"] = resourceName ?? "Unknown",
            ["CorrelationId"] = CorrelationContext.CorrelationId
          }))
          {
            _logger.LogInformation(
              "Generating IP Sec rules for inbound configurations | ResourceName: {ResourceName}",
              resourceName);

            var ipSecRules = await GenerateIpSecRules();

            if (!string.IsNullOrEmpty(_resourceDependencyInformation.ResourceId))
            {
              if (_resourceIdsWhereMainResourceOutbound == null)
              {
                _resourceIdsWhereMainResourceOutbound = await _dependencyInformationPersistenceService
                  .GetResourceIdsWhereOutbound(_resourceDependencyInformation.ResourceId);
              }

              if (_resourceIdsWhereMainResourceOutbound.Any())
              {
                _logger.LogInformation(
                  "Generating rules for outbound resources | ResourceName: {ResourceName} | OutboundCount: {Count}",
                  resourceName,
                  _resourceIdsWhereMainResourceOutbound.Length);

                var ipSecFromOtherResourceOutbound = await GetResourceRules(_resourceIdsWhereMainResourceOutbound);

                _logger.LogInformation(
                  "Generated outbound rules | ResourceName: {ResourceName} | RuleCount: {Count}",
                  resourceName,
                  ipSecFromOtherResourceOutbound.Count);

                ipSecRules.UnionWith(ipSecFromOtherResourceOutbound);
              }
            }
            else
            {
              _logger.LogWarning("ResourceId is null or empty, skipping outbound rules generation");
            }

            var scmIpSecRules = await GenerateScmIpSecRules();

            nrs = new NetworkRestrictionSettings
            {
              ResourceId = _resourceDependencyInformation.ResourceId,
              IpSecRules = ipSecRules,
              ScmIpSecRules = scmIpSecRules
            };

            _logger.LogInformation(
              "Generated network restriction settings | ResourceName: {ResourceName} | IpRuleCount: {IpCount} | ScmRuleCount: {ScmCount}",
              resourceName,
              nrs.IpSecRules.Count,
              nrs.ScmIpSecRules.Count);

            return nrs;
          }
        }
        catch (Exception ex)
        {
          _logger.LogOperationException(
            ex,
            nameof(GetUpdateNetworkRestrictionSettingsForMainResource),
            _resourceDependencyInformation);
          throw;
        }
      }
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GenerateIpSecRules()
    {
      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();

      try
      {
        Dictionary<IpSecurityRestrictionRule, List<string>> frontDoorServiceTagWithHttpFilters =
          new Dictionary<IpSecurityRestrictionRule, List<string>>();

        if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds != null &&
            _resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds.Length > 0)
        {
          frontDoorServiceTagWithHttpFilters = await GenerateFrontDoorServiceTag(
            _resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds);

          if (frontDoorServiceTagWithHttpFilters.Any())
          {
            ipSecurityRestrictionRules.Add(frontDoorServiceTagWithHttpFilters.FirstOrDefault().Key);
            _logger.LogDebug("Added FrontDoor service tag");
          }

          var allResourceIds = _resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds;
          var excludedResourceIds = frontDoorServiceTagWithHttpFilters.Values.SelectMany(x => x);
          var filteredResourceIds = allResourceIds.Where(resourceId => !excludedResourceIds.Contains(resourceId)).ToArray();

          var securityRestrictionRulesForResourceIds = await GetResourceRules(filteredResourceIds);
          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForResourceIds);

          _logger.LogInformation(
            "Generated IP rules from AllowInbound | ResourceName: {ResourceName} | RuleCount: {Count}",
            _resourceDependencyInformation.ResourceName,
            securityRestrictionRulesForResourceIds.Count);
        }

        if (_resourceDependencyInformation.ResourceId != null)
        {
          if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.AzureServiceTags != null &&
              _resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.AzureServiceTags.Length > 0)
          {
            string[] azureServiceTags = _resourceDependencyInformation.AllowInbound.SecurityRestrictions.AzureServiceTags;

            if (frontDoorServiceTagWithHttpFilters.Any())
            {
              azureServiceTags = azureServiceTags
                .Where(tag => tag.ToLower() != "AzureFrontDoor.Backend".ToLower())
                .ToArray();
            }

            var securityRestrictionRulesForAzureServiceTags = await GetAzureServiceTagRules(
              _resourceDependencyInformation.ResourceId,
              azureServiceTags);
            ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForAzureServiceTags);
          }

          var typeFromRest = await GetAzureResourceType(_resourceDependencyInformation.ResourceId);
          var resourceType = string.IsNullOrEmpty(typeFromRest) ?
            _resourceDependencyInformation.ResourceType! : typeFromRest;

          var newDayAndThirdPartyTags = GetDefaultNewDayAndThirdPartyTagsForResourceType(resourceType);

          _logger.LogDebug(
            "Got default NewDay and third-party tags | ResourceName: {ResourceName} | TagCount: {Count}",
            _resourceDependencyInformation.ResourceName,
            newDayAndThirdPartyTags.Count);

          string subscriptionId = StringHelper.GetSubscriptionId(_resourceDependencyInformation.ResourceId);

          if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags != null)
          {
            newDayAndThirdPartyTags.AddRange(
              _resourceDependencyInformation.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags);
          }

          _logger.LogDebug(
            "Total NewDay and third-party tags | ResourceName: {ResourceName} | TagCount: {Count}",
            _resourceDependencyInformation.ResourceName,
            newDayAndThirdPartyTags.Count);

          bool includeMandatoryRules = resourceType.Equals(AzureResourceType.WebSite);

          var securityRestrictionRulesForNewDayInternalAndThirdPartyTags =
            await GetNewDayInternalAndThirdPartyTagsRules(
              subscriptionId,
              newDayAndThirdPartyTags,
              includeMandatoryRulesForSubscription: includeMandatoryRules);

          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForNewDayInternalAndThirdPartyTags);

          _logger.LogInformation(
            "Generated IP rules from NewDay tags | ResourceName: {ResourceName} | RuleCount: {Count}",
            _resourceDependencyInformation.ResourceName,
            securityRestrictionRulesForNewDayInternalAndThirdPartyTags.Count());
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GenerateIpSecRules),
          _resourceDependencyInformation);
        throw;
      }

      return ipSecurityRestrictionRules;
    }

    public async Task<Dictionary<IpSecurityRestrictionRule, List<string>>> GenerateFrontDoorServiceTag(string[] resourceIds)
    {
      if (resourceIds == null || resourceIds.Length == 0)
      {
        _logger.LogWarning("No resource IDs provided for FrontDoor service tag generation");
        return new Dictionary<IpSecurityRestrictionRule, List<string>>();
      }

      var result = new Dictionary<IpSecurityRestrictionRule, List<string>>();

      try
      {
        var frontdoorResourceIds = resourceIds.Where(x => IsFrontDoorResourceId(x)).ToList();

        if (!frontdoorResourceIds.Any())
        {
          _logger.LogDebug("No FrontDoor resource IDs found in provided ResourceIds");
          return result;
        }

        var FDIDs = await _resourceGraphExplorerService.GetFrontDoorUniqueInstanceIds(frontdoorResourceIds);

        if (FDIDs.Count > 8)
        {
          string errorMessage = $"Cannot generate more than 8 unique instance IDs for Front Door! Generated FDID Count: {FDIDs.Count}";
          _logger.LogError(errorMessage);
          throw new InvalidOperationException(errorMessage);
        }

        var serviceTag = "AzureFrontDoor.Backend";
        var ipSecurityRestrictionRule = new IpSecurityRestrictionRule
        {
          Name = serviceTag,
          IpAddress = serviceTag,
          Headers = new Dictionary<string, string[]>
          {
            { "X-Azure-FDID", FDIDs.Values.ToArray() }
          },
          Action = "Allow",
          Priority = 500,
          Tag = "ServiceTag"
        };

        result.Add(ipSecurityRestrictionRule, frontdoorResourceIds);

        string FIDs = string.Join(", ", FDIDs.Values);
        string FRIDs = string.Join(", ", frontdoorResourceIds);

        _logger.LogInformation(
          "Generated FrontDoor IP Security Restriction Rule | FDIDs: {FDIDs} | ResourceIds: {ResourceIds}",
          FIDs,
          FRIDs);
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GenerateFrontDoorServiceTag),
          null,
          new Dictionary<string, object>
          {
            ["ResourceIds"] = string.Join(", ", resourceIds)
          });
        throw;
      }

      return result;
    }

    private bool IsFrontDoorResourceId(string resourceId)
    {
      bool isFrontDoor = !string.IsNullOrEmpty(resourceId) &&
                         resourceId.Contains("/providers/Microsoft.Network/frontDoors/", StringComparison.OrdinalIgnoreCase);

      _logger.LogDebug(
        "Checked resource ID for FrontDoor | ResourceId: {ResourceId} | IsFrontDoor: {IsFrontDoor}",
        resourceId,
        isFrontDoor);

      return isFrontDoor;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetAzureServiceTagRules(
      string resourceId,
      string[] serviceTags)
    {
      if (serviceTags == null || serviceTags.Length == 0)
      {
        _logger.LogWarning("Service tags are null or empty. No rules will be generated");
        return new HashSet<IpSecurityRestrictionRule>();
      }

      string tags = string.Join(", ", serviceTags);
      HashSet<IpSecurityRestrictionRule> rules = new HashSet<IpSecurityRestrictionRule>();

      try
      {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
          ["ResourceId"] = resourceId,
          ["ServiceTags"] = tags,
          ["CorrelationId"] = CorrelationContext.CorrelationId
        }))
        {
          var resourceType = await GetAzureResourceType(resourceId);
          IServiceTagManager provider;

          if (resourceType == AzureResourceType.WebSiteSlot || resourceType == AzureResourceType.WebSite)
          {
            provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.AzureWeb);
            _logger.LogDebug("Using AzureWeb service tag manager");
          }
          else
          {
            provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.Azure);
            _logger.LogDebug("Using Azure service tag manager");
          }

          _logger.LogInformation(
            "Generating rules with Azure service tags | Tags: {Tags}",
            tags);

          rules = await provider.GenerateRulesByName("", serviceTags);

          _logger.LogInformation(
            "Generated Azure service tag rules | ResourceId: {ResourceId} | RuleCount: {Count}",
            resourceId,
            rules.Count);
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GetAzureServiceTagRules),
          null,
          new Dictionary<string, object>
          {
            ["ResourceId"] = resourceId,
            ["ServiceTags"] = string.Join(", ", serviceTags)
          });
        return new HashSet<IpSecurityRestrictionRule>();
      }

      return rules;
    }

    internal List<string> GetDefaultNewDayAndThirdPartyTagsForResourceType(string azureResourceType)
    {
      List<string> newDayTags = new List<string>()
      {
        "OctopusWorkers",
        "Checkpoint.UKS.ITInfraPro",
        "Bastion.STG",
        "Bastion.PRD",
        "W365Desktops",
        "VPN.DIA.Leeds",
        "VPN.DIA.London",
        "KCOM.Leeds",
        "YLD",
        "zscaler",
        "fwazuprdneu01",
        "fwazuprdweu01",
        "fwazustgneu01",
        "fwazustgweu01",
        "NDTFirewallHub.North.DEV"
      };

      try
      {
        switch (azureResourceType)
        {
          case AzureResourceType.Storage:
            newDayTags.AddRange(new[]
            {
              "Checkpoint.UKS.ITInfraPro.UAT",
              "Checkpoint.WE.ndPRO.ITInfra.UAT",
              "Checkpoint.WE.ndPRO.ITInfra",
              "Octopus.Subnet",
              "Bastion.PRD.Subnet",
              "W365Desktops.Subnets"
            });
            break;
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(
          ex,
          "Error getting default NewDay tags | ResourceType: {ResourceType}",
          azureResourceType);
      }

      return newDayTags;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GenerateScmIpSecRules()
    {
      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();

      try
      {
        var scmResourceIds = _resourceDependencyInformation.AllowInbound?.ScmSecurityRestrictions?.ResourceIds;

        if (scmResourceIds != null && scmResourceIds.Length > 0)
        {
          string resourceIDS = string.Join(", ", scmResourceIds);

          _logger.LogInformation(
            "Generating SCM rules for ResourceIds | ResourceIds: {ResourceIds}",
            resourceIDS);

          var securityRestrictionRulesForResourceIds = await GetResourceRules(scmResourceIds);
          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForResourceIds);

          _logger.LogInformation(
            "Generated SCM IP rules from ResourceIds | RuleCount: {Count}",
            securityRestrictionRulesForResourceIds.Count);
        }

        if (_resourceDependencyInformation.ResourceId != null)
        {
          var scmServiceTags = _resourceDependencyInformation.AllowInbound?.ScmSecurityRestrictions?.AzureServiceTags;

          if (scmServiceTags != null && scmServiceTags.Length > 0)
          {
            string serviceTags = string.Join(", ", scmServiceTags);

            _logger.LogInformation(
              "Generating SCM rules for AzureServiceTags | ServiceTags: {ServiceTags}",
              serviceTags);

            var securityRestrictionRulesForAzureServiceTags = await GetAzureServiceTagRules(
              _resourceDependencyInformation.ResourceId,
              scmServiceTags);
            ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForAzureServiceTags);

            _logger.LogInformation(
              "Generated SCM IP rules from AzureServiceTags | RuleCount: {Count}",
              securityRestrictionRulesForAzureServiceTags.Count);
          }

          var tagsList = new List<string>(new[]
          {
            "OctopusWorkers",
            "Bastion.STG",
            "Bastion.PRD",
            "W365Desktops",
            "VPN.DIA.Leeds",
            "VPN.DIA.London",
            "KCOM.Leeds",
            "Masergy.DIA.Breakout",
            "zscaler",
            "fwazuprdneu01",
            "fwazuprdweu01",
            "fwazustgneu01",
            "fwazustgweu01",
            "NDTFirewallHub.North.DEV"
          });

          if (_resourceDependencyInformation.AllowInbound?.ScmSecurityRestrictions?.NewDayInternalAndThirdPartyTags != null)
          {
            tagsList.AddRange(
              _resourceDependencyInformation.AllowInbound.ScmSecurityRestrictions.NewDayInternalAndThirdPartyTags);
          }

          string tags = string.Join(", ", tagsList);

          _logger.LogInformation(
            "Generating SCM rules for NewDay tags | Tags: {Tags}",
            tags);

          string subscriptionId = StringHelper.GetSubscriptionId(_resourceDependencyInformation.ResourceId);
          var securityRestrictionRulesForNewDayInternalAndThirdPartyTags =
            await GetNewDayInternalAndThirdPartyTagsRules(
              subscriptionId,
              tagsList,
              includeMandatoryRulesForSubscription: false);

          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForNewDayInternalAndThirdPartyTags);

          if (securityRestrictionRulesForNewDayInternalAndThirdPartyTags.Any())
          {
            _logger.LogInformation(
              "Generated SCM IP rules from NewDay tags | RuleCount: {Count}",
              securityRestrictionRulesForNewDayInternalAndThirdPartyTags.Count());
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GenerateScmIpSecRules),
          _resourceDependencyInformation);
        throw;
      }

      return ipSecurityRestrictionRules;
    }

    private async Task<HashSet<IpSecurityRestrictionRule>> GetNewDayInternalAndThirdPartyTagsRules(
      string subscriptionId,
      List<string> newDayInternalAndThirdPartyTags,
      bool includeMandatoryRulesForSubscription = true)
    {
      HashSet<IpSecurityRestrictionRule> rules = new HashSet<IpSecurityRestrictionRule>();

      if (newDayInternalAndThirdPartyTags == null || !newDayInternalAndThirdPartyTags.Any())
      {
        _logger.LogWarning("No NewDay internal or third-party tags provided");
        return rules;
      }

      string tags = string.Join(", ", newDayInternalAndThirdPartyTags);

      try
      {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
          ["SubscriptionId"] = subscriptionId,
          ["ServiceTags"] = tags,
          ["IncludeMandatory"] = includeMandatoryRulesForSubscription,
          ["CorrelationId"] = CorrelationContext.CorrelationId
        }))
        {
          var provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.NewDay);

          _logger.LogInformation(
            "Generating NewDay service tag rules | SubscriptionId: {SubscriptionId} | Tags: {Tags} | IncludeMandatory: {IncludeMandatory}",
            subscriptionId,
            tags,
            includeMandatoryRulesForSubscription);

          rules = await provider.GenerateRulesByName(
            subscriptionId,
            newDayInternalAndThirdPartyTags.ToArray(),
            includeMandatoryRulesForSubscription);

          _logger.LogInformation(
            "Generated NewDay service tag rules | RuleCount: {Count}",
            rules.Count);

          return rules;
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(
          ex,
          nameof(GetNewDayInternalAndThirdPartyTagsRules),
          null,
          new Dictionary<string, object>
          {
            ["SubscriptionId"] = subscriptionId,
            ["ServiceTags"] = tags
          });
        throw new InvalidOperationException(
          "Error occurred while generating rules for NewDay internal and third-party tags.",
          ex);
      }
    }

    private void LogVnetSubnetIntegrationWarnings(HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules)
    {
      var resourceNamesWithIps = ipSecurityRestrictionRules
        .Where(x => !string.IsNullOrEmpty(x.Name) &&
                    !string.IsNullOrEmpty(x.IpAddress) &&
                    string.IsNullOrEmpty(x.VnetSubnetResourceId))
        .Select(s => s.Name).Distinct();

      var warningList = ipSecurityRestrictionRules
        .Where(x => !string.IsNullOrEmpty(x.VnetSubnetResourceId) &&
                    resourceNamesWithIps.Contains(x.Name))
        .Select(x => new { ResourceName = x.Name, SubnetId = x.VnetSubnetResourceId })
        .Distinct();

      foreach (var resource in warningList)
      {
        var warningMessage = LogMessageHelper.GetVnetAddedDueToNamingMatchMessage(
          resource.ResourceName!,
          resource.SubnetId!);
        ResultObject.Warnings.Add(warningMessage);

        _logger.LogWarning(
          "VNet added due to naming match | ResourceName: {ResourceName} | SubnetId: {SubnetId}",
          resource.ResourceName,
          resource.SubnetId);
      }
    }

    private List<string> FilterValidSubnetIds(IEnumerable<string> resourceIds, ICollection<string> allSubnets)
    {
      List<string> subnetIdsFromConfig = new List<string>();

      if (!allSubnets.Any())
      {
        _logger.LogWarning("No subnets provided to check against");
        return subnetIdsFromConfig;
      }

      try
      {
        foreach (var resourceId in resourceIds)
        {
          if (!Regex.IsMatch(resourceId, Constants.VNetSubnetIdRegex))
          {
            _logger.LogDebug(
              "Skipping resource ID - does not match VNetSubnetId regex | ResourceId: {ResourceId}",
              resourceId);
            continue;
          }

          if (allSubnets.Contains(resourceId))
          {
            subnetIdsFromConfig.Add(resourceId);
            _logger.LogDebug("Valid subnet ID found | SubnetId: {SubnetId}", resourceId);
          }
          else
          {
            var warningMessage = $"Unable to find VnetSubnetId {resourceId}.";
            ResultObject.Warnings.Add(warningMessage);
            _logger.LogWarning(warningMessage);
          }
        }

        _logger.LogInformation(
          "Filtered valid subnet IDs | ValidCount: {ValidCount} | TotalSubnets: {TotalCount}",
          subnetIdsFromConfig.Count,
          allSubnets.Count);
      }
      catch (Exception ex)
      {
        _logger.LogError(
          ex,
          "Error filtering valid subnet IDs | ResourceCount: {ResourceCount}",
          resourceIds.Count());
      }

      return subnetIdsFromConfig;
    }

    private async Task<List<string>> GetAllSubnetIds(string azureSubscriptionId)
    {
      try
      {
        if (_subnets != null)
        {
          _logger.LogDebug("Returning cached subnet IDs | Count: {Count}", _subnets.Count);
          return _subnets;
        }

        _logger.LogInformation(
          "Fetching subnet IDs from resource graph | SubscriptionId: {SubscriptionId}",
          azureSubscriptionId);

        _subnets = await _resourceGraphExplorerService.GetAllSubnetIds(azureSubscriptionId);

        _logger.LogInformation(
          "Fetched subnet IDs | SubscriptionId: {SubscriptionId} | Count: {Count}",
          azureSubscriptionId,
          _subnets.Count);

        return _subnets;
      }
      catch (Exception ex)
      {
        _logger.LogError(
          ex,
          "Error getting subnet IDs | SubscriptionId: {SubscriptionId}",
          azureSubscriptionId);
        return new List<string>();
      }
    }

    private async Task<List<string>> GetAllSubnetIds(string[] azureSubscriptionIds)
    {
      string subscriptionIDs = string.Join(", ", azureSubscriptionIds);

      try
      {
        if (_subnets == null)
        {
          _subnets = new List<string>();

          foreach (var azureSubscriptionId in azureSubscriptionIds)
          {
            _logger.LogDebug(
              "Getting subnets for subscription | SubscriptionId: {SubscriptionId}",
              azureSubscriptionId);

            try
            {
              var subnets = await _resourceGraphExplorerService.GetAllSubnetIds(azureSubscriptionId);
              _subnets.AddRange(subnets);
            }
            catch (Exception ex)
            {
              _logger.LogError(
                ex,
                "Failed to get subnets for subscription | SubscriptionId: {SubscriptionId}",
                azureSubscriptionId);
            }
          }
        }

        _logger.LogInformation(
          "Fetched subnet IDs for subscriptions | SubscriptionIds: {SubscriptionIds} | TotalCount: {Count}",
          subscriptionIDs,
          _subnets.Count);

        return _subnets;
      }
      catch (Exception ex)
      {
        _logger.LogError(
          ex,
          "Error getting subnet IDs for subscriptions | SubscriptionIds: {SubscriptionIds}",
          subscriptionIDs);
        return _subnets ?? new List<string>();
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        _azureResources = null;
        _resourceIdsWhereMainResourceOutbound = null;
        _subnets = null;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}