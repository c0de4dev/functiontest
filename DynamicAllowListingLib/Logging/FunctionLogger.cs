using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  public static partial class LoggerExtensions
  {
    // Resource Operations
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Starting {MethodName} | SubscriptionIds: {SubscriptionIds} | ResourceCount: {ResourceCount}")]
    public static partial void LogResourceOperationStart(
        this ILogger logger,
        string methodName,
        string subscriptionIds,
        int resourceCount);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Completed {MethodName} | ResourcesRetrieved: {ResourcesRetrieved} | Duration: {DurationMs}ms")]
    public static partial void LogResourceOperationComplete(
        this ILogger logger,
        string methodName,
        int resourcesRetrieved,
        long durationMs);

    // Network Restrictions
    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Applying network restrictions | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogNetworkRestrictionStart(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Information,
        Message = "Network restrictions applied | ResourceId: {ResourceId} | Duration: {DurationMs}ms")]
    public static partial void LogNetworkRestrictionComplete(
        this ILogger logger,
        string resourceId,
        long durationMs);

    // Service Tag Operations
    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Information,
        Message = "Generating rules for service tag | ServiceTag: {ServiceTag} | SubscriptionId: {SubscriptionId}")]
    public static partial void LogServiceTagRuleGeneration(
        this ILogger logger,
        string serviceTag,
        string subscriptionId);

    // Database Operations
    [LoggerMessage(
        EventId = 1301,
        Level = LogLevel.Information,
        Message = "Database operation | Operation: {Operation} | DocumentId: {DocumentId}")]
    public static partial void LogDatabaseOperation(
        this ILogger logger,
        string operation,
        string documentId);

    [LoggerMessage(
        EventId = 1302,
        Level = LogLevel.Error,
        Message = "Database operation failed | Operation: {Operation} | DocumentId: {DocumentId}")]
    public static partial void LogDatabaseOperationFailed(
        this ILogger logger,
        Exception exception,
        string operation,
        string documentId);

    // HTTP Operations
    [LoggerMessage(
        EventId = 1401,
        Level = LogLevel.Information,
        Message = "HTTP {Method} Request | Url: {Url}")]
    public static partial void LogHttpRequest(
        this ILogger logger,
        string method,
        string url);

    [LoggerMessage(
        EventId = 1402,
        Level = LogLevel.Information,
        Message = "HTTP {Method} Success | Url: {Url} | StatusCode: {StatusCode} | Duration: {DurationMs}ms")]
    public static partial void LogHttpSuccess(
        this ILogger logger,
        string method,
        string url,
        int statusCode,
        long durationMs);

    [LoggerMessage(
        EventId = 1403,
        Level = LogLevel.Error,
        Message = "HTTP {Method} Failed | Url: {Url} | Duration: {DurationMs}ms")]
    public static partial void LogHttpFailed(
        this ILogger logger,
        Exception exception,
        string method,
        string url,
        long durationMs);

    // Validation
    [LoggerMessage(
        EventId = 1501,
        Level = LogLevel.Warning,
        Message = "Validation warning | Entity: {Entity} | Field: {Field} | Issue: {Issue}")]
    public static partial void LogValidationWarning(
        this ILogger logger,
        string entity,
        string field,
        string issue);

    [LoggerMessage(
        EventId = 1502,
        Level = LogLevel.Error,
        Message = "Validation failed | Entity: {Entity} | ErrorCount: {ErrorCount}")]
    public static partial void LogValidationFailed(
        this ILogger logger,
        string entity,
        int errorCount);

    // Generic Operations
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Error,
        Message = "Operation failed | Method: {MethodName} | ResourceId: {ResourceId}")]
    public static partial void LogOperationFailed(
        this ILogger logger,
        Exception exception,
        string methodName,
        string resourceId);

    // Custom scope logging
    public static void LogOperationException(
        this ILogger logger,
        Exception ex,
        string methodName,
        ResourceDependencyInformation? resourceInfo = null,
        Dictionary<string, object>? additionalContext = null)
    {
      var context = new Dictionary<string, object>
      {
        ["MethodName"] = methodName,
        ["ExceptionType"] = ex.GetType().Name,
        ["StackTrace"] = ex.StackTrace ?? "No stack trace available"
      };

      if (resourceInfo != null)
      {
        context["ResourceId"] = resourceInfo.ResourceId ?? "Unknown";
        context["ResourceName"] = resourceInfo.ResourceName ?? "Unknown";
        context["ResourceType"] = resourceInfo.ResourceType ?? "Unknown";
        context["SubscriptionId"] = resourceInfo.RequestSubscriptionId ?? "Unknown";
      }

      if (additionalContext != null)
      {
        foreach (var kvp in additionalContext)
        {
          context[kvp.Key] = kvp.Value;
        }
      }

      using (logger.BeginScope(context))
      {
        logger.LogError(ex, "Operation failed in {MethodName}", methodName);
      }
    }
  }
}