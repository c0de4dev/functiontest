using DynamicAllowListingLib.Logging;
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

      // Log validation start
      _logger.LogValidationStarted(nameof(InternalAndThirdPartyServiceTagSetting));

      try
      {
        // Rule 1: Validate Azure Subscription Parameters
        _logger.LogValidatingAzureSubscriptionParameters();
        var subscriptionErrors = ValidateAzureSubscriptionParameters(settings);
        result.Errors.AddRange(subscriptionErrors);
        _logger.LogValidationStageCompleted("AzureSubscriptionParameters", subscriptionErrors.Count(), 0);

        // Rule 2: Validate Service Tag IP Addresses
        _logger.LogValidatingServiceTagIPAddresses();
        var ipAddressErrors = ValidateServiceTagIPAddresses(settings);
        result.Errors.AddRange(ipAddressErrors);
        _logger.LogValidationStageCompleted("ServiceTagIPAddresses", ipAddressErrors.Count(), 0);

        // Rule 3: Validate Allowed Subscription Tags
        _logger.LogValidatingAllowedSubscriptionTags();
        var allowedSubscriptionErrors = ValidateAllowedSubscriptionTags(settings);
        result.Errors.AddRange(allowedSubscriptionErrors);
        _logger.LogValidationStageCompleted("AllowedSubscriptionTags", allowedSubscriptionErrors.Count(), 0);

        // Rule 4: Validate Overlapping IP Addresses (Warnings only)
        _logger.LogValidatingOverlappingIPAddresses();
        var overlapWarnings = ValidateAddressRangeOverlapping(settings);
        result.Warnings.AddRange(overlapWarnings);
        _logger.LogValidationStageCompleted("AddressRangeOverlapping", 0, overlapWarnings.Count());
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex, nameof(Validate));
        result.Errors.Add($"Unexpected exception during validation: {ex.Message}");
      }

      // Log validation summary
      _logger.LogValidationCompleted(
          nameof(InternalAndThirdPartyServiceTagSetting),
          result.Success,
          result.Errors.Count,
          result.Warnings.Count,
          0);

      return result;
    }

    public IEnumerable<string> ValidateAzureSubscriptionParameters(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      const int defaultSubscriptionCount = 6;
      int validCount = 0;
      int invalidCount = 0;

      try
      {
        if (settings?.AzureSubscriptions == null || !settings.AzureSubscriptions.Any())
        {
          var errorMessage = "No AzureSubscriptions defined in the settings.";
          _logger.LogNoAzureSubscriptionsDefined();
          errors.Add(errorMessage);
          return errors;
        }

        // Log how many subscriptions we're processing
        _logger.LogProcessingAzureSubscriptions(settings.AzureSubscriptions.Count);

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
            invalidCount++;
            continue;
          }

          // Validate that the Id is a valid GUID
          if (!Guid.TryParse(subscription.Id, out _))
          {
            _logger.LogInvalidSubscriptionId(subscription.Name);
            errors.Add($"Invalid 'AzureSubscription.Id' value. AzureSubscription.Id must be a valid GUID. AzureSubscription.Name: {subscription.Name}");
            invalidCount++;
          }
          else
          {
            _logger.LogValidSubscriptionId(subscription.Id, subscription.Name);
            validCount++;
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex, nameof(ValidateAzureSubscriptionParameters));
        errors.Add($"Exception during Azure subscription validation: {ex.Message}");
      }

      // Log validation summary
      _logger.LogAzureSubscriptionValidationSummary(validCount, invalidCount, settings?.AzureSubscriptions?.Count ?? 0);

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

    public IEnumerable<string> ValidateServiceTagIPAddresses(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      int validAddresses = 0;
      int invalidAddresses = 0;
      int validSubnets = 0;
      int invalidSubnets = 0;
      int tagsProcessed = 0;

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
          tagsProcessed++;

          // Log processing of each service tag
          _logger.LogProcessingServiceTagForIPValidation(
              tag.Name,
              tag.AddressPrefixes?.Count ?? 0,
              tag.SubnetIds?.Count ?? 0);

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
              if (string.IsNullOrEmpty(address))
              {
                _logger.LogInvalidAddressPrefixValue(tag.Name, "(null or empty)");
                errors.Add($"Null/Empty 'ServiceTags.AddressPrefixes' value. ServiceTags.Name: {tag.Name}");
                invalidAddresses++;
              }
              else if (!IsCIDRAddressValid(address))
              {
                string errorMessage = $"Invalid 'ServiceTags.AddressPrefixes' value. ServiceTags.Name: {tag.Name}, IPAddress: {address}";
                _logger.LogInvalidAddressPrefixValue(tag.Name, address);
                errors.Add(errorMessage);
                invalidAddresses++;
              }
              else
              {
                _logger.LogValidAddressPrefix(tag.Name, address);
                validAddresses++;
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
              if (string.IsNullOrEmpty(subnetId))
              {
                _logger.LogInvalidSubnetIdValue(tag.Name, "(null or empty)");
                errors.Add($"Null/Empty 'ServiceTags.Subnet' value. ServiceTags.Name: {tag.Name}");
                invalidSubnets++;
              }
              else if (!IsValidSubnetId(subnetId))
              {
                string errorMessage = $"Invalid 'ServiceTags.Subnet' value. ServiceTags.Name: {tag.Name}, SubnetId: {subnetId}";
                _logger.LogInvalidSubnetIdValue(tag.Name, subnetId);
                errors.Add(errorMessage);
                invalidSubnets++;
              }
              else
              {
                _logger.LogValidSubnetIdProcessed(tag.Name, subnetId);
                validSubnets++;
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex, nameof(ValidateServiceTagIPAddresses));
        errors.Add($"Exception during IP address validation: {ex.Message}");
      }

      // Log validation summary
      _logger.LogIPValidationSummary(tagsProcessed, validAddresses, invalidAddresses, validSubnets, invalidSubnets);

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

    public IEnumerable<string> ValidateAllowedSubscriptionTags(InternalAndThirdPartyServiceTagSetting settings)
    {
      List<string> errors = new List<string>();
      int validReferences = 0;
      int invalidReferences = 0;

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
          // Log processing of each service tag
          _logger.LogValidatingServiceTagSubscriptions(tag.Name, tag.AllowedSubscriptions?.Count ?? 0);

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
              invalidReferences++;
            }
            else
            {
              _logger.LogValidAllowedSubscription(subscriptionTag.SubscriptionName, tag.Name);
              validReferences++;
            }
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex, nameof(ValidateAllowedSubscriptionTags));
        errors.Add($"Exception during allowed subscription validation: {ex.Message}");
      }

      // Log validation summary
      _logger.LogAllowedSubscriptionValidationSummary(validReferences, invalidReferences);

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

    public IEnumerable<string> ValidateAddressRangeOverlapping(InternalAndThirdPartyServiceTagSetting settings)
    {
      // Warnings list to store overlapping address issues
      List<string> warnings = new List<string>();
      // List of IP address scopes to track processed ranges
      List<IpAddressScope> addressPairs = new List<IpAddressScope>();
      int totalAddressesProcessed = 0;
      int overlapsFound = 0;
      int tagsProcessed = 0;

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
          tagsProcessed++;

          if (tag?.AddressPrefixes == null)
          {
            _logger.LogServiceTagNoAddressPrefixes(tag?.Name ?? "Unknown");
            warnings.Add($"ServiceTag '{tag?.Name}' has no AddressPrefixes defined.");
            continue;
          }

          // Log processing of each service tag for overlap validation
          _logger.LogProcessingServiceTagForOverlap(tag.Name ?? "Unknown", tag.AddressPrefixes.Count);

          foreach (var addr in tag.AddressPrefixes)
          {
            if (addr != null && IsCIDRAddressValid(addr))
            {
              totalAddressesProcessed++;

              // Check for overlapping IP address ranges within the same subscription scope
              var hasOverlap = addressPairs.Any(x =>
                  x.IpAddress != null &&
                  AreIPRangesOverlap(x.IpAddress, addr) &&
                  x.AllowedSubscriptions.Intersect(tag.AllowedSubscriptions.Select(s => s.SubscriptionName)).Any());

              if (hasOverlap)
              {
                var warningMessage = $"Overlapping 'ServiceTags.AddressPrefixes' detected. " +
                                     $"ServiceTags.Name: {tag.Name}, AddressPrefix: {addr}";
                warnings.Add(warningMessage);
                _logger.LogOverlappingAddressPrefixDetected(tag.Name ?? "Unknown", addr);
                overlapsFound++;
              }
              else
              {
                // Add to the list of address pairs for future overlap checks
                addressPairs.Add(new IpAddressScope
                {
                  IpAddress = addr,
                  AllowedSubscriptions = tag.AllowedSubscriptions.Select(x => x.SubscriptionName ?? "").ToList()
                });
                _logger.LogAddressPrefixTracked(tag.Name ?? "Unknown", addr);
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
        _logger.LogValidationException(ex, nameof(ValidateAddressRangeOverlapping));
        warnings.Add($"Exception during address range overlap validation: {ex.Message}");
      }

      // Log overlap validation summary
      _logger.LogAddressOverlapSummary(totalAddressesProcessed, overlapsFound, tagsProcessed);

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

    public bool AreIPRangesOverlap(string ipRange1, string ipRange2)
    {
      if (string.IsNullOrEmpty(ipRange1) || string.IsNullOrEmpty(ipRange2))
      {
        _logger.LogIPRangeOverlapNullInput(ipRange1, ipRange2);
        throw new ArgumentNullException("IP ranges cannot be null or empty.");
      }

      try
      {
        // Parse the first IP range
        if (!IPNetwork2.TryParse(ipRange1, out var ipNetwork1))
        {
          _logger.LogIPRangeOverlapInvalidCIDR(ipRange1);
          throw new FormatException($"Invalid CIDR format for IP range: {ipRange1}");
        }

        // Parse the second IP range
        if (!IPNetwork2.TryParse(ipRange2, out var ipNetwork2))
        {
          _logger.LogIPRangeOverlapInvalidCIDR(ipRange2);
          throw new FormatException($"Invalid CIDR format for IP range: {ipRange2}");
        }

        // Check for overlap between the two IP ranges
        var overlaps = ipNetwork1.Overlap(ipNetwork2);
        _logger.LogIPRangeOverlapResult(ipRange1, ipRange2, overlaps);

        return overlaps;
      }
      catch (FormatException)
      {
        throw; // Re-throw the exception after logging
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex, nameof(AreIPRangesOverlap));
        throw;
      }
    }

    public bool IsCIDRAddressValid(string addr)
    {
      if (string.IsNullOrEmpty(addr))
      {
        return false;
      }

      Match m = Regex.Match(addr, _cidrPattern, RegexOptions.IgnoreCase);
      return m.Success;
    }

    public bool IsValidSubnetId(string subnetId)
    {
      if (string.IsNullOrEmpty(subnetId))
      {
        return false;
      }

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