using System.Text.Json.Serialization;

namespace DynamicAllowListingLib
{
  public class AllowOutboundGroup
  {
    [JsonPropertyName("resourceIds")]
    public string[]? ResourceIds { get; set; }
  }
}