using DynamicAllowListingLib.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicAllowListingLib
{
  public interface IResourceGraphExplorerService
  {
    /// <summary>
    /// Perform an Azure resource graph query.
    /// Wrapper around
    /// https://docs.microsoft.com/en-us/rest/api/azureresourcegraph/resourcegraph(2019-04-01)/resources/resources
    /// </summary>
    /// <param name="subscriptionId">Azure subscription id to perform query in.</param>
    /// <param name="query">The query as string.</param>
    /// <returns>Results as json string or null if unable to get result.</returns>
    public Task<string> GetResourceGraphExplorerResponse(string[] subscriptionId, string query);
    /// <summary>
    /// Get the list of resources that exist in the provided azure subscription.
    /// The generated list is subset of the one passed in as parameter to the function.
    /// </summary>
    /// <param name="subscriptionId">Azure subscription id to check within.</param>
    /// <param name="resourceList">List of azure resource ids to check for existence.</param>
    /// <returns><see cref="List{string}"/> of azure resource ids.</returns>
    public Task<List<string>> GetExistingResourceIds(string subscriptionId, List<string> resourceList);
    /// <summary>
    /// Get resource information as objects
    /// </summary>
    /// <param name="subscriptionIds">to check within.</param>
    /// <param name="resourceIds">Resource Ids to check with</param>
    /// <returns></returns>
    public Task<List<IAzureResource>> GetResourceInstances(string[] subscriptionIds, List<string> resourceIds);
    /// <summary>
    /// Get list of all subnet ids in given subscriptionId
    /// </summary>
    /// <param name="subscriptionId">to check within.</param>
    /// <returns></returns>
    public Task<List<string>> GetAllSubnetIds(string subscriptionId);
    /// <summary>
    /// Get ids of resources which are hosted in provided app service plan resource id
    /// </summary>
    /// <param name="appServicePlanResourceId">Azure resource id of app service plan</param>
    /// <returns>Resource ids as string HashSet.</returns>
    public Task<HashSet<string>> GetResourcesHostedOnPlan(string appServicePlanResourceId);
    /// <summary>
    /// Check if provided Azure resource id exists.
    /// </summary>
    /// <param name="resourceId">Azure resource id</param>
    /// <returns>True if resource exists else false.</returns>
    public Task<bool> ResourceExists(string resourceId);
    public Task<List<string>> GetExistingResourceIdsByType(string subscriptionId, List<string> azureResourceTypes);
    /// <summary>
    /// Get FrontDoor Ids
    /// </summary>
    /// <param name="resourceId">Frontdoor resource ids</param>
    /// <returns>ResourceId & FDID pairs</returns>
    public Task<Dictionary<string,string>> GetFrontDoorUniqueInstanceIds(List<string> resourceId);
  }
}