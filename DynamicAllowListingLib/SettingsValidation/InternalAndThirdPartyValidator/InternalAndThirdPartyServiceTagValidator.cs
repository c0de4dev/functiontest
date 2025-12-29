using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using static DynamicAllowListingLib.Models.VNets;

namespace DynamicAllowListingLib.SettingsValidation.InternalAndThirdPartyValidator
{
  public class InternalAndThirdPartyServiceTagValidator : ISettingValidator<InternalAndThirdPartyServiceTagSetting>
  {
    private readonly string _cidrPattern = @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/(3[0-2]|[1-2][0-9]|[0-9]))$";
    private ILogger<InternalAndThirdPartyServiceTagValidator> _logger;

    public InternalAndThirdPartyServiceTagValidator(ILogger<InternalAndThirdPartyServiceTagValidator> logger)
    {
      _logger = logger;
    }

    public ResultObject Validate(InternalAndThirdPartyServiceTagSetting settings)
    {
      var result = new ResultObject();
      var stopwatch = Stopwatch.StartNew();

      // Log validation start
      _logger.LogValidationStarted(nameof(InternalAndThirdPartyServiceTagSetting));

      try
      {
        //run rules
        _logger.LogValidatingAzureSubscriptionParameters();
        result.Errors.AddRange(ValidateAzureSubscriptionParameters(settings));

        _logger.LogValidatingServiceTagIPAddresses();
        result.Errors.AddRange(ValidateServiceTagIPAddresses(settings));

        _logger.LogValidatingAllowedSubscriptionTags();
        result.Errors.AddRange(ValidateAllowedSubscriptionTags(settings));

        _logger.LogValidatingOverlappingIPAddresses();
        result.Warnings.AddRange(ValidateAddressRangeOverlapping(settings));
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }
      finally
      {
        stopwatch.Stop();

        // Log validation summary
        _logger.LogValidationCompleted(
            nameof(InternalAndThirdPartyServiceTagSetting),
            result.Success,
            result.Errors.Count,
            result.Warnings.Count,
            stopwatch.ElapsedMilliseconds);
      }

      return result;
    }

    public IEnumerable<string> ValidateAddressRangeOverlapping(InternalAndThirdPartyServiceTagSetting settings)
    {
      //Warnings list to store overlapping address issues
      List<string> warnings = new List<string>();
      // List of IP address scopes to track processed ranges
      List<IpAddressScope> addressPairs = new List<IpAddressScope>();
      try
      {
        if (settings?.ServiceTags == null || !settings.ServiceTags.Any())
        {
          _logger.LogNoServiceTagsInSettings();
          warnings.Add("No ServiceTags provided in the settings.");
          return warnings;
        }

        foreach (var tag in settings.ServiceTags)
        {
          if (tag?.AddressPrefixes == null)
          {
            _logger.LogServiceTagNoAddressPrefixes(tag?.Name ?? "Unknown");
            warnings.Add($"ServiceTag '{tag?.Name}' has no AddressPrefixes defined.");
            continue;
          }
          foreach (var addr in tag.AddressPrefixes)
          {
            if (addr != null && IsCIDRAddressValid(addr))
            {
              // Check for overlapping IP address ranges
              if (addressPairs.Any(x => x.IpAddress != null && AreIPRangesOverlap(x.IpAddress, addr) && x.AllowedSubscriptions.Intersect(tag.AllowedSubscriptions.Select(x => x.SubscriptionName)).Any()))
              {
                var warningMessage = $"Overlapping 'ServiceTags.AddressPrefixes' detected. " +
                                             $"ServiceTags.Name: {tag.Name}, AddressPrefix: {addr}";
                warnings.Add(warningMessage);
                _logger.LogOverlappingAddressPrefixDetected(tag.Name ?? "Unknown", addr);
              }
              else
              {
                // Add to the list of address pairs
                addressPairs.Add(new IpAddressScope { IpAddress = addr, AllowedSubscriptions = tag.AllowedSubscriptions.Select(x => x.SubscriptionName ?? "").ToList() });
              }
            }
            else
            {
              _logger.LogInvalidAddressPrefix(addr, tag.Name ?? "Unknown");
            }
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }
      // Log the outcome of the validation
      if (warnings.Any())
      {
        _logger.LogAddressRangeValidationWithWarnings(warnings.Count);
      }
      else
      {
        _logger.LogAddressRangeValidationSuccess();
      }
      return warnings;
    }

    public IEnumerable<string> ValidateAllowedSubscriptionTags(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      try
      {
        if (settings?.AzureSubscriptions == null || !settings.AzureSubscriptions.Any())
        {
          var errorMessage = "No Azure subscriptions are available for validation.";
          _logger.LogNoAzureSubscriptionsForValidation();
          errors.Add(errorMessage);
          return errors;
        }

        var subscriptionList = settings.AzureSubscriptions.Select(x => x.Name).ToList();
        // Log the subscriptions available for validation
        _logger.LogAvailableAzureSubscriptions(string.Join(", ", subscriptionList));

        if (settings.ServiceTags == null || !settings.ServiceTags.Any())
        {
          var errorMessage = "No ServiceTags are provided in the settings.";
          _logger.LogNoServiceTagsProvided();
          errors.Add(errorMessage);
          return errors;
        }
        foreach (var tag in settings.ServiceTags)
        {
          // Validate allowed subscriptions for each service tag
          if (tag.AllowedSubscriptions == null || tag.AllowedSubscriptions.Count <= 0)
          {
            string errorMessage = $"Null/Empty 'ServiceTags.AllowedSubscriptions' value. ServiceTag: {tag.Name}";
            _logger.LogNullOrEmptyAllowedSubscriptions(tag.Name);
            errors.Add(errorMessage);
            continue;
          }
          foreach (var subscriptionTag in tag.AllowedSubscriptions)
          {
            if (!subscriptionList.Contains(subscriptionTag.SubscriptionName))
            {
              string errorMessage = $"Invalid 'ServiceTags.AllowedSubscriptions'. SubscriptionName: {subscriptionTag.SubscriptionName} Tag: {tag.Name}";
              _logger.LogInvalidAllowedSubscription(subscriptionTag.SubscriptionName, tag.Name);
              errors.Add(errorMessage);
            }
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }
      // Log the outcome of the validation
      if (errors.Any())
      {
        _logger.LogAllowedSubscriptionValidationWithErrors(errors.Count);
      }
      else
      {
        _logger.LogAllowedSubscriptionValidationSuccess();
      }
      return errors;
    }

    public IEnumerable<string> ValidateServiceTagIPAddresses(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      try
      {
        if (settings?.ServiceTags == null || !settings.ServiceTags.Any())
        {
          var errorMessage = "No ServiceTags provided in the settings.";
          _logger.LogNoServiceTagsProvidedForIPValidation();
          errors.Add(errorMessage);
          return errors;
        }
        foreach (var tag in settings.ServiceTags)
        {
          // Validate Service Tag Name
          if (string.IsNullOrEmpty(tag.Name))
          {
            string errorMessage = $"Null/Empty 'ServiceTags.Name' value.";
            _logger.LogNullOrEmptyServiceTagName();
            errors.Add(errorMessage);
          }
          // Validate Address Prefixes
          if (tag.AddressPrefixes == null || !tag.AddressPrefixes.Any())
          {
            _logger.LogNoAddressPrefixesDefined(tag.Name);
          }
          else
          {
            foreach (var address in tag.AddressPrefixes)
            {
              if (address != null && !IsCIDRAddressValid(address))
              {
                string errorMessage = $"Invalid 'ServiceTags.AddressPrefixes' value. ServiceTags.Name: {tag.Name}, IPAddress: {address}";
                _logger.LogInvalidAddressPrefixValue(tag.Name, address);
                errors.Add(errorMessage);
              }
            }
          }
          // Validate Subnet IDs
          if (tag.SubnetIds == null || !tag.SubnetIds.Any())
          {
            _logger.LogNoSubnetIdsDefined(tag.Name);
          }
          else
          {
            foreach (var subnetId in tag.SubnetIds)
            {
              if (subnetId != null && !IsValidSubnetId(subnetId))
              {
                string errorMessage = $"Invalid 'ServiceTags.Subnet' value. ServiceTags.Name: {tag.Name}, SubnetId: {subnetId}";
                _logger.LogInvalidSubnetIdValue(tag.Name, subnetId);
                errors.Add(errorMessage);
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }
      // Log the outcome of the validation
      if (errors.Any())
      {
        _logger.LogIPAddressValidationWithErrors(errors.Count);
      }
      else
      {
        _logger.LogIPAddressValidationSuccess();
      }
      return errors;
    }

    public IEnumerable<string> ValidateAzureSubscriptionParameters(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      const int defaultSubscriptionCount = 6;
      try
      {
        if (settings?.AzureSubscriptions == null || !settings.AzureSubscriptions.Any())
        {
          var errorMessage = "No AzureSubscriptions defined in the settings.";
          _logger.LogNoAzureSubscriptionsDefined();
          errors.Add(errorMessage);
          return errors;
        }

        // Validate minimum subscription count and required fields
        var validSubscriptionCount = settings.AzureSubscriptions.Count(x => !string.IsNullOrEmpty(x.Id) && !string.IsNullOrEmpty(x.Name));
        if (validSubscriptionCount < defaultSubscriptionCount)
        {
          _logger.LogAzureSubscriptionValidationFailed(defaultSubscriptionCount);
          errors.Add($"AzureSubscription validation failed. A minimum of {defaultSubscriptionCount} subscriptions with valid 'Id' and 'Name' is required.");
        }

        foreach (var subscription in settings.AzureSubscriptions)
        {
          // Validate that both Id and Name are provided
          if (string.IsNullOrEmpty(subscription.Id) || string.IsNullOrEmpty(subscription.Name))
          {
            _logger.LogMissingIdOrName(subscription.Name ?? "Unknown");
            errors.Add($"AzureSubscription has missing 'Id' or 'Name'. SubscriptionName: {subscription.Name}");
            continue;
          }

          // Validate that the Id is a valid GUID
          if (!Guid.TryParse(subscription.Id, out _))
          {
            _logger.LogInvalidSubscriptionId(subscription.Name);
            errors.Add($"Invalid 'AzureSubscription.Id' value. AzureSubscription.Id must be a valid GUID. AzureSubscription.Name: {subscription.Name}");
          }
          else
          {
            _logger.LogValidSubscriptionId(subscription.Id, subscription.Name);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }
      // Log the outcome of the validation
      if (errors.Any())
      {
        _logger.LogAzureSubscriptionValidationWithErrors(errors.Count);
      }
      else
      {
        _logger.LogAzureSubscriptionValidationSuccess();
      }
      return errors;
    }

    public bool AreIPRangesOverlap(string ipRange1, string ipRange2)
    {
      if (string.IsNullOrEmpty(ipRange1) || string.IsNullOrEmpty(ipRange2))
      {
        throw new ArgumentNullException("IP ranges cannot be null or empty.");
      }
      try
      {
        // Parse the first IP range
        if (!IPNetwork2.TryParse(ipRange1, out var ipNetwork1))
        {
          throw new FormatException($"Invalid CIDR format for IP range: {ipRange1}");
        }
        // Parse the second IP range
        if (!IPNetwork2.TryParse(ipRange2, out var ipNetwork2))
        {
          throw new FormatException($"Invalid CIDR format for IP range: {ipRange2}");
        }
        // Check for overlap between the two IP ranges
        return ipNetwork1.Overlap(ipNetwork2);
      }
      catch (Exception)
      {
        throw; // Re-throw the exception after logging for further handling
      }
    }

    public bool IsCIDRAddressValid(string addr)
    {
      Match m = Regex.Match(addr, _cidrPattern, RegexOptions.IgnoreCase);
      return m.Success;
    }

    public bool IsValidSubnetId(string subnetId)
    {
      return Regex.IsMatch(subnetId, Constants.VNetSubnetIdRegex);
    }

    public ResultObject ValidateFormat(InternalAndThirdPartyServiceTagSetting settings)
    {
      throw new NotImplementedException();
    }

    private class IpAddressScope
    {
      public string? IpAddress { get; set; }
      public List<string> AllowedSubscriptions { get; set; } = new List<string>();
    }
  }
}