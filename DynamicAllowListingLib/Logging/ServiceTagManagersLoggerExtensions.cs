using Microsoft.Extensions.Logging;
using System;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for ServiceTagManagers operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// EVENT ID RANGE: 10000-10499
  /// </summary>
  public static partial class ServiceTagManagersLoggerExtensions
  {
    // ============================================================
    // Azure Subscriptions Persistence (EventIds 10100-10149)
    // ============================================================

    [LoggerMessage(
        EventId = 10100,
        Level = LogLevel.Information,
        Message = "Getting Items from cosmos Container: {ContainerName}")]
    public static partial void LogGettingItemsFromContainer(
        this ILogger logger,
        string containerName);

    [LoggerMessage(
        EventId = 10101,
        Level = LogLevel.Information,
        Message = "Retrieved existing items from database. Count: {Count}")]
    public static partial void LogRetrievedExistingItems(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10102,
        Level = LogLevel.Information,
        Message = "Identified {Count} items to delete.")]
    public static partial void LogItemsToDelete(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10103,
        Level = LogLevel.Information,
        Message = "Deleted item with Id: {ItemId}")]
    public static partial void LogItemDeleted(
        this ILogger logger,
        string itemId);

    [LoggerMessage(
        EventId = 10104,
        Level = LogLevel.Warning,
        Message = "Item with Id: {ItemId} not found during deletion. Skipping.")]
    public static partial void LogItemNotFoundDuringDeletion(
        this ILogger logger,
        string itemId);

    [LoggerMessage(
        EventId = 10105,
        Level = LogLevel.Information,
        Message = "Upserted item with Id: {ItemId}")]
    public static partial void LogItemUpserted(
        this ILogger logger,
        string itemId);

    [LoggerMessage(
        EventId = 10106,
        Level = LogLevel.Information,
        Message = "Querying Azure subscriptions from Cosmos container: {ContainerName}")]
    public static partial void LogQueryingFromContainer(
        this ILogger logger,
        string containerName);

    [LoggerMessage(
        EventId = 10107,
        Level = LogLevel.Information,
        Message = "Fetched {Count} subscriptions from current batch.")]
    public static partial void LogBatchFetched(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10108,
        Level = LogLevel.Information,
        Message = "Successfully retrieved {Count} Azure subscriptions from the database.")]
    public static partial void LogSubscriptionsRetrieved(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10109,
        Level = LogLevel.Warning,
        Message = "No Azure subscriptions found in Cosmos container: {ContainerName}")]
    public static partial void LogNoSubscriptionsFound(
        this ILogger logger,
        string containerName);

    [LoggerMessage(
        EventId = 10110,
        Level = LogLevel.Error,
        Message = "A Cosmos DB exception occurred while retrieving Azure subscriptions.")]
    public static partial void LogCosmosExceptionRetrieving(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 10111,
        Level = LogLevel.Error,
        Message = "An unexpected error occurred while retrieving Azure subscriptions.")]
    public static partial void LogUnexpectedErrorRetrieving(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 10112,
        Level = LogLevel.Warning,
        Message = "{MethodName} was called with an empty or null list.")]
    public static partial void LogEmptyOrNullList(
        this ILogger logger,
        string methodName);

    [LoggerMessage(
        EventId = 10113,
        Level = LogLevel.Information,
        Message = "Preparing to delete {Count} AzureSubscription items from the database.")]
    public static partial void LogPreparingToDelete(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10114,
        Level = LogLevel.Information,
        Message = "Successfully deleted item with Id: {ItemId}")]
    public static partial void LogSuccessfullyDeletedItem(
        this ILogger logger,
        string itemId);

    [LoggerMessage(
        EventId = 10115,
        Level = LogLevel.Warning,
        Message = "Item with Id: {ItemId} was not found in the database. Skipping.")]
    public static partial void LogItemNotFoundSkipping(
        this ILogger logger,
        string itemId);

    [LoggerMessage(
        EventId = 10116,
        Level = LogLevel.Error,
        Message = "Failed to delete item with Id: {ItemId}. Skipping to the next item.")]
    public static partial void LogFailedToDeleteItem(
        this ILogger logger,
        Exception exception,
        string itemId);

    [LoggerMessage(
        EventId = 10117,
        Level = LogLevel.Error,
        Message = "An unexpected error occurred while deleting items from the database.")]
    public static partial void LogUnexpectedErrorDeleting(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 10118,
        Level = LogLevel.Warning,
        Message = "{MethodName} was called with a null or empty subscription ID.")]
    public static partial void LogNullOrEmptySubscriptionId(
        this ILogger logger,
        string methodName);

    [LoggerMessage(
        EventId = 10119,
        Level = LogLevel.Information,
        Message = "Successfully retrieved AzureSubscription for ID: {SubscriptionId}")]
    public static partial void LogSubscriptionRetrievedById(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10120,
        Level = LogLevel.Warning,
        Message = "AzureSubscription with ID '{SubscriptionId}' was not found in the database.")]
    public static partial void LogSubscriptionNotFoundById(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10121,
        Level = LogLevel.Error,
        Message = "An error occurred while accessing the Cosmos DB.")]
    public static partial void LogCosmosAccessError(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 10122,
        Level = LogLevel.Error,
        Message = "An unexpected error occurred while retrieving the AzureSubscription.")]
    public static partial void LogUnexpectedErrorRetrievingSubscription(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 10123,
        Level = LogLevel.Error,
        Message = "A Cosmos DB exception occurred during the update process.")]
    public static partial void LogCosmosExceptionDuringUpdate(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 10124,
        Level = LogLevel.Error,
        Message = "An unexpected error occurred during the update process.")]
    public static partial void LogUnexpectedErrorDuringUpdate(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 10125,
        Level = LogLevel.Error,
        Message = "Failed to upsert item with Id: {ItemId}")]
    public static partial void LogFailedToUpsertItem(
        this ILogger logger,
        Exception exception,
        string itemId);

    // ============================================================
    // Service Tags Persistence (EventIds 10150-10199)
    // ============================================================

    [LoggerMessage(
        EventId = 10150,
        Level = LogLevel.Information,
        Message = "Fetching existing items from Cosmos Container: {ContainerName}")]
    public static partial void LogFetchingExistingItems(
        this ILogger logger,
        string containerName);

    [LoggerMessage(
        EventId = 10151,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} existing items from the database.")]
    public static partial void LogRetrievedExistingItemsCount(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10152,
        Level = LogLevel.Information,
        Message = "Database state successfully updated.")]
    public static partial void LogDatabaseStateUpdated(this ILogger logger);

    [LoggerMessage(
        EventId = 10153,
        Level = LogLevel.Information,
        Message = "Fetching ServiceTags from Cosmos container: {ContainerName}")]
    public static partial void LogFetchingServiceTags(
        this ILogger logger,
        string containerName);

    [LoggerMessage(
        EventId = 10154,
        Level = LogLevel.Information,
        Message = "Fetched a batch of {BatchCount} ServiceTags. Total retrieved so far: {TotalCount}")]
    public static partial void LogServiceTagBatchFetched(
        this ILogger logger,
        int batchCount,
        int totalCount);

    [LoggerMessage(
        EventId = 10155,
        Level = LogLevel.Warning,
        Message = "CosmosDB query failed while retrieving a batch. StatusCode: {StatusCode}, Message: {ErrorMessage}")]
    public static partial void LogCosmosQueryFailed(
        this ILogger logger,
        string statusCode,
        string errorMessage);

    [LoggerMessage(
        EventId = 10156,
        Level = LogLevel.Information,
        Message = "Successfully retrieved a total of {Count} ServiceTags from the database.")]
    public static partial void LogServiceTagsRetrieved(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10157,
        Level = LogLevel.Information,
        Message = "No items provided for deletion.")]
    public static partial void LogNoItemsForDeletion(this ILogger logger);

    [LoggerMessage(
        EventId = 10158,
        Level = LogLevel.Information,
        Message = "Preparing to delete {Count} items from the database.")]
    public static partial void LogPreparingToDeleteItems(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10159,
        Level = LogLevel.Information,
        Message = "Deleted ServiceTag with ID: {ServiceTagId}")]
    public static partial void LogServiceTagDeleted(
        this ILogger logger,
        string serviceTagId);

    [LoggerMessage(
        EventId = 10160,
        Level = LogLevel.Warning,
        Message = "CosmosDB deletion failed for ServiceTag with ID: {ServiceTagId}. StatusCode: {StatusCode}, Message: {ErrorMessage}")]
    public static partial void LogServiceTagDeletionFailed(
        this ILogger logger,
        string serviceTagId,
        string statusCode,
        string errorMessage);

    [LoggerMessage(
        EventId = 10161,
        Level = LogLevel.Information,
        Message = "Successfully deleted {Count} items from the database.")]
    public static partial void LogSuccessfullyDeletedItems(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10162,
        Level = LogLevel.Warning,
        Message = "Provided ID is null or empty.")]
    public static partial void LogProvidedIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 10163,
        Level = LogLevel.Information,
        Message = "Successfully retrieved ServiceTag with ID: {ServiceTagId}")]
    public static partial void LogServiceTagRetrievedById(
        this ILogger logger,
        string serviceTagId);

    [LoggerMessage(
        EventId = 10164,
        Level = LogLevel.Warning,
        Message = "ServiceTag with ID: {ServiceTagId} not found. CosmosDB Status Code: {StatusCode}")]
    public static partial void LogServiceTagNotFoundById(
        this ILogger logger,
        string serviceTagId,
        string statusCode);

    [LoggerMessage(
        EventId = 10165,
        Level = LogLevel.Error,
        Message = "Exception occurred during operation")]
    public static partial void LogOperationException(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // Internal/ThirdParty Settings Persistence (EventIds 10200-10249)
    // ============================================================

    [LoggerMessage(
        EventId = 10200,
        Level = LogLevel.Error,
        Message = "Provided settings object is null.")]
    public static partial void LogSettingsObjectNull(this ILogger logger);

    [LoggerMessage(
        EventId = 10201,
        Level = LogLevel.Warning,
        Message = "Both AzureSubscriptions and ServiceTags are null or empty. Nothing to update.")]
    public static partial void LogSettingsEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 10202,
        Level = LogLevel.Information,
        Message = "Starting database state update.")]
    public static partial void LogStartingDatabaseUpdate(this ILogger logger);

    [LoggerMessage(
        EventId = 10203,
        Level = LogLevel.Information,
        Message = "Updating Azure Subscriptions. Count: {Count}")]
    public static partial void LogUpdatingAzureSubscriptions(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10204,
        Level = LogLevel.Information,
        Message = "Updating Service Tags. Count: {Count}")]
    public static partial void LogUpdatingServiceTags(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10205,
        Level = LogLevel.Information,
        Message = "Database state update completed successfully.")]
    public static partial void LogDatabaseUpdateCompleted(this ILogger logger);

    [LoggerMessage(
        EventId = 10206,
        Level = LogLevel.Information,
        Message = "Retrieving Azure Subscriptions from database.")]
    public static partial void LogRetrievingAzureSubscriptions(this ILogger logger);

    [LoggerMessage(
        EventId = 10207,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} Azure Subscriptions from database.")]
    public static partial void LogRetrievedAzureSubscriptions(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10208,
        Level = LogLevel.Information,
        Message = "Retrieving Service Tags from database.")]
    public static partial void LogRetrievingServiceTags(this ILogger logger);

    [LoggerMessage(
        EventId = 10209,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} Service Tags from database.")]
    public static partial void LogRetrievedServiceTagsCount(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10210,
        Level = LogLevel.Information,
        Message = "Successfully retrieved database settings. Number of Azure Subscriptions: {AzureSubscriptionCount}, Number of Service Tags: {ServiceTagCount}")]
    public static partial void LogDatabaseSettingsRetrieved(
        this ILogger logger,
        int azureSubscriptionCount,
        int serviceTagCount);

    // ============================================================
    // InternalAndThirdPartyServiceTagsManager (EventIds 10250-10299)
    // ============================================================

    [LoggerMessage(
        EventId = 10250,
        Level = LogLevel.Information,
        Message = "Attempting to get settings from the database.")]
    public static partial void LogGettingSettingsFromDb(this ILogger logger);

    [LoggerMessage(
        EventId = 10251,
        Level = LogLevel.Information,
        Message = "Database settings are empty or incomplete. Loading settings from file.")]
    public static partial void LogLoadingSettingsFromFile(this ILogger logger);

    [LoggerMessage(
        EventId = 10252,
        Level = LogLevel.Information,
        Message = "Settings loaded successfully from file.")]
    public static partial void LogSettingsLoadedFromFile(this ILogger logger);

    [LoggerMessage(
        EventId = 10253,
        Level = LogLevel.Information,
        Message = "Settings successfully retrieved from database. ServiceTags count: {ServiceTagsCount}, AzureSubscriptions count: {AzureSubscriptionsCount}.")]
    public static partial void LogSettingsRetrievedFromDb(
        this ILogger logger,
        int serviceTagsCount,
        int azureSubscriptionsCount);

    [LoggerMessage(
        EventId = 10254,
        Level = LogLevel.Warning,
        Message = "Empty or null Subscription ID provided.")]
    public static partial void LogEmptySubscriptionId(this ILogger logger);

    [LoggerMessage(
        EventId = 10255,
        Level = LogLevel.Information,
        Message = "Fetching service tags for Subscription ID: {SubscriptionId}")]
    public static partial void LogFetchingServiceTagsBySubscription(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10256,
        Level = LogLevel.Warning,
        Message = "Subscription with ID '{SubscriptionId}' not found.")]
    public static partial void LogServiceTagSubscriptionNotFound(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10257,
        Level = LogLevel.Information,
        Message = "Found {Count} service tags for subscription '{SubscriptionName}'.")]
    public static partial void LogFoundServiceTagsForSubscription(
        this ILogger logger,
        int count,
        string subscriptionName);

    [LoggerMessage(
        EventId = 10258,
        Level = LogLevel.Information,
        Message = "Fetching service tags by subscription ID: {SubscriptionId} and mandatory: {IsMandatory}")]
    public static partial void LogFetchingServiceTagsWithMandatory(
        this ILogger logger,
        string subscriptionId,
        bool isMandatory);

    [LoggerMessage(
        EventId = 10259,
        Level = LogLevel.Information,
        Message = "Found {Count} service tags for subscription '{SubscriptionName}' with mandatory filter: {IsMandatory}.")]
    public static partial void LogFoundServiceTagsWithMandatory(
        this ILogger logger,
        int count,
        string subscriptionName,
        bool isMandatory);

    [LoggerMessage(
        EventId = 10260,
        Level = LogLevel.Warning,
        Message = "Invalid or empty subscription ID provided. Returning empty rules set.")]
    public static partial void LogInvalidSubscriptionIdForRules(this ILogger logger);

    [LoggerMessage(
        EventId = 10261,
        Level = LogLevel.Warning,
        Message = "Subscription with ID '{SubscriptionId}' not found. Returning empty rules set.")]
    public static partial void LogSubscriptionNotFoundForRules(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10262,
        Level = LogLevel.Information,
        Message = "Generating IP and Subnet rules for Service Tag: {ServiceTagName} for Subscription: {SubscriptionName}")]
    public static partial void LogGeneratingRulesForServiceTag(
        this ILogger logger,
        string serviceTagName,
        string subscriptionName);

    [LoggerMessage(
        EventId = 10263,
        Level = LogLevel.Information,
        Message = "Generated {RuleCount} rules for SubscriptionId: {SubscriptionId}")]
    public static partial void LogGeneratedRules(
        this ILogger logger,
        int ruleCount,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10264,
        Level = LogLevel.Information,
        Message = "No Address Prefixes found for ServiceTag: {ServiceTagName}")]
    public static partial void LogNoAddressPrefixes(
        this ILogger logger,
        string serviceTagName);

    [LoggerMessage(
        EventId = 10265,
        Level = LogLevel.Information,
        Message = "No Subnet IDs found for ServiceTag: {ServiceTagName}")]
    public static partial void LogNoSubnetIds(
        this ILogger logger,
        string serviceTagName);

    [LoggerMessage(
        EventId = 10266,
        Level = LogLevel.Information,
        Message = "Checking if service tag : {ServiceTagName} exists.")]
    public static partial void LogCheckingServiceTagExists(
        this ILogger logger,
        string serviceTagName);

    [LoggerMessage(
        EventId = 10267,
        Level = LogLevel.Information,
        Message = "Service tag existence check result: {Exists}")]
    public static partial void LogServiceTagExistsResult(
        this ILogger logger,
        bool exists);

    // ============================================================
    // Azure Service Tags Manager (EventIds 10300-10349)
    // ============================================================

    [LoggerMessage(
        EventId = 10300,
        Level = LogLevel.Information,
        Message = "Checking for existence of Service Tag {ServiceTagName} exist for Subscription ID {SubscriptionId}.")]
    public static partial void LogCheckingAzureServiceTagExists(
        this ILogger logger,
        string serviceTagName,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10301,
        Level = LogLevel.Information,
        Message = "Service Tag {ServiceTagName} exist for Subscription ID {SubscriptionId}.")]
    public static partial void LogAzureServiceTagExists(
        this ILogger logger,
        string serviceTagName,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10302,
        Level = LogLevel.Information,
        Message = "Service Tag {ServiceTagName} does not exist for Subscription ID {SubscriptionId}.")]
    public static partial void LogAzureServiceTagNotExists(
        this ILogger logger,
        string serviceTagName,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10303,
        Level = LogLevel.Information,
        Message = "Failed to retrieve Azure Service Tags JSON for Subscription ID: {SubscriptionId}. Response was null or empty.")]
    public static partial void LogAzureServiceTagsJsonEmpty(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10304,
        Level = LogLevel.Warning,
        Message = "Deserialization of Azure Service Tags failed for Subscription ID: {SubscriptionId}.")]
    public static partial void LogAzureServiceTagsDeserializationFailed(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10305,
        Level = LogLevel.Information,
        Message = "Successfully retrieved and deserialized Azure Service Tags for Subscription ID: {SubscriptionId}.")]
    public static partial void LogAzureServiceTagsRetrieved(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10306,
        Level = LogLevel.Information,
        Message = "Using cached Azure Service Tags for Subscription ID: {SubscriptionId}.")]
    public static partial void LogUsingCachedAzureServiceTags(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10307,
        Level = LogLevel.Warning,
        Message = "Invalid input: Service tags are null or empty for Subscription ID: {SubscriptionId}")]
    public static partial void LogServiceTagsNullOrEmpty(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10308,
        Level = LogLevel.Warning,
        Message = "Invalid input: Subscription ID is null or empty.")]
    public static partial void LogSubscriptionIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 10309,
        Level = LogLevel.Information,
        Message = "Fetching Azure Service Tags for Subscription ID: {SubscriptionId}")]
    public static partial void LogFetchingAzureServiceTags(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10310,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} Azure Service Tags for Subscription ID: {SubscriptionId}")]
    public static partial void LogRetrievedAzureServiceTags(
        this ILogger logger,
        int? count,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10311,
        Level = LogLevel.Information,
        Message = "Processing Service Tag: {ServiceTag}")]
    public static partial void LogProcessingServiceTag(
        this ILogger logger,
        string serviceTag);

    [LoggerMessage(
        EventId = 10312,
        Level = LogLevel.Information,
        Message = "Found {Count} rules for Service Tag: {ServiceTag}.")]
    public static partial void LogFoundRulesForServiceTag(
        this ILogger logger,
        int count,
        string serviceTag);

    [LoggerMessage(
        EventId = 10313,
        Level = LogLevel.Information,
        Message = "Service Tag: '{ServiceTag}' not found in Azure Service Tags.")]
    public static partial void LogServiceTagNotFoundInAzure(
        this ILogger logger,
        string serviceTag);

    [LoggerMessage(
        EventId = 10314,
        Level = LogLevel.Information,
        Message = "The following Azure Service Tags were not found: {ServiceTags}, Subscription ID:{SubscriptionId} .")]
    public static partial void LogServiceTagsNotFound(
        this ILogger logger,
        string serviceTags,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10315,
        Level = LogLevel.Information,
        Message = "Successfully retrieved {Count} IP Security Restriction Rules for Subscription ID: {SubscriptionId}")]
    public static partial void LogRetrievedIpSecurityRules(
        this ILogger logger,
        int count,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10316,
        Level = LogLevel.Error,
        Message = "An unexpected error occurred while processing service tags for Subscription ID: {SubscriptionId}.")]
    public static partial void LogUnexpectedErrorProcessingServiceTags(
        this ILogger logger,
        Exception exception,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10317,
        Level = LogLevel.Warning,
        Message = "Invalid input: Azure Service Tag is null.")]
    public static partial void LogAzureServiceTagNull(this ILogger logger);

    [LoggerMessage(
        EventId = 10318,
        Level = LogLevel.Information,
        Message = "Generating security restriction rules for Azure Service Tag: {ServiceTagName}.")]
    public static partial void LogGeneratingRulesForAzureServiceTag(
        this ILogger logger,
        string serviceTagName);

    [LoggerMessage(
        EventId = 10319,
        Level = LogLevel.Warning,
        Message = "No IP Address Prefixes found for Azure Service Tag: {ServiceTagName}.")]
    public static partial void LogNoIpPrefixesForAzureServiceTag(
        this ILogger logger,
        string serviceTagName);

    [LoggerMessage(
        EventId = 10320,
        Level = LogLevel.Information,
        Message = "Created rule: {RuleName}, IP Address: {IpAddress}, Action: {Action}, Priority: {Priority}")]
    public static partial void LogRuleCreated(
        this ILogger logger,
        string ruleName,
        string ipAddress,
        string action,
        int priority);

    [LoggerMessage(
        EventId = 10321,
        Level = LogLevel.Warning,
        Message = "Duplicate rule detected and ignored: {RuleName}, IP Address: {IpAddress}.")]
    public static partial void LogDuplicateRuleIgnored(
        this ILogger logger,
        string ruleName,
        string ipAddress);

    [LoggerMessage(
        EventId = 10322,
        Level = LogLevel.Information,
        Message = "Successfully generated {Count} security restriction rules for Azure Service Tag: {ServiceTagName}.")]
    public static partial void LogGeneratedRulesForAzureServiceTag(
        this ILogger logger,
        int count,
        string serviceTagName);

    // ============================================================
    // AzureWebServiceTagManager (EventIds 10350-10399)
    // ============================================================

    [LoggerMessage(
        EventId = 10350,
        Level = LogLevel.Information,
        Message = "No service tags provided for SubscriptionID: {SubscriptionId}. Returning an empty rule set.")]
    public static partial void LogNoServiceTagsProvided(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10351,
        Level = LogLevel.Information,
        Message = "Generating security restriction rules for Subscription ID: {SubscriptionId} with Service Tags: {ServiceTags}")]
    public static partial void LogGeneratingSecurityRules(
        this ILogger logger,
        string subscriptionId,
        string serviceTags);

    [LoggerMessage(
        EventId = 10352,
        Level = LogLevel.Warning,
        Message = "Encountered a null or empty service tag for Subscription ID: {SubscriptionId}. Skipping.")]
    public static partial void LogNullOrEmptyServiceTag(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10353,
        Level = LogLevel.Warning,
        Message = "Duplicate rule detected for Service Tag: {ServiceTag}. Rule was not added again.")]
    public static partial void LogDuplicateRule(
        this ILogger logger,
        string serviceTag);

    [LoggerMessage(
        EventId = 10354,
        Level = LogLevel.Information,
        Message = "Created rule: {RuleName}, IP Address: {IpAddress}, Action: {Action}, Priority: {Priority}, Tag: {Tag}")]
    public static partial void LogRuleCreatedWithTag(
        this ILogger logger,
        string ruleName,
        string ipAddress,
        string action,
        int priority,
        string tag);

    [LoggerMessage(
        EventId = 10355,
        Level = LogLevel.Information,
        Message = "Successfully generated {Count} security restriction rules for Subscription ID: {SubscriptionId}.")]
    public static partial void LogSecurityRulesGenerated(
        this ILogger logger,
        int count,
        string subscriptionId);

    [LoggerMessage(
        EventId = 10356,
        Level = LogLevel.Error,
        Message = "Exception occurred during rule generation")]
    public static partial void LogRuleGenerationException(
        this ILogger logger,
        Exception exception);

    // ============================================================
    // Azure Service Tags JSON Helper (EventIds 10400-10449)
    // ============================================================

    [LoggerMessage(
        EventId = 10400,
        Level = LogLevel.Information,
        Message = "Azure Service Tags Not Available in Memory Cache")]
    public static partial void LogCacheMiss(this ILogger logger);

    [LoggerMessage(
        EventId = 10401,
        Level = LogLevel.Information,
        Message = "Getting Azure Service Tags using REST endpoint")]
    public static partial void LogGettingServiceTagsFromRest(this ILogger logger);

    [LoggerMessage(
        EventId = 10402,
        Level = LogLevel.Information,
        Message = "Got Azure Service Tags from Memory Cache")]
    public static partial void LogRetrievedFromCache(this ILogger logger);

    [LoggerMessage(
        EventId = 10403,
        Level = LogLevel.Information,
        Message = "AzureServiceTags data expiry Time:{ExpiryTime}")]
    public static partial void LogCacheExpiryTime(
        this ILogger logger,
        string expiryTime);
  }
}