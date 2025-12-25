namespace DynamicAllowListingLib
{
  public static class Constants
  {
    public const string DatabaseName = "DynamicAllowListingDb";
    public const string ServiceTagsCollection = "ServiceTags";
    public const string AzureSubscriptionsCollection = "AzureSubscriptions";
    public const string NetworkRestrictionsConfigsCollection = "NetworkRestrictionsConfigs";
    //Cosmos Collection Names
    public const string ServiceTagsLease = "ServiceTagsLease";
    public const string AzureSubscriptionsLease = "AzureSubscriptionsLease";
    //Regexs
    public const string VNetSubnetIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.Network/virtualNetworks\\/([a-zA-Z0-9-]+)/subnets/([a-zA-Z0-9-]+)";
    public const string AppServicePlanResourceIdRegex =
      "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\b\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.Web/serverfarms\\/([a-zA-Z0-9-]+)";
    public const string WebSiteResourceIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.Web/sites\\/([a-zA-Z0-9-]+)";
    public const string PublicIpAddressResourceIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.Network/publicIPAddresses\\/([a-zA-Z0-9-]+)";
    public const string FrontDoorResourceIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.Network/frontdoors\\/([a-zA-Z0-9-]+)";
  }
}