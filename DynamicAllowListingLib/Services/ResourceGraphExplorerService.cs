using DynamicAllowListingLib.Helpers;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.AzureResources;
using DynamicAllowListingLib.Models.ResourceGraphResponses;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DynamicAllowListingLib.Models.ResourceGraphResponses.ResourceIds;

namespace DynamicAllowListingLib
{
  public class ResourceGraphExplorerService : IResourceGraphExplorerService
  {
    private readonly IRestHelper _restHelper;
    private readonly IAzureResourceJsonConvertor _jsonConvertor;
    private readonly ILogger<ResourceGraphExplorerService> _logger;

    public ResourceGraphExplorerService(
        IRestHelper restHelper,
        IAzureResourceJsonConvertor jsonConvertor,
        ILogger<ResourceGraphExplorerService> logger)
    {
      _restHelper = restHelper ?? throw new ArgumentNullException(nameof(restHelper));
      _jsonConvertor = jsonConvertor ?? throw new ArgumentNullException(nameof(jsonConvertor));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<IAzureResource>> GetResourceInstances(string[] subscriptionIds, List<string> resourceIds)
    {
      var resources = new List<IAzureResource>();

      using (_logger.BeginResourceGraphScope(nameof(GetResourceInstances), subscriptionIds))
      {
        // Validate input parameters
        if (resourceIds == null || resourceIds.Count < 1)
        {
          _logger.LogNoResourceIdsProvided();
          return resources;
        }

        _logger.LogGetResourceInstancesStart(subscriptionIds?.Length ?? 0, resourceIds.Count);

        try
        {
          // Build query
          StringBuilder queryBuilder = new StringBuilder();
          queryBuilder.Append("Resources | project id, name, location, type, properties | where id in~ (");
          queryBuilder.Append("'" + string.Join("','", resourceIds) + "'");
          queryBuilder.Append(")");

          _logger.LogExecutingQuery(queryBuilder.Length);

          // Ensure subscriptionIds is not null before passing to GetResourceGraphExplorerResponse
          var safeSubscriptionIds = subscriptionIds ?? Array.Empty<string>();

          var response = await GetResourceGraphExplorerResponse(safeSubscriptionIds, queryBuilder.ToString());
          var deserializedResources = JsonConvert.DeserializeObject<List<IAzureResource>>(response, (JsonConverter)_jsonConvertor);

          if (deserializedResources != null)
          {
            resources = deserializedResources;
            _logger.LogGetResourceInstancesComplete(resources.Count);
          }
          else
          {
            _logger.LogNullResourcesReturned();
          }

        }
        catch (Exception ex)
        {
          _logger.LogGetResourceInstancesFailed(ex);
          throw;
        }
      }

      return resources;
    }

    public async Task<string> GetResourceGraphExplorerResponse(string[] subscriptionIds, string query)
    {
      string response = string.Empty;

      using (_logger.BeginResourceGraphScope(nameof(GetResourceGraphExplorerResponse), subscriptionIds))
      {

        // Input validation
        if (subscriptionIds == null || subscriptionIds.Length <= 0 || string.IsNullOrEmpty(query))
        {
          _logger.LogInvalidResourceGraphParameters(
              subscriptionIds != null && subscriptionIds.Length > 0,
              !string.IsNullOrEmpty(query));
          return response;
        }

        _logger.LogResourceGraphRequestStart(subscriptionIds.Length);

        try
        {
          string url = "https://management.azure.com/providers/Microsoft.ResourceGraph/resources?api-version=2021-03-01";

          string subscriptionArray = string.Join(",", subscriptionIds.Select(x => string.Format("\"{0}\"", x)).ToList()).Trim('"');
          string requestBody = "{" +
                               "\"subscriptions\": [\"" + subscriptionArray + "\"]," +
                               "\"query\": \"" + query + "\"" +
                               "}";

          _logger.LogResourceGraphRequestBody(requestBody.Length);

          // Fix CS8600: Handle potential null response from DoPostAsJson
          var apiResponse = await _restHelper.DoPostAsJson(url, requestBody);
          response = apiResponse ?? string.Empty;

          if (!string.IsNullOrEmpty(response))
          {
            _logger.LogResourceGraphResponseReceived(response.Length);
          }
          else
          {
            _logger.LogEmptyResourceGraphResponse();
          }
        }
        catch (Exception ex)
        {
          _logger.LogResourceGraphRequestFailed(ex);
          throw new Exception(ex.Message, ex);
        }
      }

      // Fix CS8603: response is guaranteed to be non-null (initialized to string.Empty)
      return response;
    }

    public async Task<List<string>> GetExistingResourceIds(string subscriptionId, List<string> resourceList)
    {
      var existingResources = new List<string>();

      using (_logger.BeginQueryScope("GetExistingResourceIds", subscriptionId ?? "Unknown"))
      {

        // Validate inputs
        if (string.IsNullOrEmpty(subscriptionId) || resourceList == null || !resourceList.Any())
        {
          _logger.LogInvalidGetExistingResourceIdsParameters(
              !string.IsNullOrEmpty(subscriptionId),
              resourceList != null && resourceList.Any());
          return existingResources;
        }

        _logger.LogGetExistingResourceIdsStart(subscriptionId, resourceList.Count);

        try
        {
          // Build query
          StringBuilder queryBuilder = new StringBuilder();
          queryBuilder.Append("Resources | project id | where id in~ (");
          queryBuilder.Append("'" + string.Join("','", resourceList) + "'");
          queryBuilder.Append(")");

          _logger.LogExecutingQuery(queryBuilder.Length);

          var response = await GetResourceGraphExplorerResponse(new string[] { subscriptionId }, queryBuilder.ToString());

          var queryResult = JsonConvert.DeserializeObject<ResourceIds>(response);

          if (queryResult?.data != null)
          {
            existingResources.AddRange(queryResult.data.Select(datum => datum.id));
          }
          else
          {
            _logger.LogNoDataInResourceGraphResponse();
          }
          _logger.LogGetExistingResourceIdsComplete(existingResources.Count);
        }
        catch (Exception ex)
        {
          _logger.LogGetExistingResourceIdsFailed(ex, subscriptionId);
          throw;
        }
      }

      return existingResources;
    }

    public async Task<List<string>> GetExistingResourceIdsByType(string subscriptionId, List<string> azureResourceTypes)
    {
      var results = new List<string>();

      using (_logger.BeginQueryScope("GetExistingResourceIdsByType", subscriptionId ?? "Unknown"))
      {

        // Validate inputs
        if (string.IsNullOrEmpty(subscriptionId) || azureResourceTypes == null || !azureResourceTypes.Any())
        {
          _logger.LogInvalidGetExistingResourceIdsByTypeParameters(
              !string.IsNullOrEmpty(subscriptionId),
              azureResourceTypes != null && azureResourceTypes.Any());
          return results;
        }

        _logger.LogGetExistingResourceIdsByTypeStart(subscriptionId, azureResourceTypes.Count);

        try
        {
          // Get access token and set up ResourceGraphClient
          var token = await _restHelper.GetAccessToken("https://management.azure.com/");
          ServiceClientCredentials serviceClientCreds = new TokenCredentials(token);
          ResourceGraphClient resourceGraphClient = new ResourceGraphClient(serviceClientCreds);

          // Build query
          StringBuilder queryBuilder = new StringBuilder();
          queryBuilder.Append("Resources | where type in~ (");
          queryBuilder.Append("'" + string.Join("','", azureResourceTypes) + "'");
          queryBuilder.Append(") | project id");

          // Log query preview (first 100 chars for security)
          var queryPreview = queryBuilder.ToString();
          _logger.LogResourceGraphQueryExecuted(queryPreview.Length > 100 ? queryPreview.Substring(0, 100) + "..." : queryPreview);

          // Run query
          var queryRequest = new QueryRequest(
              new string[] { subscriptionId },
              queryBuilder.ToString());
          var azureOperationResponse = await resourceGraphClient
              .ResourcesWithHttpMessagesAsync(queryRequest)
              .ConfigureAwait(false);

          if (azureOperationResponse != null)
          {
            // Check if the response data is null or empty before deserializing
            var dataAsString = azureOperationResponse.Body?.Data?.ToString();
            if (string.IsNullOrEmpty(dataAsString))
            {
              _logger.LogEmptyOrNullResponseData();
            }
            else
            {
              var result = JsonConvert.DeserializeObject<Datum[]>(dataAsString);
              if (result != null && result.Any())
              {
                results.AddRange(result.Select(x => x.id).ToList());
                _logger.LogInitialResponseReceived(result.Length);
              }
              else
              {
                _logger.LogNoValidResourceIdsInResponse();
              }
            }

            // Handle pagination (if more results are available)
            while (!string.IsNullOrEmpty(azureOperationResponse?.Body?.SkipToken))
            {
              _logger.LogProcessingPagination();

              queryRequest.Options ??= new QueryRequestOptions();
              queryRequest.Options.SkipToken = azureOperationResponse.Body.SkipToken;

              azureOperationResponse = await resourceGraphClient
                  .ResourcesWithHttpMessagesAsync(queryRequest)
                  .ConfigureAwait(false);

              // Deserialize the next page of results
              dataAsString = azureOperationResponse.Body?.Data?.ToString();
              if (string.IsNullOrEmpty(dataAsString))
              {
                _logger.LogEmptyOrNullResponseData();
              }
              else
              {
                var pageResult = JsonConvert.DeserializeObject<Datum[]>(dataAsString);
                if (pageResult != null && pageResult.Any())
                {
                  results.AddRange(pageResult.Select(x => x.id).ToList());
                  _logger.LogPaginationPageProcessed(pageResult.Length);
                }
                else
                {
                  _logger.LogNoValidResourceIdsInResponse();
                }
              }
            }
          }
          _logger.LogGetExistingResourceIdsByTypeComplete(results.Count);
        }
        catch (Exception ex)
        {
          _logger.LogGetExistingResourceIdsByTypeFailed(ex, subscriptionId);
          throw;
        }
      }

      return results;
    }

    public async Task<bool> ResourceExists(string resourceId)
    {
      bool resourceExists = false;

      using (_logger.BeginResourceExistsScope(resourceId ?? "Unknown"))
      {

        if (string.IsNullOrEmpty(resourceId))
        {
          _logger.LogResourceIdNullOrEmpty();
          return resourceExists;
        }

        _logger.LogResourceExistsStart(resourceId);

        try
        {
          // Extract subscription ID from resourceId
          string subscriptionId = StringHelper.GetSubscriptionId(resourceId);

          // Fetch existing resources
          var existingResources = await GetExistingResourceIds(subscriptionId, new List<string> { resourceId });

          // Check if the resource exists in the list of existing resources
          resourceExists = existingResources.Count > 0 &&
                           existingResources.FirstOrDefault()!.Equals(resourceId, StringComparison.OrdinalIgnoreCase);

          _logger.LogResourceExistsComplete(resourceId, resourceExists);
        }
        catch (Exception ex)
        {
          _logger.LogResourceExistsFailed(ex, resourceId);
          throw;
        }
      }

      return resourceExists;
    }

    public async Task<List<string>> GetAllSubnetIds(string subscriptionId)
    {
      var idList = new List<string>();

      using (_logger.BeginQueryScope("GetAllSubnetIds", subscriptionId ?? "Unknown"))
      {
        _logger.LogGetAllSubnetIdsStart(subscriptionId ?? "Unknown");

        try
        {
          string query = @"Resources | where type =~'Microsoft.Network/virtualNetworks'";

          var response = await GetResourceGraphExplorerResponse(new string[] { subscriptionId }, query);

          var queryResult = JsonConvert.DeserializeObject<VNets>(response);

          // Fix CS8601: Handle potential null queryResult.data
          if (queryResult?.data != null)
          {
            foreach (var row in queryResult.data)
            {
              if (row?.properties?.subnets != null)
              {
                idList.AddRange(row.properties.subnets.Select(subnet => subnet.id));
              }
            }
          }

          _logger.LogGetAllSubnetIdsComplete(idList.Count);
        }
        catch (Exception ex)
        {
          _logger.LogGetAllSubnetIdsFailed(ex, subscriptionId ?? "Unknown");
          throw;
        }
      }

      return idList;
    }

    public async Task<HashSet<string>> GetResourcesHostedOnPlan(string appServicePlanResourceId)
    {
      HashSet<string> resourceIdsOnAppServicePlan = new HashSet<string>();

      using (_logger.BeginResourceGraphScope(nameof(GetResourcesHostedOnPlan)))
      {
        // Validate the input parameter
        if (string.IsNullOrEmpty(appServicePlanResourceId))
        {
          _logger.LogAppServicePlanResourceIdEmpty();
          throw new ArgumentException("AppServicePlanResourceId is null or empty.", nameof(appServicePlanResourceId));
        }

        _logger.LogGetResourcesHostedOnPlanStart(appServicePlanResourceId);

        try
        {
          string query = "Resources | where properties.serverFarmId =~ '" + appServicePlanResourceId + "' | project id";

          // Get the subscription ID from the app service plan resource ID
          string subscriptionId = StringHelper.GetSubscriptionId(appServicePlanResourceId);

          var response = await GetResourceGraphExplorerResponse(new string[] { subscriptionId }, query);

          var queryResult = JsonConvert.DeserializeObject<ResourceIds>(response);

          if (queryResult?.data != null)
          {
            resourceIdsOnAppServicePlan.UnionWith(queryResult.data.Select(datum => datum.id));
          }
          else
          {
            _logger.LogNoResourcesFoundOnPlan();
          }
          _logger.LogGetResourcesHostedOnPlanComplete(resourceIdsOnAppServicePlan.Count);
        }
        catch (Exception ex)
        {
          _logger.LogGetResourcesHostedOnPlanFailed(ex, appServicePlanResourceId);
          throw;
        }
      }

      return resourceIdsOnAppServicePlan;
    }

    public async Task<Dictionary<string, string>> GetFrontDoorUniqueInstanceIds(List<string> resourceIds)
    {
      var frontDoorUniqueInstanceIds = new Dictionary<string, string>();

      using (_logger.BeginResourceGraphScope(nameof(GetFrontDoorUniqueInstanceIds)))
      {
        try
        {
          var distinctIds = resourceIds.Distinct().ToList();
          if (!distinctIds.Any())
          {
            _logger.LogNoDistinctFrontDoorIds();
            return frontDoorUniqueInstanceIds;
          }

          _logger.LogGetFrontDoorIdsStart(distinctIds.Count);

          // Build the query to get Front Door resources by their IDs
          StringBuilder queryBuilder = new StringBuilder();
          queryBuilder.Append("Resources | where type =~ 'Microsoft.Network/frontDoors' and id in~ (");
          queryBuilder.Append("'" + string.Join("','", distinctIds) + "'");
          queryBuilder.Append(") | project id, FDID = properties.frontdoorId");

          _logger.LogExecutingQuery(queryBuilder.Length);

          // Extract subscription IDs from the resource IDs
          var subscriptionIds = StringHelper.GetSubscriptionIds(distinctIds.ToArray());
          var response = await GetResourceGraphExplorerResponse(subscriptionIds, queryBuilder.ToString());

          // Deserialize the response and extract the required data
          var queryResult = JsonConvert.DeserializeObject<FrontDoorGraphResult>(response);

          // Map the resource ID to the unique Front Door instance ID
          if (queryResult?.data != null)
          {
            foreach (var data in queryResult.data)
            {
              var resourceId = data.id;
              var uniqueInstanceId = data.FDID;

              if (string.IsNullOrEmpty(resourceId) || string.IsNullOrEmpty(uniqueInstanceId))
              {
                _logger.LogMissingFrontDoorIdOrFdid(resourceId ?? "Unknown");
                throw new MissingMemberException($"FrontDoor IDs or FDID is missing for resource: {resourceId}");
              }

              frontDoorUniqueInstanceIds[resourceId] = uniqueInstanceId;
            }
          }
          _logger.LogGetFrontDoorIdsComplete(frontDoorUniqueInstanceIds.Count);
        }
        catch (Exception ex)
        {
          _logger.LogGetFrontDoorIdsFailed(ex);
          throw;
        }
      }

      return frontDoorUniqueInstanceIds;
    }

    public async Task<List<WebSite>> GetWebAppSlots(string webAppId)
    {
      var slots = new List<WebSite>();

      using (_logger.BeginResourceGraphScope(nameof(GetWebAppSlots)))
      {
        if (string.IsNullOrEmpty(webAppId))
        {
          _logger.LogWebAppIdNullOrEmpty();
          return slots;
        }

        _logger.LogGetWebAppSlotsStart(webAppId);

        try
        {
          // Extract subscription ID from webAppId
          string subscriptionId = StringHelper.GetSubscriptionId(webAppId);

          // Build query to find slots for this web app
          string query = $"Resources | where type =~ 'Microsoft.Web/sites/slots' and id startswith '{webAppId}/slots' | project id, name, location, type, properties";

          var response = await GetResourceGraphExplorerResponse(new string[] { subscriptionId }, query);

          var queryResult = JsonConvert.DeserializeObject<List<WebSite>>(response, (JsonConverter)_jsonConvertor);

          if (queryResult != null)
          {
            slots.AddRange(queryResult);
          }
          _logger.LogGetWebAppSlotsComplete(webAppId, slots.Count);
        }
        catch (Exception ex)
        {
          _logger.LogGetWebAppSlotsFailed(ex, webAppId);
          throw;
        }
      }
      return slots;
    }
  }
}