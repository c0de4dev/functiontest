using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for helper classes.
  /// Uses LoggerMessage source generators for optimal performance.
  /// </summary>
  public static partial class HelpersLoggerExtensions
  {
    // ============================================================
    // RestHelper - HTTP Operations (EventIds 8000-8099)
    // ============================================================

    #region HTTP Request Lifecycle

    [LoggerMessage(
        EventId = 8000,
        Level = LogLevel.Information,
        Message = "HTTP {Method} starting | Url: {Url}")]
    public static partial void LogHttpRequestStart(
        this ILogger logger,
        string method,
        string url);

    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Information,
        Message = "HTTP {Method} completed | Url: {Url} | StatusCode: {StatusCode}")]
    public static partial void LogHttpRequestComplete(
        this ILogger logger,
        string method,
        string url,
        int statusCode);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Error,
        Message = "HTTP {Method} failed | Url: {Url}")]
    public static partial void LogHttpRequestFailed(
        this ILogger logger,
        Exception exception,
        string method,
        string url);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Warning,
        Message = "HTTP {Method} retry | Url: {Url} | RetryCount: {RetryCount}")]
    public static partial void LogHttpRetry(
        this ILogger logger,
        string method,
        string url,
        int retryCount);

    #endregion

    #region HTTP GET Operations

    [LoggerMessage(
        EventId = 8010,
        Level = LogLevel.Information,
        Message = "HTTP GET | Url: {Url}")]
    public static partial void LogHttpGet(
        this ILogger logger,
        string url);

    [LoggerMessage(
        EventId = 8011,
        Level = LogLevel.Information,
        Message = "HTTP GET success | Url: {Url} | ResponseSize: {ResponseSize} bytes")]
    public static partial void LogHttpGetSuccess(
        this ILogger logger,
        string url,
        int responseSize);

    [LoggerMessage(
        EventId = 8012,
        Level = LogLevel.Error,
        Message = "HTTP GET failed | Url: {Url} | ErrorType: {ErrorType}")]
    public static partial void LogHttpGetFailed(
        this ILogger logger,
        Exception exception,
        string url,
        string errorType);

    #endregion

    #region HTTP POST Operations

    [LoggerMessage(
        EventId = 8020,
        Level = LogLevel.Information,
        Message = "HTTP POST | Url: {Url} | RequestBodySize: {RequestBodySize} bytes")]
    public static partial void LogHttpPost(
        this ILogger logger,
        string url,
        int requestBodySize);

    [LoggerMessage(
        EventId = 8021,
        Level = LogLevel.Information,
        Message = "HTTP POST success | Url: {Url} | ResponseSize: {ResponseSize} bytes")]
    public static partial void LogHttpPostSuccess(
        this ILogger logger,
        string url,
        int responseSize);

    [LoggerMessage(
        EventId = 8022,
        Level = LogLevel.Error,
        Message = "HTTP POST failed | Url: {Url} | ErrorType: {ErrorType}")]
    public static partial void LogHttpPostFailed(
        this ILogger logger,
        Exception exception,
        string url,
        string errorType);

    #endregion

    #region HTTP PUT Operations

    [LoggerMessage(
        EventId = 8030,
        Level = LogLevel.Information,
        Message = "HTTP PUT | Url: {Url} | RequestBodySize: {RequestBodySize} bytes")]
    public static partial void LogHttpPut(
        this ILogger logger,
        string url,
        int requestBodySize);

    [LoggerMessage(
        EventId = 8031,
        Level = LogLevel.Information,
        Message = "HTTP PUT success | Url: {Url} | ResponseSize: {ResponseSize} bytes")]
    public static partial void LogHttpPutSuccess(
        this ILogger logger,
        string url,
        int responseSize);

    [LoggerMessage(
        EventId = 8032,
        Level = LogLevel.Error,
        Message = "HTTP PUT failed | Url: {Url} | ErrorType: {ErrorType}")]
    public static partial void LogHttpPutFailed(
        this ILogger logger,
        Exception exception,
        string url,
        string errorType);

    #endregion

    #region HTTP PATCH Operations

    [LoggerMessage(
        EventId = 8040,
        Level = LogLevel.Information,
        Message = "HTTP PATCH | Url: {Url} | RequestBodySize: {RequestBodySize} bytes")]
    public static partial void LogHttpPatch(
        this ILogger logger,
        string url,
        int requestBodySize);

    [LoggerMessage(
        EventId = 8041,
        Level = LogLevel.Information,
        Message = "HTTP PATCH success | Url: {Url} | ResponseSize: {ResponseSize} bytes")]
    public static partial void LogHttpPatchSuccess(
        this ILogger logger,
        string url,
        int responseSize);

    [LoggerMessage(
        EventId = 8042,
        Level = LogLevel.Error,
        Message = "HTTP PATCH failed | Url: {Url} | ErrorType: {ErrorType}")]
    public static partial void LogHttpPatchFailed(
        this ILogger logger,
        Exception exception,
        string url,
        string errorType);

    #endregion

    #region HTTP DELETE Operations

    [LoggerMessage(
        EventId = 8050,
        Level = LogLevel.Information,
        Message = "HTTP DELETE | Url: {Url}")]
    public static partial void LogHttpDelete(
        this ILogger logger,
        string url);

    [LoggerMessage(
        EventId = 8051,
        Level = LogLevel.Information,
        Message = "HTTP DELETE success | Url: {Url}")]
    public static partial void LogHttpDeleteSuccess(
        this ILogger logger,
        string url);

    [LoggerMessage(
        EventId = 8052,
        Level = LogLevel.Error,
        Message = "HTTP DELETE failed | Url: {Url} | ErrorType: {ErrorType}")]
    public static partial void LogHttpDeleteFailed(
        this ILogger logger,
        Exception exception,
        string url,
        string errorType);

    #endregion

    #region Access Token Operations

    [LoggerMessage(
        EventId = 8060,
        Level = LogLevel.Debug,
        Message = "Retrieving access token | BaseUrl: {BaseUrl}")]
    public static partial void LogAccessTokenRequest(
        this ILogger logger,
        string baseUrl);

    [LoggerMessage(
        EventId = 8061,
        Level = LogLevel.Debug,
        Message = "Using cached access token | ExpiresIn: {ExpiresInMinutes} minutes")]
    public static partial void LogAccessTokenCached(
        this ILogger logger,
        double expiresInMinutes);

    [LoggerMessage(
        EventId = 8062,
        Level = LogLevel.Information,
        Message = "Access token refreshed | ExpiresAt: {ExpiresAt}")]
    public static partial void LogAccessTokenRefreshed(
        this ILogger logger,
        DateTimeOffset expiresAt);

    [LoggerMessage(
        EventId = 8063,
        Level = LogLevel.Error,
        Message = "Access token retrieval failed | BaseUrl: {BaseUrl}")]
    public static partial void LogAccessTokenFailed(
        this ILogger logger,
        Exception exception,
        string baseUrl);

    #endregion

    #region HTTP Request Creation

    [LoggerMessage(
        EventId = 8070,
        Level = LogLevel.Debug,
        Message = "Creating HTTP request | Method: {Method} | Url: {Url}")]
    public static partial void LogHttpRequestCreation(
        this ILogger logger,
        string method,
        string url);

    [LoggerMessage(
        EventId = 8071,
        Level = LogLevel.Debug,
        Message = "HTTP request created with content | Method: {Method} | Url: {Url} | ContentLength: {ContentLength} bytes")]
    public static partial void LogHttpRequestCreatedWithContent(
        this ILogger logger,
        string method,
        string url,
        int contentLength);

    #endregion

    // ============================================================
    // AzureResourceJsonConvertor (EventIds 8100-8149)
    // ============================================================

    #region JSON Conversion Operations

    [LoggerMessage(
        EventId = 8100,
        Level = LogLevel.Debug,
        Message = "Starting JSON conversion | ExpectedType: {ExpectedType}")]
    public static partial void LogJsonConversionStart(
        this ILogger logger,
        string expectedType);

    [LoggerMessage(
        EventId = 8101,
        Level = LogLevel.Debug,
        Message = "JSON conversion completed | ResourceCount: {ResourceCount}")]
    public static partial void LogJsonConversionComplete(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 8102,
        Level = LogLevel.Error,
        Message = "JSON conversion failed | ExpectedType: {ExpectedType}")]
    public static partial void LogJsonConversionFailed(
        this ILogger logger,
        Exception exception,
        string expectedType);

    [LoggerMessage(
        EventId = 8103,
        Level = LogLevel.Debug,
        Message = "Processing resource row | ResourceType: {ResourceType} | ResourceId: {ResourceId}")]
    public static partial void LogResourceRowProcessing(
        this ILogger logger,
        string resourceType,
        string resourceId);

    [LoggerMessage(
        EventId = 8104,
        Level = LogLevel.Warning,
        Message = "Resource type not implemented | ResourceType: {ResourceType}")]
    public static partial void LogResourceTypeNotImplemented(
        this ILogger logger,
        string resourceType);

    [LoggerMessage(
        EventId = 8105,
        Level = LogLevel.Warning,
        Message = "Missing required field in resource row | FieldName: {FieldName}")]
    public static partial void LogMissingRequiredField(
        this ILogger logger,
        string fieldName);

    #endregion

    // ============================================================
    // IpRulesHelper (EventIds 8150-8179)
    // ============================================================

    #region IP Rule Generation

    [LoggerMessage(
        EventId = 8150,
        Level = LogLevel.Information,
        Message = "Generating IP restriction rules | RuleName: {RuleName} | IpCount: {IpCount}")]
    public static partial void LogIpRuleGeneration(
        this ILogger logger,
        string ruleName,
        int ipCount);

    [LoggerMessage(
        EventId = 8151,
        Level = LogLevel.Information,
        Message = "IP restriction rules generated | RuleName: {RuleName} | RulesGenerated: {RulesGenerated}")]
    public static partial void LogIpRulesGenerated(
        this ILogger logger,
        string ruleName,
        int rulesGenerated);

    [LoggerMessage(
        EventId = 8152,
        Level = LogLevel.Warning,
        Message = "IP rule generation skipped | Reason: {Reason}")]
    public static partial void LogIpRuleGenerationSkipped(
        this ILogger logger,
        string reason);

    [LoggerMessage(
        EventId = 8153,
        Level = LogLevel.Information,
        Message = "Generating VNet subnet rule | RuleName: {RuleName} | SubnetId: {SubnetId}")]
    public static partial void LogVNetRuleGeneration(
        this ILogger logger,
        string ruleName,
        string subnetId);

    #endregion

    // ============================================================
    // StringHelper (EventIds 8180-8199)
    // ============================================================

    #region String Operations

    [LoggerMessage(
        EventId = 8180,
        Level = LogLevel.Debug,
        Message = "Extracting subscription ID | ResourceId: {ResourceId}")]
    public static partial void LogSubscriptionIdExtraction(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 8181,
        Level = LogLevel.Warning,
        Message = "Invalid resource ID format | ResourceId: {ResourceId}")]
    public static partial void LogInvalidResourceIdFormat(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 8182,
        Level = LogLevel.Debug,
        Message = "Extracted subscription IDs | Count: {Count}")]
    public static partial void LogSubscriptionIdsExtracted(
        this ILogger logger,
        int count);

    #endregion

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for HTTP operations.
    /// </summary>
    public static IDisposable? BeginHttpOperationScope(
        this ILogger logger,
        string method,
        string url)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["HttpMethod"] = method,
        ["Url"] = url,
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for JSON conversion operations.
    /// </summary>
    public static IDisposable? BeginJsonConversionScope(
        this ILogger logger,
        string operationType)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = operationType,
        ["ServiceName"] = "AzureResourceJsonConvertor",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for helper operations.
    /// </summary>
    public static IDisposable? BeginHelperScope(
        this ILogger logger,
        string helperName,
        string methodName)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["HelperName"] = helperName,
        ["MethodName"] = methodName,
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}