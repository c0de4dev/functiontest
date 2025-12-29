using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models.AzureResources;
using DynamicAllowListingLib.ServiceTagManagers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static DynamicAllowListingLib.Constants;

namespace DynamicAllowListingLib.SettingsValidation.ResourceDependencyValidator
{
  public class ResourceDependencyInformationValidator : ISettingValidator<ResourceDependencyInformation>
  {
    private readonly IServiceTagManagerProvider _managerProvider;
    private readonly IResourceGraphExplorerService _resourceService;
    private readonly ILogger<ResourceDependencyInformationValidator> _logger;

    public ResourceDependencyInformationValidator(
      IServiceTagManagerProvider managerProvider,
      IResourceGraphExplorerService resourceService,
      ILogger<ResourceDependencyInformationValidator> logger)
    {
      _managerProvider = managerProvider;
      _resourceService = resourceService;
      _logger = logger;
    }

    public ResultObject Validate(ResourceDependencyInformation settings)
    {
      var result = new ResultObject();
      const int totalChecks = 5;

      try
      {
        // 1. Validate main resource ID
        _logger.LogValidatingResourceId(settings.ResourceId);
        result.Errors.AddRange(ValidateMainResourceId(settings.ResourceId));

        // 2. Validate NewDay service tags
        _logger.LogValidatingNewdayServiceTags();
        result.Errors.AddRange(ValidateNewDayServiceTagExistence(settings));

        // 3. Validate Azure service tags
        _logger.LogValidatingAzureServiceTags();
        result.Errors.AddRange(ValidateAzureServiceTagExistence(settings));

        // 4. Validate resource ID formats
        _logger.LogValidatingResourceIdFormat();
        result.Errors.AddRange(ValidateResourceIdFormat(settings));

        // 5. Validate cross-subscription allowance
        _logger.LogValidatingCrossSubscriptionAllowance(settings.RequestSubscriptionId);
        result.Errors.AddRange(ValidateCrossSubscriptionAllowance(settings));

        // Log validation completion
        if (result.Success)
        {
          _logger.LogValidationCompletedSuccess(settings.ResourceId);
        }
        else
        {
          _logger.LogValidationCompletedWithErrors(settings.ResourceId, result.Errors.Count);
        }

        _logger.LogValidationSummary(settings.ResourceId, totalChecks, result.Errors.Count, result.Success);
      }
      catch (Exception ex)
      {
        result.Errors.Add($"Exception occured in validation: {ex.Message}");
        _logger.LogValidationExceptionWithMessage(ex.Message);

        // Check and log the inner exception if present
        if (ex.InnerException != null)
        {
          result.Errors.Add($"Inner Exception: {ex.InnerException.Message}");
          _logger.LogInnerException(ex.InnerException.Message);
        }
      }

      return result;
    }

    public ResultObject ValidateFormat(ResourceDependencyInformation settings)
    {
      var result = new ResultObject();

      try
      {
        // Validate main resource ID format
        _logger.LogValidatingFormatResourceId(settings.ResourceId);
        result.Errors.AddRange(ValidateMainResourceId(settings.ResourceId));

        // Validate resource ID formats
        _logger.LogValidatingResourceIdFormatOnly();
        result.Errors.AddRange(ValidateResourceIdFormat(settings));

        // Validate cross-subscription allowance
        _logger.LogValidatingCrossSubscriptionFormat(settings.RequestSubscriptionId);
        result.Errors.AddRange(ValidateCrossSubscriptionAllowance(settings));

        // Log validation completion
        if (result.Success)
        {
          _logger.LogFormatValidationCompletedSuccess(settings.ResourceId);
        }
        else
        {
          _logger.LogFormatValidationCompletedWithErrors(settings.ResourceId, result.Errors.Count);
        }
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }

      return result;
    }

    public IEnumerable<string> ValidateMainResourceId(string? resourceId)
    {
      var errors = new List<string>();

      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogMainResourceIdNullOrEmpty();
        errors.Add("Main resource id is null or empty.");
        return errors;
      }

      if (!MatchesMainResourceIdRegex(resourceId, out string errorMessage, out string? matchedType))
      {
        _logger.LogMainResourceIdInvalidFormat(resourceId, errorMessage);
        errors.Add(errorMessage);
      }
      else
      {
        _logger.LogMainResourceIdValid(resourceId, matchedType ?? "Unknown");
      }

      return errors;
    }

    public IEnumerable<string> ValidateNewDayServiceTagExistence(ResourceDependencyInformation settings)
    {
      List<string> errors = new List<string>();

      try
      {
        // Collect tags from both sources
        var securityRestrictionsTags = settings.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags ?? Enumerable.Empty<string>();
        var scmSecurityRestrictionsTags = settings.AllowInbound?.ScmSecurityRestrictions?.NewDayInternalAndThirdPartyTags ?? Enumerable.Empty<string>();

        _logger.LogNewDayServiceTagsCollected(
            securityRestrictionsTags.Count(),
            scmSecurityRestrictionsTags.Count());

        var serviceTagList = securityRestrictionsTags
            .Union(scmSecurityRestrictionsTags)
            .ToArray();

        // Check if there are any tags to validate
        if (!serviceTagList.Any())
        {
          _logger.LogNoNewDayServiceTagsToValidate();
          return errors;
        }

        _logger.LogNewDayServiceTagValidationStart(serviceTagList.Length);

        var manager = _managerProvider.GetServiceTagManager(ManagerType.NewDay);
        int validCount = 0;
        int invalidCount = 0;

        foreach (var tagName in serviceTagList)
        {
          if (!manager.IsServiceTagExists(tagName).GetAwaiter().GetResult())
          {
            var errorMessage = $"Invalid/Null 'NewDayInternalAndThirdPartyTags' value. TagName: {tagName}";
            errors.Add(errorMessage);
            _logger.LogInvalidNewDayServiceTag(tagName);
            invalidCount++;
          }
          else
          {
            _logger.LogValidatedNewDayServiceTag(tagName);
            validCount++;
          }
        }

        _logger.LogNewDayServiceTagValidationComplete(validCount, invalidCount);
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
        throw;
      }

      return errors;
    }

    public IEnumerable<string> ValidateAzureServiceTagExistence(ResourceDependencyInformation settings)
    {
      List<string> errors = new List<string>();

      try
      {
        // Collect tags from both sources
        var securityRestrictionsTags = settings.AllowInbound?.SecurityRestrictions?.AzureServiceTags ?? Enumerable.Empty<string>();
        var scmSecurityRestrictionsTags = settings.AllowInbound?.ScmSecurityRestrictions?.AzureServiceTags ?? Enumerable.Empty<string>();

        _logger.LogAzureServiceTagsCollected(
            securityRestrictionsTags.Count(),
            scmSecurityRestrictionsTags.Count());

        var serviceTagList = securityRestrictionsTags
            .Union(scmSecurityRestrictionsTags)
            .ToArray();

        // Check if there are any tags to validate
        if (!serviceTagList.Any())
        {
          _logger.LogNoAzureServiceTagsToValidate();
          return errors;
        }

        _logger.LogAzureServiceTagValidationStart(serviceTagList.Length, settings.RequestSubscriptionId);

        var manager = _managerProvider.GetServiceTagManager(ManagerType.Azure);
        int validCount = 0;
        int invalidCount = 0;

        foreach (var tagName in serviceTagList)
        {
          if (!manager.IsServiceTagExists(tagName, settings.RequestSubscriptionId!).GetAwaiter().GetResult())
          {
            var errorMessage = $"Invalid/Null 'AzureServiceTag' value. TagName: {tagName}";
            errors.Add(errorMessage);
            _logger.LogInvalidAzureServiceTag(tagName);
            invalidCount++;
          }
          else
          {
            _logger.LogValidatedAzureServiceTag(tagName);
            validCount++;
          }
        }

        _logger.LogAzureServiceTagValidationComplete(validCount, invalidCount);
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
        throw;
      }

      return errors;
    }

    public IEnumerable<string> ValidateResourceIdFormat(ResourceDependencyInformation settings)
    {
      List<string> errorList = new List<string>();

      try
      {
        // Validate inbound resource IDs
        if (settings.AllowInbound?.SecurityRestrictions?.ResourceIds != null &&
            settings.AllowInbound.SecurityRestrictions.ResourceIds.Length > 0)
        {
          _logger.LogValidatingInboundResourceIds(settings.AllowInbound.SecurityRestrictions.ResourceIds.Length);

          var inboundResourceIdErrors = ValidateInboundResourceIds(settings.AllowInbound.SecurityRestrictions.ResourceIds);
          var inboundErrorList = inboundResourceIdErrors.ToList();

          if (inboundErrorList.Any())
          {
            foreach (var inboundResourceIdError in inboundErrorList)
            {
              _logger.LogInboundResourceIdError(inboundResourceIdError);
            }
          }

          _logger.LogInboundResourceIdValidationComplete(
              settings.AllowInbound.SecurityRestrictions.ResourceIds.Length - inboundErrorList.Count,
              inboundErrorList.Count);

          errorList.AddRange(inboundErrorList);
        }
        else
        {
          _logger.LogNoInboundSecurityRestrictions();
        }

        // Check if outbound resource IDs are provided
        if (settings.AllowOutbound?.ResourceIds == null ||
            !(settings.AllowOutbound?.ResourceIds.Length > 0))
        {
          _logger.LogNoOutboundResourceIds();

          // Log the final validation results if no outbound to process
          if (!errorList.Any())
          {
            _logger.LogResourceIdFormatValidationSuccess();
          }

          return errorList;
        }

        // Validate outbound resource allowance
        if (settings.ResourceId != null && !AreOutboundResourcesAllowed(settings.ResourceId, out string errorMessage))
        {
          errorList.Add(errorMessage);
          _logger.LogOutboundResourceIdError(errorMessage);
          _logger.LogOutboundResourcesNotAllowed(settings.ResourceId);
          return errorList;
        }

        // Validate outbound resource IDs
        _logger.LogValidatingOutboundResourceIds(settings.AllowOutbound.ResourceIds.Length);

        var outboundResourceIdErrors = ValidateOutboundResourceIds(settings.AllowOutbound.ResourceIds);
        var outboundErrorList = outboundResourceIdErrors.ToList();

        if (outboundErrorList.Any())
        {
          foreach (var outboundResourceIdError in outboundErrorList)
          {
            _logger.LogOutboundResourceIdError(outboundResourceIdError);
          }
        }

        _logger.LogOutboundResourceIdValidationComplete(
            settings.AllowOutbound.ResourceIds.Length - outboundErrorList.Count,
            outboundErrorList.Count);

        errorList.AddRange(outboundErrorList);
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }

      // Log the final validation results
      if (!errorList.Any())
      {
        _logger.LogResourceIdFormatValidationSuccess();
      }

      return errorList;
    }

    public IEnumerable<string> ValidateCrossSubscriptionAllowance(ResourceDependencyInformation settings)
    {
      List<string> errorList = new List<string>();
      int inboundErrorCount = 0;
      int outboundErrorCount = 0;

      try
      {
        _logger.LogValidatingCrossSubscription(settings.ResourceId, settings.RequestSubscriptionId);

        // Check if request subscription ID is provided
        if (string.IsNullOrEmpty(settings.RequestSubscriptionId))
        {
          _logger.LogRequestSubscriptionIdNullOrEmpty();
          return errorList;
        }

        var allowedCrossSubscriptionPool = GetAllowedCrossSubList(settings.RequestSubscriptionId!);

        if (allowedCrossSubscriptionPool.Key == null)
        {
          _logger.LogCrossSubscriptionPoolNotFound(settings.RequestSubscriptionId);
          return errorList;
        }

        _logger.LogCrossSubscriptionPoolFound(
            allowedCrossSubscriptionPool.Key,
            allowedCrossSubscriptionPool.Value.Count);

        // Validate inbound resources
        if (settings.AllowInbound?.SecurityRestrictions?.ResourceIds != null &&
            settings.AllowInbound.SecurityRestrictions.ResourceIds.Length > 0)
        {
          _logger.LogValidatingInboundCrossSubscription(settings.AllowInbound.SecurityRestrictions.ResourceIds.Length);

          foreach (var resourceId in settings.AllowInbound.SecurityRestrictions.ResourceIds!)
          {
            var subId = ResourceDependencyInformation.GetSubscriptionId(resourceId);

            if (!string.IsNullOrEmpty(subId) && !allowedCrossSubscriptionPool.Value.ContainsKey(subId))
            {
              string errorMessage = $"Provided Inbound Resource, SubscriptionId: {settings.RequestSubscriptionId} is not allowed for cross-subscription with SubscriptionId: {subId}!";
              errorList.Add(errorMessage);
              _logger.LogInboundCrossSubscriptionNotAllowed(resourceId, subId);
              _logger.LogCrossSubscriptionDetected(settings.RequestSubscriptionId ?? "Unknown", subId);
              inboundErrorCount++;
            }
            else if (!string.IsNullOrEmpty(subId))
            {
              _logger.LogInboundCrossSubscriptionAllowed(resourceId, subId);
            }
          }
        }

        // Validate outbound resources
        if (settings.AllowOutbound?.ResourceIds != null &&
            settings.AllowOutbound.ResourceIds.Length > 0)
        {
          _logger.LogValidatingOutboundCrossSubscription(settings.AllowOutbound.ResourceIds.Length);

          foreach (var resourceId in settings.AllowOutbound?.ResourceIds!)
          {
            var subId = ResourceDependencyInformation.GetSubscriptionId(resourceId);

            if (!string.IsNullOrEmpty(subId) && !allowedCrossSubscriptionPool.Value.ContainsKey(subId))
            {
              string errorMessage = $"Provided Outbound Resource, SubscriptionId: {settings.RequestSubscriptionId} is not allowed for cross-subscription with SubscriptionId: {subId}!";
              errorList.Add(errorMessage);
              _logger.LogOutboundCrossSubscriptionNotAllowed(resourceId, subId);
              _logger.LogCrossSubscriptionDetected(settings.RequestSubscriptionId ?? "Unknown", subId);
              outboundErrorCount++;
            }
            else if (!string.IsNullOrEmpty(subId))
            {
              _logger.LogOutboundCrossSubscriptionAllowed(resourceId, subId);
            }
          }
        }

        _logger.LogCrossSubscriptionValidationComplete(inboundErrorCount, outboundErrorCount);
      }
      catch (Exception ex)
      {
        _logger.LogValidationException(ex);
      }

      // Log the result
      if (!errorList.Any())
      {
        _logger.LogCrossSubscriptionValidationPassed();
      }

      return errorList;
    }

    #region Private Helper Methods

    private bool MatchesMainResourceIdRegex(string resourceId, out string invalidityMessage, out string? matchedType)
    {
      var mainResourceIdPatterns = new Dictionary<string, string>
      {
        { "WebSite", WebSiteResourceIdRegex },
        { "CosmosDb", CosmosDb.ResourceIdRegex },
        { "Storage", Storage.ResourceIdRegex },
        { "KeyVault", KeyVault.ResourceIdRegex },
        { "SqlServer", SqlServer.ResourceIdRegex }
      };

      // Check each regex pattern to determine if the resourceId matches any of them
      foreach (var pattern in mainResourceIdPatterns)
      {
        _logger.LogCheckingResourceIdPattern(resourceId, pattern.Key);

        if (Regex.IsMatch(resourceId, pattern.Value))
        {
          invalidityMessage = $"Provided id '{resourceId}' is a valid main resource.";
          matchedType = pattern.Key;
          return true;
        }
      }

      // If no regex matched, provide a detailed invalidity message
      invalidityMessage = $"Provided main resourceId {resourceId} is invalid. It should match one of the follow regexs: {string.Join(",", mainResourceIdPatterns.Values)}.";
      matchedType = null;
      return false;
    }

    private bool AreOutboundResourcesAllowed(string resourceId, out string invalidityMessage)
    {
      string[] outboundAllowedResourceIdRegexs = {
        WebSiteResourceIdRegex
      };

      if (outboundAllowedResourceIdRegexs.Any(regex => Regex.IsMatch(resourceId, regex)))
      {
        invalidityMessage = $"Provided id {resourceId} can have outbound resources.";
        return true;
      }

      invalidityMessage = $"Outbound resources are not allowed for resourceId {resourceId}. " +
                    $"Outbound resources are allowed for resource IDs matching regexs: {string.Join(",", outboundAllowedResourceIdRegexs)}.";
      return false;
    }

    private IEnumerable<string> ValidateOutboundResourceIds(string[]? resourceIds)
    {
      var errorList = new List<string>();

      if (resourceIds == null)
      {
        return errorList;
      }

      foreach (string resourceId in resourceIds)
      {
        if (string.IsNullOrEmpty(resourceId))
        {
          const string errorMessage = "Invalid null or empty resource id found in provided outbound resource ids.";
          errorList.Add(errorMessage);
          _logger.LogNullOrEmptyOutboundResourceId();
          continue;
        }

        _logger.LogValidatingOutboundResourceId(resourceId);

        if (MatchesAnOutboundResourceIdRegex(resourceId, out string invalidityMessage, out string? matchedPattern))
        {
          _logger.LogOutboundResourceIdValid(resourceId, matchedPattern ?? "Unknown");
          continue;
        }

        errorList.Add(invalidityMessage);
      }

      return errorList;
    }

    private bool MatchesAnOutboundResourceIdRegex(string resourceId, out string invalidityMessage, out string? matchedPattern)
    {
      var outboundResourceIdPatterns = new Dictionary<string, string>
      {
        { "WebSite", WebSiteResourceIdRegex },
        { "CosmosDb", CosmosDb.ResourceIdRegex },
        { "Storage", Storage.ResourceIdRegex },
        { "KeyVault", KeyVault.ResourceIdRegex },
        { "SqlServer", SqlServer.ResourceIdRegex }
      };

      foreach (var pattern in outboundResourceIdPatterns)
      {
        if (Regex.IsMatch(resourceId, pattern.Value))
        {
          invalidityMessage = $"Provided id {resourceId} is a valid outbound resource.";
          matchedPattern = pattern.Key;
          return true;
        }
      }

      invalidityMessage = $"Provided outbound resourceId {resourceId} is invalid. It should match one of the follow regexs: {string.Join(",", outboundResourceIdPatterns.Values)}.";
      matchedPattern = null;
      return false;
    }

    private IEnumerable<string> ValidateInboundResourceIds(string[]? securityRestrictionsResourceIds)
    {
      var errorList = new List<string>();

      if (securityRestrictionsResourceIds == null)
      {
        return errorList;
      }

      foreach (string resourceId in securityRestrictionsResourceIds)
      {
        if (string.IsNullOrEmpty(resourceId))
        {
          const string emErrorMessage = "Invalid null or empty resource id found in provided inbound resource ids.";
          errorList.Add(emErrorMessage);
          _logger.LogNullOrEmptyInboundResourceId();
          continue;
        }

        _logger.LogValidatingInboundResourceId(resourceId);

        if (MatchesAnInboundResourceIdRegex(resourceId, out string errorMessage, out string? matchedPattern))
        {
          _logger.LogInboundResourceIdValid(resourceId, matchedPattern ?? "Unknown");
          continue;
        }

        errorList.Add(errorMessage);
      }

      return errorList;
    }

    private bool MatchesAnInboundResourceIdRegex(string resourceId, out string invalidityMessage, out string? matchedPattern)
    {
      var inboundResourceIdPatterns = new Dictionary<string, string>
      {
        { "WebSite", WebSiteResourceIdRegex },
        { "VNetSubnet", VNetSubnetIdRegex },
        { "PublicIpAddress", PublicIpAddressResourceIdRegex },
        { "FrontDoor", FrontDoorResourceIdRegex }
      };

      foreach (var pattern in inboundResourceIdPatterns)
      {
        if (Regex.IsMatch(resourceId, pattern.Value))
        {
          invalidityMessage = $"Provided id {resourceId} is a valid inbound resource.";
          matchedPattern = pattern.Key;
          return true;
        }
      }

      invalidityMessage = $"Provided inbound resourceId {resourceId} is invalid. It should match one of the follow regexs: {string.Join(",", inboundResourceIdPatterns.Values)}.";
      matchedPattern = null;
      return false;
    }

    private KeyValuePair<string, Dictionary<string, string>> GetAllowedCrossSubList(string requestSubscriptionId)
    {
      if (string.IsNullOrEmpty(requestSubscriptionId))
      {
        return default;
      }

      // Try to find the allowed cross-subscription list
      var result = AllowedCrossSubscription.Group
          .Where(x => x.Value.ContainsKey(requestSubscriptionId))
          .FirstOrDefault();

      return result;
    }

    #endregion
  }

  public static class AllowedCrossSubscription
  {
    public static Dictionary<string, Dictionary<string, string>> Group = new Dictionary<string, Dictionary<string, string>> {
      {
        "dev",
        new Dictionary<string, string> {
        { "4f394001-a863-40f3-b6c7-a9f6e91c2b46","ndSIT" },
        { "0f8b9bd9-53ae-4493-9477-e048ca720641","DIGITAL-DEV" },
        { "6473388f-42fe-49d6-8300-5c62a33fb380","NDC-EU-PE-DEV-NDC" }}
      },
      {
        "uat",
        new Dictionary<string, string> {
        { "87bd1863-6d5c-4d04-9aaa-a9564c89692f","ndUAT" },
        { "8aae5d53-f263-479e-959c-0c67619ac334","DIGITAL-UAT" },
        { "5dd3b886-e45a-44c2-b3c1-56b6614d5549","NDC-EU-PE-STG-NDC" }}
      },
      {
        "prd",
        new Dictionary<string, string> {
        { "89969ba8-2bfd-42e0-88bb-b16d646e30a9","ndPRO" },
        { "57effcb3-b94c-4408-948b-de6afbbdb13c","DIGITAL-PRD" },
        { "ee132daf-cc00-448e-8617-2ee3b3799f7c","NDC-EU-PE-PRD-NDC" }}
      }
    };
  }
}