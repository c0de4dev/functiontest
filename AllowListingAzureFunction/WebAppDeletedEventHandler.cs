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
  public class WebAppDeletedEventHandler
  {
    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<WebAppDeletedEventHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public WebAppDeletedEventHandler(
        IDynamicAllowListingService dynamicAllowListingHelper,
        ICustomTelemetryService telemetry,
        ILogger<WebAppDeletedEventHandler> logger,
        TimeProvider? timeProvider = null)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper;
      _telemetry = telemetry;
      _logger = logger;
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    [Function("WebAppDeletedEventHandler")]
    [QueueOutput("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")]
    public async Task<ResourceDependencyInformation[]> Run(
        [QueueTrigger("webapp-deleted", Connection = "StorageQueueStorageAccountConnectionString")] string queueItem)
    {
      var operationId = Guid.NewGuid().ToString();
      var overwriteQueue = new List<ResourceDependencyInformation>();

      // *** CORRELATION FIX: Set correlation context for queue trigger ***
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginFunctionScope(nameof(WebAppDeletedEventHandler), operationId);
      using var operation = _telemetry.StartOperation("WebAppDeletedEventHandler",
          new Dictionary<string, string> { ["OperationId"] = operationId });

      try
      {
        _logger.LogQueueTriggerStarted(nameof(WebAppDeletedEventHandler), operationId, "WebAppDeleted");
        _logger.LogProcessingQueueMessage("WebAppDeleted", queueItem);

        var eventGridData = JsonConvert.DeserializeObject<EventGridData>(queueItem);

        // Null check for resourceId
        if (eventGridData?.ResourceId == null)
        {
          _logger.LogOperationErrors(operationId, $"Null ResourceId in queue item: {queueItem}");
          operation.SetFailed("Null ResourceId");
          return overwriteQueue.ToArray();
        }

        operation.AddProperty("ResourceId", eventGridData.ResourceId);

        // Validate the resourceId
        if (!ValidationHelper.IsValidWebSiteId(eventGridData.ResourceId))
        {
          _logger.LogOperationCompletedWithErrors(operationId, 1);
          _logger.LogOperationErrors(operationId, $"Invalid WebSite ResourceId: {eventGridData.ResourceId}");
          _logger.LogQueueTriggerCompleted(nameof(WebAppDeletedEventHandler), operationId, true);
          operation.SetSuccess();
          return overwriteQueue.ToArray();
        }

        _logger.LogProcessingResource(nameof(WebAppDeletedEventHandler), eventGridData.ResourceId, "WebApp");

        // Get configs to overwrite after web app deletion
        var configs = await _dynamicAllowListingHelper.GetOverwriteConfigsWhenWebAppDeleted(eventGridData.ResourceId);
        _logger.LogFetchedDependencyConfigs(configs.Count, operationId);

        foreach (var config in configs)
        {
          overwriteQueue.Add(config);
          _logger.LogAddingConfigToQueue(config.ResourceName ?? "Unknown");
        }

        operation.AddMetric("ConfigsQueued", overwriteQueue.Count);
        _logger.LogQueueTriggerCompleted(nameof(WebAppDeletedEventHandler), operationId, true);
        operation.SetSuccess();
      }
      catch (Exception ex)
      {
        _logger.LogQueueTriggerFailed(ex, nameof(WebAppDeletedEventHandler), operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = nameof(WebAppDeletedEventHandler),
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