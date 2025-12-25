using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
      try
      {
        //run rules
        FunctionLogger.MethodInformation(_logger, "Validating Azure Subscriptions Parameters");
        result.Errors.AddRange(ValidateAzureSubscriptionParameters(settings));

        FunctionLogger.MethodInformation(_logger, "Validating Service Tag IP Addresses");
        result.Errors.AddRange(ValidateServiceTagIPAddresses(settings));

        FunctionLogger.MethodInformation(_logger, "Validating Subscriptions Tag IP Addresses");
        result.Errors.AddRange(ValidateAllowedSubscriptionTags(settings));

        FunctionLogger.MethodInformation(_logger, "Validating Overlapping IP Addresses");
        result.Warnings.AddRange(ValidateAddressRangeOverlapping(settings));
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
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
        FunctionLogger.MethodStart(_logger, nameof(ValidateAddressRangeOverlapping));

        if (settings?.ServiceTags == null || !settings.ServiceTags.Any())
        {
          string warningMessage = "No ServiceTags provided in the settings.";
          FunctionLogger.MethodWarning(_logger, warningMessage);
          warnings.Add(warningMessage);
          return warnings;
        }

        foreach (var tag in settings.ServiceTags)
        {
          if (tag?.AddressPrefixes == null)
          {
            string warningMessage = $"ServiceTag '{tag?.Name}' has no AddressPrefixes defined.";
            FunctionLogger.MethodWarning(_logger, warningMessage);
            warnings.Add(warningMessage);
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
                FunctionLogger.MethodWarning(_logger,warningMessage);
              }
              else
              {
                // Add to the list of address pairs
                addressPairs.Add(new IpAddressScope { IpAddress = addr, AllowedSubscriptions = tag.AllowedSubscriptions.Select(x => x.SubscriptionName ?? "").ToList() });
              }
            }
            else
            {
              FunctionLogger.MethodWarning(_logger, $"Invalid or null AddressPrefix '{addr}' in ServiceTag '{tag.Name}'.");
            }
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Log the outcome of the validation
      if (warnings.Any())
      {
        FunctionLogger.MethodInformation(_logger, $"Address range validation completed with {warnings.Count} warnings.");
      }
      else
      {
        FunctionLogger.MethodInformation(_logger, "Address range validation completed successfully with no overlapping ranges.");
      }
      return warnings;
    }

    public IEnumerable<string> ValidateAllowedSubscriptionTags(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      try
      {
        FunctionLogger.MethodStart(_logger,nameof(ValidateAllowedSubscriptionTags));

        if (settings?.AzureSubscriptions == null || !settings.AzureSubscriptions.Any())
        {
          var errorMessage = "No Azure subscriptions are available for validation.";
          FunctionLogger.MethodError(_logger, errorMessage);
          errors.Add(errorMessage);
          return errors;
        }

        var subscriptionList = settings.AzureSubscriptions.Select(x => x.Name).ToList();
        // Log the subscriptions available for validation
        FunctionLogger.MethodInformation(_logger,$"Available Azure subscriptions: {string.Join(", ", subscriptionList)}");

        if (settings.ServiceTags == null || !settings.ServiceTags.Any())
        {
          var errorMessage = "No ServiceTags are provided in the settings.";
          FunctionLogger.MethodError(_logger, errorMessage);
          errors.Add(errorMessage);
          return errors;
        }
        foreach (var tag in settings.ServiceTags)
        {
          // Validate allowed subscriptions for each service tag
          if (tag.AllowedSubscriptions == null || tag.AllowedSubscriptions.Count <= 0)
          {
            string errorMessage = $"Null/Empty 'ServiceTags.AllowedSubscriptions' value. ServiceTag: {tag.Name}";
            FunctionLogger.MethodWarning(_logger, errorMessage);
            errors.Add(errorMessage);
            continue;
          }
          foreach (var subscriptionTag in tag.AllowedSubscriptions)
          {
            if (!subscriptionList.Contains(subscriptionTag.SubscriptionName))
            {
              string errorMessage = $"Invalid 'ServiceTags.AllowedSubscriptions'. SubscriptionName: {subscriptionTag.SubscriptionName} Tag: {tag.Name}";
              FunctionLogger.MethodError(_logger, errorMessage);
              errors.Add(errorMessage);
            }
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Log the outcome of the validation
      if (errors.Any())
      {
        FunctionLogger.MethodInformation(_logger, $"Subscription allowed Service Tag Validation completed with {errors.Count} errors.");
      }
      else
      {
        FunctionLogger.MethodInformation(_logger, "Subscription allowed Service Tag validation completed successfully with no errors.");
      }
      return errors;
    }

    public IEnumerable<string> ValidateServiceTagIPAddresses(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      try
      {
        FunctionLogger.MethodStart(_logger,nameof(ValidateServiceTagIPAddresses));
        if (settings?.ServiceTags == null || !settings.ServiceTags.Any())
        {
          var errorMessage = "No ServiceTags provided in the settings.";
          FunctionLogger.MethodError(_logger, errorMessage);
          errors.Add(errorMessage);
          return errors;
        }
        foreach (var tag in settings.ServiceTags)
        {
          // Validate Service Tag Name
          if (string.IsNullOrEmpty(tag.Name))
          {
            string errorMessage = $"Null/Empty 'ServiceTags.Name' value.";
            FunctionLogger.MethodWarning(_logger, errorMessage);
            errors.Add(errorMessage);
          }
          // Validate Address Prefixes
          if (tag.AddressPrefixes == null || !tag.AddressPrefixes.Any())
          {
            FunctionLogger.MethodWarning(_logger, $"ServiceTag '{tag.Name}' has no AddressPrefixes defined.");
          }
          else
          {
            foreach (var address in tag.AddressPrefixes)
            {
              if (address != null && !IsCIDRAddressValid(address))
              {
                string errorMessage = $"Invalid 'ServiceTags.AddressPrefixes' value. ServiceTags.Name: {tag.Name}, IPAddress: {address}";
                FunctionLogger.MethodError(_logger, errorMessage);
                errors.Add(errorMessage);
              }
            }
          }
          // Validate Subnet IDs
          if (tag.SubnetIds == null || !tag.SubnetIds.Any())
          {
            FunctionLogger.MethodWarning(_logger, $"ServiceTag '{tag.Name}' has no SubnetIds defined.");
          }
          else
          {
            foreach (var subnetId in tag.SubnetIds)
            {
              if (subnetId != null && !IsValidSubnetId(subnetId))
              {
                string errorMessage = $"Invalid 'ServiceTags.Subnet' value. ServiceTags.Name: {tag.Name}, SubnetId: {subnetId}";
                FunctionLogger.MethodError(_logger, errorMessage);
                errors.Add(errorMessage);
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Log the outcome of the validation
      if (errors.Any())
      {
        FunctionLogger.MethodWarning(_logger, $"Service Tag IP Address Validation completed with {errors.Count} errors");
      }
      else
      {
        FunctionLogger.MethodInformation(_logger, "Service Tag IP Address Validation completed successfully with no errors.");
      }
      return errors;
    }

    public IEnumerable<string> ValidateAzureSubscriptionParameters(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      const int defaultSubscriptionCount = 6;
      try
      {
        FunctionLogger.MethodStart(_logger, nameof(ValidateAddressRangeOverlapping));

        if (settings?.AzureSubscriptions == null || !settings.AzureSubscriptions.Any())
        {
          var errorMessage = "No AzureSubscriptions defined in the settings.";
          errors.Add(errorMessage);
          FunctionLogger.MethodError(_logger, errorMessage);
          return errors;
        }

        // Validate Azure Subscription settings
        if (settings.AzureSubscriptions == null ||
            settings.AzureSubscriptions.Count < defaultSubscriptionCount ||
            settings.AzureSubscriptions.Any(x => String.IsNullOrEmpty(x.Id) || String.IsNullOrEmpty(x.Name)))
        {
          var errorMessage = $"AzureSubscription validation failed. A minimum of {defaultSubscriptionCount} subscriptions with valid 'Id' and 'Name' is required.";
          errors.Add(errorMessage);
          FunctionLogger.MethodWarning(_logger, errorMessage);
        }
        else
        {
          foreach (var subscription in settings.AzureSubscriptions)
          {
            if (string.IsNullOrWhiteSpace(subscription.Id) || string.IsNullOrWhiteSpace(subscription.Name))
            {
              var errorMessage = $"AzureSubscription has missing 'Id' or 'Name'. SubscriptionName: {subscription.Name ?? "N/A"}";
              errors.Add(errorMessage);
              FunctionLogger.MethodWarning(_logger, errorMessage);
              continue;
            }

            if (!Guid.TryParse(subscription.Id, out Guid result))
            {
              string errorMessage = $"Invalid 'AzureSubscription.Id' value. AzureSubscription.Id must be a valid GUID. AzureSubscription.Name: {subscription.Name}";
              errors.Add(errorMessage);
              FunctionLogger.MethodWarning(_logger, errorMessage);
            }
            else
            {
              string message = $"Validated 'AzureSubscription.Id' as valid GUID. AzureSubscription.Id: {subscription.Id}, AzureSubscription.Name: {subscription.Name}";
              // Log valid GUID for tracing
              FunctionLogger.MethodInformation(_logger, message);
            }
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Return any accumulated errors
      if (errors.Any())
      {
        FunctionLogger.MethodWarning(_logger, $"Azure Subscription Validation failed with {errors.Count} errors");
      }
      else
      {
        FunctionLogger.MethodInformation(_logger, "Azure Subscription Validation completed successfully with no errors.");
      }
      return errors;
    }

    public static bool AreIPRangesOverlap(string ipRange1, string ipRange2)
    {
      try
      {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(ipRange1) || string.IsNullOrWhiteSpace(ipRange2))
        {
          throw new ArgumentException("IP ranges cannot be null, empty, or whitespace.");
        }
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