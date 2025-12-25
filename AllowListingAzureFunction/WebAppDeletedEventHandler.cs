using DynamicAllowListingLib;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace AllowListingAzureFunction
{
  public class WebAppDeletedEventHandler
  {
    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly ILogger<WebAppDeletedEventHandler> _logger;
    private readonly TelemetryClient _telemetryClient;

    public WebAppDeletedEventHandler(IDynamicAllowListingService dynamicAllowListingHelper, 
    ILogger<WebAppDeletedEventHandler> logger,
    TelemetryClient telemetryClient)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper;
      _logger = logger;
      _telemetryClient = telemetryClient;
    }

    [Function("WebAppDeletedEventHandler")]
    [QueueOutput("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")]
    public async Task<ResourceDependencyInformation[]> Run(
        [QueueTrigger("webapp-deleted", Connection = "StorageQueueStorageAccountConnectionString")] string queueItem)
    {
      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing

      // Start logging and tracking the operation with telemetry
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("WebAppDeletedEventHandler", operationId))
      {
        _telemetryClient.Context.Operation.Id = operationId;
        _telemetryClient.TrackTrace($"Starting WebAppDeletedEventHandler for OperationId: {operationId}", SeverityLevel.Information);

        var overwriteQueue = new List<ResourceDependencyInformation>();
        try
        {
          var eventGridData = JsonConvert.DeserializeObject<EventGridData>(queueItem);

          // Null check for resourceId
          if (eventGridData?.ResourceId == null)
          {
            _telemetryClient.TrackTrace($"Null ResourceId in queue item. OperationId: {operationId}, QueueItem: {queueItem}", SeverityLevel.Warning);
            return overwriteQueue.ToArray();
          }

          // Validate the resourceId
          if (!ValidationHelper.IsValidWebSiteId(eventGridData.ResourceId))
          {
            _telemetryClient.TrackTrace($"Invalid ResourceId in queue item. OperationId: {operationId}, QueueItem: {queueItem}", SeverityLevel.Warning);
            return overwriteQueue.ToArray();
          }

          // Log successful resource detection
          _telemetryClient.TrackTrace($"Resource deletion detected for resourceId: {eventGridData.ResourceId}. OperationId: {operationId}", SeverityLevel.Information);

          // Get the configs to overwrite based on the resourceId
          var configsToOverwrite = await _dynamicAllowListingHelper.GetOverwriteConfigsWhenWebAppDeleted(eventGridData.ResourceId);

          foreach (var resourceDependencyInformation in configsToOverwrite)
          {
            overwriteQueue.Add(resourceDependencyInformation);
            _telemetryClient.TrackTrace($"{resourceDependencyInformation.ResourceName} config added to overwrite queue. OperationId: {operationId}", SeverityLevel.Information);
          }
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "QueueItem", queueItem },
                              { "OperationId", operationId }
                          });

          // Log the error and provide meaningful feedback
          _telemetryClient.TrackTrace($"Error occurred while processing queue item. OperationId: {operationId}, Error: {ex.Message}", SeverityLevel.Error);
          return overwriteQueue.ToArray();
        }
        finally
        {
          // Stop the operation after processing
          _telemetryClient.TrackTrace($"Finished processing WebAppDeletedEventHandler. OperationId: {operationId}", SeverityLevel.Information);
        }

        return overwriteQueue.ToArray();
      }
    }

  }
}