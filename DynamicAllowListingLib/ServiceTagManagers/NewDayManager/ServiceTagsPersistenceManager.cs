using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.ServiceTagManagers.NewDayManager
{
  public class ServiceTagsPersistenceManager : IPersistenceManager<ServiceTag>
  {
    private const string ServiceTagsContainerName = "ServiceTags";
    private const string ServiceTagsPartitionKey = "servicetags";

    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<ServiceTagsPersistenceManager> _logger;

    public ServiceTagsPersistenceManager(CosmosClient cosmosClient,ILogger<ServiceTagsPersistenceManager> logger)
    {
      _cosmosClient = cosmosClient;
      _container = _cosmosClient.GetContainer(Constants.DatabaseName, ServiceTagsContainerName);
      _logger = logger;
    }

    public async Task UpdateDatabaseStateTo(List<ServiceTag> itemsToUpdateWith)
    {
      FunctionLogger.MethodStart(_logger, nameof(UpdateDatabaseStateTo));
      try
      {
        // Retrieve existing items from the database
        FunctionLogger.MethodInformation(_logger, $"Fetching existing items from Cosmos Container: {_container.Id}");
        var existingItems = await GetFromDatabase();
        FunctionLogger.MethodInformation(_logger, $"Retrieved {existingItems.Count} existing items from the database.");

        // Identify items to delete
        var itemsToDelete = existingItems.Where(existingItem => !itemsToUpdateWith.Any(newItem => newItem.Id == existingItem.Id)).ToList();
        FunctionLogger.MethodInformation(_logger, $"Identified {itemsToDelete.Count} items to delete.");

        // Identify items to add or update
        var itemsToAddOrUpdate = itemsToUpdateWith;
        // Perform deletions
        foreach (var item in itemsToDelete)
        {
          await _container.DeleteItemAsync<ServiceTag>(item.Id, new PartitionKey(ServiceTagsPartitionKey));

          FunctionLogger.MethodInformation(_logger, $"Deleted item with Id: {item.Id}");
        }
        // Perform inserts/updates
        foreach (var item in itemsToAddOrUpdate)
        {
          await _container.UpsertItemAsync(item, new PartitionKey(ServiceTagsPartitionKey));
          FunctionLogger.MethodInformation(_logger, $"Upserted item with Id: {item.Id}");
        }
        FunctionLogger.MethodInformation(_logger, "Database state successfully updated.");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
    }

    public async Task<List<ServiceTag>> GetFromDatabase()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetFromDatabase));
      var results = new List<ServiceTag>();
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Fetching ServiceTags from Cosmos container: {_container.Id}");
        var query = _container.GetItemLinqQueryable<ServiceTag>()
                              .ToFeedIterator();
        while (query.HasMoreResults)
        {
          try
          {
            // Fetch next batch of results
            var response = await query.ReadNextAsync();
            results.AddRange(response);

            // Log batch processing information
            FunctionLogger.MethodInformation(_logger,
                $"Fetched a batch of {response.Count} ServiceTags. Total retrieved so far: {results.Count}");
          }
          catch (CosmosException cosmosEx)
          {
            // Handle specific CosmosDB exceptions
            FunctionLogger.MethodWarning(_logger,
                $"CosmosDB query failed while retrieving a batch. StatusCode: {cosmosEx.StatusCode}, Message: {cosmosEx.Message}");
            throw;
          }
        }
        FunctionLogger.MethodInformation(_logger, $"Successfully retrieved a total of {results.Count} ServiceTags from the database.");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
      return results;
    }

    public async Task RemoveItemsFromDatabase(List<ServiceTag> itemsToBeDeleted)
    {
      FunctionLogger.MethodStart(_logger, nameof(RemoveItemsFromDatabase));
      if (itemsToBeDeleted == null || !itemsToBeDeleted.Any())
      {
        FunctionLogger.MethodInformation(_logger, "No items provided for deletion.");
        return;
      }
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Preparing to delete {itemsToBeDeleted.Count} items from the database.");
        foreach (var item in itemsToBeDeleted)
        {
          try
          {
            // Delete the item from the Cosmos DB container
            await _container.DeleteItemAsync<ServiceTag>(item.Id, new PartitionKey(ServiceTagsPartitionKey));

            // Log the successful deletion of each item
            FunctionLogger.MethodInformation(_logger, $"Deleted ServiceTag with ID: {item.Id}");
          }
          catch (CosmosException cosmosEx)
          {
            // Handle specific CosmosDB exceptions for deletion
            FunctionLogger.MethodWarning(_logger,
                $"CosmosDB deletion failed for ServiceTag with ID: {item.Id}. StatusCode: {cosmosEx.StatusCode}, Message: {cosmosEx.Message}");
            // Optionally, rethrow or continue depending on requirements
            throw;
          }
        }
        FunctionLogger.MethodInformation(_logger, $"Successfully deleted {itemsToBeDeleted.Count} items from the database.");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
    }

    public async Task<ServiceTag?> GetById(string id)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetById));
      // Return an empty ServiceTag in case of errors or not found (useful as default value)
      ServiceTag response1 = new ServiceTag();
      if (string.IsNullOrEmpty(id))
      {
        string errorMessage = "Provided ID is null or empty.";
        FunctionLogger.MethodWarning(_logger, errorMessage);
        return response1;
      }
      try
      {
        // Attempt to read the item from Cosmos DB container
        var response = await _container.ReadItemAsync<ServiceTag>(id, new PartitionKey(ServiceTagsPartitionKey));
        FunctionLogger.MethodInformation(_logger, $"Successfully retrieved ServiceTag with ID: {id}");
        return response.Resource;
      }
      catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        // Log the specific case where the item is not found
        FunctionLogger.MethodWarning(_logger, $"ServiceTag with ID: {id} not found. CosmosDB Status Code: {cex.StatusCode}");
        FunctionLogger.MethodException(_logger, cex);
        return response1;
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        return response1;
      }
    }
  }
}