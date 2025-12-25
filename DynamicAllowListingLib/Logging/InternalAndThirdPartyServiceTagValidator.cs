using Microsoft.Extensions.Logging;
using System;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// Structured logging extensions for InternalAndThirdPartyServiceTagValidator.
  /// Uses LoggerMessage source generators for optimal performance.
  /// </summary>
  public static partial class InternalAndThirdPartyServiceTagValidatorLoggerExtensions
  {
    // Validation lifecycle (EventIds 12000-12019)

    [LoggerMessage(
        EventId = 12000,
        Level = LogLevel.Information,
        Message = "Starting validation method {MethodName}")]
    public static partial void LogValidationMethodStart(
        this ILogger logger,
        string methodName);

    [LoggerMessage(
        EventId = 12001,
        Level = LogLevel.Information,
        Message = "Validation stage: {StageName}")]
    public static partial void LogValidationStage(
        this ILogger logger,
        string stageName);

    [LoggerMessage(
        EventId = 12002,
        Level = LogLevel.Error,
        Message = "Exception in validation method {MethodName}")]
    public static partial void LogValidationException(
        this ILogger logger,
        Exception exception,
        string methodName);

    // Service tag validations (EventIds 12020-12049)

    [LoggerMessage(
        EventId = 12020,
        Level = LogLevel.Warning,
        Message = "No ServiceTags provided for {Context}.")]
    public static partial void LogNoServiceTags(
        this ILogger logger,
        string context);

    [LoggerMessage(
        EventId = 12021,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no AddressPrefixes defined.")]
    public static partial void LogMissingAddressPrefixes(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 12022,
        Level = LogLevel.Warning,
        Message = "Invalid or null AddressPrefix '{AddressPrefix}' in ServiceTag '{ServiceTagName}'.")]
    public static partial void LogInvalidAddressPrefix(
        this ILogger logger,
        string? serviceTagName,
        string? addressPrefix);

    [LoggerMessage(
        EventId = 12023,
        Level = LogLevel.Warning,
        Message = "Overlapping AddressPrefix detected | ServiceTag: {ServiceTagName} | AddressPrefix: {AddressPrefix}")]
    public static partial void LogOverlappingAddressPrefix(
        this ILogger logger,
        string? serviceTagName,
        string addressPrefix);

    [LoggerMessage(
        EventId = 12024,
        Level = LogLevel.Information,
        Message = "Address range validation completed with {WarningCount} warnings.")]
    public static partial void LogAddressOverlapValidationSummary(
        this ILogger logger,
        int warningCount);

    [LoggerMessage(
        EventId = 12025,
        Level = LogLevel.Information,
        Message = "Address range validation completed successfully with no overlapping ranges.")]
    public static partial void LogAddressOverlapValidationSuccess(
        this ILogger logger);

    // Allowed subscription validations (EventIds 12050-12079)

    [LoggerMessage(
        EventId = 12050,
        Level = LogLevel.Error,
        Message = "No Azure subscriptions are available for validation.")]
    public static partial void LogNoAzureSubscriptions(
        this ILogger logger);

    [LoggerMessage(
        EventId = 12051,
        Level = LogLevel.Information,
        Message = "Available Azure subscriptions: {SubscriptionNames}")]
    public static partial void LogAvailableSubscriptions(
        this ILogger logger,
        string subscriptionNames);

    [LoggerMessage(
        EventId = 12052,
        Level = LogLevel.Error,
        Message = "No ServiceTags are provided for allowed subscription validation.")]
    public static partial void LogNoServiceTagsForAllowedSubscriptionValidation(
        this ILogger logger);

    [LoggerMessage(
        EventId = 12053,
        Level = LogLevel.Warning,
        Message = "Null/Empty 'ServiceTags.AllowedSubscriptions' value. ServiceTag: {ServiceTagName}")]
    public static partial void LogMissingAllowedSubscriptions(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 12054,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.AllowedSubscriptions'. SubscriptionName: {SubscriptionName} Tag: {ServiceTagName}")]
    public static partial void LogInvalidAllowedSubscription(
        this ILogger logger,
        string? subscriptionName,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 12055,
        Level = LogLevel.Information,
        Message = "Subscription allowed Service Tag Validation completed with {ErrorCount} errors.")]
    public static partial void LogAllowedSubscriptionsValidationSummary(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 12056,
        Level = LogLevel.Information,
        Message = "Subscription allowed Service Tag validation completed successfully with no errors.")]
    public static partial void LogAllowedSubscriptionsValidationSuccess(
        this ILogger logger);

    // Service tag IP validations (EventIds 12080-12109)

    [LoggerMessage(
        EventId = 12080,
        Level = LogLevel.Warning,
        Message = "Null/Empty 'ServiceTags.Name' value.")]
    public static partial void LogMissingServiceTagName(
        this ILogger logger);

    [LoggerMessage(
        EventId = 12081,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no SubnetIds defined.")]
    public static partial void LogMissingSubnetIds(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 12082,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.Subnet' value. ServiceTags.Name: {ServiceTagName}, SubnetId: {SubnetId}")]
    public static partial void LogInvalidSubnetId(
        this ILogger logger,
        string? serviceTagName,
        string? subnetId);

    [LoggerMessage(
        EventId = 12083,
        Level = LogLevel.Warning,
        Message = "Service Tag IP Address Validation completed with {ErrorCount} errors")]
    public static partial void LogServiceTagIpValidationSummary(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 12084,
        Level = LogLevel.Information,
        Message = "Service Tag IP Address Validation completed successfully with no errors.")]
    public static partial void LogServiceTagIpValidationSuccess(
        this ILogger logger);

    // Azure subscription validations (EventIds 12110-12139)

    [LoggerMessage(
        EventId = 12110,
        Level = LogLevel.Warning,
        Message = "AzureSubscription validation failed. A minimum of {RequiredCount} subscriptions with valid 'Id' and 'Name' is required.")]
    public static partial void LogInvalidSubscriptionConfiguration(
        this ILogger logger,
        int requiredCount);

    [LoggerMessage(
        EventId = 12111,
        Level = LogLevel.Warning,
        Message = "AzureSubscription has missing 'Id' or 'Name'. SubscriptionName: {SubscriptionName}")]
    public static partial void LogMissingSubscriptionFields(
        this ILogger logger,
        string? subscriptionName);

    [LoggerMessage(
        EventId = 12112,
        Level = LogLevel.Warning,
        Message = "Invalid 'AzureSubscription.Id' value. AzureSubscription.Id must be a valid GUID. AzureSubscription.Name: {SubscriptionName}")]
    public static partial void LogInvalidSubscriptionId(
        this ILogger logger,
        string? subscriptionName,
        string? subscriptionId);

    [LoggerMessage(
        EventId = 12113,
        Level = LogLevel.Information,
        Message = "Validated 'AzureSubscription.Id' as valid GUID. AzureSubscription.Id: {SubscriptionId}, AzureSubscription.Name: {SubscriptionName}")]
    public static partial void LogValidatedSubscriptionId(
        this ILogger logger,
        string? subscriptionId,
        string? subscriptionName);

    [LoggerMessage(
        EventId = 12114,
        Level = LogLevel.Warning,
        Message = "Azure Subscription Validation failed with {ErrorCount} errors")]
    public static partial void LogAzureSubscriptionValidationSummary(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 12115,
        Level = LogLevel.Information,
        Message = "Azure Subscription Validation completed successfully with no errors.")]
    public static partial void LogAzureSubscriptionValidationSuccess(
        this ILogger logger);
  }
}