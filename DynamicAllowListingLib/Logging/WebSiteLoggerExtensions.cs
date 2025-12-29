using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for WebSite (Azure App Service) operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 7000-7199
  /// 
  /// Event ID Allocation:
  /// - 7000-7019: OverWriteNetworkRestrictionRules lifecycle
  /// - 7020-7039: AppendNetworkRestrictionRules lifecycle
  /// - 7040-7059: Configuration fetching and parsing
  /// - 7060-7079: Rule consolidation and splitting
  /// - 7080-7099: Apply and verify operations
  /// - 7100-7119: Validation operations
  /// - 7120-7139: Limit checks
  /// - 7140-7159: Error conditions
  /// - 7160-7179: Audit logging (before/after comparisons)
  /// </summary>
  public static partial class WebSiteLoggerExtensions
  {
    // ============================================================
    // OverWriteNetworkRestrictionRules Lifecycle (EventIds 7000-7019)
    // ============================================================

    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Information,
        Message = "WebSite OverwriteNetworkRestrictionRules started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogWebSiteOverwriteStarted(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Information,
        Message = "WebSite OverwriteNetworkRestrictionRules completed | ResourceId: {ResourceId} | IpRulesApplied: {IpRulesApplied} | ScmRulesApplied: {ScmRulesApplied}")]
    public static partial void LogWebSiteOverwriteCompleted(
        this ILogger logger,
        string resourceId,
        int ipRulesApplied,
        int scmRulesApplied);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Error,
        Message = "WebSite OverwriteNetworkRestrictionRules failed | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogWebSiteOverwriteFailed(
        this ILogger logger,
        Exception exception,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 7003,
        Level = LogLevel.Warning,
        Message = "WebSite ResourceId is null | Operation cannot proceed")]
    public static partial void LogWebSiteResourceIdNull(
        this ILogger logger);

    // ============================================================
    // AppendNetworkRestrictionRules Lifecycle (EventIds 7020-7039)
    // ============================================================

    [LoggerMessage(
        EventId = 7020,
        Level = LogLevel.Information,
        Message = "WebSite AppendNetworkRestrictionRules started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogWebSiteAppendStarted(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 7021,
        Level = LogLevel.Information,
        Message = "WebSite AppendNetworkRestrictionRules completed | ResourceId: {ResourceId} | TotalIpRules: {TotalIpRules} | TotalScmRules: {TotalScmRules}")]
    public static partial void LogWebSiteAppendCompleted(
        this ILogger logger,
        string resourceId,
        int totalIpRules,
        int totalScmRules);

    [LoggerMessage(
        EventId = 7022,
        Level = LogLevel.Error,
        Message = "WebSite AppendNetworkRestrictionRules failed | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteAppendFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7023,
        Level = LogLevel.Information,
        Message = "No existing restrictions found for append | ResourceId: {ResourceId} | Resource may already allow all traffic")]
    public static partial void LogWebSiteNoExistingRestrictionsForAppend(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Configuration Fetching and Parsing (EventIds 7040-7059)
    // ============================================================

    [LoggerMessage(
        EventId = 7040,
        Level = LogLevel.Debug,
        Message = "Fetching WebSite configuration | ResourceId: {ResourceId} | Url: {Url}")]
    public static partial void LogWebSiteFetchingConfig(
        this ILogger logger,
        string resourceId,
        string url);

    [LoggerMessage(
        EventId = 7041,
        Level = LogLevel.Debug,
        Message = "WebSite configuration retrieved | ResourceId: {ResourceId} | HasIpSecurityRestrictions: {HasIpRestrictions} | HasScmRestrictions: {HasScmRestrictions}")]
    public static partial void LogWebSiteConfigRetrieved(
        this ILogger logger,
        string resourceId,
        bool hasIpRestrictions,
        bool hasScmRestrictions);

    [LoggerMessage(
        EventId = 7042,
        Level = LogLevel.Debug,
        Message = "Preparing network restriction settings | ResourceId: {ResourceId} | IpSecRuleCount: {IpSecRuleCount} | ScmIpSecRuleCount: {ScmRuleCount}")]
    public static partial void LogWebSitePreparingSettings(
        this ILogger logger,
        string resourceId,
        int ipSecRuleCount,
        int scmRuleCount);

    [LoggerMessage(
        EventId = 7043,
        Level = LogLevel.Warning,
        Message = "No existing IP security rules found before apply | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteNoExistingIpRules(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7044,
        Level = LogLevel.Information,
        Message = "Existing IP restrictions detected | ResourceId: {ResourceId} | ExistingCount: {ExistingCount}")]
    public static partial void LogWebSiteExistingIpRestrictionsDetected(
        this ILogger logger,
        string resourceId,
        int existingCount);

    [LoggerMessage(
        EventId = 7045,
        Level = LogLevel.Information,
        Message = "Existing SCM restrictions detected | ResourceId: {ResourceId} | ExistingCount: {ExistingCount}")]
    public static partial void LogWebSiteExistingScmRestrictionsDetected(
        this ILogger logger,
        string resourceId,
        int existingCount);

    // ============================================================
    // Rule Consolidation and Splitting (EventIds 7060-7079)
    // ============================================================

    [LoggerMessage(
        EventId = 7060,
        Level = LogLevel.Debug,
        Message = "Consolidating IP addresses | ResourceId: {ResourceId} | InputRuleCount: {InputCount}")]
    public static partial void LogWebSiteConsolidatingIpAddresses(
        this ILogger logger,
        string resourceId,
        int inputCount);

    [LoggerMessage(
        EventId = 7061,
        Level = LogLevel.Debug,
        Message = "IP addresses consolidated | ResourceId: {ResourceId} | OutputRuleCount: {OutputCount} | Reduction: {Reduction}")]
    public static partial void LogWebSiteIpAddressesConsolidated(
        this ILogger logger,
        string resourceId,
        int outputCount,
        int reduction);

    [LoggerMessage(
        EventId = 7062,
        Level = LogLevel.Debug,
        Message = "Splitting rules | ResourceId: {ResourceId} | RuleName: {RuleName} | IpCount: {IpCount} | SplitInto: {SplitCount} groups")]
    public static partial void LogWebSiteSplittingRules(
        this ILogger logger,
        string resourceId,
        string ruleName,
        int ipCount,
        int splitCount);

    [LoggerMessage(
        EventId = 7063,
        Level = LogLevel.Debug,
        Message = "Service tag rule preserved | ResourceId: {ResourceId} | RuleName: {RuleName} | Tag: {Tag}")]
    public static partial void LogWebSiteServiceTagPreserved(
        this ILogger logger,
        string resourceId,
        string ruleName,
        string tag);

    [LoggerMessage(
        EventId = 7064,
        Level = LogLevel.Debug,
        Message = "Subnet rule preserved | ResourceId: {ResourceId} | RuleName: {RuleName} | SubnetId: {SubnetId}")]
    public static partial void LogWebSiteSubnetRulePreserved(
        this ILogger logger,
        string resourceId,
        string ruleName,
        string subnetId);

    // ============================================================
    // Apply and Verify Operations (EventIds 7080-7099)
    // ============================================================

    [LoggerMessage(
        EventId = 7080,
        Level = LogLevel.Information,
        Message = "Applying WebSite configuration | ResourceId: {ResourceId} | Url: {Url}")]
    public static partial void LogWebSiteApplyingConfig(
        this ILogger logger,
        string resourceId,
        string url);

    [LoggerMessage(
        EventId = 7081,
        Level = LogLevel.Information,
        Message = "WebSite configuration applied successfully | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteConfigAppliedSuccessfully(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7082,
        Level = LogLevel.Error,
        Message = "WebSite configuration apply failed | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteConfigApplyFailed(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7083,
        Level = LogLevel.Debug,
        Message = "Retrying apply operation | ResourceId: {ResourceId} | AttemptNumber: {AttemptNumber}")]
    public static partial void LogWebSiteRetryingApply(
        this ILogger logger,
        string resourceId,
        int attemptNumber);

    [LoggerMessage(
        EventId = 7084,
        Level = LogLevel.Information,
        Message = "Verifying applied configuration | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteVerifyingConfig(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7085,
        Level = LogLevel.Error,
        Message = "Failed to get web config after apply | ResourceId: {ResourceId} | Response was null")]
    public static partial void LogWebSiteGetConfigAfterApplyFailed(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7086,
        Level = LogLevel.Information,
        Message = "Configuration verified after apply | ResourceId: {ResourceId} | RulesAfterApply: {RulesAfterApply}")]
    public static partial void LogWebSiteConfigVerified(
        this ILogger logger,
        string resourceId,
        int rulesAfterApply);

    // ============================================================
    // Validation Operations (EventIds 7100-7119)
    // ============================================================

    [LoggerMessage(
        EventId = 7100,
        Level = LogLevel.Error,
        Message = "IP rules validation failed | ResourceId: {ResourceId} | ExpectedCount: {ExpectedCount} | ActualCount: {ActualCount}")]
    public static partial void LogWebSiteValidationFailed(
        this ILogger logger,
        string resourceId,
        int expectedCount,
        int actualCount);

    [LoggerMessage(
        EventId = 7101,
        Level = LogLevel.Error,
        Message = "Missing IP restrictions detected | ResourceId: {ResourceId} | MissingCount: {MissingCount}")]
    public static partial void LogWebSiteMissingRestrictions(
        this ILogger logger,
        string resourceId,
        int missingCount);

    [LoggerMessage(
        EventId = 7102,
        Level = LogLevel.Warning,
        Message = "Unexpected restrictions on resource | ResourceId: {ResourceId} | UnexpectedCount: {UnexpectedCount}")]
    public static partial void LogWebSiteUnexpectedRestrictions(
        this ILogger logger,
        string resourceId,
        int unexpectedCount);

    [LoggerMessage(
        EventId = 7103,
        Level = LogLevel.Information,
        Message = "IP rules validation passed | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogWebSiteValidationPassed(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 7104,
        Level = LogLevel.Error,
        Message = "No IP security restrictions found in web config after apply | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteNoRestrictionsAfterApply(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Limit Checks (EventIds 7120-7139)
    // ============================================================

    [LoggerMessage(
        EventId = 7120,
        Level = LogLevel.Warning,
        Message = "IP security restriction limit reached | ResourceId: {ResourceId} | RuleCount: {RuleCount} | Limit: {Limit}")]
    public static partial void LogWebSiteIpRuleLimitReached(
        this ILogger logger,
        string resourceId,
        int ruleCount,
        int limit);

    [LoggerMessage(
        EventId = 7121,
        Level = LogLevel.Warning,
        Message = "SCM IP security restriction limit reached | ResourceId: {ResourceId} | RuleCount: {RuleCount} | Limit: {Limit}")]
    public static partial void LogWebSiteScmRuleLimitReached(
        this ILogger logger,
        string resourceId,
        int ruleCount,
        int limit);

    [LoggerMessage(
        EventId = 7122,
        Level = LogLevel.Debug,
        Message = "Rule count within limits | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | ScmRuleCount: {ScmRuleCount} | Limit: {Limit}")]
    public static partial void LogWebSiteRuleCountWithinLimits(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int scmRuleCount,
        int limit);

    // ============================================================
    // Error Conditions (EventIds 7140-7159)
    // ============================================================

    [LoggerMessage(
        EventId = 7140,
        Level = LogLevel.Error,
        Message = "Apply operation failed after all retries | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteApplyOperationFailed(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 7141,
        Level = LogLevel.Error,
        Message = "Unable to update web config | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteUnableToUpdateConfig(
        this ILogger logger,
        Exception exception,
        string resourceId);

    [LoggerMessage(
        EventId = 7142,
        Level = LogLevel.Error,
        Message = "Web config GET request failed | ResourceId: {ResourceId}")]
    public static partial void LogWebSiteGetRequestFailed(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // Audit Logging - Before/After Comparisons (EventIds 7160-7179)
    // ============================================================

    [LoggerMessage(
        EventId = 7160,
        Level = LogLevel.Information,
        Message = "Existing IP rules before apply | ResourceId: {ResourceId} | Count: {Count}")]
    public static partial void LogWebSiteExistingRulesBeforeApply(
        this ILogger logger,
        string resourceId,
        int count);

    [LoggerMessage(
        EventId = 7161,
        Level = LogLevel.Information,
        Message = "Generated IP rules to apply | ResourceId: {ResourceId} | Count: {Count}")]
    public static partial void LogWebSiteGeneratedRulesToApply(
        this ILogger logger,
        string resourceId,
        int count);

    [LoggerMessage(
        EventId = 7162,
        Level = LogLevel.Information,
        Message = "Rules will be removed | ResourceId: {ResourceId} | RulesToRemoveCount: {Count}")]
    public static partial void LogWebSiteRulesToBeRemoved(
        this ILogger logger,
        string resourceId,
        int count);

    [LoggerMessage(
        EventId = 7163,
        Level = LogLevel.Information,
        Message = "Rules after apply | ResourceId: {ResourceId} | Count: {Count}")]
    public static partial void LogWebSiteRulesAfterApply(
        this ILogger logger,
        string resourceId,
        int count);

    [LoggerMessage(
        EventId = 7164,
        Level = LogLevel.Debug,
        Message = "Rule comparison details | ResourceId: {ResourceId} | Added: {AddedCount} | Removed: {RemovedCount} | Unchanged: {UnchangedCount}")]
    public static partial void LogWebSiteRuleComparisonDetails(
        this ILogger logger,
        string resourceId,
        int addedCount,
        int removedCount,
        int unchangedCount);

    // ============================================================
    // Scoped Logging Helper
    // ============================================================

    /// <summary>
    /// Creates a logging scope for WebSite operations.
    /// </summary>
    public static IDisposable? BeginWebSiteOperationScope(
        this ILogger logger,
        string operationName,
        string resourceId,
        string? resourceName = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ResourceType"] = "WebSite",
        ["OperationName"] = operationName,
        ["ResourceId"] = resourceId,
        ["ResourceName"] = resourceName ?? "Unknown",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}
