using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
      var stopwatch = Stopwatch.StartNew();
      int deletedCount = 0;
      int upsertedCount = 0;

      // Gap 5b.1 fix: Log null/empty input check
      if (itemsToUpdateWith == null || !itemsToUpdateWith.Any())
      {
        _logger.LogServiceTagsInputListNullOrEmpty();
        return;
      }

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

        // Gap 5b.2 fix: Log items to add/update count
        _logger.LogServiceTagsToAddOrUpdate(itemsToAddOrUpdate.Count);

        // Log deletion phase start
        _logger.LogStartingServiceTagsDeletionPhase(itemsToDelete.Count);

        // Perform deletions - Gap 5b.3 fix: Add try-catch for each delete
        foreach (var item in itemsToDelete)
        {
          try
          {
            await _container.DeleteItemAsync<ServiceTag>(item.Id, new PartitionKey(ServiceTagsPartitionKey));
            _logger.LogItemDeleted(item.Id ?? "Unknown");
            deletedCount++;
          }
          catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
          {
            // Handle scenario where the item to delete doesn't exist in the database
            _logger.LogServiceTagNotFoundDuringDeletion(item.Id ?? "Unknown");
          }
          catch (Exception ex)
          {
            // Log delete exception but continue processing
            _logger.LogServiceTagDeleteFailed(ex, item.Id ?? "Unknown");
          }
        }

        // Log upsert phase start
        _logger.LogStartingServiceTagsUpsertPhase(itemsToAddOrUpdate.Count);

        // Perform inserts/updates - Gap 5b.4 fix: Add try-catch for each upsert
        foreach (var item in itemsToAddOrUpdate)
        {
          try
          {
            await _container.UpsertItemAsync(item, new PartitionKey(ServiceTagsPartitionKey));
            _logger.LogItemUpserted(item.Id ?? "Unknown");
            upsertedCount++;
          }
          catch (Exception ex)
          {
            // Log upsert exception but continue processing
            _logger.LogServiceTagUpsertFailed(ex, item.Id ?? "Unknown");
          }
        }

        _logger.LogDatabaseStateUpdated();
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        throw;
      }
      finally
      {
        stopwatch.Stop();
        // Log completion summary with counts and duration
        _logger.LogServiceTagsUpdateCompleted(
            deletedCount,
            upsertedCount,
            stopwatch.ElapsedMilliseconds);
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
            _logger.LogServiceTagDeletionFailed(item.Id ?? "Unknown", cosmosEx.StatusCode.ToString(), cosmosEx.Message.ToString());
          }
          catch (Exception ex)
          {
            // Log generic exception during deletion
            _logger.LogServiceTagDeleteFailed(ex, item.Id ?? "Unknown");
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        throw;
      }
    }

    public async Task<ServiceTag?> GetById(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        _logger.LogProvidedIdNullOrEmpty();
        return null;
      }

      try
      {
        var response = await _container.ReadItemAsync<ServiceTag>(id, new PartitionKey(ServiceTagsPartitionKey));
        _logger.LogServiceTagRetrievedById(id);
        return response.Resource;
      }
      catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        _logger.LogServiceTagNotFoundById(id, cex.StatusCode.ToString());
        return null;
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        throw;
      }
    }
  }
}