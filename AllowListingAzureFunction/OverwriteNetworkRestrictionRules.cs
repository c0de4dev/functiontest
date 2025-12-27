using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using static AllowListingAzureFunction.UpdateNetworkRestrictionsUsingConfig;
using System.Collections.Generic;
using AllowListingAzureFunction.Logging;

namespace AllowListingAzureFunction
{
  public class OverwriteNetworkRestrictionRules
  {
    public const string OverwriteNetworkRestrictionsActivityFunction = nameof(OverwriteNetworkRestrictionsActivityTrigger);

    private readonly IDynamicAllowListingService _dalService;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<OverwriteNetworkRestrictionRules> _logger;
    private readonly TimeProvider _timeProvider;

    public OverwriteNetworkRestrictionRules(
        IDynamicAllowListingService dalService,
        ICustomTelemetryService telemetry,
        ILogger<OverwriteNetworkRestrictionRules> logger,
        TimeProvider? timeProvider = null)
    {
      _dalService = dalService;
      _telemetry = telemetry;
      _logger = logger;
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    [Function(nameof(OverwriteNetworkRestrictionsActivityTrigger))]
    public async Task<ResultObject> OverwriteNetworkRestrictionsActivityTrigger(
        [ActivityTrigger] OrchestrationParameters parameters,
        FunctionContext context)
    {
      if (parameters == null)
      {
        _logger.LogNullParameters(nameof(OverwriteNetworkRestrictionsActivityTrigger));
        throw new ArgumentNullException(nameof(parameters));
      }

      var instanceId = parameters.InvocationID;
      var resourceInfo = parameters.InputData;
      var result = new ResultObject();

      // *** CORRELATION FIX: Set correlation context from parent InvocationID ***
      CorrelationContext.SetCorrelationId(instanceId);

      using var loggerScope = _logger.BeginActivityScope(
          nameof(OverwriteNetworkRestrictionsActivityTrigger),
          instanceId,
          context.InvocationId,
          resourceInfo.ResourceName);

      using var operation = _telemetry.StartOperation(
          "OverwriteNetworkRestrictionsActivity",
          new Dictionary<string, string>
          {
            ["InstanceId"] = instanceId,
            ["ResourceName"] = resourceInfo.ResourceName ?? "Unknown",
            ["ResourceId"] = resourceInfo.ResourceId ?? "Unknown"
          });

      try
      {
        result.FunctionNames.Add(nameof(OverwriteNetworkRestrictionsActivityTrigger));
        result.InvocationIDs.Add(context.InvocationId);

        _logger.LogActivityStarted(nameof(OverwriteNetworkRestrictionsActivityTrigger), instanceId, context.InvocationId);
        _logger.LogOverwritingNetworkRestrictions(resourceInfo.ResourceName ?? "Unknown", instanceId);

        // Perform the overwrite
        result = await _dalService.OverwriteNetworkRestrictionRulesForMainResource(resourceInfo);

        _logger.LogNetworkRestrictionsOverwritten(resourceInfo.ResourceName ?? "Unknown", instanceId);
        _logger.LogActivityCompleted(nameof(OverwriteNetworkRestrictionsActivityTrigger), instanceId, result.Success);

        operation.SetSuccess();
      }
      catch (Exception ex)
      {
        _logger.LogActivityFailed(ex, nameof(OverwriteNetworkRestrictionsActivityTrigger), instanceId);
        _logger.LogNetworkRestrictionsOverwriteFailed(ex, resourceInfo.ResourceName ?? "Unknown", instanceId);

        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["InstanceId"] = instanceId,
          ["Activity"] = nameof(OverwriteNetworkRestrictionsActivityTrigger),
          ["ResourceName"] = resourceInfo.ResourceName ?? "Unknown"
        });

        result.Errors.Add($"Error overwriting network restrictions: {ex.Message}");
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of activity ***
        CorrelationContext.Clear();
      }

      return result;
    }

    [Function(nameof(OverwriteNetworkRestrictionsQueueTrigger))]
    public async Task OverwriteNetworkRestrictionsQueueTrigger(
        [QueueTrigger("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")] ResourceDependencyInformation config,
        FunctionContext context)
    {
      var instanceId = context.InvocationId.ToString();

      // *** CORRELATION FIX: Set correlation context for queue trigger ***
      CorrelationContext.SetCorrelationId(instanceId);

      using var loggerScope = _logger.BeginFunctionScope(nameof(OverwriteNetworkRestrictionsQueueTrigger), instanceId);
      using var operation = _telemetry.StartOperation(
          "OverwriteNetworkRestrictionsQueueTrigger",
          new Dictionary<string, string>
          {
            ["InstanceId"] = instanceId,
            ["ResourceName"] = config.ResourceName ?? "Unknown",
            ["ResourceId"] = config.ResourceId ?? "Unknown"
          });

      try
      {
        _logger.LogQueueTriggerStarted(nameof(OverwriteNetworkRestrictionsQueueTrigger), instanceId, config.ResourceName ?? "Unknown");
        _logger.LogProcessingQueueMessage(config.ResourceName ?? "Unknown", config.ResourceId ?? "Unknown");
        _logger.LogOverwritingNetworkRestrictions(config.ResourceName ?? "Unknown", instanceId);

        // Perform the overwrite
        var result = await _dalService.OverwriteNetworkRestrictionRulesForMainResource(config);

        _logger.LogNetworkRestrictionsOverwritten(config.ResourceName ?? "Unknown", instanceId);
        _logger.LogQueueTriggerCompleted(nameof(OverwriteNetworkRestrictionsQueueTrigger), instanceId, result.Success);

        if (result.Errors.Count > 0)
        {
          _logger.LogOperationCompletedWithErrors(instanceId, result.Errors.Count);
          operation.SetFailed($"{result.Errors.Count} errors");
        }
        else
        {
          operation.SetSuccess();
        }
      }
      catch (Exception ex)
      {
        _logger.LogQueueTriggerFailed(ex, nameof(OverwriteNetworkRestrictionsQueueTrigger), instanceId);
        _logger.LogNetworkRestrictionsOverwriteFailed(ex, config.ResourceName ?? "Unknown", instanceId);

        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["InstanceId"] = instanceId,
          ["Function"] = nameof(OverwriteNetworkRestrictionsQueueTrigger),
          ["ResourceName"] = config.ResourceName ?? "Unknown"
        });
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of function ***
        CorrelationContext.Clear();
      }
    }
  }
}