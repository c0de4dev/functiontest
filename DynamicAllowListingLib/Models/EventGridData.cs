using Newtonsoft.Json;

namespace DynamicAllowListingLib.Models
{
  public class EventGridData
  {
    [JsonProperty(PropertyName = "subject")]
    public string? ResourceId { get; set; }
  }
}