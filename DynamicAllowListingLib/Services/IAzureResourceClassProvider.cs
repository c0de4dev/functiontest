using DynamicAllowListingLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamicAllowListingLib.Services
{
  public interface IAzureResourceClassProvider
  {
    public IAzureResource GetResourceClass(string resourceType);
  }
  public class AzureResourceClassProvider : IAzureResourceClassProvider
  {
    private IEnumerable<IAzureResource> _resourceInstances;
    public AzureResourceClassProvider(IEnumerable<IAzureResource> resourceInstances)
    {
      _resourceInstances = resourceInstances;
    }
    public IAzureResource GetResourceClass(string resourceType)
    {
      /*
      var instance = _resourceInstances.Where(x => x.Type == resourceType);
      if (instance.Any())
      {
        return instance.First();
      }
      */
      // Find the resource instance matching the resource type
      var instance = _resourceInstances.FirstOrDefault(x => x.Type == resourceType);

      if (instance != null)
      {
        return instance;
      }
      throw new NotImplementedException($"Azure Resource Type is not implemented! Type: {resourceType}.");
    }
  }
}