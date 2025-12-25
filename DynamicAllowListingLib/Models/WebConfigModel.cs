using System.Collections.Generic;

namespace DynamicAllowListingLib.Models
{
  public class WebConfigModel
  {
    public string? Name { get; set; }
    public string? Id { get; set; }
    public Properties? Properties { get; set; }
  }
  public class Properties
  {
    public List<IpSecurityRestrictionRule>? IpSecurityRestrictions { get; set; }
    public List<IpSecurityRestrictionRule>? ScmIpSecurityRestrictions { get; set; }
  }
}