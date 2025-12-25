using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.ResourceGraphResponses;
using DynamicAllowListingLib.ServiceTagManagers;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    }

    public async Task<HashSet<NetworkRestrictionSettings>> GetAppendNetworkRestrictionSettings()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAppendNetworkRestrictionSettings));
      if (!string.IsNullOrEmpty(_resourceDependencyInformation.ResourceId))
      {
        // If AllowOutbound resourceIds are provided, call the internal method
        if (_resourceDependencyInformation.AllowOutbound?.ResourceIds != null && _resourceDependencyInformation.AllowOutbound?.ResourceIds.Length > 0)
        {
          var allowOutboundResourceIds = _resourceDependencyInformation.AllowOutbound.ResourceIds;
          FunctionLogger.MethodInformation(_logger, $"Calling GetAppendNetworkRestrictionSettings with ResourceId: {_resourceDependencyInformation.ResourceId} and AllowOutboundResourceIds: {string.Join(", ", allowOutboundResourceIds)}");
          return await GetAppendNetworkRestrictionSettings(_resourceDependencyInformation.ResourceId, allowOutboundResourceIds);
        }
      }
      return new HashSet<NetworkRestrictionSettings>();
    }

    private async Task<HashSet<NetworkRestrictionSettings>> GetAppendNetworkRestrictionSettings(string resourceIdToAllow, IEnumerable<string> resourceIdsToApplyRuleTo)
    {
      var networkRestrictionSettingsHashSet = new HashSet<NetworkRestrictionSettings>();
      try
      {
        // Retrieve the rules for the provided resource ID
        var resourceRules = await GetResourceRules(new[] { resourceIdToAllow });
        if (resourceRules.Count <= 0)
        {
          FunctionLogger.MethodWarning(_logger, $"No rules found for ResourceIdToAllow: {resourceIdToAllow}");
          return networkRestrictionSettingsHashSet;
        }
        // Apply rules to each specified resource ID
        foreach (var resourceId in resourceIdsToApplyRuleTo)
        {
          var nrs = new NetworkRestrictionSettings { ResourceId = resourceId, IpSecRules = resourceRules };
          networkRestrictionSettingsHashSet.Add(nrs);
          FunctionLogger.MethodInformation(_logger, $"Added NetworkRestrictionSettings for ResourceId: {resourceId}");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return networkRestrictionSettingsHashSet;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetResourceRules(string[] resourceIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetResourceRules));
      var ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();
      // Early return if no resource IDs are provided
      if (!resourceIds.Any())
      {
        FunctionLogger.MethodWarning(_logger, "No resource IDs provided to GetResourceRules.");
        return ipSecurityRestrictionRules;
      }
      try
      {
        string resourceIDs = string.Join(", ", resourceIds);
        //get generated ip rules
        List<IAzureResource> azureResources = await GetAzureResources(resourceIds);

        //if (azureResources.Any())
        //{
          FunctionLogger.MethodInformation(_logger, $"Getting Unique subscription ID from resourceIDs: {resourceIDs}");
          var subscriptionIds = StringHelper.GetSubscriptionIds(resourceIds);

          var subnetIds = await GetAllSubnetIds(subscriptionIds);
          List<string> subnetIdsFromConfig = FilterValidSubnetIds(resourceIds, subnetIds);

          ipSecurityRestrictionRules = _ipRulesService.GenerateIpRestrictionRules(azureResources, subnetIdsFromConfig, subnetIds);

          // Log potential subnet mismatch warnings
          LogVnetSubnetIntegrationWarnings(ipSecurityRestrictionRules);

          // Log the count of generated rules
          FunctionLogger.MethodInformation(_logger, $"{ipSecurityRestrictionRules.Count} IP security restriction rules generated for resource IDs: {resourceIDs}");
        //}
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return ipSecurityRestrictionRules;
    }


    internal async Task<List<IAzureResource>> GetAzureResources(IEnumerable<string> resourceIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureResources));
      List<IAzureResource> azureResources = new List<IAzureResource>();
      try
      {
        // Retrieve initialized Azure resources
        var initializedAzureResources = await GetAzureResources();
        //add condition if initialized resources are not null
        foreach (string resourceId in resourceIds)
        {
          var matchingResources = initializedAzureResources
                                          .Where(azureResource => azureResource.Id != null &&
                                                 azureResource.Id.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase))
                                          .ToList();
          if (matchingResources.Any())
          {
            azureResources.AddRange(matchingResources); 
            FunctionLogger.MethodInformation(_logger, $"Found resource for Resource ID: {resourceId}");
          }
          else
          {
            string errorMessage = $"No matching resource found for Resource ID: {resourceId}";
            ResultObject.Errors.Add(errorMessage);
            FunctionLogger.MethodError(_logger, errorMessage);
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return azureResources;
    }

    public async Task<IAzureResource?> GetAzureResource(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureResource));
      // Early return if resourceId is null or empty
      if (string.IsNullOrEmpty(resourceId))
      {
        FunctionLogger.MethodWarning(_logger, "The provided ResourceId is null or empty.");
        return null;
      }
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Fetching resource with ResourceId: {resourceId}");
        var azureResources = await GetAzureResources();

        // Find and return the resource with matching ID.
        var matchedResource = azureResources.FirstOrDefault(azureResource =>
            azureResource.Id != null && azureResource.Id.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase));

        if (matchedResource != null)
        {
          FunctionLogger.MethodInformation(_logger, $"Found resource: {matchedResource.Name} for ResourceId: {resourceId}");
        }
        else
        {
          string errorMessage = $"No matching resource found for Resource ID: {resourceId}";
          ResultObject.Errors.Add(errorMessage);
          FunctionLogger.MethodError(_logger, errorMessage);
        }
        return matchedResource;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return null;
    }

    public async Task<HashSet<IAzureResource>> GetAzureResources()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureResources));
      try
      {
        // Initialize resources if not already done
        if (_azureResources == null)
        {
          FunctionLogger.MethodInformation(_logger, "Azure resources are being initialized...");
          _azureResources = await InitializeAzureResources(_resourceDependencyInformation);

          if (_azureResources != null && _azureResources.Any())
          {
            FunctionLogger.MethodInformation(_logger, $"Initialized {_azureResources.Count} Azure resources.");
          }
          else
          {
            string errorMessage = "No Azure resources found during initialization.";
            ResultObject.Errors.Add(errorMessage);
            FunctionLogger.MethodError(_logger, errorMessage);
          }
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, $"Returning cached Azure resources. Count: {_azureResources.Count}");
          foreach (var azureResource in _azureResources)
          {
            FunctionLogger.MethodInformation(_logger, $"Cached Azure resources: {azureResource.Name}");
          }
        }
        // Return the cached or newly initialized resources
        return _azureResources ?? new HashSet<IAzureResource>();
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex); 
        return new HashSet<IAzureResource>(); // Return empty set if an exception occurs
      }
    }
    
    private async Task<HashSet<IAzureResource>> InitializeAzureResources(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(InitializeAzureResources));
      var azureResources = new HashSet<IAzureResource>();
      try
      {
        // Validate the main resource ID
        if (string.IsNullOrEmpty(resourceDependencyInformation.ResourceId))
        {
          var ex = new Exception($"The main resourceId was found to be null or empty for Resource: {resourceDependencyInformation.ResourceName}");
          FunctionLogger.MethodException(_logger, ex);
          throw ex;
        }
        List<string> azureResourceIds = new List<string> { resourceDependencyInformation.ResourceId };

        // Fetch outbound resource IDs
        _resourceIdsWhereMainResourceOutbound = await _dependencyInformationPersistenceService.GetResourceIdsWhereOutbound(resourceDependencyInformation.ResourceId);
        azureResourceIds.AddRange(_resourceIdsWhereMainResourceOutbound);

        // Add inbound security restrictions resource IDs, if any
        if (resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds != null && resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds.Length > 0)
        {
          FunctionLogger.MethodInformation(_logger, $"Adding AllowInbound SecurityRestrictions ResourceIds");
          azureResourceIds.AddRange(resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds ?? Array.Empty<string>());
        }
        // Add outbound resource IDs, if any
        if (resourceDependencyInformation.AllowOutbound?.ResourceIds != null && resourceDependencyInformation.AllowOutbound?.ResourceIds.Length > 0)
        {
          FunctionLogger.MethodInformation(_logger, $"Adding AllowOutbound ResourceIds");
          azureResourceIds.AddRange(resourceDependencyInformation.AllowOutbound?.ResourceIds ?? Array.Empty<string>());
        }

        string resourceIDs = string.Join(",", azureResourceIds);
        FunctionLogger.MethodInformation(_logger, $"Instantiating Resources with Resource IDs: {resourceIDs}");

        // Retrieve subscription IDs and fetch Azure resource instances
        var subscriptionIds = StringHelper.GetSubscriptionIds(azureResourceIds.ToArray());
        var azureResourceInstances = await _resourceGraphExplorerService.GetResourceInstances(subscriptionIds, azureResourceIds);

        if (azureResourceInstances != null)
        {
          // Add initialized resources to the set
          foreach (var azureResource in azureResourceInstances)
          {
            FunctionLogger.MethodInformation(_logger, $"Initialized Azure resources: {azureResource.Name}");
            azureResources.Add(azureResource);
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return azureResources;
    }

    private async Task<string> GetAzureResourceType(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureResourceType));
      try
      {
        var azureResources = await GetAzureResources();
        var azureResource = azureResources.FirstOrDefault(a => a.Id!.ToLower().Equals(resourceId.ToLower()));

        // Log and return empty if resource is not found
        if (azureResource == null)
        {
          FunctionLogger.MethodWarning(_logger, $"Resource ID {resourceId} not found in Azure resources.");
          return string.Empty;
        }        
        // Handle the case where the resource has no type (null)
        if (string.IsNullOrEmpty(azureResource.Type))
        {
          FunctionLogger.MethodWarning(_logger, $"Resource ID {resourceId} found, but no resource type is available.");
          return string.Empty;
        }        
        // Log the found resource type
        FunctionLogger.MethodInformation(_logger, $"Found Azure ResourceType: {azureResource.Type} for ResourceId: {resourceId}");
        return azureResource.Type;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return string.Empty;
      }
    }

    public async Task<NetworkRestrictionSettings> GetUpdateNetworkRestrictionSettingsForMainResource()
    { 
      FunctionLogger.MethodStart(_logger, nameof(GetUpdateNetworkRestrictionSettingsForMainResource));

      var nrs = new NetworkRestrictionSettings();
      try
      {
        // Cache resource name for multiple log uses
        var resourceName = _resourceDependencyInformation.ResourceName;
        // Generate Rules from Inbound Definitions
        FunctionLogger.MethodInformation(_logger, $"Generating IP Sec rules for Inbound Configurations for Resource: {resourceName}");

        var ipSecRules = await GenerateIpSecRules();
        // Ensure the Resource ID is valid before continuing
        if (!string.IsNullOrEmpty(_resourceDependencyInformation.ResourceId))
        {
          // Initialize outbound resource IDs if not already done
          if (_resourceIdsWhereMainResourceOutbound == null)
          {
            _resourceIdsWhereMainResourceOutbound = await _dependencyInformationPersistenceService.GetResourceIdsWhereOutbound(_resourceDependencyInformation.ResourceId);
          }
          // If there are outbound resources, generate additional rules
          if (_resourceIdsWhereMainResourceOutbound.Any())
          {
            FunctionLogger.MethodInformation(_logger, $"Generating rules for Resource ID in allowOutbound.resourceIds for Resource: {resourceName}");

            var ipSecFromOtherResourceOutbound = await GetResourceRules(_resourceIdsWhereMainResourceOutbound);
            FunctionLogger.MethodInformation(_logger, $"{ipSecFromOtherResourceOutbound.Count} IP Rules generated from allowOutbound.resourceIds for Resource: {resourceName}");

            ipSecRules.UnionWith(ipSecFromOtherResourceOutbound);
          }
        }
        else
        {
          FunctionLogger.MethodWarning(_logger, "ResourceId is null or empty, skipping outbound rules generation.");
        }
        // Add SCM IP rules to the network restriction settings
        var scmIpSecRules = await GenerateScmIpSecRules();
        //scm ip rules
        nrs = new NetworkRestrictionSettings
        {
          ResourceId = _resourceDependencyInformation.ResourceId,
          IpSecRules = ipSecRules,
          ScmIpSecRules = scmIpSecRules
        };

        FunctionLogger.MethodInformation(_logger, $"{nrs.IpSecRules.Count} IP Rules (total) and {nrs.ScmIpSecRules.Count} SCM IP Rules (total) generated for {resourceName}");
        return nrs;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return nrs;
      }
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GenerateIpSecRules()
    {
      FunctionLogger.MethodStart(_logger, nameof(GenerateIpSecRules));

      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        //rule and frontdoor resource Ids
        Dictionary<IpSecurityRestrictionRule, List<string>> frontDoorServiceTagWithHttpFilters = new Dictionary<IpSecurityRestrictionRule, List<string>>();
        // Check for AllowInbound SecurityRestrictions and ResourceIds
        if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds != null && _resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds.Length > 0)
        {
          //extract Front Door IDs if there are any
          frontDoorServiceTagWithHttpFilters = await GenerateFrontDoorServiceTag(_resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds);
          //add generated frontdoor rule
          if (frontDoorServiceTagWithHttpFilters.Any())
            ipSecurityRestrictionRules.Add(frontDoorServiceTagWithHttpFilters.FirstOrDefault().Key);

          // Exclude FrontDoor IDs and generate security rules for other ResourceIds
          /*
          var securityRestrictionRulesForResourceIds = await GetResourceRules(
              _resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds
              .Where(resourceId => !frontDoorServiceTagWithHttpFilters.Values.SelectMany(x => x).Contains(resourceId))
              .ToArray()
          );
          */

          // Get the list of all ResourceIds
          var allResourceIds = _resourceDependencyInformation.AllowInbound.SecurityRestrictions.ResourceIds;

          // Get the list of ResourceIds that should be excluded
          var excludedResourceIds = frontDoorServiceTagWithHttpFilters.Values.SelectMany(x => x);

          // Filter ResourceIds to include only those not in the excluded list
          var filteredResourceIds = allResourceIds
              .Where(resourceId => !excludedResourceIds.Contains(resourceId))
              .ToArray();

          // Fetch the security restriction rules for the filtered ResourceIds
          var securityRestrictionRulesForResourceIds = await GetResourceRules(filteredResourceIds);


          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForResourceIds);
          FunctionLogger.MethodInformation(_logger, $"{securityRestrictionRulesForResourceIds.Count} IP Rules generated by AllowInbound for Resource: {_resourceDependencyInformation.ResourceName}");

        }
        // Check if the ResourceId is not null before proceeding
        if (_resourceDependencyInformation.ResourceId != null)
        {
          // Check for Azure Service Tags in AllowInbound SecurityRestrictions
          if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.AzureServiceTags != null && _resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.AzureServiceTags.Length > 0)
          {
            string[] azureServiceTags = _resourceDependencyInformation.AllowInbound.SecurityRestrictions.AzureServiceTags;
            // Exclude FrontDoor tag if it's already added
            if (frontDoorServiceTagWithHttpFilters.Any())
            {
              //do not add frontdoor service tag if frondoorId is provided
              azureServiceTags = azureServiceTags.Where(tag => tag.ToLower() != "AzureFrontDoor.Backend".ToLower()).ToArray();
            }

            var securityRestrictionRulesForAzureServiceTags = await GetAzureServiceTagRules(
              _resourceDependencyInformation.ResourceId, azureServiceTags);
            ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForAzureServiceTags); 
          }

          // Retrieve resource type for fallback if not found
          var typeFromRest = await GetAzureResourceType(_resourceDependencyInformation.ResourceId);
          var resourceType = string.IsNullOrEmpty(typeFromRest) ? _resourceDependencyInformation.ResourceType! : typeFromRest;
          
          // Get default tags for NewDay and ThirdParty
          var newDayAndThirdPartyTags = GetDefaultNewDayAndThirdPartyTagsForResourceType(resourceType);
          FunctionLogger.MethodInformation(_logger, $"Got {newDayAndThirdPartyTags.Count()} Default Newday and Third party tags for Resource: {_resourceDependencyInformation.ResourceName}");

          string subscriptionId = StringHelper.GetSubscriptionId(_resourceDependencyInformation.ResourceId);
          IEnumerable<IpSecurityRestrictionRule> securityRestrictionRulesForNewDayInternalAndThirdPartyTags;
          // Add additional tags if configured in AllowInbound
          if (_resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags != null)
          {
            newDayAndThirdPartyTags.AddRange(_resourceDependencyInformation.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags);
          }

          FunctionLogger.MethodInformation(_logger, $"Got Total {newDayAndThirdPartyTags.Count()} Newday and Third party tags for Resource: {_resourceDependencyInformation.ResourceName} after adding newDayInternalAndThirdPartyTags from config.");

          bool includeMandatoryRules = resourceType.Equals(AzureResourceType.WebSite);
          // Generate rules based on NewDay and Third Party tags
          securityRestrictionRulesForNewDayInternalAndThirdPartyTags =
              await GetNewDayInternalAndThirdPartyTagsRules(subscriptionId, newDayAndThirdPartyTags, includeMandatoryRulesForSubscription: includeMandatoryRules);

          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForNewDayInternalAndThirdPartyTags);

          FunctionLogger.MethodInformation(_logger, $"{securityRestrictionRulesForNewDayInternalAndThirdPartyTags.Count()} Ip Rules generated by NewDayInternalAndThirdPartyTags for Resource: {_resourceDependencyInformation.ResourceName}");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return ipSecurityRestrictionRules;
    }

    public async Task<Dictionary<IpSecurityRestrictionRule, List<string>>> GenerateFrontDoorServiceTag(string[] resourceIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(GenerateFrontDoorServiceTag));
      if (resourceIds == null || resourceIds.Length == 0)
      {
        FunctionLogger.MethodWarning(_logger, "No resource IDs provided.");
        return new Dictionary<IpSecurityRestrictionRule, List<string>>();  // Return early if no resource IDs are provided
      }
      var result = new Dictionary<IpSecurityRestrictionRule, List<string>>();
      try
      {
        // Check if there are any FrontDoor resource IDs defined
        var frontdoorResourceIds = resourceIds.Where(x => IsFrontDoorResourceId(x)).ToList();
        if (!frontdoorResourceIds.Any())
        {
          FunctionLogger.MethodWarning(_logger, "No FrontDoor resource IDs found in provided ResourceIds.");
          return result;
        }
        var FDIDs = await _resourceGraphExplorerService.GetFrontDoorUniqueInstanceIds(frontdoorResourceIds);
        // hard limit is 8
        if (FDIDs.Count > 8)
        {
          string errorMessage = $"Cannot generate more than 8 unique instance IDs for Front Door! Generated FDID Count: {FDIDs.Count}";
          FunctionLogger.MethodWarning(_logger, errorMessage);
          throw new InvalidOperationException(errorMessage);
        }

        // Generate the IP security restriction rule with service tag
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
        // Log the generated FDIDs and resource IDs
        string FIDs = string.Join(", ", FDIDs.Values);
        string FRIDs = string.Join(", ", frontdoorResourceIds);
        FunctionLogger.MethodInformation(_logger, $"Generated IP Security Restriction Rule for FrontDoor with FDIDs: {FIDs} and ResourceIds: {FRIDs}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return result;
    }

    private bool IsFrontDoorResourceId(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(IsFrontDoorResourceId));
      // Check for null, empty resourceId
      bool isFrontDoor = !string.IsNullOrEmpty(resourceId) &&
                   resourceId.Contains("/providers/Microsoft.Network/frontDoors/", StringComparison.OrdinalIgnoreCase);

      // Log the input resourceId and result
      FunctionLogger.MethodInformation(_logger, $"Checked resource ID for FrontDoor with ResourceId: {resourceId}, IsFrontDoor: {isFrontDoor}");

      return isFrontDoor;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetAzureServiceTagRules(string resourceId, string[] serviceTags)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureServiceTagRules));
      // Return early if serviceTags is null or empty
      if (serviceTags == null || serviceTags.Length == 0)
      {
        FunctionLogger.MethodWarning(_logger, "Service tags are null or empty. No rules will be generated.");
        return new HashSet<IpSecurityRestrictionRule>();
      }
      string tags = string.Join(", ", serviceTags);
      HashSet<IpSecurityRestrictionRule> rules = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        var resourceType = await GetAzureResourceType(resourceId);
        IServiceTagManager provider;
        // Determine the appropriate service tag manager based on resource type
        if (resourceType == AzureResourceType.WebSiteSlot || resourceType == AzureResourceType.WebSite)
        {
          provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.AzureWeb);
        }
        else
        {
          //return IP Range of Service Tags
          provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.Azure);
        }
        FunctionLogger.MethodInformation(_logger, $"Generating rules with tags: {tags}");
        // Generate rules using the service tag manager
        rules = await provider.GenerateRulesByName("", serviceTags);

        FunctionLogger.MethodInformation(_logger, $"Generated {rules.Count} rules for ResourceID: {resourceId} with Azure Service Tags: {tags}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);        
        //return an empty set on failure to avoid further processing
        return new HashSet<IpSecurityRestrictionRule>();
      }
      return rules;
    }

    internal List<string> GetDefaultNewDayAndThirdPartyTagsForResourceType(string azureResourceType)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetDefaultNewDayAndThirdPartyTagsForResourceType));
      List<string> newDayTags = new List<string>()
      {
        "OctopusWorkers",
        "Checkpoint.UKS.ITInfraPro", // will be discarded in stg
        "Bastion.STG", // will be discarded in prd
        "Bastion.PRD", // will be discarded in stg
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
            "Checkpoint.UKS.ITInfraPro.UAT", // will be discarded in prd
            "Checkpoint.WE.ndPRO.ITInfra.UAT", // will be discarded in prd
            "Checkpoint.WE.ndPRO.ITInfra", // will be discarded in stg
            "Octopus.Subnet",
            "Bastion.PRD.Subnet",
            "W365Desktops.Subnets"
          });
            break;
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return newDayTags;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GenerateScmIpSecRules()
    {
      FunctionLogger.MethodStart(_logger, nameof(GenerateScmIpSecRules));
      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();
      try
      {

        // Check and log security restrictions for resource IDs
        var scmResourceIds = _resourceDependencyInformation.AllowInbound?.ScmSecurityRestrictions?.ResourceIds;
        if (scmResourceIds != null && scmResourceIds.Length > 0)
        {
          string resourceIDS = string.Join(", ", scmResourceIds);
          
          FunctionLogger.MethodInformation(_logger, $"Generating rules for SCM ResourceIds: {resourceIDS}");

          var securityRestrictionRulesForResourceIds = await GetResourceRules(scmResourceIds);
          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForResourceIds);

          FunctionLogger.MethodInformation(_logger, $"{securityRestrictionRulesForResourceIds.Count} SCM IP Rules generated from ResourceIds.");
        }
        // Check and log for AzureServiceTags
        if (_resourceDependencyInformation.ResourceId != null)
        {
          var scmServiceTags = _resourceDependencyInformation.AllowInbound?.ScmSecurityRestrictions?.AzureServiceTags;
          if (scmServiceTags != null && scmServiceTags.Length > 0)
          {
            string serviceTags = string.Join(", ", scmServiceTags);

            FunctionLogger.MethodInformation(_logger, $"Generating rules for SCM AzureServiceTags: {serviceTags}");

            var securityRestrictionRulesForAzureServiceTags =
              await GetAzureServiceTagRules(_resourceDependencyInformation.ResourceId, scmServiceTags);
            ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForAzureServiceTags);

            FunctionLogger.MethodInformation(_logger, $"{securityRestrictionRulesForAzureServiceTags.Count} SCM IP Rules generated from AzureServiceTags.");
          }

          // Default tags for internal and third-party resources
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
            tagsList.AddRange(_resourceDependencyInformation.AllowInbound.ScmSecurityRestrictions.NewDayInternalAndThirdPartyTags);
          }
          string tags = string.Join(", ", tagsList);
          FunctionLogger.MethodInformation(_logger, $"Generating rules for SCM NewDayInternalAndThirdPartyTags: {tags}");

          string subscriptionId = StringHelper.GetSubscriptionId(_resourceDependencyInformation.ResourceId);
          var securityRestrictionRulesForNewDayInternalAndThirdPartyTags =
              await GetNewDayInternalAndThirdPartyTagsRules(subscriptionId, tagsList, includeMandatoryRulesForSubscription: false);

          ipSecurityRestrictionRules.UnionWith(securityRestrictionRulesForNewDayInternalAndThirdPartyTags);

          if (securityRestrictionRulesForNewDayInternalAndThirdPartyTags.Any())
          {
            FunctionLogger.MethodInformation(_logger, $"{securityRestrictionRulesForNewDayInternalAndThirdPartyTags.Count()} SCM IP Rules generated from NewDayInternalAndThirdPartyTags.");
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return ipSecurityRestrictionRules;
    }

    private async Task<HashSet<IpSecurityRestrictionRule>> GetNewDayInternalAndThirdPartyTagsRules(string subscriptionId, List<string> newDayInternalAndThirdPartyTags, bool includeMandatoryRulesForSubscription = true)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetNewDayInternalAndThirdPartyTagsRules));
      HashSet<IpSecurityRestrictionRule> rules = new HashSet<IpSecurityRestrictionRule>();
      if (newDayInternalAndThirdPartyTags == null || !newDayInternalAndThirdPartyTags.Any())
      {
        FunctionLogger.MethodWarning(_logger, "No NewDay internal or third-party tags provided.");
        return rules; // Return empty set if no tags are provided
      }
      string tags = string.Join(", ", newDayInternalAndThirdPartyTags);
      try
      {
        var provider = _serviceTagManagerProvider.GetServiceTagManager(ManagerType.NewDay);

        // Log information before generating rules
        FunctionLogger.MethodInformation(_logger,
            $"Generating security restriction rules for Subscription ID: {subscriptionId} with Service Tags: {tags}. Include Mandatory Rules: {includeMandatoryRulesForSubscription}");

        // Generate the rules using the service tag manager
        rules = await provider.GenerateRulesByName(subscriptionId, newDayInternalAndThirdPartyTags.ToArray(), includeMandatoryRulesForSubscription);

        return rules;
      }
      catch (Exception ex)
      {
        // Log and rethrow exception to ensure calling code is aware of the failure
        FunctionLogger.MethodException(_logger, ex);
        throw new InvalidOperationException("Error occurred while generating rules for NewDay internal and third-party tags.", ex);
      }
    }

    private void LogVnetSubnetIntegrationWarnings(HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules)
    {
      var resourceNamesWithIps = ipSecurityRestrictionRules
          .Where(x => !string.IsNullOrEmpty(x.Name) && !string.IsNullOrEmpty(x.IpAddress) && string.IsNullOrEmpty(x.VnetSubnetResourceId))
          .Select(s => s.Name).Distinct();

      var warningList = ipSecurityRestrictionRules
          .Where(x => !string.IsNullOrEmpty(x.VnetSubnetResourceId) && resourceNamesWithIps.Contains(x.Name))
          .Select(x => new { ResourceName = x.Name, SubnetId = x.VnetSubnetResourceId })
          .Distinct();

      foreach (var resource in warningList)
      {
        var warningMessage = LogMessageHelper.GetVnetAddedDueToNamingMatchMessage(resource.ResourceName!, resource.SubnetId!);
        ResultObject.Warnings.Add(warningMessage);
      }
    }

    private List<string> FilterValidSubnetIds(IEnumerable<string> resourceIds, ICollection<string> allSubnets)
    {
      FunctionLogger.MethodStart(_logger, nameof(FilterValidSubnetIds));
      List<string> subnetIdsFromConfig = new List<string>();
      if (!allSubnets.Any())
      {
        FunctionLogger.MethodWarning(_logger, "No subnets provided to check against.");
        return subnetIdsFromConfig;
      }
      try
      {
        foreach (var resourceId in resourceIds)
        {
          if (!Regex.IsMatch(resourceId, Constants.VNetSubnetIdRegex))
          {
            FunctionLogger.MethodWarning(_logger, $"Skipping resource ID {resourceId} as it does not match the VNetSubnetId regex.");
            continue;
          }
          if (allSubnets.Contains(resourceId))
          {
            subnetIdsFromConfig.Add(resourceId);
            FunctionLogger.MethodInformation(_logger, $"Valid subnet ID found: {resourceId}");
          }
          else
          {
            var warningMessage = $"Unable to find VnetSubnetId {resourceId}.";
            ResultObject.Warnings.Add(warningMessage);
            FunctionLogger.MethodWarning(_logger, warningMessage);
          }
        }
        // Log the count of valid subnet IDs found
        FunctionLogger.MethodInformation(_logger, $"{subnetIdsFromConfig.Count} valid subnet IDs filtered from {allSubnets.Count} subnets");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return subnetIdsFromConfig;
    }

    private async Task<List<string>> GetAllSubnetIds(string azureSubscriptionId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAllSubnetIds));
      try
      {        
        // Check if the subnet list is already cached
        if (_subnets != null)
        {
          return _subnets;
        }
        // Fetch subnet IDs from the resource graph if not cached
        _subnets = await _resourceGraphExplorerService.GetAllSubnetIds(azureSubscriptionId);
        FunctionLogger.MethodInformation(_logger, $"Fetched {_subnets.Count} subnet IDs from resource graph.");
        return _subnets;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return new List<string>();
      }
    }

    private async Task<List<string>> GetAllSubnetIds(string[] azureSubscriptionIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAllSubnetIds));
      string subscriptionIDs =string.Join(", ", azureSubscriptionIds);
      try
      {
        if (_subnets == null)
        {
          _subnets = new List<string>();
          foreach (var azureSubscriptionId in azureSubscriptionIds)
          {
            FunctionLogger.MethodInformation(_logger, $"Getting subnet for SubscriptionID: {azureSubscriptionId}");
            try
            {
              var subnets = await _resourceGraphExplorerService.GetAllSubnetIds(azureSubscriptionId);
              _subnets.AddRange(subnets);
            }
            catch (Exception ex)
            {
              // Log error for each subscription and return an empty list in case of failure
              FunctionLogger.MethodException(_logger, ex);
            }
          }
        }
        FunctionLogger.MethodInformation(_logger, $"Fetched {_subnets.Count} subnet IDs from resource graph.");
        return _subnets;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
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
