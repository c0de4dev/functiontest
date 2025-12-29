using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Azure Cosmos DB operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 7600-7799
  /// 
  /// Event ID Allocation:
  /// - 7600-7619: OverWriteNetworkRestrictionRules lifecycle
  /// - 7620-7639: AppendNetworkRestrictionRules lifecycle
  /// - 7640-7659: Validation and limit checks
  /// - 7660-7679: Configuration fetching
  /// - 7680-7699: IP rule operations
  /// - 7700-7719: VNet rule operations
  /// - 7720-7739: Apply operations
  /// - 7740-7759: Error conditions
  /// - 7760-7779: PrintOut mode and audit logging
  /// </summary>
  public static partial class CosmosDbLoggerExtensions
  {
    // ============================================================
    // OverWriteNetworkRestrictionRules Lifecycle (EventIds 7600-7619)
    // ============================================================

    [LoggerMessage(
        EventId = 7600,
        Level = LogLevel.Information,
        Message = "CosmosDb OverwriteNetworkRestrictionRules started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogCosmosDbOverwriteStarted(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 7601,
        Level = LogLevel.Information,
        Message = "CosmosDb OverwriteNetworkRestrictionRules completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogCosmosDbOverwriteCompleted(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 7602,
        Level = LogLevel.Error,
        Message = "CosmosDb OverwriteNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbOverwriteFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7603,
        Level = LogLevel.Information,
        Message = "CosmosDb applying config | ResourceId: {ResourceId} | OverwriteMode: {OverwriteMode}")]
    public static partial void LogCosmosDbApplyingConfig(
        this ILogger logger,
        string resourceId,
        bool overwriteMode);

    // ============================================================
    // AppendNetworkRestrictionRules Lifecycle (EventIds 7620-7639)
    // ============================================================

    [LoggerMessage(
        EventId = 7620,
        Level = LogLevel.Information,
        Message = "CosmosDb AppendNetworkRestrictionRules started | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbAppendStarted(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7621,
        Level = LogLevel.Information,
        Message = "CosmosDb AppendNetworkRestrictionRules completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogCosmosDbAppendCompleted(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 7622,
        Level = LogLevel.Error,
        Message = "CosmosDb AppendNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbAppendFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // Validation and Limit Checks (EventIds 7640-7659)
    // ============================================================

    [LoggerMessage(
        EventId = 7640,
        Level = LogLevel.Information,
        Message = "Validating CosmosDb rules | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | Limit: {Limit}")]
    public static partial void LogCosmosDbValidatingRules(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int limit);

    [LoggerMessage(
        EventId = 7641,
        Level = LogLevel.Warning,
        Message = "CosmosDb rule limit exceeded | ResourceId: {ResourceId} | RuleCount: {RuleCount} | Limit: {Limit}")]
    public static partial void LogCosmosDbRuleLimitExceeded(
        this ILogger logger,
        string resourceId,
        int ruleCount,
        int limit);

    [LoggerMessage(
        EventId = 7642,
        Level = LogLevel.Information,
        Message = "CosmosDb validation passed | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogCosmosDbValidationPassed(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    // ============================================================
    // Configuration Fetching (EventIds 7660-7679)
    // ============================================================

    [LoggerMessage(
        EventId = 7660,
        Level = LogLevel.Information,
        Message = "Fetching CosmosDb configuration | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbFetchingConfig(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7661,
        Level = LogLevel.Information,
        Message = "CosmosDb configuration fetched | ResourceId: {ResourceId} | PublicNetworkAccess: {PublicNetworkAccess}")]
    public static partial void LogCosmosDbConfigFetched(
        this ILogger logger,
        string resourceId,
        string publicNetworkAccess);

    [LoggerMessage(
        EventId = 7662,
        Level = LogLevel.Error,
        Message = "Failed to fetch CosmosDb configuration | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbFetchConfigFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7663,
        Level = LogLevel.Debug,
        Message = "CosmosDb existing config | ResourceId: {ResourceId} | ExistingIpRules: {IpRuleCount} | ExistingVNetRules: {VNetRuleCount}")]
    public static partial void LogCosmosDbExistingConfig(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    // ============================================================
    // IP Rule Operations (EventIds 7680-7699)
    // ============================================================

    [LoggerMessage(
        EventId = 7680,
        Level = LogLevel.Debug,
        Message = "Converting IP rules to CosmosDb format | ResourceId: {ResourceId} | InputCount: {InputCount}")]
    public static partial void LogCosmosDbConvertingIpRules(
        this ILogger logger,
        string resourceId,
        int inputCount);

    [LoggerMessage(
        EventId = 7681,
        Level = LogLevel.Debug,
        Message = "CosmosDb IP rules converted | ResourceId: {ResourceId} | OutputCount: {OutputCount}")]
    public static partial void LogCosmosDbIpRulesConverted(
        this ILogger logger,
        string resourceId,
        int outputCount);

    [LoggerMessage(
        EventId = 7682,
        Level = LogLevel.Information,
        Message = "Setting CosmosDb IP filter | ResourceId: {ResourceId} | IpFilterString: {IpFilterLength} chars")]
    public static partial void LogCosmosDbSettingIpFilter(
        this ILogger logger,
        string resourceId,
        int ipFilterLength);

    // ============================================================
    // VNet Rule Operations (EventIds 7700-7719)
    // ============================================================

    [LoggerMessage(
        EventId = 7700,
        Level = LogLevel.Debug,
        Message = "Converting VNet rules to CosmosDb format | ResourceId: {ResourceId} | InputCount: {InputCount}")]
    public static partial void LogCosmosDbConvertingVNetRules(
        this ILogger logger,
        string resourceId,
        int inputCount);

    [LoggerMessage(
        EventId = 7701,
        Level = LogLevel.Debug,
        Message = "CosmosDb VNet rules converted | ResourceId: {ResourceId} | OutputCount: {OutputCount}")]
    public static partial void LogCosmosDbVNetRulesConverted(
        this ILogger logger,
        string resourceId,
        int outputCount);

    [LoggerMessage(
        EventId = 7702,
        Level = LogLevel.Information,
        Message = "Setting CosmosDb virtual network rules | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogCosmosDbSettingVNetRules(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    // ============================================================
    // Apply Operations (EventIds 7720-7739)
    // ============================================================

    [LoggerMessage(
        EventId = 7720,
        Level = LogLevel.Information,
        Message = "Patching CosmosDb configuration | ResourceId: {ResourceId} | Url: {Url}")]
    public static partial void LogCosmosDbPatchingConfig(
        this ILogger logger,
        string resourceId,
        string url);

    [LoggerMessage(
        EventId = 7721,
        Level = LogLevel.Information,
        Message = "CosmosDb configuration patched successfully | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbPatchSucceeded(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7722,
        Level = LogLevel.Error,
        Message = "Failed to patch CosmosDb configuration | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbPatchFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7723,
        Level = LogLevel.Debug,
        Message = "Enabling public network access for CosmosDb | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbEnablingPublicAccess(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7724,
        Level = LogLevel.Information,
        Message = "CosmosDb public network access enabled | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbPublicAccessEnabled(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Error Conditions (EventIds 7740-7759)
    // ============================================================

    [LoggerMessage(
        EventId = 7740,
        Level = LogLevel.Error,
        Message = "Unable to update CosmosDb resource | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbUnableToUpdate(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7741,
        Level = LogLevel.Warning,
        Message = "CosmosDb operation aborted | ResourceId: {ResourceId} | Reason: {Reason}")]
    public static partial void LogCosmosDbOperationAborted(
        this ILogger logger,
        string resourceId,
        string reason);

    [LoggerMessage(
        EventId = 7742,
        Level = LogLevel.Error,
        Message = "CosmosDb response was null or empty | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbResponseNull(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // PrintOut Mode and Audit Logging (EventIds 7760-7779)
    // ============================================================

    [LoggerMessage(
        EventId = 7760,
        Level = LogLevel.Information,
        Message = "CosmosDb PrintOut mode active | ResourceId: {ResourceId}")]
    public static partial void LogCosmosDbPrintOutMode(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7761,
        Level = LogLevel.Information,
        Message = "CosmosDb PrintOut output generated | ResourceId: {ResourceId} | IpCount: {IpCount} | SubnetCount: {SubnetCount}")]
    public static partial void LogCosmosDbPrintOutGenerated(
        this ILogger logger,
        string resourceId,
        int ipCount,
        int subnetCount);

    [LoggerMessage(
        EventId = 7762,
        Level = LogLevel.Information,
        Message = "CosmosDb config before apply | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogCosmosDbConfigBeforeApply(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7763,
        Level = LogLevel.Information,
        Message = "CosmosDb config to be applied | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogCosmosDbConfigToBeApplied(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int vNetRuleCount);

    // ============================================================
    // Scoped Logging Helper
    // ============================================================

    /// <summary>
    /// Creates a logging scope for CosmosDb operations.
    /// </summary>
    public static IDisposable? BeginCosmosDbOperationScope(
        this ILogger logger,
        string operationName,
        string resourceId,
        string? resourceName = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ResourceType"] = "CosmosDb",
        ["OperationName"] = operationName,
        ["ResourceId"] = resourceId,
        ["ResourceName"] = resourceName ?? "Unknown",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}
