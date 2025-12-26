using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AllowListingAzureFunction.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Azure Functions.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGES:
  /// - 11000-11099: HTTP Trigger Functions
  /// - 11100-11199: Orchestrator Functions
  /// - 11200-11299: Activity Functions
  /// - 11300-11399: Queue Trigger Functions
  /// - 11400-11499: Cosmos DB Trigger Functions
  /// - 11500-11599: Validation & Parsing
  /// - 11600-11699: Error Handling
  /// </summary>
  public static partial class AzureFunctionLoggerExtensions
  {
    // ============================================================
    // HTTP Trigger Functions (EventIds 11000-11099)
    // ============================================================

    [LoggerMessage(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "HTTP function started | Function: {FunctionName} | OperationId: {OperationId} | Method: {HttpMethod}")]
    public static partial void LogHttpFunctionStarted(
        this ILogger logger,
        string functionName,
        string operationId,
        string httpMethod);

    [LoggerMessage(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "HTTP function completed | Function: {FunctionName} | OperationId: {OperationId} | StatusCode: {StatusCode} | Duration: {DurationMs}ms")]
    public static partial void LogHttpFunctionCompleted(
        this ILogger logger,
        string functionName,
        string operationId,
        int statusCode,
        long durationMs);

    [LoggerMessage(
        EventId = 11002,
        Level = LogLevel.Error,
        Message = "HTTP function failed | Function: {FunctionName} | OperationId: {OperationId}")]
    public static partial void LogHttpFunctionFailed(
        this ILogger logger,
        Exception exception,
        string functionName,
        string operationId);

    [LoggerMessage(
        EventId = 11003,
        Level = LogLevel.Warning,
        Message = "HTTP function error response | Function: {FunctionName} | OperationId: {OperationId} | StatusCode: {StatusCode} | Reason: {Reason}")]
    public static partial void LogHttpFunctionErrorResponse(
        this ILogger logger,
        string functionName,
        string operationId,
        int statusCode,
        string reason);

    [LoggerMessage(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Reading request body | OperationId: {OperationId}")]
    public static partial void LogReadingRequestBody(
        this ILogger logger,
        string operationId);

    [LoggerMessage(
        EventId = 11011,
        Level = LogLevel.Warning,
        Message = "Request body is empty | OperationId: {OperationId}")]
    public static partial void LogEmptyRequestBody(
        this ILogger logger,
        string operationId);

    [LoggerMessage(
        EventId = 11012,
        Level = LogLevel.Debug,
        Message = "Request body received | OperationId: {OperationId} | ContentLength: {ContentLength}")]
    public static partial void LogRequestBodyReceived(
        this ILogger logger,
        string operationId,
        int contentLength);

    [LoggerMessage(
        EventId = 11020,
        Level = LogLevel.Information,
        Message = "Starting orchestration | Function: {FunctionName} | OperationId: {OperationId} | InstanceId: {InstanceId}")]
    public static partial void LogStartingOrchestration(
        this ILogger logger,
        string functionName,
        string operationId,
        string instanceId);

    [LoggerMessage(
        EventId = 11021,
        Level = LogLevel.Information,
        Message = "Orchestration started | InstanceId: {InstanceId} | StatusQueryUrl: {StatusQueryUrl}")]
    public static partial void LogOrchestrationStarted(
        this ILogger logger,
        string instanceId,
        string statusQueryUrl);

    // ============================================================
    // Orchestrator Functions (EventIds 11100-11199)
    // ============================================================

    [LoggerMessage(
        EventId = 11100,
        Level = LogLevel.Information,
        Message = "Orchestrator started | Orchestrator: {OrchestratorName} | InstanceId: {InstanceId} | ParentOperationId: {ParentOperationId}")]
    public static partial void LogOrchestratorStarted(
        this ILogger logger,
        string orchestratorName,
        string instanceId,
        string parentOperationId);

    [LoggerMessage(
        EventId = 11101,
        Level = LogLevel.Information,
        Message = "Orchestrator completed | Orchestrator: {OrchestratorName} | InstanceId: {InstanceId} | Success: {Success}")]
    public static partial void LogOrchestratorCompleted(
        this ILogger logger,
        string orchestratorName,
        string instanceId,
        bool success);

    [LoggerMessage(
        EventId = 11102,
        Level = LogLevel.Error,
        Message = "Orchestrator failed | Orchestrator: {OrchestratorName} | InstanceId: {InstanceId}")]
    public static partial void LogOrchestratorFailed(
        this ILogger logger,
        Exception exception,
        string orchestratorName,
        string instanceId);

    [LoggerMessage(
        EventId = 11103,
        Level = LogLevel.Warning,
        Message = "Orchestrator completed with errors | Orchestrator: {OrchestratorName} | InstanceId: {InstanceId} | ErrorCount: {ErrorCount}")]
    public static partial void LogOrchestratorCompletedWithErrors(
        this ILogger logger,
        string orchestratorName,
        string instanceId,
        int errorCount);

    [LoggerMessage(
        EventId = 11110,
        Level = LogLevel.Information,
        Message = "Calling activity | Activity: {ActivityName} | InstanceId: {InstanceId} | ResourceName: {ResourceName}")]
    public static partial void LogCallingActivity(
        this ILogger logger,
        string activityName,
        string instanceId,
        string resourceName);

    [LoggerMessage(
        EventId = 11111,
        Level = LogLevel.Information,
        Message = "Activity returned | Activity: {ActivityName} | InstanceId: {InstanceId} | Success: {Success}")]
    public static partial void LogActivityReturned(
        this ILogger logger,
        string activityName,
        string instanceId,
        bool success);

    [LoggerMessage(
        EventId = 11112,
        Level = LogLevel.Information,
        Message = "Scheduling parallel activities | ActivityCount: {ActivityCount} | InstanceId: {InstanceId}")]
    public static partial void LogSchedulingParallelActivities(
        this ILogger logger,
        int activityCount,
        string instanceId);

    [LoggerMessage(
        EventId = 11113,
        Level = LogLevel.Information,
        Message = "Parallel activities completed | SuccessCount: {SuccessCount} | FailureCount: {FailureCount} | InstanceId: {InstanceId}")]
    public static partial void LogParallelActivitiesCompleted(
        this ILogger logger,
        int successCount,
        int failureCount,
        string instanceId);

    [LoggerMessage(
        EventId = 11114,
        Level = LogLevel.Warning,
        Message = "Activity result mismatch | Expected: {ExpectedCount} | Actual: {ActualCount} | InstanceId: {InstanceId}")]
    public static partial void LogActivityResultMismatch(
        this ILogger logger,
        int expectedCount,
        int actualCount,
        string instanceId);

    // ============================================================
    // Activity Functions (EventIds 11200-11299)
    // ============================================================

    [LoggerMessage(
        EventId = 11200,
        Level = LogLevel.Information,
        Message = "Activity started | Activity: {ActivityName} | InstanceId: {InstanceId} | InvocationId: {InvocationId}")]
    public static partial void LogActivityStarted(
        this ILogger logger,
        string activityName,
        string instanceId,
        string invocationId);

    [LoggerMessage(
        EventId = 11201,
        Level = LogLevel.Information,
        Message = "Activity completed | Activity: {ActivityName} | InstanceId: {InstanceId} | Success: {Success}")]
    public static partial void LogActivityCompleted(
        this ILogger logger,
        string activityName,
        string instanceId,
        bool success);

    [LoggerMessage(
        EventId = 11202,
        Level = LogLevel.Error,
        Message = "Activity failed | Activity: {ActivityName} | InstanceId: {InstanceId}")]
    public static partial void LogActivityFailed(
        this ILogger logger,
        Exception exception,
        string activityName,
        string instanceId);

    [LoggerMessage(
        EventId = 11210,
        Level = LogLevel.Information,
        Message = "Database update started | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogDatabaseUpdateStarted(
        this ILogger logger,
        string resourceName,
        string instanceId);

    [LoggerMessage(
        EventId = 11211,
        Level = LogLevel.Information,
        Message = "Database update completed | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogDatabaseUpdateCompleted(
        this ILogger logger,
        string resourceName,
        string instanceId);

    [LoggerMessage(
        EventId = 11212,
        Level = LogLevel.Information,
        Message = "Unmanaged resources update started | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogUnmanagedResourcesUpdateStarted(
        this ILogger logger,
        string resourceName,
        string instanceId);

    [LoggerMessage(
        EventId = 11213,
        Level = LogLevel.Information,
        Message = "Unmanaged resources update completed | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogUnmanagedResourcesUpdateCompleted(
        this ILogger logger,
        string resourceName,
        string instanceId);

    [LoggerMessage(
        EventId = 11220,
        Level = LogLevel.Information,
        Message = "Getting overwrite configs | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogGettingOverwriteConfigs(
        this ILogger logger,
        string resourceName,
        string instanceId);

    [LoggerMessage(
        EventId = 11221,
        Level = LogLevel.Information,
        Message = "Overwrite configs retrieved | ConfigCount: {ConfigCount} | InstanceId: {InstanceId}")]
    public static partial void LogOverwriteConfigsRetrieved(
        this ILogger logger,
        int configCount,
        string instanceId);

    [LoggerMessage(
        EventId = 11222,
        Level = LogLevel.Information,
        Message = "No overwrite configs found | InstanceId: {InstanceId}")]
    public static partial void LogNoOverwriteConfigsFound(
        this ILogger logger,
        string instanceId);

    [LoggerMessage(
        EventId = 11230,
        Level = LogLevel.Information,
        Message = "Overwriting network restrictions | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogOverwritingNetworkRestrictions(
        this ILogger logger,
        string resourceName,
        string instanceId);

    [LoggerMessage(
        EventId = 11231,
        Level = LogLevel.Information,
        Message = "Network restrictions overwritten | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogNetworkRestrictionsOverwritten(
        this ILogger logger,
        string resourceName,
        string instanceId);

    [LoggerMessage(
        EventId = 11232,
        Level = LogLevel.Error,
        Message = "Network restrictions overwrite failed | ResourceName: {ResourceName} | InstanceId: {InstanceId}")]
    public static partial void LogNetworkRestrictionsOverwriteFailed(
        this ILogger logger,
        Exception exception,
        string resourceName,
        string instanceId);

    // ============================================================
    // Queue Trigger Functions (EventIds 11300-11399)
    // ============================================================

    [LoggerMessage(
        EventId = 11300,
        Level = LogLevel.Information,
        Message = "Queue trigger started | Function: {FunctionName} | InstanceId: {InstanceId} | ResourceName: {ResourceName}")]
    public static partial void LogQueueTriggerStarted(
        this ILogger logger,
        string functionName,
        string instanceId,
        string resourceName);

    [LoggerMessage(
        EventId = 11301,
        Level = LogLevel.Information,
        Message = "Queue trigger completed | Function: {FunctionName} | InstanceId: {InstanceId} | Success: {Success}")]
    public static partial void LogQueueTriggerCompleted(
        this ILogger logger,
        string functionName,
        string instanceId,
        bool success);

    [LoggerMessage(
        EventId = 11302,
        Level = LogLevel.Error,
        Message = "Queue trigger failed | Function: {FunctionName} | InstanceId: {InstanceId}")]
    public static partial void LogQueueTriggerFailed(
        this ILogger logger,
        Exception exception,
        string functionName,
        string instanceId);

    [LoggerMessage(
        EventId = 11310,
        Level = LogLevel.Debug,
        Message = "Processing queue message | ResourceName: {ResourceName} | ResourceId: {ResourceId}")]
    public static partial void LogProcessingQueueMessage(
        this ILogger logger,
        string resourceName,
        string resourceId);

    [LoggerMessage(
        EventId = 11311,
        Level = LogLevel.Debug,
        Message = "Adding config to queue | ResourceName: {ResourceName}")]
    public static partial void LogAddingConfigToQueue(
        this ILogger logger,
        string resourceName);

    // ============================================================
    // Cosmos DB Trigger Functions (EventIds 11400-11499)
    // ============================================================

    [LoggerMessage(
        EventId = 11400,
        Level = LogLevel.Information,
        Message = "Cosmos trigger started | Function: {FunctionName} | OperationId: {OperationId} | DocumentCount: {DocumentCount}")]
    public static partial void LogCosmosTriggerStarted(
        this ILogger logger,
        string functionName,
        string operationId,
        int documentCount);

    [LoggerMessage(
        EventId = 11401,
        Level = LogLevel.Information,
        Message = "Cosmos trigger completed | Function: {FunctionName} | OperationId: {OperationId} | QueuedConfigs: {QueuedConfigCount}")]
    public static partial void LogCosmosTriggerCompleted(
        this ILogger logger,
        string functionName,
        string operationId,
        int queuedConfigCount);

    [LoggerMessage(
        EventId = 11402,
        Level = LogLevel.Error,
        Message = "Cosmos trigger failed | Function: {FunctionName} | OperationId: {OperationId}")]
    public static partial void LogCosmosTriggerFailed(
        this ILogger logger,
        Exception exception,
        string functionName,
        string operationId);

    [LoggerMessage(
        EventId = 11403,
        Level = LogLevel.Information,
        Message = "No documents to process | Function: {FunctionName} | OperationId: {OperationId}")]
    public static partial void LogNoDocumentsToProcess(
        this ILogger logger,
        string functionName,
        string operationId);

    [LoggerMessage(
        EventId = 11410,
        Level = LogLevel.Debug,
        Message = "Processing subscription change | SubscriptionId: {SubscriptionId} | OperationId: {OperationId}")]
    public static partial void LogProcessingSubscriptionChange(
        this ILogger logger,
        string subscriptionId,
        string operationId);

    [LoggerMessage(
        EventId = 11411,
        Level = LogLevel.Debug,
        Message = "Processing service tag change | ServiceTagName: {ServiceTagName} | ServiceTagId: {ServiceTagId} | OperationId: {OperationId}")]
    public static partial void LogProcessingServiceTagChange(
        this ILogger logger,
        string serviceTagName,
        string serviceTagId,
        string operationId);

    [LoggerMessage(
        EventId = 11412,
        Level = LogLevel.Information,
        Message = "Fetched dependency configs | ConfigCount: {ConfigCount} | OperationId: {OperationId}")]
    public static partial void LogFetchedDependencyConfigs(
        this ILogger logger,
        int configCount,
        string operationId);

    // ============================================================
    // Validation & Parsing (EventIds 11500-11599)
    // ============================================================

    [LoggerMessage(
        EventId = 11500,
        Level = LogLevel.Debug,
        Message = "Parsing request JSON | OperationId: {OperationId}")]
    public static partial void LogParsingRequestJson(
        this ILogger logger,
        string operationId);

    [LoggerMessage(
        EventId = 11501,
        Level = LogLevel.Warning,
        Message = "JSON parsing failed | OperationId: {OperationId} | Error: {ErrorMessage}")]
    public static partial void LogJsonParsingFailed(
        this ILogger logger,
        string operationId,
        string errorMessage);

    [LoggerMessage(
        EventId = 11502,
        Level = LogLevel.Debug,
        Message = "JSON parsed | OperationId: {OperationId} | ResourceName: {ResourceName}")]
    public static partial void LogJsonParsedSuccessfully(
        this ILogger logger,
        string operationId,
        string resourceName);

    [LoggerMessage(
        EventId = 11510,
        Level = LogLevel.Debug,
        Message = "Validating model | OperationId: {OperationId}")]
    public static partial void LogValidatingModel(
        this ILogger logger,
        string operationId);

    [LoggerMessage(
        EventId = 11511,
        Level = LogLevel.Warning,
        Message = "Model validation failed | OperationId: {OperationId} | ErrorCount: {ErrorCount}")]
    public static partial void LogModelValidationFailed(
        this ILogger logger,
        string operationId,
        int errorCount);

    [LoggerMessage(
        EventId = 11512,
        Level = LogLevel.Debug,
        Message = "Model validation succeeded | OperationId: {OperationId} | ResourceName: {ResourceName}")]
    public static partial void LogModelValidationSucceeded(
        this ILogger logger,
        string operationId,
        string resourceName);

    [LoggerMessage(
        EventId = 11513,
        Level = LogLevel.Warning,
        Message = "Null parameters received | Function: {FunctionName}")]
    public static partial void LogNullParameters(
        this ILogger logger,
        string functionName);

    // ============================================================
    // Error Handling (EventIds 11600-11699)
    // ============================================================

    [LoggerMessage(
        EventId = 11600,
        Level = LogLevel.Warning,
        Message = "Operation completed with errors | OperationId: {OperationId} | ErrorCount: {ErrorCount}")]
    public static partial void LogOperationCompletedWithErrors(
        this ILogger logger,
        string operationId,
        int errorCount);

    [LoggerMessage(
        EventId = 11601,
        Level = LogLevel.Error,
        Message = "Operation errors | OperationId: {OperationId} | Errors: {Errors}")]
    public static partial void LogOperationErrors(
        this ILogger logger,
        string operationId,
        string errors);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for an Azure Function operation.
    /// </summary>
    public static IDisposable? BeginFunctionScope(
        this ILogger logger,
        string functionName,
        string operationId,
        string? instanceId = null)
    {
      var properties = new Dictionary<string, object>
      {
        ["FunctionName"] = functionName,
        ["OperationId"] = operationId
      };

      if (!string.IsNullOrEmpty(instanceId))
        properties["InstanceId"] = instanceId;

      return logger.BeginScope(properties);
    }

    /// <summary>
    /// Creates a logging scope for an orchestrator operation.
    /// </summary>
    public static IDisposable? BeginOrchestratorScope(
        this ILogger logger,
        string orchestratorName,
        string instanceId,
        string parentOperationId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OrchestratorName"] = orchestratorName,
        ["InstanceId"] = instanceId,
        ["ParentOperationId"] = parentOperationId
      });
    }

    /// <summary>
    /// Creates a logging scope for an activity function.
    /// </summary>
    public static IDisposable? BeginActivityScope(
        this ILogger logger,
        string activityName,
        string instanceId,
        string invocationId,
        string? resourceName = null)
    {
      var properties = new Dictionary<string, object>
      {
        ["ActivityName"] = activityName,
        ["InstanceId"] = instanceId,
        ["InvocationId"] = invocationId
      };

      if (!string.IsNullOrEmpty(resourceName))
        properties["ResourceName"] = resourceName;

      return logger.BeginScope(properties);
    }

    /// <summary>
    /// Creates a logging scope for a Cosmos DB trigger operation.
    /// </summary>
    public static IDisposable? BeginCosmosTriggerScope(
        this ILogger logger,
        string functionName,
        string operationId,
        int documentCount)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["FunctionName"] = functionName,
        ["OperationId"] = operationId,
        ["DocumentCount"] = documentCount,
        ["TriggerType"] = "CosmosDB"
      });
    }
  }
}