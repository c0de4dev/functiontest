using DynamicAllowListingLib;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace AllowListingAzureFunction
{
  public class AppServicePlanScaledEventHandler
  {
    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly TelemetryClient _telemetryClient;

    public AppServicePlanScaledEventHandler(IDynamicAllowListingService dynamicAllowListingHelper, 
    TelemetryClient telemetryClient)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper;
      _telemetryClient = telemetryClient;
    }

    [Function("AppServicePlanScaledEventHandler")]
    [QueueOutput("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")]
    public async Task<ResourceDependencyInformation[]> Run(
      [QueueTrigger("apspl-scaled", Connection = "StorageQueueStorageAccountConnectionString")] string queueItem)
    {
      var operationId = Guid.NewGuid().ToString(); // Generate a unique operation ID for correlation

      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("AppServicePlanScaledEventHandler", operationId))
      {
        var overwriteQueue = new List<ResourceDependencyInformation>();
        try
        {
          // Log the incoming queue item
          _telemetryClient.TrackTrace($"Processing scaling event for queueItem: {queueItem}", SeverityLevel.Information);
          var eventGridData = JsonConvert.DeserializeObject<EventGridData>(queueItem);

          if (eventGridData?.ResourceId == null)
          {
            var errorMessage = $"Null ResourceId found in queue item: {queueItem}";
            _telemetryClient.TrackTrace(errorMessage, SeverityLevel.Error);
            throw new ArgumentNullException("ResourceId", errorMessage); // Throw with context for clearer debugging
          }

          if (!ValidationHelper.IsValidAppServicePlanId(eventGridData.ResourceId))
          {
            var errorMessage = $"Invalid ResourceId in queue item: {queueItem}";
            _telemetryClient.TrackTrace(errorMessage, SeverityLevel.Error);
            throw new ArgumentException("Invalid App Service Plan ID", "ResourceId");
          }

          _telemetryClient.TrackTrace($"Scaling detected for ResourceId: {eventGridData.ResourceId}", SeverityLevel.Information);


          var configsToOverwrite = await
            _dynamicAllowListingHelper.GetOverwriteConfigsForAppServicePlanScale(eventGridData.ResourceId);

          if (configsToOverwrite == null || configsToOverwrite.Count == 0)
          {
            _telemetryClient.TrackTrace($"No overwrite configurations found for ResourceId: {eventGridData.ResourceId}", SeverityLevel.Information);
          }
          else
          {
            foreach (var resourceDependencyInformation in configsToOverwrite)
            {
              overwriteQueue.Add(resourceDependencyInformation);
              _telemetryClient.TrackTrace($"Added {resourceDependencyInformation.ResourceName} to overwrite queue.", SeverityLevel.Information);
            }
          }
          return overwriteQueue.ToArray();
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "QueueItem", queueItem },
                              { "OperationId", operationId }
                          });
          throw; // Re-throw the exception to propagate it
        }
        finally
        {
          _telemetryClient.StopOperation(operation); // Ensure operation is stopped
        }
      }
    }
  }
}