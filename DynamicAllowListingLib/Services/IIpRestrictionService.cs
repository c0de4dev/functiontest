using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Services
{
  public interface IIpRestrictionService<T>
  {
    Task<HashSet<ResourceDependencyInformation>> GetValidDependencyConfigs(T updatedServiceTagModels);
  }

  public class BaseIpRestrictionService
  {
    private readonly IResourceDependencyInformationPersistenceService _dependencyInfoManager;
    private readonly IPersistenceManager<AzureSubscription> _azureSubscriptionPersistenceManager;
    private readonly IResourceGraphExplorerService _resourceService;
    private readonly ISettingValidator<ResourceDependencyInformation> _validator;
    private readonly ILogger _logger;

    public BaseIpRestrictionService(
        IResourceDependencyInformationPersistenceService dependencyInfoManager,
        IPersistenceManager<AzureSubscription> azureSubscriptionPersistenceManager,
        IResourceGraphExplorerService resourceService,
        ISettingValidator<ResourceDependencyInformation> validator,
        ILogger logger)
    {
      _azureSubscriptionPersistenceManager = azureSubscriptionPersistenceManager;
      _dependencyInfoManager = dependencyInfoManager;
      _resourceService = resourceService;
      _validator = validator;
      _logger = logger;
    }

    public async Task<HashSet<ResourceDependencyInformation>> FindRelatedDependencyConfigs(List<ServiceTag> relatedServiceTags)
    {
      _logger.LogMethodStart(nameof(FindRelatedDependencyConfigs));
      _logger.LogFindingRelatedDependencyConfigs(relatedServiceTags?.Count ?? 0);

      HashSet<ResourceDependencyInformation> dependencyConfigList;

      try
      {
        using (_logger.BeginIpRestrictionScope(nameof(FindRelatedDependencyConfigs), relatedServiceTags?.Count ?? 0))
        {
          if (await IsMandatoryForCurrentSubscription(relatedServiceTags!))
          {
            _logger.LogMandatoryServiceTag();
            // Get all dependency configs because service tag is mandatory
            dependencyConfigList = new HashSet<ResourceDependencyInformation>(await _dependencyInfoManager.GetAll());
          }
          else
          {
            // Find dependency config by referenced service tag
            dependencyConfigList = await FindDependencyConfigs(relatedServiceTags!);
          }


          if (dependencyConfigList.Any())
          {
            _logger.LogNetworkRestrictionConfigsFound(dependencyConfigList.Count);
          }
          else
          {
            _logger.LogNoNetworkRestrictionConfigsFound();
          }

          _logger.LogMethodComplete(nameof(FindRelatedDependencyConfigs), true);
        }
      }
      catch (Exception ex)
      {
        _logger.LogMethodException(ex, nameof(FindRelatedDependencyConfigs));
        throw;
      }

      return dependencyConfigList;
    }

    public async Task<HashSet<ResourceDependencyInformation>> RemoveInvalidResourceConfigs(HashSet<ResourceDependencyInformation> dependencyConfigList)
    {
      var initialCount = dependencyConfigList?.Count ?? 0;
      _logger.LogMethodStart(nameof(RemoveInvalidResourceConfigs));
      _logger.LogStartingConfigValidation(initialCount);

      try
      {
        using (_logger.BeginConfigValidationScope(initialCount))
        {
          // Duplicated resource id check
          var duplicatedIds = dependencyConfigList!
              .GroupBy(x => x.ResourceId)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();

          var uniqueIdConfigs = (dependencyConfigList ?? Enumerable.Empty<ResourceDependencyInformation>())
              .Where(x => !duplicatedIds.Contains(x.ResourceId))
              .ToList();

          if (duplicatedIds.Any())
          {
            _logger.LogDuplicatedResourceIds(
                duplicatedIds.Count,
                string.Join(", ", duplicatedIds));
          }

          // Wrong formatted resource id check
          var wrongFormattedIds = uniqueIdConfigs
              .Where(x => !_validator.ValidateFormat(x).Success)
              .Select(x => x.ResourceId)
              .ToList();

          var correctFormattedIdConfigs = uniqueIdConfigs
              .Where(x => !wrongFormattedIds.Contains(x.ResourceId))
              .ToList();

          if (wrongFormattedIds.Any())
          {
            _logger.LogInvalidFormatResourceIds(
                wrongFormattedIds.Count,
                string.Join(", ", wrongFormattedIds));
          }

          // Main resource existence check
          var nonexistentIds = await GetNonexistentResourceIds(correctFormattedIdConfigs);
          var validConfigs = correctFormattedIdConfigs
              .Where(x => !nonexistentIds.Contains(x.ResourceId!))
              .ToList();

          if (nonexistentIds.Any())
          {
            _logger.LogNonexistentResourceIds(
                nonexistentIds.Count,
                string.Join(", ", nonexistentIds));
          }

          // Remove nonexistent configs from database
          await RemoveInvalidConfigsFromDb(nonexistentIds);

          // Log summary of invalid configs
          LogInvalidConfigsSummary("duplicated", duplicatedIds!);
          LogInvalidConfigsSummary("invalid", wrongFormattedIds!);
          LogInvalidConfigsSummary("nonexistent", nonexistentIds!);

          var removedCount = initialCount - validConfigs.Count;
          _logger.LogConfigValidationComplete(validConfigs.Count, removedCount);

          return new HashSet<ResourceDependencyInformation>(validConfigs);
        }
      }
      catch (Exception ex)
      {
        _logger.LogMethodException(ex, nameof(RemoveInvalidResourceConfigs));
        throw;
      }
    }

    private async Task<List<string>> GetNonexistentResourceIds(List<ResourceDependencyInformation> configs)
    {
      if (configs == null || configs.Count == 0)
      {
        _logger.LogNoConfigsForExistenceCheck();
        return new List<string>();
      }

      var azureSubscriptionId = configs
          .First(x => !string.IsNullOrEmpty(x.RequestSubscriptionId))
          .RequestSubscriptionId;

      var resourceIdsToBeChecked = configs.Select(x => x.ResourceId).ToList();

      _logger.LogCheckingResourceExistence(azureSubscriptionId!, resourceIdsToBeChecked.Count);

      var existingResourceIds = await _resourceService.GetExistingResourceIdsByType(
          azureSubscriptionId!,
          new List<string>
          {
            AzureResourceType.CosmosDb,
            AzureResourceType.WebSite,
            AzureResourceType.Storage,
            AzureResourceType.PublicIpAddress,
            AzureResourceType.WebSiteSlot
          });

      var nonexistentIds = resourceIdsToBeChecked.Except(existingResourceIds).ToList();

      _logger.LogResourceExistenceCheckComplete(
          existingResourceIds.Count(),
          nonexistentIds.Count);

      return nonexistentIds!;
    }

    private void LogInvalidConfigsSummary(string type, List<string> resourceIds)
    {
      if (resourceIds != null && resourceIds.Count > 0)
      {
        _logger.LogInvalidConfigs(
            type,
            resourceIds.Count,
            string.Join(", ", resourceIds));
      }
    }

    private async Task RemoveInvalidConfigsFromDb(params object[] args)
    {
      var invalidRecords = new List<string>();

      foreach (var invalidResourceIds in args)
      {
        invalidRecords.AddRange((List<string>)invalidResourceIds);
      }

      var distinctRecords = invalidRecords.Distinct().ToList();

      if (!distinctRecords.Any())
      {
        return;
      }

      _logger.LogRemovingInvalidConfigsFromDb(distinctRecords.Count);

      foreach (var invalidResourceId in distinctRecords)
      {
        try
        {
          await _dependencyInfoManager.RemoveConfig(invalidResourceId);
        }
        catch (Exception ex)
        {
          _logger.LogRemoveConfigFromDbFailed(ex, invalidResourceId);
        }
      }
      _logger.LogRemovedResourceIdsFromDb(
          string.Join(", ", distinctRecords));
    }

    public async Task UpdateConfigsInDb(ResourceDependencyInformation config)
    {
      try
      {
        await _dependencyInfoManager.CreateOrReplaceItemInDb(config);
        _logger.LogConfigUpdatedInDb(
            config.ResourceId ?? "Unknown",
            config.ResourceName ?? "Unknown");
      }
      catch (Exception ex)
      {
        _logger.LogUpdateConfigInDbFailed(ex, config.ResourceId ?? "Unknown");
        throw;
      }
    }

    private async Task<HashSet<ResourceDependencyInformation>> FindDependencyConfigs(List<ServiceTag> relatedServiceTags)
    {
      var dependencyConfigList = new HashSet<ResourceDependencyInformation>();

      foreach (var serviceTag in relatedServiceTags)
      {
        _logger.LogFindingConfigsForServiceTag(
            serviceTag.Id ?? "Unknown",
            serviceTag.Name ?? "Unknown");

        var configsForTag = await _dependencyInfoManager.FindByInternalAndThirdPartyTagName(serviceTag);

        foreach (var dependencyInfo in configsForTag)
        {
          dependencyConfigList.Add(dependencyInfo);
        }
        _logger.LogDependencyConfigsLookupComplete(
            serviceTag.Name ?? "Unknown",
            configsForTag.Count());
      }

      return dependencyConfigList;
    }

    public async Task<bool> IsMandatoryForCurrentSubscription(List<ServiceTag> relatedServiceTags)
    {
      var firstRecordFromDb = await _dependencyInfoManager.GetFirstOrDefault();
      var subscriptionName = await GetSubscriptionNameById(firstRecordFromDb.RequestSubscriptionId);

      if (string.IsNullOrEmpty(subscriptionName))
      {
        _logger.LogSubscriptionNotFound(firstRecordFromDb.RequestSubscriptionId ?? "Unknown");
        throw new ArgumentNullException(
            $"There is no subscription found for the id:{firstRecordFromDb.RequestSubscriptionId}");
      }

      _logger.LogCheckingMandatoryTag(subscriptionName);

      var isAnyMandatoryTagForSubscription = relatedServiceTags.Any(x =>
          x.AllowedSubscriptions
              .Where(s => s.SubscriptionName == subscriptionName && s.IsMandatory == true)
              .Any());

      _logger.LogMandatoryTagCheckResult(subscriptionName, isAnyMandatoryTagForSubscription);

      return isAnyMandatoryTagForSubscription;
    }

    public async Task<string> GetSubscriptionNameById(string? requestSubscriptionId)
    {
      if (requestSubscriptionId != null)
      {
        using (_logger.BeginSubscriptionScope(requestSubscriptionId))
        {
          var azureSubscription = await _azureSubscriptionPersistenceManager.GetById(requestSubscriptionId);
          var subscriptionName = azureSubscription?.Name ?? string.Empty;

          if (!string.IsNullOrEmpty(subscriptionName))
          {
            _logger.LogSubscriptionNameRetrieved(requestSubscriptionId, subscriptionName);
          }
          else
          {
            _logger.LogSubscriptionNotFound(requestSubscriptionId);
          }

          return subscriptionName;
        }
      }

      return string.Empty;
    }
  }
}