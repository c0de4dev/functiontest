#pragma warning disable CS8618 
namespace DynamicAllowListingLib.Models
{
  public class VNets
  {
    public int totalRecords { get; set; }
    public int count { get; set; }
    public Datum[] data { get; set; }
    public object[] facets { get; set; }
    public string resultTruncated { get; set; }
    public class Datum
    {
      public string id { get; set; }
      public string name { get; set; }
      public string type { get; set; }
      public string tenantId { get; set; }
      public string kind { get; set; }
      public string location { get; set; }
      public string resourceGroup { get; set; }
      public string subscriptionId { get; set; }
      public string managedBy { get; set; }
      public object sku { get; set; }
      public object plan { get; set; }
      public Properties properties { get; set; }
      public Tags tags { get; set; }
      public object identity { get; set; }
      public object zones { get; set; }
      public object extendedLocation { get; set; }
    }
    public class Properties
    {
      public string provisioningState { get; set; }
      public string resourceGuid { get; set; }
      public Virtualnetworkpeering[] virtualNetworkPeerings { get; set; }
      public bool enableDdosProtection { get; set; }
      public Subnet[] subnets { get; set; }
      public Addressspace addressSpace { get; set; }
      public Dhcpoptions dhcpOptions { get; set; }
    }
    public class Addressspace
    {
      public string[] addressPrefixes { get; set; }
    }
    public class Dhcpoptions
    {
      public object[] dnsServers { get; set; }
    }
    public class Virtualnetworkpeering
    {
      public Properties1 properties { get; set; }
      public string id { get; set; }
      public string name { get; set; }
      public string type { get; set; }
      public string etag { get; set; }
    }
    public class Properties1
    {
      public string provisioningState { get; set; }
      public string resourceGuid { get; set; }
      public bool doNotVerifyRemoteGateways { get; set; }
      public bool allowVirtualNetworkAccess { get; set; }
      public bool allowForwardedTraffic { get; set; }
      public Remotevirtualnetwork remoteVirtualNetwork { get; set; }
      public bool allowGatewayTransit { get; set; }
      public Remoteaddressspace remoteAddressSpace { get; set; }
      public bool useRemoteGateways { get; set; }
      public Routeservicevips routeServiceVips { get; set; }
      public string peeringState { get; set; }
      public Remotegateway[] remoteGateways { get; set; }
    }
    public class Remotevirtualnetwork
    {
      public string id { get; set; }
    }
    public class Remoteaddressspace
    {
      public string[] addressPrefixes { get; set; }
    }
    public class Routeservicevips
    {
    }
    public class Remotegateway
    {
      public string id { get; set; }
    }
    public class Subnet
    {
      public SubnetProperties properties { get; set; }
      public string id { get; set; }
      public string name { get; set; }
      public string type { get; set; }
      public string etag { get; set; }
    }
    public class SubnetProperties
    {
      public string provisioningState { get; set; }
      public string addressPrefix { get; set; }
      public string privateLinkServiceNetworkPolicies { get; set; }
      public string privateEndpointNetworkPolicies { get; set; }
      public Networksecuritygroup networkSecurityGroup { get; set; }
      public Delegation[] delegations { get; set; }
      public Serviceendpoint[] serviceEndpoints { get; set; }
      public Serviceassociationlink[] serviceAssociationLinks { get; set; }
      public Resourcenavigationlink[] resourceNavigationLinks { get; set; }
      public Routetable routeTable { get; set; }
      public Ipconfiguration[] ipConfigurations { get; set; }
    }
    public class Networksecuritygroup
    {
      public string id { get; set; }
    }
    public class Routetable
    {
      public string id { get; set; }
    }
    public class Delegation
    {
      public Properties3 properties { get; set; }
      public string id { get; set; }
      public string name { get; set; }
      public string type { get; set; }
      public string etag { get; set; }
    }
    public class Properties3
    {
      public string provisioningState { get; set; }
      public string[] actions { get; set; }
      public string serviceName { get; set; }
    }
    public class Serviceendpoint
    {
      public string provisioningState { get; set; }
      public string[] locations { get; set; }
      public string service { get; set; }
    }
    public class Serviceassociationlink
    {
      public Properties4 properties { get; set; }
      public string id { get; set; }
      public string name { get; set; }
      public string type { get; set; }
      public string etag { get; set; }
    }
    public class Properties4
    {
      public string provisioningState { get; set; }
      public object[] locations { get; set; }
      public bool enabledForArmDeployments { get; set; }
      public string linkedResourceType { get; set; }
      public bool allowDelete { get; set; }
      public string link { get; set; }
    }
    public class Resourcenavigationlink
    {
      public Properties5 properties { get; set; }
      public string id { get; set; }
      public string name { get; set; }
      public string type { get; set; }
      public string etag { get; set; }
    }

    public class Properties5
    {
      public string provisioningState { get; set; }
      public string linkedResourceType { get; set; }
      public string link { get; set; }
    }
    public class Ipconfiguration
    {
      public string id { get; set; }
    }

    public class Tags
    {
      public string createdDate { get; set; }
      public string product { get; set; }
      public string expiryDate { get; set; }
      public string createdBy { get; set; }
      public string group { get; set; }
      public string purpose { get; set; }
      public string description { get; set; }
      public string application { get; set; }
    }
  }
}
#pragma warning restore CS8618