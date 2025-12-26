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
  /// EVENT ID RANGE: 9000-9099 (Fixed to avoid conflicts with HelpersLoggerExtensions which uses 8000-8199)
  /// </summary>
  public static partial class ResourceGraphExplorerLoggerExtensions
  {

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
        Message = "Resource Graph query completed | ResourcesFound: {ResourceCount}")]
    public static partial void LogQueryComplete(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 9024,
        Level = LogLevel.Warning,
        Message = "No resources found for query")]
    public static partial void LogNoResourcesFound(this ILogger logger);

    [LoggerMessage(
        EventId = 9025,
        Level = LogLevel.Error,
        Message = "Resource Graph query failed | Query: {Query}")]
    public static partial void LogQueryFailed(
        this ILogger logger,
        Exception exception,
        string query);

    [LoggerMessage(
        EventId = 9026,
        Level = LogLevel.Debug,
        Message = "Resource Graph query details | Query: {Query} | SubscriptionIds: {SubscriptionIds}")]
    public static partial void LogQueryDetails(
        this ILogger logger,
        string query,
        string subscriptionIds);

    // ============================================================
    // GetVNets (EventIds 9040-9049)
    // ============================================================

    [LoggerMessage(
        EventId = 9040,
        Level = LogLevel.Information,
        Message = "Fetching VNets | SubscriptionId: {SubscriptionId}")]
    public static partial void LogFetchingVNets(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 9041,
        Level = LogLevel.Information,
        Message = "VNets retrieved | SubscriptionId: {SubscriptionId} | VNetCount: {VNetCount}")]
    public static partial void LogVNetsRetrieved(
        this ILogger logger,
        string subscriptionId,
        int vnetCount);

    [LoggerMessage(
        EventId = 9042,
        Level = LogLevel.Warning,
        Message = "No VNets found for subscription | SubscriptionId: {SubscriptionId}")]
    public static partial void LogNoVNetsFound(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 9043,
        Level = LogLevel.Error,
        Message = "Failed to fetch VNets | SubscriptionId: {SubscriptionId}")]
    public static partial void LogFetchVNetsFailed(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    // ============================================================
    // GetSubnets (EventIds 9050-9059)
    // ============================================================

    [LoggerMessage(
        EventId = 9050,
        Level = LogLevel.Information,
        Message = "Fetching subnets | VNetId: {VNetId}")]
    public static partial void LogFetchingSubnets(
        this ILogger logger,
        string vnetId);

    [LoggerMessage(
        EventId = 9051,
        Level = LogLevel.Information,
        Message = "Subnets retrieved | VNetId: {VNetId} | SubnetCount: {SubnetCount}")]
    public static partial void LogSubnetsRetrieved(
        this ILogger logger,
        string vnetId,
        int subnetCount);

    [LoggerMessage(
        EventId = 9052,
        Level = LogLevel.Warning,
        Message = "No subnets found | VNetId: {VNetId}")]
    public static partial void LogNoSubnetsFound(
        this ILogger logger,
        string vnetId);

    [LoggerMessage(
        EventId = 9053,
        Level = LogLevel.Error,
        Message = "Failed to fetch subnets | VNetId: {VNetId}")]
    public static partial void LogFetchSubnetsFailed(
        this ILogger logger,
        Exception exception,
        string vnetId);

    // ============================================================
    // ResourceExists (EventIds 9060-9069)
    // ============================================================

    [LoggerMessage(
        EventId = 9060,
        Level = LogLevel.Debug,
        Message = "Checking if resource exists | ResourceId: {ResourceId}")]
    public static partial void LogCheckingResourceExists(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 9061,
        Level = LogLevel.Information,
        Message = "Resource exists check complete | ResourceId: {ResourceId} | Exists: {Exists}")]
    public static partial void LogResourceExistsResult(
        this ILogger logger,
        string resourceId,
        bool exists);

    [LoggerMessage(
        EventId = 9062,
        Level = LogLevel.Error,
        Message = "Resource exists check failed | ResourceId: {ResourceId}")]
    public static partial void LogResourceExistsCheckFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // Batch Operations (EventIds 9070-9079)
    // ============================================================

    [LoggerMessage(
        EventId = 9070,
        Level = LogLevel.Information,
        Message = "Starting batch resource lookup | BatchSize: {BatchSize} | TotalResources: {TotalResources}")]
    public static partial void LogBatchOperationStart(
        this ILogger logger,
        int batchSize,
        int totalResources);

    [LoggerMessage(
        EventId = 9071,
        Level = LogLevel.Information,
        Message = "Batch processing progress | ProcessedCount: {ProcessedCount} | TotalCount: {TotalCount} | SuccessCount: {SuccessCount}")]
    public static partial void LogBatchProgress(
        this ILogger logger,
        int processedCount,
        int totalCount,
        int successCount);

    [LoggerMessage(
        EventId = 9072,
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