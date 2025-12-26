using DynamicAllowListingLib.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for IpRestrictionService operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// </summary>
  public static partial class IpRestrictionServiceLoggerExtensions
  {
    // ============================================================
    // Method Lifecycle (EventIds 6000-6019)
    // ============================================================

    [LoggerMessage(
        EventId = 6000,
        Level = LogLevel.Information,
        Message = "Starting method: {MethodName}")]
    public static partial void LogMethodStart(
        this ILogger logger,
        string methodName);

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Information,
        Message = "Completed method: {MethodName} | Success: {Success}")]
    public static partial void LogMethodComplete(
        this ILogger logger,
        string methodName,
        bool success);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Error,
        Message = "Exception in method: {MethodName}s")]
    public static partial void LogMethodException(
        this ILogger logger,
        Exception exception,
        string methodName);

    [LoggerMessage(
        EventId = 6003,
        Level = LogLevel.Warning,
        Message = "{MethodName} | {Message}")]
    public static partial void LogMethodWarning(
        this ILogger logger,
        string methodName,
        string message);

    // ============================================================
    // FindRelatedDependencyConfigs (EventIds 6020-6039)
    // ============================================================

    [LoggerMessage(
        EventId = 6020,
        Level = LogLevel.Information,
        Message = "Finding related dependency configs | ServiceTagCount: {ServiceTagCount}")]
    public static partial void LogFindingRelatedDependencyConfigs(
        this ILogger logger,
        int serviceTagCount);

    [LoggerMessage(
        EventId = 6021,
        Level = LogLevel.Information,
        Message = "Service tag is mandatory for current subscription | Fetching all dependency configs")]
    public static partial void LogMandatoryServiceTag(this ILogger logger);

    [LoggerMessage(
        EventId = 6022,
        Level = LogLevel.Information,
        Message = "Network restriction configs found | ConfigCount: {ConfigCount}")]
    public static partial void LogNetworkRestrictionConfigsFound(
        this ILogger logger,
        int configCount);

    [LoggerMessage(
        EventId = 6023,
        Level = LogLevel.Information,
        Message = "No network restriction configs found for the provided service tags")]
    public static partial void LogNoNetworkRestrictionConfigsFound(this ILogger logger);

    [LoggerMessage(
        EventId = 6024,
        Level = LogLevel.Information,
        Message = "Dependency configs lookup completed | ServiceTag: {ServiceTagName} | ConfigsFound: {ConfigCount}")]
    public static partial void LogDependencyConfigsLookupComplete(
        this ILogger logger,
        string serviceTagName,
        int configCount);

    // ============================================================
    // RemoveInvalidResourceConfigs (EventIds 6040-6059)
    // ============================================================

    [LoggerMessage(
        EventId = 6040,
        Level = LogLevel.Information,
        Message = "Starting validation of resource configs | TotalConfigs: {TotalConfigs}")]
    public static partial void LogStartingConfigValidation(
        this ILogger logger,
        int totalConfigs);

    [LoggerMessage(
        EventId = 6041,
        Level = LogLevel.Warning,
        Message = "Found duplicated resource IDs | DuplicateCount: {DuplicateCount} | ResourceIds: {ResourceIds}")]
    public static partial void LogDuplicatedResourceIds(
        this ILogger logger,
        int duplicateCount,
        string resourceIds);

    [LoggerMessage(
        EventId = 6042,
        Level = LogLevel.Warning,
        Message = "Found invalid format resource IDs | InvalidCount: {InvalidCount} | ResourceIds: {ResourceIds}")]
    public static partial void LogInvalidFormatResourceIds(
        this ILogger logger,
        int invalidCount,
        string resourceIds);

    [LoggerMessage(
        EventId = 6043,
        Level = LogLevel.Warning,
        Message = "Found nonexistent resource IDs | NonexistentCount: {NonexistentCount} | ResourceIds: {ResourceIds}")]
    public static partial void LogNonexistentResourceIds(
        this ILogger logger,
        int nonexistentCount,
        string resourceIds);

    [LoggerMessage(
        EventId = 6044,
        Level = LogLevel.Information,
        Message = "Config validation completed | ValidConfigs: {ValidCount} | InvalidConfigs: {InvalidCount}")]
    public static partial void LogConfigValidationComplete(
        this ILogger logger,
        int validCount,
        int invalidCount);

    [LoggerMessage(
        EventId = 6045,
        Level = LogLevel.Warning,
        Message = "Invalid configs found | Type: {InvalidType} | Count: {Count} | ResourceIds: {ResourceIds}")]
    public static partial void LogInvalidConfigs(
        this ILogger logger,
        string invalidType,
        int count,
        string resourceIds);

    // ============================================================
    // Database Operations (EventIds 6060-6079)
    // ============================================================

    [LoggerMessage(
        EventId = 6060,
        Level = LogLevel.Information,
        Message = "Removing invalid configs from database | Count: {Count}")]
    public static partial void LogRemovingInvalidConfigsFromDb(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 6061,
        Level = LogLevel.Information,
        Message = "Removed resource IDs from database | ResourceIds: {ResourceIds}")]
    public static partial void LogRemovedResourceIdsFromDb(
        this ILogger logger,
        string resourceIds);

    [LoggerMessage(
        EventId = 6062,
        Level = LogLevel.Information,
        Message = "Config updated in database | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogConfigUpdatedInDb(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 6063,
        Level = LogLevel.Error,
        Message = "Failed to remove config from database | ResourceId: {ResourceId}")]
    public static partial void LogRemoveConfigFromDbFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 6064,
        Level = LogLevel.Error,
        Message = "Failed to update config in database | ResourceId: {ResourceId}")]
    public static partial void LogUpdateConfigInDbFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // Subscription Operations (EventIds 6080-6099)
    // ============================================================

    [LoggerMessage(
        EventId = 6080,
        Level = LogLevel.Information,
        Message = "Checking if service tag is mandatory for subscription | SubscriptionName: {SubscriptionName}")]
    public static partial void LogCheckingMandatoryTag(
        this ILogger logger,
        string subscriptionName);

    [LoggerMessage(
        EventId = 6081,
        Level = LogLevel.Information,
        Message = "Mandatory tag check completed | SubscriptionName: {SubscriptionName} | IsMandatory: {IsMandatory}")]
    public static partial void LogMandatoryTagCheckResult(
        this ILogger logger,
        string subscriptionName,
        bool isMandatory);

    [LoggerMessage(
        EventId = 6082,
        Level = LogLevel.Warning,
        Message = "Subscription not found | SubscriptionId: {SubscriptionId}")]
    public static partial void LogSubscriptionNotFound(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 6083,
        Level = LogLevel.Information,
        Message = "Retrieved subscription name | SubscriptionId: {SubscriptionId} | SubscriptionName: {SubscriptionName}")]
    public static partial void LogSubscriptionNameRetrieved(
        this ILogger logger,
        string subscriptionId,
        string subscriptionName);

    // ============================================================
    // Resource Existence Check (EventIds 6100-6119)
    // ============================================================

    [LoggerMessage(
        EventId = 6100,
        Level = LogLevel.Information,
        Message = "Checking resource existence | SubscriptionId: {SubscriptionId} | ResourceCount: {ResourceCount}")]
    public static partial void LogCheckingResourceExistence(
        this ILogger logger,
        string subscriptionId,
        int resourceCount);

    [LoggerMessage(
        EventId = 6101,
        Level = LogLevel.Information,
        Message = "Resource existence check completed | ExistingCount: {ExistingCount} | NonexistentCount: {NonexistentCount}")]
    public static partial void LogResourceExistenceCheckComplete(
        this ILogger logger,
        int existingCount,
        int nonexistentCount);

    [LoggerMessage(
        EventId = 6102,
        Level = LogLevel.Warning,
        Message = "No configs provided for existence check")]
    public static partial void LogNoConfigsForExistenceCheck(this ILogger logger);

    // ============================================================
    // Service Tag Operations (EventIds 6120-6139)
    // ============================================================

    [LoggerMessage(
        EventId = 6120,
        Level = LogLevel.Information,
        Message = "Finding dependency configs for service tag | ServiceTagId: {ServiceTagId} | ServiceTagName: {ServiceTagName}")]
    public static partial void LogFindingConfigsForServiceTag(
        this ILogger logger,
        string serviceTagId,
        string serviceTagName);

    [LoggerMessage(
        EventId = 6121,
        Level = LogLevel.Information,
        Message = "Dependency configs found for service tag | ServiceTagName: {ServiceTagName} | ConfigCount: {ConfigCount}")]
    public static partial void LogDependencyConfigsFoundForTag(
        this ILogger logger,
        string serviceTagName,
        int configCount);

    // ============================================================
    // GetValidDependencyConfigs Operations (EventIds 6140-6159)
    // ============================================================

    [LoggerMessage(
        EventId = 6140,
        Level = LogLevel.Warning,
        Message = "No updated service tags provided | Returning empty configuration list")]
    public static partial void LogNoUpdatedServiceTagsProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 6141,
        Level = LogLevel.Information,
        Message = "Fetching related dependency configurations | ServiceTagCount: {ServiceTagCount}")]
    public static partial void LogFetchingRelatedDependencyConfigs(
        this ILogger logger,
        int serviceTagCount);

    [LoggerMessage(
        EventId = 6142,
        Level = LogLevel.Information,
        Message = "No related dependency configs found | Returning empty configuration list")]
    public static partial void LogNoRelatedDependencyConfigsFound(this ILogger logger);

    [LoggerMessage(
        EventId = 6143,
        Level = LogLevel.Information,
        Message = "Validating dependency configurations | ConfigCount: {ConfigCount}")]
    public static partial void LogValidatingDependencyConfigs(
        this ILogger logger,
        int configCount);

    [LoggerMessage(
        EventId = 6144,
        Level = LogLevel.Information,
        Message = "Updating resources | ResourceCount: {ResourceCount} | ResourceNames: {ResourceNames}")]
    public static partial void LogUpdatingResources(
        this ILogger logger,
        int resourceCount,
        string resourceNames);

    [LoggerMessage(
        EventId = 6145,
        Level = LogLevel.Information,
        Message = "GetValidDependencyConfigs completed | TotalConfigs: {TotalConfigs}")]
    public static partial void LogGetValidDependencyConfigsComplete(
        this ILogger logger,
        int totalConfigs);

    // ============================================================
    // Deleted Service Tag Operations (EventIds 6160-6179)
    // ============================================================

    [LoggerMessage(
        EventId = 6160,
        Level = LogLevel.Information,
        Message = "Identifying deleted service tags | InputCount: {InputCount}")]
    public static partial void LogIdentifyingDeletedServiceTags(
        this ILogger logger,
        int inputCount);

    [LoggerMessage(
        EventId = 6161,
        Level = LogLevel.Information,
        Message = "Deleted service tags identified | DeletedCount: {DeletedCount} | TagIds: {TagIds}")]
    public static partial void LogDeletedServiceTagsIdentified(
        this ILogger logger,
        int deletedCount,
        string tagIds);

    [LoggerMessage(
        EventId = 6162,
        Level = LogLevel.Information,
        Message = "No deleted service tags found")]
    public static partial void LogNoDeletedServiceTagsFound(this ILogger logger);

    [LoggerMessage(
        EventId = 6163,
        Level = LogLevel.Information,
        Message = "Removing deleted service tags from configs | DeletedTagCount: {DeletedTagCount} | ConfigCount: {ConfigCount}")]
    public static partial void LogRemovingDeletedServiceTagsFromConfigs(
        this ILogger logger,
        int deletedTagCount,
        int configCount);

    [LoggerMessage(
        EventId = 6164,
        Level = LogLevel.Information,
        Message = "Removed tag from resource config | TagName: {TagName} | ResourceId: {ResourceId}")]
    public static partial void LogRemovedTagFromResourceConfig(
        this ILogger logger,
        string tagName,
        string resourceId);

    [LoggerMessage(
        EventId = 6165,
        Level = LogLevel.Information,
        Message = "RemoveDeletedServiceTagsFromConfig completed | ModifiedConfigCount: {ModifiedConfigCount}")]
    public static partial void LogRemoveDeletedServiceTagsComplete(
        this ILogger logger,
        int modifiedConfigCount);

    [LoggerMessage(
        EventId = 6166,
        Level = LogLevel.Warning,
        Message = "No deleted service tags provided | Returning unmodified config list")]
    public static partial void LogNoDeletedServiceTagsProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 6167,
        Level = LogLevel.Warning,
        Message = "No dependency configurations provided | Returning empty list")]
    public static partial void LogNoDependencyConfigsProvided(this ILogger logger);

    [LoggerMessage(
        EventId = 6168,
        Level = LogLevel.Information,
        Message = "Removing deleted service tags from database | Count: {Count}")]
    public static partial void LogRemovingDeletedServiceTagsFromDb(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 6169,
        Level = LogLevel.Warning,
        Message = "No service tags provided or empty set | Returning empty list")]
    public static partial void LogNoServiceTagsProvided(this ILogger logger);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for IP restriction service operations.
    /// </summary>
    public static IDisposable? BeginIpRestrictionScope(
        this ILogger logger,
        string methodName,
        int serviceTagCount = 0)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ServiceName"] = "IpRestrictionService",
        ["MethodName"] = methodName,
        ["ServiceTagCount"] = serviceTagCount,
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for config validation operations.
    /// </summary>
    public static IDisposable? BeginConfigValidationScope(
        this ILogger logger,
        int totalConfigs)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "ConfigValidation",
        ["TotalConfigs"] = totalConfigs,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }

    /// <summary>
    /// Creates a logging scope for subscription operations.
    /// </summary>
    public static IDisposable? BeginSubscriptionScope(
        this ILogger logger,
        string subscriptionId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "SubscriptionOperation",
        ["SubscriptionId"] = subscriptionId,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }

    /// <summary>
    /// Creates a logging scope for service tag operations.
    /// </summary>
    public static IDisposable? BeginServiceTagScope(
        this ILogger logger,
        string operationType,
        int serviceTagCount)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ServiceName"] = "IpRestrictionServiceForServiceTag",
        ["OperationType"] = operationType,
        ["ServiceTagCount"] = serviceTagCount,
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for deleted service tag operations.
    /// </summary>
    public static IDisposable? BeginDeletedTagScope(
        this ILogger logger,
        int deletedTagCount,
        int configCount)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "DeletedServiceTagRemoval",
        ["DeletedTagCount"] = deletedTagCount,
        ["ConfigCount"] = configCount,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }
  }
}