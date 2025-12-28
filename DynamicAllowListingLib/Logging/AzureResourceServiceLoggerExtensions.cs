using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for AzureResourceService operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// 
  /// EVENT ID RANGE: 3000-3199
  /// 
  /// Event ID Allocation:
  /// - 3000-3019: Service Lifecycle & Factory
  /// - 3020-3039: GetAzureResources / GetAzureResource
  /// - 3040-3059: GetUpdateNetworkRestrictionSettingsForMainResource
  /// - 3060-3079: GenerateIpSecRules
  /// - 3080-3099: GenerateScmIpSecRules
  /// - 3100-3119: GetAppendNetworkRestrictionSettings
  /// - 3120-3139: GetResourceRules
  /// - 3140-3159: Service Tag Operations
  /// - 3160-3179: Subnet Operations
  /// - 3180-3199: Cache & Performance
  /// </summary>
  public static partial class AzureResourceServiceLoggerExtensions
  {
    // ============================================================
    // Service Lifecycle & Factory (EventIds 3000-3019)
    // ============================================================

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "AzureResourceService created | ResourceId: {ResourceId} | ResourceName: {ResourceName} | ResourceType: {ResourceType}")]
    public static partial void LogServiceCreated(
        this ILogger logger,
        string resourceId,
        string resourceName,
        string resourceType);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Debug,
        Message = "AzureResourceService disposed | ResourceId: {ResourceId}")]
    public static partial void LogServiceDisposed(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Method started | Method: {MethodName} | ResourceId: {ResourceId}")]
    public static partial void LogMethodStarted(
        this ILogger logger,
        string methodName,
        string resourceId);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "Method completed | Method: {MethodName} | ResourceId: {ResourceId} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogMethodCompleted(
        this ILogger logger,
        string methodName,
        string resourceId,
        long durationMs,
        bool success);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Error,
        Message = "Method failed | Method: {MethodName} | ResourceId: {ResourceId}")]
    public static partial void LogMethodFailed(
        this ILogger logger,
        Exception exception,
        string methodName,
        string resourceId);

    // ============================================================
    // GetAzureResources / GetAzureResource (EventIds 3020-3039)
    // ============================================================

    [LoggerMessage(
        EventId = 3020,
        Level = LogLevel.Information,
        Message = "GetAzureResources started | ResourceId: {ResourceId} | ResourceCount: {ResourceCount}")]
    public static partial void LogGetAzureResourcesStart(
        this ILogger logger,
        string resourceId,
        int resourceCount);

    [LoggerMessage(
        EventId = 3021,
        Level = LogLevel.Information,
        Message = "GetAzureResources cache hit | ResourceId: {ResourceId} | CachedCount: {CachedCount}")]
    public static partial void LogGetAzureResourcesCacheHit(
        this ILogger logger,
        string resourceId,
        int cachedCount);

    [LoggerMessage(
        EventId = 3022,
        Level = LogLevel.Information,
        Message = "GetAzureResources cache miss, fetching from Azure | ResourceId: {ResourceId}")]
    public static partial void LogGetAzureResourcesCacheMiss(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3023,
        Level = LogLevel.Information,
        Message = "GetAzureResources completed | ResourceId: {ResourceId} | ResourcesFound: {ResourcesFound} | Duration: {DurationMs}ms")]
    public static partial void LogGetAzureResourcesComplete(
        this ILogger logger,
        string resourceId,
        int resourcesFound,
        long durationMs);

    [LoggerMessage(
        EventId = 3024,
        Level = LogLevel.Information,
        Message = "GetAzureResource started | TargetResourceId: {TargetResourceId}")]
    public static partial void LogGetAzureResourceStart(
        this ILogger logger,
        string targetResourceId);

    [LoggerMessage(
        EventId = 3025,
        Level = LogLevel.Information,
        Message = "GetAzureResource found | ResourceId: {ResourceId} | ResourceName: {ResourceName} | ResourceType: {ResourceType}")]
    public static partial void LogGetAzureResourceFound(
        this ILogger logger,
        string resourceId,
        string resourceName,
        string resourceType);

    [LoggerMessage(
        EventId = 3026,
        Level = LogLevel.Warning,
        Message = "GetAzureResource not found | ResourceId: {ResourceId}")]
    public static partial void LogGetAzureResourceNotFound(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3027,
        Level = LogLevel.Information,
        Message = "Initializing Azure resources | MainResourceId: {MainResourceId} | InboundResourceCount: {InboundCount} | OutboundResourceCount: {OutboundCount}")]
    public static partial void LogInitializingAzureResources(
        this ILogger logger,
        string mainResourceId,
        int inboundCount,
        int outboundCount);

    [LoggerMessage(
        EventId = 3028,
        Level = LogLevel.Information,
        Message = "Azure resources initialized | MainResourceId: {MainResourceId} | TotalResources: {TotalResources} | Duration: {DurationMs}ms")]
    public static partial void LogAzureResourcesInitialized(
        this ILogger logger,
        string mainResourceId,
        int totalResources,
        long durationMs);

    [LoggerMessage(
        EventId = 3029,
        Level = LogLevel.Warning,
        Message = "Resource ID is null or empty | Context: {Context}")]
    public static partial void LogResourceIdNullOrEmpty(
        this ILogger logger,
        string context);

    // ============================================================
    // GetUpdateNetworkRestrictionSettingsForMainResource (EventIds 3040-3059)
    // ============================================================

    [LoggerMessage(
        EventId = 3040,
        Level = LogLevel.Information,
        Message = "GetUpdateNetworkRestrictionSettingsForMainResource started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogGetUpdateSettingsStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3041,
        Level = LogLevel.Information,
        Message = "Generating IP security rules for inbound configurations | ResourceName: {ResourceName}")]
    public static partial void LogGeneratingInboundRules(
        this ILogger logger,
        string resourceName);

    [LoggerMessage(
        EventId = 3042,
        Level = LogLevel.Information,
        Message = "Fetching resources where main resource is outbound | ResourceId: {ResourceId}")]
    public static partial void LogFetchingOutboundResources(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3043,
        Level = LogLevel.Information,
        Message = "Found resources where main resource is outbound | ResourceId: {ResourceId} | OutboundCount: {OutboundCount}")]
    public static partial void LogOutboundResourcesFound(
        this ILogger logger,
        string resourceId,
        int outboundCount);

    [LoggerMessage(
        EventId = 3044,
        Level = LogLevel.Information,
        Message = "Generating rules for outbound resources | ResourceName: {ResourceName} | OutboundCount: {OutboundCount}")]
    public static partial void LogGeneratingOutboundRules(
        this ILogger logger,
        string resourceName,
        int outboundCount);

    [LoggerMessage(
        EventId = 3045,
        Level = LogLevel.Information,
        Message = "Generated outbound rules | ResourceName: {ResourceName} | RuleCount: {RuleCount}")]
    public static partial void LogOutboundRulesGenerated(
        this ILogger logger,
        string resourceName,
        int ruleCount);

    [LoggerMessage(
        EventId = 3046,
        Level = LogLevel.Information,
        Message = "GetUpdateNetworkRestrictionSettingsForMainResource completed | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | ScmRuleCount: {ScmRuleCount} | Duration: {DurationMs}ms")]
    public static partial void LogGetUpdateSettingsComplete(
        this ILogger logger,
        string resourceId,
        int ipRuleCount,
        int scmRuleCount,
        long durationMs);

    [LoggerMessage(
        EventId = 3047,
        Level = LogLevel.Warning,
        Message = "Skipping outbound rules generation - ResourceId is null or empty | ResourceName: {ResourceName}")]
    public static partial void LogSkippingOutboundRulesNullResourceId(
        this ILogger logger,
        string resourceName);

    // ============================================================
    // GenerateIpSecRules (EventIds 3060-3079)
    // ============================================================

    [LoggerMessage(
        EventId = 3060,
        Level = LogLevel.Information,
        Message = "GenerateIpSecRules started | ResourceId: {ResourceId}")]
    public static partial void LogGenerateIpSecRulesStart(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3061,
        Level = LogLevel.Information,
        Message = "Processing inbound security restrictions | ResourceId: {ResourceId} | ResourceIdCount: {ResourceIdCount}")]
    public static partial void LogProcessingInboundSecurityRestrictions(
        this ILogger logger,
        string resourceId,
        int resourceIdCount);

    [LoggerMessage(
        EventId = 3062,
        Level = LogLevel.Information,
        Message = "Generated FrontDoor service tag | ResourceId: {ResourceId} | HasFrontDoorTag: {HasTag}")]
    public static partial void LogFrontDoorServiceTagGenerated(
        this ILogger logger,
        string resourceId,
        bool hasTag);

    [LoggerMessage(
        EventId = 3063,
        Level = LogLevel.Information,
        Message = "Processing subnet rules | ResourceId: {ResourceId} | SubnetCount: {SubnetCount}")]
    public static partial void LogProcessingSubnetRules(
        this ILogger logger,
        string resourceId,
        int subnetCount);

    [LoggerMessage(
        EventId = 3064,
        Level = LogLevel.Information,
        Message = "Processing Azure service tags | ResourceId: {ResourceId} | TagCount: {TagCount} | Tags: {Tags}")]
    public static partial void LogProcessingAzureServiceTags(
        this ILogger logger,
        string resourceId,
        int tagCount,
        string tags);

    [LoggerMessage(
        EventId = 3065,
        Level = LogLevel.Information,
        Message = "Processing Azure web service tags | ResourceId: {ResourceId} | TagCount: {TagCount} | Tags: {Tags}")]
    public static partial void LogProcessingAzureWebServiceTags(
        this ILogger logger,
        string resourceId,
        int tagCount,
        string tags);

    [LoggerMessage(
        EventId = 3066,
        Level = LogLevel.Information,
        Message = "Processing NewDay internal and third-party tags | ResourceId: {ResourceId} | TagCount: {TagCount} | Tags: {Tags}")]
    public static partial void LogProcessingNewDayTags(
        this ILogger logger,
        string resourceId,
        int tagCount,
        string tags);

    [LoggerMessage(
        EventId = 3067,
        Level = LogLevel.Information,
        Message = "Processing allowed IPs | ResourceId: {ResourceId} | IpCount: {IpCount}")]
    public static partial void LogProcessingAllowedIPs(
        this ILogger logger,
        string resourceId,
        int ipCount);

    [LoggerMessage(
        EventId = 3068,
        Level = LogLevel.Information,
        Message = "GenerateIpSecRules completed | ResourceId: {ResourceId} | TotalRulesGenerated: {TotalRules} | Duration: {DurationMs}ms")]
    public static partial void LogGenerateIpSecRulesComplete(
        this ILogger logger,
        string resourceId,
        int totalRules,
        long durationMs);

    [LoggerMessage(
        EventId = 3069,
        Level = LogLevel.Debug,
        Message = "IP security rule source breakdown | ResourceId: {ResourceId} | FromResources: {FromResources} | FromSubnets: {FromSubnets} | FromServiceTags: {FromServiceTags} | FromAllowedIPs: {FromAllowedIPs}")]
    public static partial void LogIpSecRuleBreakdown(
        this ILogger logger,
        string resourceId,
        int fromResources,
        int fromSubnets,
        int fromServiceTags,
        int fromAllowedIPs);

    // ============================================================
    // GenerateScmIpSecRules (EventIds 3080-3099)
    // ============================================================

    [LoggerMessage(
        EventId = 3080,
        Level = LogLevel.Information,
        Message = "GenerateScmIpSecRules started | ResourceId: {ResourceId}")]
    public static partial void LogGenerateScmIpSecRulesStart(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3081,
        Level = LogLevel.Information,
        Message = "Processing SCM security restrictions | ResourceId: {ResourceId} | HasScmRestrictions: {HasRestrictions}")]
    public static partial void LogProcessingScmRestrictions(
        this ILogger logger,
        string resourceId,
        bool hasRestrictions);

    [LoggerMessage(
        EventId = 3082,
        Level = LogLevel.Information,
        Message = "GenerateScmIpSecRules completed | ResourceId: {ResourceId} | TotalScmRules: {TotalRules} | Duration: {DurationMs}ms")]
    public static partial void LogGenerateScmIpSecRulesComplete(
        this ILogger logger,
        string resourceId,
        int totalRules,
        long durationMs);

    [LoggerMessage(
        EventId = 3083,
        Level = LogLevel.Debug,
        Message = "SCM uses same rules as main | ResourceId: {ResourceId} | ScmUseMain: {ScmUseMain}")]
    public static partial void LogScmUseMainRules(
        this ILogger logger,
        string resourceId,
        bool scmUseMain);

    // ============================================================
    // GetAppendNetworkRestrictionSettings (EventIds 3100-3119)
    // ============================================================

    [LoggerMessage(
        EventId = 3100,
        Level = LogLevel.Information,
        Message = "GetAppendNetworkRestrictionSettings started | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
    public static partial void LogGetAppendSettingsStart(
        this ILogger logger,
        string resourceId,
        string resourceName);

    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Identifying resources to apply rules to | SourceResourceId: {SourceResourceId} | TargetResourceCount: {TargetCount}")]
    public static partial void LogIdentifyingTargetResources(
        this ILogger logger,
        string sourceResourceId,
        int targetCount);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Information,
        Message = "Generating rules for source resource | SourceResourceId: {SourceResourceId}")]
    public static partial void LogGeneratingRulesForSource(
        this ILogger logger,
        string sourceResourceId);

    [LoggerMessage(
        EventId = 3103,
        Level = LogLevel.Information,
        Message = "Created network restriction settings for target | TargetResourceId: {TargetResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogCreatedSettingsForTarget(
        this ILogger logger,
        string targetResourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 3104,
        Level = LogLevel.Information,
        Message = "GetAppendNetworkRestrictionSettings completed | ResourceId: {ResourceId} | TargetResourceCount: {TargetCount} | TotalRulesGenerated: {TotalRules} | Duration: {DurationMs}ms")]
    public static partial void LogGetAppendSettingsComplete(
        this ILogger logger,
        string resourceId,
        int targetCount,
        int totalRules,
        long durationMs);

    [LoggerMessage(
        EventId = 3105,
        Level = LogLevel.Warning,
        Message = "No outbound resources configured | ResourceId: {ResourceId}")]
    public static partial void LogNoOutboundResourcesConfigured(
        this ILogger logger,
        string resourceId);

    // ============================================================
    // GetResourceRules (EventIds 3120-3139)
    // ============================================================

    [LoggerMessage(
        EventId = 3120,
        Level = LogLevel.Information,
        Message = "GetResourceRules started | ResourceCount: {ResourceCount}")]
    public static partial void LogGetResourceRulesStart(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 3121,
        Level = LogLevel.Warning,
        Message = "No resource IDs provided to GetResourceRules")]
    public static partial void LogNoResourceIdsForRules(
        this ILogger logger);

    [LoggerMessage(
        EventId = 3122,
        Level = LogLevel.Information,
        Message = "Fetching Azure resources for rule generation | ResourceIds: {ResourceIds}")]
    public static partial void LogFetchingResourcesForRules(
        this ILogger logger,
        string resourceIds);

    [LoggerMessage(
        EventId = 3123,
        Level = LogLevel.Information,
        Message = "Extracting subscription IDs | SubscriptionCount: {SubscriptionCount}")]
    public static partial void LogExtractingSubscriptionIds(
        this ILogger logger,
        int subscriptionCount);

    [LoggerMessage(
        EventId = 3124,
        Level = LogLevel.Information,
        Message = "Fetching subnet IDs | SubscriptionCount: {SubscriptionCount}")]
    public static partial void LogFetchingSubnetIds(
        this ILogger logger,
        int subscriptionCount);

    [LoggerMessage(
        EventId = 3125,
        Level = LogLevel.Information,
        Message = "Subnet IDs retrieved | TotalSubnets: {TotalSubnets}")]
    public static partial void LogSubnetIdsRetrieved(
        this ILogger logger,
        int totalSubnets);

    [LoggerMessage(
        EventId = 3126,
        Level = LogLevel.Information,
        Message = "Filtering valid subnet IDs | ValidSubnetCount: {ValidCount} | TotalSubnetCount: {TotalCount}")]
    public static partial void LogFilteringValidSubnets(
        this ILogger logger,
        int validCount,
        int totalCount);

    [LoggerMessage(
        EventId = 3127,
        Level = LogLevel.Information,
        Message = "Generating IP restriction rules | AzureResourceCount: {ResourceCount} | SubnetCount: {SubnetCount}")]
    public static partial void LogGeneratingIpRestrictionRules(
        this ILogger logger,
        int resourceCount,
        int subnetCount);

    [LoggerMessage(
        EventId = 3128,
        Level = LogLevel.Information,
        Message = "GetResourceRules completed | RulesGenerated: {RulesGenerated} | Duration: {DurationMs}ms")]
    public static partial void LogGetResourceRulesComplete(
        this ILogger logger,
        int rulesGenerated,
        long durationMs);

    [LoggerMessage(
        EventId = 3129,
        Level = LogLevel.Warning,
        Message = "VNet subnet integration warning | Message: {Message}")]
    public static partial void LogVNetSubnetIntegrationWarning(
        this ILogger logger,
        string message);

    // ============================================================
    // Service Tag Operations (EventIds 3140-3159)
    // ============================================================

    [LoggerMessage(
        EventId = 3140,
        Level = LogLevel.Information,
        Message = "Getting Azure service tag rules | ResourceId: {ResourceId} | TagCount: {TagCount}")]
    public static partial void LogGettingAzureServiceTagRules(
        this ILogger logger,
        string resourceId,
        int tagCount);

    [LoggerMessage(
        EventId = 3141,
        Level = LogLevel.Information,
        Message = "Azure service tag rules generated | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogAzureServiceTagRulesGenerated(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 3142,
        Level = LogLevel.Warning,
        Message = "No service tags provided | ResourceId: {ResourceId}")]
    public static partial void LogNoServiceTagsProvided(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 3143,
        Level = LogLevel.Debug,
        Message = "Using service tag manager | ManagerType: {ManagerType} | ResourceType: {ResourceType}")]
    public static partial void LogUsingServiceTagManager(
        this ILogger logger,
        string managerType,
        string resourceType);

    [LoggerMessage(
        EventId = 3144,
        Level = LogLevel.Information,
        Message = "Getting NewDay internal tag rules | ResourceId: {ResourceId} | TagCount: {TagCount}")]
    public static partial void LogGettingNewDayTagRules(
        this ILogger logger,
        string resourceId,
        int tagCount);

    [LoggerMessage(
        EventId = 3145,
        Level = LogLevel.Information,
        Message = "NewDay internal tag rules generated | ResourceId: {ResourceId} | RuleCount: {RuleCount}")]
    public static partial void LogNewDayTagRulesGenerated(
        this ILogger logger,
        string resourceId,
        int ruleCount);

    [LoggerMessage(
        EventId = 3146,
        Level = LogLevel.Information,
        Message = "Generating FrontDoor service tag | ResourceIds: {ResourceIds}")]
    public static partial void LogGeneratingFrontDoorTag(
        this ILogger logger,
        string resourceIds);

    [LoggerMessage(
        EventId = 3147,
        Level = LogLevel.Information,
        Message = "FrontDoor service tag generated | RuleCount: {RuleCount} | HttpHeaderFilters: {FilterCount}")]
    public static partial void LogFrontDoorTagGenerated(
        this ILogger logger,
        int ruleCount,
        int filterCount);

    // ============================================================
    // Subnet Operations (EventIds 3160-3179)
    // ============================================================

    [LoggerMessage(
        EventId = 3160,
        Level = LogLevel.Information,
        Message = "Getting all subnet IDs | SubscriptionIds: {SubscriptionIds}")]
    public static partial void LogGettingAllSubnetIds(
        this ILogger logger,
        string subscriptionIds);

    [LoggerMessage(
        EventId = 3161,
        Level = LogLevel.Information,
        Message = "All subnet IDs retrieved | TotalCount: {TotalCount} | Duration: {DurationMs}ms")]
    public static partial void LogAllSubnetIdsRetrieved(
        this ILogger logger,
        int totalCount,
        long durationMs);

    [LoggerMessage(
        EventId = 3162,
        Level = LogLevel.Debug,
        Message = "Generating subnet rule | SubnetId: {SubnetId} | RuleName: {RuleName}")]
    public static partial void LogGeneratingSubnetRule(
        this ILogger logger,
        string subnetId,
        string ruleName);

    [LoggerMessage(
        EventId = 3163,
        Level = LogLevel.Warning,
        Message = "Invalid subnet ID skipped | SubnetId: {SubnetId} | Reason: {Reason}")]
    public static partial void LogInvalidSubnetSkipped(
        this ILogger logger,
        string subnetId,
        string reason);

    // ============================================================
    // Cache & Performance (EventIds 3180-3199)
    // ============================================================

    [LoggerMessage(
        EventId = 3180,
        Level = LogLevel.Debug,
        Message = "Caching Azure resources | ResourceId: {ResourceId} | CachedCount: {CachedCount}")]
    public static partial void LogCachingAzureResources(
        this ILogger logger,
        string resourceId,
        int cachedCount);

    [LoggerMessage(
        EventId = 3181,
        Level = LogLevel.Debug,
        Message = "Caching subnet IDs | SubscriptionId: {SubscriptionId} | CachedCount: {CachedCount}")]
    public static partial void LogCachingSubnetIds(
        this ILogger logger,
        string subscriptionId,
        int cachedCount);

    [LoggerMessage(
        EventId = 3182,
        Level = LogLevel.Warning,
        Message = "Slow operation detected | Operation: {Operation} | Duration: {DurationMs}ms | Threshold: {ThresholdMs}ms")]
    public static partial void LogSlowOperationDetected(
        this ILogger logger,
        string operation,
        long durationMs,
        long thresholdMs);

    [LoggerMessage(
        EventId = 3183,
        Level = LogLevel.Debug,
        Message = "Performance tracking | Operation: {Operation} | StartTimestamp: {StartTimestamp}")]
    public static partial void LogPerformanceTracking(
        this ILogger logger,
        string operation,
        long startTimestamp);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for AzureResourceService operations.
    /// </summary>
    public static IDisposable? BeginAzureResourceServiceScope(
        this ILogger logger,
        string methodName,
        string? resourceId = null,
        string? resourceName = null)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["ServiceName"] = "AzureResourceService",
        ["MethodName"] = methodName,
        ["ResourceId"] = resourceId ?? "N/A",
        ["ResourceName"] = resourceName ?? "N/A",
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for rule generation operations.
    /// </summary>
    public static IDisposable? BeginRuleGenerationScope(
        this ILogger logger,
        string operationType,
        string resourceId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["OperationType"] = "RuleGeneration",
        ["RuleType"] = operationType,
        ["ResourceId"] = resourceId,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      });
    }
  }
}