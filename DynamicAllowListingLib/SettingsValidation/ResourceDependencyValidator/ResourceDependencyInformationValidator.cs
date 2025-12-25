using DynamicAllowListingLib.Logger;
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
        FunctionLogger.MethodInformation(_logger, $"Validating Resource ID: {settings.ResourceId}");
        result.Errors.AddRange(ValidateMainResourceId(settings.ResourceId));

        FunctionLogger.MethodInformation(_logger, "Validating Newday Service Tags");
        result.Errors.AddRange(ValidateNewDayServiceTagExistence(settings));

        FunctionLogger.MethodInformation(_logger, "Validating Azure Service Tags");
        result.Errors.AddRange(ValidateAzureServiceTagExistence(settings));

        FunctionLogger.MethodInformation(_logger, "Validating Resource ID Format for outbound and inbound");
        result.Errors.AddRange(ValidateResourceIdFormat(settings));

        FunctionLogger.MethodInformation(_logger, $"Validating Cross Subscription Allowance for Subscription ID: {settings.RequestSubscriptionId}");
        result.Errors.AddRange(ValidateCrossSubscriptionAllowance(settings));
      }
      catch (Exception ex)
      {
        result.Errors.Add($"Exception occured in validation: {ex.Message}");
        // Check and log the inner exception if present
        if (ex.InnerException != null)
        {
          result.Errors.Add($"Inner Exception: {ex.InnerException.Message}");
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
        FunctionLogger.MethodInformation(_logger, $"Validating Resource ID: {settings.ResourceId}");
        result.Errors.AddRange(ValidateMainResourceId(settings.ResourceId));

        FunctionLogger.MethodInformation(_logger, "Validating Resource ID Format for outbound and inbound");
        result.Errors.AddRange(ValidateResourceIdFormat(settings));

        FunctionLogger.MethodInformation(_logger,$"Validating Cross Subscription Allowance for Subscription ID: {settings.RequestSubscriptionId}");
        result.Errors.AddRange(ValidateCrossSubscriptionAllowance(settings));
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
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
      FunctionLogger.MethodStart(_logger, nameof(ValidateNewDayServiceTagExistence));
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
            FunctionLogger.MethodWarning(_logger, errorMessage);
          }
          else
          {
            string message = $"Validated NewDayInternalAndThirdPartyTag: {tagName}";
            FunctionLogger.MethodInformation(_logger, message);
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
      return errors;
    }
    public IEnumerable<string> ValidateAzureServiceTagExistence(ResourceDependencyInformation settings)
    {
      FunctionLogger.MethodStart(_logger, nameof(ValidateAzureServiceTagExistence));
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
            FunctionLogger.MethodWarning(_logger, errorMessage);
          }
          else
          {
            string message = $"Validated AzureServiceTag: {tagName}";
            FunctionLogger.MethodInformation(_logger, message);
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
      return errors;
    }

    public IEnumerable<string> ValidateResourceIdFormat(ResourceDependencyInformation settings)
    {
      List<string> errorList = new List<string>();
      try
      {
        FunctionLogger.MethodStart(_logger, nameof(ValidateResourceIdFormat));
        // Validate inbound resource IDs
        if (settings.AllowInbound?.SecurityRestrictions != null)
        {
          var inboundResourceIdErrors = ValidateInboundResourceIds(settings.AllowInbound.SecurityRestrictions.ResourceIds);
          if (inboundResourceIdErrors.Any())
          {
            foreach (var inboundResourceIdError in inboundResourceIdErrors)
            {
              string message = $"Inbound resource ID validation errors found, InboundResourceIdErrors:{inboundResourceIdError}";
              FunctionLogger.MethodError(_logger, message);

            }
          }
          errorList.AddRange(inboundResourceIdErrors);
        }
        // Check if outbound resource IDs are provided
        if (settings.AllowOutbound?.ResourceIds == null || !(settings.AllowOutbound?.ResourceIds.Length > 0))
        {
          FunctionLogger.MethodWarning(_logger, "No outbound resource IDs provided.");
          return errorList;
        }
        // Validate outbound resource allowance
        if (settings.ResourceId != null && !AreOutboundResourcesAllowed(settings.ResourceId, out string errorMessage))
        {
          errorList.Add(errorMessage);
          FunctionLogger.MethodError(_logger, errorMessage);
          return errorList;
        }
        // Validate outbound resource IDs
        var outboundResourceIdErrors = ValidateOutboundResourceIds(settings.AllowOutbound.ResourceIds);
        if (outboundResourceIdErrors.Any())
        {
          foreach (var outboundResourceIdError in outboundResourceIdErrors)
          {
            string message = $"Outbound resource ID validation errors found, OutboundResourceIdErrors:{outboundResourceIdError}";
            FunctionLogger.MethodError(_logger, message);
          }
        }
        errorList.AddRange(outboundResourceIdErrors);
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Log the final validation results
      if (errorList.Any())
      {
        FunctionLogger.MethodInformation(_logger, $"Resource ID Validation completed with {errorList.Count} errors.");
      }
      else
      {
        FunctionLogger.MethodWarning(_logger, "Validation completed successfully");
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
        FunctionLogger.MethodInformation(_logger, $"Validating Outbound ResourceID: {resourceId}");
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
        FunctionLogger.MethodInformation(_logger, $"Validating Inbound ResourceID: {resourceId}");
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
        FunctionLogger.MethodStart(_logger, nameof(ValidateCrossSubscriptionAllowance));
        
        var allowedCrossSubscriptionPool = GetAllowedCrossSubList(settings.RequestSubscriptionId!); //subscription list
        if (allowedCrossSubscriptionPool.Key == null)
        {
          string warning = $"No allowed cross-subscription pool found for the given request subscription ID: {settings.RequestSubscriptionId}";
          FunctionLogger.MethodWarning(_logger, warning);
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
              FunctionLogger.MethodWarning(_logger, errorMessage);
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
              FunctionLogger.MethodWarning(_logger, errorMessage);
            }
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      // Log the errors if any were found
      if (errorList.Count > 0)
      {
        FunctionLogger.MethodInformation(_logger, $"Cross subscription allowance validation completed with {errorList.Count} errors.");
        foreach (string error in errorList)
        {
          FunctionLogger.MethodWarning( _logger, error);
        }
      }
      else
      {
        FunctionLogger.MethodInformation(_logger, "Cross subscription allowance validation completed successfully with no errors.");
      }
      return errorList;
    }

    private KeyValuePair<string, Dictionary<string, string>> GetAllowedCrossSubList(string requestSubscriptionId)
    {
      string message = string.Empty;
      FunctionLogger.MethodStart(_logger, nameof(GetAllowedCrossSubList));

      FunctionLogger.MethodInformation(_logger, $"Request subscription ID:{requestSubscriptionId}");
      if (string.IsNullOrEmpty(requestSubscriptionId))
      {
        // Log the scenario where the subscription ID is null or empty
        message = $"Request subscription ID:{requestSubscriptionId} is null or empty.";
        FunctionLogger.MethodWarning(_logger, message);
        return default;
      }
      // Try to find the allowed cross-subscription list
      var result = AllowedCrossSubscription.Group
          .Where(x => x.Value.ContainsKey(requestSubscriptionId))
          .FirstOrDefault();
      // Log the outcome
      if (result.Key != null)
      {
        message = $"Found allowed cross-subscription entry, Environment: {result.Key}, SubscriptionName:{result.Value[requestSubscriptionId]}";
        FunctionLogger.MethodInformation( _logger, message);
      }
      else
      {
        message = "No allowed cross-subscription entry found for the provided subscription ID";
        FunctionLogger.MethodInformation(_logger, message);
      }
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