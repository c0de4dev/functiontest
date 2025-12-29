using DynamicAllowListingLib.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
    /// <summary>
    /// Additional structured logging extensions for DynamicAllowListingService operations.
    /// These supplement the existing DynamicAllowListingLoggerExtensions.cs with methods 
    /// identified as missing in the logging gap analysis.
    /// 
    /// EVENT ID RANGE: 4200-4299 (Additional DynamicAllowListingService methods)
    /// 
    /// Event ID Allocation:
    /// - 4200-4219: AzureResourceService creation and retrieval
    /// - 4220-4239: PrintOut mode operations
    /// - 4240-4259: Subnet validation operations
    /// - 4260-4279: Website slot operations
    /// - 4280-4299: Additional audit logging
    /// </summary>
    public static partial class DynamicAllowListingLoggerExtensionsAdditional
    {
        // ============================================================
        // AzureResourceService Creation and Retrieval (EventIds 4200-4219)
        // ============================================================

        [LoggerMessage(
            EventId = 4200,
            Level = LogLevel.Debug,
            Message = "Creating AzureResourceService instance | ResourceId: {ResourceId} | ResourceName: {ResourceName}")]
        public static partial void LogAzureResourceServiceCreating(
            this ILogger logger,
            string resourceId,
            string resourceName);

        [LoggerMessage(
            EventId = 4201,
            Level = LogLevel.Information,
            Message = "Azure resource retrieved | ResourceId: {ResourceId} | ResourceType: {ResourceType} | Found: {Found}")]
        public static partial void LogAzureResourceRetrieved(
            this ILogger logger,
            string resourceId,
            string resourceType,
            bool found);

        [LoggerMessage(
            EventId = 4202,
            Level = LogLevel.Debug,
            Message = "AzureResourceService instance created | ResourceId: {ResourceId}")]
        public static partial void LogAzureResourceServiceCreated(
            this ILogger logger,
            string resourceId);

        [LoggerMessage(
            EventId = 4203,
            Level = LogLevel.Warning,
            Message = "Main Azure resource not found | ResourceId: {ResourceId} | PrintOutMode: {PrintOutMode}")]
        public static partial void LogMainResourceNotFound(
            this ILogger logger,
            string resourceId,
            bool printOutMode);

        // ============================================================
        // PrintOut Mode Operations (EventIds 4220-4239)
        // ============================================================

        [LoggerMessage(
            EventId = 4220,
            Level = LogLevel.Information,
            Message = "PrintOut mode parsing | ResourceId: {ResourceId} | RawValue: {RawValue} | ParsedValue: {ParsedValue}")]
        public static partial void LogPrintOutModeParsed(
            this ILogger logger,
            string resourceId,
            string rawValue,
            bool parsedValue);

        [LoggerMessage(
            EventId = 4221,
            Level = LogLevel.Information,
            Message = "PrintOut mode enabled | ResourceId: {ResourceId} | Skipping actual apply")]
        public static partial void LogPrintOutModeEnabled(
            this ILogger logger,
            string resourceId);

        [LoggerMessage(
            EventId = 4222,
            Level = LogLevel.Warning,
            Message = "PrintOut mode not supported for resource type | ResourceId: {ResourceId} | ResourceType: {ResourceType}")]
        public static partial void LogPrintOutModeNotSupported(
            this ILogger logger,
            string resourceId,
            string resourceType);

        [LoggerMessage(
            EventId = 4223,
            Level = LogLevel.Information,
            Message = "PrintOut output generated | ResourceId: {ResourceId} | IpCount: {IpCount} | SubnetCount: {SubnetCount}")]
        public static partial void LogPrintOutOutputGenerated(
            this ILogger logger,
            string resourceId,
            int ipCount,
            int subnetCount);

        // ============================================================
        // Subnet Validation Operations (EventIds 4240-4259)
        // ============================================================

        [LoggerMessage(
            EventId = 4240,
            Level = LogLevel.Information,
            Message = "Starting subnet validation | ResourceId: {ResourceId} | SubnetCount: {SubnetCount}")]
        public static partial void LogSubnetValidationStart(
            this ILogger logger,
            string resourceId,
            int subnetCount);

        [LoggerMessage(
            EventId = 4241,
            Level = LogLevel.Information,
            Message = "Subnet validation completed | ResourceId: {ResourceId} | ValidSubnets: {ValidCount} | RemovedSubnets: {RemovedCount}")]
        public static partial void LogSubnetValidationComplete(
            this ILogger logger,
            string resourceId,
            int validCount,
            int removedCount);

        [LoggerMessage(
            EventId = 4243,
            Level = LogLevel.Debug,
            Message = "Subnet validated successfully | ResourceId: {ResourceId} | SubnetId: {SubnetId}")]
        public static partial void LogSubnetValidated(
            this ILogger logger,
            string resourceId,
            string subnetId);

        [LoggerMessage(
            EventId = 4244,
            Level = LogLevel.Debug,
            Message = "Cross-subscription subnet allowed | ResourceId: {ResourceId} | SubnetId: {SubnetId}")]
        public static partial void LogCrossSubscriptionSubnetAllowed(
            this ILogger logger,
            string resourceId,
            string subnetId);

        [LoggerMessage(
            EventId = 4245,
            Level = LogLevel.Warning,
            Message = "Subnet does not exist in Azure | SubnetId: {SubnetId} | SubscriptionId: {SubscriptionId}")]
        public static partial void LogSubnetDoesNotExist(
            this ILogger logger,
            string subnetId,
            string subscriptionId);

        // ============================================================
        // Website Slot Operations (EventIds 4260-4279)
        // ============================================================

        [LoggerMessage(
            EventId = 4260,
            Level = LogLevel.Information,
            Message = "Checking for website slot | ResourceId: {ResourceId}")]
        public static partial void LogCheckingForWebsiteSlot(
            this ILogger logger,
            string resourceId);

        [LoggerMessage(
            EventId = 4261,
            Level = LogLevel.Information,
            Message = "Website slot found | MainResourceId: {MainResourceId} | SlotId: {SlotId}")]
        public static partial void LogWebsiteSlotFound(
            this ILogger logger,
            string mainResourceId,
            string slotId);

        [LoggerMessage(
            EventId = 4262,
            Level = LogLevel.Debug,
            Message = "No website slot found | ResourceId: {ResourceId} | Skipping slot processing")]
        public static partial void LogNoWebsiteSlotFound(
            this ILogger logger,
            string resourceId);

        [LoggerMessage(
            EventId = 4263,
            Level = LogLevel.Information,
            Message = "Processing website slot restrictions | SlotId: {SlotId} | MainResourceId: {MainResourceId}")]
        public static partial void LogProcessingWebsiteSlot(
            this ILogger logger,
            string slotId,
            string mainResourceId);

        [LoggerMessage(
            EventId = 4264,
            Level = LogLevel.Information,
            Message = "Website slot restrictions applied | SlotId: {SlotId} | Success: {Success}")]
        public static partial void LogWebsiteSlotRestrictionsApplied(
            this ILogger logger,
            string slotId,
            bool success);

        [LoggerMessage(
            EventId = 4265,
            Level = LogLevel.Error,
            Message = "Failed to apply website slot restrictions | SlotId: {SlotId}")]
        public static partial void LogWebsiteSlotRestrictionsFailed(
            this ILogger logger,
            Exception exception,
            string slotId);

        [LoggerMessage(
            EventId = 4266,
            Level = LogLevel.Debug,
            Message = "Resource is not a WebSite | ResourceId: {ResourceId} | ResourceType: {ResourceType} | Skipping slot check")]
        public static partial void LogNotWebSiteSkippingSlotCheck(
            this ILogger logger,
            string resourceId,
            string resourceType);

        // ============================================================
        // Additional Audit Logging (EventIds 4280-4299)
        // ============================================================

        [LoggerMessage(
            EventId = 4280,
            Level = LogLevel.Information,
            Message = "Network restriction rules retrieved | ResourceId: {ResourceId} | IpRuleCount: {IpRuleCount} | ScmRuleCount: {ScmRuleCount} | SubnetCount: {SubnetCount}")]
        public static partial void LogNetworkRestrictionRulesRetrieved(
            this ILogger logger,
            string resourceId,
            int ipRuleCount,
            int scmRuleCount,
            int subnetCount);

        [LoggerMessage(
            EventId = 4281,
            Level = LogLevel.Information,
            Message = "Overwrite operation summary | ResourceId: {ResourceId} | TotalRulesApplied: {TotalRules} | Success: {Success} | Warnings: {WarningCount} | Errors: {ErrorCount}")]
        public static partial void LogOverwriteOperationSummary(
            this ILogger logger,
            string resourceId,
            int totalRules,
            bool success,
            int warningCount,
            int errorCount);

        [LoggerMessage(
            EventId = 4282,
            Level = LogLevel.Debug,
            Message = "Merging result objects | CurrentErrors: {CurrentErrors} | NewErrors: {NewErrors} | CurrentWarnings: {CurrentWarnings} | NewWarnings: {NewWarnings}")]
        public static partial void LogMergingResultObjects(
            this ILogger logger,
            int currentErrors,
            int newErrors,
            int currentWarnings,
            int newWarnings);

        [LoggerMessage(
            EventId = 4283,
            Level = LogLevel.Information,
            Message = "Resource dependency information | ResourceId: {ResourceId} | HasInbound: {HasInbound} | HasOutbound: {HasOutbound} | HasServiceTags: {HasServiceTags}")]
        public static partial void LogResourceDependencyInfo(
            this ILogger logger,
            string resourceId,
            bool hasInbound,
            bool hasOutbound,
            bool hasServiceTags);

        // ============================================================
        // Scoped Logging Helper for Additional Operations
        // ============================================================

        /// <summary>
        /// Creates a logging scope for subnet validation operations.
        /// </summary>
        public static IDisposable? BeginSubnetValidationScope(
            this ILogger logger,
            string resourceId,
            int subnetCount)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["OperationType"] = "SubnetValidation",
                ["ResourceId"] = resourceId,
                ["SubnetCount"] = subnetCount,
                ["CorrelationId"] = CorrelationContext.CorrelationId,
                ["Timestamp"] = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Creates a logging scope for website slot operations.
        /// </summary>
        public static IDisposable? BeginWebsiteSlotScope(
            this ILogger logger,
            string mainResourceId,
            string? slotId)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["OperationType"] = "WebsiteSlotProcessing",
                ["MainResourceId"] = mainResourceId,
                ["SlotId"] = slotId ?? "None",
                ["CorrelationId"] = CorrelationContext.CorrelationId,
                ["Timestamp"] = DateTimeOffset.UtcNow
            });
        }
    }
}
