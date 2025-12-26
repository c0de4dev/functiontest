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
  /// <summary>
  /// IP restriction service implementation for Azure Subscription-based operations.
  /// Processes subscription changes and retrieves related dependency configurations.
  /// </summary>
  public class IpRestrictionServiceForAzureSubscription : BaseIpRestrictionService, IIpRestrictionService<HashSet<AzureSubscription>>
  {
    private readonly IPersistenceManager<ServiceTag> _serviceTagsPersistenceManager;
    private readonly ILogger<IpRestrictionServiceForAzureSubscription> _logger;

    public IpRestrictionServiceForAzureSubscription(
        IPersistenceManager<ServiceTag> serviceTagsPersistenceManager,
        IResourceDependencyInformationPersistenceService dependencyInfoManager,
        IPersistenceManager<AzureSubscription> azureSubscriptionPersistenceManager,
        IResourceGraphExplorerService resourceService,
        ISettingValidator<ResourceDependencyInformation> validator,
        ILogger<IpRestrictionServiceForAzureSubscription> logger)
      : base(dependencyInfoManager, azureSubscriptionPersistenceManager, resourceService, validator, logger)
    {
      _serviceTagsPersistenceManager = serviceTagsPersistenceManager;
      _logger = logger;
    }

    /// <summary>
    /// Retrieves valid dependency configurations based on updated Azure subscriptions.
    /// </summary>
    /// <param name="updatedAzureSubscriptionsModel">The set of updated Azure subscriptions to process.</param>
    /// <returns>A set of valid resource dependency configurations.</returns>
    public async Task<HashSet<ResourceDependencyInformation>> GetValidDependencyConfigs(
        HashSet<AzureSubscription> updatedAzureSubscriptionsModel)
    {
      var subscriptionCount = updatedAzureSubscriptionsModel?.Count ?? 0;

      _logger.LogMethodStart(nameof(GetValidDependencyConfigs));
      _logger.LogGettingValidDependencyConfigs(subscriptionCount);

      var validConfigs = new HashSet<ResourceDependencyInformation>();

      // Check for null or empty subscription model
      if (updatedAzureSubscriptionsModel == null || !updatedAzureSubscriptionsModel.Any())
      {
        _logger.LogAzureSubscriptionsEmpty();
        _logger.LogMethodComplete(nameof(GetValidDependencyConfigs), true);
        return validConfigs;
      }

      try
      {
        using (_logger.BeginAzureSubscriptionOperationScope(nameof(GetValidDependencyConfigs), subscriptionCount))
        {
          // Find related service tags
          var relatedServiceTags = await FindRelatedServiceTags(updatedAzureSubscriptionsModel);
          if (!relatedServiceTags.Any())
          {
            _logger.LogNoRelatedServiceTagsFound();
            _logger.LogMethodComplete(nameof(GetValidDependencyConfigs), true);
            return validConfigs;
          }

          // Find related dependency configurations
          var dependencyConfigList = await FindRelatedDependencyConfigs(relatedServiceTags);
          if (!dependencyConfigList.Any())
          {
            _logger.LogNoDependencyConfigsFound();
            _logger.LogMethodComplete(nameof(GetValidDependencyConfigs), true);
            return validConfigs;
          }

          // Remove invalid resource configurations
          var cleanDpList = await RemoveInvalidResourceConfigs(dependencyConfigList);
          if (!cleanDpList.Any())
          {
            _logger.LogAllConfigsInvalid();
            _logger.LogMethodComplete(nameof(GetValidDependencyConfigs), true);
            return validConfigs;
          }

          // Log the count and resource names for valid configurations
          var resourceNames = string.Join(", ", cleanDpList.Select(x => x.ResourceName ?? "Unknown"));
          _logger.LogValidDependencyConfigsFound(cleanDpList.Count, resourceNames);

          validConfigs = cleanDpList;
        }
        _logger.LogMethodComplete(nameof(GetValidDependencyConfigs), true);
      }
      catch (Exception ex)
      {
        _logger.LogGetValidDependencyConfigsFailed(ex);
        _logger.LogMethodException(ex, nameof(GetValidDependencyConfigs));
        return validConfigs;
      }

      return validConfigs;
    }

    /// <summary>
    /// Finds service tags that are related to the changed Azure subscriptions.
    /// </summary>
    /// <param name="changedModelList">The set of changed Azure subscriptions.</param>
    /// <returns>A list of service tags that reference the changed subscriptions.</returns>
    public async Task<List<ServiceTag>> FindRelatedServiceTags(HashSet<AzureSubscription> changedModelList)
    {
      var subscriptionCount = changedModelList?.Count ?? 0;

      _logger.LogMethodStart(nameof(FindRelatedServiceTags));
      _logger.LogFindingRelatedServiceTags(subscriptionCount);

      // Validate input
      if (changedModelList == null || !changedModelList.Any())
      {
        _logger.LogAzureSubscriptionModelEmpty();
        _logger.LogMethodComplete(nameof(FindRelatedServiceTags), true);
        return new List<ServiceTag>();
      }

      try
      {
        // Extract subscription names from the changed model list
        var subscriptionNames = changedModelList
            .Select(x => x.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        var subscriptionNamesString = string.Join(", ", subscriptionNames);

        using (_logger.BeginServiceTagLookupScope(subscriptionNamesString))
        {
          _logger.LogLookingForServiceTags(subscriptionNamesString);

          // Retrieve all Service Tags from the database
          var allServiceTags = await _serviceTagsPersistenceManager.GetFromDatabase();

          if (allServiceTags == null || !allServiceTags.Any())
          {
            _logger.LogNoServiceTagsInDatabase();
            _logger.LogMethodComplete(nameof(FindRelatedServiceTags), true);
            return new List<ServiceTag>();
          }

          // Filter service tags that reference the updated subscriptions
          var relatedServiceTags = allServiceTags
              .Where(x => x.AllowedSubscriptions != null &&
                         x.AllowedSubscriptions
                           .Select(a => a.SubscriptionName)
                           .Intersect(subscriptionNames)
                           .Any())
              .ToList();
          // Log the results of the filtering process
          if (!relatedServiceTags.Any())
          {
            _logger.LogNoMatchingServiceTagsFound();
          }
          else
          {
            var tagNames = string.Join(", ", relatedServiceTags.Select(tag => tag.Name ?? "Unknown"));
            _logger.LogRelatedServiceTagsFound(relatedServiceTags.Count, tagNames);
          }

          _logger.LogMethodComplete(nameof(FindRelatedServiceTags), true);
          return relatedServiceTags;
        }
      }
      catch (Exception ex)
      {
        _logger.LogFindRelatedServiceTagsFailed(ex);
        _logger.LogMethodException(ex, nameof(FindRelatedServiceTags));
        throw;
      }
    }
  }
}