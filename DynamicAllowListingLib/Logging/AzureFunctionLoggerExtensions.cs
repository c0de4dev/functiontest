using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AllowListingAzureFunction.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Azure Functions.
  /// Uses LoggerMessage source generators for optimal performance.
  /// EVENT ID RANGE: 100-999 (Function-level operations)
  /// </summary>
  public static partial class AzureFunctionLoggerExtensions
  {
    // ============================================================
    // Function Invocation Lifecycle (EventIds 100-149)
    // ============================================================

    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "Function started | Function: {FunctionName} | InvocationId: {InvocationId} | CorrelationId: {CorrelationId}")]
    public static partial void LogFunctionStarted(
        this ILogger logger,
        string functionName,
        string invocationId,
        string correlationId);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "Function completed | Function: {FunctionName} | InvocationId: {InvocationId} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogFunctionCompleted(
        this ILogger logger,
        string functionName,
        string invocationId,
        long durationMs,
        bool success);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Error,
        Message = "Function failed | Function: {FunctionName} | InvocationId: {InvocationId} | Duration: {DurationMs}ms")]
    public static partial void LogFunctionFailed(
        this ILogger logger,
        Exception exception,
        string functionName,
        string invocationId,
        long durationMs);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Warning,
        Message = "Function slow execution | Function: {FunctionName} | Duration: {DurationMs}ms | Threshold: {ThresholdMs}ms")]
    public static partial void LogFunctionSlowExecution(
        this ILogger logger,
        string functionName,
        long durationMs,
        long thresholdMs);

    // ============================================================
    // HTTP Trigger Functions (EventIds 150-199)
    // ============================================================

    [LoggerMessage(
        EventId = 150,
        Level = LogLevel.Information,
        Message = "HTTP request received | Function: {FunctionName} | Method: {Method} | Path: {Path}")]
    public static partial void LogHttpRequestReceived(
        this ILogger logger,
        string functionName,
        string method,
        string path);

    [LoggerMessage(
        EventId = 151,
        Level = LogLevel.Information,
        Message = "HTTP request body received | Function: {FunctionName} | ContentLength: {ContentLength} bytes")]
    public static partial void LogHttpRequestBody(
        this ILogger logger,
        string functionName,
        int contentLength);

    [LoggerMessage(
        EventId = 152,
        Level = LogLevel.Information,
        Message = "HTTP response | Function: {FunctionName} | StatusCode: {StatusCode} | Duration: {DurationMs}ms")]
    public static partial void LogHttpResponse(
        this ILogger logger,
        string functionName,
        int statusCode,
        long durationMs);

    [LoggerMessage(
        EventId = 153,
        Level = LogLevel.Warning,
        Message = "HTTP request validation failed | Function: {FunctionName} | Reason: {Reason}")]
    public static partial void LogHttpValidationFailed(
        this ILogger logger,
        string functionName,
        string reason);

    // ============================================================
    // Timer Trigger Functions (EventIds 200-249)
    // ============================================================

    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Information,
        Message = "Timer triggered | Function: {FunctionName} | ScheduleStatus: {IsPastDue} | LastRun: {LastRun}")]
    public static partial void LogTimerTriggered(
        this ILogger logger,
        string functionName,
        bool isPastDue,
        string lastRun);

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Warning,
        Message = "Timer past due | Function: {FunctionName} | ScheduledTime: {ScheduledTime}")]
    public static partial void LogTimerPastDue(
        this ILogger logger,
        string functionName,
        string scheduledTime);

    // ============================================================
    // Queue Trigger Functions (EventIds 250-299)
    // ============================================================

    [LoggerMessage(
        EventId = 250,
        Level = LogLevel.Information,
        Message = "Queue message received | Function: {FunctionName} | MessageId: {MessageId} | DequeueCount: {DequeueCount}")]
    public static partial void LogQueueMessageReceived(
        this ILogger logger,
        string functionName,
        string messageId,
        int dequeueCount);

    [LoggerMessage(
        EventId = 251,
        Level = LogLevel.Information,
        Message = "Queue message processed | Function: {FunctionName} | MessageId: {MessageId} | Duration: {DurationMs}ms")]
    public static partial void LogQueueMessageProcessed(
        this ILogger logger,
        string functionName,
        string messageId,
        long durationMs);

    [LoggerMessage(
        EventId = 252,
        Level = LogLevel.Warning,
        Message = "Queue message retry | Function: {FunctionName} | MessageId: {MessageId} | DequeueCount: {DequeueCount}")]
    public static partial void LogQueueMessageRetry(
        this ILogger logger,
        string functionName,
        string messageId,
        int dequeueCount);

    [LoggerMessage(
        EventId = 253,
        Level = LogLevel.Error,
        Message = "Queue message poison | Function: {FunctionName} | MessageId: {MessageId} | DequeueCount: {DequeueCount}")]
    public static partial void LogQueueMessagePoison(
        this ILogger logger,
        string functionName,
        string messageId,
        int dequeueCount);

    // ============================================================
    // CosmosDB Trigger Functions (EventIds 300-349)
    // ============================================================

    [LoggerMessage(
        EventId = 300,
        Level = LogLevel.Information,
        Message = "CosmosDB change feed triggered | Function: {FunctionName} | DocumentCount: {DocumentCount}")]
    public static partial void LogCosmosDbTriggered(
        this ILogger logger,
        string functionName,
        int documentCount);

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Information,
        Message = "CosmosDB document processed | Function: {FunctionName} | DocumentId: {DocumentId}")]
    public static partial void LogCosmosDbDocumentProcessed(
        this ILogger logger,
        string functionName,
        string documentId);

    // ============================================================
    // Resource Operations (EventIds 350-399)
    // ============================================================

    [LoggerMessage(
        EventId = 350,
        Level = LogLevel.Information,
        Message = "Processing resource | Function: {FunctionName} | ResourceId: {ResourceId} | ResourceType: {ResourceType}")]
    public static partial void LogProcessingResource(
        this ILogger logger,
        string functionName,
        string resourceId,
        string resourceType);

    [LoggerMessage(
        EventId = 351,
        Level = LogLevel.Information,
        Message = "Resource processed successfully | Function: {FunctionName} | ResourceId: {ResourceId}")]
    public static partial void LogResourceProcessedSuccess(
        this ILogger logger,
        string functionName,
        string resourceId);

    [LoggerMessage(
        EventId = 352,
        Level = LogLevel.Error,
        Message = "Resource processing failed | Function: {FunctionName} | ResourceId: {ResourceId}")]
    public static partial void LogResourceProcessingFailed(
        this ILogger logger,
        Exception exception,
        string functionName,
        string resourceId);

    [LoggerMessage(
        EventId = 353,
        Level = LogLevel.Warning,
        Message = "Resource not found | Function: {FunctionName} | ResourceId: {ResourceId}")]
    public static partial void LogResourceNotFound(
        this ILogger logger,
        string functionName,
        string resourceId);

    // ============================================================
    // Validation Operations (EventIds 400-449)
    // ============================================================

    [LoggerMessage(
        EventId = 400,
        Level = LogLevel.Information,
        Message = "Validation started | Function: {FunctionName} | Entity: {Entity}")]
    public static partial void LogValidationStarted(
        this ILogger logger,
        string functionName,
        string entity);

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Information,
        Message = "Validation completed | Function: {FunctionName} | Entity: {Entity} | IsValid: {IsValid}")]
    public static partial void LogValidationCompleted(
        this ILogger logger,
        string functionName,
        string entity,
        bool isValid);

    [LoggerMessage(
        EventId = 402,
        Level = LogLevel.Warning,
        Message = "Validation errors | Function: {FunctionName} | Entity: {Entity} | ErrorCount: {ErrorCount}")]
    public static partial void LogValidationErrors(
        this ILogger logger,
        string functionName,
        string entity,
        int errorCount);

    // ============================================================
    // Network Restriction Operations (EventIds 450-499)
    // ============================================================

    [LoggerMessage(
        EventId = 450,
        Level = LogLevel.Information,
        Message = "Network restriction operation started | Function: {FunctionName} | ResourceId: {ResourceId} | Operation: {Operation}")]
    public static partial void LogNetworkRestrictionStart(
        this ILogger logger,
        string functionName,
        string resourceId,
        string operation);

    [LoggerMessage(
        EventId = 451,
        Level = LogLevel.Information,
        Message = "Network restriction operation completed | Function: {FunctionName} | ResourceId: {ResourceId} | Operation: {Operation} | Duration: {DurationMs}ms")]
    public static partial void LogNetworkRestrictionComplete(
        this ILogger logger,
        string functionName,
        string resourceId,
        string operation,
        long durationMs);

    [LoggerMessage(
        EventId = 452,
        Level = LogLevel.Error,
        Message = "Network restriction operation failed | Function: {FunctionName} | ResourceId: {ResourceId} | Operation: {Operation}")]
    public static partial void LogNetworkRestrictionFailed(
        this ILogger logger,
        Exception exception,
        string functionName,
        string resourceId,
        string operation);

    // ============================================================
    // Batch Operations (EventIds 500-549)
    // ============================================================

    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Information,
        Message = "Batch operation started | Function: {FunctionName} | BatchSize: {BatchSize}")]
    public static partial void LogBatchStarted(
        this ILogger logger,
        string functionName,
        int batchSize);

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Information,
        Message = "Batch operation progress | Function: {FunctionName} | Processed: {Processed}/{Total} | SuccessCount: {SuccessCount} | FailureCount: {FailureCount}")]
    public static partial void LogBatchProgress(
        this ILogger logger,
        string functionName,
        int processed,
        int total,
        int successCount,
        int failureCount);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Information,
        Message = "Batch operation completed | Function: {FunctionName} | Total: {Total} | SuccessCount: {SuccessCount} | FailureCount: {FailureCount} | Duration: {DurationMs}ms")]
    public static partial void LogBatchCompleted(
        this ILogger logger,
        string functionName,
        int total,
        int successCount,
        int failureCount,
        long durationMs);

    // ============================================================
    // Scope Extension Methods
    // ============================================================

    /// <summary>
    /// Creates a logging scope for function execution.
    /// </summary>
    public static IDisposable BeginFunctionScope(
        this ILogger logger,
        string functionName,
        string invocationId,
        string correlationId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["FunctionName"] = functionName,
        ["InvocationId"] = invocationId,
        ["CorrelationId"] = correlationId
      });
    }

    /// <summary>
    /// Creates a logging scope for resource processing.
    /// </summary>
    public static IDisposable BeginResourceProcessingScope(
        this ILogger logger,
        string functionName,
        string resourceId,
        string? resourceType = null)
    {
      var scope = new Dictionary<string, object>
      {
        ["FunctionName"] = functionName,
        ["ResourceId"] = resourceId
      };

      if (!string.IsNullOrEmpty(resourceType))
      {
        scope["ResourceType"] = resourceType;
      }

      return logger.BeginScope(scope);
    }
  }
}