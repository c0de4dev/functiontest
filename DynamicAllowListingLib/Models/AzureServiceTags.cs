using DynamicAllowListingLib.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace DynamicAllowListingLib.Models
{
  class AzureServiceTags
  {
    public List<AzureServiceTag> Values { get; set; } = null!;
    public class Properties
    {
      public List<string> AddressPrefixes { get; set; } = null!;
      public IEnumerable<string> AddressPrefixesIpV4 => AddressPrefixes.Where(ip => IpAddressHelper.IsIpV4(IpAddressHelper.ConvertToIpNetwork(ip)));
    }
    public class AzureServiceTag
    {
      public string Name { get; set; } = null!;
      public Properties Properties { get; set; } = null!;
    }
  }
}
