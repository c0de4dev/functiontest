using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace DynamicAllowListingLib.Helpers
{
  public static class IpAddressHelper
  {
    public static bool IsPrivateIpAddress(IPNetwork2 ipNetwork)
    {
      IPNetwork2[] privateIpCidrs = {
        new IPNetwork2() { Value = "10.0.0.0/8" },
        new IPNetwork2() { Value = "100.64.0.0/10" },
        new IPNetwork2() { Value = "172.16.0.0/12" },
        new IPNetwork2() { Value = "192.168.0.0/16" }
      };

      return privateIpCidrs.Any(privateIpCidr => privateIpCidr.Contains(ipNetwork));
    }

    public static bool IsIpV4(IPNetwork2 ipNetwork) => ipNetwork.AddressFamily == AddressFamily.InterNetwork;

    public static IPNetwork2 ConvertToIpNetwork(string cidr) =>
      IPNetwork2.Parse(cidr);

    public static bool IsValidCosmosDbFirewallIp(IPNetwork2 ipNetwork) => IsIpV4(ipNetwork) && !IsPrivateIpAddress(ipNetwork);

    public static bool IsValidCosmosDbFirewallIp(string cidr) => IsValidCosmosDbFirewallIp(ConvertToIpNetwork(cidr));

    public static bool IsValidKeyVaultFirewallIp(IPNetwork2 ipNetwork) => IsIpV4(ipNetwork) && !IsPrivateIpAddress(ipNetwork);

    public static bool IsValidKeyVaultFirewallIp(string cidr) => IsValidCosmosDbFirewallIp(ConvertToIpNetwork(cidr));

    public static bool IsValidStorageFirewallIp(IPNetwork2 ipNetwork) => IsIpV4(ipNetwork) && !IsPrivateIpAddress(ipNetwork);

    public static bool IsValidStorageFirewallIp(string cidr) => IsValidStorageFirewallIp(ConvertToIpNetwork(cidr));
  }
}
