using AllowListingAzureFunction.Logging;
using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AllowListingAzureFunction
{
  public class UpdateNetworkRestrictionsUsingConfig
  {
    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly ISettingValidator<ResourceDependencyInformation> _validator;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<UpdateNetworkRestrictionsUsingConfig> _logger;
    private readonly TimeProvider _timeProvider;

    public UpdateNetworkRestrictionsUsingConfig(
        IDynamicAllowListingService dynamicAllowListingHelper,
        ISettingValidator<ResourceDependencyInformation> validator,
        ICustomTelemetryService telemetry,
        ILogger<UpdateNetworkRestrictionsUsingConfig> logger,
        TimeProvider? timeProvider = null)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper;
      _validator = validator;
      _telemetry = telemetry;
      _logger = logger;
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    [Function("UpdateNetworkRestrictionsUsingConfig")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient starter)
    {
      var operationId = Guid.NewGuid().ToString();

      // *** CORRELATION FIX: Set correlation context for library layer ***
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginFunctionScope(nameof(UpdateNetworkRestrictionsUsingConfig), operationId);
      using var operation = _telemetry.StartOperation("UpdateNetworkRestrictionsUsingConfig",
          new Dictionary<string, string> { ["OperationId"] = operationId });

      try
      {
        _logger.LogHttpFunctionStarted(nameof(UpdateNetworkRestrictionsUsingConfig), operationId, req.Method);

        // Read request body
        _logger.LogReadingRequestBody(operationId);
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
          _logger.LogEmptyRequestBody(operationId);
          _logger.LogHttpFunctionErrorResponse(nameof(UpdateNetworkRestrictionsUsingConfig), operationId, 400, "Empty request body");
          operation.SetFailed("Empty request body");
          return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        _logger.LogRequestBodyReceived(operationId, requestBody.Length);

        // Parse JSON
        _logger.LogParsingRequestJson(operationId);
        var parseResult = requestBody.TryParseJson(out ResourceDependencyInformation model);

        if (!parseResult.Success)
        {
          _logger.LogJsonParsingFailed(operationId, string.Join(", ", parseResult.Errors));
          operation.SetFailed("JSON parsing failed");
          var response = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
          await response.WriteAsJsonAsync(parseResult);
          return response;
        }

        _logger.LogJsonParsedSuccessfully(operationId, model?.ResourceName ?? "Unknown");
        operation.AddProperty("ResourceName", model?.ResourceName ?? "Unknown");

        // Validate model
        _logger.LogValidatingModel(operationId);
        var validationResult = _validator.Validate(model!);

        if (!validationResult.Success)
        {
          _logger.LogModelValidationFailed(operationId, validationResult.Errors.Count);
          operation.SetFailed("Validation failed");
          var response = req.CreateResponse(HttpStatusCode.BadRequest);
          await response.WriteAsJsonAsync(validationResult);
          return response;
        }

        _logger.LogModelValidationSucceeded(operationId, model!.ResourceName ?? "Unknown");

        // Start orchestration
        var orchestrationParams = new OrchestrationParameters
        {
          InvocationID = operationId,
          InputData = model!
        };

        _logger.LogStartingOrchestration(nameof(UpdateNetworkRestrictionsOrchestrator), operationId, operationId);

        string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(
            nameof(UpdateNetworkRestrictionsOrchestrator),
            orchestrationParams);

        _logger.LogOrchestrationStarted(instanceId, $"/runtime/webhooks/durabletask/instances/{instanceId}");
        operation.AddProperty("InstanceId", instanceId);

        // Wait for completion
        OrchestrationMetadata? metadata = await starter.WaitForInstanceCompletionAsync(instanceId, getInputsAndOutputs: true);
        var resultObject = metadata?.ReadOutputAs<ResultObject>();

        // Track result
        operation.AddMetric("ErrorCount", resultObject?.Errors.Count ?? 0);

        if (resultObject?.Errors.Count > 0)
        {
          _logger.LogOperationCompletedWithErrors(operationId, resultObject.Errors.Count);
          _logger.LogOperationErrors(operationId, string.Join("; ", resultObject.Errors));
          operation.SetFailed($"{resultObject.Errors.Count} errors");
        }
        else
        {
          operation.SetSuccess();
        }

        _logger.LogHttpFunctionCompleted(nameof(UpdateNetworkRestrictionsUsingConfig), operationId, 200, (long)operation.Elapsed.TotalMilliseconds);

        var resp = req.CreateResponse(HttpStatusCode.OK);
        await resp.WriteAsJsonAsync(resultObject);
        return resp;
      }
      catch (Exception ex)
      {
        _logger.LogHttpFunctionFailed(ex, nameof(UpdateNetworkRestrictionsUsingConfig), operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = nameof(UpdateNetworkRestrictionsUsingConfig)
        });

        return req.CreateResponse(HttpStatusCode.InternalServerError);
      }
      finally
      {
        // *** CORRELATION FIX: Clear correlation context at end of request ***
        CorrelationContext.Clear();
      }
    }

    [Function(nameof(UpdateNetworkRestrictionsOrchestrator))]
    public async Task<ResultObject> UpdateNetworkRestrictionsOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
      var parameters = context.GetInput<OrchestrationParameters>();
      if (parameters == null)
      {
        _logger.LogNullParameters(nameof(UpdateNetworkRestrictionsOrchestrator));
        throw new ArgumentNullException(nameof(parameters));
      }

      var instanceId = context.InstanceId;
      var resultObject = new ResultObject();

      // *** CORRELATION FIX: Orchestrators inherit correlation from parent via InvocationID ***
      // Note: Orchestrators must be deterministic - we pass correlation through parameters
      // The correlation context will be set in each activity function

      using var loggerScope = _logger.BeginOrchestratorScope(nameof(UpdateNetworkRestrictionsOrchestrator), instanceId, parameters.InvocationID);

      try
      {
        _logger.LogOrchestratorStarted(nameof(UpdateNetworkRestrictionsOrchestrator), instanceId, parameters.InvocationID);

        var activityParams = new OrchestrationParameters
        {
          InvocationID = parameters.InvocationID, // Pass parent correlation ID
          InputData = parameters.InputData
        };

        // Activity 1: Update DB and unmanaged resources
        _logger.LogCallingActivity(nameof(UpdateDbAndUnmanagedResources), instanceId, parameters.InputData.ResourceName ?? "Unknown");
        var unmanagedResult = await context.CallActivityAsync<ResultObject>(nameof(UpdateDbAndUnmanagedResources), activityParams);
        resultObject.Merge(unmanagedResult);
        _logger.LogActivityReturned(nameof(UpdateDbAndUnmanagedResources), instanceId, unmanagedResult.Success);

        // Activity 2: Overwrite main resource
        _logger.LogCallingActivity(OverwriteNetworkRestrictionRules.OverwriteNetworkRestrictionsActivityFunction, instanceId, parameters.InputData.ResourceName ?? "Unknown");
        var mainResult = await context.CallActivityAsync<ResultObject>(OverwriteNetworkRestrictionRules.OverwriteNetworkRestrictionsActivityFunction, activityParams);
        resultObject.Merge(mainResult);
        _logger.LogActivityReturned(OverwriteNetworkRestrictionRules.OverwriteNetworkRestrictionsActivityFunction, instanceId, mainResult.Success);

        // Early exit on errors
        if (!resultObject.Success || resultObject.Errors.Count > 0)
        {
          _logger.LogOrchestratorCompletedWithErrors(nameof(UpdateNetworkRestrictionsOrchestrator), instanceId, resultObject.Errors.Count);
          return resultObject;
        }

        // Activity 3: Get overwrite configs
        _logger.LogCallingActivity(nameof(GetOverwriteConfigs), instanceId, parameters.InputData.ResourceName ?? "Unknown");
        var configsOutput = await context.CallActivityAsync<OverriteConfigOutput>(nameof(GetOverwriteConfigs), activityParams);

        if (configsOutput.ResourceDependencyInformation.Count == 0)
        {
          resultObject.Merge(configsOutput.OverwriteConfigResult);
          _logger.LogNoOverwriteConfigsFound(instanceId);
          _logger.LogOrchestratorCompleted(nameof(UpdateNetworkRestrictionsOrchestrator), instanceId, true);
          return resultObject;
        }

        _logger.LogOverwriteConfigsRetrieved(configsOutput.ResourceDependencyInformation.Count, instanceId);

        // Fan-out: Process all configs in parallel
        _logger.LogSchedulingParallelActivities(configsOutput.ResourceDependencyInformation.Count, instanceId);

        var tasks = new List<Task<ResultObject>>();
        foreach (var config in configsOutput.ResourceDependencyInformation)
        {
          _logger.LogCallingActivity(OverwriteNetworkRestrictionRules.OverwriteNetworkRestrictionsActivityFunction, instanceId, config.ResourceName ?? "Unknown");
          tasks.Add(context.CallActivityAsync<ResultObject>(
              OverwriteNetworkRestrictionRules.OverwriteNetworkRestrictionsActivityFunction,
              new OrchestrationParameters { InvocationID = parameters.InvocationID, InputData = config }));
        }

        // Fan-in
        var results = await Task.WhenAll(tasks);

        if (results.Length != configsOutput.ResourceDependencyInformation.Count)
        {
          _logger.LogActivityResultMismatch(configsOutput.ResourceDependencyInformation.Count, results.Length, instanceId);
          resultObject.Errors.Add($"Result mismatch: expected {configsOutput.ResourceDependencyInformation.Count}, got {results.Length}");
        }

        int successCount = 0, failureCount = 0;
        foreach (var result in results)
        {
          resultObject.Merge(result);
          if (result.Success) successCount++; else failureCount++;
        }

        _logger.LogParallelActivitiesCompleted(successCount, failureCount, instanceId);
        _logger.LogOrchestratorCompleted(nameof(UpdateNetworkRestrictionsOrchestrator), instanceId, resultObject.Success);

        return resultObject;
      }
      catch (Exception ex)
      {
        _logger.LogOrchestratorFailed(ex, nameof(UpdateNetworkRestrictionsOrchestrator), instanceId);
        resultObject.Errors.Add($"Orchestrator exception: {ex.Message}");
        return resultObject;
      }
    }

    [Function(nameof(UpdateDbAndUnmanagedResources))]
    public async Task<ResultObject> UpdateDbAndUnmanagedResources(
        [ActivityTrigger] OrchestrationParameters parameters,
        FunctionContext context)
    {
      if (parameters == null)
      {
        _logger.LogNullParameters(nameof(UpdateDbAndUnmanagedResources));
        throw new ArgumentNullException(nameof(parameters));
      }

      var resourceInfo = parameters.InputData;
      var instanceId = parameters.InvocationID;
      var result = new ResultObject();

      // *** CORRELATION FIX: Set correlation context from parent InvocationID ***
      CorrelationContext.SetCorrelationId(instanceId);

      using var loggerScope = _logger.BeginActivityScope(nameof(UpdateDbAndUnmanagedResources), instanceId, context.InvocationId, resourceInfo.ResourceName);
      using var operation = _telemetry.StartOperation("UpdateDbAndUnmanagedResources",
          new Dictionary<string, string>
          {
            ["InstanceId"] = instanceId,
            ["ResourceName"] = resourceInfo.ResourceName ?? "Unknown",
            ["ResourceId"] = resourceInfo.ResourceId ?? "Unknown"
          });

      try
      {
        result.FunctionNames.Add(nameof(UpdateDbAndUnmanagedResources));
        result.InvocationIDs.Add(context.InvocationId);

        _logger.LogActivityStarted(nameof(UpdateDbAndUnmanagedResources), instanceId, context.InvocationId);

        // Update database
        _logger.LogDatabaseUpdateStarted(resourceInfo.ResourceName ?? "Unknown", instanceId);
        var dbResult = await _dynamicAllowListingHelper.UpdateDb(resourceInfo);
        result.Merge(dbResult);
        _logger.LogDatabaseUpdateCompleted(resourceInfo.ResourceName ?? "Unknown", instanceId);

        // Update unmanaged resources
        _logger.LogUnmanagedResourcesUpdateStarted(resourceInfo.ResourceName ?? "Unknown", instanceId);
        var unmanagedResult = await _dynamicAllowListingHelper.UpdateUnmanagedResources(resourceInfo);
        result.Merge(unmanagedResult);
        _logger.LogUnmanagedResourcesUpdateCompleted(resourceInfo.ResourceName ?? "Unknown", instanceId);

        if (result.Errors.Count > 0)
        {
          _logger.LogOperationCompletedWithErrors(instanceId, result.Errors.Count);
          operation.SetFailed($"{result.Errors.Count} errors");
        }
        else
        {
          operation.SetSuccess();
        }

        _logger.LogActivityCompleted(nameof(UpdateDbAndUnmanagedResources), instanceId, result.Success);
        return result;
      }
      catch (Exception ex)
      {
        _logger.LogActivityFailed(ex, nameof(UpdateDbAndUnmanagedResources), instanceId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string> { ["InstanceId"] = instanceId, ["Activity"] = nameof(UpdateDbAndUnmanagedResources) });
        result.Errors.Add($"Exception: {ex.Message}");
        return result;
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of activity ***
        CorrelationContext.Clear();
      }
    }

    [Function(nameof(GetOverwriteConfigs))]
    public async Task<OverriteConfigOutput> GetOverwriteConfigs(
        [ActivityTrigger] OrchestrationParameters parameters,
        FunctionContext context)
    {
      if (parameters == null)
      {
        _logger.LogNullParameters(nameof(GetOverwriteConfigs));
        throw new ArgumentNullException(nameof(parameters));
      }

      var resourceInfo = parameters.InputData;
      var instanceId = parameters.InvocationID;
      var output = new OverriteConfigOutput { OverwriteConfigResult = new ResultObject() };

      // *** CORRELATION FIX: Set correlation context from parent InvocationID ***
      CorrelationContext.SetCorrelationId(instanceId);

      using var loggerScope = _logger.BeginActivityScope(nameof(GetOverwriteConfigs), instanceId, context.InvocationId, resourceInfo.ResourceName);
      using var operation = _telemetry.StartOperation("GetOverwriteConfigs",
          new Dictionary<string, string> { ["InstanceId"] = instanceId, ["ResourceName"] = resourceInfo.ResourceName ?? "Unknown" });

      try
      {
        output.OverwriteConfigResult.FunctionNames.Add(nameof(GetOverwriteConfigs));
        output.OverwriteConfigResult.InvocationIDs.Add(context.InvocationId);

        _logger.LogActivityStarted(nameof(GetOverwriteConfigs), instanceId, context.InvocationId);
        _logger.LogGettingOverwriteConfigs(resourceInfo.ResourceName ?? "Unknown", instanceId);

        var configs = await _dynamicAllowListingHelper.GetOutboundOverwriteConfigs(resourceInfo);

        if (configs.Count > 0)
        {
          output.ResourceDependencyInformation = configs;
          _logger.LogOverwriteConfigsRetrieved(configs.Count, instanceId);
          operation.AddMetric("ConfigCount", configs.Count);
        }
        else
        {
          _logger.LogNoOverwriteConfigsFound(instanceId);
        }

        operation.SetSuccess();
        _logger.LogActivityCompleted(nameof(GetOverwriteConfigs), instanceId, true);
      }
      catch (Exception ex)
      {
        _logger.LogActivityFailed(ex, nameof(GetOverwriteConfigs), instanceId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string> { ["InstanceId"] = instanceId, ["Activity"] = nameof(GetOverwriteConfigs) });
        output.OverwriteConfigResult.Errors.Add($"Exception: {ex.Message}");
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of activity ***
        CorrelationContext.Clear();
      }

      return output;
    }

    public class OrchestrationParameters
    {
      public required string InvocationID { get; set; }
      public required ResourceDependencyInformation InputData { get; set; }
    }

    public class OverriteConfigOutput
    {
      public HashSet<ResourceDependencyInformation> ResourceDependencyInformation { get; set; } = new();
      public required ResultObject OverwriteConfigResult { get; set; }
    }
  }
}