using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicAllowListingLib
{
  public class IpSecurityRestrictionRule
  {
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; } = null;
    [JsonPropertyName("vnetSubnetResourceId")]
    public string? VnetSubnetResourceId { get; set; } = null;
    [JsonPropertyName("action")]
    public string? Action { get; set; } = "Allow";
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "Default";
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 500;
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; } = "Allow all access";
    [JsonPropertyName("headers")]
    public Dictionary<string, string[]>? Headers { get; set; }  
    public bool Matches(IpSecurityRestrictionRule other) => other.Name != null && (other.Equals(this) || other.Name.Equals(this.Name));
    public override int GetHashCode()
    {
      return new
      {
        IpAddress = IpAddress?.ToLower(),
        SubnetId = VnetSubnetResourceId?.ToLower()
      }.GetHashCode();
    }
    public override bool Equals(object? obj)
    {
      IpSecurityRestrictionRule? other = obj as IpSecurityRestrictionRule;
      if (other == null) return false;
      if (!string.IsNullOrEmpty(VnetSubnetResourceId) && !string.IsNullOrEmpty(other.VnetSubnetResourceId))
        return other.VnetSubnetResourceId!.Equals(VnetSubnetResourceId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(IpAddress) && !string.IsNullOrEmpty(other.IpAddress))
        return IpAddress! == other.IpAddress!;

      return false;
    }
    public override string ToString()
    {
      return $"{Name} {VnetSubnetResourceId} {IpAddress} {Action} {Tag} {Description} {Priority}";
    }
    public string VnetSubnetSubscriptionId
    {
      get
      {
        if (string.IsNullOrEmpty(this.VnetSubnetResourceId))
          return string.Empty;

        var split = this.VnetSubnetResourceId.Split('/');
        if (split.Length >= 2)
          return split[2].ToString();
        return string.Empty;
      }
    }
    // to be able to solve deep reference problem, clone object that you would like to add into a list!
    public IpSecurityRestrictionRule Clone()
    {
      return new IpSecurityRestrictionRule
      {
        Name = this.Name,
        Action = this.Action,
        Description = this.Description,
        IpAddress = this.IpAddress,
        Headers = this.Headers,
        Priority = this.Priority,
        Tag = this.Tag,
        VnetSubnetResourceId = this.VnetSubnetResourceId
      };
    }
  }
}