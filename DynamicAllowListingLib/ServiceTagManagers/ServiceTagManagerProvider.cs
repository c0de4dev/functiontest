using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamicAllowListingLib.ServiceTagManagers
{
  public interface IServiceTagManagerProvider
  {
    public IServiceTagManager GetServiceTagManager(ManagerType managerType);
  }

  public class ServiceTagManagerProvider : IServiceTagManagerProvider
  {
    private Dictionary<ManagerType, IServiceTagManager> _dicInstances;

    public ServiceTagManagerProvider(IEnumerable<IServiceTagManager> instanceList)
    {
      _dicInstances = instanceList.ToDictionary(x => x.SupportedManager);
    }

    public IServiceTagManager GetServiceTagManager(ManagerType managerType)
    {
      if (_dicInstances.TryGetValue(managerType, out var instance))
      {
        return instance;
      }

      throw new NotImplementedException($"Manager Type not implemented! Manager Type: {managerType.ToString()}");
    }
  }
}