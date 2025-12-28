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
    private readonly TimeProvider _timeProvider;

    private ResultObject? _resultObject;

    public ResultObject ResultObject => _resultObject ??= new ResultObject();

    public AzureResourceService(
      ResourceDependencyInformation resourceDependencyInformation,
      IRestHelper restHelper,
      IResourceDependencyInformationPersistenceService dependencyInformationPersistenceService,
      IResourceGraphExplorerService resourceGraphExplorerService,
      IIpRestrictionRuleGeneratorService ipRulesService,
      IServiceTagManagerProvider serviceTagManagerProvider,
      ILogger<AzureResourceService> logger,
      TimeProvider? timeProvider = null)
    {
      _resourceDependencyInformation = resourceDependencyInformation;
      _dependencyInformationPersistenceService = dependencyInformationPersistenceService;
      _logger = logger;
      _ipRulesService = ipRulesService;
      _serviceTagManagerProvider = serviceTagManagerProvider;
      _resourceGraphExplorerService = resourceGraphExplorerService;
      _restHelper = restHelper;
      _timeProvider = timeProvider ?? TimeProvider.System;

      // Log service creation
      _logger.LogServiceCreated(
        resourceDependencyInformation.ResourceId ?? "Unknown",
        resourceDependencyInformation.ResourceName ?? "Unknown",
        resourceDependencyInformation.ResourceType ?? "Unknown");
    }

    public async Task<NetworkRestrictionSettings> GetUpdateNetworkRestrictionSettingsForMainResource()
    {
      var startTimestamp = _timeProvider.GetTimestamp();
      var resourceId = _resourceDependencyInformation.ResourceId ?? "Unknown";
      var resourceName = _resourceDependencyInformation.ResourceName ?? "Unknown";

      using (_logger.BeginAzureResourceServiceScope(
        nameof(GetUpdateNetworkRestrictionSettingsForMainResource),
        resourceId,
        resourceName))
      {
        _logger.LogGetUpdateSettingsStart(resourceId, resourceName);

        var nrs = new NetworkRestrictionSettings();

        try
        {
          // Generate IP security rules for inbound configurations
          _logger.LogGeneratingInboundRules(resourceName);
          var ipSecRules = await GenerateIpSecRules();

          // Check for resources where this resource is outbound
          if (!string.IsNullOrEmpty(_resourceDependencyInformation.ResourceId))
          {
            _logger.LogFetchingOutboundResources(resourceId);

            if (_resourceIdsWhereMainResourceOutbound == null)
            {
              _resourceIdsWhereMainResourceOutbound = await _dependencyInformationPersistenceService
                .GetResourceIdsWhereOutbound(_resourceDependencyInformation.ResourceId);
            }

            _logger.LogOutboundResourcesFound(resourceId, _resourceIdsWhereMainResourceOutbound.Length);

            if (_resourceIdsWhereMainResourceOutbound.Any())
            {
              _logger.LogGeneratingOutboundRules(resourceName, _resourceIdsWhereMainResourceOutbound.Length);

              var ipSecFromOtherResourceOutbound = await GetResourceRules(_resourceIdsWhereMainResourceOutbound);

              _logger.LogOutboundRulesGenerated(resourceName, ipSecFromOtherResourceOutbound.Count);

              ipSecRules.UnionWith(ipSecFromOtherResourceOutbound);
            }
          }
          else
          {
            _logger.LogSkippingOutboundRulesNullResourceId(resourceName);
          }

          // Generate SCM IP security rules
          var scmIpSecRules = await GenerateScmIpSecRules();

          nrs = new NetworkRestrictionSettings
          {
            ResourceId = _resourceDependencyInformation.ResourceId,
            IpSecRules = ipSecRules,
            ScmIpSecRules = scmIpSecRules
          };

          var elapsedMs = (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

          _logger.LogGetUpdateSettingsComplete(
            resourceId,
            nrs.IpSecRules.Count,
            nrs.ScmIpSecRules.Count,
            elapsedMs);

          return nrs;
        }
        catch (Exception ex)
        {
          _logger.LogMethodFailed(ex, nameof(GetUpdateNetworkRestrictionSettingsForMainResource), resourceId);
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
      var startTimestamp = _timeProvider.GetTimestamp();
      var resourceId = _resourceDependencyInformation.ResourceId ?? "Unknown";
      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();

      // Track rule counts by source for detailed logging
      int rulesFromResources = 0;
      int rulesFromServiceTags = 0;

      try
      {
        _logger.LogGenerateIpSecRulesStart(resourceId);

        Dictionary<IpSecurityRestrictionRule, List<string>> frontDoorServiceTagWithHttpFilters =
          new Dictionary<IpSecurityRestrictionRule, List<string>>();

        // Process inbound security restrictions from ResourceIds
        if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds != null &&
            _resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds.Length > 0)
        {
          var inboundResourceIds = _resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds;
          _logger.LogProcessingInboundSecurityRestrictions(resourceId, inboundResourceIds.Length);

          // Generate FrontDoor service tag if applicable
          frontDoorServiceTagWithHttpFilters = await GenerateFrontDoorServiceTag(inboundResourceIds);

          if (frontDoorServiceTagWithHttpFilters.Any())
          {
            ipSecurityRestrictionRules.Add(frontDoorServiceTagWithHttpFilters.FirstOrDefault().Key);
            _logger.LogFrontDoorServiceTagGenerated(resourceId, true);
          }
          else
          {
            _logger.LogFrontDoorServiceTagGenerated(resourceId, false);
          }

          // Get rules from Azure resources (excluding FrontDoor resources)
          var excludedResourceIds = frontDoorServiceTagWithHttpFilters.Values.SelectMany(x => x);
          var filteredResourceIds = inboundResourceIds.Where(rid => !excludedResourceIds.Contains(rid)).ToArray();

          if (filteredResourceIds.Any())
          {
            var resourceRules = await GetResourceRules(filteredResourceIds);
            rulesFromResources = resourceRules.Count;
            ipSecurityRestrictionRules.UnionWith(resourceRules);

            _logger.LogIpSecRuleBreakdown(resourceId, rulesFromResources, 0, 0, 0);
          }
        }

        // Process Azure service tags
        if (_resourceDependencyInformation.ResourceId != null)
        {
          if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.AzureServiceTags != null &&
              _resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.AzureServiceTags.Length > 0)
          {
            string[] azureServiceTags = _resourceDependencyInformation.AllowInbound.SecurityRestrictions.AzureServiceTags;
            _logger.LogProcessingAzureServiceTags(resourceId, azureServiceTags.Length, string.Join(", ", azureServiceTags));

            // Exclude FrontDoor.Backend if we already have FrontDoor rules
            if (frontDoorServiceTagWithHttpFilters.Any())
            {
              azureServiceTags = azureServiceTags
                .Where(tag => !tag.Equals("AzureFrontDoor.Backend", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            }

            var azureTagRules = await GetAzureServiceTagRules(_resourceDependencyInformation.ResourceId, azureServiceTags);
            rulesFromServiceTags += azureTagRules.Count;
            ipSecurityRestrictionRules.UnionWith(azureTagRules);

            _logger.LogAzureServiceTagRulesGenerated(resourceId, azureTagRules.Count);
          }

          // Add default NewDay and third-party tags based on resource type
          await AddDefaultNewDayTagRules(ipSecurityRestrictionRules);
        }

        var elapsedMs = (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

        _logger.LogGenerateIpSecRulesComplete(resourceId, ipSecurityRestrictionRules.Count, elapsedMs);
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GenerateIpSecRules), resourceId);
        _logger.LogOperationException(
          ex,
          nameof(GenerateIpSecRules),
          _resourceDependencyInformation);
        throw;
      }

      return ipSecurityRestrictionRules;
    }

    private async Task AddDefaultNewDayTagRules(HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules)
    {
      var resourceId = _resourceDependencyInformation.ResourceId ?? "Unknown";

      try
      {
        string? typeFromRest = null;
        if (!string.IsNullOrEmpty(_resourceDependencyInformation.ResourceId))
        {
          var mainResource = await GetAzureResource(_resourceDependencyInformation.ResourceId);
          typeFromRest = mainResource?.Type;
        }

        string resourceType = string.IsNullOrEmpty(typeFromRest)
          ? _resourceDependencyInformation.ResourceType!
          : typeFromRest;

        var newDayAndThirdPartyTags = GetDefaultNewDayAndThirdPartyTagsForResourceType(resourceType);

        _logger.LogGettingNewDayTagRules(resourceId, newDayAndThirdPartyTags.Count);

        // Add custom tags from config
        if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags != null)
        {
          newDayAndThirdPartyTags.AddRange(
            _resourceDependencyInformation.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags);
        }

        string subscriptionId = StringHelper.GetSubscriptionId(_resourceDependencyInformation.ResourceId!);
        bool includeMandatoryRules = resourceType.Equals(AzureResourceType.WebSite, StringComparison.OrdinalIgnoreCase);

        var securityRestrictionRulesForNewDayInternalAndThirdPartyTags =
          await GetNewDayInternalAndThirdPartyTagsRules(
            subscriptionId,
            newDayAndThirdPartyTags,
            includeMandatoryRulesForSubscription: includeMandatoryRules);

        ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForNewDayInternalAndThirdPartyTags);

        _logger.LogNewDayTagRulesGenerated(resourceId, securityRestrictionRulesForNewDayInternalAndThirdPartyTags.Count());
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, "AddDefaultNewDayTagRules", resourceId);
        throw;
      }
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GenerateScmIpSecRules()
    {
      var startTimestamp = _timeProvider.GetTimestamp();
      var resourceId = _resourceDependencyInformation.ResourceId ?? "Unknown";
      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();

      try
      {
        _logger.LogGenerateScmIpSecRulesStart(resourceId);

        var scmResourceIds = _resourceDependencyInformation.AllowInbound?.ScmSecurityRestrictions?.ResourceIds;
        var hasScmRestrictions = scmResourceIds != null && scmResourceIds.Length > 0;

        _logger.LogProcessingScmRestrictions(resourceId, hasScmRestrictions);

        if (hasScmRestrictions)
        {
          string resourceIDS = string.Join(", ", scmResourceIds!);
          _logger.LogFetchingResourcesForRules(resourceIDS);

          var securityRestrictionRulesForResourceIds = await GetResourceRules(scmResourceIds!);
          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForResourceIds);
        }

        // Process SCM Azure service tags
        if (_resourceDependencyInformation.ResourceId != null)
        {
          var scmServiceTags = _resourceDependencyInformation.AllowInbound?.ScmSecurityRestrictions?.AzureServiceTags;

          if (scmServiceTags != null && scmServiceTags.Length > 0)
          {
            _logger.LogProcessingAzureServiceTags(resourceId, scmServiceTags.Length, string.Join(", ", scmServiceTags));

            var securityRestrictionRulesForAzureServiceTags = await GetAzureServiceTagRules(
              _resourceDependencyInformation.ResourceId,
              scmServiceTags);
            ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForAzureServiceTags);
          }

          // Process SCM NewDay tags
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

          _logger.LogProcessingNewDayTags(resourceId, tagsList.Count, string.Join(", ", tagsList));

          string subscriptionId = StringHelper.GetSubscriptionId(_resourceDependencyInformation.ResourceId);
          var securityRestrictionRulesForNewDayInternalAndThirdPartyTags =
            await GetNewDayInternalAndThirdPartyTagsRules(
              subscriptionId,
              tagsList,
              includeMandatoryRulesForSubscription: false);

          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForNewDayInternalAndThirdPartyTags);
        }

        var elapsedMs = (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;
        _logger.LogGenerateScmIpSecRulesComplete(resourceId, ipSecurityRestrictionRules.Count, elapsedMs);
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GenerateScmIpSecRules), resourceId);
        _logger.LogOperationException(
          ex,
          nameof(GenerateScmIpSecRules),
          _resourceDependencyInformation);
        throw;
      }

      return ipSecurityRestrictionRules;
    }

    public async Task<HashSet<NetworkRestrictionSettings>> GetAppendNetworkRestrictionSettings()
    {
      var startTimestamp = _timeProvider.GetTimestamp();
      var resourceId = _resourceDependencyInformation.ResourceId ?? "Unknown";
      var resourceName = _resourceDependencyInformation.ResourceName ?? "Unknown";
      var networkRestrictionSettingsHashSet = new HashSet<NetworkRestrictionSettings>();

      using (_logger.BeginAzureResourceServiceScope(
        nameof(GetAppendNetworkRestrictionSettings),
        resourceId,
        resourceName))
      {
        _logger.LogGetAppendSettingsStart(resourceId, resourceName);

        try
        {
          string resourceIdToAllow = _resourceDependencyInformation.ResourceId ?? string.Empty;
          var resourceIdsToApplyRuleTo = _resourceDependencyInformation.AllowOutbound?.ResourceIds ?? Array.Empty<string>();

          if (!resourceIdsToApplyRuleTo.Any())
          {
            _logger.LogNoOutboundResourcesConfigured(resourceId);
            return networkRestrictionSettingsHashSet;
          }

          _logger.LogIdentifyingTargetResources(resourceIdToAllow, resourceIdsToApplyRuleTo.Length);

          // Generate rules for the source resource
          _logger.LogGeneratingRulesForSource(resourceIdToAllow);
          var resourceRules = await GetResourceRules(new[] { resourceIdToAllow });

          int totalRules = 0;

          // Create network restriction settings for each target resource
          foreach (var targetResourceId in resourceIdsToApplyRuleTo)
          {
            if (string.IsNullOrEmpty(targetResourceId))
            {
              _logger.LogResourceIdNullOrEmpty("GetAppendNetworkRestrictionSettings target");
              continue;
            }

            var nrs = new NetworkRestrictionSettings
            {
              ResourceId = targetResourceId,
              IpSecRules = resourceRules
            };

            networkRestrictionSettingsHashSet.Add(nrs);
            totalRules += resourceRules.Count;

            _logger.LogCreatedSettingsForTarget(targetResourceId, resourceRules.Count);
          }

          var elapsedMs = (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

          _logger.LogGetAppendSettingsComplete(
            resourceId,
            networkRestrictionSettingsHashSet.Count,
            totalRules,
            elapsedMs);
        }
        catch (Exception ex)
        {
          _logger.LogMethodFailed(ex, nameof(GetAppendNetworkRestrictionSettings), resourceId);
          _logger.LogOperationException(
            ex,
            nameof(GetAppendNetworkRestrictionSettings),
            _resourceDependencyInformation,
            new Dictionary<string, object>
            {
              ["ResourceIdToAllow"] = _resourceDependencyInformation.ResourceId ?? "Unknown",
              ["TargetResourceCount"] = _resourceDependencyInformation.AllowOutbound?.ResourceIds?.Length ?? 0
            });
          throw;
        }
      }

      return networkRestrictionSettingsHashSet;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetResourceRules(string[] resourceIds)
    {
      var startTimestamp = _timeProvider.GetTimestamp();
      var ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();

      _logger.LogGetResourceRulesStart(resourceIds.Length);

      if (!resourceIds.Any())
      {
        _logger.LogNoResourceIdsForRules();
        return ipSecurityRestrictionRules;
      }

      try
      {
        string resourceIDs = string.Join(", ", resourceIds);
        _logger.LogFetchingResourcesForRules(resourceIDs);

        // Get Azure resources
        List<IAzureResource> azureResources = await GetAzureResources(resourceIds);

        // Get subscription IDs and subnet IDs
        var subscriptionIds = StringHelper.GetSubscriptionIds(resourceIds);
        _logger.LogExtractingSubscriptionIds(subscriptionIds.Length);

        _logger.LogFetchingSubnetIds(subscriptionIds.Length);
        var subnetIds = await GetAllSubnetIds(subscriptionIds);
        _logger.LogSubnetIdsRetrieved(subnetIds.Count);

        // Filter valid subnet IDs from config
        List<string> subnetIdsFromConfig = FilterValidSubnetIds(resourceIds, subnetIds);
        _logger.LogFilteringValidSubnets(subnetIdsFromConfig.Count, subnetIds.Count);

        // Generate IP restriction rules
        _logger.LogGeneratingIpRestrictionRules(azureResources.Count, subnetIdsFromConfig.Count);

        ipSecurityRestrictionRules = _ipRulesService.GenerateIpRestrictionRules(
          azureResources,
          subnetIdsFromConfig,
          subnetIds);

        // Log VNet subnet integration warnings
        LogVnetSubnetIntegrationWarnings(ipSecurityRestrictionRules);

        var elapsedMs = (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;
        _logger.LogGetResourceRulesComplete(ipSecurityRestrictionRules.Count, elapsedMs);
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GetResourceRules), string.Join(", ", resourceIds));
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

    public async Task<IAzureResource?> GetAzureResource(string resourceId)
    {
      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogResourceIdNullOrEmpty("GetAzureResource");
        return null;
      }

      _logger.LogGetAzureResourceStart(resourceId);

      try
      {
        var azureResources = await GetAzureResources();
        var matchedResource = azureResources.FirstOrDefault(azureResource =>
          azureResource.Id != null &&
          azureResource.Id.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase));

        if (matchedResource != null)
        {
          _logger.LogGetAzureResourceFound(
            resourceId,
            matchedResource.Name ?? "Unknown",
            matchedResource.Type ?? "Unknown");
        }
        else
        {
          _logger.LogGetAzureResourceNotFound(resourceId);
          ResultObject.Errors.Add($"No matching resource found for Resource ID: {resourceId}");
        }

        return matchedResource;
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GetAzureResource), resourceId);
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
      var resourceId = _resourceDependencyInformation.ResourceId ?? "Unknown";

      try
      {
        if (_azureResources != null)
        {
          _logger.LogGetAzureResourcesCacheHit(resourceId, _azureResources.Count);
          return _azureResources;
        }

        _logger.LogGetAzureResourcesCacheMiss(resourceId);

        var startTimestamp = _timeProvider.GetTimestamp();

        _azureResources = await InitializeAzureResources(_resourceDependencyInformation);

        if (_azureResources != null && _azureResources.Any())
        {
          _logger.LogCachingAzureResources(resourceId, _azureResources.Count);
        }
        else
        {
          string errorMessage = "No Azure resources found during initialization";
          ResultObject.Errors.Add(errorMessage);
          _logger.LogGetAzureResourceNotFound(resourceId);
        }

        var elapsedMs = (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;
        _logger.LogAzureResourcesInitialized(resourceId, _azureResources?.Count ?? 0, elapsedMs);

        return _azureResources ?? new HashSet<IAzureResource>();
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GetAzureResources), resourceId);
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
          _logger.LogMethodFailed(ex, nameof(InitializeAzureResources), "Unknown");
          throw ex;
        }

        List<string> azureResourceIds = new List<string> { resourceDependencyInformation.ResourceId };

        // Get resources where this resource is outbound
        _resourceIdsWhereMainResourceOutbound = await _dependencyInformationPersistenceService
          .GetResourceIdsWhereOutbound(resourceDependencyInformation.ResourceId);
        azureResourceIds.AddRange(_resourceIdsWhereMainResourceOutbound);

        // Add inbound resource IDs
        if (resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds != null &&
            resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds.Length > 0)
        {
          azureResourceIds.AddRange(resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds);
        }

        // Add outbound resource IDs
        if (resourceDependencyInformation.AllowOutbound?.ResourceIds != null &&
            resourceDependencyInformation.AllowOutbound.ResourceIds.Length > 0)
        {
          azureResourceIds.AddRange(resourceDependencyInformation.AllowOutbound.ResourceIds);
        }

        var inboundCount = resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds?.Length ?? 0;
        var outboundCount = resourceDependencyInformation.AllowOutbound?.ResourceIds?.Length ?? 0;

        _logger.LogInitializingAzureResources(
          resourceDependencyInformation.ResourceId,
          inboundCount,
          outboundCount);

        var subscriptionIds = StringHelper.GetSubscriptionIds(azureResourceIds.ToArray());

        var resources = await _resourceGraphExplorerService.GetResourceInstances(
          subscriptionIds,
          azureResourceIds.Distinct().ToList());

        azureResources = resources.ToHashSet();
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(InitializeAzureResources), resourceDependencyInformation.ResourceId ?? "Unknown");
        throw;
      }

      return azureResources;
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
            _logger.LogGetAzureResourceFound(resourceId, matchingResources.First().Name ?? "Unknown", matchingResources.First().Type ?? "Unknown");
          }
          else
          {
            _logger.LogGetAzureResourceNotFound(resourceId);
            ResultObject.Errors.Add($"No matching resource found for Resource ID: {resourceId}");
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GetAzureResources), string.Join(", ", resourceIds));
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

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetAzureServiceTagRules(string resourceId, string[] serviceTags)
    {
      if (serviceTags == null || serviceTags.Length == 0)
      {
        _logger.LogNoServiceTagsProvided(resourceId);
        return new HashSet<IpSecurityRestrictionRule>();
      }

      _logger.LogGettingAzureServiceTagRules(resourceId, serviceTags.Length);

      HashSet<IpSecurityRestrictionRule> rules = new HashSet<IpSecurityRestrictionRule>();

      try
      {
        var resourceType = await GetAzureResourceType(resourceId);
        IServiceTagManager provider;

        if (resourceType == AzureResourceType.WebSiteSlot || resourceType == AzureResourceType.WebSite)
        {
          provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.AzureWeb);
          _logger.LogUsingServiceTagManager("AzureWeb", resourceType);
        }
        else
        {
          provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.Azure);
          _logger.LogUsingServiceTagManager("Azure", resourceType);
        }

        rules = await provider.GenerateRulesByName("", serviceTags);

        _logger.LogAzureServiceTagRulesGenerated(resourceId, rules.Count);
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GetAzureServiceTagRules), resourceId);
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

    private async Task<HashSet<IpSecurityRestrictionRule>> GetNewDayInternalAndThirdPartyTagsRules(
      string subscriptionId,
      List<string> newDayInternalAndThirdPartyTags,
      bool includeMandatoryRulesForSubscription = true)
    {
      HashSet<IpSecurityRestrictionRule> rules = new HashSet<IpSecurityRestrictionRule>();

      if (newDayInternalAndThirdPartyTags == null || !newDayInternalAndThirdPartyTags.Any())
      {
        _logger.LogNoServiceTagsProvided(_resourceDependencyInformation.ResourceId ?? "Unknown");
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
          // Use ManagerType.NewDay (the correct enum value)
          var provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.NewDay);

          _logger.LogGettingNewDayTagRules(_resourceDependencyInformation.ResourceId ?? "Unknown", newDayInternalAndThirdPartyTags.Count);

          rules = await provider.GenerateRulesByName(
            subscriptionId,
            newDayInternalAndThirdPartyTags.ToArray(),
            includeMandatoryRulesForSubscription);

          _logger.LogNewDayTagRulesGenerated(_resourceDependencyInformation.ResourceId ?? "Unknown", rules.Count);

          return rules;
        }
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GetNewDayInternalAndThirdPartyTagsRules), subscriptionId);
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

    public async Task<Dictionary<IpSecurityRestrictionRule, List<string>>> GenerateFrontDoorServiceTag(string[] resourceIds)
    {
      if (resourceIds == null || resourceIds.Length == 0)
      {
        _logger.LogNoServiceTagsProvided(_resourceDependencyInformation.ResourceId ?? "Unknown");
        return new Dictionary<IpSecurityRestrictionRule, List<string>>();
      }

      _logger.LogGeneratingFrontDoorTag(string.Join(", ", resourceIds));

      var result = new Dictionary<IpSecurityRestrictionRule, List<string>>();

      try
      {
        var frontdoorResourceIds = resourceIds.Where(x => IsFrontDoorResourceId(x)).ToList();

        if (!frontdoorResourceIds.Any())
        {
          return result;
        }

        var FDIDs = await _resourceGraphExplorerService.GetFrontDoorUniqueInstanceIds(frontdoorResourceIds);

        if (FDIDs.Count > 8)
        {
          string errorMessage = "Cannot generate more than 8 unique instance IDs for Front Door!";
          ResultObject.Errors.Add(errorMessage);
          _logger.LogVNetSubnetIntegrationWarning(errorMessage);
          return result;
        }

        // Generate FrontDoor service tag rule manually
        var frontDoorServiceTagRule = new IpSecurityRestrictionRule
        {
          Name = "AzureFrontDoor.Backend",
          IpAddress = "AzureFrontDoor.Backend",
          Action = "Allow",
          Priority = 100,
          Tag = "ServiceTag",
          Headers = new Dictionary<string, string[]>
          {
            { "X-Azure-FDID", FDIDs.Values.ToArray() }
          }
        };

        result.Add(frontDoorServiceTagRule, frontdoorResourceIds);

        _logger.LogFrontDoorTagGenerated(1, FDIDs.Count);
      }
      catch (Exception ex)
      {
        _logger.LogMethodFailed(ex, nameof(GenerateFrontDoorServiceTag), string.Join(", ", resourceIds));
      }

      return result;
    }

    internal async Task<List<string>> GetAllSubnetIds(string[] subscriptionIds)
    {
      var startTimestamp = _timeProvider.GetTimestamp();

      if (_subnets != null)
      {
        return _subnets;
      }

      _logger.LogGettingAllSubnetIds(string.Join(", ", subscriptionIds));

      _subnets = new List<string>();

      foreach (var subscriptionId in subscriptionIds)
      {
        var subnetIds = await _resourceGraphExplorerService.GetAllSubnetIds(subscriptionId);
        _subnets.AddRange(subnetIds);
      }

      _logger.LogCachingSubnetIds(string.Join(", ", subscriptionIds), _subnets.Count);

      var elapsedMs = (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;
      _logger.LogAllSubnetIdsRetrieved(_subnets.Count, elapsedMs);

      return _subnets;
    }

    internal List<string> FilterValidSubnetIds(string[] resourceIds, List<string> allSubnetIds)
    {
      var validSubnetIds = new List<string>();

      foreach (var resourceId in resourceIds)
      {
        if (resourceId.Contains("/subnets/", StringComparison.OrdinalIgnoreCase))
        {
          if (allSubnetIds.Any(s => s.Equals(resourceId, StringComparison.OrdinalIgnoreCase)))
          {
            validSubnetIds.Add(resourceId);
            _logger.LogGeneratingSubnetRule(resourceId, StringHelper.GetResourceName(resourceId));
          }
          else
          {
            _logger.LogInvalidSubnetSkipped(resourceId, "Subnet not found in subscription");
          }
        }
      }

      return validSubnetIds;
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
        _logger.LogVNetSubnetIntegrationWarning(warningMessage);
        ResultObject.Warnings.Add(warningMessage);
      }
    }

    internal async Task<string> GetAzureResourceType(string resourceId)
    {
      if (string.IsNullOrEmpty(resourceId))
      {
        return string.Empty;
      }

      var resource = await GetAzureResource(resourceId);
      return resource?.Type ?? string.Empty;
    }

    internal bool IsFrontDoorResourceId(string resourceId)
    {
      return !string.IsNullOrEmpty(resourceId) &&
             resourceId.Contains("Microsoft.Network/frontDoors", StringComparison.OrdinalIgnoreCase);
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
        _logger.LogMethodFailed(ex, "GetDefaultNewDayAndThirdPartyTagsForResourceType", azureResourceType);
      }

      return newDayTags;
    }

    public void Dispose()
    {
      _logger.LogServiceDisposed(_resourceDependencyInformation.ResourceId ?? "Unknown");
    }
  }
}