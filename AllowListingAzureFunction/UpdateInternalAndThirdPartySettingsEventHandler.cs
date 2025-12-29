using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using AllowListingAzureFunction.Logging;

namespace AllowListingAzureFunction
{
  public class UpdateInternalAndThirdPartySettingsEventHandler
  {
    private const string ListenAzureSubscriptionFunctionName = "ListenAzureSubscriptionChangeEvents";
    private const string ListenServiceTagsFunctionName = "ListenServiceTagsChangeEvents";

    private readonly IIpRestrictionService<HashSet<AzureSubscription>> _ipRestrictionServiceAz;
    private readonly IIpRestrictionService<HashSet<ServiceTag>> _ipRestrictionServiceServiceTag;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<UpdateInternalAndThirdPartySettingsEventHandler> _logger;

    public UpdateInternalAndThirdPartySettingsEventHandler(
        IIpRestrictionService<HashSet<AzureSubscription>> ipRestrictionServiceAz,
        IIpRestrictionService<HashSet<ServiceTag>> ipRestrictionServiceServiceTag,
        ICustomTelemetryService telemetry,
        ILogger<UpdateInternalAndThirdPartySettingsEventHandler> logger)
    {
      _ipRestrictionServiceAz = ipRestrictionServiceAz;
      _ipRestrictionServiceServiceTag = ipRestrictionServiceServiceTag;
      _telemetry = telemetry;
      _logger = logger;
    }

    [Function(ListenAzureSubscriptionFunctionName)]
    [QueueOutput("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")]
    public async Task<ResourceDependencyInformation[]> ListenAzSubscriptionEvents(
        [CosmosDBTrigger(
            databaseName: Constants.DatabaseName,
            collectionName: Constants.AzureSubscriptionsCollection,
            LeaseCollectionName = Constants.AzureSubscriptionsLease,
            ConnectionStringSetting = "CosmosDBConnectionString",
            CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<AzureSubscription> azureSubscriptionDocs)
    {
      var queueMessages = new List<ResourceDependencyInformation>();
      var operationId = Guid.NewGuid().ToString();
      var documentCount = azureSubscriptionDocs?.Count ?? 0;

      // Set correlation context for Cosmos DB trigger
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginCosmosTriggerScope(ListenAzureSubscriptionFunctionName, operationId, documentCount);
      using var operation = _telemetry.StartOperation("ListenAzSubscriptionEvents",
          new Dictionary<string, string> { ["OperationId"] = operationId, ["DocumentCount"] = documentCount.ToString() });

      try
      {
        _logger.LogCosmosTriggerStarted(ListenAzureSubscriptionFunctionName, operationId, documentCount);

        if (azureSubscriptionDocs != null && azureSubscriptionDocs.Count > 0)
        {
          var inputList = new HashSet<AzureSubscription>(azureSubscriptionDocs);

          // Log input processing with deduplication info
          _logger.LogInputDocumentsProcessed(ListenAzureSubscriptionFunctionName, operationId,
              azureSubscriptionDocs.Count, inputList.Count);

          // Count and log deleted vs active subscriptions
          var deletedCount = inputList.Count(s => s.IsDeleted);
          var activeCount = inputList.Count - deletedCount;
          if (deletedCount > 0)
          {
            _logger.LogDeletedDocumentsDetected(ListenAzureSubscriptionFunctionName, operationId,
                deletedCount, activeCount);
          }

          // Log each subscription with full details
          foreach (var subscription in inputList)
          {
            _logger.LogProcessingSubscriptionDocument(
                subscription.Id ?? "Unknown",
                subscription.Name ?? "Unknown",
                subscription.IsDeleted,
                operationId);
          }

          // Log before service call
          _logger.LogGetValidDependencyConfigsStarting(ListenAzureSubscriptionFunctionName, operationId, inputList.Count);

          // Fetch valid dependency configurations
          var configs = await _ipRestrictionServiceAz.GetValidDependencyConfigs(inputList);
          _logger.LogFetchedDependencyConfigs(configs.Count, operationId);

          // Log when no configs found after processing
          if (configs.Count == 0)
          {
            _logger.LogNoValidConfigsAfterProcessing(ListenAzureSubscriptionFunctionName, operationId, inputList.Count);
          }

          foreach (var config in configs)
          {
            queueMessages.Add(config);
            _logger.LogAddingConfigToQueueWithDetails(
                config.ResourceName ?? "Unknown",
                config.ResourceId ?? "Unknown",
                operationId);
          }

          // Log queue summary
          if (queueMessages.Count > 0)
          {
            var resourceNames = string.Join(", ", queueMessages.Select(q => q.ResourceName ?? "Unknown").Take(10));
            if (queueMessages.Count > 10)
            {
              resourceNames += $"... (+{queueMessages.Count - 10} more)";
            }
            _logger.LogQueueMessagesPrepared(ListenAzureSubscriptionFunctionName, operationId,
                queueMessages.Count, resourceNames);
          }

          operation.AddMetric("ConfigsQueued", queueMessages.Count);
        }
        else
        {
          _logger.LogNoDocumentsToProcess(ListenAzureSubscriptionFunctionName, operationId);
        }

        _logger.LogCosmosTriggerCompleted(ListenAzureSubscriptionFunctionName, operationId, queueMessages.Count);
        operation.SetSuccess();
      }
      catch (Exception ex)
      {
        _logger.LogCosmosTriggerFailed(ex, ListenAzureSubscriptionFunctionName, operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = ListenAzureSubscriptionFunctionName
        });
        throw;
      }
      finally
      {
        // Clear correlation context at end of function
        CorrelationContext.Clear();
      }

      return queueMessages.ToArray();
    }

    [Function(ListenServiceTagsFunctionName)]
    [QueueOutput("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")]
    public async Task<ResourceDependencyInformation[]> ListenServiceTagEvents(
        [CosmosDBTrigger(
            databaseName: Constants.DatabaseName,
            collectionName: Constants.ServiceTagsCollection,
            LeaseCollectionName = Constants.ServiceTagsLease,
            ConnectionStringSetting = "CosmosDBConnectionString",
            CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<ServiceTag> serviceTagDocs)
    {
      var queueMessages = new List<ResourceDependencyInformation>();
      var operationId = Guid.NewGuid().ToString();
      var documentCount = serviceTagDocs?.Count ?? 0;

      // Set correlation context for Cosmos DB trigger
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginCosmosTriggerScope(ListenServiceTagsFunctionName, operationId, documentCount);
      using var operation = _telemetry.StartOperation("ListenServiceTagEvents",
          new Dictionary<string, string> { ["OperationId"] = operationId, ["DocumentCount"] = documentCount.ToString() });

      try
      {
        _logger.LogCosmosTriggerStarted(ListenServiceTagsFunctionName, operationId, documentCount);

        if (serviceTagDocs != null && serviceTagDocs.Count > 0)
        {
          var inputList = new HashSet<ServiceTag>(serviceTagDocs);

          // Log input processing with deduplication info
          _logger.LogInputDocumentsProcessed(ListenServiceTagsFunctionName, operationId,
              serviceTagDocs.Count, inputList.Count);

          // Count and log deleted vs active service tags
          var deletedCount = inputList.Count(s => s.IsDeleted);
          var activeCount = inputList.Count - deletedCount;
          if (deletedCount > 0)
          {
            _logger.LogDeletedDocumentsDetected(ListenServiceTagsFunctionName, operationId,
                deletedCount, activeCount);
          }

          // Log each service tag with full details
          foreach (var serviceTag in inputList)
          {
            _logger.LogProcessingServiceTagDocument(
                serviceTag.Id ?? "Unknown",
                serviceTag.Name ?? "Unknown",
                serviceTag.IsDeleted,
                operationId);
          }

          // Log before service call
          _logger.LogGetValidDependencyConfigsStarting(ListenServiceTagsFunctionName, operationId, inputList.Count);

          // Fetch valid dependency configurations
          var configs = await _ipRestrictionServiceServiceTag.GetValidDependencyConfigs(inputList);
          _logger.LogFetchedDependencyConfigs(configs.Count, operationId);

          // Log when no configs found after processing
          if (configs.Count == 0)
          {
            _logger.LogNoValidConfigsAfterProcessing(ListenServiceTagsFunctionName, operationId, inputList.Count);
          }

          foreach (var config in configs)
          {
            queueMessages.Add(config);
            _logger.LogAddingConfigToQueueWithDetails(
                config.ResourceName ?? "Unknown",
                config.ResourceId ?? "Unknown",
                operationId);
          }

          // Log queue summary
          if (queueMessages.Count > 0)
          {
            var resourceNames = string.Join(", ", queueMessages.Select(q => q.ResourceName ?? "Unknown").Take(10));
            if (queueMessages.Count > 10)
            {
              resourceNames += $"... (+{queueMessages.Count - 10} more)";
            }
            _logger.LogQueueMessagesPrepared(ListenServiceTagsFunctionName, operationId,
                queueMessages.Count, resourceNames);
          }

          operation.AddMetric("ConfigsQueued", queueMessages.Count);
        }
        else
        {
          _logger.LogNoDocumentsToProcess(ListenServiceTagsFunctionName, operationId);
        }

        _logger.LogCosmosTriggerCompleted(ListenServiceTagsFunctionName, operationId, queueMessages.Count);
        operation.SetSuccess();
      }
      catch (Exception ex)
      {
        _logger.LogCosmosTriggerFailed(ex, ListenServiceTagsFunctionName, operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = ListenServiceTagsFunctionName
        });
        throw;
      }
      finally
      {
        // Clear correlation context at end of function
        CorrelationContext.Clear();
      }

      return queueMessages.ToArray();
    }
  }
}