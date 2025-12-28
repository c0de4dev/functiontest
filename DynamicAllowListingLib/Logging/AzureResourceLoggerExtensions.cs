using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for IAzureResource implementations.
  /// Covers WebSite, Storage, CosmosDb, KeyVault, SqlServer operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 3200-3499
  /// 
  /// Event ID Allocation:
  /// - 3200-3249: Common IAzureResource Operations
  /// - 3250-3299: WebSite Specific Operations
  /// - 3300-3349: Storage Specific Operations
  /// - 3350-3399: CosmosDb Specific Operations
  /// - 3400-3449: KeyVault Specific Operations
  /// - 3450-3499: SqlServer Specific Operations
  /// </summary>
  public static partial class AzureResourceLoggerExtensions
  {
    // ============================================================
    // Common IAzureResource Operations (EventIds 3200-3249)
    // ============================================================

    #region Network Restriction Operations

    [LoggerMessage(
        EventId = 3200,
        Level = LogLevel.Information,
        Message = "Applying network restrictions | ResourceType: {ResourceType} | ResourceId: {ResourceId} | Operation: {Operation}")]
    public static partial void LogApplyingNetworkRestrictions(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string operation);

    [LoggerMessage(
        EventId = 3201,
        Level = LogLevel.Information,
        Message = "Network restrictions applied successfully | ResourceType: {ResourceType} | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | SubnetRuleCount: {SubnetRuleCount}")]
    public static partial void LogNetworkRestrictionsApplied(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int ipRuleCount,
        int subnetRuleCount);

    [LoggerMessage(
        EventId = 3202,
        Level = LogLevel.Error,
        Message = "Failed to apply network restrictions | ResourceType: {ResourceType} | ResourceId: {ResourceId} | Operation: {Operation}")]
    public static partial void LogNetworkRestrictionsFailed(
        this ILogger logger,
        Exception exception,
        string resourceType,
        string resourceId,
        string operation);

    [LoggerMessage(
        EventId = 3203,
        Level = LogLevel.Information,
        Message = "Fetching existing configuration | ResourceType: {ResourceType} | ResourceId: {ResourceId} | ApiUrl: {ApiUrl}")]
    public static partial void LogFetchingExistingConfig(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string apiUrl);

    [LoggerMessage(
        EventId = 3204,
        Level = LogLevel.Information,
        Message = "Existing configuration retrieved | ResourceType: {ResourceType} | ResourceId: {ResourceId} | ExistingIpRules: {ExistingIpRules} | ExistingSubnetRules: {ExistingSubnetRules}")]
    public static partial void LogExistingConfigRetrieved(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int existingIpRules,
        int existingSubnetRules);

    [LoggerMessage(
        EventId = 3205,
        Level = LogLevel.Warning,
        Message = "Resource ID is null | ResourceType: {ResourceType} | Context: {Context}")]
    public static partial void LogResourceIdNull(
        this ILogger logger,
        string resourceType,
        string context);

    [LoggerMessage(
        EventId = 3206,
        Level = LogLevel.Information,
        Message = "Checking provisioning state | ResourceType: {ResourceType} | ResourceId: {ResourceId}")]
    public static partial void LogCheckingProvisioningState(
        this ILogger logger,
        string resourceType,
        string resourceId);

    [LoggerMessage(
        EventId = 3207,
        Level = LogLevel.Information,
        Message = "Provisioning state verified | ResourceType: {ResourceType} | ResourceId: {ResourceId} | State: {State}")]
    public static partial void LogProvisioningStateVerified(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string state);

    [LoggerMessage(
        EventId = 3208,
        Level = LogLevel.Error,
        Message = "Provisioning state not succeeded | ResourceType: {ResourceType} | ResourceId: {ResourceId} | State: {State}")]
    public static partial void LogProvisioningStateNotSucceeded(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string state);

    #endregion

    #region Rule Processing

    [LoggerMessage(
        EventId = 3210,
        Level = LogLevel.Information,
        Message = "Consolidating IP addresses | ResourceType: {ResourceType} | ResourceId: {ResourceId} | InputRules: {InputRules}")]
    public static partial void LogConsolidatingIpAddresses(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int inputRules);

    [LoggerMessage(
        EventId = 3211,
        Level = LogLevel.Information,
        Message = "IP addresses consolidated | ResourceType: {ResourceType} | ResourceId: {ResourceId} | OutputRules: {OutputRules} | Reduction: {Reduction}")]
    public static partial void LogIpAddressesConsolidated(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int outputRules,
        int reduction);

    [LoggerMessage(
        EventId = 3212,
        Level = LogLevel.Information,
        Message = "Splitting rules | ResourceType: {ResourceType} | ResourceId: {ResourceId} | InputRules: {InputRules} | MaxPerRule: {MaxPerRule}")]
    public static partial void LogSplittingRules(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int inputRules,
        int maxPerRule);

    [LoggerMessage(
        EventId = 3213,
        Level = LogLevel.Information,
        Message = "Rules split completed | ResourceType: {ResourceType} | ResourceId: {ResourceId} | OutputRules: {OutputRules}")]
    public static partial void LogRulesSplitCompleted(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int outputRules);

    [LoggerMessage(
        EventId = 3214,
        Level = LogLevel.Information,
        Message = "Merging rules with existing | ResourceType: {ResourceType} | ResourceId: {ResourceId} | ExistingCount: {ExistingCount} | NewCount: {NewCount}")]
    public static partial void LogMergingRules(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int existingCount,
        int newCount);

    [LoggerMessage(
        EventId = 3215,
        Level = LogLevel.Information,
        Message = "Rules merged | ResourceType: {ResourceType} | ResourceId: {ResourceId} | MergedCount: {MergedCount}")]
    public static partial void LogRulesMerged(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int mergedCount);

    [LoggerMessage(
        EventId = 3216,
        Level = LogLevel.Warning,
        Message = "Rule limit reached | ResourceType: {ResourceType} | ResourceId: {ResourceId} | CurrentCount: {CurrentCount} | Limit: {Limit}")]
    public static partial void LogRuleLimitReached(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int currentCount,
        int limit);

    #endregion

    #region Azure Management API Operations

    [LoggerMessage(
        EventId = 3220,
        Level = LogLevel.Information,
        Message = "Sending PATCH request to Azure | ResourceType: {ResourceType} | ResourceId: {ResourceId} | ApiUrl: {ApiUrl}")]
    public static partial void LogSendingPatchRequest(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string apiUrl);

    [LoggerMessage(
        EventId = 3221,
        Level = LogLevel.Information,
        Message = "PATCH request successful | ResourceType: {ResourceType} | ResourceId: {ResourceId}")]
    public static partial void LogPatchRequestSuccessful(
        this ILogger logger,
        string resourceType,
        string resourceId);

    [LoggerMessage(
        EventId = 3222,
        Level = LogLevel.Error,
        Message = "PATCH request failed | ResourceType: {ResourceType} | ResourceId: {ResourceId}")]
    public static partial void LogPatchRequestFailed(
        this ILogger logger,
        Exception exception,
        string resourceType,
        string resourceId);

    [LoggerMessage(
        EventId = 3223,
        Level = LogLevel.Information,
        Message = "Sending PUT request to Azure | ResourceType: {ResourceType} | ResourceId: {ResourceId} | ApiUrl: {ApiUrl}")]
    public static partial void LogSendingPutRequest(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string apiUrl);

    [LoggerMessage(
        EventId = 3224,
        Level = LogLevel.Information,
        Message = "PUT request successful | ResourceType: {ResourceType} | ResourceId: {ResourceId}")]
    public static partial void LogPutRequestSuccessful(
        this ILogger logger,
        string resourceType,
        string resourceId);

    [LoggerMessage(
        EventId = 3225,
        Level = LogLevel.Error,
        Message = "PUT request failed | ResourceType: {ResourceType} | ResourceId: {ResourceId}")]
    public static partial void LogPutRequestFailed(
        this ILogger logger,
        Exception exception,
        string resourceType,
        string resourceId);

    [LoggerMessage(
        EventId = 3226,
        Level = LogLevel.Debug,
        Message = "API request payload | ResourceType: {ResourceType} | ResourceId: {ResourceId} | PayloadSize: {PayloadSize} bytes")]
    public static partial void LogApiRequestPayload(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int payloadSize);

    #endregion

    #region PrintOut Mode

    [LoggerMessage(
        EventId = 3230,
        Level = LogLevel.Information,
        Message = "PrintOut mode active - skipping Azure update | ResourceType: {ResourceType} | ResourceId: {ResourceId}")]
    public static partial void LogPrintOutModeActive(
        this ILogger logger,
        string resourceType,
        string resourceId);

    [LoggerMessage(
        EventId = 3231,
        Level = LogLevel.Information,
        Message = "PrintOut result | ResourceType: {ResourceType} | ResourceId: {ResourceId} | IpCount: {IpCount} | SubnetCount: {SubnetCount}")]
    public static partial void LogPrintOutResult(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int ipCount,
        int subnetCount);

    #endregion

    #region Rule Generation

    [LoggerMessage(
        EventId = 3240,
        Level = LogLevel.Information,
        Message = "Generating IP restriction rules | ResourceType: {ResourceType} | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogGeneratingResourceRules(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3241,
        Level = LogLevel.Information,
        Message = "IP restriction rules generated | ResourceType: {ResourceType} | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogResourceRulesGenerated(
        this ILogger logger,
        string resourceType,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 3242,
        Level = LogLevel.Warning,
        Message = "Cannot generate rules for resource type | ResourceType: {ResourceType} | ResourceId: {ResourceId} | Reason: {Reason}")]
    public static partial void LogCannotGenerateRules(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string reason);

    #endregion

    // ============================================================
    // WebSite Specific Operations (EventIds 3250-3299)
    // ============================================================

    #region WebSite Operations

    [LoggerMessage(
        EventId = 3250,
        Level = LogLevel.Information,
        Message = "WebSite overwrite started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogWebSiteOverwriteStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3251,
        Level = LogLevel.Information,
        Message = "WebSite overwrite completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | ScmRulesApplied: {ScmRulesApplied}")]
    public static partial void LogWebSiteOverwriteComplete(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int scmRulesApplied);

    [LoggerMessage(
        EventId = 3252,
        Level = LogLevel.Information,
        Message = "WebSite append started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogWebSiteAppendStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3253,
        Level = LogLevel.Information,
        Message = "WebSite append completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalScmRules: {TotalScmRules}")]
    public static partial void LogWebSiteAppendComplete(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalScmRules);

    [LoggerMessage(
        EventId = 3254,
        Level = LogLevel.Information,
        Message = "Fetching existing WebSite IP restrictions | ResourceId: {ResourceId}")]
    public static partial void LogFetchingWebSiteRestrictions(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3255,
        Level = LogLevel.Information,
        Message = "Existing WebSite restrictions retrieved | ResourceId: {ResourceId} | IpSecurityRestrictions: {IpCount} | ScmIpSecurityRestrictions: {ScmCount}")]
    public static partial void LogWebSiteRestrictionsRetrieved(
        this ILogger logger,
        string resourceId,
        int ipCount,
        int scmCount);

    [LoggerMessage(
        EventId = 3256,
        Level = LogLevel.Warning,
        Message = "WebSite IP rule limit reached | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | Limit: {Limit}")]
    public static partial void LogWebSiteIpRuleLimitReached(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int limit);

    [LoggerMessage(
        EventId = 3257,
        Level = LogLevel.Warning,
        Message = "WebSite SCM rule limit reached | ResourceId: {ResourceId} | ScmRuleCount: {ScmRuleCount} | Limit: {Limit}")]
    public static partial void LogWebSiteScmRuleLimitReached(
        this ILogger logger,
        string resourceId,
        int scmRuleCount,
        int limit);

    [LoggerMessage(
        EventId = 3258,
        Level = LogLevel.Information,
        Message = "Applying WebSite config | ResourceId: {ResourceId} | ApiVersion: {ApiVersion}")]
    public static partial void LogApplyingWebSiteConfig(
        this ILogger logger,
        string resourceId,
        string apiVersion);

    [LoggerMessage(
        EventId = 3259,
        Level = LogLevel.Information,
        Message = "WebSite config applied successfully | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteConfigApplied(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3260,
        Level = LogLevel.Error,
        Message = "Failed to apply WebSite config | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteConfigFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 3261,
        Level = LogLevel.Warning,
        Message = "No existing IP security rules found | ResourceId: {ResourceId}")]
    public static partial void LogNoExistingIpSecurityRules(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3262,
        Level = LogLevel.Information,
        Message = "Consolidating WebSite IP rules | ResourceId: {ResourceId} | InputCount: {InputCount}")]
    public static partial void LogConsolidatingWebSiteRules(
        this ILogger logger,
        string resourceId,
        int inputCount);

    [LoggerMessage(
        EventId = 3263,
        Level = LogLevel.Information,
        Message = "WebSite rules consolidated | ResourceId: {ResourceId} | OutputCount: {OutputCount}")]
    public static partial void LogWebSiteRulesConsolidated(
        this ILogger logger,
        string resourceId,
        int outputCount);

    #endregion

    // ============================================================
    // Storage Specific Operations (EventIds 3300-3349)
    // ============================================================

    #region Storage Operations

    [LoggerMessage(
        EventId = 3300,
        Level = LogLevel.Information,
        Message = "Storage overwrite started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogStorageOverwriteStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3301,
        Level = LogLevel.Information,
        Message = "Storage overwrite completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogStorageOverwriteComplete(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 3302,
        Level = LogLevel.Information,
        Message = "Storage append started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogStorageAppendStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3303,
        Level = LogLevel.Information,
        Message = "Storage append completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogStorageAppendComplete(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 3304,
        Level = LogLevel.Information,
        Message = "Fetching existing Storage network ACLs | ResourceId: {ResourceId}")]
    public static partial void LogFetchingStorageNetworkAcls(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3305,
        Level = LogLevel.Information,
        Message = "Storage network ACLs retrieved | ResourceId: {ResourceId} | DefaultAction: {DefaultAction} | IpRules: {IpRules} | VNetRules: {VNetRules}")]
    public static partial void LogStorageNetworkAclsRetrieved(
        this ILogger logger,
        string resourceId,
        string defaultAction,
        int ipRules,
        int vNetRules);

    [LoggerMessage(
        EventId = 3306,
        Level = LogLevel.Warning,
        Message = "Storage IP rule limit reached | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | Limit: {Limit}")]
    public static partial void LogStorageIpRuleLimitReached(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int limit);

    [LoggerMessage(
        EventId = 3307,
        Level = LogLevel.Information,
        Message = "Storage default action set to Deny | ResourceId: {ResourceId}")]
    public static partial void LogStorageDefaultActionDeny(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3308,
        Level = LogLevel.Warning,
        Message = "Storage allows all traffic - skipping append | ResourceId: {ResourceId} | DefaultAction: {DefaultAction}")]
    public static partial void LogStorageAllowsAllTraffic(
        this ILogger logger,
        string resourceId,
        string defaultAction);

    [LoggerMessage(
        EventId = 3309,
        Level = LogLevel.Information,
        Message = "Patching Storage config | ResourceId: {ResourceId}")]
    public static partial void LogPatchingStorageConfig(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3310,
        Level = LogLevel.Information,
        Message = "Storage config patched successfully | ResourceId: {ResourceId}")]
    public static partial void LogStorageConfigPatched(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3311,
        Level = LogLevel.Error,
        Message = "Failed to patch Storage config | ResourceId: {ResourceId}")]
    public static partial void LogStorageConfigPatchFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    #endregion

    // ============================================================
    // CosmosDb Specific Operations (EventIds 3350-3399)
    // ============================================================

    #region CosmosDb Operations

    [LoggerMessage(
        EventId = 3350,
        Level = LogLevel.Information,
        Message = "CosmosDb overwrite started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogCosmosDbOverwriteStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3351,
        Level = LogLevel.Information,
        Message = "CosmosDb overwrite completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogCosmosDbOverwriteComplete(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 3352,
        Level = LogLevel.Information,
        Message = "CosmosDb append started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogCosmosDbAppendStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3353,
        Level = LogLevel.Information,
        Message = "CosmosDb append completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogCosmosDbAppendComplete(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 3354,
        Level = LogLevel.Information,
        Message = "Fetching existing CosmosDb properties | ResourceId: {ResourceId}")]
    public static partial void LogFetchingCosmosDbProperties(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3355,
        Level = LogLevel.Information,
        Message = "CosmosDb properties retrieved | ResourceId: {ResourceId} | VNetFilterEnabled: {VNetFilterEnabled} | IpRules: {IpRules} | VNetRules: {VNetRules}")]
    public static partial void LogCosmosDbPropertiesRetrieved(
        this ILogger logger,
        string resourceId,
        bool vNetFilterEnabled,
        int ipRules,
        int vNetRules);

    [LoggerMessage(
        EventId = 3356,
        Level = LogLevel.Information,
        Message = "Adding default CosmosDb IP ranges | ResourceId: {ResourceId} | DefaultRangeCount: {DefaultRangeCount}")]
    public static partial void LogAddingDefaultCosmosDbRanges(
        this ILogger logger,
        string resourceId,
        int defaultRangeCount);

    [LoggerMessage(
        EventId = 3357,
        Level = LogLevel.Information,
        Message = "Enabling VNet filter for CosmosDb | ResourceId: {ResourceId}")]
    public static partial void LogEnablingCosmosDbVNetFilter(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3358,
        Level = LogLevel.Warning,
        Message = "CosmosDb has no existing restrictions - skipping append | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbNoExistingRestrictions(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3359,
        Level = LogLevel.Information,
        Message = "Validating CosmosDb subnet rules | ResourceId: {ResourceId} | SubnetCount: {SubnetCount}")]
    public static partial void LogValidatingCosmosDbSubnets(
        this ILogger logger,
        string resourceId,
        int subnetCount);

    [LoggerMessage(
        EventId = 3360,
        Level = LogLevel.Information,
        Message = "CosmosDb subnet validation completed | ResourceId: {ResourceId} | ValidSubnets: {ValidSubnets} | InvalidSubnets: {InvalidSubnets}")]
    public static partial void LogCosmosDbSubnetValidationComplete(
        this ILogger logger,
        string resourceId,
        int validSubnets,
        int invalidSubnets);

    [LoggerMessage(
        EventId = 3361,
        Level = LogLevel.Information,
        Message = "Patching CosmosDb config | ResourceId: {ResourceId}")]
    public static partial void LogPatchingCosmosDbConfig(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3362,
        Level = LogLevel.Information,
        Message = "CosmosDb config patched successfully | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbConfigPatched(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3363,
        Level = LogLevel.Error,
        Message = "Failed to patch CosmosDb config | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbConfigPatchFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    #endregion

    // ============================================================
    // KeyVault Specific Operations (EventIds 3400-3449)
    // ============================================================

    #region KeyVault Operations

    [LoggerMessage(
        EventId = 3400,
        Level = LogLevel.Information,
        Message = "KeyVault overwrite started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogKeyVaultOverwriteStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3401,
        Level = LogLevel.Information,
        Message = "KeyVault overwrite completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogKeyVaultOverwriteComplete(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 3402,
        Level = LogLevel.Information,
        Message = "KeyVault append started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogKeyVaultAppendStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3403,
        Level = LogLevel.Information,
        Message = "KeyVault append completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogKeyVaultAppendComplete(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 3404,
        Level = LogLevel.Information,
        Message = "Fetching existing KeyVault properties | ResourceId: {ResourceId}")]
    public static partial void LogFetchingKeyVaultProperties(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3405,
        Level = LogLevel.Information,
        Message = "KeyVault properties retrieved | ResourceId: {ResourceId} | DefaultAction: {DefaultAction} | IpRules: {IpRules} | VNetRules: {VNetRules}")]
    public static partial void LogKeyVaultPropertiesRetrieved(
        this ILogger logger,
        string resourceId,
        string defaultAction,
        int ipRules,
        int vNetRules);

    [LoggerMessage(
        EventId = 3406,
        Level = LogLevel.Information,
        Message = "Patching KeyVault config | ResourceId: {ResourceId}")]
    public static partial void LogPatchingKeyVaultConfig(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3407,
        Level = LogLevel.Information,
        Message = "KeyVault config patched successfully | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultConfigPatched(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3408,
        Level = LogLevel.Error,
        Message = "Failed to patch KeyVault config | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultConfigPatchFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    #endregion

    // ============================================================
    // SqlServer Specific Operations (EventIds 3450-3499)
    // ============================================================

    #region SqlServer Operations

    [LoggerMessage(
        EventId = 3450,
        Level = LogLevel.Information,
        Message = "SqlServer overwrite started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogSqlServerOverwriteStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3451,
        Level = LogLevel.Information,
        Message = "SqlServer overwrite completed | ResourceId: {ResourceId} | FirewallRulesApplied: {FirewallRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogSqlServerOverwriteComplete(
        this ILogger logger,
        string resourceId,
        int firewallRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 3452,
        Level = LogLevel.Information,
        Message = "SqlServer append started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogSqlServerAppendStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3453,
        Level = LogLevel.Information,
        Message = "SqlServer append completed | ResourceId: {ResourceId} | TotalFirewallRules: {TotalFirewallRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogSqlServerAppendComplete(
        this ILogger logger,
        string resourceId,
        int totalFirewallRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 3454,
        Level = LogLevel.Information,
        Message = "Fetching existing SqlServer firewall rules | ResourceId: {ResourceId}")]
    public static partial void LogFetchingSqlServerFirewallRules(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3455,
        Level = LogLevel.Information,
        Message = "SqlServer firewall rules retrieved | ResourceId: {ResourceId} | FirewallRules: {FirewallRules} | VNetRules: {VNetRules}")]
    public static partial void LogSqlServerFirewallRulesRetrieved(
        this ILogger logger,
        string resourceId,
        int firewallRules,
        int vNetRules);

    [LoggerMessage(
        EventId = 3456,
        Level = LogLevel.Warning,
        Message = "SqlServer firewall rule limit reached | ResourceId: {ResourceId} | RuleCount: {RuleCount} | Limit: {Limit}")]
    public static partial void LogSqlServerFirewallRuleLimitReached(
        this ILogger logger,
        string resourceId,
        int ruleCount,
        int limit);

    [LoggerMessage(
        EventId = 3457,
        Level = LogLevel.Information,
        Message = "Disabling SqlServer public access | ResourceId: {ResourceId}")]
    public static partial void LogDisablingSqlServerPublicAccess(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3458,
        Level = LogLevel.Information,
        Message = "Creating SqlServer firewall rule | ResourceId: {ResourceId} | RuleName: {RuleName} | StartIp: {StartIp} | EndIp: {EndIp}")]
    public static partial void LogCreatingSqlServerFirewallRule(
        this ILogger logger,
        string resourceId,
        string ruleName,
        string startIp,
        string endIp);

    [LoggerMessage(
        EventId = 3459,
        Level = LogLevel.Information,
        Message = "SqlServer firewall rule created | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerFirewallRuleCreated(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 3460,
        Level = LogLevel.Information,
        Message = "Creating SqlServer VNet rule | ResourceId: {ResourceId} | RuleName: {RuleName} | SubnetId: {SubnetId}")]
    public static partial void LogCreatingSqlServerVNetRule(
        this ILogger logger,
        string resourceId,
        string ruleName,
        string subnetId);

    [LoggerMessage(
        EventId = 3461,
        Level = LogLevel.Information,
        Message = "SqlServer VNet rule created | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerVNetRuleCreated(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 3462,
        Level = LogLevel.Information,
        Message = "Deleting leftover SqlServer firewall rules | ResourceId: {ResourceId} | RulesToDelete: {RulesToDelete}")]
    public static partial void LogDeletingLeftoverFirewallRules(
        this ILogger logger,
        string resourceId,
        int rulesToDelete);

    [LoggerMessage(
        EventId = 3463,
        Level = LogLevel.Information,
        Message = "Deleting leftover SqlServer VNet rules | ResourceId: {ResourceId} | RulesToDelete: {RulesToDelete}")]
    public static partial void LogDeletingLeftoverVNetRules(
        this ILogger logger,
        string resourceId,
        int rulesToDelete);

    [LoggerMessage(
        EventId = 3464,
        Level = LogLevel.Error,
        Message = "Failed to create SqlServer rule | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerRuleCreationFailed(
        this ILogger logger,
        Exception exception,
        string resourceId,
        string ruleName);

    #endregion

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for Azure resource operations.
    /// </summary>
    public static IDisposable? BeginAzureResourceScope(
        this ILogger logger,
        string resourceType,
        string resourceId,
        string operation)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ResourceType"] = resourceType,
        ["ResourceId"] = resourceId,
        ["Operation"] = operation,
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}