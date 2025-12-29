using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for SQL Server operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 7200-7399
  /// 
  /// Event ID Allocation:
  /// - 7200-7219: OverWriteNetworkRestrictionRules lifecycle
  /// - 7220-7239: AppendNetworkRestrictionRules lifecycle
  /// - 7240-7259: Validation operations
  /// - 7260-7279: Rule generation
  /// - 7280-7299: Firewall rule operations (CRUD)
  /// - 7300-7319: VNet rule operations (CRUD)
  /// - 7320-7339: Public access operations
  /// - 7340-7359: Existing rules fetching
  /// - 7360-7379: Delete operations (overwrite cleanup)
  /// - 7380-7399: Error conditions and PrintOut mode
  /// </summary>
  public static partial class SqlServerLoggerExtensions
  {
    // ============================================================
    // OverWriteNetworkRestrictionRules Lifecycle (EventIds 7200-7219)
    // ============================================================

    [LoggerMessage(
        EventId = 7200,
        Level = LogLevel.Information,
        Message = "SqlServer OverwriteNetworkRestrictionRules started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogSqlServerOverwriteStarted(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 7201,
        Level = LogLevel.Information,
        Message = "SqlServer OverwriteNetworkRestrictionRules completed | ResourceId: {ResourceId} | FirewallRulesApplied: {FwRulesApplied} | VNetRulesApplied: {VNetRulesApplied}")]
    public static partial void LogSqlServerOverwriteCompleted(
        this ILogger logger,
        string resourceId,
        int fwRulesApplied,
        int vNetRulesApplied);

    [LoggerMessage(
        EventId = 7202,
        Level = LogLevel.Error,
        Message = "SqlServer OverwriteNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerOverwriteFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7203,
        Level = LogLevel.Information,
        Message = "SqlServer applying config | ResourceId: {ResourceId} | OverwriteMode: {OverwriteMode}")]
    public static partial void LogSqlServerApplyingConfig(
        this ILogger logger,
        string resourceId,
        bool overwriteMode);

    // ============================================================
    // AppendNetworkRestrictionRules Lifecycle (EventIds 7220-7239)
    // ============================================================

    [LoggerMessage(
        EventId = 7220,
        Level = LogLevel.Information,
        Message = "SqlServer AppendNetworkRestrictionRules started | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerAppendStarted(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7221,
        Level = LogLevel.Information,
        Message = "SqlServer AppendNetworkRestrictionRules completed | ResourceId: {ResourceId} | TotalFwRules: {TotalFwRules} | TotalVNetRules: {TotalVNetRules}")]
    public static partial void LogSqlServerAppendCompleted(
        this ILogger logger,
        string resourceId,
        int totalFwRules,
        int totalVNetRules);

    [LoggerMessage(
        EventId = 7222,
        Level = LogLevel.Error,
        Message = "SqlServer AppendNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerAppendFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // Validation Operations (EventIds 7240-7259)
    // ============================================================

    [LoggerMessage(
        EventId = 7240,
        Level = LogLevel.Information,
        Message = "Validating SqlServer rules | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | Limit: {Limit}")]
    public static partial void LogSqlServerValidatingRules(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int limit);

    [LoggerMessage(
        EventId = 7241,
        Level = LogLevel.Warning,
        Message = "SqlServer rule limit exceeded | ResourceId: {ResourceId} | RuleCount: {RuleCount} | Limit: {Limit}")]
    public static partial void LogSqlServerRuleLimitExceeded(
        this ILogger logger,
        string resourceId,
        int ruleCount,
        int limit);

    [LoggerMessage(
        EventId = 7242,
        Level = LogLevel.Information,
        Message = "SqlServer validation passed | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogSqlServerValidationPassed(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 7243,
        Level = LogLevel.Error,
        Message = "SqlServer validation failed | ResourceId: {ResourceId} | Reason: {Reason}")]
    public static partial void LogSqlServerValidationFailed(
        this ILogger logger,
        string resourceId,
        string reason);

    // ============================================================
    // Rule Generation (EventIds 7260-7279)
    // ============================================================

    [LoggerMessage(
        EventId = 7260,
        Level = LogLevel.Information,
        Message = "Generating SqlServer rules | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerGeneratingRules(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7261,
        Level = LogLevel.Information,
        Message = "SqlServer rules generated | ResourceId: {ResourceId} | FirewallRuleCount: {FwRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogSqlServerRulesGenerated(
        this ILogger logger,
        string resourceId,
        int fwRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7262,
        Level = LogLevel.Debug,
        Message = "Generating firewall rule | ResourceId: {ResourceId} | RuleName: {RuleName} | StartIP: {StartIP} | EndIP: {EndIP}")]
    public static partial void LogSqlServerGeneratingFwRule(
        this ILogger logger,
        string resourceId,
        string ruleName,
        string startIP,
        string endIP);

    [LoggerMessage(
        EventId = 7263,
        Level = LogLevel.Debug,
        Message = "Generating VNet rule | ResourceId: {ResourceId} | RuleName: {RuleName} | SubnetId: {SubnetId}")]
    public static partial void LogSqlServerGeneratingVNetRule(
        this ILogger logger,
        string resourceId,
        string ruleName,
        string subnetId);

    // ============================================================
    // Firewall Rule Operations - CRUD (EventIds 7280-7299)
    // ============================================================

    [LoggerMessage(
        EventId = 7280,
        Level = LogLevel.Information,
        Message = "Creating firewall rule | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerCreatingFwRule(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7281,
        Level = LogLevel.Information,
        Message = "Firewall rule created | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerFwRuleCreated(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7282,
        Level = LogLevel.Error,
        Message = "Failed to create firewall rule | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerFwRuleCreateFailed(
        this ILogger logger,
        Exception exception,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7283,
        Level = LogLevel.Debug,
        Message = "Firewall rule already exists | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerFwRuleAlreadyExists(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7284,
        Level = LogLevel.Information,
        Message = "Checking existing firewall rules | ResourceId: {ResourceId} | GeneratedCount: {GeneratedCount} | ExistingCount: {ExistingCount}")]
    public static partial void LogSqlServerCheckingExistingFwRules(
        this ILogger logger,
        string resourceId,
        int generatedCount,
        int existingCount);

    [LoggerMessage(
        EventId = 7285,
        Level = LogLevel.Information,
        Message = "Firewall rules to create | ResourceId: {ResourceId} | Count: {Count}")]
    public static partial void LogSqlServerFwRulesToCreate(
        this ILogger logger,
        string resourceId,
        int count);

    // ============================================================
    // VNet Rule Operations - CRUD (EventIds 7300-7319)
    // ============================================================

    [LoggerMessage(
        EventId = 7300,
        Level = LogLevel.Information,
        Message = "Creating VNet rule | ResourceId: {ResourceId} | RuleName: {RuleName} | SubnetId: {SubnetId}")]
    public static partial void LogSqlServerCreatingVNetRule(
        this ILogger logger,
        string resourceId,
        string ruleName,
        string subnetId);

    [LoggerMessage(
        EventId = 7301,
        Level = LogLevel.Information,
        Message = "VNet rule created | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerVNetRuleCreated(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7302,
        Level = LogLevel.Error,
        Message = "Failed to create VNet rule | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerVNetRuleCreateFailed(
        this ILogger logger,
        Exception exception,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7303,
        Level = LogLevel.Debug,
        Message = "VNet rule already exists | ResourceId: {ResourceId} | SubnetId: {SubnetId}")]
    public static partial void LogSqlServerVNetRuleAlreadyExists(
        this ILogger logger,
        string resourceId,
        string subnetId);

    [LoggerMessage(
        EventId = 7304,
        Level = LogLevel.Information,
        Message = "Checking existing VNet rules | ResourceId: {ResourceId} | GeneratedCount: {GeneratedCount} | ExistingCount: {ExistingCount}")]
    public static partial void LogSqlServerCheckingExistingVNetRules(
        this ILogger logger,
        string resourceId,
        int generatedCount,
        int existingCount);

    [LoggerMessage(
        EventId = 7305,
        Level = LogLevel.Information,
        Message = "VNet rules to create | ResourceId: {ResourceId} | Count: {Count}")]
    public static partial void LogSqlServerVNetRulesToCreate(
        this ILogger logger,
        string resourceId,
        int count);

    // ============================================================
    // Public Access Operations (EventIds 7320-7339)
    // ============================================================

    [LoggerMessage(
        EventId = 7320,
        Level = LogLevel.Information,
        Message = "Checking public network access | ResourceId: {ResourceId} | CurrentState: {CurrentState}")]
    public static partial void LogSqlServerCheckingPublicAccess(
        this ILogger logger,
        string resourceId,
        string currentState);

    [LoggerMessage(
        EventId = 7321,
        Level = LogLevel.Information,
        Message = "Disabling public network access | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerDisablingPublicAccess(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7322,
        Level = LogLevel.Information,
        Message = "Public network access disabled | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerPublicAccessDisabled(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7323,
        Level = LogLevel.Debug,
        Message = "Public network access already enabled | ResourceId: {ResourceId} | No action needed")]
    public static partial void LogSqlServerPublicAccessAlreadyEnabled(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7324,
        Level = LogLevel.Error,
        Message = "Failed to disable public network access | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerDisablePublicAccessFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    // ============================================================
    // Existing Rules Fetching (EventIds 7340-7359)
    // ============================================================

    [LoggerMessage(
        EventId = 7340,
        Level = LogLevel.Information,
        Message = "Fetching existing SqlServer rules | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerFetchingExistingRules(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7341,
        Level = LogLevel.Information,
        Message = "Existing SqlServer rules fetched | ResourceId: {ResourceId} | FirewallRuleCount: {FwRuleCount} | VNetRuleCount: {VNetRuleCount}")]
    public static partial void LogSqlServerExistingRulesFetched(
        this ILogger logger,
        string resourceId,
        int fwRuleCount,
        int vNetRuleCount);

    [LoggerMessage(
        EventId = 7342,
        Level = LogLevel.Error,
        Message = "Failed to fetch existing SqlServer rules | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerFetchExistingRulesFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7343,
        Level = LogLevel.Debug,
        Message = "Fetching firewall rules from API | ResourceId: {ResourceId} | Url: {Url}")]
    public static partial void LogSqlServerFetchingFwRulesFromApi(
        this ILogger logger,
        string resourceId,
        string url);

    [LoggerMessage(
        EventId = 7344,
        Level = LogLevel.Debug,
        Message = "Fetching VNet rules from API | ResourceId: {ResourceId} | Url: {Url}")]
    public static partial void LogSqlServerFetchingVNetRulesFromApi(
        this ILogger logger,
        string resourceId,
        string url);

    // ============================================================
    // Delete Operations - Overwrite Cleanup (EventIds 7360-7379)
    // ============================================================

    [LoggerMessage(
        EventId = 7360,
        Level = LogLevel.Information,
        Message = "Deleting leftover firewall rules | ResourceId: {ResourceId} | Count: {Count}")]
    public static partial void LogSqlServerDeletingLeftoverFwRules(
        this ILogger logger,
        string resourceId,
        int count);

    [LoggerMessage(
        EventId = 7361,
        Level = LogLevel.Information,
        Message = "Firewall rule deleted | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerFwRuleDeleted(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7362,
        Level = LogLevel.Warning,
        Message = "Failed to delete firewall rule | ResourceId: {ResourceId} | RuleName: {RuleName} | Reason: Resource group may have delete lock")]
    public static partial void LogSqlServerFwRuleDeleteFailed(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7363,
        Level = LogLevel.Information,
        Message = "Deleting leftover VNet rules | ResourceId: {ResourceId} | Count: {Count}")]
    public static partial void LogSqlServerDeletingLeftoverVNetRules(
        this ILogger logger,
        string resourceId,
        int count);

    [LoggerMessage(
        EventId = 7364,
        Level = LogLevel.Information,
        Message = "VNet rule deleted | ResourceId: {ResourceId} | RuleName: {RuleName}")]
    public static partial void LogSqlServerVNetRuleDeleted(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7365,
        Level = LogLevel.Warning,
        Message = "Failed to delete VNet rule | ResourceId: {ResourceId} | RuleName: {RuleName} | Reason: Resource group may have delete lock")]
    public static partial void LogSqlServerVNetRuleDeleteFailed(
        this ILogger logger,
        string resourceId,
        string ruleName);

    [LoggerMessage(
        EventId = 7366,
        Level = LogLevel.Debug,
        Message = "No leftover firewall rules to delete | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerNoLeftoverFwRules(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7367,
        Level = LogLevel.Debug,
        Message = "No leftover VNet rules to delete | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerNoLeftoverVNetRules(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Error Conditions and PrintOut Mode (EventIds 7380-7399)
    // ============================================================

    [LoggerMessage(
        EventId = 7380,
        Level = LogLevel.Information,
        Message = "SqlServer PrintOut mode active | ResourceId: {ResourceId} | Skipping actual apply")]
    public static partial void LogSqlServerPrintOutMode(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7381,
        Level = LogLevel.Information,
        Message = "SqlServer PrintOut output generated | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | SubnetCount: {SubnetCount}")]
    public static partial void LogSqlServerPrintOutGenerated(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int subnetCount);

    [LoggerMessage(
        EventId = 7382,
        Level = LogLevel.Error,
        Message = "SqlServer cannot generate IP restriction rules for itself | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerCannotGenerateRules(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7383,
        Level = LogLevel.Warning,
        Message = "SqlServer operation aborted due to validation failure | ResourceId: {ResourceId}")]
    public static partial void LogSqlServerOperationAborted(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Scoped Logging Helper
    // ============================================================

    /// <summary>
    /// Creates a logging scope for SqlServer operations.
    /// </summary>
    public static IDisposable? BeginSqlServerOperationScope(
        this ILogger logger,
        string operationName,
        string resourceId,
        string? resourceName = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ResourceType"] = "SqlServer",
        ["OperationName"] = operationName,
        ["ResourceId"] = resourceId,
        ["ResourceName"] = resourceName ?? "Unknown",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}
