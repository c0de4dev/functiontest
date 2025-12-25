using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Services
{
  public class IpRestrictionServiceForServiceTag : BaseIpRestrictionService, IIpRestrictionService<HashSet<ServiceTag>>
  {
    private readonly IPersistenceManager<ServiceTag> _serviceTagsPersistenceManager;
    private readonly ILogger<IpRestrictionServiceForServiceTag> _logger;

    public IpRestrictionServiceForServiceTag(
        IResourceDependencyInformationPersistenceService dependencyInfoManager,
        IPersistenceManager<AzureSubscription> azureSubscriptionPersistenceManager,
        IPersistenceManager<ServiceTag> serviceTagsPersistenceManager,
        IResourceGraphExplorerService resourceService,
        ISettingValidator<ResourceDependencyInformation> validator,
        ILogger<IpRestrictionServiceForServiceTag> logger)
      : base(dependencyInfoManager, azureSubscriptionPersistenceManager, resourceService, validator, logger)
    {
      _serviceTagsPersistenceManager = serviceTagsPersistenceManager;
      _logger = logger;
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetValidDependencyConfigs(HashSet<ServiceTag> updatedServiceTagModels)
    {
      var stopwatch = Stopwatch.StartNew();
      _logger.LogMethodStart(nameof(GetValidDependencyConfigs));

      var modifiedDpConfiglst = new List<ResourceDependencyInformation>();
      var configList = new HashSet<ResourceDependencyInformation>();

      // Check if the input is valid
      if (updatedServiceTagModels == null || !updatedServiceTagModels.Any())
      {
        _logger.LogNoUpdatedServiceTagsProvided();
        return configList;
      }

      try
      {
        using (_logger.BeginServiceTagScope(nameof(GetValidDependencyConfigs), updatedServiceTagModels.Count))
        {
          // Step 1: Retrieve related dependency configurations
          _logger.LogFetchingRelatedDependencyConfigs(updatedServiceTagModels.Count);
          var dependencyConfigList = await FindRelatedDependencyConfigs(updatedServiceTagModels.ToList());

          if (!dependencyConfigList.Any())
          {
            _logger.LogNoRelatedDependencyConfigsFound();
            stopwatch.Stop();
            _logger.LogGetValidDependencyConfigsComplete(0, stopwatch.ElapsedMilliseconds);
            return configList;
          }

          // Step 2: Remove invalid resource configurations
          _logger.LogValidatingDependencyConfigs(dependencyConfigList.Count);
          var cleanDpList = await RemoveInvalidResourceConfigs(dependencyConfigList);

          // Step 3: Handle deleted service tags
          var deletedServiceTags = GetDeletedServiceTags(updatedServiceTagModels);
          _logger.LogRemovingDeletedServiceTagsFromConfigs(deletedServiceTags.Count, cleanDpList.Count);

          var updatedDpConfigList = await RemoveDeletedServiceTagsFromConfig(deletedServiceTags, cleanDpList);

          // Step 4: Persist changes to the database
          if (deletedServiceTags.Any())
          {
            _logger.LogRemovingDeletedServiceTagsFromDb(deletedServiceTags.Count);
            await _serviceTagsPersistenceManager.RemoveItemsFromDatabase(deletedServiceTags);
          }

          modifiedDpConfiglst = updatedDpConfigList;

          // Log the details of the modified configs
          if (updatedDpConfigList.Any())
          {
            var resourceNames = string.Join(", ", updatedDpConfigList.Select(x => x.ResourceName));
            _logger.LogUpdatingResources(updatedDpConfigList.Count, resourceNames);
          }

          stopwatch.Stop();
          _logger.LogGetValidDependencyConfigsComplete(modifiedDpConfiglst.Count, stopwatch.ElapsedMilliseconds);
        }
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        _logger.LogMethodException(ex, nameof(GetValidDependencyConfigs), stopwatch.ElapsedMilliseconds);
      }

      return new HashSet<ResourceDependencyInformation>(modifiedDpConfiglst);
    }

    public List<ServiceTag> GetDeletedServiceTags(HashSet<ServiceTag> updatedServiceTagModels)
    {
      var stopwatch = Stopwatch.StartNew();
      _logger.LogMethodStart(nameof(GetDeletedServiceTags));

      // Null check for safety
      if (updatedServiceTagModels == null || !updatedServiceTagModels.Any())
      {
        _logger.LogNoServiceTagsProvided();
        return new List<ServiceTag>();
      }

      _logger.LogIdentifyingDeletedServiceTags(updatedServiceTagModels.Count);

      // Filter deleted service tags
      var deletedServiceTags = updatedServiceTagModels
          .Where(x => x.IsDeleted == true)
          .ToList();

      stopwatch.Stop();

      // Log details of deleted tags
      if (deletedServiceTags.Any())
      {
        var deletedServiceTagIds = string.Join(", ", deletedServiceTags.Select(x => x.Id));
        _logger.LogDeletedServiceTagsIdentified(deletedServiceTags.Count, deletedServiceTagIds);
      }
      else
      {
        _logger.LogNoDeletedServiceTagsFound();
      }

      _logger.LogMethodComplete(nameof(GetDeletedServiceTags), stopwatch.ElapsedMilliseconds, true);

      return deletedServiceTags;
    }

    public async Task<List<ResourceDependencyInformation>> RemoveDeletedServiceTagsFromConfig(
        List<ServiceTag> deletedServiceTags,
        HashSet<ResourceDependencyInformation> dependencyConfigList)
    {
      var stopwatch = Stopwatch.StartNew();
      _logger.LogMethodStart(nameof(RemoveDeletedServiceTagsFromConfig));

      // Check for empty or null inputs
      if (deletedServiceTags == null || !deletedServiceTags.Any())
      {
        _logger.LogNoDeletedServiceTagsProvided();
        return dependencyConfigList?.ToList() ?? new List<ResourceDependencyInformation>();
      }

      if (dependencyConfigList == null || !dependencyConfigList.Any())
      {
        _logger.LogNoDependencyConfigsProvided();
        return new List<ResourceDependencyInformation>();
      }

      var deletedServiceTagNames = deletedServiceTags
          .Select(x => x.Name)
          .Where(name => !string.IsNullOrEmpty(name))
          .ToHashSet();

      var modifiedConfigs = dependencyConfigList.ToList();
      var modifiedCount = 0;

      using (_logger.BeginDeletedTagScope(deletedServiceTags.Count, dependencyConfigList.Count))
      {
        foreach (var tagName in deletedServiceTagNames)
        {
          var affectedConfigs = modifiedConfigs.Where(p =>
              p.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags != null &&
              p.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags.Any(x => x == tagName))
            .ToList();

          foreach (var config in affectedConfigs)
          {
            var currentTags = config.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags;
            if (currentTags == null) continue;

            var newTagList = currentTags.Where(x => x != tagName).ToArray();

            if (config.AllowInbound != null &&
                config.AllowInbound.SecurityRestrictions != null)
            {
              config.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags = newTagList;

              _logger.LogRemovedTagFromResourceConfig(tagName!, config.ResourceId ?? "Unknown");
              modifiedCount++;

              // Update config in DB
              await UpdateConfigsInDb(config);
            }
          }
        }

        stopwatch.Stop();
        _logger.LogRemoveDeletedServiceTagsComplete(modifiedCount, stopwatch.ElapsedMilliseconds);
        _logger.LogMethodComplete(nameof(RemoveDeletedServiceTagsFromConfig), stopwatch.ElapsedMilliseconds, true);
      }

      return modifiedConfigs;
    }
  }
}