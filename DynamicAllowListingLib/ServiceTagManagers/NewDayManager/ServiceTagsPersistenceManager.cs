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
  public class ServiceTagsPersistenceManager : IPersistenceManager<ServiceTag>
  {
    private const string ServiceTagsContainerName = "ServiceTags";
    private const string ServiceTagsPartitionKey = "servicetags";

    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<ServiceTagsPersistenceManager> _logger;

    public ServiceTagsPersistenceManager(CosmosClient cosmosClient, ILogger<ServiceTagsPersistenceManager> logger)
    {
      _cosmosClient = cosmosClient;
      _container = _cosmosClient.GetContainer(Constants.DatabaseName, ServiceTagsContainerName);
      _logger = logger;
    }

    public async Task UpdateDatabaseStateTo(List<ServiceTag> itemsToUpdateWith)
    {
      try
      {
        // Retrieve existing items from the database
        _logger.LogFetchingExistingItems(_container.Id);
        var existingItems = await GetFromDatabase();
        _logger.LogRetrievedExistingItemsCount(existingItems.Count);

        // Identify items to delete
        var itemsToDelete = existingItems.Where(existingItem => !itemsToUpdateWith.Any(newItem => newItem.Id == existingItem.Id)).ToList();
        _logger.LogItemsToDelete(itemsToDelete.Count);

        // Identify items to add or update
        var itemsToAddOrUpdate = itemsToUpdateWith;
        // Perform deletions
        foreach (var item in itemsToDelete)
        {
          await _container.DeleteItemAsync<ServiceTag>(item.Id, new PartitionKey(ServiceTagsPartitionKey));

          _logger.LogItemDeleted(item.Id ?? "Unknown");
        }
        // Perform inserts/updates
        foreach (var item in itemsToAddOrUpdate)
        {
          await _container.UpsertItemAsync(item, new PartitionKey(ServiceTagsPartitionKey));
          _logger.LogItemUpserted(item.Id ?? "Unknown");
        }
        _logger.LogDatabaseStateUpdated();
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        throw;
      }
    }

    public async Task<List<ServiceTag>> GetFromDatabase()
    {
      var results = new List<ServiceTag>();
      try
      {
        _logger.LogFetchingServiceTags(_container.Id);
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
            _logger.LogServiceTagBatchFetched(response.Count, results.Count);
          }
          catch (CosmosException cosmosEx)
          {
            // Handle specific CosmosDB exceptions
            _logger.LogCosmosQueryFailed(cosmosEx.StatusCode.ToString(), cosmosEx.Message);
            throw;
          }
        }
        _logger.LogServiceTagsRetrieved(results.Count);
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        throw;
      }
      return results;
    }

    public async Task RemoveItemsFromDatabase(List<ServiceTag> itemsToBeDeleted)
    {
      if (itemsToBeDeleted == null || !itemsToBeDeleted.Any())
      {
        _logger.LogNoItemsForDeletion();
        return;
      }
      try
      {
        _logger.LogPreparingToDeleteItems(itemsToBeDeleted.Count);
        foreach (var item in itemsToBeDeleted)
        {
          try
          {
            // Delete the item from the Cosmos DB container
            await _container.DeleteItemAsync<ServiceTag>(item.Id, new PartitionKey(ServiceTagsPartitionKey));

            // Log the successful deletion of each item
            _logger.LogServiceTagDeleted(item.Id ?? "Unknown");
          }
          catch (CosmosException cosmosEx)
          {
            // Handle specific CosmosDB exceptions for deletion
            _logger.LogServiceTagDeletionFailed(item.Id ?? "Unknown", cosmosEx.StatusCode.ToString(), cosmosEx.Message);
            // Optionally, rethrow or continue depending on requirements
            throw;
          }
        }
        _logger.LogSuccessfullyDeletedItems(itemsToBeDeleted.Count);
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
      }
    }

    public async Task<ServiceTag?> GetById(string id)
    {
      // Return an empty ServiceTag in case of errors or not found (useful as default value)
      ServiceTag response1 = new ServiceTag();
      if (string.IsNullOrEmpty(id))
      {
        _logger.LogProvidedIdNullOrEmpty();
        return response1;
      }
      try
      {
        // Attempt to read the item from Cosmos DB container
        var response = await _container.ReadItemAsync<ServiceTag>(id, new PartitionKey(ServiceTagsPartitionKey));
        _logger.LogServiceTagRetrievedById(id);
        return response.Resource;
      }
      catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        // Log the specific case where the item is not found
        _logger.LogServiceTagNotFoundById(id, cex.StatusCode.ToString());
        _logger.LogOperationException(cex);
        return response1;
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        return response1;
      }
    }
  }
}