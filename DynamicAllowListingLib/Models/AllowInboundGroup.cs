using System.Text.Json.Serialization;

namespace DynamicAllowListingLib
{
  public class AllowInboundGroup
  {
    [JsonPropertyName("securityRestrictions")]
    public SecurityRestrictions? SecurityRestrictions { get; set; }

    [JsonPropertyName("scmSecurityRestrictions")]
    public SecurityRestrictions? ScmSecurityRestrictions { get; set; }
  }
  public class SecurityRestrictions
  {
    [JsonPropertyName("resourceIds")]
    public string[]? ResourceIds { get; set; }

    [JsonPropertyName("azureServiceTags")]
    public string[]? AzureServiceTags { get; set; }

    [JsonPropertyName("newDayInternalAndThirdPartyTags")]
    public string[]? NewDayInternalAndThirdPartyTags { get; set; }
  }
}