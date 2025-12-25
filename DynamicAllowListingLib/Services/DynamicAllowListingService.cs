using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.AzureResources;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Xsl;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicAllowListingLib.Services
{
  public interface IDynamicAllowListingService
  {
    Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsForAppServicePlanScale(string appServicePlanResourceId);
    Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsWhenWebAppDeleted(string deletedWebAppResourceId);
    Task<HashSet<ResourceDependencyInformation>> GetOutboundOverwriteConfigs(ResourceDependencyInformation resourceDependencyInformation);
    /// <summary>
    /// Update unmanaged resources in the outbound of the provided config.
    /// Unmanaged means, the resource is not managed by DAL. This is verified by checking if it is present in db or not.
    /// If present in db then managed, else unmanaged.
    /// </summary>
    /// <param name="resourceDependencyInformation"></param>
    /// <returns></returns>
    Task<ResultObject> UpdateUnmanagedResources(ResourceDependencyInformation resourceDependencyInformation);
    Task<ResultObject> OverwriteNetworkRestrictionRulesForMainResource(ResourceDependencyInformation resourceDependencyInformation);
    Task<ResultObject> CheckProvisioningSucceeded(ResourceDependencyInformation resourceDependencyInformation);
    Task<ResultObject> UpdateDb(ResourceDependencyInformation resourceDependencyInformation);
  }

  public class DynamicAllowListingService : IDynamicAllowListingService
  {
    private readonly IResourceDependencyInformationPersistenceService _persistenceManager;
    private readonly ILogger<DynamicAllowListingService> _logger;
    private readonly IRestHelper _restHelper;
    private readonly IResourceGraphExplorerService _resourceGraphExplorerService;
    private readonly IAzureResourceServiceFactory _azureResourceServiceFactory;
    private readonly ISettingValidator<ResourceDependencyInformation> _resourceDependencyInfoValidator;
    private readonly IAzureResourceClassProvider _classProvider;
    private readonly List<string> allowedCrossSubscriptionSubnetList = new List<string>
    {
      "/subscriptions/f317937e-3f0c-4d7c-b0b2-2865d5b53c99/resourceGroups/rsgazuitcneuvnet01/providers/Microsoft.Network/virtualNetworks/vntazuitcneu01/subnets/mgmt01" //octopus subnet (NDC Tenant)
    };

    public DynamicAllowListingService(IRestHelper restHelper,
      IResourceDependencyInformationPersistenceService persistenceManager,
      IResourceGraphExplorerService resourceGraphExplorerService,
      IAzureResourceServiceFactory azureResourceServiceFactory,
      ISettingValidator<ResourceDependencyInformation> resourceDependencyInfoValidator,
      ILogger<DynamicAllowListingService> logger,
      IAzureResourceClassProvider classProvider)
    {
      _restHelper = restHelper;
      _persistenceManager = persistenceManager;
      _resourceGraphExplorerService = resourceGraphExplorerService;
      _azureResourceServiceFactory = azureResourceServiceFactory;
      _logger = logger;
      _resourceDependencyInfoValidator = resourceDependencyInfoValidator;
      _classProvider = classProvider;
    }

    public async Task<ResultObject> UpdateDb(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(UpdateDb));
      ResultObject resultObject = new ResultObject();
      //try
      //{
      resultObject = await _persistenceManager.CreateOrReplaceItemInDb(resourceDependencyInformation);
      //}
      //catch (Exception ex)
      //{
      //  FunctionLogger.MethodException(_logger, ex);
      //}
      return resultObject;
    }

    public async Task<ResultObject> UpdateUnmanagedResources(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(UpdateUnmanagedResources));
      var resultObject = new ResultObject();
      try
      {
        // Create Azure Resource Service instance
        using var azureResourceService = _azureResourceServiceFactory.CreateInstance(resourceDependencyInformation);

        // Validate the input data
        // Check if the main resourceId is null
        if (string.IsNullOrEmpty(resourceDependencyInformation.ResourceId))
        {
          var warningMessage = $"Main resourceId is null or empty for config";
          FunctionLogger.MethodWarning(_logger, warningMessage);
          resultObject.Warnings.Add(warningMessage);
          throw new InvalidOperationException(warningMessage);
        }
        if (resourceDependencyInformation.AllowOutbound == null ||
            resourceDependencyInformation.AllowOutbound.ResourceIds == null ||
            (resourceDependencyInformation.AllowOutbound.ResourceIds.Length <= 0))
        {
          var warningMessage = $"AllowOutbound is null or contains no resource IDs. No unmanaged resources to update.";
          FunctionLogger.MethodWarning(_logger, warningMessage);
          resultObject.Warnings.Add(warningMessage);
        }

        // we just append outbound rules for those we know are not managed by DAL
        var networkRestrictionsToAppend = await azureResourceService.GetAppendNetworkRestrictionSettings();
        foreach (var networkRestrictions in networkRestrictionsToAppend)
        {
          FunctionLogger.MethodInformation(_logger, $"Updating Network Restrictions : {networkRestrictions}");
          // Skip if Resource ID is null
          if (networkRestrictions.ResourceId == null)
          {
            FunctionLogger.MethodInformation(_logger, $"Empty Resource ID, Skipping to next");
            continue;
          }
          if (await IsDalManagedResource(networkRestrictions.ResourceId))
          {
            FunctionLogger.MethodInformation(_logger,$"Resource {networkRestrictions.ResourceId} is managed by DAL. Skipping to next.");
            continue;
          }
          // Fetch the Azure resource
          var azureResource = await azureResourceService.GetAzureResource(networkRestrictions.ResourceId);
          if (azureResource == null)
          {
            string warningMessage = $"Resource with ResourceId {networkRestrictions.ResourceId} not found. It either doesn't exist or the process doesn't have the necessary access.";
            FunctionLogger.MethodWarning(_logger, warningMessage);
            resultObject.Warnings.Add(warningMessage);
          }
          else
          {
            // Append network restriction rules
            var appendResult = await azureResource.AppendNetworkRestrictionRules(networkRestrictions, _logger, _restHelper);
            resultObject.Merge(appendResult);

            // Log warning for unmanaged resource
            string unmanagedWarning = $"Resource {networkRestrictions.ResourceId} is not managed by DAL or is in another subscription! This can lead to issues in the future. Check docs: https://newdaycards.atlassian.net/wiki/spaces/DD/pages/1217790190/UpdateNetworkRestrictions+Octopus+step+template#Resources-managed-by-DAL";
            FunctionLogger.MethodWarning(_logger, unmanagedWarning);
            resultObject.Warnings.Add(unmanagedWarning);
          }
        }
        resultObject.Merge(azureResourceService.ResultObject);
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        resultObject.Errors.Add($"An unexpected error occurred: {ex.Message}");
      }
      return resultObject;
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsWhenWebAppDeleted(string deletedWebAppResourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetOverwriteConfigsWhenWebAppDeleted));
      var configsToOverwrite = new HashSet<ResourceDependencyInformation>();
      try
      {        
        // Attempt to remove the config and dependencies for the deleted web app
        FunctionLogger.MethodInformation(_logger, $"Removing config and dependencies for deleted web app. ResourceId: {deletedWebAppResourceId}");

        var updatedItems = await _persistenceManager.RemoveConfigAndDependencies(deletedWebAppResourceId);

        // Process each updated item
        foreach (var resourceDependencyInformation in updatedItems)
        {
          var validationResult = _resourceDependencyInfoValidator.Validate(resourceDependencyInformation);
          if (!validationResult.Success)
          {
            FunctionLogger.MethodWarning(_logger, $"Invalid resource dependency information found.Validation result: {validationResult}");
            continue;
          }
          FunctionLogger.MethodInformation(_logger, $"Adding overrite config for resource: {resourceDependencyInformation.ResourceName}");

          configsToOverwrite.Add(resourceDependencyInformation);
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return configsToOverwrite;
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsForAppServicePlanScale(string appServicePlanResourceId)
    {
      // Log the start of the method
      FunctionLogger.MethodStart(_logger, nameof(GetOverwriteConfigsForAppServicePlanScale));
      var configsToOverwrite = new HashSet<ResourceDependencyInformation>();
      try
      {
        // Log the app service plan ID being processed
        var resourcesHostedInAppServicePlan = await _resourceGraphExplorerService.GetResourcesHostedOnPlan(appServicePlanResourceId);
        foreach (var resourceId in resourcesHostedInAppServicePlan)
        {
          FunctionLogger.MethodInformation(_logger, $"Resource {resourceId} found on app service plan {appServicePlanResourceId}");

          // Retrieve dependency information for the resource
          var resourceDependencyInformation = await _persistenceManager.GetResourceDependencyInformation(resourceId);
          if (resourceDependencyInformation == null)
          {
            FunctionLogger.MethodWarning(_logger, $"No config found in database for resource {resourceId} found on app service plan {appServicePlanResourceId}");

            continue;
          }
          // Validate the resource dependency information
          var validationResult = _resourceDependencyInfoValidator.Validate(resourceDependencyInformation);
          if (!validationResult.Success)
          {
            FunctionLogger.MethodWarning(_logger, $"Invalid resource dependency information found for {resourceId}. Validation result: {validationResult}");
            continue;
          }
          // Retrieve inbound configurations for the resource
          var configsWhereInbound = await GetConfigsWhereInbound(resourceDependencyInformation.ResourceId!);
          configsToOverwrite.UnionWith(configsWhereInbound);

          if (resourceDependencyInformation.AllowOutbound?.ResourceIds == null)
          {
            FunctionLogger.MethodWarning(_logger, $"Outbound resource IDs are null or empty for resource: {resourceId}");
            continue;
          } 
          // Retrieve outbound configurations
          var outboundConfigs = await GetConfigsForResources(resourceDependencyInformation.AllowOutbound.ResourceIds);
          configsToOverwrite.UnionWith(outboundConfigs);
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return configsToOverwrite;
    }

    public Task<HashSet<ResourceDependencyInformation>> GetOutboundOverwriteConfigs(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetOutboundOverwriteConfigs));
      var configsToBeOverwritten = new HashSet<ResourceDependencyInformation>();
      try
      {
        // Log the resource being processed
        FunctionLogger.MethodInformation(_logger, $"Fetching outbound resources for ResourceId: {resourceDependencyInformation.ResourceId}, ResourceName: {resourceDependencyInformation.ResourceName}.");
        var outboundResources = GetOutboundResources(resourceDependencyInformation);
        if (!outboundResources.Any())
        {
          FunctionLogger.MethodInformation(_logger, "No outbound resources found ");
        }

        // Get configurations for the outbound resources
        var configsToBeOverWritten = GetConfigsForResources(outboundResources);
        // Log the number of configurations retrieved
        FunctionLogger.MethodInformation(_logger, $"Retrieved {configsToBeOverwritten.Count} configurations for outbound resources of ResourceId: {resourceDependencyInformation.ResourceId}.");
        return configsToBeOverWritten;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Return an empty HashSet in case of error
      return Task.FromResult(new HashSet<ResourceDependencyInformation>());
    }
    
    private IEnumerable<string> GetOutboundResources(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetOutboundResources));

      var resourceDependencyInfoHashSet = new HashSet<string>();
      try
      {
        // Check if outbound resources exist
        var outboundResources = resourceDependencyInformation.AllowOutbound?.ResourceIds;
        if (outboundResources != null)
        {
          string outboundResourceString = String.Join(",", outboundResources);
          // Log the list of outbound resource IDs
          FunctionLogger.MethodInformation(_logger, 
            $"Found {outboundResources.Length} outbound resources for ResourceId: {resourceDependencyInformation.ResourceId}. Outbound Resource IDs: {outboundResourceString}");

          resourceDependencyInfoHashSet.UnionWith(outboundResources);
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return resourceDependencyInfoHashSet;
    }

    private async Task<HashSet<ResourceDependencyInformation>> GetConfigsForResources(IEnumerable<string> resourceIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetConfigsForResources));
      var resourceDependencyInfoHashSet = new HashSet<ResourceDependencyInformation>(); 
      try
      {
         foreach (var resourceId in resourceIds)
         {
            FunctionLogger.MethodInformation(_logger, $"Fetching configuration for ResourceID: {resourceId}");
            var resourceDependencyInformation = await GetConfigForResource(resourceId);
            if (resourceDependencyInformation == null)
            {
              FunctionLogger.MethodWarning(_logger, $"No configuration found for ResourceID: {resourceId}");          
              continue;
            }
            //set it false to get target resource firewall settings updated
            resourceDependencyInformation.PrintOut = "false";
            resourceDependencyInfoHashSet.Add(resourceDependencyInformation);
         }
        // Log the total number of configurations retrieved
        FunctionLogger.MethodWarning(_logger, $"Successfully retrieved {resourceDependencyInfoHashSet.Count} configurations for the provided resource IDs.");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return resourceDependencyInfoHashSet;
    }

    private async Task<ResourceDependencyInformation?> GetConfigForResource(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetConfigForResource));
      try
      {
        // Attempt to retrieve resource dependency information
        var resourceDependencyInformation = await _persistenceManager.GetResourceDependencyInformation(resourceId);
        if (resourceDependencyInformation == null)
        {
          FunctionLogger.MethodWarning(_logger, $"No config found in database for resource: {resourceId}");
          return null;
        }
        // Validate the retrieved dependency information
        var validationResult = _resourceDependencyInfoValidator.Validate(resourceDependencyInformation);
        if (!validationResult.Success)
        {
          FunctionLogger.MethodWarning(_logger, $"Invalid resource dependency information found for ResourceID: {resourceId}, Validation result: {validationResult}"); 
        }
        return resourceDependencyInformation;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return null;
      }
    }

    private async Task<bool> IsDalManagedResource(string resourceId)
      => await GetConfigForResource(resourceId) != null;

    private async Task<HashSet<ResourceDependencyInformation>> GetConfigsWhereInbound(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetConfigsWhereInbound));
      var resourceDependencyInfoHashSet = new HashSet<ResourceDependencyInformation>();
      try
      {
        // Retrieve inbound configurations from the persistence manager
        var configsFromDb = await _persistenceManager.GetConfigsWhereInbound(resourceId);
        if (configsFromDb == null || !configsFromDb.Any())
        {
          FunctionLogger.MethodWarning(_logger, $"No inbound configurations found for ResourceID: {resourceId}.");
          return resourceDependencyInfoHashSet;
        }

        foreach (var configFromDb in configsFromDb)
        {
          // Validate the configuration
          var validationResult = _resourceDependencyInfoValidator.Validate(configFromDb);
          if (!validationResult.Success)
          {
            FunctionLogger.MethodWarning(_logger, $"Invalid resource dependency information found for ResourceID: {resourceId}, Validation result: {validationResult}");
            continue;
          }
          FunctionLogger.MethodInformation(_logger, $"Found Config in DB for Resource: {configFromDb.ResourceName}");
          resourceDependencyInfoHashSet.Add(configFromDb);
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return resourceDependencyInfoHashSet;
    }

    public async Task<ResultObject> OverwriteNetworkRestrictionRulesForMainResource(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(OverwriteNetworkRestrictionRulesForMainResource));
      var resultObject = new ResultObject();
      try
      {
        // Create an Azure resource service instance for the resource
        using var azureResourceService = _azureResourceServiceFactory.CreateInstance(resourceDependencyInformation);
        var mainAzureResource = await azureResourceService.GetAzureResource(resourceDependencyInformation.ResourceId!);

        bool justPrintOutRules;
        if (bool.TryParse(resourceDependencyInformation.PrintOut, out justPrintOutRules) && mainAzureResource != null)
        {
          mainAzureResource!.PrintOut = justPrintOutRules;
          FunctionLogger.MethodInformation(_logger, $"Set PrintOut to {justPrintOutRules} for ResourceId={resourceDependencyInformation.ResourceId}");
        }
        // If resource is not found and we aren't just printing out rules, return an error
        if (mainAzureResource == null && justPrintOutRules == false)
        {
          FunctionLogger.MethodInformation(_logger, $"Resource with ResourceId={resourceDependencyInformation.ResourceId} Not found");
          resultObject.Errors.Add($"Resource not found. ResourceId:{resourceDependencyInformation.ResourceId}.");
          resultObject.Merge(azureResourceService.ResultObject);
          return resultObject;
        }
        // Retrieve network restrictions to overwrite
        var networkRestrictionsToOverwrite =
          await azureResourceService.GetUpdateNetworkRestrictionSettingsForMainResource();
        FunctionLogger.MethodInformation(_logger, $"Retrieved network restrictions to overwrite for ResourceName={resourceDependencyInformation.ResourceName}");

        //remove missing subnet ids
        ResultObject validatedRules = await RemoveMissingSubnets(networkRestrictionsToOverwrite, allowedCrossSubscriptionSubnetList);
        resultObject.Merge(validatedRules);

        //resource may not exists yet, create an empty model to be able to output generated rules
        if (mainAzureResource == null && justPrintOutRules == true)
        {
          var emptyModel = _classProvider.GetResourceClass(resourceDependencyInformation.ResourceType!);
          if (emptyModel.GetType() == typeof(WebSite) || emptyModel.GetType() == typeof(PublicIpAddress))
          {
            const string errorMessage = "Website and Public IP Address types cannot be used to print out the rules!";
            resultObject.Errors.Add(errorMessage);
            return resultObject;
          }
          var rules = emptyModel.ConvertRulesToPrintOut(networkRestrictionsToOverwrite);
          resultObject.Data = new ResultObject.OutputData
          {
            IPs = rules.Item1,
            SubnetIds = rules.Item2
          };
          FunctionLogger.MethodInformation(_logger, $"Generated rules for printing out: IPs={rules.Item1}, SubnetIds={rules.Item2}");
          return resultObject;
        }

        // try to obtain staging slot and add restrictions if we can
        var websiteSlot = await GetWebSiteSlot(mainAzureResource!, _resourceGraphExplorerService);
        if (websiteSlot != null)
        {
          var slotRestrictions = GetWebsiteSlotRestrictions(websiteSlot.Id!, networkRestrictionsToOverwrite);
          var slotRestrictionResult = await websiteSlot.OverWriteNetworkRestrictionRules(slotRestrictions, _logger, _restHelper);
          resultObject.Merge(slotRestrictionResult);
          FunctionLogger.MethodInformation(_logger, $"Applied network restriction rules to website slot: {websiteSlot.Id}");
        }
        // Apply network restriction rules to the main Azure resource
        var result = await mainAzureResource!.OverWriteNetworkRestrictionRules(networkRestrictionsToOverwrite, _logger, _restHelper);
        resultObject.Merge(result);
        resultObject.Merge(azureResourceService.ResultObject);
        if(resultObject.Errors.Count > 0)
        {
          FunctionLogger.MethodInformation(_logger, $"Failed for updating network restriction rules for ResourceName={resourceDependencyInformation.ResourceName}");
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, $"Successfully overwrote network restriction rules for ResourceName={resourceDependencyInformation.ResourceName}");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return resultObject;
    }

    public async Task<ResultObject> RemoveMissingSubnets(NetworkRestrictionSettings rules, List<string> skippedSubnetIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(RemoveMissingSubnets));
      var resultObject = new ResultObject();
      try
      {
        // Validate input parameters and early exit if necessary
        if (rules == null || rules.IpSecRules == null || rules.IpSecRules.Count <= 0)
          return resultObject;

        // Extract subnet information from the IP Sec rules
        var subnetRules = rules.IpSecRules.Where(x => !string.IsNullOrEmpty(x.VnetSubnetResourceId)).ToList();
        var subnetIds = subnetRules.Select(x => x.VnetSubnetResourceId).Distinct().ToList();
        var subscriptionIds = subnetRules.Where(x => !string.IsNullOrEmpty(x.VnetSubnetSubscriptionId)).Select(x => x.VnetSubnetSubscriptionId).Distinct().ToList();
        if (!subscriptionIds.Any())
        {
          FunctionLogger.MethodWarning(_logger, $"No subscription IDs found in the rules.");
          return resultObject;
        }

        // Determine which subscriptions are cross-tenant and should be skipped
        var crossTenantSubscriptionIds = skippedSubnetIds.Select(id => id.Split('/')[2]).ToList();

        // Fetch existing subnet IDs for each subscription, excluding cross-tenant subscriptions
        var existingSubnetIds = new List<string>();
        foreach (var subscriptionId in subscriptionIds.Except(crossTenantSubscriptionIds))
        {
          var subnets = await _resourceGraphExplorerService.GetAllSubnetIds(subscriptionId!);
          existingSubnetIds.AddRange(subnets);
        }

        // Add skipped subnet IDs to the list of existing subnet IDs
        existingSubnetIds.AddRange(skippedSubnetIds);
        existingSubnetIds = existingSubnetIds.ConvertAll(d => d.ToLower());

        // Find missing subnet IDs (those that are not in the existing list)
        var missingSubnetIds = subnetIds.Where(x => !existingSubnetIds.Contains(x!.ToLower())).ToList();
        if (missingSubnetIds.Count > 0)
        {
          //manipulate rules
          rules.IpSecRules = rules.IpSecRules.Where(x => string.IsNullOrEmpty(x.VnetSubnetResourceId) || !missingSubnetIds.Contains(x.VnetSubnetResourceId)).ToHashSet();
          foreach (var missingSubnetId in missingSubnetIds)
          {
            var warningMessage = $"Subnet Id:{missingSubnetId} does not exist! Removed from restriction list!";
            resultObject.Warnings.Add(warningMessage);
            FunctionLogger.MethodWarning(_logger, warningMessage);
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return resultObject;
    }

    public async Task<ResultObject> CheckProvisioningSucceeded(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(CheckProvisioningSucceeded));
      var resultObject = new ResultObject();
      try
      {
        // Create instance of AzureResourceService
        using var azureResourceService = _azureResourceServiceFactory.CreateInstance(resourceDependencyInformation);

        // Fetch Azure resources
        var azureResources = await azureResourceService.GetAzureResources();
        FunctionLogger.MethodInformation(_logger, $"Fetched {azureResources.Count} Azure resources for checking provisioning status");

        // Loop through resources and check provisioning state
        foreach (var azureResource in azureResources)
        {
          // Skip resources that are either CosmosDb or have PrintOut set to true
          if ((!(azureResource.Type is AzureResourceType.CosmosDb)) || azureResource.PrintOut)
          {
            FunctionLogger.MethodInformation(_logger, $"Skipping resource {azureResource.Id} of type {azureResource.Type} with PrintOut={azureResource.PrintOut}");
            continue;
          }

          // Cast the resource to CosmosDb and fetch provisioning status
          var cosmosDb = azureResource as CosmosDb;
          var props = await cosmosDb!.GetExistingCosmosDbArmProperties(_restHelper, _logger);
          string provisioningState = props!.Properties.ProvisioningState;
          string msg = $"ProvisioningState={provisioningState} for {azureResource.Id}";
          if (provisioningState.ToLower() == "succeeded")
          {
            resultObject.Information.Add(msg);
          }
          else
          {
            resultObject.Errors.Add(msg);
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return resultObject;
    }
    

    internal async Task<IAzureResource?> GetWebSiteSlot(IAzureResource websiteResource, IResourceGraphExplorerService resourceGraphExplorerService)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetWebSiteSlot));
      try
      {
        if (websiteResource == null || websiteResource.Type != AzureResourceType.WebSite)
        {
          FunctionLogger.MethodInformation(_logger, $"Invalid resource provided. Expected WebSite type but received {websiteResource?.Type} with Id={websiteResource?.Id}");
          return null;
        }
        string websiteSlotResourceId = websiteResource.Id + "/slots/staging";
        // Check if the slot exists
        bool slotExists = await resourceGraphExplorerService.ResourceExists(websiteSlotResourceId);
        FunctionLogger.MethodInformation(_logger, $"Checked existence of slot with Id={websiteSlotResourceId}: Exists={slotExists}");
        // If slot doesn't exist, return null
        if (!slotExists)
        {
          FunctionLogger.MethodInformation(_logger, $"Slot does not exist for Website Id={websiteResource.Id}");
          return null;
        }
        // Create and return the slot resource
        var slot = new WebSiteSlot { Id = websiteSlotResourceId };
        FunctionLogger.MethodInformation(_logger, $"WebSiteSlot with Id={websiteSlotResourceId}");
        return slot;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return null;
      }
    }

    internal NetworkRestrictionSettings GetWebsiteSlotRestrictions(string websiteSlotResourceId,NetworkRestrictionSettings productionSlotSettings)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetWebsiteSlotRestrictions));

      var slotSettings = new NetworkRestrictionSettings();
      try
      {
        slotSettings = new NetworkRestrictionSettings()
        {
          ResourceId = websiteSlotResourceId,
          IpSecRules = productionSlotSettings.IpSecRules != null ? productionSlotSettings.IpSecRules.ToHashSet() : null,
          ScmIpSecRules = productionSlotSettings.ScmIpSecRules != null ? productionSlotSettings.ScmIpSecRules.ToHashSet() : null,
          IpSecRulesToDelete = productionSlotSettings.IpSecRulesToDelete != null ? productionSlotSettings.IpSecRulesToDelete.ToHashSet() : null,
          ScmIpSecRulesToDelete = productionSlotSettings.ScmIpSecRulesToDelete != null ? productionSlotSettings.ScmIpSecRulesToDelete.ToHashSet() : null
        };

        string log = $"Slot settings created for ResourceId={websiteSlotResourceId}. Rule counts - IpSecRules: {slotSettings.IpSecRules?.Count ?? 0}, ScmIpSecRules: {slotSettings.ScmIpSecRules?.Count ?? 0}, IpSecRulesToDelete: {slotSettings.IpSecRulesToDelete?.Count ?? 0}, ScmIpSecRulesToDelete: {slotSettings.ScmIpSecRulesToDelete?.Count ?? 0}";
        FunctionLogger.MethodInformation(_logger, log);
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return slotSettings;
    }
  }
}