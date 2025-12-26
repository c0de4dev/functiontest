using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for ResourceGraphExplorerService operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 9000-9199 (Fixed to avoid conflicts with HelpersLoggerExtensions which uses 8000-8199)
  /// 
  /// Event ID Allocation:
  /// - 9000-9019: General/Common operations
  /// - 9020-9039: GetResourceInstances
  /// - 9040-9059: GetResourceGraphExplorerResponse
  /// - 9060-9079: GetExistingResourceIds
  /// - 9080-9099: GetExistingResourceIdsByType
  /// - 9100-9109: ResourceExists
  /// - 9110-9119: GetAllSubnetIds
  /// - 9120-9129: GetResourcesHostedOnPlan
  /// - 9130-9139: GetFrontDoorUniqueInstanceIds
  /// - 9140-9149: GetWebAppSlots
  /// - 9150-9169: Batch Operations
  /// - 9170-9199: Reserved for future use
  /// </summary>
  public static partial class ResourceGraphExplorerLoggerExtensions
  {
    // ============================================================
    // General/Common Operations (EventIds 9000-9019)
    // ============================================================

    [LoggerMessage(
        EventId = 9000,
        Level = LogLevel.Debug,
        Message = "Method completed | MethodName: {MethodName} | Success: {Success}")]
    public static partial void LogMethodComplete(
        this ILogger logger,
        string methodName,
        bool success);

    // ============================================================
    // GetResourceInstances (EventIds 9020-9039)
    // ============================================================

    [LoggerMessage(
        EventId = 9020,
        Level = LogLevel.Information,
        Message = "GetResourceInstances started | SubscriptionCount: {SubscriptionCount} | ResourceIdCount: {ResourceIdCount}")]
    public static partial void LogGetResourceInstancesStart(
        this ILogger logger,
        int subscriptionCount,
        int resourceIdCount);

    [LoggerMessage(
        EventId = 9021,
        Level = LogLevel.Warning,
        Message = "No resource IDs provided for GetResourceInstances")]
    public static partial void LogNoResourceIdsProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 9022,
        Level = LogLevel.Debug,
        Message = "Executing Resource Graph query | QueryLength: {QueryLength}")]
    public static partial void LogExecutingQuery(
        this ILogger logger,
        int queryLength);

    [LoggerMessage(
        EventId = 9023,
        Level = LogLevel.Information,
        Message = "GetResourceInstances completed | ResourcesFound: {ResourceCount}")]
    public static partial void LogGetResourceInstancesComplete(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 9024,
        Level = LogLevel.Warning,
        Message = "Null resources returned from deserialization")]
    public static partial void LogNullResourcesReturned(this ILogger logger);

    [LoggerMessage(
        EventId = 9025,
        Level = LogLevel.Error,
        Message = "GetResourceInstances failed")]
    public static partial void LogGetResourceInstancesFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // GetResourceGraphExplorerResponse (EventIds 9040-9059)
    // ============================================================

    [LoggerMessage(
        EventId = 9040,
        Level = LogLevel.Warning,
        Message = "Invalid Resource Graph parameters | HasValidSubscriptions: {HasValidSubscriptions} | HasValidQuery: {HasValidQuery}")]
    public static partial void LogInvalidResourceGraphParameters(
        this ILogger logger,
        bool hasValidSubscriptions,
        bool hasValidQuery);

    [LoggerMessage(
        EventId = 9041,
        Level = LogLevel.Information,
        Message = "Resource Graph request started | SubscriptionCount: {SubscriptionCount}")]
    public static partial void LogResourceGraphRequestStart(
        this ILogger logger,
        int subscriptionCount);

    [LoggerMessage(
        EventId = 9042,
        Level = LogLevel.Debug,
        Message = "Resource Graph request body prepared | BodyLength: {BodyLength}")]
    public static partial void LogResourceGraphRequestBody(
        this ILogger logger,
        int bodyLength);

    [LoggerMessage(
        EventId = 9043,
        Level = LogLevel.Information,
        Message = "Resource Graph response received | ResponseLength: {ResponseLength}")]
    public static partial void LogResourceGraphResponseReceived(
        this ILogger logger,
        int responseLength);

    [LoggerMessage(
        EventId = 9044,
        Level = LogLevel.Warning,
        Message = "Empty response received from Resource Graph")]
    public static partial void LogEmptyResourceGraphResponse(this ILogger logger);

    [LoggerMessage(
        EventId = 9045,
        Level = LogLevel.Error,
        Message = "Resource Graph request failed")]
    public static partial void LogResourceGraphRequestFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // GetExistingResourceIds (EventIds 9060-9079)
    // ============================================================

    [LoggerMessage(
        EventId = 9060,
        Level = LogLevel.Warning,
        Message = "Invalid GetExistingResourceIds parameters | HasValidSubscriptionId: {HasValidSubscriptionId} | HasValidResourceList: {HasValidResourceList}")]
    public static partial void LogInvalidGetExistingResourceIdsParameters(
        this ILogger logger,
        bool hasValidSubscriptionId,
        bool hasValidResourceList);

    [LoggerMessage(
        EventId = 9061,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIds started | SubscriptionId: {SubscriptionId} | ResourceCount: {ResourceCount}")]
    public static partial void LogGetExistingResourceIdsStart(
        this ILogger logger,
        string subscriptionId,
        int resourceCount);

    [LoggerMessage(
        EventId = 9062,
        Level = LogLevel.Warning,
        Message = "No data returned in Resource Graph response")]
    public static partial void LogNoDataInResourceGraphResponse(this ILogger logger);

    [LoggerMessage(
        EventId = 9063,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIds completed | ExistingResourceCount: {ExistingResourceCount}")]
    public static partial void LogGetExistingResourceIdsComplete(
        this ILogger logger,
        int existingResourceCount);

    [LoggerMessage(
        EventId = 9064,
        Level = LogLevel.Error,
        Message = "GetExistingResourceIds failed | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetExistingResourceIdsFailed(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    // ============================================================
    // GetExistingResourceIdsByType (EventIds 9080-9099)
    // ============================================================

    [LoggerMessage(
        EventId = 9080,
        Level = LogLevel.Warning,
        Message = "Invalid GetExistingResourceIdsByType parameters | HasValidSubscriptionId: {HasValidSubscriptionId} | HasValidResourceTypes: {HasValidResourceTypes}")]
    public static partial void LogInvalidGetExistingResourceIdsByTypeParameters(
        this ILogger logger,
        bool hasValidSubscriptionId,
        bool hasValidResourceTypes);

    [LoggerMessage(
        EventId = 9081,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIdsByType started | SubscriptionId: {SubscriptionId} | ResourceTypeCount: {ResourceTypeCount}")]
    public static partial void LogGetExistingResourceIdsByTypeStart(
        this ILogger logger,
        string subscriptionId,
        int resourceTypeCount);

    [LoggerMessage(
        EventId = 9082,
        Level = LogLevel.Debug,
        Message = "Resource Graph query executed | QueryPreview: {QueryPreview}")]
    public static partial void LogResourceGraphQueryExecuted(
        this ILogger logger,
        string queryPreview);

    [LoggerMessage(
        EventId = 9083,
        Level = LogLevel.Warning,
        Message = "Empty or null response data received")]
    public static partial void LogEmptyOrNullResponseData(this ILogger logger);

    [LoggerMessage(
        EventId = 9084,
        Level = LogLevel.Debug,
        Message = "Initial response received | ResourceCount: {ResourceCount}")]
    public static partial void LogInitialResponseReceived(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 9085,
        Level = LogLevel.Warning,
        Message = "No valid resource IDs found in response")]
    public static partial void LogNoValidResourceIdsInResponse(this ILogger logger);

    [LoggerMessage(
        EventId = 9086,
        Level = LogLevel.Debug,
        Message = "Processing pagination - fetching next page")]
    public static partial void LogProcessingPagination(this ILogger logger);

    [LoggerMessage(
        EventId = 9087,
        Level = LogLevel.Debug,
        Message = "Pagination page processed | ResourceCount: {ResourceCount}")]
    public static partial void LogPaginationPageProcessed(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 9088,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIdsByType completed | TotalResourcesFound: {TotalResourcesFound}")]
    public static partial void LogGetExistingResourceIdsByTypeComplete(
        this ILogger logger,
        int totalResourcesFound);

    [LoggerMessage(
        EventId = 9089,
        Level = LogLevel.Error,
        Message = "GetExistingResourceIdsByType failed | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetExistingResourceIdsByTypeFailed(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    // ============================================================
    // ResourceExists (EventIds 9100-9109)
    // ============================================================

    [LoggerMessage(
        EventId = 9100,
        Level = LogLevel.Warning,
        Message = "ResourceId is null or empty")]
    public static partial void LogResourceIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 9101,
        Level = LogLevel.Debug,
        Message = "ResourceExists check started | ResourceId: {ResourceId}")]
    public static partial void LogResourceExistsStart(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 9102,
        Level = LogLevel.Information,
        Message = "ResourceExists check completed | ResourceId: {ResourceId} | Exists: {Exists}")]
    public static partial void LogResourceExistsComplete(
        this ILogger logger,
        string resourceId,
        bool exists);

    [LoggerMessage(
        EventId = 9103,
        Level = LogLevel.Error,
        Message = "ResourceExists check failed | ResourceId: {ResourceId}")]
    public static partial void LogResourceExistsFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // GetAllSubnetIds (EventIds 9110-9119)
    // ============================================================

    [LoggerMessage(
        EventId = 9110,
        Level = LogLevel.Information,
        Message = "GetAllSubnetIds started | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetAllSubnetIdsStart(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 9111,
        Level = LogLevel.Information,
        Message = "GetAllSubnetIds completed | SubnetCount: {SubnetCount}")]
    public static partial void LogGetAllSubnetIdsComplete(
        this ILogger logger,
        int subnetCount);

    [LoggerMessage(
        EventId = 9112,
        Level = LogLevel.Error,
        Message = "GetAllSubnetIds failed | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetAllSubnetIdsFailed(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    // ============================================================
    // GetResourcesHostedOnPlan (EventIds 9120-9129)
    // ============================================================

    [LoggerMessage(
        EventId = 9120,
        Level = LogLevel.Warning,
        Message = "AppServicePlanResourceId is null or empty")]
    public static partial void LogAppServicePlanResourceIdEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 9121,
        Level = LogLevel.Information,
        Message = "GetResourcesHostedOnPlan started | AppServicePlanResourceId: {AppServicePlanResourceId}")]
    public static partial void LogGetResourcesHostedOnPlanStart(
        this ILogger logger,
        string appServicePlanResourceId);

    [LoggerMessage(
        EventId = 9122,
        Level = LogLevel.Warning,
        Message = "No resources found on the App Service Plan")]
    public static partial void LogNoResourcesFoundOnPlan(this ILogger logger);

    [LoggerMessage(
        EventId = 9123,
        Level = LogLevel.Information,
        Message = "GetResourcesHostedOnPlan completed | ResourceCount: {ResourceCount}")]
    public static partial void LogGetResourcesHostedOnPlanComplete(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 9124,
        Level = LogLevel.Error,
        Message = "GetResourcesHostedOnPlan failed | AppServicePlanResourceId: {AppServicePlanResourceId}")]
    public static partial void LogGetResourcesHostedOnPlanFailed(
        this ILogger logger,
        Exception exception,
        string appServicePlanResourceId);

    // ============================================================
    // GetFrontDoorUniqueInstanceIds (EventIds 9130-9139)
    // ============================================================

    [LoggerMessage(
        EventId = 9130,
        Level = LogLevel.Warning,
        Message = "No distinct Front Door IDs provided")]
    public static partial void LogNoDistinctFrontDoorIds(this ILogger logger);

    [LoggerMessage(
        EventId = 9131,
        Level = LogLevel.Information,
        Message = "GetFrontDoorUniqueInstanceIds started | DistinctIdCount: {DistinctIdCount}")]
    public static partial void LogGetFrontDoorIdsStart(
        this ILogger logger,
        int distinctIdCount);

    [LoggerMessage(
        EventId = 9132,
        Level = LogLevel.Error,
        Message = "Missing Front Door ID or FDID | ResourceId: {ResourceId}")]
    public static partial void LogMissingFrontDoorIdOrFdid(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 9133,
        Level = LogLevel.Information,
        Message = "GetFrontDoorUniqueInstanceIds completed | MappingCount: {MappingCount}")]
    public static partial void LogGetFrontDoorIdsComplete(
        this ILogger logger,
        int mappingCount);

    [LoggerMessage(
        EventId = 9134,
        Level = LogLevel.Error,
        Message = "GetFrontDoorUniqueInstanceIds failed")]
    public static partial void LogGetFrontDoorIdsFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // GetWebAppSlots (EventIds 9140-9149)
    // ============================================================

    [LoggerMessage(
        EventId = 9140,
        Level = LogLevel.Warning,
        Message = "WebAppId is null or empty")]
    public static partial void LogWebAppIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 9141,
        Level = LogLevel.Information,
        Message = "GetWebAppSlots started | WebAppId: {WebAppId}")]
    public static partial void LogGetWebAppSlotsStart(
        this ILogger logger,
        string webAppId);

    [LoggerMessage(
        EventId = 9142,
        Level = LogLevel.Information,
        Message = "GetWebAppSlots completed | WebAppId: {WebAppId} | SlotCount: {SlotCount}")]
    public static partial void LogGetWebAppSlotsComplete(
        this ILogger logger,
        string webAppId,
        int slotCount);

    [LoggerMessage(
        EventId = 9143,
        Level = LogLevel.Error,
        Message = "GetWebAppSlots failed | WebAppId: {WebAppId}")]
    public static partial void LogGetWebAppSlotsFailed(
        this ILogger logger,
        Exception exception,
        string webAppId);

    // ============================================================
    // Batch Operations (EventIds 9150-9169)
    // ============================================================

    [LoggerMessage(
        EventId = 9150,
        Level = LogLevel.Information,
        Message = "Starting batch resource lookup | BatchSize: {BatchSize} | TotalResources: {TotalResources}")]
    public static partial void LogBatchOperationStart(
        this ILogger logger,
        int batchSize,
        int totalResources);

    [LoggerMessage(
        EventId = 9151,
        Level = LogLevel.Information,
        Message = "Batch processing progress | ProcessedCount: {ProcessedCount} | TotalCount: {TotalCount} | SuccessCount: {SuccessCount}")]
    public static partial void LogBatchProgress(
        this ILogger logger,
        int processedCount,
        int totalCount,
        int successCount);

    [LoggerMessage(
        EventId = 9152,
        Level = LogLevel.Information,
        Message = "Batch operation complete | TotalProcessed: {TotalProcessed} | Duration: {DurationMs}ms | SuccessRate: {SuccessRate}%")]
    public static partial void LogBatchOperationComplete(
        this ILogger logger,
        int totalProcessed,
        long durationMs,
        double successRate);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for Resource Graph Explorer service operations.
    /// </summary>
    public static IDisposable? BeginResourceGraphScope(
        this ILogger logger,
        string methodName,
        string[]? subscriptionIds = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ServiceName"] = "ResourceGraphExplorerService",
        ["MethodName"] = methodName,
        ["SubscriptionCount"] = subscriptionIds?.Length ?? 0,
        ["CorrelationId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for query operations.
    /// </summary>
    public static IDisposable? BeginQueryScope(
        this ILogger logger,
        string queryType,
        string subscriptionId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["QueryType"] = queryType,
        ["SubscriptionId"] = subscriptionId,
        ["CorrelationId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N")
      });
    }

    /// <summary>
    /// Creates a logging scope for resource existence checks.
    /// </summary>
    public static IDisposable? BeginResourceExistsScope(
        this ILogger logger,
        string resourceId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "ResourceExistsCheck",
        ["ResourceId"] = resourceId ?? "Unknown",
        ["CorrelationId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N")
      });
    }

    /// <summary>
    /// Creates a logging scope for batch operations.
    /// </summary>
    public static IDisposable? BeginBatchOperationScope(
        this ILogger logger,
        string operationName,
        int batchSize)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "BatchOperation",
        ["OperationName"] = operationName,
        ["BatchSize"] = batchSize,
        ["CorrelationId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}