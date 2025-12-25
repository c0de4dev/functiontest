using DynamicAllowListingLib.Logging;
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

    public AzureSubscriptionsPersistenceManager(CosmosClient cosmosClient, ILogger<AzureSubscriptionsPersistenceManager> logger)
    {
      _cosmosClient = cosmosClient;
      _container = _cosmosClient.GetContainer(Constants.DatabaseName, AzureSubscriptionsContainerName);
      _logger = logger;
    }

    public async Task UpdateDatabaseStateTo(List<AzureSubscription> itemsToUpdateWith)
    {
      try
      {
        _logger.LogGettingItemsFromContainer(_container.Id);
        // Retrieve existing items
        var existingItems = await GetFromDatabase();
        _logger.LogRetrievedExistingItems(existingItems.Count);

        // Identify items to delete
        var itemsToDelete = existingItems.Where(existingItem => !itemsToUpdateWith.Any(newItem => newItem.Id == existingItem.Id)).ToList();
        _logger.LogItemsToDelete(itemsToDelete.Count);

        var itemsToAddOrUpdate = itemsToUpdateWith;
        // Perform deletions
        foreach (var item in itemsToDelete)
        {
          try
          {
            await _container.DeleteItemAsync<AzureSubscription>(item.Id, new PartitionKey(AzureSubscriptionsPartitionKey));
            _logger.LogItemDeleted(item.Id ?? "Unknown");
          }
          catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
          {
            // Handle scenario where the item to delete doesn't exist in the database
            _logger.LogItemNotFoundDuringDeletion(item.Id ?? "Unknown");
          }
        }
        // Perform inserts/updates
        foreach (var item in itemsToAddOrUpdate)
        {
          try
          {
            await _container.UpsertItemAsync(item, new PartitionKey(AzureSubscriptionsPartitionKey));
            _logger.LogItemUpserted(item.Id ?? "Unknown");
          }
          catch (Exception ex)
          {
            // Log any exception during upsert
            _logger.LogFailedToUpsertItem(ex, item.Id ?? "Unknown");
          }
        }
      }
      catch (CosmosException cex)
      {
        // Handle general Cosmos DB exceptions
        _logger.LogCosmosExceptionDuringUpdate(cex);
      }
      catch (Exception ex)
      {
        // Handle unexpected exceptions
        _logger.LogUnexpectedErrorDuringUpdate(ex);
        throw; // Rethrow to ensure the caller is aware of the failure
      }
    }


    public async Task<List<AzureSubscription>> GetFromDatabase()
    {
      var results = new List<AzureSubscription>();
      //try to enrich cosmos dependency operation with context details.
      try
      {
        _logger.LogQueryingFromContainer(_container.Id);

        // Create LINQ query and process using FeedIterator
        var query = _container.GetItemLinqQueryable<AzureSubscription>()
                                .ToFeedIterator();
        // Iterate through query results
        while (query.HasMoreResults)
        {
          var response = await query.ReadNextAsync();
          results.AddRange(response);

          // Log partial results for transparency
          _logger.LogBatchFetched(response.Count);
        }
        // Log final count of retrieved subscriptions
        _logger.LogSubscriptionsRetrieved(results.Count);

      }
      catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        // Handle case where the container or data is not found
        _logger.LogNoSubscriptionsFound(_container.Id);
      }
      catch (CosmosException cex)
      {
        // Handle general Cosmos DB exceptions
        _logger.LogCosmosExceptionRetrieving(cex);
        throw; // Rethrow exception to propagate error to the caller
      }
      catch (Exception ex)
      {
        // Handle unexpected exceptions
        _logger.LogUnexpectedErrorRetrieving(ex);
        throw; // Rethrow exception to propagate error to the caller
      }
      return results;
    }

    public async Task RemoveItemsFromDatabase(List<AzureSubscription> itemsToBeDeleted)
    {
      if (itemsToBeDeleted == null || !itemsToBeDeleted.Any())
      {
        _logger.LogEmptyOrNullList(nameof(RemoveItemsFromDatabase));
        return; // No items to delete
      }
      try
      {
        _logger.LogPreparingToDelete(itemsToBeDeleted.Count);

        foreach (var item in itemsToBeDeleted)
        {
          try
          {
            // Attempt to delete the item from Cosmos DB
            await _container.DeleteItemAsync<AzureSubscription>(item.Id, new PartitionKey(AzureSubscriptionsPartitionKey));
            _logger.LogSuccessfullyDeletedItem(item.Id ?? "Unknown");
          }
          catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
          {
            // Log if the item to delete was not found
            _logger.LogItemNotFoundSkipping(item.Id ?? "Unknown");
          }
          catch (Exception ex)
          {
            // Log unexpected errors during item deletion
            _logger.LogFailedToDeleteItem(ex, item.Id ?? "Unknown");
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogUnexpectedErrorDeleting(ex);
        throw;
      }
    }

    public async Task<AzureSubscription?> GetById(string requestSubscriptionId)
    {
      if (string.IsNullOrEmpty(requestSubscriptionId))
      {
        _logger.LogNullOrEmptySubscriptionId(nameof(GetById));
        return null; // Return null if the input is invalid
      }
      try
      {
        // Attempt to retrieve the item from Cosmos DB
        var response = await _container.ReadItemAsync<AzureSubscription>(requestSubscriptionId, new PartitionKey(AzureSubscriptionsPartitionKey));

        _logger.LogSubscriptionRetrievedById(requestSubscriptionId);
        return response.Resource;
      }
      catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        // Log a warning instead of an exception for not found cases
        _logger.LogSubscriptionNotFoundById(requestSubscriptionId);
        return null; // Return null to indicate not found
      }
      catch (CosmosException cex)
      {
        // Log unexpected Cosmos DB errors
        _logger.LogCosmosAccessError(cex);
        throw; // Re-throw the exception for higher-level handling
      }
      catch (Exception ex)
      {
        // Log any other unexpected errors
        _logger.LogUnexpectedErrorRetrievingSubscription(ex);
        throw; // Re-throw the exception for higher-level handling
      }
    }
  }
}