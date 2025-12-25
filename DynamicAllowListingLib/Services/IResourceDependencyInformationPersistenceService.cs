using DynamicAllowListingLib.ServiceTagManagers.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAllowListingLib
{
  public interface IResourceDependencyInformationPersistenceService
  {
    /// <summary>
    /// Remove all references to resource id from database. The record specific to the resourceId as well as anywhere it might be used as a dependency.
    /// </summary>
    /// <param name="resourceId">Azure resource id in format of /subscription/...</param>
    /// <returns>HasSet of documents replaced in database after resourceId has been removed from them.</returns>
    Task<HashSet<ResourceDependencyInformation>> RemoveConfigAndDependencies(string resourceId);
    Task RemoveConfig(string resourceId);
    Task<ResultObject> CreateOrReplaceItemInDb(ResourceDependencyInformation resourceDependencyInformation);
    /// <summary>
    /// Get resource ids where the provided resource id is present in outbound.
    /// </summary>
    /// <param name="resourceId"></param>
    /// <returns></returns>
    Task<string[]> GetResourceIdsWhereOutbound(string resourceId);
    /// <summary>
    /// Get <see cref="ResourceDependencyInformation"/> where the provided resource id is present in inbound.
    /// </summary>
    /// <param name="resourceId">Resource id to check</param>
    /// <returns>HashSet of configs from database.</returns>
    Task<HashSet<ResourceDependencyInformation>> GetConfigsWhereInbound(string resourceId);
    /// <summary>
    /// Get ResourceDependencyInformation by ResourceId
    /// </summary>
    /// <param name="resourceId"></param>
    /// <returns></returns>
    Task<ResourceDependencyInformation?> GetResourceDependencyInformation(string resourceId);
    /// <summary>
    /// Find records that are referencing given Service Tag in its AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags array
    /// </summary>
    /// <param name="serviceTag">Service Tag that will be looking for in NewDayInternalAndThirdPartyTags</param>
    /// <returns></returns>
    Task<List<ResourceDependencyInformation>> FindByInternalAndThirdPartyTagName(ServiceTag serviceTag);
    /// <summary>
    /// Get all records in collection
    /// </summary>
    /// <returns></returns>
    Task<List<ResourceDependencyInformation>> GetAll();
    /// <summary>
    /// Get first record of collection
    /// </summary>
    /// <returns></returns>
    Task<ResourceDependencyInformation> GetFirstOrDefault();
  }
}
