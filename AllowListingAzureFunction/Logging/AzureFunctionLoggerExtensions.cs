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
    // UpdateInternalAndThirdPartySettingsEventHandler (EventIds 11420-11449)
    // ============================================================

    [LoggerMessage(
        EventId = 11420,
        Level = LogLevel.Information,
        Message = "Processing input documents | Function: {FunctionName} | OperationId: {OperationId} | OriginalCount: {OriginalCount} | UniqueCount: {UniqueCount}")]
    public static partial void LogInputDocumentsProcessed(
        this ILogger logger,
        string functionName,
        string operationId,
        int originalCount,
        int uniqueCount);

    [LoggerMessage(
        EventId = 11421,
        Level = LogLevel.Debug,
        Message = "Processing subscription document | SubscriptionId: {SubscriptionId} | SubscriptionName: {SubscriptionName} | IsDeleted: {IsDeleted} | OperationId: {OperationId}")]
    public static partial void LogProcessingSubscriptionDocument(
        this ILogger logger,
        string subscriptionId,
        string subscriptionName,
        bool isDeleted,
        string operationId);

    [LoggerMessage(
        EventId = 11422,
        Level = LogLevel.Debug,
        Message = "Processing service tag document | ServiceTagId: {ServiceTagId} | ServiceTagName: {ServiceTagName} | IsDeleted: {IsDeleted} | OperationId: {OperationId}")]
    public static partial void LogProcessingServiceTagDocument(
        this ILogger logger,
        string serviceTagId,
        string serviceTagName,
        bool isDeleted,
        string operationId);

    [LoggerMessage(
        EventId = 11423,
        Level = LogLevel.Information,
        Message = "Starting GetValidDependencyConfigs | Function: {FunctionName} | OperationId: {OperationId} | InputCount: {InputCount}")]
    public static partial void LogGetValidDependencyConfigsStarting(
        this ILogger logger,
        string functionName,
        string operationId,
        int inputCount);

    [LoggerMessage(
        EventId = 11424,
        Level = LogLevel.Warning,
        Message = "No valid dependency configs found after processing | Function: {FunctionName} | OperationId: {OperationId} | InputDocumentCount: {InputDocumentCount}")]
    public static partial void LogNoValidConfigsAfterProcessing(
        this ILogger logger,
        string functionName,
        string operationId,
        int inputDocumentCount);

    [LoggerMessage(
        EventId = 11425,
        Level = LogLevel.Information,
        Message = "Queue messages prepared | Function: {FunctionName} | OperationId: {OperationId} | MessageCount: {MessageCount} | ResourceNames: {ResourceNames}")]
    public static partial void LogQueueMessagesPrepared(
        this ILogger logger,
        string functionName,
        string operationId,
        int messageCount,
        string resourceNames);

    [LoggerMessage(
        EventId = 11426,
        Level = LogLevel.Information,
        Message = "Detected deleted documents | Function: {FunctionName} | OperationId: {OperationId} | DeletedCount: {DeletedCount} | ActiveCount: {ActiveCount}")]
    public static partial void LogDeletedDocumentsDetected(
        this ILogger logger,
        string functionName,
        string operationId,
        int deletedCount,
        int activeCount);

    [LoggerMessage(
        EventId = 11427,
        Level = LogLevel.Debug,
        Message = "Adding config to queue | ResourceName: {ResourceName} | ResourceId: {ResourceId} | OperationId: {OperationId}")]
    public static partial void LogAddingConfigToQueueWithDetails(
        this ILogger logger,
        string resourceName,
        string resourceId,
        string operationId);

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
    // Input Processing & Summary (EventIds 11050-11069)
    // ============================================================

    [LoggerMessage(
        EventId = 11050,
        Level = LogLevel.Information,
        Message = "Input summary | OperationId: {OperationId} | AzureSubscriptionsCount: {AzureSubscriptionsCount} | ServiceTagsCount: {ServiceTagsCount}")]
    public static partial void LogInputSummary(
        this ILogger logger,
        string operationId,
        int azureSubscriptionsCount,
        int serviceTagsCount);

    [LoggerMessage(
        EventId = 11051,
        Level = LogLevel.Information,
        Message = "Processing input | OperationId: {OperationId} | ModelType: {ModelType} | PayloadSize: {PayloadSize} bytes")]
    public static partial void LogProcessingInput(
        this ILogger logger,
        string operationId,
        string modelType,
        int payloadSize);

    [LoggerMessage(
        EventId = 11052,
        Level = LogLevel.Debug,
        Message = "Input details | OperationId: {OperationId} | HasSubscriptions: {HasSubscriptions} | HasServiceTags: {HasServiceTags}")]
    public static partial void LogInputDetails(
        this ILogger logger,
        string operationId,
        bool hasSubscriptions,
        bool hasServiceTags);

    // ============================================================
    // Success Completion Details (EventIds 11070-11089)
    // ============================================================

    [LoggerMessage(
        EventId = 11070,
        Level = LogLevel.Information,
        Message = "HTTP function completed successfully | Function: {FunctionName} | OperationId: {OperationId} | StatusCode: {StatusCode} | DurationMs: {DurationMs}")]
    public static partial void LogHttpFunctionCompletedSuccessfully(
        this ILogger logger,
        string functionName,
        string operationId,
        int statusCode,
        long durationMs);

    [LoggerMessage(
        EventId = 11071,
        Level = LogLevel.Information,
        Message = "Database update operation summary | OperationId: {OperationId} | SubscriptionsProcessed: {SubscriptionsProcessed} | ServiceTagsProcessed: {ServiceTagsProcessed} | DurationMs: {DurationMs}")]
    public static partial void LogDatabaseUpdateSummary(
        this ILogger logger,
        string operationId,
        int subscriptionsProcessed,
        int serviceTagsProcessed,
        long durationMs);

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
    // DefaultTags Function - Settings & Subscription Logging (EventIds 11050-11079)
    // Add this section to AllowListingAzureFunction/Logging/AzureFunctionLoggerExtensions.cs
    // Place after the existing HTTP Trigger Functions section (after EventId 11021)
    // ============================================================

    #region DefaultTags Function (EventIds 11050-11079)

    [LoggerMessage(
        EventId = 11050,
        Level = LogLevel.Debug,
        Message = "Settings file path resolved | Function: {FunctionName} | FilePath: {FilePath}")]
    public static partial void LogSettingsFilePathResolved(
        this ILogger logger,
        string functionName,
        string filePath);

    [LoggerMessage(
        EventId = 11051,
        Level = LogLevel.Information,
        Message = "Settings file loaded successfully | Function: {FunctionName} | SubscriptionCount: {SubscriptionCount} | ServiceTagCount: {ServiceTagCount}")]
    public static partial void LogSettingsFileLoaded(
        this ILogger logger,
        string functionName,
        int subscriptionCount,
        int serviceTagCount);

    [LoggerMessage(
        EventId = 11052,
        Level = LogLevel.Debug,
        Message = "Settings file read | Function: {FunctionName} | FileSizeBytes: {FileSizeBytes}")]
    public static partial void LogSettingsFileRead(
        this ILogger logger,
        string functionName,
        int fileSizeBytes);

    [LoggerMessage(
        EventId = 11053,
        Level = LogLevel.Information,
        Message = "Subscription matched | Function: {FunctionName} | SubscriptionId: {SubscriptionId} | SubscriptionName: {SubscriptionName}")]
    public static partial void LogSubscriptionMatched(
        this ILogger logger,
        string functionName,
        string subscriptionId,
        string subscriptionName);

    [LoggerMessage(
        EventId = 11054,
        Level = LogLevel.Debug,
        Message = "Subscription lookup | Function: {FunctionName} | RequestedSubscriptionId: {RequestedSubscriptionId} | AvailableSubscriptions: {AvailableSubscriptions}")]
    public static partial void LogSubscriptionLookup(
        this ILogger logger,
        string functionName,
        string requestedSubscriptionId,
        int availableSubscriptions);

    [LoggerMessage(
        EventId = 11055,
        Level = LogLevel.Information,
        Message = "Service tags filtered for subscription | Function: {FunctionName} | SubscriptionId: {SubscriptionId} | TotalServiceTags: {TotalServiceTags} | MatchedServiceTags: {MatchedServiceTags} | TotalAddresses: {TotalAddresses}")]
    public static partial void LogServiceTagsFiltered(
        this ILogger logger,
        string functionName,
        string subscriptionId,
        int totalServiceTags,
        int matchedServiceTags,
        int totalAddresses);

    [LoggerMessage(
        EventId = 11056,
        Level = LogLevel.Debug,
        Message = "Service tag included | Function: {FunctionName} | TagName: {TagName} | AddressCount: {AddressCount}")]
    public static partial void LogServiceTagIncluded(
        this ILogger logger,
        string functionName,
        string tagName,
        int addressCount);

    [LoggerMessage(
        EventId = 11057,
        Level = LogLevel.Debug,
        Message = "Service tag skipped (no addresses for subscription) | Function: {FunctionName} | TagName: {TagName} | SubscriptionId: {SubscriptionId}")]
    public static partial void LogServiceTagSkipped(
        this ILogger logger,
        string functionName,
        string tagName,
        string subscriptionId);

    [LoggerMessage(
        EventId = 11058,
        Level = LogLevel.Information,
        Message = "DefaultTags response prepared | Function: {FunctionName} | SubscriptionId: {SubscriptionId} | SubscriptionName: {SubscriptionName} | ServiceTagCount: {ServiceTagCount} | TotalAddressCount: {TotalAddressCount} | DurationMs: {DurationMs}")]
    public static partial void LogDefaultTagsResponsePrepared(
        this ILogger logger,
        string functionName,
        string subscriptionId,
        string subscriptionName,
        int serviceTagCount,
        int totalAddressCount,
        long durationMs);

    #endregion

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