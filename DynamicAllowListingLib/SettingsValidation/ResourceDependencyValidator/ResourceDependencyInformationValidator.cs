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
    private ILogger<ResourceDependencyInformationValidator> _logger;
    public ResourceDependencyInformationValidator(IServiceTagManagerProvider managerProvider,
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
      try
      {
        //run rules
        _logger.LogValidatingResourceId(settings.ResourceId);
        result.Errors.AddRange(ValidateMainResourceId(settings.ResourceId));

        _logger.LogValidatingNewdayServiceTags();
        result.Errors.AddRange(ValidateNewDayServiceTagExistence(settings));

        _logger.LogValidatingAzureServiceTags();
        result.Errors.AddRange(ValidateAzureServiceTagExistence(settings));

        _logger.LogValidatingResourceIdFormat();
        result.Errors.AddRange(ValidateResourceIdFormat(settings));

        _logger.LogValidatingCrossSubscriptionAllowance(settings.RequestSubscriptionId);
        result.Errors.AddRange(ValidateCrossSubscriptionAllowance(settings));
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
        //run rules
        _logger.LogValidatingFormatResourceId(settings.ResourceId);
        result.Errors.AddRange(ValidateMainResourceId(settings.ResourceId));

        _logger.LogValidatingResourceIdFormatOnly();
        result.Errors.AddRange(ValidateResourceIdFormat(settings));

        _logger.LogValidatingCrossSubscriptionFormat(settings.RequestSubscriptionId);
        result.Errors.AddRange(ValidateCrossSubscriptionAllowance(settings));
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
        errors.Add("Main resource id is null or empty.");
        return errors;
      }
      if (!MatchesMainResourceIdRegex(resourceId, out string errorMessage))
      {
        errors.Add(errorMessage);
      }
      return errors;
    }
    public IEnumerable<string> ValidateNewDayServiceTagExistence(ResourceDependencyInformation settings)
    {
      List<string> errors = new List<string>();
      try
      {
        // Combine NewDayInternalAndThirdPartyTags
        var serviceTagList = (settings.AllowInbound?.SecurityRestrictions?.NewDayInternalAndThirdPartyTags ?? Enumerable.Empty<string>())
            .Union(settings.AllowInbound?.ScmSecurityRestrictions?.NewDayInternalAndThirdPartyTags ?? Enumerable.Empty<string>())
            .ToArray();

        var manager = _managerProvider.GetServiceTagManager(ManagerType.NewDay);
        foreach (var tagName in serviceTagList)
        {
          if (!manager.IsServiceTagExists(tagName).GetAwaiter().GetResult())
          {
            var errorMessage = $"Invalid/Null 'NewDayInternalAndThirdPartyTags' value. TagName: {tagName}";
            errors.Add(errorMessage);
            _logger.LogInvalidNewDayServiceTag(tagName);
          }
          else
          {
            _logger.LogValidatedNewDayServiceTag(tagName);
          }
        }
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
        var serviceTagList = (settings.AllowInbound?.SecurityRestrictions?.AzureServiceTags ?? Enumerable.Empty<string>())
            .Union(settings.AllowInbound?.ScmSecurityRestrictions?.AzureServiceTags ?? Enumerable.Empty<string>())
            .ToArray();
        var manager = _managerProvider.GetServiceTagManager(ManagerType.Azure);
        foreach (var tagName in serviceTagList)
        {
          if (!manager.IsServiceTagExists(tagName, settings.RequestSubscriptionId!).GetAwaiter().GetResult())
          {
            var errorMessage = $"Invalid/Null 'AzureServiceTag' value. TagName: {tagName}";
            errors.Add(errorMessage);
            _logger.LogInvalidAzureServiceTag(tagName);
          }
          else
          {
            _logger.LogValidatedAzureServiceTag(tagName);
          }
        }
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
        if (settings.AllowInbound?.SecurityRestrictions != null)
        {
          var inboundResourceIdErrors = ValidateInboundResourceIds(settings.AllowInbound.SecurityRestrictions.ResourceIds);
          if (inboundResourceIdErrors.Any())
          {
            foreach (var inboundResourceIdError in inboundResourceIdErrors)
            {
              _logger.LogInboundResourceIdError(inboundResourceIdError);
            }
          }
          errorList.AddRange(inboundResourceIdErrors);
        }
        // Check if outbound resource IDs are provided
        if (settings.AllowOutbound?.ResourceIds == null || !(settings.AllowOutbound?.ResourceIds.Length > 0))
        {
          _logger.LogNoOutboundResourceIds();
          return errorList;
        }
        // Validate outbound resource allowance
        if (settings.ResourceId != null && !AreOutboundResourcesAllowed(settings.ResourceId, out string errorMessage))
        {
          errorList.Add(errorMessage);
          _logger.LogOutboundResourceIdError(errorMessage);
          return errorList;
        }
        // Validate outbound resource IDs
        _logger.LogValidatingOutboundResourceIds(settings.AllowOutbound.ResourceIds.Length);
        var outboundResourceIdErrors = ValidateOutboundResourceIds(settings.AllowOutbound.ResourceIds);
        if (outboundResourceIdErrors.Any())
        {
          foreach (var outboundResourceIdError in outboundResourceIdErrors)
          {
            _logger.LogOutboundResourceIdError(outboundResourceIdError);
          }
        }
        errorList.AddRange(outboundResourceIdErrors);
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
          continue;
        }
        if (MatchesAnOutboundResourceIdRegex(resourceId, out string invalidityMessage))
        {
          continue;
        }
        errorList.Add(invalidityMessage);
      }
      return errorList;
    }
    private bool MatchesAnOutboundResourceIdRegex(string resourceId, out string invalidityMessage)
    {
      string[] outboundResourceIdRegexs = {
        WebSiteResourceIdRegex,
        CosmosDb.ResourceIdRegex,
        Storage.ResourceIdRegex,
        KeyVault.ResourceIdRegex,
        SqlServer.ResourceIdRegex
      };
      foreach (var regex in outboundResourceIdRegexs)
      {
        if (Regex.IsMatch(resourceId, regex))
        {
          invalidityMessage = $"Provided id {resourceId} is a valid outbound resource.";
          return true;
        }
      }
      invalidityMessage = $"Provided outbound resourceId {resourceId} is invalid. It should match one of the follow regexs: {string.Join(",", outboundResourceIdRegexs)}.";
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
          continue;
        }
        if (MatchesAnInboundResourceIdRegex(resourceId, out string errorMessage))
        {
          continue;
        }
        errorList.Add(errorMessage);
      }
      return errorList;
    }
    private bool MatchesAnInboundResourceIdRegex(string resourceId, out string invalidityMessage)
    {
      string[] inboundResourceIdRegexs = {
        WebSiteResourceIdRegex,
        VNetSubnetIdRegex,
        PublicIpAddressResourceIdRegex,
        FrontDoorResourceIdRegex
      };
      foreach (var regex in inboundResourceIdRegexs)
      {
        if (Regex.IsMatch(resourceId, regex))
        {
          invalidityMessage = $"Provided id {resourceId} is a valid inbound resource.";
          return true;
        }
      }
      invalidityMessage = $"Provided inbound resourceId {resourceId} is invalid. It should match one of the follow regexs: {string.Join(",", inboundResourceIdRegexs)}.";
      return false;
    }

    private bool MatchesMainResourceIdRegex(string resourceId, out string invalidityMessage)
    {
      string[] mainResourceIdRegexs = {
        WebSiteResourceIdRegex,
        CosmosDb.ResourceIdRegex,
        Storage.ResourceIdRegex,
        KeyVault.ResourceIdRegex,
        SqlServer.ResourceIdRegex
      };
      // Check each regex pattern to determine if the resourceId matches any of them
      foreach (var regex in mainResourceIdRegexs)
      {
        if (Regex.IsMatch(resourceId, regex))
        {
          invalidityMessage = $"Provided id '{resourceId}' is a valid main resource.";
          return true;
        }
      }
      // If no regex matched, provide a detailed invalidity message
      invalidityMessage = $"Provided main resourceId {resourceId} is invalid. It should match one of the follow regexs: {string.Join(",", mainResourceIdRegexs)}.";
      return false;
    }


    public IEnumerable<string> ValidateCrossSubscriptionAllowance(ResourceDependencyInformation settings)
    {
      List<string> errorList = new List<string>();
      try
      {
        _logger.LogValidatingCrossSubscription(settings.ResourceId, settings.RequestSubscriptionId);

        var allowedCrossSubscriptionPool = GetAllowedCrossSubList(settings.RequestSubscriptionId!); //subscription list
        if (allowedCrossSubscriptionPool.Key == null)
        {
          return errorList;
        }
        // Validate inbound resources
        if (settings.AllowInbound?.SecurityRestrictions != null && settings.AllowInbound?.SecurityRestrictions?.ResourceIds != null && settings.AllowInbound?.SecurityRestrictions?.ResourceIds.Length > 0)
        {
          foreach (var resourceId in settings.AllowInbound.SecurityRestrictions.ResourceIds!)
          {
            var subId = ResourceDependencyInformation.GetSubscriptionId(resourceId);
            if (!string.IsNullOrEmpty(subId) && !allowedCrossSubscriptionPool.Value.ContainsKey(subId))
            {
              string errorMessage = $"Provided Inbound Resource, SubscriptionId: {settings.RequestSubscriptionId} is not allowed for cross-subscription with SubscriptionId: {subId}!";
              errorList.Add(errorMessage);
              _logger.LogCrossSubscriptionDetected(settings.RequestSubscriptionId ?? "Unknown", subId);
            }
          }
        }
        // Validate outbound resources
        if (settings.AllowOutbound?.ResourceIds != null && (settings.AllowOutbound?.ResourceIds.Length > 0))
        {
          foreach (var resourceId in settings.AllowOutbound?.ResourceIds!)
          {
            var subId = ResourceDependencyInformation.GetSubscriptionId(resourceId);
            if (!string.IsNullOrEmpty(subId) && !allowedCrossSubscriptionPool.Value.ContainsKey(subId))
            {
              string errorMessage = $"Provided Outbound Resource, SubscriptionId: {settings.RequestSubscriptionId} is not allowed for cross-subscription with SubscriptionId: {subId}!";
              errorList.Add(errorMessage);
              _logger.LogCrossSubscriptionDetected(settings.RequestSubscriptionId ?? "Unknown", subId);
            }
          }
        }
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