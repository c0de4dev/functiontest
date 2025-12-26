using DynamicAllowListingLib.Helpers;
using DynamicAllowListingLib.Logging;
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
    public AzureServiceTagsManager(IAzureServiceTagsJsonHelper azureServiceTagsJsonHelper, ILogger<AzureServiceTagsManager> logger)
    {
      _logger = logger;
      _azureServiceTagsJsonHelper = azureServiceTagsJsonHelper;
    }

    public async Task<bool> IsServiceTagExists(string serviceTagName, string requestedSubscriptionId)
    {
      _logger.LogCheckingAzureServiceTagExists(serviceTagName, requestedSubscriptionId);

      var azureServiceTags = await GetAzureServiceTags(requestedSubscriptionId);

      bool serviceTagExists = false;
      if (azureServiceTags != null)
      {
        serviceTagExists = azureServiceTags.Values.Any(azureServiceTag => azureServiceTag.Name.Equals(serviceTagName));
        if (serviceTagExists == true)
        {
          _logger.LogAzureServiceTagExists(serviceTagName, requestedSubscriptionId);
          return serviceTagExists;
        }
        else
        {
          _logger.LogAzureServiceTagNotExists(serviceTagName, requestedSubscriptionId);
        }
      }
      return serviceTagExists;
    }

    private async Task<AzureServiceTags?> GetAzureServiceTags(string requestedSubscriptionId)
    {
      if (_azureServiceTags == null)
      {
        string azureServiceTagsJson = await _azureServiceTagsJsonHelper.GetAzureServiceTagsJson(requestedSubscriptionId);
        if (string.IsNullOrEmpty(azureServiceTagsJson))
        {
          _logger.LogAzureServiceTagsJsonEmpty(requestedSubscriptionId);
          //throw new Exception("AzureServiceTags JSON could not be obtained.");
        }
        _azureServiceTags = JsonConvert.DeserializeObject<AzureServiceTags>(azureServiceTagsJson);
        if (_azureServiceTags == null)
        {
          _logger.LogAzureServiceTagsDeserializationFailed(requestedSubscriptionId);
          //throw new Exception("AzureServiceTags could not be obtained.");
        }
        _logger.LogAzureServiceTagsRetrieved(requestedSubscriptionId);

        return _azureServiceTags;
      }
      else
      {
        _logger.LogUsingCachedAzureServiceTags(requestedSubscriptionId);
        return _azureServiceTags;
      }
    }

    public virtual async Task<HashSet<IpSecurityRestrictionRule>> GenerateRulesByName(string subscriptionId, string[] serviceTags,
      bool includeMandatoryRulesForSubscription = true)
    {
      // Retrieve the rules based on the provided service tags and subscription ID
      var rules = await GetAzureServiceTagRules(serviceTags, subscriptionId);
      _logger.LogInformation("Generated {RuleCount} rules for Subscription ID '{SubscriptionId}'",
                              rules.Count,
                              subscriptionId);
      return rules;
    }

    internal async Task<HashSet<IpSecurityRestrictionRule>> GetAzureServiceTagRules(string[] serviceTags, string requestedSubscriptionId)
    {
      // Validate inputs
      if (serviceTags == null || serviceTags.Length == 0)
      {
        _logger.LogServiceTagsNullOrEmpty(requestedSubscriptionId);
        Exception ex = new ArgumentException("Service tags must not be null or empty.", nameof(serviceTags));
        throw ex;
      }
      if (string.IsNullOrEmpty(requestedSubscriptionId))
      {
        _logger.LogSubscriptionIdNullOrEmpty();
        //throw new ArgumentException("Subscription ID must not be null or empty.", nameof(requestedSubscriptionId));
      }

      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();
      try
      {
        var nonExistentServiceTags = new List<string>();

        _logger.LogFetchingAzureServiceTags(requestedSubscriptionId);
        var azureServiceTags = await GetAzureServiceTags(requestedSubscriptionId);
        _logger.LogRetrievedAzureServiceTags(azureServiceTags?.Values.Count, requestedSubscriptionId);

        if (azureServiceTags != null)
        {
          foreach (var serviceTag in serviceTags)
          {
            _logger.LogProcessingServiceTag(serviceTag);
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

              _logger.LogFoundRulesForServiceTag(ipSecRules.Count, serviceTag);
            }
            if (!serviceTagFound)
            {
              _logger.LogServiceTagNotFoundInAzure(serviceTag);
              nonExistentServiceTags.Add(serviceTag);
            }
          }
          if (nonExistentServiceTags.Count > 0)
          {
            _logger.LogServiceTagsNotFound(string.Join(", ", nonExistentServiceTags), requestedSubscriptionId);
            throw new AzureServiceTagNotFoundException(nonExistentServiceTags);
          }
        }
        _logger.LogRetrievedIpSecurityRules(ipSecurityRestrictionRules.Count, requestedSubscriptionId);
      }
      catch (Exception ex)
      {
        _logger.LogUnexpectedErrorProcessingServiceTags(ex, requestedSubscriptionId);
        throw; // Rethrow for higher-level handling
      }
      return ipSecurityRestrictionRules;
    }

    internal HashSet<IpSecurityRestrictionRule> GetAzureServiceTagRules(AzureServiceTags.AzureServiceTag azureServiceTag)
    {
      // Validate input
      if (azureServiceTag == null)
      {
        _logger.LogAzureServiceTagNull();
        Exception ex = new ArgumentNullException(nameof(azureServiceTag), "Azure Service Tag must not be null.");
        throw ex;
      }

      _logger.LogGeneratingRulesForAzureServiceTag(azureServiceTag.Name ?? "Unknown");

      var secRules = new HashSet<IpSecurityRestrictionRule>();

      // Check if the service tag contains IPv4 address prefixes
      if (azureServiceTag.Properties?.AddressPrefixesIpV4 == null || !azureServiceTag.Properties.AddressPrefixesIpV4.Any())
      {
        _logger.LogNoIpPrefixesForAzureServiceTag(azureServiceTag.Name ?? "Unknown");
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
        if (secRules.Add(rule))
        {
          _logger.LogRuleCreated(rule.Name ?? "Unknown", rule.IpAddress ?? "Unknown", rule.Action ?? "Allow", rule.Priority);
        }
        else
        {
          _logger.LogDuplicateRuleIgnored(rule.Name ?? "Unknown", rule.IpAddress ?? "Unknown");
        }
        index++;
      }
      _logger.LogGeneratedRulesForAzureServiceTag(secRules.Count, azureServiceTag.Name ?? "Unknown");
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
      ILogger<AzureServiceTagsManager> logger) : base(azureServiceTagsJsonHelper, logger)
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
        // Validate inputs
        /*
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
          string errorMessage = "Subscription ID cannot be null, empty, or whitespace.";
          _logger.LogWarning(errorMessage);
          //throw new ArgumentException(errorMessage, nameof(subscriptionId));
        }
        */
        if (serviceTags == null || serviceTags.Length == 0)
        {
          _logger.LogNoServiceTagsProvided();
          return Task.FromResult(secRules);
        }

        _logger.LogGeneratingSecurityRules(subscriptionId, string.Join(", ", serviceTags));

        // Generate rules for each service tag
        foreach (var serviceTag in serviceTags)
        {
          if (string.IsNullOrWhiteSpace(serviceTag))
          {
            _logger.LogNullOrEmptyServiceTag(subscriptionId);
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
            _logger.LogDuplicateRule(serviceTag);
          }
          else
          {
            _logger.LogRuleCreatedWithTag(rule.Name ?? "Unknown", rule.IpAddress ?? "Unknown", rule.Action ?? "Allow", rule.Priority, rule.Tag ?? "ServiceTag");
          }
        }
        // Log the total number of generated rules
        _logger.LogSecurityRulesGenerated(secRules.Count, subscriptionId);
      }
      catch (Exception ex)
      {
        _logger.LogRuleGenerationException(ex);
        throw; // Re-throw for higher-level handling
      }
      return Task.FromResult(secRules);
    }
  }
}