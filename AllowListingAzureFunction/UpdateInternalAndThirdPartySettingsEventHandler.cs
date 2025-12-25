using DynamicAllowListingLib;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Azure.Storage.Queues.Models;

namespace AllowListingAzureFunction
{
  public class UpdateInternalAndThirdPartySettingsEventHandler
  {
    private readonly IIpRestrictionService<HashSet<AzureSubscription>> _ipRestrictionServiceAz;
    private readonly IIpRestrictionService<HashSet<ServiceTag>> _ipRestrictionServiceServiceTag;
    private readonly TelemetryClient _telemetryClient;

    public UpdateInternalAndThirdPartySettingsEventHandler(IIpRestrictionService<HashSet<AzureSubscription>> ipRestrictionServiceAz,
                                                           IIpRestrictionService<HashSet<ServiceTag>> ipRestrictionServiceServiceTag,
                                                           TelemetryClient telemetryClient)
    {
      _ipRestrictionServiceAz = ipRestrictionServiceAz;
      _ipRestrictionServiceServiceTag = ipRestrictionServiceServiceTag;
      _telemetryClient = telemetryClient;
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
      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing

      // Start logging and tracking the operation with telemetry
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("ListenAzSubscriptionEvents", operationId))
      {
        _telemetryClient.Context.Operation.Id = operationId;
        _telemetryClient.TrackTrace($"Starting subscription change event processing. OperationId: {operationId}", SeverityLevel.Information);

        try
        {
          // Log the number of subscription documents received
          if (azureSubscriptionDocs != null && azureSubscriptionDocs.Count > 0)
          {
            _telemetryClient.TrackTrace($"Received {azureSubscriptionDocs.Count} Azure subscription changes.", SeverityLevel.Information);

            var inputList = new HashSet<AzureSubscription>(azureSubscriptionDocs);
            foreach (var model in inputList)
            {
              _telemetryClient.TrackTrace($"Processing update for subscription: {model.Name} (ID: {model.Id})", SeverityLevel.Information);
            }

            // Fetch valid dependency configurations based on the received subscriptions
            var configs = await _ipRestrictionServiceAz.GetValidDependencyConfigs(inputList);

            // Log configurations added to the queue
            foreach (var config in configs)
            {
              queueMessages.Add(config);
              _telemetryClient.TrackTrace($"Config for resource '{config.ResourceName}' added to queue.", SeverityLevel.Information);
            }
          }
          else
          {
            _telemetryClient.TrackTrace("No Azure subscription changes detected.", SeverityLevel.Warning);
          }
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });
          _telemetryClient.TrackTrace($"Error occurred during Azure subscription event processing. OperationId: {operationId}. Exception: {ex.Message}, Inner Exception: {ex.InnerException?.Message}", SeverityLevel.Error);
          throw;
        }
        finally
        {
          _telemetryClient.TrackTrace($"Finished processing Azure subscription events. OperationId: {operationId}", SeverityLevel.Information);
        }

        // Return the collected queue messages
        return queueMessages.ToArray();
      }
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
      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing

      // Start logging and tracking the operation with telemetry
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("ListenServiceTagEvents", operationId))
      {
        _telemetryClient.Context.Operation.Id = operationId;
        _telemetryClient.TrackTrace($"Starting service tag change event processing. OperationId: {operationId}", SeverityLevel.Information);

        try
        {
          // Log the number of service tag documents received
          if (serviceTagDocs != null && serviceTagDocs.Count > 0)
          {
            _telemetryClient.TrackTrace($"Received {serviceTagDocs.Count} service tag changes.", SeverityLevel.Information);

            var inputList = new HashSet<ServiceTag>(serviceTagDocs);
            foreach (var model in inputList)
            {
              _telemetryClient.TrackTrace($"Processing update for service tag: {model.Name} (ID: {model.Id})", SeverityLevel.Information);
            }

            // Fetch valid dependency configurations based on the received service tags
            var configs = await _ipRestrictionServiceServiceTag.GetValidDependencyConfigs(inputList);

            // Log configurations added to the queue
            foreach (var config in configs)
            {
              queueMessages.Add(config);
              _telemetryClient.TrackTrace($"Config for resource '{config.ResourceName}' added to queue.", SeverityLevel.Information);
            }
          }
          else
          {
            _telemetryClient.TrackTrace("No service tag changes detected.", SeverityLevel.Warning);
          }
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });

          _telemetryClient.TrackTrace($"Error occurred during service tag event processing. OperationId: {operationId}. Exception: {ex.Message}, Inner Exception: {ex.InnerException?.Message}", SeverityLevel.Error);

          // Rethrow the exception after logging it
          throw;
        }
        finally
        {
          // Final telemetry operation stop
          _telemetryClient.TrackTrace($"Finished processing service tag events. OperationId: {operationId}", SeverityLevel.Information);
        }

        // Return the collected queue messages
        return queueMessages.ToArray();
      }
    }

  }
}