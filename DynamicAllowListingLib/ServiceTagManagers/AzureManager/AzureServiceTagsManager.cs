using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.ServiceTagManagers.AzureManager
{
  public class AzureServiceTagsManager : IServiceTagManager
  {
    private readonly ILogger<AzureServiceTagsManager> _logger;
    private readonly IAzureServiceTagsJsonHelper _azureServiceTagsJsonHelper;
    private AzureServiceTags? _azureServiceTags;
    public ManagerType SupportedManager
    {
      get
      {
        return ManagerType.Azure;
      }
    }
    public AzureServiceTagsManager(IAzureServiceTagsJsonHelper azureServiceTagsJsonHelper,ILogger<AzureServiceTagsManager> logger)
    {
      _logger = logger;
      _azureServiceTagsJsonHelper = azureServiceTagsJsonHelper;
    }

    public async Task<bool> IsServiceTagExists(string serviceTagName, string requestedSubscriptionId)
    {
      FunctionLogger.MethodStart(_logger, nameof(IsServiceTagExists));

      FunctionLogger.MethodInformation(_logger, $"Checking for existence of Service Tag {serviceTagName} exist for Subscription ID {requestedSubscriptionId}.");

      var azureServiceTags = await GetAzureServiceTags(requestedSubscriptionId);

      bool serviceTagExists = false;
      if (azureServiceTags != null)
      {
        serviceTagExists = azureServiceTags.Values.Any(azureServiceTag => azureServiceTag.Name.Equals(serviceTagName));
        if (serviceTagExists == true)
        {
          FunctionLogger.MethodInformation(_logger, $"Service Tag {serviceTagName} exist for Subscription ID {requestedSubscriptionId}.");
          return serviceTagExists;
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, $"Service Tag {serviceTagName} does not exist for Subscription ID {requestedSubscriptionId}.");
        }
      }
      return serviceTagExists;
    }

    private async Task<AzureServiceTags?> GetAzureServiceTags(string requestedSubscriptionId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureServiceTags));
      if (_azureServiceTags == null)
      {
        string azureServiceTagsJson = await _azureServiceTagsJsonHelper.GetAzureServiceTagsJson(requestedSubscriptionId);
        if (string.IsNullOrEmpty(azureServiceTagsJson))
        {
          FunctionLogger.MethodInformation(_logger, $"Failed to retrieve Azure Service Tags JSON for Subscription ID: {requestedSubscriptionId}. Response was null or empty.");
          //throw new Exception("AzureServiceTags JSON could not be obtained.");
        }
        _azureServiceTags = JsonConvert.DeserializeObject<AzureServiceTags>(azureServiceTagsJson);
        if (_azureServiceTags == null)
        {
          FunctionLogger.MethodWarning(_logger, $"Deserialization of Azure Service Tags failed for Subscription ID: {requestedSubscriptionId}.");
          //throw new Exception("AzureServiceTags could not be obtained.");
        }
        FunctionLogger.MethodInformation(_logger, $"Successfully retrieved and deserialized Azure Service Tags for Subscription ID: {requestedSubscriptionId}.");

        return _azureServiceTags;
      }
      else
      {
        FunctionLogger.MethodInformation(_logger, $"Using cached Azure Service Tags for Subscription ID: {requestedSubscriptionId}.");
        return _azureServiceTags;
      }
    }

    public virtual async Task<HashSet<IpSecurityRestrictionRule>> GenerateRulesByName(string subscriptionId, string[] serviceTags,
      bool includeMandatoryRulesForSubscription = true)
    {
      FunctionLogger.MethodStart(_logger, nameof(GenerateRulesByName));
      // Retrieve the rules based on the provided service tags and subscription ID
      var rules = await GetAzureServiceTagRules(serviceTags, subscriptionId);
      _logger.LogInformation("Generated {RuleCount} rules for Subscription ID '{SubscriptionId}'",
                              rules.Count,
                              subscriptionId);
      return rules;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetAzureServiceTagRules(string[] serviceTags, string requestedSubscriptionId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureServiceTagRules));
      // Validate inputs
      if (serviceTags == null || serviceTags.Length == 0)
      {
        FunctionLogger.MethodWarning(_logger, $"Invalid input: Service tags are null or empty for Subscription ID: { requestedSubscriptionId}");
        Exception ex =  new ArgumentException("Service tags must not be null or empty.", nameof(serviceTags));
        throw ex;
      }
      if (string.IsNullOrEmpty(requestedSubscriptionId))
      {
        FunctionLogger.MethodWarning(_logger, $"Invalid input: Subscription ID is null or empty.");
        //throw new ArgumentException("Subscription ID must not be null or empty.", nameof(requestedSubscriptionId));
      }

      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        var nonExistentServiceTags = new List<string>();

        FunctionLogger.MethodInformation(_logger, $"Fetching Azure Service Tags for Subscription ID: {requestedSubscriptionId}");
        var azureServiceTags = await GetAzureServiceTags(requestedSubscriptionId);
        FunctionLogger.MethodInformation(_logger, $"Retrieved {azureServiceTags?.Values.Count} Azure Service Tags for Subscription ID: {requestedSubscriptionId}");

        if (azureServiceTags != null)
        {
          foreach (var serviceTag in serviceTags)
          {
            FunctionLogger.MethodInformation(_logger, $"Processing Service Tag: {serviceTag}");
            bool serviceTagFound = false;
            foreach (var azureServiceTag in azureServiceTags.Values)
            {
              if (!azureServiceTag.Name.Equals(serviceTag, StringComparison.OrdinalIgnoreCase))
              {
                continue;
              }
              serviceTagFound = true;

              // Fetch rules for the service tag
              var ipSecRules = GetAzureServiceTagRules(azureServiceTag);
              ipSecurityRestrictionRules.UnionWith(ipSecRules);

              FunctionLogger.MethodInformation(_logger, $"Found {ipSecRules.Count} rules for Service Tag: {serviceTag}.");
            }
            if (!serviceTagFound)
            {
              FunctionLogger.MethodInformation(_logger, $"Service Tag: '{serviceTag}' not found in Azure Service Tags.");
              nonExistentServiceTags.Add(serviceTag);
            }
          }
          if (nonExistentServiceTags.Count > 0)
          {
            FunctionLogger.MethodInformation(_logger, $"The following Azure Service Tags were not found: {string.Join(", ", nonExistentServiceTags)}, Subscription ID:{requestedSubscriptionId} .");
            throw new AzureServiceTagNotFoundException(nonExistentServiceTags);
          }
        }
        FunctionLogger.MethodInformation(_logger, $"Successfully retrieved {ipSecurityRestrictionRules.Count} IP Security Restriction Rules for Subscription ID: {requestedSubscriptionId}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, $"An unexpected error occurred while processing service tags for Subscription ID: {requestedSubscriptionId}.");
        throw; // Rethrow for higher-level handling
      }
      return ipSecurityRestrictionRules;
    }
     
    internal HashSet<IpSecurityRestrictionRule> GetAzureServiceTagRules(AzureServiceTags.AzureServiceTag azureServiceTag)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAzureServiceTagRules));
      // Validate input
      if (azureServiceTag == null)
      {
        FunctionLogger.MethodWarning(_logger, $"Invalid input: Azure Service Tag is null.");
        Exception ex = new ArgumentNullException(nameof(azureServiceTag), "Azure Service Tag must not be null.");
        throw ex;
      }

      FunctionLogger.MethodInformation(_logger, $"Generating security restriction rules for Azure Service Tag: {azureServiceTag.Name}.");

      var secRules = new HashSet<IpSecurityRestrictionRule>();

      // Check if the service tag contains IPv4 address prefixes
      if (azureServiceTag.Properties?.AddressPrefixesIpV4 == null || !azureServiceTag.Properties.AddressPrefixesIpV4.Any())
      {
        FunctionLogger.MethodWarning(_logger, $"No IP Address Prefixes found for Azure Service Tag: {azureServiceTag.Name}.");
        return secRules; // Return empty set
      }
      // Generate security restriction rules
      int index = 0;
      foreach (var ipAddress in azureServiceTag.Properties.AddressPrefixesIpV4)
      {
        // Create a new rule
        var rule = new IpSecurityRestrictionRule()
        {
          Name = StringHelper.Truncate(azureServiceTag.Name) + index,
          IpAddress = ipAddress,
          Action = "Allow",
          Priority = 500
        };
        // Add rule to the set
        if(secRules.Add(rule))
        {
          FunctionLogger.MethodInformation(_logger, $"Created rule: {rule.Name}, IP Address: {rule.IpAddress}, Action: {rule.Action}, Priority: {rule.Priority}");
        }
        else
        {
          FunctionLogger.MethodWarning(_logger, $"Duplicate rule detected and ignored: {rule.Name}, IP Address: {rule.IpAddress}.");
        }
        index++;
      }
      FunctionLogger.MethodInformation(_logger, $"Successfully generated {secRules.Count} security restriction rules for Azure Service Tag: {azureServiceTag.Name}.");
      return secRules;
    }


    public Task<bool> IsServiceTagExists(string serviceTagName)
    {
      throw new NotImplementedException();
    }
  }

  public class AzureWebServiceTagManager : AzureServiceTagsManager, IServiceTagManager
  {
    private readonly ILogger _logger;
    public AzureWebServiceTagManager(IAzureServiceTagsJsonHelper azureServiceTagsJsonHelper,
      ILogger<AzureServiceTagsManager> logger): base(azureServiceTagsJsonHelper, logger)
    { 
      _logger = logger;
    }
    public new ManagerType SupportedManager => ManagerType.AzureWeb;

    public override Task<HashSet<IpSecurityRestrictionRule>> GenerateRulesByName(string subscriptionId, string[] serviceTags,
      bool includeMandatoryRulesForSubscription = true)
    {
      var secRules = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        // Log method start
        FunctionLogger.MethodStart(_logger, nameof(GenerateRulesByName));

        // Validate inputs
        /*
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
          string errorMessage = "Subscription ID cannot be null, empty, or whitespace.";
          FunctionLogger.MethodWarning(_logger, errorMessage);
          //throw new ArgumentException(errorMessage, nameof(subscriptionId));
        }
        */
        if (serviceTags == null || serviceTags.Length == 0)
        {
          FunctionLogger.MethodInformation(_logger,
              $"No service tags provided for SubscriptionID: {subscriptionId}. Returning an empty rule set.");
          return Task.FromResult(secRules);
        }

        FunctionLogger.MethodInformation(_logger,
          $"Generating security restriction rules for Subscription ID: {subscriptionId} with Service Tags: {string.Join(", ", serviceTags)}");

        // Generate rules for each service tag
        foreach (var serviceTag in serviceTags)
        {
          if (string.IsNullOrWhiteSpace(serviceTag))
          {
            FunctionLogger.MethodWarning(_logger,
                $"Encountered a null or empty service tag for Subscription ID: {subscriptionId}. Skipping.");
            continue;
          }

          var rule = new IpSecurityRestrictionRule
          {
            Name = serviceTag,
            IpAddress = serviceTag, // This might need to be replaced with actual logic to retrieve the IPs for the tag
            Action = "Allow",
            Priority = 500,
            Tag = "ServiceTag"
          };

          if (!secRules.Add(rule))
          {
            FunctionLogger.MethodWarning(_logger,
                $"Duplicate rule detected for Service Tag: {serviceTag}. Rule was not added again.");
          }
          else
          {
            FunctionLogger.MethodInformation(_logger,
                $"Created rule: {rule.Name}, IP Address: {rule.IpAddress}, Action: {rule.Action}, Priority: {rule.Priority}, Tag: {rule.Tag}");
          }
        }
        // Log the total number of generated rules
        FunctionLogger.MethodInformation(_logger,
            $"Successfully generated {secRules.Count} security restriction rules for Subscription ID: {subscriptionId}.");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw; // Re-throw for higher-level handling
      }
      return Task.FromResult(secRules);
    }
  }
}
