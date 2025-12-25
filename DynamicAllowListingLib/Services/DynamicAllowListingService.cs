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
      var stopwatch = Stopwatch.StartNew();
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

          stopwatch.Stop();
          _logger.LogDbUpdateComplete(resourceId, "CreateOrReplace", stopwatch.ElapsedMilliseconds);
          _logger.LogServiceOperationComplete(nameof(UpdateDb), resourceId, stopwatch.ElapsedMilliseconds, true);

          LogSlowOperationIfNeeded(nameof(UpdateDb), stopwatch.ElapsedMilliseconds);

          return resultObject;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogServiceException(ex, nameof(UpdateDb), resourceDependencyInformation, stopwatch.ElapsedMilliseconds);
          throw;
        }
      }
    }

    public async Task<ResultObject> UpdateUnmanagedResources(ResourceDependencyInformation resourceDependencyInformation)
    {
      var stopwatch = Stopwatch.StartNew();
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

          stopwatch.Stop();
          _logger.LogServiceOperationComplete(nameof(UpdateUnmanagedResources), resourceId, stopwatch.ElapsedMilliseconds, true);
          LogSlowOperationIfNeeded(nameof(UpdateUnmanagedResources), stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogServiceException(ex, nameof(UpdateUnmanagedResources), resourceDependencyInformation, stopwatch.ElapsedMilliseconds);
          resultObject.Errors.Add($"An unexpected error occurred: {ex.Message}");
        }

        return resultObject;
      }
    }

    public async Task<ResultObject> OverwriteNetworkRestrictionRulesForMainResource(ResourceDependencyInformation resourceDependencyInformation)
    {
      var stopwatch = Stopwatch.StartNew();
      var resultObject = new ResultObject();
      var resourceId = resourceDependencyInformation.ResourceId ?? "Unknown";

      using (_logger.BeginResourceScope(resourceDependencyInformation))
      {
        _logger.LogServiceOperationStart(
            nameof(OverwriteNetworkRestrictionRulesForMainResource),
            resourceId,
            resourceDependencyInformation.ResourceName ?? "Unknown");

        try
        {
          using var azureResourceService = _azureResourceServiceFactory.CreateInstance(resourceDependencyInformation);
          var mainAzureResource = await azureResourceService.GetAzureResource(resourceDependencyInformation.ResourceId!);

          bool justPrintOutRules = false;
          if (bool.TryParse(resourceDependencyInformation.PrintOut, out justPrintOutRules) && mainAzureResource != null)
          {
            mainAzureResource.PrintOut = justPrintOutRules;
            _logger.LogInformation("PrintOut mode set to {PrintOut} for ResourceId: {ResourceId}",
                justPrintOutRules, resourceId);
          }

          // Resource not found and not in PrintOut mode
          if (mainAzureResource == null && !justPrintOutRules)
          {
            _logger.LogResourceNotFound(resourceId, "OverwriteNetworkRestrictions");
            resultObject.Errors.Add($"Resource not found. ResourceId:{resourceId}.");
            resultObject.Merge(azureResourceService.ResultObject);
            return resultObject;
          }

          // Retrieve network restrictions to overwrite
          var networkRestrictionsToOverwrite = await azureResourceService.GetUpdateNetworkRestrictionSettingsForMainResource();

          var ipRuleCount = networkRestrictionsToOverwrite.IpSecRules?.Count ?? 0;
          var subnetRuleCount = networkRestrictionsToOverwrite.IpSecRules?
              .Count(r => !string.IsNullOrEmpty(r.VnetSubnetResourceId)) ?? 0;

          _logger.LogNetworkRestrictionOverwriteStart(resourceId, ipRuleCount, subnetRuleCount);

          // Remove missing subnet IDs
          var validatedRules = await RemoveMissingSubnets(networkRestrictionsToOverwrite, _allowedCrossSubscriptionSubnetList);
          resultObject.Merge(validatedRules);

          // Handle PrintOut mode when resource doesn't exist
          if (mainAzureResource == null && justPrintOutRules)
          {
            var emptyModel = _classProvider.GetResourceClass(resourceDependencyInformation.ResourceType!);

            if (emptyModel.GetType() == typeof(WebSite) || emptyModel.GetType() == typeof(PublicIpAddress))
            {
              const string errorMessage = "Website and Public IP Address types cannot be used to print out the rules!";
              resultObject.Errors.Add(errorMessage);
              _logger.LogError("PrintOut not supported for resource type | ResourceType: {ResourceType}",
                  resourceDependencyInformation.ResourceType);
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
            _logger.LogPrintOutMode(resourceId, ipCount, subnetCount);

            stopwatch.Stop();
            _logger.LogServiceOperationComplete(nameof(OverwriteNetworkRestrictionRulesForMainResource),
                resourceId, stopwatch.ElapsedMilliseconds, true);
            return resultObject;
          }

          // Process website slot if applicable
          var websiteSlot = await GetWebSiteSlot(mainAzureResource!, _resourceGraphExplorerService);
          if (websiteSlot != null)
          {
            var slotStopwatch = Stopwatch.StartNew();
            _logger.LogWebsiteSlotProcessing(websiteSlot.Id!, resourceId);

            var slotRestrictions = GetWebsiteSlotRestrictions(websiteSlot.Id!, networkRestrictionsToOverwrite);
            var slotRestrictionResult = await websiteSlot.OverWriteNetworkRestrictionRules(slotRestrictions, _logger, _restHelper);
            resultObject.Merge(slotRestrictionResult);

            slotStopwatch.Stop();
            _logger.LogWebsiteSlotRestrictionsApplied(websiteSlot.Id!, slotStopwatch.ElapsedMilliseconds);
          }

          // Overwrite main resource restrictions
          var overwriteResult = await mainAzureResource!.OverWriteNetworkRestrictionRules(
              networkRestrictionsToOverwrite, _logger, _restHelper);
          resultObject.Merge(overwriteResult);

          stopwatch.Stop();
          _logger.LogNetworkRestrictionOverwriteComplete(resourceId, stopwatch.ElapsedMilliseconds);
          _logger.LogServiceOperationComplete(nameof(OverwriteNetworkRestrictionRulesForMainResource),
              resourceId, stopwatch.ElapsedMilliseconds, !resultObject.Errors.Any());

          LogSlowOperationIfNeeded(nameof(OverwriteNetworkRestrictionRulesForMainResource), stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogServiceException(ex, nameof(OverwriteNetworkRestrictionRulesForMainResource),
              resourceDependencyInformation, stopwatch.ElapsedMilliseconds);
          resultObject.Errors.Add($"An unexpected error occurred: {ex.Message}");
        }

        return resultObject;
      }
    }

    public async Task<ResultObject> CheckProvisioningSucceeded(ResourceDependencyInformation resourceDependencyInformation)
    {
      var stopwatch = Stopwatch.StartNew();
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

            var provisioningState = azureResource.ProvisioningState;

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

          stopwatch.Stop();
          _logger.LogServiceOperationComplete(nameof(CheckProvisioningSucceeded),
              resourceId, stopwatch.ElapsedMilliseconds, !resultObject.Errors.Any());
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogServiceException(ex, nameof(CheckProvisioningSucceeded),
              resourceDependencyInformation, stopwatch.ElapsedMilliseconds);
          resultObject.Errors.Add($"An unexpected error occurred: {ex.Message}");
        }

        return resultObject;
      }
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsForAppServicePlanScale(string appServicePlanResourceId)
    {
      var stopwatch = Stopwatch.StartNew();
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

          stopwatch.Stop();
          _logger.LogServiceOperationComplete(nameof(GetOverwriteConfigsForAppServicePlanScale),
              appServicePlanResourceId, stopwatch.ElapsedMilliseconds, true);

          LogSlowOperationIfNeeded(nameof(GetOverwriteConfigsForAppServicePlanScale), stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogServiceOperationFailed(ex, nameof(GetOverwriteConfigsForAppServicePlanScale),
              appServicePlanResourceId, stopwatch.ElapsedMilliseconds);
        }

        return configsToOverwrite;
      }
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOverwriteConfigsWhenWebAppDeleted(string deletedWebAppResourceId)
    {
      var stopwatch = Stopwatch.StartNew();
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

          stopwatch.Stop();
          _logger.LogServiceOperationComplete(nameof(GetOverwriteConfigsWhenWebAppDeleted),
              deletedWebAppResourceId, stopwatch.ElapsedMilliseconds, true);
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogServiceOperationFailed(ex, nameof(GetOverwriteConfigsWhenWebAppDeleted),
              deletedWebAppResourceId, stopwatch.ElapsedMilliseconds);
        }

        return configsToOverwrite;
      }
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetOutboundOverwriteConfigs(ResourceDependencyInformation resourceDependencyInformation)
    {
      var stopwatch = Stopwatch.StartNew();
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
            stopwatch.Stop();
            return configsToBeOverwritten;
          }

          var configs = await GetConfigsForResources(outboundResources);
          configsToBeOverwritten.UnionWith(configs);

          stopwatch.Stop();
          _logger.LogServiceOperationComplete(nameof(GetOutboundOverwriteConfigs),
              resourceId, stopwatch.ElapsedMilliseconds, true);

          _logger.LogInformation(
              "Retrieved {ConfigCount} configurations for outbound resources | ResourceId: {ResourceId}",
              configsToBeOverwritten.Count, resourceId);
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogServiceException(ex, nameof(GetOutboundOverwriteConfigs),
              resourceDependencyInformation, stopwatch.ElapsedMilliseconds);
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