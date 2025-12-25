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
  public class IpRestrictionServiceForServiceTag : BaseIpRestrictionService, IIpRestrictionService<HashSet<ServiceTag>>
  {
    private readonly IPersistenceManager<ServiceTag> _serviceTagsPersistenceManager;
    private readonly ILogger<IpRestrictionServiceForServiceTag> _logger;

    public IpRestrictionServiceForServiceTag(IResourceDependencyInformationPersistenceService dependencyInfoManager,
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
      FunctionLogger.MethodStart(_logger, nameof(GetValidDependencyConfigs));
      var modifiedDpConfiglst = new List<ResourceDependencyInformation>();
      HashSet<ResourceDependencyInformation> configList = new HashSet<ResourceDependencyInformation>();
      // Check if the input is valid
      if (updatedServiceTagModels == null || !updatedServiceTagModels.Any())
      {
        FunctionLogger.MethodWarning(_logger, "No updated service tags provided. Returning an empty configuration list.");
        return configList;
      }
      try
      { 
        // Step 1: Retrieve related dependency configurations
        FunctionLogger.MethodInformation(_logger, $"Fetching related dependency configurations for {updatedServiceTagModels.Count} updated service tags.");
        var dependencyConfigList = await FindRelatedDependencyConfigs(updatedServiceTagModels.ToList());
        if (!dependencyConfigList.Any())
        {
          FunctionLogger.MethodInformation(_logger, "No related dependency configs found for the updated service tags. Returning an empty configuration list.");
          return configList;
        }
        // Step 2: Remove invalid resource configurations
        FunctionLogger.MethodInformation(_logger, $"Validating {dependencyConfigList.Count} dependency configurations.");
        var cleanDpList = await RemoveInvalidResourceConfigs(dependencyConfigList);

        // Step 3: Handle deleted service tags
        var deletedServiceTags = GetDeletedServiceTags(updatedServiceTagModels);
        FunctionLogger.MethodInformation(_logger, $"Identified {deletedServiceTags.Count} deleted service tags.");

        FunctionLogger.MethodInformation(_logger, "Removing deleted service tags from the configuration list.");
        var updatedDpConfigList = await RemoveDeletedServiceTagsFromConfig(deletedServiceTags, cleanDpList);
        
        // Step 4: Persist changes to the database
        FunctionLogger.MethodInformation(_logger, $"Removing {deletedServiceTags.Count} deleted service tags from the database.");
        await _serviceTagsPersistenceManager.RemoveItemsFromDatabase(deletedServiceTags);

        modifiedDpConfiglst = updatedDpConfigList;

        // Log the details of the modified configs in a structured way
        string resourceNames = string.Join(", ", updatedDpConfigList.Select(x => x.ResourceName));
        FunctionLogger.MethodInformation(_logger, $"Updating {updatedDpConfigList.Count} resources. Resource Names: {resourceNames}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, "An error occurred in GetValidDependencyConfigs");
      }
      return new HashSet<ResourceDependencyInformation>(modifiedDpConfiglst);
    }

    public List<ServiceTag> GetDeletedServiceTags(HashSet<ServiceTag> updatedServiceTagModels)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetDeletedServiceTags));
      // Null check for safety
      if (updatedServiceTagModels == null || !updatedServiceTagModels.Any())
      {
        FunctionLogger.MethodInformation(_logger, "No service tags provided or empty set. Returning an empty list.");
        return new List<ServiceTag>();
      }
      // Filter deleted service tags
      var deletedServiceTags = updatedServiceTagModels.Where(x => x.IsDeleted == true).ToList();
      // Log details of deleted tags
      if (deletedServiceTags.Any())
      {
        string deletedServiceTagNames = string.Join(", ", deletedServiceTags.Select(x => x.Id));
        FunctionLogger.MethodInformation(_logger, $"Found {deletedServiceTags.Count} deleted service tags: {deletedServiceTagNames}");
      }
      else
      {
        FunctionLogger.MethodInformation(_logger, "No deleted service tags found.");
      }
      return deletedServiceTags;
    }

    public async Task<List<ResourceDependencyInformation>> RemoveDeletedServiceTagsFromConfig(List<ServiceTag> deletedServiceTags, HashSet<ResourceDependencyInformation> dependencyConfigList)
    {
      FunctionLogger.MethodStart(_logger, nameof(RemoveDeletedServiceTagsFromConfig));
      // Check for empty or null inputs
      if (deletedServiceTags == null || !deletedServiceTags.Any())
      {
        FunctionLogger.MethodInformation(_logger, "No deleted service tags provided. Returning unmodified config list.");
        return dependencyConfigList.ToList();
      }

      if (dependencyConfigList == null || !dependencyConfigList.Any())
      {
        FunctionLogger.MethodInformation(_logger, "No dependency configurations provided. Returning an empty list.");
        return new List<ResourceDependencyInformation>();
      }

      var deletedServiceTagNames = deletedServiceTags.Select(x => x.Name);
      var modifiedConfigs = dependencyConfigList.ToList();


      foreach (var tagName in deletedServiceTagNames)
      {
        foreach (var config in modifiedConfigs.Where(p => p.AllowInbound != null &&
                                                         p.AllowInbound.SecurityRestrictions != null &&
                                                         p.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags != null &&
                                                         p.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags.Any(x => x == tagName)))
        {
          var newTagList = config.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags!.Where(x => x != tagName).ToArray();
          if (config.AllowInbound != null &&
              config.AllowInbound.SecurityRestrictions != null &&
              config.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags != null)
          {
            config.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags = newTagList;

            //_logger.LogInformation($"Removed tag {tagName} from resource: {config.ResourceId}");
            FunctionLogger.MethodInformation(_logger, $"Removed tag {tagName} from resource: {config.ResourceId}");
            
            //update config in DB
            await UpdateConfigsInDb(config);       
          }
        }
      }
      FunctionLogger.MethodInformation(_logger, "RemoveDeletedServiceTagsFromConfig completed successfully.");
      return modifiedConfigs;
    }
  }
}