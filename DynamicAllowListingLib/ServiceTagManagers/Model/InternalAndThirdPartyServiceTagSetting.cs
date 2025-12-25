using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.ServiceTagManagers.Model
{
  public class InternalAndThirdPartyServiceTagSetting : IEquatable<InternalAndThirdPartyServiceTagSetting>
  {
    public List<AzureSubscription> AzureSubscriptions { get; set; } = new List<AzureSubscription>();
    public List<ServiceTag> ServiceTags { get; set; } = new List<ServiceTag>();

    public bool Equals(InternalAndThirdPartyServiceTagSetting? other)
    {
      if (ReferenceEquals(null, other))
      {
        return false;
      }
      if (ReferenceEquals(this, other))
      {
        return true;
      }
      return ListHelper<AzureSubscription>.AreEqual(AzureSubscriptions, other.AzureSubscriptions)
             && ListHelper<ServiceTag>.AreEqual(ServiceTags, other.ServiceTags);
    }
    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      if (obj.GetType() != this.GetType())
      {
        return false;
      }
      return Equals((InternalAndThirdPartyServiceTagSetting)obj);
    }
    public override int GetHashCode()
    {
      return HashCode.Combine(AzureSubscriptions, ServiceTags);
    }
  }
  public class AzureSubscription : IEquatable<AzureSubscription>
  {
    [JsonProperty(PropertyName = "id")] // this is needed by cosmosdb
    public string? Id { get; set; }
    [JsonProperty(PropertyName = "azureSubscriptions")] // this is needed by cosmosdb
    public string? CosmosPartitionKey { get; set; } = "azureSubscriptions";
    public string? Name { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool Equals(AzureSubscription? other)
    {
      if (ReferenceEquals(null, other))
      {
        return false;
      }
      if (ReferenceEquals(this, other))
      {
        return true;
      }
      return Id == other.Id && Name == other.Name;
    }
    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      if (obj.GetType() != this.GetType())
      {
        return false;
      }
      return Equals((AzureSubscription)obj);
    }
    public override int GetHashCode()
    {
      return HashCode.Combine(Id, Name);
    }
  }
  public class ServiceTag : IEquatable<ServiceTag>
  {
    [JsonProperty(PropertyName = "id")] // this is needed by cosmosdb
    public string? Id
    {
      get
      {
        return Name;
      }
      set
      {
        Name = value;
      }
    }
    public string? Name { get; set; }
    public bool IsDeleted { get; set; } = false;
    [JsonProperty(PropertyName = "servicetags")] // this is needed by cosmosdb
    public string? CosmosPartitionKey { get; set; } = "servicetags";
    public List<string?> AddressPrefixes { get; set; } = new List<string?>();
    public List<string?> SubnetIds { get; set; } = new List<string?>();
    public List<ServiceTagSubscription> AllowedSubscriptions { get; set; } = new List<ServiceTagSubscription>();
    public bool Equals(ServiceTag? other)
    {
      if (ReferenceEquals(null, other))
      {
        return false;
      }
      if (ReferenceEquals(this, other))
      {
        return true;
      }
      return Name == other.Name
             && IsDeleted == other.IsDeleted
             && ListHelper<string?>.AreEqual(AddressPrefixes, other.AddressPrefixes)
             && ListHelper<string?>.AreEqual(SubnetIds, other.SubnetIds)
             && ListHelper<ServiceTagSubscription>.AreEqual(AllowedSubscriptions, other.AllowedSubscriptions);
    }
    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      if (obj.GetType() != this.GetType())
      {
        return false;
      }
      return Equals((ServiceTag)obj);
    }
    public override int GetHashCode()
    {
      return HashCode.Combine(Name, AddressPrefixes, AllowedSubscriptions);
    }
  }
  public class ServiceTagSubscription : IEquatable<ServiceTagSubscription>
  {
    public string? SubscriptionName { get; set; }
    public bool IsMandatory { get; set; }
    public bool Equals(ServiceTagSubscription? other)
    {
      if (ReferenceEquals(null, other))
      {
        return false;
      }
      if (ReferenceEquals(this, other))
      {
        return true;
      }
      return SubscriptionName == other.SubscriptionName && IsMandatory == other.IsMandatory;
    }
    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      if (obj.GetType() != this.GetType())
      {
        return false;
      }
      return Equals((ServiceTagSubscription)obj);
    }
    public override int GetHashCode()
    {
      return HashCode.Combine(SubscriptionName, IsMandatory);
    }
  }
}