using Microsoft.Extensions.Logging;
using System;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Settings Validation operations.
  /// Uses LoggerMessage source generators for optimal performance.
  /// EVENT ID RANGE: 11000-11549
  /// 
  /// EventId Allocation:
  /// - 11000-11199: InternalAndThirdPartyServiceTagValidator
  /// - 11200-11399: ResourceDependencyInformationValidator
  /// - 11520-11549: JSON Parsing and Validation Summary
  /// - 10115-10119: AzureSubscriptionsPersistenceManager
  /// - 10170-10177: ServiceTagsPersistenceManager
  /// - 10210-10214: InternalAndThirdPartyServiceTagPersistenceManager
  /// </summary>
  public static partial class ValidatorLoggerExtensions
  {
    // ============================================================
    // InternalAndThirdPartyServiceTagValidator (EventIds 11000-11199)
    // ============================================================

    #region Validate Method (11000-11009)

    [LoggerMessage(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Validating Azure Subscriptions Parameters")]
    public static partial void LogValidatingAzureSubscriptionParameters(this ILogger logger);

    [LoggerMessage(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Validating Service Tag IP Addresses")]
    public static partial void LogValidatingServiceTagIPAddresses(this ILogger logger);

    [LoggerMessage(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Validating Subscriptions Tag IP Addresses")]
    public static partial void LogValidatingAllowedSubscriptionTags(this ILogger logger);

    [LoggerMessage(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Validating Overlapping IP Addresses")]
    public static partial void LogValidatingOverlappingIPAddresses(this ILogger logger);

    [LoggerMessage(
        EventId = 11004,
        Level = LogLevel.Error,
        Message = "Exception occurred during validation | Method: {MethodName}")]
    public static partial void LogValidationException(
        this ILogger logger,
        Exception exception,
        string methodName);

    /// <summary>
    /// Overload for logging validation exception without method name.
    /// Calls the main LogValidationException with "Unknown" as method name.
    /// </summary>
    public static void LogValidationException(this ILogger logger, Exception exception)
    {
      LogValidationException(logger, exception, "Unknown");
    }

    [LoggerMessage(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Validation stage completed | Stage: {StageName} | Errors: {ErrorCount} | Warnings: {WarningCount}")]
    public static partial void LogValidationStageCompleted(
        this ILogger logger,
        string stageName,
        int errorCount,
        int warningCount);

    #endregion

    #region ValidateAddressRangeOverlapping (11010-11029)

    [LoggerMessage(
        EventId = 11010,
        Level = LogLevel.Warning,
        Message = "No ServiceTags provided in the settings.")]
    public static partial void LogNoServiceTagsInSettings(this ILogger logger);

    [LoggerMessage(
        EventId = 11011,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no AddressPrefixes defined.")]
    public static partial void LogServiceTagNoAddressPrefixes(
        this ILogger logger,
        string serviceTagName);

    [LoggerMessage(
        EventId = 11012,
        Level = LogLevel.Warning,
        Message = "Overlapping 'ServiceTags.AddressPrefixes' detected. ServiceTags.Name: {ServiceTagName}, AddressPrefix: {AddressPrefix}")]
    public static partial void LogOverlappingAddressPrefixDetected(
        this ILogger logger,
        string serviceTagName,
        string addressPrefix);

    [LoggerMessage(
        EventId = 11013,
        Level = LogLevel.Warning,
        Message = "Invalid or null AddressPrefix '{AddressPrefix}' in ServiceTag '{ServiceTagName}'.")]
    public static partial void LogInvalidAddressPrefix(
        this ILogger logger,
        string? addressPrefix,
        string serviceTagName);

    [LoggerMessage(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Address range validation completed with {WarningCount} warnings.")]
    public static partial void LogAddressRangeValidationWithWarnings(
        this ILogger logger,
        int warningCount);

    [LoggerMessage(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Address range validation completed successfully with no overlapping ranges.")]
    public static partial void LogAddressRangeValidationSuccess(this ILogger logger);

    [LoggerMessage(
        EventId = 11016,
        Level = LogLevel.Debug,
        Message = "Processing service tag for overlap validation | ServiceTag: {ServiceTagName} | AddressPrefixCount: {AddressCount}")]
    public static partial void LogProcessingServiceTagForOverlap(
        this ILogger logger,
        string serviceTagName,
        int addressCount);

    [LoggerMessage(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Address prefix tracked (no overlap) | ServiceTag: {ServiceTagName} | AddressPrefix: {AddressPrefix}")]
    public static partial void LogAddressPrefixTracked(
        this ILogger logger,
        string serviceTagName,
        string addressPrefix);

    [LoggerMessage(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Address overlap validation summary | TotalAddressesProcessed: {TotalProcessed} | OverlapsDetected: {OverlapsFound} | ServiceTagsProcessed: {TagsProcessed}")]
    public static partial void LogAddressOverlapSummary(
        this ILogger logger,
        int totalProcessed,
        int overlapsFound,
        int tagsProcessed);

    [LoggerMessage(
        EventId = 11019,
        Level = LogLevel.Warning,
        Message = "IP range overlap check failed: null or empty input | IpRange1: {IpRange1} | IpRange2: {IpRange2}")]
    public static partial void LogIPRangeOverlapNullInput(
        this ILogger logger,
        string? ipRange1,
        string? ipRange2);

    [LoggerMessage(
        EventId = 11020,
        Level = LogLevel.Error,
        Message = "Invalid CIDR format in overlap check | InvalidRange: {InvalidRange}")]
    public static partial void LogIPRangeOverlapInvalidCIDR(
        this ILogger logger,
        string invalidRange);

    [LoggerMessage(
        EventId = 11021,
        Level = LogLevel.Debug,
        Message = "IP range overlap check | Range1: {IpRange1} | Range2: {IpRange2} | Overlaps: {Overlaps}")]
    public static partial void LogIPRangeOverlapResult(
        this ILogger logger,
        string ipRange1,
        string ipRange2,
        bool overlaps);

    #endregion

    #region ValidateAllowedSubscriptionTags (11030-11049)

    [LoggerMessage(
        EventId = 11030,
        Level = LogLevel.Error,
        Message = "No Azure subscriptions are available for validation.")]
    public static partial void LogNoAzureSubscriptionsForValidation(this ILogger logger);

    [LoggerMessage(
        EventId = 11031,
        Level = LogLevel.Information,
        Message = "Available Azure subscriptions: {SubscriptionList}")]
    public static partial void LogAvailableAzureSubscriptions(
        this ILogger logger,
        string subscriptionList);

    [LoggerMessage(
        EventId = 11033,
        Level = LogLevel.Warning,
        Message = "Null/Empty 'ServiceTags.AllowedSubscriptions' value. ServiceTag: {ServiceTagName}")]
    public static partial void LogNullOrEmptyAllowedSubscriptions(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11034,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.AllowedSubscriptions'. SubscriptionName: {SubscriptionName} Tag: {ServiceTagName}")]
    public static partial void LogInvalidAllowedSubscription(
        this ILogger logger,
        string? subscriptionName,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11035,
        Level = LogLevel.Information,
        Message = "Subscription allowed Service Tag Validation completed with {ErrorCount} errors.")]
    public static partial void LogAllowedSubscriptionValidationWithErrors(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 11036,
        Level = LogLevel.Information,
        Message = "Subscription allowed Service Tag validation completed successfully with no errors.")]
    public static partial void LogAllowedSubscriptionValidationSuccess(this ILogger logger);

    [LoggerMessage(
        EventId = 11037,
        Level = LogLevel.Debug,
        Message = "Validating allowed subscriptions for service tag | ServiceTag: {ServiceTagName} | AllowedSubscriptionCount: {AllowedCount}")]
    public static partial void LogValidatingServiceTagSubscriptions(
        this ILogger logger,
        string? serviceTagName,
        int allowedCount);

    [LoggerMessage(
        EventId = 11038,
        Level = LogLevel.Debug,
        Message = "Valid allowed subscription reference | SubscriptionName: {SubscriptionName} | ServiceTag: {ServiceTagName}")]
    public static partial void LogValidAllowedSubscription(
        this ILogger logger,
        string? subscriptionName,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11039,
        Level = LogLevel.Information,
        Message = "Allowed subscription validation summary | ValidReferences: {ValidCount} | InvalidReferences: {InvalidCount}")]
    public static partial void LogAllowedSubscriptionValidationSummary(
        this ILogger logger,
        int validCount,
        int invalidCount);

    #endregion

    #region ValidateServiceTagIPAddresses (11050-11069)

    [LoggerMessage(
        EventId = 11050,
        Level = LogLevel.Error,
        Message = "No ServiceTags provided in the settings.")]
    public static partial void LogNoServiceTagsProvidedForIPValidation(this ILogger logger);

    [LoggerMessage(
        EventId = 11051,
        Level = LogLevel.Warning,
        Message = "Null/Empty 'ServiceTags.Name' value.")]
    public static partial void LogNullOrEmptyServiceTagName(this ILogger logger);

    [LoggerMessage(
        EventId = 11052,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no AddressPrefixes defined.")]
    public static partial void LogNoAddressPrefixesDefined(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11053,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.AddressPrefixes' value. ServiceTags.Name: {ServiceTagName}, IPAddress: {IPAddress}")]
    public static partial void LogInvalidAddressPrefixValue(
        this ILogger logger,
        string? serviceTagName,
        string? ipAddress);

    [LoggerMessage(
        EventId = 11054,
        Level = LogLevel.Warning,
        Message = "ServiceTag '{ServiceTagName}' has no SubnetIds defined.")]
    public static partial void LogNoSubnetIdsDefined(
        this ILogger logger,
        string? serviceTagName);

    [LoggerMessage(
        EventId = 11055,
        Level = LogLevel.Error,
        Message = "Invalid 'ServiceTags.Subnet' value. ServiceTags.Name: {ServiceTagName}, SubnetId: {SubnetId}")]
    public static partial void LogInvalidSubnetIdValue(
        this ILogger logger,
        string? serviceTagName,
        string? subnetId);

    [LoggerMessage(
        EventId = 11056,
        Level = LogLevel.Warning,
        Message = "Service Tag IP Address Validation completed with {ErrorCount} errors")]
    public static partial void LogIPAddressValidationWithErrors(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 11057,
        Level = LogLevel.Information,
        Message = "Service Tag IP Address Validation completed successfully with no errors.")]
    public static partial void LogIPAddressValidationSuccess(this ILogger logger);

    [LoggerMessage(
        EventId = 11058,
        Level = LogLevel.Debug,
        Message = "Processing service tag for IP validation | ServiceTag: {ServiceTagName} | AddressPrefixes: {AddressCount} | SubnetIds: {SubnetCount}")]
    public static partial void LogProcessingServiceTagForIPValidation(
        this ILogger logger,
        string? serviceTagName,
        int addressCount,
        int subnetCount);

    [LoggerMessage(
        EventId = 11059,
        Level = LogLevel.Debug,
        Message = "Valid CIDR address | ServiceTag: {ServiceTagName} | Address: {Address}")]
    public static partial void LogValidAddressPrefix(
        this ILogger logger,
        string? serviceTagName,
        string address);

    [LoggerMessage(
        EventId = 11060,
        Level = LogLevel.Debug,
        Message = "Valid subnet ID | ServiceTag: {ServiceTagName} | SubnetId: {SubnetId}")]
    public static partial void LogValidSubnetIdProcessed(
        this ILogger logger,
        string? serviceTagName,
        string subnetId);

    [LoggerMessage(
        EventId = 11061,
        Level = LogLevel.Information,
        Message = "IP validation summary | ServiceTagsProcessed: {TagCount} | ValidAddresses: {ValidAddresses} | InvalidAddresses: {InvalidAddresses} | ValidSubnets: {ValidSubnets} | InvalidSubnets: {InvalidSubnets}")]
    public static partial void LogIPValidationSummary(
        this ILogger logger,
        int tagCount,
        int validAddresses,
        int invalidAddresses,
        int validSubnets,
        int invalidSubnets);

    #endregion

    #region ValidateAzureSubscriptionParameters (11070-11089)

    [LoggerMessage(
        EventId = 11070,
        Level = LogLevel.Error,
        Message = "No AzureSubscriptions defined in the settings.")]
    public static partial void LogNoAzureSubscriptionsDefined(this ILogger logger);

    [LoggerMessage(
        EventId = 11071,
        Level = LogLevel.Warning,
        Message = "AzureSubscription validation failed. A minimum of {MinCount} subscriptions with valid 'Id' and 'Name' is required.")]
    public static partial void LogAzureSubscriptionValidationFailed(
        this ILogger logger,
        int minCount);

    [LoggerMessage(
        EventId = 11072,
        Level = LogLevel.Warning,
        Message = "AzureSubscription has missing 'Id' or 'Name'. SubscriptionName: {SubscriptionName}")]
    public static partial void LogMissingIdOrName(
        this ILogger logger,
        string subscriptionName);

    [LoggerMessage(
        EventId = 11073,
        Level = LogLevel.Warning,
        Message = "Invalid 'AzureSubscription.Id' value. AzureSubscription.Id must be a valid GUID. AzureSubscription.Name: {SubscriptionName}")]
    public static partial void LogInvalidSubscriptionId(
        this ILogger logger,
        string? subscriptionName);

    [LoggerMessage(
        EventId = 11074,
        Level = LogLevel.Debug,
        Message = "Validated 'AzureSubscription.Id' as valid GUID. AzureSubscription.Id: {SubscriptionId}, AzureSubscription.Name: {SubscriptionName}")]
    public static partial void LogValidSubscriptionId(
        this ILogger logger,
        string? subscriptionId,
        string? subscriptionName);

    [LoggerMessage(
        EventId = 11075,
        Level = LogLevel.Warning,
        Message = "Azure Subscription Validation failed with {ErrorCount} errors")]
    public static partial void LogAzureSubscriptionValidationWithErrors(
        this ILogger logger,
        int errorCount);

    [LoggerMessage(
        EventId = 11076,
        Level = LogLevel.Information,
        Message = "Azure Subscription Validation completed successfully with no errors.")]
    public static partial void LogAzureSubscriptionValidationSuccess(this ILogger logger);

    [LoggerMessage(
        EventId = 11077,
        Level = LogLevel.Information,
        Message = "Processing {SubscriptionCount} Azure subscriptions for validation")]
    public static partial void LogProcessingAzureSubscriptions(
        this ILogger logger,
        int subscriptionCount);

    [LoggerMessage(
        EventId = 11078,
        Level = LogLevel.Information,
        Message = "Azure subscription validation summary | Valid: {ValidCount} | Invalid: {InvalidCount} | Total: {TotalCount}")]
    public static partial void LogAzureSubscriptionValidationSummary(
        this ILogger logger,
        int validCount,
        int invalidCount,
        int totalCount);

    #endregion

    // ============================================================
    // ResourceDependencyInformationValidator (EventIds 11200-11399)
    // ============================================================

    #region Validate Method Lifecycle (11200-11219)

    [LoggerMessage(
        EventId = 11200,
        Level = LogLevel.Information,
        Message = "Validating Resource ID: {ResourceId}")]
    public static partial void LogValidatingResourceId(
        this ILogger logger,
        string? resourceId);

    [LoggerMessage(
        EventId = 11201,
        Level = LogLevel.Information,
        Message = "Validating Newday Service Tags")]
    public static partial void LogValidatingNewdayServiceTags(this ILogger logger);

    [LoggerMessage(
        EventId = 11202,
        Level = LogLevel.Information,
        Message = "Validating Azure Service Tags")]
    public static partial void LogValidatingAzureServiceTags(this ILogger logger);

    [LoggerMessage(
        EventId = 11203,
        Level = LogLevel.Information,
        Message = "Validating Resource ID Format for outbound and inbound")]
    public static partial void LogValidatingResourceIdFormat(this ILogger logger);

    [LoggerMessage(
        EventId = 11204,
        Level = LogLevel.Information,
        Message = "Validating Cross Subscription Allowance for Subscription ID: {SubscriptionId}")]
    public static partial void LogValidatingCrossSubscriptionAllowance(
        this ILogger logger,
        string? subscriptionId);

    [LoggerMessage(
        EventId = 11205,
        Level = LogLevel.Error,
        Message = "Validation exception occurred: {ExceptionMessage}")]
    public static partial void LogValidationExceptionWithMessage(
        this ILogger logger,
        string exceptionMessage);

    [LoggerMessage(
        EventId = 11206,
        Level = LogLevel.Error,
        Message = "Inner Exception: {InnerExceptionMessage}")]
    public static partial void LogInnerException(
        this ILogger logger,
        string innerExceptionMessage);

    // NEW: Validation completion logs
    [LoggerMessage(
        EventId = 11207,
        Level = LogLevel.Information,
        Message = "Validation completed successfully | ResourceId: {ResourceId}")]
    public static partial void LogValidationCompletedSuccess(
        this ILogger logger,
        string? resourceId);

    [LoggerMessage(
        EventId = 11208,
        Level = LogLevel.Warning,
        Message = "Validation completed with errors | ResourceId: {ResourceId} | ErrorCount: {ErrorCount}")]
    public static partial void LogValidationCompletedWithErrors(
        this ILogger logger,
        string? resourceId,
        int errorCount);

    [LoggerMessage(
        EventId = 11209,
        Level = LogLevel.Debug,
        Message = "Validation summary | ResourceId: {ResourceId} | TotalChecks: {TotalChecks} | ErrorCount: {ErrorCount} | Success: {Success}")]
    public static partial void LogValidationSummary(
        this ILogger logger,
        string? resourceId,
        int totalChecks,
        int errorCount,
        bool success);

    #endregion

    #region ValidateMainResourceId (11210-11219)

    [LoggerMessage(
        EventId = 11210,
        Level = LogLevel.Warning,
        Message = "Main resource ID is null or empty")]
    public static partial void LogMainResourceIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 11211,
        Level = LogLevel.Warning,
        Message = "Main resource ID format invalid | ResourceId: {ResourceId} | Error: {ErrorMessage}")]
    public static partial void LogMainResourceIdInvalidFormat(
        this ILogger logger,
        string resourceId,
        string errorMessage);

    [LoggerMessage(
        EventId = 11212,
        Level = LogLevel.Information,
        Message = "Main resource ID validated successfully | ResourceId: {ResourceId} | MatchedType: {MatchedResourceType}")]
    public static partial void LogMainResourceIdValid(
        this ILogger logger,
        string resourceId,
        string matchedResourceType);

    [LoggerMessage(
        EventId = 11213,
        Level = LogLevel.Debug,
        Message = "Checking resource ID against regex | ResourceId: {ResourceId} | Pattern: {PatternName}")]
    public static partial void LogCheckingResourceIdPattern(
        this ILogger logger,
        string resourceId,
        string patternName);

    #endregion

    #region ValidateNewDayServiceTagExistence (11220-11239)

    [LoggerMessage(
        EventId = 11220,
        Level = LogLevel.Warning,
        Message = "Invalid/Null 'NewDayInternalAndThirdPartyTags' value. TagName: {TagName}")]
    public static partial void LogInvalidNewDayServiceTag(
        this ILogger logger,
        string tagName);

    [LoggerMessage(
        EventId = 11221,
        Level = LogLevel.Debug,
        Message = "Validated NewDayInternalAndThirdPartyTag: {TagName}")]
    public static partial void LogValidatedNewDayServiceTag(
        this ILogger logger,
        string tagName);

    // NEW: NewDay service tag validation lifecycle logs
    [LoggerMessage(
        EventId = 11222,
        Level = LogLevel.Information,
        Message = "NewDay service tag validation started | TagCount: {TagCount}")]
    public static partial void LogNewDayServiceTagValidationStart(
        this ILogger logger,
        int tagCount);

    [LoggerMessage(
        EventId = 11223,
        Level = LogLevel.Debug,
        Message = "No NewDay service tags to validate")]
    public static partial void LogNoNewDayServiceTagsToValidate(this ILogger logger);

    [LoggerMessage(
        EventId = 11224,
        Level = LogLevel.Information,
        Message = "NewDay service tag validation completed | ValidCount: {ValidCount} | InvalidCount: {InvalidCount}")]
    public static partial void LogNewDayServiceTagValidationComplete(
        this ILogger logger,
        int validCount,
        int invalidCount);

    [LoggerMessage(
        EventId = 11225,
        Level = LogLevel.Debug,
        Message = "Collected NewDay service tags from SecurityRestrictions: {SecurityRestrictionsCount} | ScmSecurityRestrictions: {ScmSecurityRestrictionsCount}")]
    public static partial void LogNewDayServiceTagsCollected(
        this ILogger logger,
        int securityRestrictionsCount,
        int scmSecurityRestrictionsCount);

    #endregion

    #region ValidateAzureServiceTagExistence (11240-11259)

    [LoggerMessage(
        EventId = 11240,
        Level = LogLevel.Warning,
        Message = "Invalid/Null 'AzureServiceTag' value. TagName: {TagName}")]
    public static partial void LogInvalidAzureServiceTag(
        this ILogger logger,
        string tagName);

    [LoggerMessage(
        EventId = 11241,
        Level = LogLevel.Debug,
        Message = "Validated AzureServiceTag: {TagName}")]
    public static partial void LogValidatedAzureServiceTag(
        this ILogger logger,
        string tagName);

    // NEW: Azure service tag validation lifecycle logs
    [LoggerMessage(
        EventId = 11242,
        Level = LogLevel.Information,
        Message = "Azure service tag validation started | TagCount: {TagCount} | SubscriptionId: {SubscriptionId}")]
    public static partial void LogAzureServiceTagValidationStart(
        this ILogger logger,
        int tagCount,
        string? subscriptionId);

    [LoggerMessage(
        EventId = 11243,
        Level = LogLevel.Debug,
        Message = "No Azure service tags to validate")]
    public static partial void LogNoAzureServiceTagsToValidate(this ILogger logger);

    [LoggerMessage(
        EventId = 11244,
        Level = LogLevel.Information,
        Message = "Azure service tag validation completed | ValidCount: {ValidCount} | InvalidCount: {InvalidCount}")]
    public static partial void LogAzureServiceTagValidationComplete(
        this ILogger logger,
        int validCount,
        int invalidCount);

    [LoggerMessage(
        EventId = 11245,
        Level = LogLevel.Debug,
        Message = "Collected Azure service tags from SecurityRestrictions: {SecurityRestrictionsCount} | ScmSecurityRestrictions: {ScmSecurityRestrictionsCount}")]
    public static partial void LogAzureServiceTagsCollected(
        this ILogger logger,
        int securityRestrictionsCount,
        int scmSecurityRestrictionsCount);

    #endregion

    #region ValidateResourceIdFormat (11260-11279)

    [LoggerMessage(
        EventId = 11260,
        Level = LogLevel.Error,
        Message = "Inbound resource ID validation errors found, InboundResourceIdErrors:{InboundResourceIdError}")]
    public static partial void LogInboundResourceIdError(
        this ILogger logger,
        string inboundResourceIdError);

    [LoggerMessage(
        EventId = 11261,
        Level = LogLevel.Warning,
        Message = "No outbound resource IDs provided.")]
    public static partial void LogNoOutboundResourceIds(this ILogger logger);

    [LoggerMessage(
        EventId = 11262,
        Level = LogLevel.Information,
        Message = "Validating {Count} outbound resource IDs.")]
    public static partial void LogValidatingOutboundResourceIds(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 11263,
        Level = LogLevel.Error,
        Message = "Outbound resource ID validation errors found, OutboundResourceIdErrors:{OutboundResourceIdError}")]
    public static partial void LogOutboundResourceIdError(
        this ILogger logger,
        string outboundResourceIdError);

    [LoggerMessage(
        EventId = 11264,
        Level = LogLevel.Information,
        Message = "Resource ID format validation completed successfully.")]
    public static partial void LogResourceIdFormatValidationSuccess(this ILogger logger);

    // NEW: Resource ID format validation additional logs
    [LoggerMessage(
        EventId = 11265,
        Level = LogLevel.Debug,
        Message = "No inbound security restrictions defined")]
    public static partial void LogNoInboundSecurityRestrictions(this ILogger logger);

    [LoggerMessage(
        EventId = 11266,
        Level = LogLevel.Information,
        Message = "Validating inbound resource IDs | Count: {Count}")]
    public static partial void LogValidatingInboundResourceIds(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 11267,
        Level = LogLevel.Information,
        Message = "Inbound resource ID validation completed | ValidCount: {ValidCount} | InvalidCount: {InvalidCount}")]
    public static partial void LogInboundResourceIdValidationComplete(
        this ILogger logger,
        int validCount,
        int invalidCount);

    [LoggerMessage(
        EventId = 11268,
        Level = LogLevel.Warning,
        Message = "Outbound resources not allowed for this resource type | ResourceId: {ResourceId}")]
    public static partial void LogOutboundResourcesNotAllowed(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 11269,
        Level = LogLevel.Information,
        Message = "Outbound resource ID validation completed | ValidCount: {ValidCount} | InvalidCount: {InvalidCount}")]
    public static partial void LogOutboundResourceIdValidationComplete(
        this ILogger logger,
        int validCount,
        int invalidCount);

    [LoggerMessage(
        EventId = 11270,
        Level = LogLevel.Debug,
        Message = "Validating inbound resource ID | ResourceId: {ResourceId}")]
    public static partial void LogValidatingInboundResourceId(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 11271,
        Level = LogLevel.Debug,
        Message = "Inbound resource ID valid | ResourceId: {ResourceId} | MatchedPattern: {MatchedPattern}")]
    public static partial void LogInboundResourceIdValid(
        this ILogger logger,
        string resourceId,
        string matchedPattern);

    [LoggerMessage(
        EventId = 11272,
        Level = LogLevel.Debug,
        Message = "Validating outbound resource ID | ResourceId: {ResourceId}")]
    public static partial void LogValidatingOutboundResourceId(
        this ILogger logger,
        string resourceId);

    [LoggerMessage(
        EventId = 11273,
        Level = LogLevel.Debug,
        Message = "Outbound resource ID valid | ResourceId: {ResourceId} | MatchedPattern: {MatchedPattern}")]
    public static partial void LogOutboundResourceIdValid(
        this ILogger logger,
        string resourceId,
        string matchedPattern);

    [LoggerMessage(
        EventId = 11274,
        Level = LogLevel.Warning,
        Message = "Null or empty resource ID found in inbound list")]
    public static partial void LogNullOrEmptyInboundResourceId(this ILogger logger);

    [LoggerMessage(
        EventId = 11275,
        Level = LogLevel.Warning,
        Message = "Null or empty resource ID found in outbound list")]
    public static partial void LogNullOrEmptyOutboundResourceId(this ILogger logger);

    #endregion

    #region ValidateCrossSubscriptionAllowance (11280-11299)

    [LoggerMessage(
        EventId = 11280,
        Level = LogLevel.Information,
        Message = "Validating cross-subscription allowance | ResourceId: {ResourceId} | SubscriptionId: {SubscriptionId}")]
    public static partial void LogValidatingCrossSubscription(
        this ILogger logger,
        string? resourceId,
        string? subscriptionId);

    [LoggerMessage(
        EventId = 11281,
        Level = LogLevel.Warning,
        Message = "Cross-subscription access detected | ResourceSubscription: {ResourceSubscription} | RequestSubscription: {RequestSubscription}")]
    public static partial void LogCrossSubscriptionDetected(
        this ILogger logger,
        string resourceSubscription,
        string requestSubscription);

    [LoggerMessage(
        EventId = 11282,
        Level = LogLevel.Information,
        Message = "Cross-subscription allowance validation passed.")]
    public static partial void LogCrossSubscriptionValidationPassed(this ILogger logger);

    // NEW: Cross-subscription validation additional logs
    [LoggerMessage(
        EventId = 11283,
        Level = LogLevel.Debug,
        Message = "Request subscription ID is null or empty, skipping cross-subscription validation")]
    public static partial void LogRequestSubscriptionIdNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 11284,
        Level = LogLevel.Debug,
        Message = "No cross-subscription pool found for subscription | SubscriptionId: {SubscriptionId}")]
    public static partial void LogCrossSubscriptionPoolNotFound(
        this ILogger logger,
        string subscriptionId);

    [LoggerMessage(
        EventId = 11285,
        Level = LogLevel.Information,
        Message = "Cross-subscription pool found | Environment: {Environment} | AllowedSubscriptionCount: {AllowedSubscriptionCount}")]
    public static partial void LogCrossSubscriptionPoolFound(
        this ILogger logger,
        string environment,
        int allowedSubscriptionCount);

    [LoggerMessage(
        EventId = 11286,
        Level = LogLevel.Debug,
        Message = "Validating inbound resources for cross-subscription | ResourceCount: {ResourceCount}")]
    public static partial void LogValidatingInboundCrossSubscription(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 11287,
        Level = LogLevel.Debug,
        Message = "Validating outbound resources for cross-subscription | ResourceCount: {ResourceCount}")]
    public static partial void LogValidatingOutboundCrossSubscription(
        this ILogger logger,
        int resourceCount);

    [LoggerMessage(
        EventId = 11288,
        Level = LogLevel.Debug,
        Message = "Inbound resource subscription allowed | ResourceId: {ResourceId} | SubscriptionId: {SubscriptionId}")]
    public static partial void LogInboundCrossSubscriptionAllowed(
        this ILogger logger,
        string resourceId,
        string subscriptionId);

    [LoggerMessage(
        EventId = 11289,
        Level = LogLevel.Debug,
        Message = "Outbound resource subscription allowed | ResourceId: {ResourceId} | SubscriptionId: {SubscriptionId}")]
    public static partial void LogOutboundCrossSubscriptionAllowed(
        this ILogger logger,
        string resourceId,
        string subscriptionId);

    [LoggerMessage(
        EventId = 11290,
        Level = LogLevel.Warning,
        Message = "Inbound cross-subscription not allowed | ResourceId: {ResourceId} | ResourceSubscription: {ResourceSubscription}")]
    public static partial void LogInboundCrossSubscriptionNotAllowed(
        this ILogger logger,
        string resourceId,
        string resourceSubscription);

    [LoggerMessage(
        EventId = 11291,
        Level = LogLevel.Warning,
        Message = "Outbound cross-subscription not allowed | ResourceId: {ResourceId} | ResourceSubscription: {ResourceSubscription}")]
    public static partial void LogOutboundCrossSubscriptionNotAllowed(
        this ILogger logger,
        string resourceId,
        string resourceSubscription);

    [LoggerMessage(
        EventId = 11292,
        Level = LogLevel.Information,
        Message = "Cross-subscription validation completed | InboundErrorCount: {InboundErrorCount} | OutboundErrorCount: {OutboundErrorCount}")]
    public static partial void LogCrossSubscriptionValidationComplete(
        this ILogger logger,
        int inboundErrorCount,
        int outboundErrorCount);

    #endregion

    #region ValidateFormat Method (11300-11319)

    [LoggerMessage(
        EventId = 11300,
        Level = LogLevel.Information,
        Message = "Validating format for Resource ID: {ResourceId}")]
    public static partial void LogValidatingFormatResourceId(
        this ILogger logger,
        string? resourceId);

    [LoggerMessage(
        EventId = 11301,
        Level = LogLevel.Information,
        Message = "Validating Resource ID Format for outbound and inbound (Format validation)")]
    public static partial void LogValidatingResourceIdFormatOnly(this ILogger logger);

    [LoggerMessage(
        EventId = 11302,
        Level = LogLevel.Information,
        Message = "Validating Cross Subscription Allowance for Subscription ID: {SubscriptionId} (Format validation)")]
    public static partial void LogValidatingCrossSubscriptionFormat(
        this ILogger logger,
        string? subscriptionId);

    // NEW: Format validation completion logs
    [LoggerMessage(
        EventId = 11303,
        Level = LogLevel.Information,
        Message = "Format validation completed successfully | ResourceId: {ResourceId}")]
    public static partial void LogFormatValidationCompletedSuccess(
        this ILogger logger,
        string? resourceId);

    [LoggerMessage(
        EventId = 11304,
        Level = LogLevel.Warning,
        Message = "Format validation completed with errors | ResourceId: {ResourceId} | ErrorCount: {ErrorCount}")]
    public static partial void LogFormatValidationCompletedWithErrors(
        this ILogger logger,
        string? resourceId,
        int errorCount);

    #endregion

    // ============================================================
    // JSON Parsing Operations (EventIds 11520-11539)
    // ============================================================

    #region JSON Parsing

    [LoggerMessage(
        EventId = 11520,
        Level = LogLevel.Debug,
        Message = "JSON parsing started | TargetType: {TargetType} | InputLength: {InputLength} bytes")]
    public static partial void LogJsonParsingStarted(
        this ILogger logger,
        string targetType,
        int inputLength);

    [LoggerMessage(
        EventId = 11521,
        Level = LogLevel.Information,
        Message = "JSON parsing completed successfully | TargetType: {TargetType} | InputLength: {InputLength} bytes")]
    public static partial void LogJsonParsingCompleted(
        this ILogger logger,
        string targetType,
        int inputLength);

    [LoggerMessage(
        EventId = 11522,
        Level = LogLevel.Warning,
        Message = "JSON parsing error encountered | TargetType: {TargetType} | ErrorMessage: {ErrorMessage}")]
    public static partial void LogJsonParsingError(
        this ILogger logger,
        string targetType,
        string errorMessage);

    [LoggerMessage(
        EventId = 11523,
        Level = LogLevel.Error,
        Message = "JSON parsing failed | TargetType: {TargetType} | ErrorCount: {ErrorCount} | Errors: {Errors}")]
    public static partial void LogJsonParsingFailed(
        this ILogger logger,
        string targetType,
        int errorCount,
        string errors);

    [LoggerMessage(
        EventId = 11524,
        Level = LogLevel.Warning,
        Message = "JSON deserialization resulted in null | TargetType: {TargetType}")]
    public static partial void LogJsonDeserializationNull(
        this ILogger logger,
        string targetType);

    [LoggerMessage(
        EventId = 11525,
        Level = LogLevel.Debug,
        Message = "JSON parsing input is null or empty | TargetType: {TargetType}")]
    public static partial void LogJsonInputNullOrEmpty(
        this ILogger logger,
        string targetType);

    [LoggerMessage(
        EventId = 11526,
        Level = LogLevel.Error,
        Message = "JSON parsing exception | TargetType: {TargetType}")]
    public static partial void LogJsonParsingException(
        this ILogger logger,
        Exception exception,
        string targetType);

    #endregion

    // ============================================================
    // Validation Summary (EventIds 11540-11549)
    // ============================================================

    #region Validation Summary

    [LoggerMessage(
        EventId = 11540,
        Level = LogLevel.Information,
        Message = "Validation completed | ModelType: {ModelType} | Success: {Success} | ErrorCount: {ErrorCount} | WarningCount: {WarningCount} | DurationMs: {DurationMs}")]
    public static partial void LogValidationCompleted(
        this ILogger logger,
        string modelType,
        bool success,
        int errorCount,
        int warningCount,
        long durationMs);

    [LoggerMessage(
        EventId = 11541,
        Level = LogLevel.Debug,
        Message = "Validation started | ModelType: {ModelType}")]
    public static partial void LogValidationStarted(
        this ILogger logger,
        string modelType);

    #endregion

    // ============================================================
    // AzureSubscriptionsPersistenceManager - Additional Logs (EventIds 10115-10119)
    // ============================================================

    #region AzureSubscriptionsPersistenceManager

    [LoggerMessage(
        EventId = 10115,
        Level = LogLevel.Information,
        Message = "Items to add or update | Count: {Count}")]
    public static partial void LogItemsToAddOrUpdate(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10116,
        Level = LogLevel.Warning,
        Message = "Input list is null or empty for UpdateDatabaseStateTo | ContainerName: {ContainerName}")]
    public static partial void LogInputListNullOrEmpty(
        this ILogger logger,
        string containerName);

    [LoggerMessage(
        EventId = 10117,
        Level = LogLevel.Information,
        Message = "UpdateDatabaseStateTo completed | ContainerName: {ContainerName} | ItemsDeleted: {ItemsDeleted} | ItemsUpserted: {ItemsUpserted} | DurationMs: {DurationMs}")]
    public static partial void LogUpdateDatabaseStateCompleted(
        this ILogger logger,
        string containerName,
        int itemsDeleted,
        int itemsUpserted,
        long durationMs);

    [LoggerMessage(
        EventId = 10118,
        Level = LogLevel.Error,
        Message = "Failed to delete item | ItemId: {ItemId} | ContainerName: {ContainerName}")]
    public static partial void LogDeleteItemFailed(
        this ILogger logger,
        Exception exception,
        string itemId,
        string containerName);

    [LoggerMessage(
        EventId = 10119,
        Level = LogLevel.Debug,
        Message = "Starting deletion phase | ItemCount: {ItemCount}")]
    public static partial void LogStartingDeletionPhase(
        this ILogger logger,
        int itemCount);

    #endregion

    // ============================================================
    // ServiceTagsPersistenceManager - Additional Logs (EventIds 10170-10179)
    // ============================================================

    #region ServiceTagsPersistenceManager

    [LoggerMessage(
        EventId = 10170,
        Level = LogLevel.Warning,
        Message = "Input list is null or empty for ServiceTags UpdateDatabaseStateTo")]
    public static partial void LogServiceTagsInputListNullOrEmpty(
        this ILogger logger);

    [LoggerMessage(
        EventId = 10171,
        Level = LogLevel.Information,
        Message = "ServiceTags to add or update | Count: {Count}")]
    public static partial void LogServiceTagsToAddOrUpdate(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10172,
        Level = LogLevel.Error,
        Message = "Failed to delete ServiceTag | ServiceTagId: {ServiceTagId}")]
    public static partial void LogServiceTagDeleteFailed(
        this ILogger logger,
        Exception exception,
        string serviceTagId);

    [LoggerMessage(
        EventId = 10173,
        Level = LogLevel.Warning,
        Message = "ServiceTag not found during deletion | ServiceTagId: {ServiceTagId}")]
    public static partial void LogServiceTagNotFoundDuringDeletion(
        this ILogger logger,
        string serviceTagId);

    [LoggerMessage(
        EventId = 10174,
        Level = LogLevel.Error,
        Message = "Failed to upsert ServiceTag | ServiceTagId: {ServiceTagId}")]
    public static partial void LogServiceTagUpsertFailed(
        this ILogger logger,
        Exception exception,
        string serviceTagId);

    [LoggerMessage(
        EventId = 10175,
        Level = LogLevel.Information,
        Message = "ServiceTags UpdateDatabaseStateTo completed | ItemsDeleted: {ItemsDeleted} | ItemsUpserted: {ItemsUpserted} | DurationMs: {DurationMs}")]
    public static partial void LogServiceTagsUpdateCompleted(
        this ILogger logger,
        int itemsDeleted,
        int itemsUpserted,
        long durationMs);

    [LoggerMessage(
        EventId = 10176,
        Level = LogLevel.Debug,
        Message = "Starting ServiceTags deletion phase | ItemCount: {ItemCount}")]
    public static partial void LogStartingServiceTagsDeletionPhase(
        this ILogger logger,
        int itemCount);

    [LoggerMessage(
        EventId = 10177,
        Level = LogLevel.Debug,
        Message = "Starting ServiceTags upsert phase | ItemCount: {ItemCount}")]
    public static partial void LogStartingServiceTagsUpsertPhase(
        this ILogger logger,
        int itemCount);

    #endregion

    // ============================================================
    // InternalAndThirdPartyServiceTagPersistenceManager - Additional Logs (EventIds 10210-10219)
    // ============================================================

    #region InternalAndThirdPartyServiceTagPersistenceManager

    [LoggerMessage(
        EventId = 10210,
        Level = LogLevel.Information,
        Message = "Skipping Azure Subscriptions update - list is null or empty")]
    public static partial void LogSkippingAzureSubscriptionsUpdate(
        this ILogger logger);

    [LoggerMessage(
        EventId = 10211,
        Level = LogLevel.Information,
        Message = "Skipping Service Tags update - list is null or empty")]
    public static partial void LogSkippingServiceTagsUpdate(
        this ILogger logger);

    [LoggerMessage(
        EventId = 10212,
        Level = LogLevel.Information,
        Message = "UpdateDatabaseStateTo completed | AzureSubscriptionsUpdated: {AzureSubscriptionsUpdated} | ServiceTagsUpdated: {ServiceTagsUpdated} | DurationMs: {DurationMs}")]
    public static partial void LogPersistenceManagerUpdateCompleted(
        this ILogger logger,
        bool azureSubscriptionsUpdated,
        bool serviceTagsUpdated,
        long durationMs);

    [LoggerMessage(
        EventId = 10213,
        Level = LogLevel.Debug,
        Message = "Azure Subscriptions update completed successfully | Count: {Count}")]
    public static partial void LogAzureSubscriptionsUpdateCompleted(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 10214,
        Level = LogLevel.Debug,
        Message = "Service Tags update completed successfully | Count: {Count}")]
    public static partial void LogServiceTagsUpdateCompletedCount(
        this ILogger logger,
        int count);

    #endregion
  }
}