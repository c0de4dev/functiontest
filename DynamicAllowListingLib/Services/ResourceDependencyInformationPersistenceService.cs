using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.ResourceGraphResponses;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DynamicAllowListingLib
{
  public class ResourceDependencyInformationPersistenceService : IResourceDependencyInformationPersistenceService
  {
    private readonly Container _container;
    private readonly ILogger<ResourceDependencyInformationPersistenceService> _logger;

    public ResourceDependencyInformationPersistenceService(
        CosmosClient cosmosClient,
        CosmosDbSettings cosmosDbSettings,
        ILogger<ResourceDependencyInformationPersistenceService> logger)
    {
      var database = cosmosClient.GetDatabase(cosmosDbSettings.DatabaseName);
      _container = database.GetContainer("NetworkRestrictionsConfigs");
      _logger = logger;
    }

    public async Task<ResultObject> CreateOrReplaceItemInDb(ResourceDependencyInformation resourceDependencyInformation)
    {
      ResultObject resultObject = new ResultObject();

      // Check for null or invalid resourceDependencyInformation
      if (resourceDependencyInformation == null)
      {
        _logger.LogNullResourceDependencyInfo();
        return resultObject;
      }

      // Check for empty ResourceId
      if (string.IsNullOrEmpty(resourceDependencyInformation.ResourceId))
      {
        _logger.LogEmptyResourceId();
        return resultObject;
      }

      string documentId = resourceDependencyInformation.DocumentId ?? string.Empty;

      try
      {
        using (_logger.BeginCosmosDbScope("CreateOrReplace", documentId))
        {
          _logger.LogAttemptingReplaceItem(documentId);

          // Try replacing the item in the database
          await _container.ReplaceItemAsync(resourceDependencyInformation, documentId, new PartitionKey(documentId));
          _logger.LogItemReplaced(documentId);
        }
      }
      catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
      {
        // If the item is not found, we will create a new one
        _logger.LogItemNotFoundCreating(resourceDependencyInformation.DocumentId ?? "Unknown");

        try
        {
          await _container.CreateItemAsync(resourceDependencyInformation, new PartitionKey(resourceDependencyInformation.DocumentId));
          _logger.LogItemCreated(resourceDependencyInformation.DocumentId ?? "Unknown");
        }
        catch (Exception createEx)
        {
          _logger.LogCreateItemFailed(createEx, resourceDependencyInformation.DocumentId ?? "Unknown");
          resultObject.Errors.Add($"Failed to create item: {createEx.Message}");
        }
      }
      catch (Exception ex)
      {
        _logger.LogCreateOrReplaceUnexpectedError(ex, documentId);
        resultObject.Errors.Add($"Unexpected error: {ex.Message}");
      }

      return resultObject;
    }

    public async Task<HashSet<ResourceDependencyInformation>> RemoveConfigAndDependencies(string resourceId)
    {
      var updatedItems = new HashSet<ResourceDependencyInformation>();

      // Validate resourceId before proceeding
      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogRemoveConfigEmptyResourceId();
        return updatedItems;
      }

      using (_logger.BeginPersistenceScope(nameof(RemoveConfigAndDependencies), resourceId))
      {
        try
        {
          _logger.LogRemovingConfigForResourceId(resourceId);

          // Remove the record specific to resourceId
          await RemoveConfig(resourceId);

          try
          {
            // Find all documents where this resource id is present as inbound
            var dependencyConfigs = await GetConfigsWhereInbound(resourceId);
            _logger.LogFoundInboundConfigs(dependencyConfigs.Count, resourceId);

            foreach (var document in dependencyConfigs)
            {
              // Remove resourceId from each document
              var updatedDocument = RemoveResourceIdFromDocument(document, resourceId);

              if (updatedDocument != null)
              {
                _logger.LogUpdatingDocumentAfterRemoval(document.DocumentId ?? "Unknown", resourceId);

                // Update the document in the database
                await CreateOrReplaceItemInDb(updatedDocument);
                updatedItems.Add(updatedDocument);
              }
            }
            _logger.LogRemoveConfigAndDependenciesComplete(resourceId, updatedItems.Count);
          }
          catch (Exception ex)
          {
            _logger.LogProcessingDependencyDocumentsError(ex, resourceId);
            throw;
          }
        }
        catch (Exception ex)
        {
          _logger.LogRemoveConfigAndDependenciesCriticalFailure(ex, resourceId);
          throw;
        }
      }

      return updatedItems;
    }

    public async Task RemoveConfig(string resourceId)
    {
      // Ensure the resourceId is valid before attempting the operation
      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogRemoveConfigInvalidResourceId();
        return;
      }

      var documentId = ResourceDependencyInformation.GetDocumentId(resourceId);

      using (_logger.BeginCosmosDbScope("Delete", documentId))
      {
        try
        {
          await _container.DeleteItemAsync<ResourceDependencyInformation>(documentId, new PartitionKey(documentId));
          _logger.LogDocumentDeletedSuccessfully(documentId, resourceId);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
          // Handle the case where the document is not found
          _logger.LogDocumentNotFoundForDeletion(resourceId, documentId);
        }
        catch (CosmosException ex)
        {
          // Handle other Cosmos DB specific errors
          _logger.LogDeleteDocumentError(ex, resourceId, documentId);
          throw;
        }
        catch (Exception ex)
        {
          // Catch other unexpected errors
          _logger.LogRemoveConfigUnexpectedError(ex, resourceId);
          throw;
        }
      }
    }

    public async Task<string[]> GetResourceIdsWhereOutbound(string resourceId)
    {
      List<string> resourceIds = new List<string>();

      // Ensure resourceId is not null or empty before performing any DB operation
      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogGetResourceIdsWhereOutboundSkipped();
        return resourceIds.ToArray();
      }

      using (_logger.BeginPersistenceScope(nameof(GetResourceIdsWhereOutbound), resourceId))
      {
        try
        {
          var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
              .Where(r => r.AllowOutbound != null &&
                          r.AllowOutbound.ResourceIds != null &&
                          r.AllowOutbound.ResourceIds.Contains(resourceId))
              .Select(r => r.ResourceId)
              .ToFeedIterator();

          while (query.HasMoreResults)
          {
            var currentResultSet = await query.ReadNextAsync();
            foreach (var id in currentResultSet)
            {
              if (!string.IsNullOrEmpty(id))
              {
                resourceIds.Add(id);
              }
            }
          }
          _logger.LogFoundResourceIdsWhereOutbound(resourceIds.Count, resourceId);
        }
        catch (CosmosException cex)
        {
          _logger.LogGetResourceIdsWhereOutboundCosmosError(cex, resourceId);
        }
        catch (Exception ex)
        {
          _logger.LogGetResourceIdsWhereOutboundError(ex, resourceId);
        }
      }

      return resourceIds.ToArray();
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetConfigsWhereInbound(string resourceId)
    {
      var resourceDependencyInfoHashSet = new HashSet<ResourceDependencyInformation>();

      // Validate the resourceId before proceeding with the query
      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogGetConfigsWhereInboundSkipped();
        return resourceDependencyInfoHashSet;
      }

      using (_logger.BeginPersistenceScope(nameof(GetConfigsWhereInbound), resourceId))
      {
        try
        {
          var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
              .Where(r => r.AllowInbound != null &&
                          r.AllowInbound.SecurityRestrictions != null &&
                          r.AllowInbound.SecurityRestrictions.ResourceIds != null &&
                          r.AllowInbound.SecurityRestrictions.ResourceIds.Contains(resourceId))
              .ToFeedIterator();

          // Fetching and adding items to the result hash set
          while (query.HasMoreResults)
          {
            var currentResultSet = await query.ReadNextAsync();
            foreach (var item in currentResultSet)
            {
              resourceDependencyInfoHashSet.Add(item);
            }
          }
          _logger.LogInboundConfigRetrievalComplete(resourceDependencyInfoHashSet.Count, resourceId);
        }
        catch (CosmosException cex)
        {
          // Specific error handling for Cosmos DB issues
          _logger.LogGetConfigsWhereInboundCosmosError(cex, resourceId);
        }
        catch (Exception ex)
        {
          // General error handling
          _logger.LogGetConfigsWhereInboundError(ex, resourceId);
        }
      }

      return resourceDependencyInfoHashSet;
    }

    internal ResourceDependencyInformation? RemoveResourceIdFromDocument(ResourceDependencyInformation? document, string? resourceId)
    {
      if (document == null || string.IsNullOrEmpty(resourceId))
      {
        _logger.LogRemoveResourceIdFromDocumentSkipped();
        return document;
      }

      try
      {
        // Process AllowInbound SecurityRestrictions ResourceIds if they exist
        if (document.AllowInbound?.SecurityRestrictions?.ResourceIds != null)
        {
          document.AllowInbound.SecurityRestrictions.ResourceIds =
              RemoveItemFromArray(document.AllowInbound.SecurityRestrictions.ResourceIds, resourceId);
          _logger.LogRemovedFromInboundSecurityRestrictions(resourceId);
        }

        // Process AllowInbound ScmSecurityRestrictions ResourceIds if they exist
        if (document.AllowInbound?.ScmSecurityRestrictions?.ResourceIds != null)
        {
          document.AllowInbound.ScmSecurityRestrictions.ResourceIds =
              RemoveItemFromArray(document.AllowInbound.ScmSecurityRestrictions.ResourceIds, resourceId);
          _logger.LogRemovedFromScmSecurityRestrictions(resourceId);
        }

        // Process AllowOutbound ResourceIds if they exist
        if (document.AllowOutbound?.ResourceIds != null)
        {
          document.AllowOutbound.ResourceIds =
              RemoveItemFromArray(document.AllowOutbound.ResourceIds, resourceId);
          _logger.LogRemovedFromOutboundResourceIds(resourceId);
        }
      }
      catch (Exception ex)
      {
        _logger.LogRemoveResourceIdFromDocumentError(ex, resourceId);
      }

      return document;
    }

    internal string[] RemoveItemFromArray(string[] arrayToRemoveFrom, string itemToRemove)
    {
      var listOfItems = new List<string>(arrayToRemoveFrom);
      listOfItems.Remove(itemToRemove);
      return listOfItems.ToArray();
    }

    public async Task<List<ResourceDependencyInformation>> FindByInternalAndThirdPartyTagName(ServiceTag serviceTag)
    {
      List<ResourceDependencyInformation> result = new List<ResourceDependencyInformation>();

      // Validate input to avoid unnecessary queries
      if (serviceTag == null || string.IsNullOrEmpty(serviceTag.Id))
      {
        _logger.LogInvalidServiceTagNullOrEmpty();
        return result;
      }

      using (_logger.BeginPersistenceScope(nameof(FindByInternalAndThirdPartyTagName)))
      {
        try
        {
          _logger.LogSearchingByServiceTag(serviceTag.Id, serviceTag.Name ?? "Unknown");

          var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
              .Where(r => r.AllowInbound != null &&
                          r.AllowInbound.SecurityRestrictions != null &&
                          r.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags != null &&
                          r.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags.Contains(serviceTag.Name))
              .ToFeedIterator();

          while (query.HasMoreResults)
          {
            var currentResultSet = await query.ReadNextAsync();
            result.AddRange(currentResultSet);
          }
          _logger.LogFoundConfigsForServiceTag(result.Count, serviceTag.Name ?? "Unknown");
        }
        catch (Exception ex)
        {
          _logger.LogFindByServiceTagError(ex, serviceTag.Name ?? "Unknown");
        }
      }

      return result;
    }

    public async Task<ResourceDependencyInformation?> GetResourceDependencyInformation(string resourceId)
    {
      // Validate resourceId before proceeding
      if (string.IsNullOrEmpty(resourceId))
      {
        _logger.LogGetResourceDependencyInfoNullResourceId();
        return null;
      }

      var documentId = ResourceDependencyInformation.GetDocumentId(resourceId);

      using (_logger.BeginCosmosDbScope("Read", documentId))
      {
        try
        {
          _logger.LogQueryingResourceDependencyInfo(resourceId, documentId);

          var response = await _container.ReadItemAsync<ResourceDependencyInformation>(
              documentId,
              new PartitionKey(documentId));
          _logger.LogResourceDependencyInfoFound(resourceId);

          return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
          _logger.LogResourceDependencyInfoNotFound(resourceId);
          return null;
        }
        catch (CosmosException cex)
        {
          _logger.LogGetResourceDependencyInfoCosmosError(cex, resourceId);
          return null;
        }
        catch (Exception ex)
        {
          _logger.LogGetResourceDependencyInfoError(ex, resourceId);
          return null;
        }
      }
    }

    public async Task<ResourceDependencyInformation> GetFirstOrDefault()
    {
      using (_logger.BeginPersistenceScope(nameof(GetFirstOrDefault)))
      {
        try
        {
          var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
              .Take(1)
              .ToFeedIterator();

          if (query.HasMoreResults)
          {
            var currentResultSet = await query.ReadNextAsync();
            var firstItem = currentResultSet.FirstOrDefault();

            if (firstItem != null)
            {
              _logger.LogFirstItemRetrieved();
              return firstItem;
            }
            else
            {
              _logger.LogNoItemsInFirstPage();
              return new ResourceDependencyInformation();
            }
          }
          else
          {
            _logger.LogNoResultsInQuery();
            return new ResourceDependencyInformation();
          }
        }
        catch (Exception ex)
        {
          _logger.LogGetFirstOrDefaultError(ex);
        }
      }

      return new ResourceDependencyInformation();
    }

    public async Task<List<ResourceDependencyInformation>> GetAll()
    {
      List<ResourceDependencyInformation> results = new List<ResourceDependencyInformation>();

      using (_logger.BeginPersistenceScope(nameof(GetAll)))
      {
        try
        {
          _logger.LogGetAllStarting();

          var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>().ToFeedIterator();

          int resultCount = 0;
          while (query.HasMoreResults)
          {
            var currentResultSet = await query.ReadNextAsync();
            resultCount += currentResultSet.Count;
            results.AddRange(currentResultSet);

            _logger.LogGetAllBatchFetched(currentResultSet.Count);
          }
        }
        catch (Exception ex)
        {
          _logger.LogGetAllError(ex);
        }
      }

      return results;
    }
  }
}