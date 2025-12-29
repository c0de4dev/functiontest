using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Azure Storage Account operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 7400-7599
  /// 
  /// Event ID Allocation:
  /// - 7400-7419: OverWriteNetworkRestrictionRules lifecycle
  /// - 7420-7439: AppendNetworkRestrictionRules lifecycle
  /// - 7440-7459: Validation and limit checks
  /// - 7460-7479: Configuration fetching (ARM properties)
  /// - 7480-7499: Configuration conversion and preparation
  /// - 7500-7519: Apply operations (PATCH)
  /// - 7520-7539: Provisioning state checks
  /// - 7540-7559: PrintOut mode
  /// - 7560-7579: Error conditions
  /// - 7580-7599: Audit logging
  /// </summary>
  public static partial class StorageLoggerExtensions
  {
    // ============================================================
    // OverWriteNetworkRestrictionRules Lifecycle (EventIds 7400-7419)
    // ============================================================

    [LoggerMessage(
        EventId = 7400,
        Level = LogLevel.Information,
        Message = "Storage OverwriteNetworkRestrictionRules started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogStorageOverwriteStarted(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 7401,
        Level = LogLevel.Information,
        Message = "Storage OverwriteNetworkRestrictionRules completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogStorageOverwriteCompleted(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 7402,
        Level = LogLevel.Error,
        Message = "Storage OverwriteNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogStorageOverwriteFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7403,
        Level = LogLevel.Information,
        Message = "Storage applying config | ResourceId: {ResourceId} | OverwriteMode: {OverwriteMode}")]
    public static partial void LogStorageApplyingConfig(
        this ILogger logger,
        string resourceId,
        bool overwriteMode);

    // ============================================================
    // AppendNetworkRestrictionRules Lifecycle (EventIds 7420-7439)
    // ============================================================

    [LoggerMessage(
        EventId = 7420,
        Level = LogLevel.Information,
        Message = "Storage AppendNetworkRestrictionRules started | ResourceId: {ResourceId}")]
    public static partial void LogStorageAppendStarted(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7421,
        Level = LogLevel.Information,
        Message = "Storage AppendNetworkRestrictionRules completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogStorageAppendCompleted(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 7422,
        Level = LogLevel.Error,
        Message = "Storage AppendNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogStorageAppendFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7423,
        Level = LogLevel.Information,
        Message = "No existing restrictions for append | ResourceId: {ResourceId} | DefaultAction not 'Deny' - resource may allow all")]
    public static partial void LogStorageNoExistingRestrictionsForAppend(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Validation and Limit Checks (EventIds 7440-7459)
    // ============================================================

    [LoggerMessage(
        EventId = 7440,
        Level = LogLevel.Information,
        Message = "Validating Storage rules | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | Limit: {Limit}")]
    public static partial void LogStorageValidatingRules(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int limit);

    [LoggerMessage(
        EventId = 7441,
        Level = LogLevel.Warning,
        Message = "Storage rule limit exceeded | ResourceId: {ResourceId} | RuleCount: {RuleCount} | Limit: {Limit}")]
    public static partial void LogStorageRuleLimitExceeded(
        this ILogger logger,
        string resourceId,
        int ruleCount,
        int limit);

    [LoggerMessage(
        EventId = 7442,
        Level = LogLevel.Information,
        Message = "Storage validation passed | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogStorageValidationPassed(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 7443,
        Level = LogLevel.Debug,
        Message = "Storage rule count within limits | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount} | Limit: {Limit}")]
    public static partial void LogStorageRuleCountWithinLimits(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount,
        int limit);

    // ============================================================
    // Configuration Fetching - ARM Properties (EventIds 7460-7479)
    // ============================================================

    [LoggerMessage(
        EventId = 7460,
        Level = LogLevel.Information,
        Message = "Fetching Storage ARM properties | ResourceId: {ResourceId}")]
    public static partial void LogStorageFetchingArmProperties(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7461,
        Level = LogLevel.Information,
        Message = "Storage ARM properties fetched | ResourceId: {ResourceId} | ProvisioningState: {ProvisioningState}")]
    public static partial void LogStorageArmPropertiesFetched(
        this ILogger logger,
        string resourceId,
        string provisioningState);

    [LoggerMessage(
        EventId = 7462,
        Level = LogLevel.Error,
        Message = "Storage ARM properties response was null or empty | ResourceId: {ResourceId}")]
    public static partial void LogStorageArmPropertiesNull(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7463,
        Level = LogLevel.Error,
        Message = "Storage ARM properties could not be resolved | ResourceId: {ResourceId}")]
    public static partial void LogStorageArmPropertiesResolveFailed(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7464,
        Level = LogLevel.Debug,
        Message = "Storage existing config retrieved | ResourceId: {ResourceId} | DefaultAction: {DefaultAction} | ExistingIpRules: {IpRuleCount} | ExistingVNetRules: {VNetRuleCount}")]
    public static partial void LogStorageExistingConfigRetrieved(
        this ILogger logger,
        string resourceId,
        string defaultAction,
        int ipRuleCount,
        int vNetRuleCount);

    // ============================================================
    // Configuration Conversion and Preparation (EventIds 7480-7499)
    // ============================================================

    [LoggerMessage(
        EventId = 7480,
        Level = LogLevel.Debug,
        Message = "Converting to Storage firewall settings | ResourceId: {ResourceId}")]
    public static partial void LogStorageConvertingToFirewallSettings(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7481,
        Level = LogLevel.Debug,
        Message = "Storage firewall settings converted | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogStorageFirewallSettingsConverted(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7482,
        Level = LogLevel.Information,
        Message = "Overwriting Storage network ACLs | ResourceId: {ResourceId} | NewIpRuleCount: {IpRuleCount} | NewVNetRuleCount: {VNetRuleCount}")]
    public static partial void LogStorageOverwritingNetworkAcls(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7483,
        Level = LogLevel.Information,
        Message = "Merging Storage network ACLs | ResourceId: {ResourceId} | ExistingIpRules: {ExistingIpRules} | NewIpRules: {NewIpRules} | MergedCount: {MergedCount}")]
    public static partial void LogStorageMergingNetworkAcls(
        this ILogger logger,
        string resourceId,
        int existingIpRules,
        int newIpRules,
        int mergedCount);

    [LoggerMessage(
        EventId = 7484,
        Level = LogLevel.Debug,
        Message = "Setting DefaultAction to 'Deny' | ResourceId: {ResourceId}")]
    public static partial void LogStorageSettingDefaultActionDeny(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Apply Operations - PATCH (EventIds 7500-7519)
    // ============================================================

    [LoggerMessage(
        EventId = 7500,
        Level = LogLevel.Information,
        Message = "Patching Storage configuration | ResourceId: {ResourceId} | Url: {Url}")]
    public static partial void LogStoragePatchingConfig(
        this ILogger logger,
        string resourceId,
        string url);

    [LoggerMessage(
        EventId = 7501,
        Level = LogLevel.Information,
        Message = "Storage configuration patched successfully | ResourceId: {ResourceId}")]
    public static partial void LogStoragePatchSucceeded(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7502,
        Level = LogLevel.Error,
        Message = "Failed to patch Storage configuration | ResourceId: {ResourceId}")]
    public static partial void LogStoragePatchFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7503,
        Level = LogLevel.Debug,
        Message = "Storage PATCH request body prepared | ResourceId: {ResourceId} | ContentLength: {ContentLength}")]
    public static partial void LogStoragePatchRequestPrepared(
        this ILogger logger,
        string resourceId,
        int contentLength);

    // ============================================================
    // Provisioning State Checks (EventIds 7520-7539)
    // ============================================================

    [LoggerMessage(
        EventId = 7520,
        Level = LogLevel.Information,
        Message = "Checking Storage provisioning state | ResourceId: {ResourceId}")]
    public static partial void LogStorageCheckingProvisioningState(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7521,
        Level = LogLevel.Information,
        Message = "Storage provisioning state valid | ResourceId: {ResourceId} | State: {State}")]
    public static partial void LogStorageProvisioningStateValid(
        this ILogger logger,
        string resourceId,
        string state);

    [LoggerMessage(
        EventId = 7522,
        Level = LogLevel.Error,
        Message = "Storage provisioning state invalid | ResourceId: {ResourceId} | CurrentState: {CurrentState} | ExpectedState: Succeeded")]
    public static partial void LogStorageProvisioningStateInvalid(
        this ILogger logger,
        string resourceId,
        string currentState);

    [LoggerMessage(
        EventId = 7523,
        Level = LogLevel.Warning,
        Message = "Cannot update Storage - provisioning not complete | ResourceId: {ResourceId} | State: {State}")]
    public static partial void LogStorageCannotUpdateProvisioningIncomplete(
        this ILogger logger,
        string resourceId,
        string state);

    // ============================================================
    // PrintOut Mode (EventIds 7540-7559)
    // ============================================================

    [LoggerMessage(
        EventId = 7540,
        Level = LogLevel.Information,
        Message = "Storage PrintOut mode active | ResourceId: {ResourceId} | Skipping actual apply")]
    public static partial void LogStoragePrintOutMode(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7541,
        Level = LogLevel.Information,
        Message = "Storage PrintOut output generated | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | SubnetCount: {SubnetCount}")]
    public static partial void LogStoragePrintOutGenerated(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int subnetCount);

    // ============================================================
    // Error Conditions (EventIds 7560-7579)
    // ============================================================

    [LoggerMessage(
        EventId = 7560,
        Level = LogLevel.Error,
        Message = "Unable to update Storage resource | ResourceId: {ResourceId}")]
    public static partial void LogStorageUnableToUpdate(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7561,
        Level = LogLevel.Warning,
        Message = "Storage operation aborted due to limit exceeded | ResourceId: {ResourceId}")]
    public static partial void LogStorageOperationAbortedLimitExceeded(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7562,
        Level = LogLevel.Warning,
        Message = "Storage operation aborted due to provisioning state | ResourceId: {ResourceId}")]
    public static partial void LogStorageOperationAbortedProvisioningState(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Audit Logging (EventIds 7580-7599)
    // ============================================================

    [LoggerMessage(
        EventId = 7580,
        Level = LogLevel.Information,
        Message = "Storage existing config before apply | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogStorageExistingConfigBeforeApply(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7581,
        Level = LogLevel.Information,
        Message = "Storage config to be applied | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogStorageConfigToBeApplied(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7582,
        Level = LogLevel.Debug,
        Message = "Storage update request sent | ResourceId: {ResourceId}")]
    public static partial void LogStorageUpdateRequestSent(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Scoped Logging Helper
    // ============================================================

    /// <summary>
    /// Creates a logging scope for Storage operations.
    /// </summary>
    public static IDisposable? BeginStorageOperationScope(
        this ILogger logger,
        string operationName,
        string resourceId,
        string? resourceName = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ResourceType"] = "Storage",
        ["OperationName"] = operationName,
        ["ResourceId"] = resourceId,
        ["ResourceName"] = resourceName ?? "Unknown",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}
