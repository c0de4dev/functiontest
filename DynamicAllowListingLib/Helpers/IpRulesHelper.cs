using System.Collections.Generic;

namespace DynamicAllowListingLib.Helpers
{
  public class IpRulesHelper
  {
    public static HashSet<IpSecurityRestrictionRule> GenerateDynamicAllowListingRules(string name, string allIps)
    {

      HashSet<IpSecurityRestrictionRule> ipsecRules = new HashSet<IpSecurityRestrictionRule>();
      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(allIps))
        return ipsecRules;

      string[] ips = allIps.Split(',');
      foreach (var ip in ips)
      {
        ipsecRules.Add(new IpSecurityRestrictionRule()
        {
          IpAddress = ip + "/32",
          Name = StringHelper.Truncate(name)
        });
      }
      return ipsecRules;
    }

    public static IpSecurityRestrictionRule GenerateDynamicAllowListingRuleForVnet(string name, string vnetSubnetId)
    {
      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(vnetSubnetId))
        return new IpSecurityRestrictionRule();

      var rule = new IpSecurityRestrictionRule()
      {
        Name = name,
        VnetSubnetResourceId = vnetSubnetId,
        Action = "Allow",
        Priority = 500
      };
      return rule;
    }
  }
}