using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicAllowListingLib
{
  public class NetworkRestrictionSettings
  {
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }
    [JsonPropertyName("ipSecRules")]
    public HashSet<IpSecurityRestrictionRule>? IpSecRules { get; set; }
    [JsonPropertyName("scmIpSecRules")]
    public HashSet<IpSecurityRestrictionRule>? ScmIpSecRules { get; set; }
    [JsonPropertyName("ipSecRulesToDelete")]
    public HashSet<IpSecurityRestrictionRule>? IpSecRulesToDelete { get; set; }
    [JsonPropertyName("scmIpSecRulesToDelete")]
    public HashSet<IpSecurityRestrictionRule>? ScmIpSecRulesToDelete { get; set; }
  }
}