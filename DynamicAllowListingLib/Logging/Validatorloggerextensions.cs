using Microsoft.Extensions.Logging;
using System;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Settings Validation operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// EVENT ID RANGE: 11000-11499
  /// </summary>
  public static partial class ValidatorLoggerExtensions
  {
    // ============================================================
    // InternalAndThirdPartyServiceTagValidator (EventIds 11000-11199)
    // ============================================================

    #region Validate Method (11000-11009)

    [LoggerMessage(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Validating Azure Subscriptions Parameters")]
    public static partial void LogValidatingAzureSubscriptionParameters(this ILogger logger);

    [LoggerMessage(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Validating Service Tag IP Addresses")]
    public static partial void LogValidatingServiceTagIPAddresses(this ILogger logger);

    [LoggerMessage(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Validating Subscriptions Tag IP Addresses")]
    public static partial void LogValidatingAllowedSubscriptionTags(this ILogger logger);

    [LoggerMessage(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Validating Overlapping IP Addresses")]
    public static partial void LogValidatingOverlappingIPAddresses(this ILogger logger);

    [LoggerMessage(
        EventId = 11004,
        Level = LogLevel.Error,
        Message = "Exception occurred during validation")]
    public static partial void LogValidationException(
        this ILogger logger,
        Exception exception);

    #endregion

    #region ValidateAddressRangeOverlapping (11010-11029)

    [LoggerMessage(
        EventId = 11010,
        Level = LogLevel.Warning,
        Message = "No ServiceTags provided in the settings.")]
    public static partial void LogNoServiceTagsInSettings(this ILogger logger);

    [LoggerMessage(
        EventId = 11011,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no AddressPrefixes defined.")]
    public static partial void LogServiceTagNoAddressPrefixes(
        this ILogger logger,
        string serviceTagName);

    [LoggerMessage(
        EventId = 11012,
        Level = LogLevel.Warning,
        Message = "Overlapping 'ServiceTags.AddressPrefixes' detected. ServiceTags.Name: {ServiceTagName}, AddressPrefix: {AddressPrefix}")]
    public static partial void LogOverlappingAddressPrefixDetected(
        this ILogger logger,
        string serviceTagName,
        string addressPrefix);

    [LoggerMessage(
        EventId = 11013,
        Level = LogLevel.Warning,
        Message = "Invalid or null AddressPrefix '{AddressPrefix}' in ServiceTag '{ServiceTagName}'.")]
    public static partial void LogInvalidAddressPrefix(
        this ILogger logger,
        string? addressPrefix,
        string serviceTagName);

    [LoggerMessage(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Address range validation completed with {WarningCount} warnings.")]
    public static partial void LogAddressRangeValidationWithWarnings(
        this ILogger logger,
        int warningCount);

    [LoggerMessage(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Address range validation completed successfully with no overlapping ranges.")]
    public static partial void LogAddressRangeValidationSuccess(this ILogger logger);

    #endregion

    #region ValidateAllowedSubscriptionTags (11030-11049)

    [LoggerMessage(
        EventId = 11030,
        Level = LogLevel.Error,
        Message = "No Azure subscriptions are available for validation.")]
    public static partial void LogNoAzureSubscriptionsForValidation(this ILogger logger);

    [LoggerMessage(
        EventId = 11031,
        Level = LogLevel.Information,
        Message = "Available Azure subscriptions: {SubscriptionList}")]
    public static partial void LogAvailableAzureSubscriptions(
        this ILogger logger,
        string subscriptionList);

    [LoggerMessage(
        EventId = 11032,
        Level = LogLevel.Error,
        Message = "No ServiceTags are provided in the settings.")]
    public static partial void LogNoServiceTagsProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 11033,
        Level = LogLevel.Warning,
        Message = "Null/Empty 'ServiceTags.AllowedSubscriptions' value. ServiceTag: {ServiceTagName}")]
    public static partial void LogNullOrEmptyAllowedSubscriptions(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11034,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.AllowedSubscriptions'. SubscriptionName: {SubscriptionName} Tag: {ServiceTagName}")]
    public static partial void LogInvalidAllowedSubscription(
        this ILogger logger,
        string? subscriptionName,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11035,
        Level = LogLevel.Information,
        Message = "Subscription allowed Service Tag Validation completed with {ErrorCount} errors.")]
    public static partial void LogAllowedSubscriptionValidationWithErrors(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 11036,
        Level = LogLevel.Information,
        Message = "Subscription allowed Service Tag validation completed successfully with no errors.")]
    public static partial void LogAllowedSubscriptionValidationSuccess(this ILogger logger);

    #endregion

    #region ValidateServiceTagIPAddresses (11050-11069)

    [LoggerMessage(
        EventId = 11050,
        Level = LogLevel.Error,
        Message = "No ServiceTags provided in the settings.")]
    public static partial void LogNoServiceTagsProvidedForIPValidation(this ILogger logger);

    [LoggerMessage(
        EventId = 11051,
        Level = LogLevel.Warning,
        Message = "Null/Empty 'ServiceTags.Name' value.")]
    public static partial void LogNullOrEmptyServiceTagName(this ILogger logger);

    [LoggerMessage(
        EventId = 11052,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no AddressPrefixes defined.")]
    public static partial void LogNoAddressPrefixesDefined(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11053,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.AddressPrefixes' value. ServiceTags.Name: {ServiceTagName}, IPAddress: {IPAddress}")]
    public static partial void LogInvalidAddressPrefixValue(
        this ILogger logger,
        string? serviceTagName,
        string? ipAddress);

    [LoggerMessage(
        EventId = 11054,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no SubnetIds defined.")]
    public static partial void LogNoSubnetIdsDefined(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11055,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.Subnet' value. ServiceTags.Name: {ServiceTagName}, SubnetId: {SubnetId}")]
    public static partial void LogInvalidSubnetIdValue(
        this ILogger logger,
        string? serviceTagName,
        string? subnetId);

    [LoggerMessage(
        EventId = 11056,
        Level = LogLevel.Warning,
        Message = "Service Tag IP Address Validation completed with {ErrorCount} errors")]
    public static partial void LogIPAddressValidationWithErrors(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 11057,
        Level = LogLevel.Information,
        Message = "Service Tag IP Address Validation completed successfully with no errors.")]
    public static partial void LogIPAddressValidationSuccess(this ILogger logger);

    #endregion

    #region ValidateAzureSubscriptionParameters (11070-11089)

    [LoggerMessage(
        EventId = 11070,
        Level = LogLevel.Error,
        Message = "No AzureSubscriptions defined in the settings.")]
    public static partial void LogNoAzureSubscriptionsDefined(this ILogger logger);

    [LoggerMessage(
        EventId = 11071,
        Level = LogLevel.Warning,
        Message = "AzureSubscription validation failed. A minimum of {MinCount} subscriptions with valid 'Id' and 'Name' is required.")]
    public static partial void LogAzureSubscriptionValidationFailed(
        this ILogger logger,
        int minCount);

    [LoggerMessage(
        EventId = 11072,
        Level = LogLevel.Warning,
        Message = "AzureSubscription has missing 'Id' or 'Name'. SubscriptionName: {SubscriptionName}")]
    public static partial void LogMissingIdOrName(
        this ILogger logger,
        string subscriptionName);

    [LoggerMessage(
        EventId = 11073,
        Level = LogLevel.Warning,
        Message = "Invalid 'AzureSubscription.Id' value. AzureSubscription.Id must be a valid GUID. AzureSubscription.Name: {SubscriptionName}")]
    public static partial void LogInvalidSubscriptionId(
        this ILogger logger,
        string? subscriptionName);

    [LoggerMessage(
        EventId = 11074,
        Level = LogLevel.Information,
        Message = "Validated 'AzureSubscription.Id' as valid GUID. AzureSubscription.Id: {SubscriptionId}, AzureSubscription.Name: {SubscriptionName}")]
    public static partial void LogValidSubscriptionId(
        this ILogger logger,
        string? subscriptionId,
        string? subscriptionName);

    [LoggerMessage(
        EventId = 11075,
        Level = LogLevel.Warning,
        Message = "Azure Subscription Validation failed with {ErrorCount} errors")]
    public static partial void LogAzureSubscriptionValidationWithErrors(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 11076,
        Level = LogLevel.Information,
        Message = "Azure Subscription Validation completed successfully with no errors.")]
    public static partial void LogAzureSubscriptionValidationSuccess(this ILogger logger);

    #endregion

    // ============================================================
    // ResourceDependencyInformationValidator (EventIds 11200-11399)
    // ============================================================

    #region Validate Method (11200-11219)

    [LoggerMessage(
        EventId = 11200,
        Level = LogLevel.Information,
        Message = "Validating Resource ID: {ResourceId}")]
    public static partial void LogValidatingResourceId(
        this ILogger logger,
        string? resourceId);

    [LoggerMessage(
        EventId = 11201,
        Level = LogLevel.Information,
        Message = "Validating Newday Service Tags")]
    public static partial void LogValidatingNewdayServiceTags(this ILogger logger);

    [LoggerMessage(
        EventId = 11202,
        Level = LogLevel.Information,
        Message = "Validating Azure Service Tags")]
    public static partial void LogValidatingAzureServiceTags(this ILogger logger);

    [LoggerMessage(
        EventId = 11203,
        Level = LogLevel.Information,
        Message = "Validating Resource ID Format for outbound and inbound")]
    public static partial void LogValidatingResourceIdFormat(this ILogger logger);

    [LoggerMessage(
        EventId = 11204,
        Level = LogLevel.Information,
        Message = "Validating Cross Subscription Allowance for Subscription ID: {SubscriptionId}")]
    public static partial void LogValidatingCrossSubscriptionAllowance(
        this ILogger logger,
        string? subscriptionId);

    [LoggerMessage(
        EventId = 11205,
        Level = LogLevel.Error,
        Message = "Exception occurred in validation: {ExceptionMessage}")]
    public static partial void LogValidationExceptionWithMessage(
        this ILogger logger,
        string exceptionMessage);

    [LoggerMessage(
        EventId = 11206,
        Level = LogLevel.Error,
        Message = "Inner Exception: {InnerExceptionMessage}")]
    public static partial void LogInnerException(
        this ILogger logger,
        string innerExceptionMessage);

    #endregion

    #region ValidateNewDayServiceTagExistence (11220-11239)

    [LoggerMessage(
        EventId = 11220,
        Level = LogLevel.Warning,
        Message = "Invalid/Null 'NewDayInternalAndThirdPartyTags' value. TagName: {TagName}")]
    public static partial void LogInvalidNewDayServiceTag(
        this ILogger logger,
        string tagName);

    [LoggerMessage(
        EventId = 11221,
        Level = LogLevel.Information,
        Message = "Validated NewDayInternalAndThirdPartyTag: {TagName}")]
    public static partial void LogValidatedNewDayServiceTag(
        this ILogger logger,
        string tagName);

    #endregion

    #region ValidateAzureServiceTagExistence (11240-11259)

    [LoggerMessage(
        EventId = 11240,
        Level = LogLevel.Warning,
        Message = "Invalid/Null 'AzureServiceTag' value. TagName: {TagName}")]
    public static partial void LogInvalidAzureServiceTag(
        this ILogger logger,
        string tagName);

    [LoggerMessage(
        EventId = 11241,
        Level = LogLevel.Information,
        Message = "Validated AzureServiceTag: {TagName}")]
    public static partial void LogValidatedAzureServiceTag(
        this ILogger logger,
        string tagName);

    #endregion

    #region ValidateResourceIdFormat (11260-11279)

    [LoggerMessage(
        EventId = 11260,
        Level = LogLevel.Error,
        Message = "Inbound resource ID validation errors found, InboundResourceIdErrors:{InboundResourceIdError}")]
    public static partial void LogInboundResourceIdError(
        this ILogger logger,
        string inboundResourceIdError);

    [LoggerMessage(
        EventId = 11261,
        Level = LogLevel.Warning,
        Message = "No outbound resource IDs provided.")]
    public static partial void LogNoOutboundResourceIds(this ILogger logger);

    [LoggerMessage(
        EventId = 11262,
        Level = LogLevel.Information,
        Message = "Validating {Count} outbound resource IDs.")]
    public static partial void LogValidatingOutboundResourceIds(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 11263,
        Level = LogLevel.Error,
        Message = "Outbound resource ID validation errors found, OutboundResourceIdErrors:{OutboundResourceIdError}")]
    public static partial void LogOutboundResourceIdError(
        this ILogger logger,
        string outboundResourceIdError);

    [LoggerMessage(
        EventId = 11264,
        Level = LogLevel.Information,
        Message = "Resource ID format validation completed successfully.")]
    public static partial void LogResourceIdFormatValidationSuccess(this ILogger logger);

    #endregion

    #region ValidateCrossSubscriptionAllowance (11280-11299)

    [LoggerMessage(
        EventId = 11280,
        Level = LogLevel.Information,
        Message = "Validating cross-subscription allowance | ResourceId: {ResourceId} | SubscriptionId: {SubscriptionId}")]
    public static partial void LogValidatingCrossSubscription(
        this ILogger logger,
        string? resourceId,
        string? subscriptionId);

    [LoggerMessage(
        EventId = 11281,
        Level = LogLevel.Warning,
        Message = "Cross-subscription access detected | ResourceSubscription: {ResourceSubscription} | RequestSubscription: {RequestSubscription}")]
    public static partial void LogCrossSubscriptionDetected(
        this ILogger logger,
        string resourceSubscription,
        string requestSubscription);

    [LoggerMessage(
        EventId = 11282,
        Level = LogLevel.Information,
        Message = "Cross-subscription allowance validation passed.")]
    public static partial void LogCrossSubscriptionValidationPassed(this ILogger logger);

    #endregion

    #region ValidateFormat Method (11300-11319)

    [LoggerMessage(
        EventId = 11300,
        Level = LogLevel.Information,
        Message = "Validating format for Resource ID: {ResourceId}")]
    public static partial void LogValidatingFormatResourceId(
        this ILogger logger,
        string? resourceId);

    [LoggerMessage(
        EventId = 11301,
        Level = LogLevel.Information,
        Message = "Validating Resource ID Format for outbound and inbound (Format validation)")]
    public static partial void LogValidatingResourceIdFormatOnly(this ILogger logger);

    [LoggerMessage(
        EventId = 11302,
        Level = LogLevel.Information,
        Message = "Validating Cross Subscription Allowance for Subscription ID: {SubscriptionId} (Format validation)")]
    public static partial void LogValidatingCrossSubscriptionFormat(
        this ILogger logger,
        string? subscriptionId);

    #endregion
  }
}