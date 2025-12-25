using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.ResourceGraphResponses;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace DynamicAllowListingLib
{
  public class ResourceDependencyInformationPersistenceService : IResourceDependencyInformationPersistenceService
  {
    private readonly Container _container;
    private readonly ILogger<ResourceDependencyInformationPersistenceService> _logger;
    public ResourceDependencyInformationPersistenceService(CosmosClient cosmosClient, CosmosDbSettings cosmosDbSettings,
      ILogger<ResourceDependencyInformationPersistenceService> logger)
    {
      var database = cosmosClient.GetDatabase(cosmosDbSettings.DatabaseName);
      _container = database.GetContainer("NetworkRestrictionsConfigs");
      _logger = logger;
    }

    public async Task<ResultObject> CreateOrReplaceItemInDb(ResourceDependencyInformation resourceDependencyInformation)
    {
      FunctionLogger.MethodStart(_logger, nameof(CreateOrReplaceItemInDb));

      ResultObject resultObject = new ResultObject();
      // Check for null or invalid resourceDependencyInformation
      if (resourceDependencyInformation == null)
      {
        FunctionLogger.MethodInformation(_logger, "ResourceDependencyInformation is null, skipping DB operation.");
        return resultObject;
      }
      // Check for empty ResourceId
      if (string.IsNullOrEmpty(resourceDependencyInformation.ResourceId))
      {
        FunctionLogger.MethodInformation(_logger, "Empty Resource ID, Skipping DB Operation");
        return resultObject;
      }
      try
      {
        string documentId = resourceDependencyInformation.DocumentId ?? string.Empty;
        FunctionLogger.MethodInformation(_logger, $"Attempting to replace item in DB. DocumentId: {documentId}");

        // Try replacing the item in the database
        await _container.ReplaceItemAsync(resourceDependencyInformation, documentId, new PartitionKey(documentId));
        FunctionLogger.MethodInformation(_logger, $"Item replaced successfully. DocumentId: {documentId}");
      }
      catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
      {
        // If the item is not found, we will create a new one
        FunctionLogger.MethodInformation(_logger, $"Item not found in DB, trying to create new item. DocumentId: {resourceDependencyInformation.DocumentId}");
        try
        {
          await _container.CreateItemAsync(resourceDependencyInformation, new PartitionKey(resourceDependencyInformation.DocumentId));
          FunctionLogger.MethodInformation(_logger, $"Item created successfully. DocumentId: {resourceDependencyInformation.DocumentId}");
        }
        catch (Exception cex)
        {
          // Log any exceptions during the create operation
          resultObject.Errors.Add("Error in creating Cosmos DB item");
          FunctionLogger.MethodException(_logger, cex, $"Error occurred while creating item. DocumentId: {resourceDependencyInformation.DocumentId}");
        }
      }
      catch (CosmosException ex)
      {
        // Log any Cosmos DB exceptions (other than NotFound)
        resultObject.Errors.Add("Error in updating Cosmos DB item");
        FunctionLogger.MethodException(_logger, ex, $"Error occurred while replacing item. DocumentId: {resourceDependencyInformation.DocumentId}");
      }
      catch (Exception ex)
      {
        // Log any unexpected exceptions
        resultObject.Errors.Add("Error in cosmos DB operations");
        FunctionLogger.MethodException(_logger, ex, "Unexpected error while processing CreateOrReplaceItemInDb operation.");
      }
      resultObject.Information.Add($"Cosmos DB operation successful.");
      return resultObject;
    }

    public async Task<ResourceDependencyInformation?> GetResourceDependencyInformation(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetResourceDependencyInformation));
      if (string.IsNullOrWhiteSpace(resourceId))
      {
        string error = $"Invalid resourceId provided: '{resourceId}'";
        FunctionLogger.MethodWarning(_logger, error);
        return null;
      }
      try
      {
        // Generate the document ID based on the resourceId
        string documentId = ResourceDependencyInformation.GetDocumentId(resourceId);
        // Attempt to retrieve the item from the Cosmos DB container
        ItemResponse<ResourceDependencyInformation> itemResponse = await _container.ReadItemAsync<ResourceDependencyInformation>(documentId, new PartitionKey(documentId));
        // Log and return the retrieved resource
        FunctionLogger.MethodInformation(_logger, $"Successfully retrieved ResourceDependencyInformation for ResourceID: {resourceId}");
        return itemResponse.Resource;
      }
      catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
      {
        string warning = $"ResourceDependencyInformation not found with ResourceID: {resourceId}";
        FunctionLogger.MethodWarning(_logger, warning);
      }
      catch (Exception ex)
      {        
        // Log the exception with relevant context
        string error = $"An error occurred while retrieving ResourceDependencyInformation for ResourceID: {resourceId}";
        FunctionLogger.MethodException(_logger, ex, error);
        // Rethrow if necessary (optional, depending on how you want to handle general exceptions)
        throw;
      }
      return null;
    }

    public async Task<HashSet<ResourceDependencyInformation>> RemoveConfigAndDependencies(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(RemoveConfigAndDependencies));
      HashSet<ResourceDependencyInformation> updatedItems = new HashSet<ResourceDependencyInformation>();
      try
      {
        var documentId = ResourceDependencyInformation.GetDocumentId(resourceId);
        try
        {
          await _container.DeleteItemAsync<ResourceDependencyInformation>(documentId, new PartitionKey(documentId));
          FunctionLogger.MethodInformation(_logger, $"Successfully deleted configuration document with DocumentId: {documentId} from the Cosmos DB container.");
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
          FunctionLogger.MethodWarning(_logger, $"Document not found for deletion in RemoveConfigAndDependencies. ResourceID: {resourceId}");
        }
        catch (Exception ex)
        {
          FunctionLogger.MethodException(_logger, ex, $"Error while deleting configuration document. ResourceID: {resourceId}");
          throw; // Re-throw to ensure the operation halts if critical errors occur.
        }
        FunctionLogger.MethodInformation(_logger, "Deleted configuration from cosmos db container");
        try
        {
          var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
              .Where(r =>
              (r.AllowInbound != null && r.AllowInbound.SecurityRestrictions != null && r.AllowInbound.SecurityRestrictions.ResourceIds != null &&
               r.AllowInbound.SecurityRestrictions.ResourceIds.Contains(resourceId)) ||
              (r.AllowInbound != null && r.AllowInbound.ScmSecurityRestrictions != null && r.AllowInbound.ScmSecurityRestrictions.ResourceIds != null &&
               r.AllowInbound.ScmSecurityRestrictions.ResourceIds.Contains(resourceId)) ||
              (r.AllowOutbound != null && r.AllowOutbound.ResourceIds != null &&
               r.AllowOutbound.ResourceIds.Contains(resourceId)))
              .ToFeedIterator();

          while (query.HasMoreResults)
          {
            var currentResultSet = await query.ReadNextAsync();
            foreach (var res in currentResultSet)
            {
              var updatedItem = RemoveResourceIdFromDocument(res, resourceId);
              if (updatedItem?.DocumentId != null)
              {
                var itemReplaceResponse = await _container.ReplaceItemAsync(updatedItem, updatedItem.DocumentId, new PartitionKey(updatedItem.DocumentId));
                updatedItems.Add(itemReplaceResponse.Resource);

                FunctionLogger.MethodInformation(_logger, $"Updated dependency document after removal, UpdatedDocumentId: {updatedItem.DocumentId}");
              }
            }
          }
          FunctionLogger.MethodInformation(_logger, $"Completed RemoveConfigAndDependencies. ResourceID: {resourceId}, UpdatedItemCount: {updatedItems.Count}");
        }
        catch (Exception ex)
        {
          FunctionLogger.MethodException(_logger, ex, $"Error while retrieving or processing dependency documents for ResourceID: {resourceId}");
          throw;
        }
      }
      catch(Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, $"Critical failure in RemoveConfigAndDependencies for ResourceID: {resourceId}");
        throw; // Re-throw to ensure upstream systems are aware of the failure.
      }
      return updatedItems;
    }

    public async Task RemoveConfig(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(RemoveConfig));
      // Ensure the resourceId is valid before attempting the operation
      if (string.IsNullOrEmpty(resourceId))
      {
        FunctionLogger.MethodWarning(_logger, "ResourceId is empty or null. Cannot proceed with the removal.");
        return;
      }
      var documentId = ResourceDependencyInformation.GetDocumentId(resourceId);
      try
      {
        await _container.DeleteItemAsync<ResourceDependencyInformation>(documentId, new PartitionKey(documentId));

        FunctionLogger.MethodInformation(_logger, $"Successfully deleted document from Cosmos DB. DocumentId: {documentId}, ResourceId: {resourceId}");
      }
      catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
      {
        // Handle the case where the document is not found
        FunctionLogger.MethodWarning(_logger, $"Document not found for deletion. ResourceId: {resourceId}, DocumentId: {documentId}");
      }
      catch (CosmosException ex)
      {
        // Handle other Cosmos DB specific errors
        FunctionLogger.MethodException(_logger, ex, $"Error occurred while deleting document from Cosmos DB. ResourceId: {resourceId}, DocumentId: {documentId}");
        throw; // Re-throw for higher-level handling if needed
      }
      catch (Exception ex)
      {
        // Catch other unexpected errors
        FunctionLogger.MethodException(_logger, ex, $"Unexpected error while removing configuration. ResourceId: {resourceId}");
        throw; // Re-throw for higher-level handling if needed
      }
    }

    public async Task<string[]> GetResourceIdsWhereOutbound(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetResourceIdsWhereOutbound));
      List<string> resourceIds = new List<string>();

      // Ensure resourceId is not null or empty before performing any DB operation
      if (string.IsNullOrEmpty(resourceId))
      {
        FunctionLogger.MethodInformation(_logger, "Provided resourceId is null or empty. Skipping query.");
        return resourceIds.ToArray();
      }
      try
      {
        var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
            .Where(r => r.AllowOutbound != null && r.AllowOutbound.ResourceIds != null && r.AllowOutbound.ResourceIds.Contains(resourceId))
            .ToFeedIterator();

        while (query.HasMoreResults)
        {
          var currentResultSet = await query.ReadNextAsync();
          foreach (var item in currentResultSet)
          {
            if (!string.IsNullOrEmpty(item.ResourceId))
            {
              resourceIds.Add(item.ResourceId);
            }
          }
        }
        FunctionLogger.MethodInformation(_logger, $"Retrieved {resourceIds.Count} items where resource ID: {resourceId} is allowed for outbound.");
      }
      catch (CosmosException cex)
      {
        FunctionLogger.MethodException(_logger, cex, $"Cosmos DB exception occurred while fetching outbound resource IDs for resource ID: {resourceId}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, "An error occurred while processing GetResourceIdsWhereOutbound.");
      }
      return resourceIds.ToArray();
    }

    public async Task<HashSet<ResourceDependencyInformation>> GetConfigsWhereInbound(string resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(GetConfigsWhereInbound));
      var resourceDependencyInfoHashSet = new HashSet<ResourceDependencyInformation>();
      // Validate the resourceId before proceeding with the query
      if (string.IsNullOrEmpty(resourceId))
      {
        FunctionLogger.MethodInformation(_logger, "Provided resourceId is null or empty. Skipping query.");
        return resourceDependencyInfoHashSet;
      }
      try
      {
        var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
             .Where(r => r.AllowInbound != null &&
               r.AllowInbound.SecurityRestrictions != null &&
               r.AllowInbound.SecurityRestrictions.ResourceIds != null &&
               r.AllowInbound.SecurityRestrictions.ResourceIds.Contains(resourceId)).ToFeedIterator();

        // Fetching and adding items to the result hash set
        while (query.HasMoreResults)
        {
          var currentResultSet = await query.ReadNextAsync();
          foreach (var item in currentResultSet)
          {
            resourceDependencyInfoHashSet.Add(item);
          }
        }
        FunctionLogger.MethodInformation(_logger, $"Completed retrieval with {resourceDependencyInfoHashSet.Count} configurations found for inbound dependencies.");
      }
      catch (CosmosException cex)
      {
        // Specific error handling for Cosmos DB issues
        FunctionLogger.MethodException(_logger, cex, "Cosmos DB exception occurred while retrieving inbound configurations.");
      }
      catch (Exception ex)
      {
        // General error handling
        FunctionLogger.MethodException(_logger, ex, "An error occurred while processing GetConfigsWhereInbound.");
      }
      return resourceDependencyInfoHashSet;
    }

    internal ResourceDependencyInformation? RemoveResourceIdFromDocument(ResourceDependencyInformation? document, string? resourceId)
    {
      FunctionLogger.MethodStart(_logger, nameof(RemoveResourceIdFromDocument));
      if (document == null || string.IsNullOrEmpty(resourceId))
      {
        FunctionLogger.MethodInformation(_logger, "Document or Resource ID is null, skipping removal.");
        return document;
      }
      try
      {
        // Process AllowInbound SecurityRestrictions ResourceIds if they exist
        if (document.AllowInbound?.SecurityRestrictions?.ResourceIds != null)
        {
          document.AllowInbound.SecurityRestrictions.ResourceIds =
              RemoveItemFromArray(document.AllowInbound.SecurityRestrictions.ResourceIds, resourceId);

          FunctionLogger.MethodInformation(_logger, $"Removed ResourceId from AllowInbound.SecurityRestrictions.ResourceIds if it existed: {resourceId}");
        }        
        // Process AllowInbound ScmSecurityRestrictions ResourceIds if they exist
        if (document.AllowInbound?.ScmSecurityRestrictions?.ResourceIds != null)
        {
          document.AllowInbound.ScmSecurityRestrictions.ResourceIds =
              RemoveItemFromArray(document.AllowInbound.ScmSecurityRestrictions.ResourceIds, resourceId);

          FunctionLogger.MethodInformation(_logger, $"Removed ResourceId from AllowInbound.ScmSecurityRestrictions.ResourceIds if it existed: {resourceId}");
        }
        // Process AllowOutbound ResourceIds if they exist
        if (document.AllowOutbound?.ResourceIds != null)
        {
          document.AllowOutbound.ResourceIds =
              RemoveItemFromArray(document.AllowOutbound.ResourceIds, resourceId);

          FunctionLogger.MethodInformation(_logger, $"Removed ResourceId from AllowOutbound.ResourceIds if it existed: {resourceId}");
        }
      }
      catch(Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex, $"Error occurred while removing ResourceId: {resourceId} from document.");
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
      FunctionLogger.MethodStart(_logger, nameof(FindByInternalAndThirdPartyTagName));
      List<ResourceDependencyInformation> result = new List<ResourceDependencyInformation>();
      
      // Validate input to avoid unnecessary queries
      if (serviceTag == null || string.IsNullOrEmpty(serviceTag.Id))
      {
        FunctionLogger.MethodInformation(_logger, "Invalid service tag: null or empty Id.");
        return result; // Early return if the input is invalid
      }
      try
      {
        var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>()
            .Where(p => p.AllowInbound != null &&
                                   p.AllowInbound.SecurityRestrictions != null &&
                                   p.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags != null &&
                                   p.AllowInbound.SecurityRestrictions.NewDayInternalAndThirdPartyTags.Any(t => t.Equals(serviceTag.Id)))
            .ToFeedIterator();

        // Fetch and process query results
        while (query.HasMoreResults)
        {
          var currentResultSet = await query.ReadNextAsync();
          result.AddRange(currentResultSet);
        }

        // Log success if items are found
        if (result.Count > 0)
        {
          FunctionLogger.MethodInformation(_logger, $"Found {result.Count} ResourceDependencyInformation items matching tag: {serviceTag.Id}");
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, "No ResourceDependencyInformation found for the provided tag.");
        }
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return result;
    }

    public async Task<ResourceDependencyInformation> GetFirstOrDefault()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetFirstOrDefault));
      try
      {
        var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>().ToFeedIterator();
        if (query.HasMoreResults)
        { 
          // Log that there are results to read
          FunctionLogger.MethodInformation(_logger, "Results found, reading the first page of results.");

          var result = await query.ReadNextAsync();
          var firstItem = result.FirstOrDefault();

          if (firstItem != null)
          {
            // Log when the first item is found
            FunctionLogger.MethodInformation(_logger, $"First item found with ID: {firstItem.DocumentId}.");
            return firstItem;
          }
          else
          {
            // Log when no item is found
            FunctionLogger.MethodWarning(_logger, "No items found in the first page of results, returning a new ResourceDependencyInformation.");
            return new ResourceDependencyInformation(); // Return a default object if no item is found
          }
        }
        else
        {
          // Log when no results are found at all
          FunctionLogger.MethodWarning(_logger, "No results found in the query.");
          return new ResourceDependencyInformation(); // Return a default object if no items are returned
        }
      }
      catch(Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return new ResourceDependencyInformation();
    }

    public async Task<List<ResourceDependencyInformation>> GetAll()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetAll));
      List<ResourceDependencyInformation> results = new List<ResourceDependencyInformation>();
      try
      { // Log the query process
        FunctionLogger.MethodInformation(_logger, "Starting to query for all ResourceDependencyInformation documents from Cosmos DB.");
        
        var query = _container.GetItemLinqQueryable<ResourceDependencyInformation>().ToFeedIterator();
        // Log the process of fetching results
        int resultCount = 0;
        while (query.HasMoreResults)
        {
          var currentResultSet = await query.ReadNextAsync();
          resultCount += currentResultSet.Count;
          results.AddRange(currentResultSet);

          // Log the number of items retrieved in each batch
          FunctionLogger.MethodInformation(_logger, $"Fetched {currentResultSet.Count} items in the current batch.");
        }
        // Log total number of results fetched
        FunctionLogger.MethodInformation(_logger, $"Total items retrieved: {resultCount}");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
      }
      return results;
    }
  }
}