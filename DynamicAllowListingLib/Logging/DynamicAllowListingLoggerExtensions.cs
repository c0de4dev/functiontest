using DynamicAllowListingLib.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// Structured logging extensions for DynamicAllowListingService operations.
  /// Uses LoggerMessage source generators for high-performance, structured logging.
  /// </summary>
  public static partial class DynamicAllowListingLoggerExtensions
  {
    // ============================================================
    // Service Operation Lifecycle (EventIds 4000-4099)
    // ============================================================

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Starting {MethodName} | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogServiceOperationStart(
        this ILogger logger,
        string methodName,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Completed {MethodName} | ResourceId: {ResourceId} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogServiceOperationComplete(
        this ILogger logger,
        string methodName,
        string resourceId,
        long durationMs,
        bool success);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Error,
        Message = "Failed {MethodName} | ResourceId: {ResourceId} | Duration: {DurationMs}ms")]
    public static partial void LogServiceOperationFailed(
        this ILogger logger,
        Exception exception,
        string methodName,
        string resourceId,
        long durationMs);

    // ============================================================
    // Database Operations (EventIds 4100-4149)
    // ============================================================

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message = "Database update started | ResourceId: {ResourceId} | Operation: {Operation}")]
    public static partial void LogDbUpdateStart(
        this ILogger logger,
        string resourceId,
        string operation);

    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Information,
        Message = "Database update completed | ResourceId: {ResourceId} | Operation: {Operation} | Duration: {DurationMs}ms")]
    public static partial void LogDbUpdateComplete(
        this ILogger logger,
        string resourceId,
        string operation,
        long durationMs);

    [LoggerMessage(
        EventId = 4103,
        Level = LogLevel.Warning,
        Message = "Config not found in database | ResourceId: {ResourceId}")]
    public static partial void LogConfigNotFound(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 4104,
        Level = LogLevel.Information,
        Message = "Config found in database | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogConfigFound(
        this ILogger logger,
        string resourceId,
        string resourceName);

    // ============================================================
    // Network Restriction Operations (EventIds 4150-4199)
    // ============================================================

    [LoggerMessage(
        EventId = 4151,
        Level = LogLevel.Information,
        Message = "Overwriting network restrictions | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | SubnetRuleCount: {SubnetRuleCount}")]
    public static partial void LogNetworkRestrictionOverwriteStart(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int subnetRuleCount);

    [LoggerMessage(
        EventId = 4152,
        Level = LogLevel.Information,
        Message = "Network restrictions overwritten | ResourceId: {ResourceId} | Duration: {DurationMs}ms")]
    public static partial void LogNetworkRestrictionOverwriteComplete(
        this ILogger logger,
        string resourceId,
        long durationMs);

    [LoggerMessage(
        EventId = 4153,
        Level = LogLevel.Warning,
        Message = "Missing subnet detected and removed | SubnetId: {SubnetId} | ResourceId: {ResourceId}")]
    public static partial void LogMissingSubnetRemoved(
        this ILogger logger,
        string subnetId,
        string resourceId);

    [LoggerMessage(
        EventId = 4154,
        Level = LogLevel.Information,
        Message = "PrintOut mode active | ResourceId: {ResourceId} | GeneratedIPs: {IpCount} | GeneratedSubnets: {SubnetCount}")]
    public static partial void LogPrintOutMode(
        this ILogger logger,
        string resourceId,
        int ipCount,
        int subnetCount);

    // ============================================================
    // Unmanaged Resource Operations (EventIds 4200-4249)
    // ============================================================

    [LoggerMessage(
        EventId = 4201,
        Level = LogLevel.Information,
        Message = "Processing unmanaged resources | ResourceId: {ResourceId} | OutboundCount: {OutboundCount}")]
    public static partial void LogUnmanagedResourceProcessingStart(
        this ILogger logger,
        string resourceId,
        int outboundCount);

    [LoggerMessage(
        EventId = 4202,
        Level = LogLevel.Information,
        Message = "Appending network restrictions | TargetResourceId: {TargetResourceId} | SourceResourceId: {SourceResourceId}")]
    public static partial void LogAppendingNetworkRestrictions(
        this ILogger logger,
        string targetResourceId,
        string sourceResourceId);

    [LoggerMessage(
        EventId = 4203,
        Level = LogLevel.Information,
        Message = "Skipping DAL-managed resource | ResourceId: {ResourceId}")]
    public static partial void LogSkippingManagedResource(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 4204,
        Level = LogLevel.Warning,
        Message = "Unmanaged resource detected | ResourceId: {ResourceId} | Warning: Resource not managed by DAL may cause future issues")]
    public static partial void LogUnmanagedResourceWarning(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Provisioning Check Operations (EventIds 4250-4299)
    // ============================================================

    [LoggerMessage(
        EventId = 4251,
        Level = LogLevel.Information,
        Message = "Checking provisioning status | ResourceId: {ResourceId} | ResourceCount: {ResourceCount}")]
    public static partial void LogProvisioningCheckStart(
        this ILogger logger,
        string resourceId,
        int resourceCount);

    [LoggerMessage(
        EventId = 4252,
        Level = LogLevel.Information,
        Message = "Provisioning check passed | ResourceId: {ResourceId} | State: {ProvisioningState}")]
    public static partial void LogProvisioningCheckPassed(
        this ILogger logger,
        string resourceId,
        string provisioningState);

    [LoggerMessage(
        EventId = 4253,
        Level = LogLevel.Error,
        Message = "Provisioning check failed | ResourceId: {ResourceId} | State: {ProvisioningState} | Expected: Succeeded")]
    public static partial void LogProvisioningCheckFailed(
        this ILogger logger,
        string resourceId,
        string provisioningState);

    // ============================================================
    // Config Retrieval Operations (EventIds 4300-4349)
    // ============================================================

    [LoggerMessage(
        EventId = 4301,
        Level = LogLevel.Information,
        Message = "Retrieving configs for app service plan scale | AppServicePlanId: {AppServicePlanId} | HostedResourceCount: {ResourceCount}")]
    public static partial void LogAppServicePlanScaleConfigRetrieval(
        this ILogger logger,
        string appServicePlanId,
        int resourceCount);

    [LoggerMessage(
        EventId = 4302,
        Level = LogLevel.Information,
        Message = "Processing deleted web app | DeletedWebAppId: {WebAppId} | ConfigsUpdated: {ConfigCount}")]
    public static partial void LogDeletedWebAppProcessing(
        this ILogger logger,
        string webAppId,
        int configCount);

    [LoggerMessage(
        EventId = 4303,
        Level = LogLevel.Information,
        Message = "Retrieving outbound overwrite configs | ResourceId: {ResourceId} | OutboundResourceCount: {OutboundCount}")]
    public static partial void LogOutboundConfigRetrieval(
        this ILogger logger,
        string resourceId,
        int outboundCount);

    [LoggerMessage(
        EventId = 4304,
        Level = LogLevel.Information,
        Message = "Retrieved inbound configurations | ResourceId: {ResourceId} | InboundConfigCount: {ConfigCount}")]
    public static partial void LogInboundConfigsRetrieved(
        this ILogger logger,
        string resourceId,
        int configCount);

    // ============================================================
    // Validation Operations (EventIds 4350-4399)
    // ============================================================

    [LoggerMessage(
        EventId = 4351,
        Level = LogLevel.Warning,
        Message = "Validation failed | ResourceId: {ResourceId} | ValidationErrors: {Errors}")]
    public static partial void LogValidationFailed(
        this ILogger logger,
        string resourceId,
        string errors);

    [LoggerMessage(
        EventId = 4352,
        Level = LogLevel.Warning,
        Message = "Null or empty input | Field: {FieldName} | Context: {Context}")]
    public static partial void LogNullOrEmptyInput(
        this ILogger logger,
        string fieldName,
        string context);

    [LoggerMessage(
        EventId = 4353,
        Level = LogLevel.Warning,
        Message = "Outbound resources null or empty | ResourceId: {ResourceId}")]
    public static partial void LogOutboundResourcesEmpty(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Website Slot Operations (EventIds 4400-4449)
    // ============================================================

    [LoggerMessage(
        EventId = 4401,
        Level = LogLevel.Information,
        Message = "Processing website slot | SlotId: {SlotId} | MainResourceId: {MainResourceId}")]
    public static partial void LogWebsiteSlotProcessing(
        this ILogger logger,
        string slotId,
        string mainResourceId);

    [LoggerMessage(
        EventId = 4402,
        Level = LogLevel.Information,
        Message = "Website slot restrictions applied | SlotId: {SlotId} | Duration: {DurationMs}ms")]
    public static partial void LogWebsiteSlotRestrictionsApplied(
        this ILogger logger,
        string slotId,
        long durationMs);

    // ============================================================
    // Resource Not Found Operations (EventIds 4450-4499)
    // ============================================================

    [LoggerMessage(
        EventId = 4451,
        Level = LogLevel.Warning,
        Message = "Resource not found | ResourceId: {ResourceId} | Context: {Context}")]
    public static partial void LogResourceNotFound(
        this ILogger logger,
        string resourceId,
        string context);

    [LoggerMessage(
        EventId = 4452,
        Level = LogLevel.Information,
        Message = "Resource found | ResourceId: {ResourceId} | ResourceType: {ResourceType}")]
    public static partial void LogResourceFound(
        this ILogger logger,
        string resourceId,
        string resourceType);

    // ============================================================
    // Cross-Subscription Operations (EventIds 4500-4549)
    // ============================================================

    [LoggerMessage(
        EventId = 4501,
        Level = LogLevel.Information,
        Message = "Processing cross-subscription subnets | SkippedCount: {SkippedCount} | TotalSubnets: {TotalCount}")]
    public static partial void LogCrossSubscriptionSubnets(
        this ILogger logger,
        int skippedCount,
        int totalCount);

    [LoggerMessage(
        EventId = 4502,
        Level = LogLevel.Information,
        Message = "Fetching subnet IDs | SubscriptionId: {SubscriptionId} | SubnetsFound: {SubnetCount}")]
    public static partial void LogSubnetFetch(
        this ILogger logger,
        string subscriptionId,
        int subnetCount);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope with resource context for correlation.
    /// </summary>
    public static IDisposable? BeginResourceScope(
        this ILogger logger,
        ResourceDependencyInformation resourceInfo)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ResourceId"] = resourceInfo.ResourceId ?? "Unknown",
        ["ResourceName"] = resourceInfo.ResourceName ?? "Unknown",
        ["ResourceType"] = resourceInfo.ResourceType ?? "Unknown",
        ["SubscriptionId"] = resourceInfo.RequestSubscriptionId ?? "Unknown",
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }

    /// <summary>
    /// Creates a logging scope for a specific operation.
    /// </summary>
    public static IDisposable? BeginOperationScope(
        this ILogger logger,
        string operationName,
        string resourceId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationName"] = operationName,
        ["ResourceId"] = resourceId ?? "Unknown",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Logs comprehensive exception details with resource context.
    /// </summary>
    public static void LogServiceException(
        this ILogger logger,
        Exception ex,
        string methodName,
        ResourceDependencyInformation? resourceInfo = null,
        long? durationMs = null)
    {
      var context = new Dictionary<string, object>
      {
        ["MethodName"] = methodName,
        ["ExceptionType"] = ex.GetType().Name,
        ["ExceptionMessage"] = ex.Message,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      };

      if (durationMs.HasValue)
      {
        context["DurationMs"] = durationMs.Value;
      }

      if (resourceInfo != null)
      {
        context["ResourceId"] = resourceInfo.ResourceId ?? "Unknown";
        context["ResourceName"] = resourceInfo.ResourceName ?? "Unknown";
        context["ResourceType"] = resourceInfo.ResourceType ?? "Unknown";
        context["SubscriptionId"] = resourceInfo.RequestSubscriptionId ?? "Unknown";
      }

      if (ex.InnerException != null)
      {
        context["InnerExceptionType"] = ex.InnerException.GetType().Name;
        context["InnerExceptionMessage"] = ex.InnerException.Message;
      }

      using (logger.BeginScope(context))
      {
        logger.LogError(ex, "Service operation failed | Method: {MethodName} | ResourceId: {ResourceId}",
            methodName,
            resourceInfo?.ResourceId ?? "Unknown");
      }
    }
  }
}