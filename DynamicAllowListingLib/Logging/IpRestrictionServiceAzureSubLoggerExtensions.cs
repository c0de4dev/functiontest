using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// Additional structured logging extensions for IpRestrictionServiceForAzureSubscription operations.
  /// Extends IpRestrictionServiceLoggerExtensions with Azure Subscription-specific logging methods.
  /// Uses LoggerMessage source generators for optimal performance.
  /// </summary>
  public static partial class IpRestrictionServiceAzureSubLoggerExtensions
  {
    // ============================================================
    // GetValidDependencyConfigs (EventIds 6140-6159)
    // ============================================================

    [LoggerMessage(
        EventId = 6140,
        Level = LogLevel.Information,
        Message = "Getting valid dependency configs | SubscriptionCount: {SubscriptionCount}")]
    public static partial void LogGettingValidDependencyConfigs(
        this ILogger logger,
        int subscriptionCount);

    [LoggerMessage(
        EventId = 6141,
        Level = LogLevel.Warning,
        Message = "Azure subscriptions model is null or empty | Returning empty config set")]
    public static partial void LogAzureSubscriptionsEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 6142,
        Level = LogLevel.Information,
        Message = "No related service tags found for Azure subscriptions | Returning empty config set")]
    public static partial void LogNoRelatedServiceTagsFound(this ILogger logger);

    [LoggerMessage(
        EventId = 6143,
        Level = LogLevel.Information,
        Message = "No dependency configurations found for service tags | Returning empty config set")]
    public static partial void LogNoDependencyConfigsFound(this ILogger logger);

    [LoggerMessage(
        EventId = 6144,
        Level = LogLevel.Warning,
        Message = "All resource configurations were invalid | Returning empty config set")]
    public static partial void LogAllConfigsInvalid(this ILogger logger);

    [LoggerMessage(
        EventId = 6145,
        Level = LogLevel.Information,
        Message = "Valid dependency configs retrieved | ValidCount: {ValidCount} | ResourceNames: {ResourceNames}")]
    public static partial void LogValidDependencyConfigsFound(
        this ILogger logger,
        int validCount,
        string resourceNames);

    [LoggerMessage(
        EventId = 6146,
        Level = LogLevel.Error,
        Message = "Failed to get valid dependency configs | Returning empty config set")]
    public static partial void LogGetValidDependencyConfigsFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // FindRelatedServiceTags (EventIds 6160-6179)
    // ============================================================

    [LoggerMessage(
        EventId = 6160,
        Level = LogLevel.Information,
        Message = "Finding related service tags | SubscriptionCount: {SubscriptionCount}")]
    public static partial void LogFindingRelatedServiceTags(
        this ILogger logger,
        int subscriptionCount);

    [LoggerMessage(
        EventId = 6161,
        Level = LogLevel.Warning,
        Message = "Azure subscription model list is null or empty")]
    public static partial void LogAzureSubscriptionModelEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 6162,
        Level = LogLevel.Information,
        Message = "Looking for service tags referencing subscriptions | SubscriptionNames: {SubscriptionNames}")]
    public static partial void LogLookingForServiceTags(
        this ILogger logger,
        string subscriptionNames);

    [LoggerMessage(
        EventId = 6163,
        Level = LogLevel.Warning,
        Message = "No service tags found in database")]
    public static partial void LogNoServiceTagsInDatabase(this ILogger logger);

    [LoggerMessage(
        EventId = 6164,
        Level = LogLevel.Warning,
        Message = "No service tags found referencing updated Azure subscriptions")]
    public static partial void LogNoMatchingServiceTagsFound(this ILogger logger);

    [LoggerMessage(
        EventId = 6165,
        Level = LogLevel.Information,
        Message = "Related service tags found | TagCount: {TagCount} | TagNames: {TagNames}")]
    public static partial void LogRelatedServiceTagsFound(
        this ILogger logger,
        int tagCount,
        string tagNames);

    [LoggerMessage(
        EventId = 6166,
        Level = LogLevel.Error,
        Message = "Failed to find related service tags")]
    public static partial void LogFindRelatedServiceTagsFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // Scoped Logging Helpers for Azure Subscription Operations
    // ============================================================

    /// <summary>
    /// Creates a logging scope for Azure subscription-based operations.
    /// </summary>
    public static IDisposable? BeginAzureSubscriptionOperationScope(
        this ILogger logger,
        string methodName,
        int subscriptionCount)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ServiceName"] = "IpRestrictionServiceForAzureSubscription",
        ["MethodName"] = methodName,
        ["SubscriptionCount"] = subscriptionCount,
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for service tag lookup operations.
    /// </summary>
    public static IDisposable? BeginServiceTagLookupScope(
        this ILogger logger,
        string subscriptionNames)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "ServiceTagLookup",
        ["SubscriptionNames"] = subscriptionNames,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }
  }
}