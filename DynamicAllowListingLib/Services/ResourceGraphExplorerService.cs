using DynamicAllowListingLib.Helpers;
using DynamicAllowListingLib.Logger;
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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static DynamicAllowListingLib.Models.ResourceGraphResponses.ResourceIds;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DynamicAllowListingLib
{
  public class ResourceGraphExplorerService : IResourceGraphExplorerService
  {
    private readonly IRestHelper _restHelper;
    private readonly IAzureResourceJsonConvertor _jsonConvertor;
    private readonly ILogger<ResourceGraphExplorerService> _logger;

    public ResourceGraphExplorerService(IRestHelper restHelper,
      IAzureResourceJsonConvertor jsonConvertor,
      ILogger<ResourceGraphExplorerService> logger)
    {
      _restHelper = restHelper;
      _jsonConvertor = jsonConvertor;
      _logger = logger;
    }

    public async Task<List<IAzureResource>> GetResourceInstances(string[] subscriptionIds, List<string> resourceIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetResourceInstances));
      var resources = new List<IAzureResource>();
      // Validate input parameters
      if (resourceIds == null || resourceIds.Count < 1)
      {
        var warningMessage = "No resource IDs provided for GetResourceInstances.";
        FunctionLogger.MethodWarning(_logger, warningMessage);
        return resources;
      }
      try
      {
        // Log the input parameters (subscription IDs and resource IDs)
        string subscriptionIdsLog = string.Join(",", subscriptionIds);
        FunctionLogger.MethodInformation(_logger, $"Starting GetResourceInstances for Subscription IDs: {subscriptionIdsLog} and Resource IDs: {string.Join(",", resourceIds)}");

        //build query
        StringBuilder queryBuilder = new StringBuilder();
        queryBuilder.Append("Resources | project id, name, location, type, properties | where id in~ (");
        queryBuilder.Append("'" + string.Join("','", resourceIds) + "'");
        queryBuilder.Append(")");

        // Log the query being executed
        FunctionLogger.MethodInformation(_logger, $"Executing Resource Graph Explorer query: {queryBuilder}");

        // Execute the query
        var response = await GetResourceGraphExplorerResponse(subscriptionIds, queryBuilder.ToString());
        resources = JsonConvert.DeserializeObject<List<IAzureResource>>(response, (JsonConverter)_jsonConvertor);

        // Log the number of resources retrieved
        if (resources != null)
        {
          FunctionLogger.MethodInformation(_logger, $"GetResourceInstances completed successfully. Resources retrieved: {resources.Count}");
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, "GetResourceInstances completed successfully. No resources were retrieved (resources is null).");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
      return resources ?? new List<IAzureResource>(); 
    }

    public async Task<string> GetResourceGraphExplorerResponse(string[] subscriptionIds, string query)
    {
      // Log method start
      FunctionLogger.MethodStart(_logger, nameof(GetResourceGraphExplorerResponse));
      string response = string.Empty;
      // Input validation and logging
      if (subscriptionIds == null || subscriptionIds.Length <= 0 || string.IsNullOrEmpty(query))
      {
        var errorMessage = "GetResourceGraphExplorerResponse failed due to invalid parameters.";
        FunctionLogger.MethodWarning(_logger, errorMessage);
      }
      try
      {
        FunctionLogger.MethodInformation(_logger, "Sending Resource Graph Explorer request");
        
        string url = "https://management.azure.com/providers/Microsoft.ResourceGraph/resources?api-version=2021-03-01";

        string subscriptionArray = string.Join(",", subscriptionIds!.Select(x => string.Format("\"{0}\"", x)).ToList()).Trim('"');
        string requestBody = "{" +
                             "\"subscriptions\": [\"" + subscriptionArray + "\"]," +
                             "\"query\": \"" + query + "\"" +
                             "}";

        FunctionLogger.MethodInformation(_logger, $"Resource graph explorer request body: {requestBody}");
        
        response = await _restHelper.DoPostAsJson(url, requestBody);
        // Log response status and size
        if (!string.IsNullOrEmpty(response))
        {
          FunctionLogger.MethodInformation(_logger, $"Resource Graph Explorer response received successfully, response size: {response.Length} characters.");
        }
        else
        {
          FunctionLogger.MethodWarning(_logger, "Resource Graph Explorer response was empty.");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw new Exception(ex.Message, ex);
      }
      return response;
    }

    public async Task<List<string>> GetExistingResourceIds(string subscriptionId, List<string> resourceList)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetExistingResourceIds));
      var existingResources = new List<string>();
      // Validate inputs
      if (string.IsNullOrEmpty(subscriptionId) || resourceList == null || !resourceList.Any())
      {
        var errorMessage = "Invalid parameters: subscriptionId or resourceList is null or empty.";
        FunctionLogger.MethodWarning(_logger, errorMessage);
      }
      try
      {
        // Log input parameters (excluding sensitive details)
        FunctionLogger.MethodInformation(_logger, $"Fetching existing resource IDs for subscription: {subscriptionId}, with {resourceList?.Count} resources to query.");

        //build query
        StringBuilder queryBuilder = new StringBuilder();
        queryBuilder.Append("Resources | project id | where id in~ (");
        queryBuilder.Append("'" + string.Join("','", resourceList!) + "'");
        queryBuilder.Append(")");

        // Log query construction (sanitize data if sensitive)
        FunctionLogger.MethodInformation(_logger, $"Constructed query for Resource Graph Explorer: {queryBuilder.ToString().Substring(0, Math.Min(queryBuilder.Length, 100))}...");

        var response = await GetResourceGraphExplorerResponse(new string[] { subscriptionId }, queryBuilder.ToString());

        FunctionLogger.MethodInformation(_logger, "Received response from Resource Graph Explorer");
        
        var queryResult = JsonConvert.DeserializeObject<ResourceIds>(response);

        if (queryResult?.data != null)
        {
          // If data exists, extract resource IDs
          existingResources.AddRange(queryResult.data.Select(datum => datum.id)); 
          FunctionLogger.MethodInformation(_logger, $"Successfully retrieved {existingResources.Count} existing resource IDs.");
        }
        else
        {
          FunctionLogger.MethodWarning(_logger, "No data found in Resource Graph Explorer response");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, "Failed to retrieve existing resource IDs");
        throw;
      }
      return existingResources;
    }

    public async Task<List<string>> GetExistingResourceIdsByType(string subscriptionId, List<string> azureResourceTypes)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetExistingResourceIdsByType));
      // Validate inputs
      if (string.IsNullOrEmpty(subscriptionId) || azureResourceTypes == null || !azureResourceTypes.Any())
      {
        var errorMessage = "Invalid parameters: subscriptionId or azureResourceTypes is null or empty.";
        FunctionLogger.MethodWarning(_logger, errorMessage);
      }
      var results = new List<string>();
      try
      {
        string types = string.Join(", ", azureResourceTypes!); 
        FunctionLogger.MethodInformation(_logger, $"Fetching existing resource IDs for Subscription ID: {subscriptionId} and Resource Types: {types}");

        // Get access token and set up ResourceGraphClient
        var token = await _restHelper.GetAccessToken("https://management.azure.com/");
        ServiceClientCredentials serviceClientCreds = new TokenCredentials(token);
        ResourceGraphClient resourceGraphClient = new ResourceGraphClient(serviceClientCreds);

        //build query
        StringBuilder queryBuilder = new StringBuilder();
        queryBuilder.Append("Resources | where type in~ (");
        queryBuilder.Append("'" + string.Join("','", azureResourceTypes!) + "'");
        queryBuilder.Append(") | project id");

        //run query
        var queryRequest = new QueryRequest(
            new string[] { subscriptionId },
            queryBuilder.ToString());
        var azureOperationResponse = await resourceGraphClient
          .ResourcesWithHttpMessagesAsync(queryRequest)
          .ConfigureAwait(false);

        if (azureOperationResponse != null)
        {
          // Log the query execution
          FunctionLogger.MethodInformation(_logger, $"Resource graph query executed. Query: {queryBuilder.ToString()}");

          // Check if the response data is null or empty before deserializing
          if (string.IsNullOrEmpty(azureOperationResponse?.Body?.Data?.ToString()))
          {
            _logger.LogWarning("Received empty or null response data. No resource IDs to fetch.");
          }
          else
          {
            var dataAsString = azureOperationResponse.Body?.Data?.ToString();
            if (string.IsNullOrEmpty(dataAsString))
            {
              _logger.LogWarning("Received empty or null response data. No resource IDs to fetch.");
            }
            else
            {
              var result = JsonConvert.DeserializeObject<Datum[]>(dataAsString);
              if (result != null && result.Any())
              {
                results.AddRange(result.Select(x => x.id).ToList());
                FunctionLogger.MethodInformation(_logger, $"Fetched {result.Length} resource IDs from the initial response.");
              }
              else
              {
                FunctionLogger.MethodWarning(_logger, "No valid resource IDs found in the response.");
              }
            }
          }

          // Handle pagination (if more results are available)
          while (!string.IsNullOrEmpty(azureOperationResponse?.Body?.SkipToken))
          {
            queryRequest.Options ??= new QueryRequestOptions();
            queryRequest.Options.SkipToken = azureOperationResponse.Body.SkipToken;

            azureOperationResponse = await resourceGraphClient.ResourcesWithHttpMessagesAsync(queryRequest).ConfigureAwait(false);

            // Deserialize the next page of results
            var dataAsString = azureOperationResponse.Body?.Data?.ToString();
            if (string.IsNullOrEmpty(dataAsString))
            {
              _logger.LogWarning("Received empty or null response data. No resource IDs to fetch.");
            }
            else
            {
              var pageResult = JsonConvert.DeserializeObject<Datum[]>(dataAsString);
              if (pageResult != null && pageResult.Any())
              {
                results.AddRange(pageResult.Select(x => x.id).ToList());
                FunctionLogger.MethodInformation(_logger, "Processed page of results with SkipToken.");
              }
              else
              {
                _logger.LogWarning("No valid resource IDs found on the next page.");
              }
            }
          }
          // Log completion of the method
          FunctionLogger.MethodInformation(_logger, $"Completed fetching resource IDs by type. Total resource IDs found: {results.Count}");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, "Failed to retrieve existing resource IDs by Type");
      }
      return results;
    }

    public async Task<bool> ResourceExists(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(ResourceExists));
      bool resourceExists = false;
      if (string.IsNullOrEmpty(resourceId))
      {
        var errorMessage = "Resource ID is null or empty, skipping existence check.";
        FunctionLogger.MethodWarning(_logger, errorMessage);
        return resourceExists;
      }
      try
      {
        // Extract subscription ID from resourceId
        string subscriptionId = StringHelper.GetSubscriptionId(resourceId);
        FunctionLogger.MethodInformation(_logger, $"Starting ResourceExists check for ResourceID: {resourceId}");
        // Fetch existing resources
        var existingResources = await GetExistingResourceIds(subscriptionId, new List<string> { resourceId });
        FunctionLogger.MethodInformation(_logger, $"Fetched {existingResources.Count} existing resource(s) for ResourceID: {resourceId}");

        // Check if the resource exists in the list of existing resources
        resourceExists = existingResources.Count > 0 &&
                        existingResources.FirstOrDefault()!.Equals(resourceId, StringComparison.OrdinalIgnoreCase);

        // Log the result of the existence check
        if (resourceExists)
        {
          FunctionLogger.MethodInformation(_logger, $"Resource exists for ResourceID: {resourceId}");
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, $"Resource does not exist for ResourceID: {resourceId}");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, "Failed to check if Resource Exists");
        throw;
      }
      return resourceExists;
    }

    public async Task<List<string>> GetAllSubnetIds(string subscriptionId)
    {
      string query = @"Resources | where type =~'Microsoft.Network/virtualNetworks'";

      var idList = new List<string>();
      var response = await GetResourceGraphExplorerResponse(new string[] { subscriptionId }, query);

      var queryResult = JsonConvert.DeserializeObject<VNets>(response);

      // The result is of the form 
      //{
      //  "data": {
      //    "rows": [
      //        [
      //            [
      //                {
      //                  "id": "/subscriptions/0f8b9bd9-53ae-4493-9477-e048ca720641/resourceGroups/rgngdshrg01/providers/Microsoft.Network/virtualNetworks/nvnetngdshrg0101/subnets/nsbnt-funapngdfdas0101"
      //                }
      //            ]
      //        ]
      //    ]
      //}
      //}
      if (queryResult != null)
      {
        foreach (var row in queryResult.data)
        {
          idList.AddRange(row.properties.subnets.Select(subnet => subnet.id));
        }
      }

      return idList;
    }

    public async Task<HashSet<string>> GetResourcesHostedOnPlan(string appServicePlanResourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetResourcesHostedOnPlan));        
      // Validate the input parameter
      if (string.IsNullOrEmpty(appServicePlanResourceId))
      {
        string errorMessage = "AppServicePlanResourceId is null or empty.";
        FunctionLogger.MethodWarning(_logger, errorMessage);
        throw new ArgumentException(errorMessage);
      }
      HashSet<string> resourceIdsOnAppServicePlan = new HashSet<string>();
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Starting GetResourcesHostedOnPlan with AppServicePlanResourceId: {appServicePlanResourceId}");

        string query = "Resources | where properties.serverFarmId =~ '" + appServicePlanResourceId + "' | project id";
        // Get the subscription ID from the app service plan resource ID
        string subscriptionId = StringHelper.GetSubscriptionId(appServicePlanResourceId);
        // Log the constructed query and the subscription ID being used
        FunctionLogger.MethodInformation(_logger, $"Constructed query: {query}");
        FunctionLogger.MethodInformation(_logger, $"Using Subscription ID: {subscriptionId}");

        var response = await GetResourceGraphExplorerResponse(new string[] { subscriptionId }, query);
        // Log the response received from the Resource Graph Explorer
        FunctionLogger.MethodInformation(_logger, $"Received response from Resource Graph Explorer: {response}");

        var queryResult = JsonConvert.DeserializeObject<ResourceIds>(response);

        if (queryResult?.data != null)
        {
          resourceIdsOnAppServicePlan.UnionWith(queryResult.data.Select(datum => datum.id));
          FunctionLogger.MethodInformation(_logger, $"Successfully processed {queryResult?.data?.Count()} resources from the response.");
        }
        else
        {
          FunctionLogger.MethodWarning(_logger, "No resources found in the response.");
        }
        FunctionLogger.MethodInformation(_logger, $"GetResourcesHostedOnPlan completed successfully ResourcesFoundCount : {resourceIdsOnAppServicePlan.Count.ToString()}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
      return resourceIdsOnAppServicePlan;
    }

    public async Task<Dictionary<string, string>> GetFrontDoorUniqueInstanceIds(List<string> resourceIds)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetFrontDoorUniqueInstanceIds));
      var frontDoorUniqueInstanceIds = new Dictionary<string, string>();
      try
      {
        var distinctIds = resourceIds.Distinct().ToList();
        if (!distinctIds.Any())
        {
          FunctionLogger.MethodWarning(_logger, "No distinct resource IDs found for Front Door instance retrieval.");
          return frontDoorUniqueInstanceIds;
        }
        FunctionLogger.MethodInformation(_logger, $"Starting GetFrontDoorUniqueInstanceIds with ResourceIdsCount: {distinctIds.Count.ToString()}");
        // Build the query to get Front Door resources by their IDs.
        StringBuilder queryBuilder = new StringBuilder();
        queryBuilder.Append("Resources | where type =~ 'Microsoft.Network/frontDoors' and id in~ (");
        queryBuilder.Append("'" + string.Join("','", distinctIds) + "'");
        queryBuilder.Append(") | project id, FDID = properties.frontdoorId");

        // Log the generated query
        FunctionLogger.MethodInformation(_logger, $"Generated query for Resource Graph Explorer: {queryBuilder.ToString()}");

        // Extract subscription IDs from the resource IDs.
        var subscriptionIds = StringHelper.GetSubscriptionIds(distinctIds.ToArray());
        var response = await GetResourceGraphExplorerResponse(subscriptionIds, queryBuilder.ToString());

        // Log the response length (for debugging purposes)
        FunctionLogger.MethodInformation(_logger, $"Received response from Resource Graph Explorer. Response length: {response.Length} characters.");

        // Deserialize the response and extract the required data.
        var queryResult = JsonConvert.DeserializeObject<FrontDoorGraphResult>(response);

        // Map the resource ID to the unique Front Door instance ID.
        if (queryResult != null)
        {
          foreach (var data in queryResult.data)
          {
            var resourceId = data.id;
            var uniqueInstanceId = data.FDID; // Using FDID from the new model
            if (string.IsNullOrEmpty(resourceId) || string.IsNullOrEmpty(uniqueInstanceId))
            {
              // Log which resource is missing an ID or FDID
              FunctionLogger.MethodError(_logger, $"Missing resource ID or FDID for resource: {resourceId}");
              throw new MissingMemberException($"FrontDoor IDs or FDID is missing for resource: {resourceId}");
            }
            frontDoorUniqueInstanceIds[resourceId] = uniqueInstanceId;
          }
        }
        FunctionLogger.MethodInformation(_logger, "GetFrontDoorUniqueInstanceIds completed successfully");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
      return frontDoorUniqueInstanceIds;
    }
  }
}