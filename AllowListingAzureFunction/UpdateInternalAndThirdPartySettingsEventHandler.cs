using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using AllowListingAzureFunction.Logging;

namespace AllowListingAzureFunction
{
  public class UpdateInternalAndThirdPartySettingsEventHandler
  {
    private readonly IIpRestrictionService<HashSet<AzureSubscription>> _ipRestrictionServiceAz;
    private readonly IIpRestrictionService<HashSet<ServiceTag>> _ipRestrictionServiceServiceTag;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<UpdateInternalAndThirdPartySettingsEventHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public UpdateInternalAndThirdPartySettingsEventHandler(
        IIpRestrictionService<HashSet<AzureSubscription>> ipRestrictionServiceAz,
        IIpRestrictionService<HashSet<ServiceTag>> ipRestrictionServiceServiceTag,
        ICustomTelemetryService telemetry,
        ILogger<UpdateInternalAndThirdPartySettingsEventHandler> logger,
        TimeProvider? timeProvider = null)
    {
      _ipRestrictionServiceAz = ipRestrictionServiceAz;
      _ipRestrictionServiceServiceTag = ipRestrictionServiceServiceTag;
      _telemetry = telemetry;
      _logger = logger;
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    [Function("ListenAzureSubscriptionChangeEvents")]
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

      // *** CORRELATION FIX: Set correlation context for Cosmos DB trigger ***
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginCosmosTriggerScope("ListenAzureSubscriptionChangeEvents", operationId, documentCount);
      using var operation = _telemetry.StartOperation("ListenAzSubscriptionEvents",
          new Dictionary<string, string> { ["OperationId"] = operationId, ["DocumentCount"] = documentCount.ToString() });

      try
      {
        _logger.LogCosmosTriggerStarted("ListenAzureSubscriptionChangeEvents", operationId, documentCount);

        if (azureSubscriptionDocs != null && azureSubscriptionDocs.Count > 0)
        {
          var inputList = new HashSet<AzureSubscription>(azureSubscriptionDocs);

          foreach (var subscription in inputList)
          {
            _logger.LogProcessingSubscriptionChange(subscription.Id ?? "Unknown", operationId);
          }

          // Fetch valid dependency configurations
          var configs = await _ipRestrictionServiceAz.GetValidDependencyConfigs(inputList);
          _logger.LogFetchedDependencyConfigs(configs.Count, operationId);

          foreach (var config in configs)
          {
            queueMessages.Add(config);
            _logger.LogAddingConfigToQueue(config.ResourceName ?? "Unknown");
          }

          operation.AddMetric("ConfigsQueued", queueMessages.Count);
        }
        else
        {
          _logger.LogNoDocumentsToProcess("ListenAzureSubscriptionChangeEvents", operationId);
        }

        _logger.LogCosmosTriggerCompleted("ListenAzureSubscriptionChangeEvents", operationId, queueMessages.Count);
        operation.SetSuccess();
      }
      catch (Exception ex)
      {
        _logger.LogCosmosTriggerFailed(ex, "ListenAzureSubscriptionChangeEvents", operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = "ListenAzureSubscriptionChangeEvents"
        });
        throw;
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of function ***
        CorrelationContext.Clear();
      }

      return queueMessages.ToArray();
    }

    [Function("ListenServiceTagsChangeEvents")]
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

      // *** CORRELATION FIX: Set correlation context for Cosmos DB trigger ***
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginCosmosTriggerScope("ListenServiceTagsChangeEvents", operationId, documentCount);
      using var operation = _telemetry.StartOperation("ListenServiceTagEvents",
          new Dictionary<string, string> { ["OperationId"] = operationId, ["DocumentCount"] = documentCount.ToString() });

      try
      {
        _logger.LogCosmosTriggerStarted("ListenServiceTagsChangeEvents", operationId, documentCount);

        if (serviceTagDocs != null && serviceTagDocs.Count > 0)
        {
          var inputList = new HashSet<ServiceTag>(serviceTagDocs);

          foreach (var serviceTag in inputList)
          {
            _logger.LogProcessingServiceTagChange(serviceTag.Name ?? "Unknown", serviceTag.Id ?? "Unknown", operationId);
          }

          // Fetch valid dependency configurations
          var configs = await _ipRestrictionServiceServiceTag.GetValidDependencyConfigs(inputList);
          _logger.LogFetchedDependencyConfigs(configs.Count, operationId);

          foreach (var config in configs)
          {
            queueMessages.Add(config);
            _logger.LogAddingConfigToQueue(config.ResourceName ?? "Unknown");
          }

          operation.AddMetric("ConfigsQueued", queueMessages.Count);
        }
        else
        {
          _logger.LogNoDocumentsToProcess("ListenServiceTagsChangeEvents", operationId);
        }

        _logger.LogCosmosTriggerCompleted("ListenServiceTagsChangeEvents", operationId, queueMessages.Count);
        operation.SetSuccess();
      }
      catch (Exception ex)
      {
        _logger.LogCosmosTriggerFailed(ex, "ListenServiceTagsChangeEvents", operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = "ListenServiceTagsChangeEvents"
        });
        throw;
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of function ***
        CorrelationContext.Clear();
      }

      return queueMessages.ToArray();
    }
  }
}