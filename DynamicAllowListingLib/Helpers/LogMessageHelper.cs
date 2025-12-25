using System;

namespace DynamicAllowListingLib
{
  public class LogMessageHelper
  {
    public static string GetWebConfigNullMessage(string resourceIdUri) => $"Unable to update network restriction rules. Rest call to get web config returned null. Does the resource exist? Resource: {resourceIdUri}";

    public static string GetSuccessfullyUpdatedConfigMessage(string resourceIdUri) => $"Successfully updated network restrictions for resource: {resourceIdUri}.";

    public static string GetVnetAddedDueToNamingMatchMessage(string resourceId, string vnetSubnetResourceId) => $"The app should be vnet integrated? A vnetsubnetid {vnetSubnetResourceId} has been added to allow listing rules for {resourceId} as it complies with NewDay naming convention.";

    public static string GetWebAppRulesLimitReachedMessage(string resourceId)
      => $"Unable to apply restrictions to resource {resourceId}. Limit of 512 reached. Refer docs: https://docs.microsoft.com/en-us/azure/app-service/app-service-ip-restrictions#add-an-access-restriction-rule.";

    public static string GetWebAppRulesLimitReachedMessage(string resourceId, string errorDetails)
      => $"Unable to apply restrictions to resource {resourceId}. Limit of 512 reached. Refer docs: https://docs.microsoft.com/en-us/azure/app-service/app-service-ip-restrictions#add-an-access-restriction-rule." +
         Environment.NewLine + "Error details:" +
         Environment.NewLine + errorDetails;

    public static string GetStorageLimitReachedMessage(string resourceId)
      => $"Unable to apply restrictions to resource {resourceId}. Limit of 200 reached. Refer docs: https://docs.microsoft.com/en-us/azure/storage/common/storage-network-security?tabs=azure-portal#grant-access-from-an-internet-ip-range.";
  }
}