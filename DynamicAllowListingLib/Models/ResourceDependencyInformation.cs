using Newtonsoft.Json;
using System;
using System.Text;
using System.Text.Json.Serialization;

namespace DynamicAllowListingLib
{
  public class ResourceDependencyInformation
  {
    /// <summary>
    /// We cannot store the resource id as is due to https://github.com/Azure/azure-cosmos-dotnet-v2/issues/35
    /// Hence we keep the encoded base as id.
    /// </summary>
    private string? _documentId;
    [JsonProperty(PropertyName = "id")] // this is needed by cosmosdb
    [JsonPropertyName("id")] // this is needed for the integration tests which serialize and pass objects
    public string? DocumentId
    {
      get
      {
        if (string.IsNullOrEmpty(_documentId) && ResourceId != null)
        {
          _documentId = GetDocumentId(ResourceId);
        }
        return _documentId;
      }
      set
      {
        _documentId = value;
      }
    }
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }
    [JsonPropertyName("printOut")]
    public string? PrintOut { get; set; }
    [JsonPropertyName("allowInbound")]
    public AllowInboundGroup? AllowInbound { get; set; }
    [JsonPropertyName("allowOutbound")]
    public AllowOutboundGroup? AllowOutbound { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] // need this for system.text.json to ignore
    [Newtonsoft.Json.JsonIgnore] // need this for cosmosdb to ignore this
    public string? RequestSubscriptionId
    {
      get
      {
        if (ResourceId == null || !ResourceId.Contains("/"))
        {
          return null;
        }
        var resourceIdParts = ResourceId.Split('/');
        return resourceIdParts.Length >= 2 ? resourceIdParts[2] : null;
      }
    }
    public string? ResourceName
    {
      get
      {
        if (ResourceId == null || !ResourceId.Contains("/"))
        {
          return null;
        }
        var resourceIdParts = ResourceId.Split('/');
        return resourceIdParts[resourceIdParts.Length - 1];
      }
    }
    public string? ResourceType
    {
      get
      {
        if (ResourceId == null || !ResourceId.Contains("/"))
        {
          return null;
        }
        var resourceIdParts = ResourceId.Split('/');
        return resourceIdParts.Length >= 7 ? $"{resourceIdParts[6]}/{resourceIdParts[7]}".ToLower() : null;
      }
    }
    public string? ResourceGroup
    {
      get
      {
        if (ResourceId == null || !ResourceId.Contains("/"))
        {
          return null;
        }

        var resourceIdParts = ResourceId.Split('/');
        return resourceIdParts.Length >= 4 ? resourceIdParts[4] : null;
      }
    }
    public static string GetDocumentId(string resourceId) => Convert.ToBase64String(Encoding.UTF8.GetBytes(resourceId));
    public static string GetSubscriptionId(string resourceId)
    {
      if (string.IsNullOrEmpty(resourceId)) return string.Empty;
      var resourceIdParts = resourceId.Split('/');
      return resourceIdParts.Length >= 2 ? resourceIdParts[2] : string.Empty;
    }
  }
}