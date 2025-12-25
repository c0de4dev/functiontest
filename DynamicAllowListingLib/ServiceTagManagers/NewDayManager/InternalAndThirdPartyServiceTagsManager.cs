using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DynamicAllowListingLib.ServiceTagManagers.NewDayManager
{
  public interface INewDayServiceTagManager : IServiceTagManager
  {
    public Task<List<ServiceTag>> GetAllServiceTagsBySubscriptionID(string subscriptionId, bool isMandatory);
    public Task<List<ServiceTag>> GetAllServiceTagsBySubscriptionID(string subscriptionId);
  }

  public class InternalAndThirdPartyServiceTagsManager : INewDayServiceTagManager
  {
    private readonly AsyncLazy<InternalAndThirdPartyServiceTagSetting> _settings;
    private readonly ISettingLoader _settingLoader;
    private readonly IInternalAndThirdPartyServiceTagPersistenceManager _internalAndThirdPartyServiceTagPersistenceManager;
    private readonly ILogger<InternalAndThirdPartyServiceTagsManager> _logger;
    public ManagerType SupportedManager => ManagerType.NewDay;

    public InternalAndThirdPartyServiceTagsManager(ISettingLoader settingLoader, 
      IInternalAndThirdPartyServiceTagPersistenceManager internalAndThirdPartyServiceTagPersistenceManager, ILogger<InternalAndThirdPartyServiceTagsManager> logger)
    {
      _internalAndThirdPartyServiceTagPersistenceManager = internalAndThirdPartyServiceTagPersistenceManager;
      _settingLoader = settingLoader;
      _settings = new AsyncLazy<InternalAndThirdPartyServiceTagSetting>(async () => await GetSettings());
      _logger = logger;
    }

    private async Task<InternalAndThirdPartyServiceTagSetting> GetSettings()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetSettings));
      InternalAndThirdPartyServiceTagSetting setting = new InternalAndThirdPartyServiceTagSetting();
      try
      {
        FunctionLogger.MethodInformation(_logger, "Attempting to get settings from the database.");
        var settings = await _internalAndThirdPartyServiceTagPersistenceManager.GetFromDatabase();
        // Check if the retrieved settings are empty
        if (settings.AzureSubscriptions.Count <= 0 || settings.ServiceTags.Count <= 0)
        {
          string infoMessage = "Database settings are empty or incomplete. Loading settings from file.";
          FunctionLogger.MethodInformation(_logger, infoMessage);

          // Attempt to load settings from the file if the database has no data
          settings = await _settingLoader.LoadSettingsFromFile<InternalAndThirdPartyServiceTagSetting>(
            InternalAndThirdPartyServiceTagSettingFileHelper.GetFilePath());

          // Log the action of loading settings from the file
          FunctionLogger.MethodInformation(_logger, "Settings loaded successfully from file.");
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, $"Settings successfully retrieved from database. ServiceTags count: {settings.ServiceTags.Count}, AzureSubscriptions count: {settings.AzureSubscriptions.Count}.");
        }
        return settings;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return setting;
      }
    }

    public async Task<List<ServiceTag>> GetAllServiceTagsBySubscriptionID(string subscriptionId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAllServiceTagsBySubscriptionID));
      List<ServiceTag> emptyServiceTags = new List<ServiceTag>();
      try
      {
        // Validate if subscriptionId is provided
        if (string.IsNullOrEmpty(subscriptionId))
        {
          string errorMessage = "Empty or null Subscription ID provided.";
          FunctionLogger.MethodWarning(_logger, errorMessage);
          return emptyServiceTags;
        }
        // Logging the attempt to fetch service tags by subscription ID
        FunctionLogger.MethodInformation(_logger, $"Fetching service tags for Subscription ID: {subscriptionId}");

        // Fetch the settings and retrieve the relevant data
        var data = await _settings.Value;
        var subscription = data.AzureSubscriptions.First(x => x.Id == subscriptionId);
        if (subscription == null)
        {
          // Logging the situation where the subscription was not found
          string errorMessage = $"Subscription with ID '{subscriptionId}' not found.";
          FunctionLogger.MethodWarning(_logger, errorMessage);
          return emptyServiceTags;
        }
        // Adding subscriptionName to the logging context
        var subscriptionName = subscription.Name;
        // Retrieve the service tags associated with the provided subscription
        var serviceTags = data.ServiceTags
            .Where(x => x.AllowedSubscriptions.Any(s => s.SubscriptionName == subscriptionName))
            .ToList();

        FunctionLogger.MethodInformation(_logger, $"Found {serviceTags.Count} service tags for subscription '{subscriptionName}'.");
        return serviceTags;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return emptyServiceTags;
      }
    }

    public async Task<List<ServiceTag>> GetAllServiceTagsBySubscriptionID(string subscriptionId, bool isMandatory)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAllServiceTagsBySubscriptionID));
      List<ServiceTag> emptyServiceTags = new List<ServiceTag>();
      try
      {        
        // Validate if the subscriptionId is provided
        if (string.IsNullOrEmpty(subscriptionId))
        {
          string warningMessage = "Empty or null Subscription ID provided.";
          FunctionLogger.MethodWarning(_logger, warningMessage);
          return emptyServiceTags;
        }
        FunctionLogger.MethodInformation(_logger, $"Fetching service tags by subscription ID: {subscriptionId} and mandatory: {isMandatory}");
        // Fetch the settings and retrieve the relevant data
        var data = await _settings.Value;
        var subscription = data.AzureSubscriptions.First(x => x.Id == subscriptionId);
        // Check if subscription exists
        if (subscription == null)
        {
          string warningMessage = $"Subscription with ID '{subscriptionId}' not found.";
          FunctionLogger.MethodWarning(_logger, warningMessage);
          return emptyServiceTags;
        }
        // Adding subscriptionName to the logging context
        var subscriptionName = subscription.Name;
        // Retrieve the service tags associated with the provided subscription and mandatory flag
        var serviceTags = data.ServiceTags
            .Where(x => x.AllowedSubscriptions.Any(s => s.SubscriptionName == subscriptionName && s.IsMandatory == isMandatory))
            .ToList();

        // Log the result of the query
        FunctionLogger.MethodInformation(_logger, $"Found {serviceTags.Count} service tags for subscription '{subscriptionName}' with Mandatory flag '{isMandatory}'.");
        return serviceTags;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return emptyServiceTags;
      }
    }

    public async Task<HashSet<IpSecurityRestrictionRule>> GenerateRulesByName(string subscriptionId, string[] serviceTags, bool includeMandatoryRulesForSubscription = true)
    {
      FunctionLogger.MethodStart(_logger, nameof(GenerateRulesByName));
      var results = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        // Early return if subscriptionId is invalid or empty
        if (string.IsNullOrEmpty(subscriptionId) || !Guid.TryParse(subscriptionId, out _))
        {
          string warningMessage = "Invalid or empty subscription ID provided. Returning empty rules set.";
          FunctionLogger.MethodWarning(_logger, warningMessage);
          return results; // Return empty HashSet directly
        }
        // Fetch settings data asynchronously
        var data = await _settings.Value;

        // Retrieve subscription based on the subscriptionId
        var subscription = data.AzureSubscriptions.FirstOrDefault(x => x.Id == subscriptionId);
        if (subscription == null)
        {
          string warningMessage = $"Subscription with ID '{subscriptionId}' not found. Returning empty rules set.";
          FunctionLogger.MethodWarning(_logger, warningMessage);
          return results; // Return empty HashSet directly
        }

        var subscriptionName = subscription.Name;
        // Loop through service tags to generate rules
        foreach (var tag in data.ServiceTags)
        {  
          // Check if the tag should be included based on serviceTags array and mandatory rules
          if ((serviceTags != null && serviceTags.Length > 0
           && serviceTags.Contains(tag.Name, StringComparer.OrdinalIgnoreCase)
           && tag.AllowedSubscriptions.Any(x => x.SubscriptionName == subscriptionName))  // check if service tag has definition for the subscription
           || (includeMandatoryRulesForSubscription &&
              tag.AllowedSubscriptions.Any(x => x.SubscriptionName == subscriptionName && x.IsMandatory)))
          {
            // Log the tag being processed
            FunctionLogger.MethodInformation(_logger, $"Generating IP and Subnet rules for Service Tag: {tag.Name} for Subscription: {subscriptionName}");

            var ipAddressRules = GetIpAddressRules(tag);
            results.UnionWith(ipAddressRules);
            var subnetRules = GetSubnetRules(tag);
            results.UnionWith(subnetRules);
          }
        }
        FunctionLogger.MethodInformation(_logger, $"Generated {results.Count} rules for SubscriptionId: {subscriptionId}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return results;
    }

    private HashSet<IpSecurityRestrictionRule> GetIpAddressRules(ServiceTag tag)
    {
      //FunctionLogger.MethodStart(_logger, nameof(GetIpAddressRules));
      var results = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        // Ensure that AddressPrefixes is not null or empty before processing
        if (tag.AddressPrefixes != null && tag.AddressPrefixes.Any())
        {
          int i = 0;
          foreach (var address in tag.AddressPrefixes)
          {
            if (!string.IsNullOrEmpty(tag.Name) && !string.IsNullOrEmpty(address))
            {
              var ruleName = StringHelper.Truncate(tag.Name) + "." + i;
              results.Add(new IpSecurityRestrictionRule
              {
                Name = ruleName,
                IpAddress = address
              });
            }
            i++;
          }
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, $"No Address Prefixes found for ServiceTag: {tag.Name}");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Return the results HashSet
      return results;
    }

    private HashSet<IpSecurityRestrictionRule> GetSubnetRules(ServiceTag tag)
    {
      //FunctionLogger.MethodStart(_logger, nameof(GetSubnetRules));
      var results = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        if (tag.SubnetIds != null && tag.SubnetIds.Any())
        {
          int i = 0;
          foreach (var subnetId in tag.SubnetIds)
          {
            if (!string.IsNullOrEmpty(tag.Name) && !string.IsNullOrEmpty(subnetId))
            {
              var ruleName = StringHelper.Truncate(tag.Name) + "." + i;
              results.Add(new IpSecurityRestrictionRule
              {
                Name = ruleName,
                VnetSubnetResourceId = subnetId
              });
            }
            i++;
          }
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, $"No Subnet IDs found for ServiceTag: {tag.Name}");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Return the results HashSet
      return results;
    }

    public async Task<bool> IsServiceTagExists(string serviceTagName)
    {
      FunctionLogger.MethodStart(_logger, nameof(IsServiceTagExists));
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Checking if service tag : {serviceTagName} exists.");
        var data = await _settings.Value;
        if (data == null)
        {
          throw new InvalidOperationException("Service tag data is not available.");
        }
        var tagExists = data.ServiceTags.Any(x => x.Name == serviceTagName);

        FunctionLogger.MethodInformation(_logger, $"Service tag existence check result: {tagExists}");
        return tagExists;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return false;
    }
    public Task<bool> IsServiceTagExists(string serviceTagName, string requestedSubscriptionId)
    {
      throw new NotImplementedException();
    }
  }
}