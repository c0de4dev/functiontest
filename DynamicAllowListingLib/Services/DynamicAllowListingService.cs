using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.AzureResources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

    private readonly List<string> _allowedCrossSubscriptionSubnetList = new List<string>
        {
            "/subscriptions/f317937e-3f0c-4d7c-b0b2-2865d5b53c99/resourceGroups/rsgazuitcneuvnet01/providers/Microsoft.Network/virtualNetworks/vntazuitcneu01/subnets/mgmt01"
        };

    // Performance thresholds for slow operation warnings
    private const long SlowOperationThresholdMs = 5000;
    private const long VerySlowOperationThresholdMs = 10000;

    public DynamicAllowListingService(
        IRestHelper restHelper,
        IResourceDependencyInformationPersistenceService persistenceManager,
        IResourceGraphExplorerService resourceGraphExplorerService,
        IAzureResourceServiceFactory azureResourceServiceFactory,
        ISettingValidator<ResourceDependencyInformation> resourceDependencyInfoValidator,
        ILogger<DynamicAllowListingService> logger,
        IAzureResourceClassProvider classProvider)
    {
      _restHelper = restHelper ?? throw new ArgumentNullException(nameof(restHelper));
      _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
      _resourceGraphExplorerService = resourceGraphExplorerService ?? throw new ArgumentNullException(nameof(resourceGraphExplorerService));
      _azureResourceServiceFactory = azureResourceServiceFactory ?? throw new ArgumentNullException(nameof(azureResourceServiceFactory));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _resourceDependencyInfoValidator = resourceDependencyInfoValidator ?? throw new ArgumentNullException(nameof(resourceDependencyInfoValidator));
      _classProvider = classProvider ?? throw new ArgumentNullException(nameof(classProvider));
    }

    public async Task<ResultObject> UpdateDb(ResourceDependencyInformation resourceDependencyInformation)
    {
      var resourceId = resourceDependencyInformation.ResourceId ?? "Unknown";

      using (_logger.BeginResourceScope(resourceDependencyInformation))
      {
        _logger.LogServiceOperationStart(
            nameof(UpdateDb),
            resourceId,
            resourceDependencyInformation.ResourceName ?? "Unknown");

        _logger.LogDbUpdateStart(resourceId, "CreateOrReplace");

        try
        {
          var resultObject = await _persistenceManager.CreateOrReplaceItemInDb(resourceDependencyInformation);
          _logger.LogDbUpdateComplete(resourceId, "CreateOrReplace");
          _logger.LogServiceOperationComplete(nameof(UpdateDb), resourceId, true);

          return resultObject;
        }
        catch (Exception ex)
        {
          _logger.LogServiceException(ex, nameof(UpdateDb), resourceDependencyInformation);
          throw;
        }
      }
    }

    public async Task<ResultObject> UpdateUnmanagedResources(ResourceDependencyInformation resourceDependencyInformation)
    {
      var resultObject = new ResultObject();
      var resourceId = resourceDependencyInformation.ResourceId ?? "Unknown";

      using (_logger.BeginResourceScope(resourceDependencyInformation))
      {
        _logger.LogServiceOperationStart(
            nameof(UpdateUnmanagedResources),
            resourceId,
            resourceDependencyInformation.ResourceName ?? "Unknown");

        try
        {
          using var azureResourceService = _azureResourceServiceFactory.CreateInstance(resourceDependencyInformation);

          // Validate main resourceId
          if (string.IsNullOrEmpty(resourceDependencyInformation.ResourceId))
          {
            var warningMessage = "Main resourceId is null or empty for config";
            _logger.LogNullOrEmptyInput("ResourceId", nameof(UpdateUnmanagedResources));
            resultObject.Warnings.Add(warningMessage);
            throw new InvalidOperationException(warningMessage);
          }

          // Validate outbound resources
          if (resourceDependencyInformation.AllowOutbound?.ResourceIds == null ||
              resourceDependencyInformation.AllowOutbound.ResourceIds.Length <= 0)
          {
            var warningMessage = "AllowOutbound is null or contains no resource IDs. No unmanaged resources to update.";
            _logger.LogOutboundResourcesEmpty(resourceId);
            resultObject.Warnings.Add(warningMessage);
            return resultObject;
          }

          var outboundCount = resourceDependencyInformation.AllowOutbound.ResourceIds.Length;
          _logger.LogUnmanagedResourceProcessingStart(resourceId, outboundCount);

          var networkRestrictionsToAppend = await azureResourceService.GetAppendNetworkRestrictionSettings();

          foreach (var networkRestrictions in networkRestrictionsToAppend)
          {
            if (networkRestrictions.ResourceId == null)
            {
              _logger.LogNullOrEmptyInput("NetworkRestrictions.ResourceId", "AppendNetworkRestrictions");
              continue;
            }

            if (await IsDalManagedResource(networkRestrictions.ResourceId))
            {
              _logger.LogSkippingManagedResource(networkRestrictions.ResourceId);
              continue;
            }

            _logger.LogAppendingNetworkRestrictions(networkRestrictions.ResourceId, resourceId);

            var azureResource = await azureResourceService.GetAzureResource(networkRestrictions.ResourceId);
            if (azureResource == null)
            {
              var warningMessage = $"Resource with ResourceId {networkRestrictions.ResourceId} not found. It either doesn't exist or the process doesn't have the necessary access.";
              _logger.LogResourceNotFound(networkRestrictions.ResourceId, nameof(UpdateUnmanagedResources));
              resultObject.Warnings.Add(warningMessage);
              continue;
            }

            var appendResult = await azureResource.AppendNetworkRestrictionRules(networkRestrictions, _logger, _restHelper);
            resultObject.Merge(appendResult);

            _logger.LogUnmanagedResourceWarning(networkRestrictions.ResourceId);
            resultObject.Warnings.Add($"Resource {networkRestrictions.ResourceId} is not managed by DAL or is in another subscription! This can lead to issues in the future.");
          }

          resultObject.Merge(azureResourceService.ResultObject);

          _logger.LogServiceOperationComplete(nameof(UpdateUnmanagedResources), resourceId, true);
        }
        catch (Exception ex)
        {
          _logger.LogServiceException(ex, nameof(UpdateUnmanagedResources), resourceDependencyInformation);
          resultObject.Errors.Add($"An unexpected error occurred: {ex.Message}");
        }

        return resultObject;
      }
    }

    /// <summary>
    /// Overwrites network restriction rules for the main Azure resource.
    /// This method:
    /// 1. Creates an AzureResourceService instance for the resource
    /// 2. Retrieves the Azure resource from ARM API
    /// 3. Generates all network restriction settings (IP rules, subnet rules, service tags)
    /// 4. Validates subnet IDs exist in Azure
    /// 5. Applies restrictions to website deployment slots (if applicable)
    /// 6. Applies restrictions to the main resource
    /// </summary>
    public async Task<ResultObject> OverwriteNetworkRestrictionRulesForMainResource(
        ResourceDependencyInformation resourceDependencyInformation)
    {
      var resultObject = new ResultObject();
      var resourceId = resourceDependencyInformation.ResourceId ?? "Unknown";
      var resourceName = resourceDependencyInformation.ResourceName ?? "Unknown";
      var resourceType = resourceDependencyInformation.ResourceType ?? "Unknown";

      using (_logger.BeginResourceScope(resourceDependencyInformation))
      {
        _logger.LogServiceOperationStart(
            nameof(OverwriteNetworkRestrictionRulesForMainResource),
            resourceId,
            resourceName);

        // Log resource dependency information for audit
        var hasInbound = resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.ResourceIds?.Any() ?? false;
        var hasOutbound = resourceDependencyInformation.AllowOutbound?.ResourceIds?.Any() ?? false;
        var hasServiceTags = resourceDependencyInformation.AllowInbound?.SecurityRestrictions?.AzureServiceTags?.Any() ?? false;
        _logger.LogResourceDependencyInfo(resourceId, hasInbound, hasOutbound, hasServiceTags);

        try
        {
          // Step 1: Create AzureResourceService instance
          _logger.LogAzureResourceServiceCreating(resourceId, resourceName);
          using var azureResourceService = _azureResourceServiceFactory.CreateInstance(resourceDependencyInformation);
          _logger.LogAzureResourceServiceCreated(resourceId);

          // Step 2: Get the main Azure resource
          var mainAzureResource = await azureResourceService.GetAzureResource(resourceDependencyInformation.ResourceId!);
          _logger.LogAzureResourceRetrieved(
              resourceId,
              mainAzureResource?.Type ?? resourceType,
              mainAzureResource != null);

          // Step 3: Handle PrintOut mode parsing
          bool justPrintOutRules = false;
          if (!string.IsNullOrEmpty(resourceDependencyInformation.PrintOut))
          {
            bool.TryParse(resourceDependencyInformation.PrintOut, out justPrintOutRules);
            _logger.LogPrintOutModeParsed(resourceId, resourceDependencyInformation.PrintOut, justPrintOutRules);

            if (justPrintOutRules && mainAzureResource != null)
            {
              mainAzureResource.PrintOut = justPrintOutRules;
              _logger.LogPrintOutModeEnabled(resourceId);
            }
          }

          // Step 4: Handle resource not found scenario
          if (mainAzureResource == null && !justPrintOutRules)
          {
            _logger.LogMainResourceNotFound(resourceId, justPrintOutRules);
            _logger.LogResourceNotFound(resourceId, "OverwriteNetworkRestrictions");
            resultObject.Errors.Add($"Resource not found. ResourceId:{resourceId}.");
            resultObject.Merge(azureResourceService.ResultObject);
            return resultObject;
          }

          // Step 5: Retrieve network restrictions to overwrite
          var networkRestrictionsToOverwrite = await azureResourceService.GetUpdateNetworkRestrictionSettingsForMainResource();

          var ipRuleCount = networkRestrictionsToOverwrite.IpSecRules?.Count ?? 0;
          var scmRuleCount = networkRestrictionsToOverwrite.ScmIpSecRules?.Count ?? 0;
          var subnetRuleCount = networkRestrictionsToOverwrite.IpSecRules?
              .Count(r => !string.IsNullOrEmpty(r.VnetSubnetResourceId)) ?? 0;

          _logger.LogNetworkRestrictionRulesRetrieved(resourceId, ipRuleCount, scmRuleCount, subnetRuleCount);
          _logger.LogNetworkRestrictionOverwriteStart(resourceId, ipRuleCount, subnetRuleCount);

          // Step 6: Validate and remove missing subnet IDs
          _logger.LogSubnetValidationStart(resourceId, subnetRuleCount);
          var validatedRules = await RemoveMissingSubnetsWithLogging(
              networkRestrictionsToOverwrite,
              _allowedCrossSubscriptionSubnetList,
              resourceId);
          resultObject.Merge(validatedRules.Item1);
          _logger.LogSubnetValidationComplete(resourceId, validatedRules.Item2, validatedRules.Item3);

          // Step 7: Handle PrintOut mode when resource doesn't exist
          if (mainAzureResource == null && justPrintOutRules)
          {
            var emptyModel = _classProvider.GetResourceClass(resourceDependencyInformation.ResourceType!);

            if (emptyModel.GetType() == typeof(WebSite) || emptyModel.GetType() == typeof(PublicIpAddress))
            {
              const string errorMessage = "Website and Public IP Address types cannot be used to print out the rules!";
              _logger.LogPrintOutModeNotSupported(resourceId, resourceDependencyInformation.ResourceType!);
              resultObject.Errors.Add(errorMessage);
              return resultObject;
            }

            var rules = emptyModel.ConvertRulesToPrintOut(networkRestrictionsToOverwrite);
            resultObject.Data = new ResultObject.OutputData
            {
              IPs = rules.Item1,
              SubnetIds = rules.Item2
            };

            var ipCount = rules.Item1?.Split(',').Length ?? 0;
            var subnetCount = rules.Item2?.Split(',').Length ?? 0;
            _logger.LogPrintOutOutputGenerated(resourceId, ipCount, subnetCount);
            _logger.LogPrintOutMode(resourceId, ipCount, subnetCount);

            _logger.LogServiceOperationComplete(nameof(OverwriteNetworkRestrictionRulesForMainResource),
                resourceId, true);
            return resultObject;
          }

          // Step 8: Process website slot if applicable
          _logger.LogCheckingForWebsiteSlot(resourceId);
          var websiteSlot = await GetWebSiteSlotWithLogging(mainAzureResource!, resourceId);

          if (websiteSlot != null)
          {
            _logger.LogWebsiteSlotFound(resourceId, websiteSlot.Id!);
            _logger.LogProcessingWebsiteSlot(websiteSlot.Id!, resourceId);
            _logger.LogWebsiteSlotProcessing(websiteSlot.Id!, resourceId);

            var slotRestrictions = GetWebsiteSlotRestrictions(websiteSlot.Id!, networkRestrictionsToOverwrite);

            try
            {
              var slotRestrictionResult = await websiteSlot.OverWriteNetworkRestrictionRules(
                  slotRestrictions, _logger, _restHelper);
              resultObject.Merge(slotRestrictionResult);

              _logger.LogWebsiteSlotRestrictionsApplied(websiteSlot.Id!, !slotRestrictionResult.Errors.Any());
            }
            catch (Exception slotEx)
            {
              _logger.LogWebsiteSlotRestrictionsFailed(slotEx, websiteSlot.Id!);
              resultObject.Warnings.Add($"Failed to apply slot restrictions: {slotEx.Message}");
            }
          }
          else
          {
            if (mainAzureResource is WebSite)
            {
              _logger.LogNoWebsiteSlotFound(resourceId);
            }
            else
            {
              _logger.LogNotWebSiteSkippingSlotCheck(resourceId, mainAzureResource?.Type ?? resourceType);
            }
          }

          // Step 9: Overwrite main resource restrictions
          var overwriteResult = await mainAzureResource!.OverWriteNetworkRestrictionRules(
              networkRestrictionsToOverwrite, _logger, _restHelper);

          _logger.LogMergingResultObjects(
              resultObject.Errors.Count,
              overwriteResult.Errors.Count,
              resultObject.Warnings.Count,
              overwriteResult.Warnings.Count);

          resultObject.Merge(overwriteResult);

          // Step 10: Log completion summary
          var totalRulesApplied = ipRuleCount + scmRuleCount;
          _logger.LogOverwriteOperationSummary(
              resourceId,
              totalRulesApplied,
              !resultObject.Errors.Any(),
              resultObject.Warnings.Count,
              resultObject.Errors.Count);

          _logger.LogNetworkRestrictionOverwriteComplete(resourceId);
          _logger.LogServiceOperationComplete(nameof(OverwriteNetworkRestrictionRulesForMainResource),
              resourceId, !resultObject.Errors.Any());
        }
        catch (Exception ex)
        {
          _logger.LogServiceException(ex, nameof(OverwriteNetworkRestrictionRulesForMainResource),
              resourceDependencyInformation);
          resultObject.Errors.Add($"An unexpected error occurred: {ex.Message}");
        }

        return resultObject;
      }
    }

    /// <summary>
    /// Removes missing subnets with comprehensive logging.
    /// </summary>
    /// <returns>Tuple of (ResultObject, ValidSubnetCount, RemovedSubnetCount)</returns>
    private async Task<(ResultObject, int, int)> RemoveMissingSubnetsWithLogging(
        NetworkRestrictionSettings rules,
        List<string> allowedCrossSubscriptionSubnetList,
        string resourceId)
    {
      var resultObject = new ResultObject();
      int validCount = 0;
      int removedCount = 0;

      try
      {
        if (rules.IpSecRules == null || !rules.IpSecRules.Any())
        {
          return (resultObject, 0, 0);
        }

        var subnetRules = rules.IpSecRules
            .Where(r => !string.IsNullOrEmpty(r.VnetSubnetResourceId))
            .ToList();

        using (_logger.BeginSubnetValidationScope(resourceId, subnetRules.Count))
        {
          foreach (var rule in subnetRules)
          {
            var subnetId = rule.VnetSubnetResourceId!;

            // Check if it's an allowed cross-subscription subnet
            if (allowedCrossSubscriptionSubnetList.Contains(subnetId))
            {
              _logger.LogCrossSubscriptionSubnetAllowed(resourceId, subnetId);
              validCount++;
              continue;
            }

            // Extract subscription ID and check if subnet exists
            var subscriptionId = StringHelper.GetSubscriptionId(subnetId);
            var existingSubnets = await _resourceGraphExplorerService.GetAllSubnetIds(subscriptionId);

            if (existingSubnets.Contains(subnetId))
            {
              _logger.LogSubnetValidated(resourceId, subnetId);
              validCount++;
            }
            else
            {
              _logger.LogSubnetDoesNotExist(subnetId, subscriptionId);
              _logger.LogMissingSubnetRemoved(resourceId, subnetId);

              rules.IpSecRules.Remove(rule);
              removedCount++;

              resultObject.Warnings.Add($"Subnet {subnetId} does not exist and was removed from restriction list.");
            }
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogServiceException(ex, nameof(RemoveMissingSubnetsWithLogging));
      }

      return (resultObject, validCount, removedCount);
    }

    /// <summary>
    /// Gets website slot with logging.
    /// </summary>
    private async Task<WebSite?> GetWebSiteSlotWithLogging(
        IAzureResource mainAzureResource,
        string resourceId)
    {
      if (mainAzureResource is WebSite webSite)
      {
        using (_logger.BeginWebsiteSlotScope(resourceId, null))
        {
          var slots = await _resourceGraphExplorerService.GetWebAppSlots(webSite.Id!);
          return slots.FirstOrDefault();
        }
      }

      return null;
    }

    public async Task<ResultObject> CheckProvisioningSucceeded(ResourceDependencyInformation resourceDependencyInformation)
    {
      var resultObject = new ResultObject();
      var resourceId = resourceDependencyInformation.ResourceId ?? "Unknown";

      using (_logger.BeginResourceScope(resourceDependencyInformation))
      {
        _logger.LogServiceOperationStart(
            nameof(CheckProvisioningSucceeded),
            resourceId,
            resourceDependencyInformation.ResourceName ?? "Unknown");

        try
        {
          using var azureResourceService = _azureResourceServiceFactory.CreateInstance(resourceDependencyInformation);
          var azureResources = await azureResourceService.GetAzureResources();

          _logger.LogProvisioningCheckStart(resourceId, azureResources.Count);

          foreach (var azureResource in azureResources)
          {
            // Skip CosmosDb or PrintOut resources
            if (!string.IsNullOrEmpty(resourceDependencyInformation.PrintOut) ||
                resourceDependencyInformation.ResourceType == "Microsoft.DocumentDB/databaseAccounts")
            {
              continue;
            }


            // Cast the resource to CosmosDb and fetch provisioning status
            var cosmosDb = azureResource as CosmosDb;
            var props = await cosmosDb!.GetExistingCosmosDbArmProperties(_restHelper, _logger);
            string provisioningState = props!.Properties.ProvisioningState;

            if (provisioningState != "Succeeded")
            {
              _logger.LogProvisioningCheckFailed(azureResource.Id ?? "Unknown", provisioningState ?? "Unknown");
              resultObject.Errors.Add($"Resource {azureResource.Id} provisioning state is '{provisioningState}', expected 'Succeeded'.");
            }
            else
            {
              _logger.LogProvisioningCheckPassed(azureResource.Id ?? "Unknown", provisioningState);
            }
          }
          _logger.LogServiceOperationComplete(nameof(CheckProvisioningSucceeded),
              resourceId, !resultObject.Errors.Any());
        }
        catch (Exception ex) { 
          _logger.LogServiceException(ex, nameof(CheckProvisioningSucceeded),
              resourceDependencyInformation);
          resultObject.Errors.Add($"An unexpected error occurred: {ex.Message}");
        }

        return resultObject;
      }
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsForAppServicePlanScale(string appServicePlanResourceId)
    {
      var configsToOverwrite = new HashSet<ResourceDependencyInformation>();

      using (_logger.BeginOperationScope(nameof(GetOverwriteConfigsForAppServicePlanScale), appServicePlanResourceId))
      {
        _logger.LogServiceOperationStart(nameof(GetOverwriteConfigsForAppServicePlanScale),
            appServicePlanResourceId, "AppServicePlan");

        try
        {
          var resourcesHostedInAppServicePlan = await _resourceGraphExplorerService.GetResourcesHostedOnPlan(appServicePlanResourceId);

          _logger.LogAppServicePlanScaleConfigRetrieval(appServicePlanResourceId, resourcesHostedInAppServicePlan.Count());

          foreach (var resourceId in resourcesHostedInAppServicePlan)
          {
            var resourceDependencyInformation = await _persistenceManager.GetResourceDependencyInformation(resourceId);

            if (resourceDependencyInformation == null)
            {
              _logger.LogConfigNotFound(resourceId);
              continue;
            }

            var validationResult = _resourceDependencyInfoValidator.Validate(resourceDependencyInformation);
            if (!validationResult.Success)
            {
              _logger.LogValidationFailed(resourceId, validationResult.ToString() ?? "Unknown validation error");
              continue;
            }

            _logger.LogConfigFound(resourceId, resourceDependencyInformation.ResourceName ?? "Unknown");

            var configsWhereInbound = await GetConfigsWhereInbound(resourceDependencyInformation.ResourceId!);
            configsToOverwrite.UnionWith(configsWhereInbound);

            if (resourceDependencyInformation.AllowOutbound?.ResourceIds == null)
            {
              _logger.LogOutboundResourcesEmpty(resourceId);
              continue;
            }

            var outboundConfigs = await GetConfigsForResources(resourceDependencyInformation.AllowOutbound.ResourceIds);
            configsToOverwrite.UnionWith(outboundConfigs);
          }
          _logger.LogServiceOperationComplete(nameof(GetOverwriteConfigsForAppServicePlanScale),
              appServicePlanResourceId, true);
        }
        catch (Exception ex)
        {
          _logger.LogServiceOperationFailed(ex, nameof(GetOverwriteConfigsForAppServicePlanScale),
              appServicePlanResourceId);
        }

        return configsToOverwrite;
      }
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsWhenWebAppDeleted(string deletedWebAppResourceId)
    {
      var configsToOverwrite = new HashSet<ResourceDependencyInformation>();

      using (_logger.BeginOperationScope(nameof(GetOverwriteConfigsWhenWebAppDeleted), deletedWebAppResourceId))
      {
        _logger.LogServiceOperationStart(nameof(GetOverwriteConfigsWhenWebAppDeleted),
            deletedWebAppResourceId, "DeletedWebApp");

        try
        {
          var updatedItems = await _persistenceManager.RemoveConfigAndDependencies(deletedWebAppResourceId);

          _logger.LogDeletedWebAppProcessing(deletedWebAppResourceId, updatedItems.Count());

          foreach (var resourceDependencyInformation in updatedItems)
          {
            var validationResult = _resourceDependencyInfoValidator.Validate(resourceDependencyInformation);
            if (!validationResult.Success)
            {
              _logger.LogValidationFailed(
                  resourceDependencyInformation.ResourceId ?? "Unknown",
                  validationResult.ToString() ?? "Unknown validation error");
              continue;
            }

            _logger.LogConfigFound(
                resourceDependencyInformation.ResourceId ?? "Unknown",
                resourceDependencyInformation.ResourceName ?? "Unknown");

            configsToOverwrite.Add(resourceDependencyInformation);
          }
          _logger.LogServiceOperationComplete(nameof(GetOverwriteConfigsWhenWebAppDeleted),
              deletedWebAppResourceId, true);
        }
        catch (Exception ex)
        {
          _logger.LogServiceOperationFailed(ex, nameof(GetOverwriteConfigsWhenWebAppDeleted),
              deletedWebAppResourceId);
        }

        return configsToOverwrite;
      }
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOutboundOverwriteConfigs(ResourceDependencyInformation resourceDependencyInformation)
    {
      var configsToBeOverwritten = new HashSet<ResourceDependencyInformation>();
      var resourceId = resourceDependencyInformation.ResourceId ?? "Unknown";

      using (_logger.BeginResourceScope(resourceDependencyInformation))
      {
        _logger.LogServiceOperationStart(nameof(GetOutboundOverwriteConfigs),
            resourceId, resourceDependencyInformation.ResourceName ?? "Unknown");

        try
        {
          var outboundResources = GetOutboundResources(resourceDependencyInformation);
          var outboundCount = outboundResources.Count();

          _logger.LogOutboundConfigRetrieval(resourceId, outboundCount);

          if (!outboundResources.Any())
          {
            _logger.LogInformation("No outbound resources found for ResourceId: {ResourceId}", resourceId);
            return configsToBeOverwritten;
          }

          var configs = await GetConfigsForResources(outboundResources);
          configsToBeOverwritten.UnionWith(configs);

          _logger.LogServiceOperationComplete(nameof(GetOutboundOverwriteConfigs),
              resourceId, true);

          _logger.LogInformation(
              "Retrieved {ConfigCount} configurations for outbound resources | ResourceId: {ResourceId}",
              configsToBeOverwritten.Count, resourceId);
        }
        catch (Exception ex)
        {
          _logger.LogServiceException(ex, nameof(GetOutboundOverwriteConfigs),
              resourceDependencyInformation);
        }

        return configsToBeOverwritten;
      }
    }

    #region Private Helper Methods

    private async Task<ResourceDependencyInformation?> GetConfigForResource(string resourceId)
    {
      using (_logger.BeginOperationScope(nameof(GetConfigForResource), resourceId))
      {
        try
        {
          var resourceDependencyInformation = await _persistenceManager.GetResourceDependencyInformation(resourceId);

          if (resourceDependencyInformation == null)
          {
            _logger.LogConfigNotFound(resourceId);
            return null;
          }

          var validationResult = _resourceDependencyInfoValidator.Validate(resourceDependencyInformation);
          if (!validationResult.Success)
          {
            _logger.LogValidationFailed(resourceId, validationResult.ToString() ?? "Unknown");
          }

          _logger.LogConfigFound(resourceId, resourceDependencyInformation.ResourceName ?? "Unknown");
          return resourceDependencyInformation;
        }
        catch (Exception ex)
        {
          _logger.LogServiceException(ex, nameof(GetConfigForResource));
          return null;
        }
      }
    }

    private async Task<bool> IsDalManagedResource(string resourceId)
        => await GetConfigForResource(resourceId) != null;

    private async Task<HashSet<ResourceDependencyInformation>> GetConfigsWhereInbound(string resourceId)
    {
      var resourceDependencyInfoHashSet = new HashSet<ResourceDependencyInformation>();

      using (_logger.BeginOperationScope(nameof(GetConfigsWhereInbound), resourceId))
      {
        try
        {
          var configsFromDb = await _persistenceManager.GetConfigsWhereInbound(resourceId);

          if (configsFromDb == null || !configsFromDb.Any())
          {
            _logger.LogInformation("No inbound configurations found | ResourceId: {ResourceId}", resourceId);
            return resourceDependencyInfoHashSet;
          }

          foreach (var configFromDb in configsFromDb)
          {
            var validationResult = _resourceDependencyInfoValidator.Validate(configFromDb);
            if (!validationResult.Success)
            {
              _logger.LogValidationFailed(resourceId, validationResult.ToString() ?? "Unknown");
              continue;
            }

            _logger.LogConfigFound(configFromDb.ResourceId ?? "Unknown", configFromDb.ResourceName ?? "Unknown");
            resourceDependencyInfoHashSet.Add(configFromDb);
          }

          _logger.LogInboundConfigsRetrieved(resourceId, resourceDependencyInfoHashSet.Count);
        }
        catch (Exception ex)
        {
          _logger.LogServiceException(ex, nameof(GetConfigsWhereInbound));
        }

        return resourceDependencyInfoHashSet;
      }
    }

    private async Task<HashSet<ResourceDependencyInformation>> GetConfigsForResources(IEnumerable<string> resourceIds)
    {
      var resourceDependencyInfoHashSet = new HashSet<ResourceDependencyInformation>();

      foreach (var resourceId in resourceIds)
      {
        var config = await GetConfigForResource(resourceId);
        if (config != null)
        {
          resourceDependencyInfoHashSet.Add(config);
        }
      }

      return resourceDependencyInfoHashSet;
    }

    private static IEnumerable<string> GetOutboundResources(ResourceDependencyInformation resourceDependencyInformation)
    {
      if (resourceDependencyInformation.AllowOutbound?.ResourceIds == null)
      {
        return Enumerable.Empty<string>();
      }

      return resourceDependencyInformation.AllowOutbound.ResourceIds
          .Where(id => !string.IsNullOrEmpty(id));
    }

    private async Task<ResultObject> RemoveMissingSubnets(
        NetworkRestrictionSettings rules,
        List<string> allowedCrossSubscriptionSubnetList)
    {
      var resultObject = new ResultObject();
      var resourceId = rules.ResourceId ?? "Unknown";

      try
      {
        if (rules.IpSecRules == null || !rules.IpSecRules.Any())
        {
          return resultObject;
        }

        var subnetIds = rules.IpSecRules
            .Where(r => !string.IsNullOrEmpty(r.VnetSubnetResourceId))
            .Select(r => r.VnetSubnetResourceId!)
            .ToList();

        if (!subnetIds.Any())
        {
          return resultObject;
        }

        var subscriptionIds = subnetIds
            .Select(id => id.Split('/')[2])
            .Distinct()
            .ToList();

        var skippedSubnetIds = subnetIds
            .Where(id => allowedCrossSubscriptionSubnetList.Any(allowed =>
                id.Equals(allowed, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        _logger.LogCrossSubscriptionSubnets(skippedSubnetIds.Count, subnetIds.Count);

        var crossTenantSubscriptionIds = skippedSubnetIds
            .Select(id => id.Split('/')[2])
            .ToList();

        var existingSubnetIds = new List<string>();

        foreach (var subscriptionId in subscriptionIds.Except(crossTenantSubscriptionIds))
        {
          var subnets = await _resourceGraphExplorerService.GetAllSubnetIds(subscriptionId);
          existingSubnetIds.AddRange(subnets);
          _logger.LogSubnetFetch(subscriptionId, subnets.Count());
        }

        existingSubnetIds.AddRange(skippedSubnetIds);
        existingSubnetIds = existingSubnetIds.ConvertAll(d => d.ToLower());

        var missingSubnetIds = subnetIds
            .Where(x => !existingSubnetIds.Contains(x.ToLower()))
            .ToList();

        if (missingSubnetIds.Count > 0)
        {
          rules.IpSecRules = rules.IpSecRules
              .Where(x => string.IsNullOrEmpty(x.VnetSubnetResourceId) ||
                         !missingSubnetIds.Contains(x.VnetSubnetResourceId))
              .ToHashSet();

          foreach (var missingSubnetId in missingSubnetIds)
          {
            _logger.LogMissingSubnetRemoved(missingSubnetId, resourceId);
            resultObject.Warnings.Add($"Subnet Id:{missingSubnetId} does not exist! Removed from restriction list!");
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogServiceException(ex, nameof(RemoveMissingSubnets));
      }

      return resultObject;
    }

    private static async Task<WebSite?> GetWebSiteSlot(
        IAzureResource mainAzureResource,
        IResourceGraphExplorerService resourceGraphExplorerService)
    {
      if (mainAzureResource is WebSite webSite)
      {
        var slots = await resourceGraphExplorerService.GetWebAppSlots(webSite.Id!);
        return slots.FirstOrDefault();
      }

      return null;
    }

    private static NetworkRestrictionSettings GetWebsiteSlotRestrictions(
        string slotId,
        NetworkRestrictionSettings mainResourceRestrictions)
    {
      return new NetworkRestrictionSettings
      {
        ResourceId = slotId,
        IpSecRules = mainResourceRestrictions.IpSecRules,
        ScmIpSecRules = mainResourceRestrictions.ScmIpSecRules
      };
    }

    private void LogSlowOperationIfNeeded(string methodName, long durationMs)
    {
      if (durationMs >= VerySlowOperationThresholdMs)
      {
        _logger.LogError(
            "Very slow operation detected | Method: {MethodName} | Duration: {DurationMs}ms | Threshold: {ThresholdMs}ms",
            methodName, durationMs, VerySlowOperationThresholdMs);
      }
      else if (durationMs >= SlowOperationThresholdMs)
      {
        _logger.LogSlowOperation(methodName, durationMs, SlowOperationThresholdMs);
      }
    }

    #endregion
  }
}