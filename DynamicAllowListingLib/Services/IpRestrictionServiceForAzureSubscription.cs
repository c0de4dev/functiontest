using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Services
{
  public class IpRestrictionServiceForAzureSubscription : BaseIpRestrictionService, IIpRestrictionService<HashSet<AzureSubscription>>
  {
    private readonly IPersistenceManager<ServiceTag> _serviceTagsPersistenceManager;
    private ILogger<IpRestrictionServiceForAzureSubscription> _logger;
    public IpRestrictionServiceForAzureSubscription(IPersistenceManager<ServiceTag> serviceTagsPersistenceManager,
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

    public async Task<HashSet<ResourceDependencyInformation>> GetValidDependencyConfigs(HashSet<AzureSubscription> updatedAzureSubscriptionsModel)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetValidDependencyConfigs));
      var validConfigs = new HashSet<ResourceDependencyInformation>();
      // Check for null or empty subscription model
      if (updatedAzureSubscriptionsModel == null || !updatedAzureSubscriptionsModel.Any())
      {
        FunctionLogger.MethodInformation(_logger, $"Updated Azure subscriptions model is null or empty.");
        return validConfigs;
      }
      try
      {        
        // Find related service tags
        var relatedServiceTags = await FindRelatedServiceTags(updatedAzureSubscriptionsModel);
        if (!relatedServiceTags.Any())
        {
          FunctionLogger.MethodInformation(_logger, $"No related service tags found for the provided Azure subscriptions.");
          return validConfigs;
        }

        // Find related dependency configurations
        var dependencyConfigList = await FindRelatedDependencyConfigs(relatedServiceTags);
        if (!dependencyConfigList.Any())
        {
          FunctionLogger.MethodInformation(_logger, $"No dependency configurations found for the related service tags.");
          return validConfigs;
        }

        // Remove invalid resource configurations
        var cleanDpList = await RemoveInvalidResourceConfigs(dependencyConfigList);
        if (!cleanDpList.Any())
        {
          FunctionLogger.MethodInformation(_logger, $"All resource configurations were invalid; returning empty list.");
          return validConfigs;
        }
        // Log the count and resource names for valid configurations
        string resourceNames = string.Join(", ", cleanDpList.Select(x => x.ResourceName));
        FunctionLogger.MethodInformation(_logger,
            $"Found {cleanDpList.Count} valid resource configurations. Resource Names: {resourceNames}");

        // Return the cleaned dependency list
        validConfigs = cleanDpList;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, "An error occurred while processing valid dependency configs. Returning an empty list.");
        return validConfigs;
      }
      return validConfigs;
    }

    public async Task<List<ServiceTag>> FindRelatedServiceTags(HashSet<AzureSubscription> changedModelList)
    {
      FunctionLogger.MethodStart(_logger, nameof(FindRelatedServiceTags));
      // Step 1: Validate input
      if (changedModelList == null || !changedModelList.Any())
      {
        FunctionLogger.MethodWarning(_logger, "The changed AzureSubscription model list is null or empty.");
        return new List<ServiceTag>();
      }
      try
      {
        // Step 2: Extract subscription names from the changed model list
        var subscriptionNames = changedModelList.Select(x => x.Name).ToList();

        // Log the subscription names being processed
        FunctionLogger.MethodInformation(_logger,
            $"Looking for Service Tags referencing Azure Subscriptions: {string.Join(", ", subscriptionNames)}");

        // Step 3: Retrieve all Service Tags from the database
        var allServiceTags = await _serviceTagsPersistenceManager.GetFromDatabase();

        if (allServiceTags == null || !allServiceTags.Any())
        {
          FunctionLogger.MethodWarning(_logger, "No Service Tags found in the database.");
          return new List<ServiceTag>();
        }
        // Filter service tags that reference the updated subscriptions
        var relatedServiceTags = allServiceTags
           .Where(x => x.AllowedSubscriptions
           .Select(a => a.SubscriptionName)
           .Intersect(subscriptionNames).Any())
           .ToList();

        // Step 5: Log the results of the filtering process
        if (!relatedServiceTags.Any())
        {
          FunctionLogger.MethodWarning(_logger,
              "No Service Tags found referencing the updated Azure Subscriptions.");
        }
        else
        {
          FunctionLogger.MethodInformation(_logger,
              $"{relatedServiceTags.Count} Service Tags found referencing updated Azure Subscriptions: {string.Join(", ", relatedServiceTags.Select(tag => tag.Name))}");
        }
        return relatedServiceTags;
      }
      catch (Exception ex)
      {        
        // Log any exceptions encountered during execution
        FunctionLogger.MethodException(_logger, ex, "An error occurred while finding related service tags.");
        throw;
      }
    }
  }
}