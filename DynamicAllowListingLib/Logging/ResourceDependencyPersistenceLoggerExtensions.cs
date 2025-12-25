using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for ResourceDependencyInformationPersistenceService operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// </summary>
  public static partial class ResourceDependencyPersistenceLoggerExtensions
  {
    // ============================================================
    // Method Lifecycle (EventIds 7000-7019)
    // ============================================================

    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Information,
        Message = "Starting method: {MethodName}")]
    public static partial void LogMethodStart(
        this ILogger logger,
        string methodName);

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Information,
        Message = "Completed method: {MethodName} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogMethodComplete(
        this ILogger logger,
        string methodName,
        long durationMs,
        bool success);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Error,
        Message = "Exception in method: {MethodName} | Duration: {DurationMs}ms")]
    public static partial void LogMethodException(
        this ILogger logger,
        Exception exception,
        string methodName,
        long durationMs);

    // ============================================================
    // CreateOrReplaceItemInDb (EventIds 7020-7039)
    // ============================================================

    [LoggerMessage(
        EventId = 7020,
        Level = LogLevel.Information,
        Message = "ResourceDependencyInformation is null, skipping DB operation")]
    public static partial void LogNullResourceDependencyInfo(this ILogger logger);

    [LoggerMessage(
        EventId = 7021,
        Level = LogLevel.Information,
        Message = "Empty Resource ID, skipping DB operation")]
    public static partial void LogEmptyResourceId(this ILogger logger);

    [LoggerMessage(
        EventId = 7022,
        Level = LogLevel.Information,
        Message = "Attempting to replace item in DB | DocumentId: {DocumentId}")]
    public static partial void LogAttemptingReplaceItem(
        this ILogger logger,
        string documentId);

    [LoggerMessage(
        EventId = 7023,
        Level = LogLevel.Information,
        Message = "Item replaced successfully | DocumentId: {DocumentId}")]
    public static partial void LogItemReplaced(
        this ILogger logger,
        string documentId);

    [LoggerMessage(
        EventId = 7024,
        Level = LogLevel.Information,
        Message = "Item not found in DB, creating new item | DocumentId: {DocumentId}")]
    public static partial void LogItemNotFoundCreating(
        this ILogger logger,
        string documentId);

    [LoggerMessage(
        EventId = 7025,
        Level = LogLevel.Information,
        Message = "Item created successfully | DocumentId: {DocumentId}")]
    public static partial void LogItemCreated(
        this ILogger logger,
        string documentId);

    [LoggerMessage(
        EventId = 7026,
        Level = LogLevel.Error,
        Message = "Failed to create item in DB | DocumentId: {DocumentId}")]
    public static partial void LogCreateItemFailed(
        this ILogger logger,
        Exception exception,
        string documentId);

    [LoggerMessage(
        EventId = 7027,
        Level = LogLevel.Error,
        Message = "Unexpected error during CreateOrReplaceItemInDb | DocumentId: {DocumentId}")]
    public static partial void LogCreateOrReplaceUnexpectedError(
        this ILogger logger,
        Exception exception,
        string documentId);

    // ============================================================
    // RemoveConfigAndDependencies (EventIds 7040-7059)
    // ============================================================

    [LoggerMessage(
        EventId = 7040,
        Level = LogLevel.Warning,
        Message = "ResourceId is null or empty, cannot remove config and dependencies")]
    public static partial void LogRemoveConfigEmptyResourceId(this ILogger logger);

    [LoggerMessage(
        EventId = 7041,
        Level = LogLevel.Information,
        Message = "Removing config for ResourceId: {ResourceId}")]
    public static partial void LogRemovingConfigForResourceId(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7042,
        Level = LogLevel.Information,
        Message = "Found {Count} configs where ResourceId is inbound | ResourceId: {ResourceId}")]
    public static partial void LogFoundInboundConfigs(
        this ILogger logger,
        int count,
        string resourceId);

    [LoggerMessage(
        EventId = 7043,
        Level = LogLevel.Information,
        Message = "Updating document after removing ResourceId | DocumentId: {DocumentId} | ResourceId: {ResourceId}")]
    public static partial void LogUpdatingDocumentAfterRemoval(
        this ILogger logger,
        string documentId,
        string resourceId);

    [LoggerMessage(
        EventId = 7044,
        Level = LogLevel.Information,
        Message = "RemoveConfigAndDependencies completed | ResourceId: {ResourceId} | UpdatedItemCount: {UpdatedCount}")]
    public static partial void LogRemoveConfigAndDependenciesComplete(
        this ILogger logger,
        string resourceId,
        int updatedCount);

    [LoggerMessage(
        EventId = 7045,
        Level = LogLevel.Error,
        Message = "Error while processing dependency documents | ResourceId: {ResourceId}")]
    public static partial void LogProcessingDependencyDocumentsError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7046,
        Level = LogLevel.Error,
        Message = "Critical failure in RemoveConfigAndDependencies | ResourceId: {ResourceId}")]
    public static partial void LogRemoveConfigAndDependenciesCriticalFailure(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // RemoveConfig (EventIds 7060-7079)
    // ============================================================

    [LoggerMessage(
        EventId = 7060,
        Level = LogLevel.Warning,
        Message = "ResourceId is empty or null, cannot proceed with removal")]
    public static partial void LogRemoveConfigInvalidResourceId(this ILogger logger);

    [LoggerMessage(
        EventId = 7061,
        Level = LogLevel.Information,
        Message = "Successfully deleted document from Cosmos DB | DocumentId: {DocumentId} | ResourceId: {ResourceId}")]
    public static partial void LogDocumentDeletedSuccessfully(
        this ILogger logger,
        string documentId,
        string resourceId);

    [LoggerMessage(
        EventId = 7062,
        Level = LogLevel.Warning,
        Message = "Document not found for deletion | ResourceId: {ResourceId} | DocumentId: {DocumentId}")]
    public static partial void LogDocumentNotFoundForDeletion(
        this ILogger logger,
        string resourceId,
        string documentId);

    [LoggerMessage(
        EventId = 7063,
        Level = LogLevel.Error,
        Message = "Error deleting document from Cosmos DB | ResourceId: {ResourceId} | DocumentId: {DocumentId}")]
    public static partial void LogDeleteDocumentError(
        this ILogger logger,
        Exception exception,
        string resourceId,
        string documentId);

    [LoggerMessage(
        EventId = 7064,
        Level = LogLevel.Error,
        Message = "Unexpected error removing configuration | ResourceId: {ResourceId}")]
    public static partial void LogRemoveConfigUnexpectedError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // GetResourceIdsWhereOutbound (EventIds 7080-7099)
    // ============================================================

    [LoggerMessage(
        EventId = 7080,
        Level = LogLevel.Information,
        Message = "Provided resourceId is null or empty, skipping query")]
    public static partial void LogGetResourceIdsWhereOutboundSkipped(this ILogger logger);

    [LoggerMessage(
        EventId = 7081,
        Level = LogLevel.Information,
        Message = "Found {Count} resource IDs where outbound | ResourceId: {ResourceId}")]
    public static partial void LogFoundResourceIdsWhereOutbound(
        this ILogger logger,
        int count,
        string resourceId);

    [LoggerMessage(
        EventId = 7082,
        Level = LogLevel.Error,
        Message = "Cosmos DB error retrieving outbound resource IDs | ResourceId: {ResourceId}")]
    public static partial void LogGetResourceIdsWhereOutboundCosmosError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7083,
        Level = LogLevel.Error,
        Message = "Unexpected error retrieving outbound resource IDs | ResourceId: {ResourceId}")]
    public static partial void LogGetResourceIdsWhereOutboundError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // GetConfigsWhereInbound (EventIds 7100-7119)
    // ============================================================

    [LoggerMessage(
        EventId = 7100,
        Level = LogLevel.Information,
        Message = "Provided resourceId is null or empty, skipping inbound query")]
    public static partial void LogGetConfigsWhereInboundSkipped(this ILogger logger);

    [LoggerMessage(
        EventId = 7101,
        Level = LogLevel.Information,
        Message = "Completed inbound config retrieval | ConfigCount: {ConfigCount} | ResourceId: {ResourceId}")]
    public static partial void LogInboundConfigRetrievalComplete(
        this ILogger logger,
        int configCount,
        string resourceId);

    [LoggerMessage(
        EventId = 7102,
        Level = LogLevel.Error,
        Message = "Cosmos DB exception retrieving inbound configurations | ResourceId: {ResourceId}")]
    public static partial void LogGetConfigsWhereInboundCosmosError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7103,
        Level = LogLevel.Error,
        Message = "Error processing GetConfigsWhereInbound | ResourceId: {ResourceId}")]
    public static partial void LogGetConfigsWhereInboundError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // RemoveResourceIdFromDocument (EventIds 7120-7139)
    // ============================================================

    [LoggerMessage(
        EventId = 7120,
        Level = LogLevel.Information,
        Message = "Document or Resource ID is null, skipping removal")]
    public static partial void LogRemoveResourceIdFromDocumentSkipped(this ILogger logger);

    [LoggerMessage(
        EventId = 7121,
        Level = LogLevel.Information,
        Message = "Removed ResourceId from AllowInbound.SecurityRestrictions.ResourceIds | ResourceId: {ResourceId}")]
    public static partial void LogRemovedFromInboundSecurityRestrictions(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7122,
        Level = LogLevel.Information,
        Message = "Removed ResourceId from AllowInbound.ScmSecurityRestrictions.ResourceIds | ResourceId: {ResourceId}")]
    public static partial void LogRemovedFromScmSecurityRestrictions(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7123,
        Level = LogLevel.Information,
        Message = "Removed ResourceId from AllowOutbound.ResourceIds | ResourceId: {ResourceId}")]
    public static partial void LogRemovedFromOutboundResourceIds(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7124,
        Level = LogLevel.Error,
        Message = "Error removing ResourceId from document | ResourceId: {ResourceId}")]
    public static partial void LogRemoveResourceIdFromDocumentError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // FindByInternalAndThirdPartyTagName (EventIds 7140-7159)
    // ============================================================

    [LoggerMessage(
        EventId = 7140,
        Level = LogLevel.Information,
        Message = "Invalid service tag: null or empty Id, returning empty result")]
    public static partial void LogInvalidServiceTagNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 7141,
        Level = LogLevel.Information,
        Message = "Searching for configs by service tag | ServiceTagId: {ServiceTagId} | ServiceTagName: {ServiceTagName}")]
    public static partial void LogSearchingByServiceTag(
        this ILogger logger,
        string serviceTagId,
        string serviceTagName);

    [LoggerMessage(
        EventId = 7142,
        Level = LogLevel.Information,
        Message = "Found {Count} configs for service tag | ServiceTagName: {ServiceTagName}")]
    public static partial void LogFoundConfigsForServiceTag(
        this ILogger logger,
        int count,
        string serviceTagName);

    [LoggerMessage(
        EventId = 7143,
        Level = LogLevel.Error,
        Message = "Error searching for configs by service tag | ServiceTagName: {ServiceTagName}")]
    public static partial void LogFindByServiceTagError(
        this ILogger logger,
        Exception exception,
        string serviceTagName);

    // ============================================================
    // GetResourceDependencyInformation (EventIds 7160-7179)
    // ============================================================

    [LoggerMessage(
        EventId = 7160,
        Level = LogLevel.Information,
        Message = "ResourceId is null or empty, returning null")]
    public static partial void LogGetResourceDependencyInfoNullResourceId(this ILogger logger);

    [LoggerMessage(
        EventId = 7161,
        Level = LogLevel.Information,
        Message = "Querying for ResourceDependencyInformation | ResourceId: {ResourceId} | DocumentId: {DocumentId}")]
    public static partial void LogQueryingResourceDependencyInfo(
        this ILogger logger,
        string resourceId,
        string documentId);

    [LoggerMessage(
        EventId = 7162,
        Level = LogLevel.Information,
        Message = "ResourceDependencyInformation found | ResourceId: {ResourceId}")]
    public static partial void LogResourceDependencyInfoFound(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7163,
        Level = LogLevel.Warning,
        Message = "ResourceDependencyInformation not found | ResourceId: {ResourceId}")]
    public static partial void LogResourceDependencyInfoNotFound(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7164,
        Level = LogLevel.Error,
        Message = "Cosmos DB error retrieving ResourceDependencyInformation | ResourceId: {ResourceId}")]
    public static partial void LogGetResourceDependencyInfoCosmosError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7165,
        Level = LogLevel.Error,
        Message = "Unexpected error retrieving ResourceDependencyInformation | ResourceId: {ResourceId}")]
    public static partial void LogGetResourceDependencyInfoError(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // GetAll (EventIds 7180-7199)
    // ============================================================

    [LoggerMessage(
        EventId = 7180,
        Level = LogLevel.Information,
        Message = "Starting to query for all ResourceDependencyInformation documents from Cosmos DB")]
    public static partial void LogGetAllStarting(this ILogger logger);

    [LoggerMessage(
        EventId = 7181,
        Level = LogLevel.Information,
        Message = "Fetched {Count} items in the current batch")]
    public static partial void LogGetAllBatchFetched(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 7182,
        Level = LogLevel.Information,
        Message = "Total items retrieved: {TotalCount} | Duration: {DurationMs}ms")]
    public static partial void LogGetAllComplete(
        this ILogger logger,
        int totalCount,
        long durationMs);

    [LoggerMessage(
        EventId = 7183,
        Level = LogLevel.Error,
        Message = "Error retrieving all ResourceDependencyInformation documents")]
    public static partial void LogGetAllError(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // GetFirstOrDefault (EventIds 7200-7219)
    // ============================================================

    [LoggerMessage(
        EventId = 7200,
        Level = LogLevel.Information,
        Message = "First item retrieved successfully")]
    public static partial void LogFirstItemRetrieved(this ILogger logger);

    [LoggerMessage(
        EventId = 7201,
        Level = LogLevel.Warning,
        Message = "No items found in the first page of results, returning default")]
    public static partial void LogNoItemsInFirstPage(this ILogger logger);

    [LoggerMessage(
        EventId = 7202,
        Level = LogLevel.Warning,
        Message = "No results found in the query, returning default")]
    public static partial void LogNoResultsInQuery(this ILogger logger);

    [LoggerMessage(
        EventId = 7203,
        Level = LogLevel.Error,
        Message = "Error in GetFirstOrDefault")]
    public static partial void LogGetFirstOrDefaultError(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for persistence service operations.
    /// </summary>
    public static IDisposable? BeginPersistenceScope(
        this ILogger logger,
        string methodName,
        string? resourceId = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ServiceName"] = "ResourceDependencyInformationPersistenceService",
        ["MethodName"] = methodName,
        ["ResourceId"] = resourceId ?? "N/A",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for Cosmos DB operations.
    /// </summary>
    public static IDisposable? BeginCosmosDbScope(
        this ILogger logger,
        string operation,
        string? documentId = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "CosmosDB",
        ["Operation"] = operation,
        ["DocumentId"] = documentId ?? "N/A",
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }
  }
}