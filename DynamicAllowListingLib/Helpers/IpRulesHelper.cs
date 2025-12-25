using DynamicAllowListingLib.Logging;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Helpers
{
  /// <summary>
  /// Helper class for generating IP security restriction rules.
  /// </summary>
  public class IpRulesHelper
  {
    /// <summary>
    /// Generates a set of IP security restriction rules from a comma-separated list of IP addresses.
    /// </summary>
    /// <param name="name">The name prefix for the rules.</param>
    /// <param name="allIps">Comma-separated list of IP addresses.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <returns>A set of IP security restriction rules.</returns>
    public static HashSet<IpSecurityRestrictionRule> GenerateDynamicAllowListingRules(
        string name,
        string allIps,
        ILogger? logger = null)
    {
      var ipsecRules = new HashSet<IpSecurityRestrictionRule>();

      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(allIps))
      {
        logger?.LogIpRuleGenerationSkipped("Name or IP list is null or empty");
        return ipsecRules;
      }

      string[] ips = allIps.Split(',');
      logger?.LogIpRuleGeneration(name, ips.Length);

      foreach (var ip in ips)
      {
        if (string.IsNullOrWhiteSpace(ip))
          continue;

        ipsecRules.Add(new IpSecurityRestrictionRule()
        {
          IpAddress = ip.Trim() + "/32",
          Name = StringHelper.Truncate(name)
        });
      }

      logger?.LogIpRulesGenerated(name, ipsecRules.Count);
      return ipsecRules;
    }

    /// <summary>
    /// Generates a single VNet subnet restriction rule.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="vnetSubnetId">The VNet subnet resource ID.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <returns>An IP security restriction rule configured for VNet access.</returns>
    public static IpSecurityRestrictionRule GenerateDynamicAllowListingRuleForVnet(
        string name,
        string vnetSubnetId,
        ILogger? logger = null)
    {
      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(vnetSubnetId))
      {
        logger?.LogIpRuleGenerationSkipped("Name or VNet subnet ID is null or empty");
        return new IpSecurityRestrictionRule();
      }

      logger?.LogVNetRuleGeneration(name, vnetSubnetId);

      var rule = new IpSecurityRestrictionRule()
      {
        Name = name,
        VnetSubnetResourceId = vnetSubnetId,
        Action = "Allow",
        Priority = 500
      };

      return rule;
    }

    /// <summary>
    /// Generates a set of IP security restriction rules from a comma-separated list of IP addresses.
    /// Overload without logger parameter for backward compatibility.
    /// </summary>
    /// <param name="name">The name prefix for the rules.</param>
    /// <param name="allIps">Comma-separated list of IP addresses.</param>
    /// <returns>A set of IP security restriction rules.</returns>
    public static HashSet<IpSecurityRestrictionRule> GenerateDynamicAllowListingRules(string name, string allIps)
    {
      return GenerateDynamicAllowListingRules(name, allIps, null);
    }

    /// <summary>
    /// Generates a single VNet subnet restriction rule.
    /// Overload without logger parameter for backward compatibility.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="vnetSubnetId">The VNet subnet resource ID.</param>
    /// <returns>An IP security restriction rule configured for VNet access.</returns>
    public static IpSecurityRestrictionRule GenerateDynamicAllowListingRuleForVnet(string name, string vnetSubnetId)
    {
      return GenerateDynamicAllowListingRuleForVnet(name, vnetSubnetId, null);
    }
  }
}