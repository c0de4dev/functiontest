using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for ResourceGraphExplorerService operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// </summary>
  public static partial class ResourceGraphExplorerLoggerExtensions
  {
    // ============================================================
    // Method Lifecycle (EventIds 8000-8019)
    // ============================================================

    [LoggerMessage(
        EventId = 8000,
        Level = LogLevel.Information,
        Message = "Starting method: {MethodName}")]
    public static partial void LogMethodStart(
        this ILogger logger,
        string methodName);

    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Information,
        Message = "Completed method: {MethodName} | Success: {Success}")]
    public static partial void LogMethodComplete(
        this ILogger logger,
        string methodName,
        bool success);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Error,
        Message = "Exception in method: {MethodName}")]
    public static partial void LogMethodException(
        this ILogger logger,
        Exception exception,
        string methodName);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Warning,
        Message = "{MethodName} | {Message}")]
    public static partial void LogMethodWarning(
        this ILogger logger,
        string methodName,
        string message);

    // ============================================================
    // GetResourceInstances (EventIds 8020-8039)
    // ============================================================

    [LoggerMessage(
        EventId = 8020,
        Level = LogLevel.Information,
        Message = "GetResourceInstances started | SubscriptionCount: {SubscriptionCount} | ResourceIdCount: {ResourceIdCount}")]
    public static partial void LogGetResourceInstancesStart(
        this ILogger logger,
        int subscriptionCount,
        int resourceIdCount);

    [LoggerMessage(
        EventId = 8021,
        Level = LogLevel.Warning,
        Message = "No resource IDs provided for GetResourceInstances")]
    public static partial void LogNoResourceIdsProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 8022,
        Level = LogLevel.Debug,
        Message = "Executing Resource Graph query | QueryLength: {QueryLength}")]
    public static partial void LogExecutingQuery(
        this ILogger logger,
        int queryLength);

    [LoggerMessage(
        EventId = 8023,
        Level = LogLevel.Information,
        Message = "GetResourceInstances completed | ResourcesRetrieved: {ResourceCount}")]
    public static partial void LogGetResourceInstancesComplete(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 8024,
        Level = LogLevel.Warning,
        Message = "GetResourceInstances returned null resources")]
    public static partial void LogNullResourcesReturned(this ILogger logger);

    [LoggerMessage(
        EventId = 8025,
        Level = LogLevel.Error,
        Message = "GetResourceInstances faileds")]
    public static partial void LogGetResourceInstancesFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // GetResourceGraphExplorerResponse (EventIds 8040-8059)
    // ============================================================

    [LoggerMessage(
        EventId = 8040,
        Level = LogLevel.Information,
        Message = "Resource Graph API request started | SubscriptionCount: {SubscriptionCount}")]
    public static partial void LogResourceGraphRequestStart(
        this ILogger logger,
        int subscriptionCount);

    [LoggerMessage(
        EventId = 8041,
        Level = LogLevel.Warning,
        Message = "Invalid parameters for Resource Graph request | HasSubscriptions: {HasSubscriptions} | HasQuery: {HasQuery}")]
    public static partial void LogInvalidResourceGraphParameters(
        this ILogger logger,
        bool hasSubscriptions,
        bool hasQuery);

    [LoggerMessage(
        EventId = 8042,
        Level = LogLevel.Debug,
        Message = "Resource Graph API request body prepared | BodyLength: {BodyLength}")]
    public static partial void LogResourceGraphRequestBody(
        this ILogger logger,
        int bodyLength);

    [LoggerMessage(
        EventId = 8043,
        Level = LogLevel.Information,
        Message = "Resource Graph API response received | ResponseSize: {ResponseSize}")]
    public static partial void LogResourceGraphResponseReceived(
        this ILogger logger,
        int responseSize);

    [LoggerMessage(
        EventId = 8044,
        Level = LogLevel.Warning,
        Message = "Resource Graph API returned empty response")]
    public static partial void LogEmptyResourceGraphResponse(this ILogger logger);

    [LoggerMessage(
        EventId = 8045,
        Level = LogLevel.Error,
        Message = "Resource Graph API request failed")]
    public static partial void LogResourceGraphRequestFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // GetExistingResourceIds (EventIds 8060-8079)
    // ============================================================

    [LoggerMessage(
        EventId = 8060,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIds started | SubscriptionId: {SubscriptionId} | ResourceListCount: {ResourceListCount}")]
    public static partial void LogGetExistingResourceIdsStart(
        this ILogger logger,
        string subscriptionId,
        int resourceListCount);

    [LoggerMessage(
        EventId = 8061,
        Level = LogLevel.Warning,
        Message = "Invalid parameters for GetExistingResourceIds | HasSubscriptionId: {HasSubscriptionId} | HasResourceList: {HasResourceList}")]
    public static partial void LogInvalidGetExistingResourceIdsParameters(
        this ILogger logger,
        bool hasSubscriptionId,
        bool hasResourceList);

    [LoggerMessage(
        EventId = 8062,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIds completed | ExistingResourceCount: {ExistingCount}")]
    public static partial void LogGetExistingResourceIdsComplete(
        this ILogger logger,
        int existingCount);

    [LoggerMessage(
        EventId = 8063,
        Level = LogLevel.Warning,
        Message = "No data found in Resource Graph Explorer response for GetExistingResourceIds")]
    public static partial void LogNoDataInResourceGraphResponse(this ILogger logger);

    [LoggerMessage(
        EventId = 8064,
        Level = LogLevel.Error,
        Message = "GetExistingResourceIds failed | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetExistingResourceIdsFailed(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    // ============================================================
    // GetExistingResourceIdsByType (EventIds 8080-8099)
    // ============================================================

    [LoggerMessage(
        EventId = 8080,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIdsByType started | SubscriptionId: {SubscriptionId} | ResourceTypeCount: {ResourceTypeCount}")]
    public static partial void LogGetExistingResourceIdsByTypeStart(
        this ILogger logger,
        string subscriptionId,
        int resourceTypeCount);

    [LoggerMessage(
        EventId = 8081,
        Level = LogLevel.Warning,
        Message = "Invalid parameters for GetExistingResourceIdsByType | HasSubscriptionId: {HasSubscriptionId} | HasResourceTypes: {HasResourceTypes}")]
    public static partial void LogInvalidGetExistingResourceIdsByTypeParameters(
        this ILogger logger,
        bool hasSubscriptionId,
        bool hasResourceTypes);

    [LoggerMessage(
        EventId = 8082,
        Level = LogLevel.Debug,
        Message = "Resource Graph query executed for types | Query: {QueryPreview}")]
    public static partial void LogResourceGraphQueryExecuted(
        this ILogger logger,
        string queryPreview);

    [LoggerMessage(
        EventId = 8083,
        Level = LogLevel.Information,
        Message = "Initial response received | ResourceCount: {ResourceCount}")]
    public static partial void LogInitialResponseReceived(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 8084,
        Level = LogLevel.Warning,
        Message = "Empty or null response data received")]
    public static partial void LogEmptyOrNullResponseData(this ILogger logger);

    [LoggerMessage(
        EventId = 8085,
        Level = LogLevel.Warning,
        Message = "No valid resource IDs found in response")]
    public static partial void LogNoValidResourceIdsInResponse(this ILogger logger);

    [LoggerMessage(
        EventId = 8086,
        Level = LogLevel.Debug,
        Message = "Processing pagination | SkipToken present")]
    public static partial void LogProcessingPagination(this ILogger logger);

    [LoggerMessage(
        EventId = 8087,
        Level = LogLevel.Debug,
        Message = "Pagination page processed | PageResourceCount: {PageCount}")]
    public static partial void LogPaginationPageProcessed(
        this ILogger logger,
        int pageCount);

    [LoggerMessage(
        EventId = 8088,
        Level = LogLevel.Information,
        Message = "GetExistingResourceIdsByType completed | TotalResourceIds: {TotalCount}")]
    public static partial void LogGetExistingResourceIdsByTypeComplete(
        this ILogger logger,
        int totalCount);

    [LoggerMessage(
        EventId = 8089,
        Level = LogLevel.Error,
        Message = "GetExistingResourceIdsByType failed | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetExistingResourceIdsByTypeFailed(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    // ============================================================
    // ResourceExists (EventIds 8100-8119)
    // ============================================================

    [LoggerMessage(
        EventId = 8100,
        Level = LogLevel.Information,
        Message = "ResourceExists check started | ResourceId: {ResourceId}")]
    public static partial void LogResourceExistsStart(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 8101,
        Level = LogLevel.Warning,
        Message = "ResourceExists check skipped - ResourceId is null or empty")]
    public static partial void LogResourceIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 8102,
        Level = LogLevel.Information,
        Message = "ResourceExists check completed | ResourceId: {ResourceId} | Exists: {Exists}")]
    public static partial void LogResourceExistsComplete(
        this ILogger logger,
        string resourceId,
        bool exists);

    [LoggerMessage(
        EventId = 8103,
        Level = LogLevel.Error,
        Message = "ResourceExists check failed | ResourceId: {ResourceId}")]
    public static partial void LogResourceExistsFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // GetAllSubnetIds (EventIds 8120-8139)
    // ============================================================

    [LoggerMessage(
        EventId = 8120,
        Level = LogLevel.Information,
        Message = "GetAllSubnetIds started | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetAllSubnetIdsStart(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 8121,
        Level = LogLevel.Information,
        Message = "GetAllSubnetIds completed | SubnetCount: {SubnetCount}")]
    public static partial void LogGetAllSubnetIdsComplete(
        this ILogger logger,
        int subnetCount);

    [LoggerMessage(
        EventId = 8122,
        Level = LogLevel.Error,
        Message = "GetAllSubnetIds failed | SubscriptionId: {SubscriptionId}")]
    public static partial void LogGetAllSubnetIdsFailed(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    // ============================================================
    // GetResourcesHostedOnPlan (EventIds 8140-8159)
    // ============================================================

    [LoggerMessage(
        EventId = 8140,
        Level = LogLevel.Information,
        Message = "GetResourcesHostedOnPlan started | AppServicePlanResourceId: {AppServicePlanResourceId}")]
    public static partial void LogGetResourcesHostedOnPlanStart(
        this ILogger logger,
        string appServicePlanResourceId);

    [LoggerMessage(
        EventId = 8141,
        Level = LogLevel.Warning,
        Message = "AppServicePlanResourceId is null or empty")]
    public static partial void LogAppServicePlanResourceIdEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 8142,
        Level = LogLevel.Information,
        Message = "GetResourcesHostedOnPlan completed | ResourceCount: {ResourceCount}")]
    public static partial void LogGetResourcesHostedOnPlanComplete(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 8143,
        Level = LogLevel.Warning,
        Message = "No resources found hosted on App Service Plan")]
    public static partial void LogNoResourcesFoundOnPlan(this ILogger logger);

    [LoggerMessage(
        EventId = 8144,
        Level = LogLevel.Error,
        Message = "GetResourcesHostedOnPlan failed | AppServicePlanResourceId: {AppServicePlanResourceId}")]
    public static partial void LogGetResourcesHostedOnPlanFailed(
        this ILogger logger,
        Exception exception,
        string appServicePlanResourceId);

    // ============================================================
    // GetFrontDoorUniqueInstanceIds (EventIds 8160-8179)
    // ============================================================

    [LoggerMessage(
        EventId = 8160,
        Level = LogLevel.Information,
        Message = "GetFrontDoorUniqueInstanceIds started | ResourceIdCount: {ResourceIdCount}")]
    public static partial void LogGetFrontDoorIdsStart(
        this ILogger logger,
        int resourceIdCount);

    [LoggerMessage(
        EventId = 8161,
        Level = LogLevel.Warning,
        Message = "No distinct resource IDs found for Front Door instance retrieval")]
    public static partial void LogNoDistinctFrontDoorIds(this ILogger logger);

    [LoggerMessage(
        EventId = 8162,
        Level = LogLevel.Information,
        Message = "GetFrontDoorUniqueInstanceIds completed | FrontDoorCount: {FrontDoorCount}")]
    public static partial void LogGetFrontDoorIdsComplete(
        this ILogger logger,
        int frontDoorCount);

    [LoggerMessage(
        EventId = 8163,
        Level = LogLevel.Error,
        Message = "Missing resource ID or FDID for Front Door resource | ResourceId: {ResourceId}")]
    public static partial void LogMissingFrontDoorIdOrFdid(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 8164,
        Level = LogLevel.Error,
        Message = "GetFrontDoorUniqueInstanceIds failed")]
    public static partial void LogGetFrontDoorIdsFailed(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // GetWebAppSlots (EventIds 8180-8199)
    // ============================================================

    [LoggerMessage(
        EventId = 8180,
        Level = LogLevel.Information,
        Message = "GetWebAppSlots started | WebAppId: {WebAppId}")]
    public static partial void LogGetWebAppSlotsStart(
        this ILogger logger,
        string webAppId);

    [LoggerMessage(
        EventId = 8181,
        Level = LogLevel.Warning,
        Message = "WebAppId is null or empty for GetWebAppSlots")]
    public static partial void LogWebAppIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 8182,
        Level = LogLevel.Information,
        Message = "GetWebAppSlots completed | WebAppId: {WebAppId} | SlotsFound: {SlotCount}")]
    public static partial void LogGetWebAppSlotsComplete(
        this ILogger logger,
        string webAppId,
        int slotCount);

    [LoggerMessage(
        EventId = 8183,
        Level = LogLevel.Error,
        Message = "GetWebAppSlots failed | WebAppId: {WebAppId}")]
    public static partial void LogGetWebAppSlotsFailed(
        this ILogger logger,
        Exception exception,
        string webAppId);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for Resource Graph Explorer operations.
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
        ["CorrelationId"] = CorrelationContext.CorrelationId,
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
        ["CorrelationId"] = CorrelationContext.CorrelationId
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
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }
  }
}