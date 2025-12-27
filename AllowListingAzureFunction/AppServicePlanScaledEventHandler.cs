using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using AllowListingAzureFunction.Logging;

namespace AllowListingAzureFunction
{
  public class AppServicePlanScaledEventHandler
  {
    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<AppServicePlanScaledEventHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public AppServicePlanScaledEventHandler(
        IDynamicAllowListingService dynamicAllowListingHelper,
        ICustomTelemetryService telemetry,
        ILogger<AppServicePlanScaledEventHandler> logger,
        TimeProvider? timeProvider = null)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper;
      _telemetry = telemetry;
      _logger = logger;
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    [Function("AppServicePlanScaledEventHandler")]
    [QueueOutput("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")]
    public async Task<ResourceDependencyInformation[]> Run(
        [QueueTrigger("apspl-scaled", Connection = "StorageQueueStorageAccountConnectionString")] string queueItem)
    {
      var operationId = Guid.NewGuid().ToString();
      var overwriteQueue = new List<ResourceDependencyInformation>();

      // *** CORRELATION FIX: Set correlation context for queue trigger ***
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginFunctionScope(nameof(AppServicePlanScaledEventHandler), operationId);
      using var operation = _telemetry.StartOperation("AppServicePlanScaledEventHandler",
          new Dictionary<string, string> { ["OperationId"] = operationId });

      try
      {
        _logger.LogQueueTriggerStarted(nameof(AppServicePlanScaledEventHandler), operationId, "AppServicePlanScaling");
        _logger.LogProcessingQueueMessage("AppServicePlanScaling", queueItem);

        var eventGridData = JsonConvert.DeserializeObject<EventGridData>(queueItem);

        if (eventGridData?.ResourceId == null)
        {
          _logger.LogOperationErrors(operationId, $"Null ResourceId in queue item: {queueItem}");
          operation.SetFailed("Null ResourceId");
          throw new ArgumentNullException("ResourceId", $"Null ResourceId found in queue item");
        }

        operation.AddProperty("ResourceId", eventGridData.ResourceId);

        if (!ValidationHelper.IsValidAppServicePlanId(eventGridData.ResourceId))
        {
          _logger.LogOperationCompletedWithErrors(operationId, 1);
          _logger.LogQueueTriggerCompleted(nameof(AppServicePlanScaledEventHandler), operationId, true);
          operation.SetSuccess();
          return overwriteQueue.ToArray();
        }

        // Get resources hosted on this App Service Plan
        var configs = await _dynamicAllowListingHelper.GetOverwriteConfigsForAppServicePlanScale(eventGridData.ResourceId);
        _logger.LogFetchedDependencyConfigs(configs.Count, operationId);

        foreach (var config in configs)
        {
          overwriteQueue.Add(config);
          _logger.LogAddingConfigToQueue(config.ResourceName ?? "Unknown");
        }

        operation.AddMetric("ConfigsQueued", overwriteQueue.Count);
        _logger.LogQueueTriggerCompleted(nameof(AppServicePlanScaledEventHandler), operationId, true);
        operation.SetSuccess();
      }
      catch (Exception ex)
      {
        _logger.LogQueueTriggerFailed(ex, nameof(AppServicePlanScaledEventHandler), operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = nameof(AppServicePlanScaledEventHandler),
          ["QueueItem"] = queueItem
        });
        throw;
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of function ***
        CorrelationContext.Clear();
      }

      return overwriteQueue.ToArray();
    }
  }
}