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
  public class AzureSubscriptionsPersistenceManager : IPersistenceManager<AzureSubscription>
  {
    private const string AzureSubscriptionsContainerName = "AzureSubscriptions";
    private const string AzureSubscriptionsPartitionKey = "azureSubscriptions";
     
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<AzureSubscriptionsPersistenceManager> _logger;

    public AzureSubscriptionsPersistenceManager(CosmosClient cosmosClient,ILogger<AzureSubscriptionsPersistenceManager> logger)
    {
      _cosmosClient = cosmosClient;
      _container = _cosmosClient.GetContainer(Constants.DatabaseName, AzureSubscriptionsContainerName);
      _logger = logger;
    }
 
    public async Task UpdateDatabaseStateTo(List<AzureSubscription> itemsToUpdateWith)
    {
      // Start logging
      FunctionLogger.MethodStart(_logger, nameof(UpdateDatabaseStateTo));
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Getting Items from cosmos Container: {_container.Id}");
        // Retrieve existing items
        var existingItems = await GetFromDatabase();
        FunctionLogger.MethodInformation(_logger, $"Retrieved existing items from database. Count: {existingItems.Count}");

        // Identify items to delete
        var itemsToDelete = existingItems.Where(existingItem => !itemsToUpdateWith.Any(newItem => newItem.Id == existingItem.Id)).ToList();
        FunctionLogger.MethodInformation(_logger, $"Identified {itemsToDelete.Count} items to delete.");

        var itemsToAddOrUpdate = itemsToUpdateWith;
        // Perform deletions
        foreach (var item in itemsToDelete)
        {
          try
          {
            await _container.DeleteItemAsync<AzureSubscription>(item.Id, new PartitionKey(AzureSubscriptionsPartitionKey));
            FunctionLogger.MethodInformation(_logger, $"Deleted item with Id: {item.Id}");
          }
          catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
          {
            // Handle scenario where the item to delete doesn't exist in the database
            FunctionLogger.MethodWarning(_logger, $"Item with Id: {item.Id} not found during deletion. Skipping.");
          }
        }
        // Perform inserts/updates
        foreach (var item in itemsToAddOrUpdate)
        {
          try
          {
            await _container.UpsertItemAsync(item, new PartitionKey(AzureSubscriptionsPartitionKey));
            FunctionLogger.MethodInformation(_logger, $"Upserted item with Id: {item.Id}");
          }
          catch (Exception ex)
          {
            // Log any exception during upsert
            FunctionLogger.MethodException(_logger, ex, $"Failed to upsert item with Id: {item.Id}");
          }
        }
      }
      catch (CosmosException cex)
      {     
        // Handle general Cosmos DB exceptions
        FunctionLogger.MethodException(_logger, cex, "A Cosmos DB exception occurred during the update process.");
      }
      catch (Exception ex)
      {        
        // Handle unexpected exceptions
        FunctionLogger.MethodException(_logger, ex, "An unexpected error occurred during the update process.");
        throw; // Rethrow to ensure the caller is aware of the failure
      }
    }


    public async Task<List<AzureSubscription>> GetFromDatabase()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetFromDatabase));
      var results = new List<AzureSubscription>();
      //try to enrich cosmos dependency operation with context details.
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Querying Azure subscriptions from Cosmos container: {_container.Id}");

        // Create LINQ query and process using FeedIterator
        var query = _container.GetItemLinqQueryable<AzureSubscription>()
                                .ToFeedIterator();
        // Iterate through query results
        while (query.HasMoreResults)
        {
          var response = await query.ReadNextAsync();
          results.AddRange(response);

          // Log partial results for transparency
          FunctionLogger.MethodInformation(_logger, $"Fetched {response.Count} subscriptions from current batch.");
        }
        // Log final count of retrieved subscriptions
        FunctionLogger.MethodInformation(_logger, $"Successfully retrieved {results.Count} Azure subscriptions from the database.");

      }
      catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        // Handle case where the container or data is not found
        FunctionLogger.MethodWarning(_logger, $"No Azure subscriptions found in Cosmos container: {_container.Id}");
      }
      catch (CosmosException cex)
      {
        // Handle general Cosmos DB exceptions
        FunctionLogger.MethodException(_logger, cex, "A Cosmos DB exception occurred while retrieving Azure subscriptions.");
        throw; // Rethrow exception to propagate error to the caller
      }
      catch (Exception ex)
      {
        // Handle unexpected exceptions
        FunctionLogger.MethodException(_logger, ex, "An unexpected error occurred while retrieving Azure subscriptions.");
        throw; // Rethrow exception to propagate error to the caller
      }
      return results;
    }

    public async Task RemoveItemsFromDatabase(List<AzureSubscription> itemsToBeDeleted)
    {
      if (itemsToBeDeleted == null || !itemsToBeDeleted.Any())
      {
        FunctionLogger.MethodWarning(_logger, $"{nameof(RemoveItemsFromDatabase)} was called with an empty or null list.");
        return; // No items to delete
      }
      FunctionLogger.MethodStart(_logger, nameof(RemoveItemsFromDatabase));
      try
      {
        FunctionLogger.MethodInformation(_logger, $"Preparing to delete {itemsToBeDeleted.Count} AzureSubscription items from the database.");

        foreach (var item in itemsToBeDeleted)
        {
          try
          {
            // Attempt to delete the item from Cosmos DB
            await _container.DeleteItemAsync<AzureSubscription>(item.Id, new PartitionKey(AzureSubscriptionsPartitionKey));
            FunctionLogger.MethodInformation(_logger, $"Successfully deleted item with Id: {item.Id}");
          }
          catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
          {
            // Log if the item to delete was not found
            FunctionLogger.MethodWarning(_logger, $"Item with Id: {item.Id} was not found in the database. Skipping.");
          }
          catch (Exception ex)
          {
            // Log unexpected errors during item deletion
            FunctionLogger.MethodException(_logger, ex, $"Failed to delete item with Id: {item.Id}. Skipping to the next item.");
          }
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, "An unexpected error occurred while deleting items from the database.");
        throw;
      }
    }

    public async Task<AzureSubscription?> GetById(string requestSubscriptionId)
    {
      if (string.IsNullOrEmpty(requestSubscriptionId))
      {
        FunctionLogger.MethodWarning(_logger, $"{nameof(GetById)} was called with a null or empty subscription ID.");
        return null; // Return null if the input is invalid
      }
      try
      {
        FunctionLogger.MethodStart(_logger, nameof(GetById));

        // Attempt to retrieve the item from Cosmos DB
        var response = await _container.ReadItemAsync<AzureSubscription>(requestSubscriptionId, new PartitionKey(AzureSubscriptionsPartitionKey));

        FunctionLogger.MethodInformation(_logger, $"Successfully retrieved AzureSubscription for ID: {requestSubscriptionId}");
        return response.Resource;
      }
      catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        // Log a warning instead of an exception for not found cases
        FunctionLogger.MethodWarning(_logger, $"AzureSubscription with ID '{requestSubscriptionId}' was not found in the database.");
        return null; // Return null to indicate not found
      }
      catch (CosmosException cex)
      {
        // Log unexpected Cosmos DB errors
        FunctionLogger.MethodException(_logger, cex, "An error occurred while accessing the Cosmos DB.");
        throw; // Re-throw the exception for higher-level handling
      }
      catch (Exception ex)
      {
        // Log any other unexpected errors
        FunctionLogger.MethodException(_logger, ex, "An unexpected error occurred while retrieving the AzureSubscription.");
        throw; // Re-throw the exception for higher-level handling
      }
    }
  }
}