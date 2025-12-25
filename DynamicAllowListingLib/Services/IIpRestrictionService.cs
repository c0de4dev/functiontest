using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

    public BaseIpRestrictionService(IResourceDependencyInformationPersistenceService dependencyInfoManager,
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
      HashSet<ResourceDependencyInformation> dependencyConfigList;
      if (await IsMandatoryForCurrentSubscription(relatedServiceTags))
      {
        _logger.LogInformation($"Updated Service Tag is Mandatory for current subscription!");
        //get all dependency configs because service tag is mandatory
        dependencyConfigList = new HashSet<ResourceDependencyInformation>(await _dependencyInfoManager.GetAll());
      }
      else
      {
        //Find dependency config by referenced service tag
        dependencyConfigList = await FindDependencyConfigs(relatedServiceTags);
      }

      if (dependencyConfigList.Any())
        _logger.LogInformation($"{dependencyConfigList.Count} Network Restriction Config found!");

      return dependencyConfigList;
    }

    public async Task<HashSet<ResourceDependencyInformation>> RemoveInvalidResourceConfigs(HashSet<ResourceDependencyInformation> dependencyConfigList)
    {
      // duplicated resource id check
      var duplicatedIds = dependencyConfigList.GroupBy(x => x.ResourceId).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
      var uniqueIdConfigs = dependencyConfigList.Where(x => !duplicatedIds.Contains(x.ResourceId)).ToList();
      // do not remove duplicated records as it may cause removing the existing resourceId from all the configs referencing that id

      // wrong formatted resource id check
      var wrongFormattedIds = uniqueIdConfigs.Where(x => !_validator.ValidateFormat(x).Success).Select(x => x.ResourceId).ToList();
      var correctFormattedIdConfigs = uniqueIdConfigs.Where(x => !wrongFormattedIds.Contains(x.ResourceId)).ToList();

      // main resource existence check
      var nonexistentIds = await GetNonexistentResourceIds(correctFormattedIdConfigs);
      var validConfigs = correctFormattedIdConfigs.Where(x => !nonexistentIds.Contains(x.ResourceId!)).ToList();

      //remove nonexisting configs
      await RemoveInvalidConfigsFromDb(nonexistentIds);

      LogInvalidConfigs("duplicated", duplicatedIds!);
      LogInvalidConfigs("invalid", wrongFormattedIds!);
      LogInvalidConfigs("nonexistent", nonexistentIds!);

      return new HashSet<ResourceDependencyInformation>(validConfigs);
    }

    private async Task<List<string>> GetNonexistentResourceIds(List<ResourceDependencyInformation> configs)
    {
      if (configs != null && configs.Count > 0)
      {
        var azureSubscriptionId = configs.First(x => !string.IsNullOrEmpty(x.RequestSubscriptionId)).RequestSubscriptionId;
        var resourceIdsToBeChecked = configs.Select(x => x.ResourceId).ToList();

        var existingResourceIds = await _resourceService.GetExistingResourceIdsByType(azureSubscriptionId!, new List<string>
        {
          AzureResourceType.CosmosDb,
          AzureResourceType.WebSite,
          AzureResourceType.Storage,
          AzureResourceType.PublicIpAddress,
          AzureResourceType.WebSiteSlot
        });
        var nonexistentIds = resourceIdsToBeChecked.Except(existingResourceIds).ToList();
        return nonexistentIds!;
      }
      return new List<string>();
    }

    private void LogInvalidConfigs(string type, List<string> resourceIds)
    {
      if (resourceIds != null && resourceIds.Count > 0)
        _logger.LogWarning($"{resourceIds.Count} {type} ResourceIds found!  ResourceIds: {string.Join(", \n", resourceIds)}");
    }

    private async Task RemoveInvalidConfigsFromDb(params object[] args)
    {
      var invalidRecords = new List<string>();
      foreach (var invalidResourceIds in args)
      {
        invalidRecords.AddRange((List<string>)invalidResourceIds);
      }

      foreach (var invalidResourceId in invalidRecords.Distinct())
      {
        await _dependencyInfoManager.RemoveConfig(invalidResourceId);
      }
      _logger.LogWarning($"Removed Resource Ids: {string.Join(", \n", invalidRecords)}");
    }

    public async Task UpdateConfigsInDb(ResourceDependencyInformation config)
    {
      await _dependencyInfoManager.CreateOrReplaceItemInDb(config);
      _logger.LogWarning($"Config Updated in DB, ResourceId: {config.ResourceId}");
    }


    private async Task<HashSet<ResourceDependencyInformation>> FindDependencyConfigs(List<ServiceTag> relatedServiceTags)
    {
      var dependencyConfigList = new HashSet<ResourceDependencyInformation>();
      foreach (var serviceTag in relatedServiceTags)
      {
        foreach (var dependencyInfo in await _dependencyInfoManager.FindByInternalAndThirdPartyTagName(serviceTag))
        {
          dependencyConfigList.Add(dependencyInfo);
        }
      }
      return dependencyConfigList;
    }

    public async Task<bool> IsMandatoryForCurrentSubscription(List<ServiceTag> relatedServiceTags)
    {
      var firstRecordFromDb = await _dependencyInfoManager.GetFirstOrDefault();
      var subscriptionName = await GetSubscriptionNameById(firstRecordFromDb.RequestSubscriptionId);
      if (string.IsNullOrEmpty(subscriptionName))
        throw new ArgumentNullException($"There is no subscription found for the id:{firstRecordFromDb.RequestSubscriptionId}");

      var isAnyMandatoryTagForSubscription = relatedServiceTags.Any(x => x.AllowedSubscriptions.Where(s => s.SubscriptionName == subscriptionName && s.IsMandatory == true).Any());
      return isAnyMandatoryTagForSubscription;
    }

    public async Task<string> GetSubscriptionNameById(string? requestSubscriptionId)
    {
      if (requestSubscriptionId != null)
      {
        var azureSubscription = await _azureSubscriptionPersistenceManager.GetById(requestSubscriptionId);
        return azureSubscription!.Name ?? string.Empty;
      }
      return string.Empty;
    }
  }
}