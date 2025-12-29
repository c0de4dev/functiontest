using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Azure Key Vault operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 7800-7999
  /// 
  /// Event ID Allocation:
  /// - 7800-7819: OverWriteNetworkRestrictionRules lifecycle
  /// - 7820-7839: AppendNetworkRestrictionRules lifecycle
  /// - 7840-7859: Validation and limit checks
  /// - 7860-7879: Configuration fetching
  /// - 7880-7899: Network ACL operations
  /// - 7900-7919: Apply operations
  /// - 7920-7939: Error conditions
  /// - 7940-7959: PrintOut mode and audit logging
  /// </summary>
  public static partial class KeyVaultLoggerExtensions
  {
    // ============================================================
    // OverWriteNetworkRestrictionRules Lifecycle (EventIds 7800-7819)
    // ============================================================

    [LoggerMessage(
        EventId = 7800,
        Level = LogLevel.Information,
        Message = "KeyVault OverwriteNetworkRestrictionRules started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogKeyVaultOverwriteStarted(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 7801,
        Level = LogLevel.Information,
        Message = "KeyVault OverwriteNetworkRestrictionRules completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogKeyVaultOverwriteCompleted(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 7802,
        Level = LogLevel.Error,
        Message = "KeyVault OverwriteNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultOverwriteFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7803,
        Level = LogLevel.Information,
        Message = "KeyVault applying config | ResourceId: {ResourceId} | OverwriteMode: {OverwriteMode}")]
    public static partial void LogKeyVaultApplyingConfig(
        this ILogger logger,
        string resourceId,
        bool overwriteMode);

    // ============================================================
    // AppendNetworkRestrictionRules Lifecycle (EventIds 7820-7839)
    // ============================================================

    [LoggerMessage(
        EventId = 7820,
        Level = LogLevel.Information,
        Message = "KeyVault AppendNetworkRestrictionRules started | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultAppendStarted(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7821,
        Level = LogLevel.Information,
        Message = "KeyVault AppendNetworkRestrictionRules completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogKeyVaultAppendCompleted(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 7822,
        Level = LogLevel.Error,
        Message = "KeyVault AppendNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultAppendFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // Validation and Limit Checks (EventIds 7840-7859)
    // ============================================================

    [LoggerMessage(
        EventId = 7840,
        Level = LogLevel.Information,
        Message = "Validating KeyVault rules | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | Limit: {Limit}")]
    public static partial void LogKeyVaultValidatingRules(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int limit);

    [LoggerMessage(
        EventId = 7841,
        Level = LogLevel.Warning,
        Message = "KeyVault rule limit exceeded | ResourceId: {ResourceId} | RuleCount: {RuleCount} | Limit: {Limit}")]
    public static partial void LogKeyVaultRuleLimitExceeded(
        this ILogger logger,
        string resourceId,
        int ruleCount,
        int limit);

    [LoggerMessage(
        EventId = 7842,
        Level = LogLevel.Information,
        Message = "KeyVault validation passed | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogKeyVaultValidationPassed(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 7843,
        Level = LogLevel.Debug,
        Message = "KeyVault rule count within limits | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount} | Limit: {Limit}")]
    public static partial void LogKeyVaultRuleCountWithinLimits(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount,
        int limit);

    // ============================================================
    // Configuration Fetching (EventIds 7860-7879)
    // ============================================================

    [LoggerMessage(
        EventId = 7860,
        Level = LogLevel.Information,
        Message = "Fetching KeyVault configuration | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultFetchingConfig(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7861,
        Level = LogLevel.Information,
        Message = "KeyVault configuration fetched | ResourceId: {ResourceId} | DefaultAction: {DefaultAction}")]
    public static partial void LogKeyVaultConfigFetched(
        this ILogger logger,
        string resourceId,
        string defaultAction);

    [LoggerMessage(
        EventId = 7862,
        Level = LogLevel.Error,
        Message = "Failed to fetch KeyVault configuration | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultFetchConfigFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7863,
        Level = LogLevel.Debug,
        Message = "KeyVault existing config | ResourceId: {ResourceId} | ExistingIpRules: {IpRuleCount} | ExistingVNetRules: {VNetRuleCount}")]
    public static partial void LogKeyVaultExistingConfig(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7864,
        Level = LogLevel.Error,
        Message = "KeyVault response was null or empty | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultResponseNull(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Network ACL Operations (EventIds 7880-7899)
    // ============================================================

    [LoggerMessage(
        EventId = 7880,
        Level = LogLevel.Debug,
        Message = "Converting rules to KeyVault network ACL format | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultConvertingToNetworkAcl(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7881,
        Level = LogLevel.Debug,
        Message = "KeyVault network ACL prepared | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogKeyVaultNetworkAclPrepared(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7882,
        Level = LogLevel.Information,
        Message = "Setting KeyVault default action | ResourceId: {ResourceId} | DefaultAction: {DefaultAction}")]
    public static partial void LogKeyVaultSettingDefaultAction(
        this ILogger logger,
        string resourceId,
        string defaultAction);

    [LoggerMessage(
        EventId = 7883,
        Level = LogLevel.Debug,
        Message = "Merging KeyVault network rules | ResourceId: {ResourceId} | ExistingIpRules: {ExistingIpRules} | NewIpRules: {NewIpRules}")]
    public static partial void LogKeyVaultMergingRules(
        this ILogger logger,
        string resourceId,
        int existingIpRules,
        int newIpRules);

    // ============================================================
    // Apply Operations (EventIds 7900-7919)
    // ============================================================

    [LoggerMessage(
        EventId = 7900,
        Level = LogLevel.Information,
        Message = "Patching KeyVault configuration | ResourceId: {ResourceId} | Url: {Url}")]
    public static partial void LogKeyVaultPatchingConfig(
        this ILogger logger,
        string resourceId,
        string url);

    [LoggerMessage(
        EventId = 7901,
        Level = LogLevel.Information,
        Message = "KeyVault configuration patched successfully | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultPatchSucceeded(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7902,
        Level = LogLevel.Error,
        Message = "Failed to patch KeyVault configuration | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultPatchFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7903,
        Level = LogLevel.Debug,
        Message = "KeyVault PATCH request prepared | ResourceId: {ResourceId} | ContentLength: {ContentLength}")]
    public static partial void LogKeyVaultPatchRequestPrepared(
        this ILogger logger,
        string resourceId,
        int contentLength);

    // ============================================================
    // Error Conditions (EventIds 7920-7939)
    // ============================================================

    [LoggerMessage(
        EventId = 7920,
        Level = LogLevel.Error,
        Message = "Unable to update KeyVault resource | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultUnableToUpdate(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7921,
        Level = LogLevel.Warning,
        Message = "KeyVault operation aborted | ResourceId: {ResourceId} | Reason: {Reason}")]
    public static partial void LogKeyVaultOperationAborted(
        this ILogger logger,
        string resourceId,
        string reason);

    [LoggerMessage(
        EventId = 7922,
        Level = LogLevel.Warning,
        Message = "KeyVault not configured for network restrictions | ResourceId: {ResourceId} | DefaultAction: Allow")]
    public static partial void LogKeyVaultNotConfiguredForRestrictions(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // PrintOut Mode and Audit Logging (EventIds 7940-7959)
    // ============================================================

    [LoggerMessage(
        EventId = 7940,
        Level = LogLevel.Information,
        Message = "KeyVault PrintOut mode active | ResourceId: {ResourceId}")]
    public static partial void LogKeyVaultPrintOutMode(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7941,
        Level = LogLevel.Information,
        Message = "KeyVault PrintOut output generated | ResourceId: {ResourceId} | IpCount: {IpCount} | SubnetCount: {SubnetCount}")]
    public static partial void LogKeyVaultPrintOutGenerated(
        this ILogger logger,
        string resourceId,
        int ipCount,
        int subnetCount);

    [LoggerMessage(
        EventId = 7942,
        Level = LogLevel.Information,
        Message = "KeyVault config before apply | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogKeyVaultConfigBeforeApply(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7943,
        Level = LogLevel.Information,
        Message = "KeyVault config to be applied | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogKeyVaultConfigToBeApplied(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    // ============================================================
    // Scoped Logging Helper
    // ============================================================

    /// <summary>
    /// Creates a logging scope for KeyVault operations.
    /// </summary>
    public static IDisposable? BeginKeyVaultOperationScope(
        this ILogger logger,
        string operationName,
        string resourceId,
        string? resourceName = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ResourceType"] = "KeyVault",
        ["OperationName"] = operationName,
        ["ResourceId"] = resourceId,
        ["ResourceName"] = resourceName ?? "Unknown",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}
