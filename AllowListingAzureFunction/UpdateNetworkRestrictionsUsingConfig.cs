using DynamicAllowListingLib;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using System.Net;
using DynamicAllowListingLib.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace AllowListingAzureFunction
{
  public class UpdateNetworkRestrictionsUsingConfig
  {
    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly ISettingValidator<ResourceDependencyInformation> _validator;
    private readonly TelemetryClient _telemetryClient;

    public UpdateNetworkRestrictionsUsingConfig(IDynamicAllowListingService dynamicAllowListingHelper,
        ISettingValidator<ResourceDependencyInformation> validator,
        TelemetryClient telemetryClient)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper;
      _validator = validator;
      _telemetryClient = telemetryClient;
    }

    [Function("UpdateNetworkRestrictionsUsingConfig")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient starter)
    {
      Guid operationID = Guid.NewGuid();
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("UpdateNetworkRestrictionsUsingConfig", operationID.ToString()))
      {
        try
        {
          _telemetryClient.Context.Operation.Id = operationID.ToString();
          _telemetryClient.TrackTrace($"Starting UpdateNetworkRestrictionsUsingConfig. OperationId: {operationID}", SeverityLevel.Information);

          // Read request body
          string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
          if (string.IsNullOrWhiteSpace(requestBody))
          {
            _telemetryClient.TrackTrace($"Request body is empty. OperationId: {operationID}", SeverityLevel.Warning);
            return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
          }

          // Parse the request body into a model
          var parseJsonResult = requestBody.TryParseJson(out ResourceDependencyInformation model);
          if (!parseJsonResult.Success)
          {
            var response = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            await response.WriteAsJsonAsync(parseJsonResult);
            _telemetryClient.TrackTrace($"Request JSON parsing failed. OperationId: {operationID}", SeverityLevel.Error);
            return response;
          }

          // Validate the model
          var validationResults = _validator.Validate(model);
          if (!validationResults.Success && validationResults.Errors.Count > 0)
          {
            var response = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            await response.WriteAsJsonAsync(validationResults);
            _telemetryClient.TrackTrace($"Validation failed. OperationId: {operationID}, ValidationErrors: {string.Join(", ", validationResults.Errors)}", SeverityLevel.Error);
            return response;
          }

          // Prepare orchestration parameters
          var parameters = new OrchestrationParameters
          {
            InvocationID = operationID.ToString(),
            InputData = model,
          };

          // Start a new orchestration instance
          string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(nameof(UpdateNetworkRestrictionsOrchestrator), parameters);
          _telemetryClient.TrackTrace($"Orchestration instance started. OperationId: {operationID}, InstanceId: {instanceId}", SeverityLevel.Information);

          // Wait for the orchestration instance to complete
          var result = await starter.WaitForInstanceCompletionAsync(instanceId, getInputsAndOutputs: true, CancellationToken.None);
          var resultObject = result.ReadOutputAs<ResultObject>();

          // Return the result to the client
          var resp = req.CreateResponse(HttpStatusCode.OK);
          await resp.WriteAsJsonAsync(resultObject);
          _telemetryClient.TrackTrace($"Orchestration completed successfully. OperationId: {operationID}, Result: {resultObject?.ToString()}", SeverityLevel.Information);
          return resp;
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "OperationId", operationID.ToString() }
                          });

          // Return an internal server error response
          var resp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
          _telemetryClient.TrackTrace($"An error occurred during processing. OperationId: {operationID}, Error: {ex.Message}", SeverityLevel.Error);
          return resp;
        }
      }
    }

    [Function(nameof(UpdateNetworkRestrictionsOrchestrator))]
    public async Task<ResultObject> UpdateNetworkRestrictionsOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
    {
      var OrchParameters = context.GetInput<OrchestrationParameters>();
      if (OrchParameters == null) throw new ArgumentNullException(nameof(OrchParameters));

      var guid = context.InstanceId;
      var resultObject = new ResultObject();
      //operationname,operationid, parentoperationid=invocationID=newguid
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>(nameof(UpdateNetworkRestrictionsOrchestrator), guid, OrchParameters.InvocationID))
      {
        try
        {
          _telemetryClient.TrackTrace($"Started orchestrator. OperationId: {OrchParameters.InvocationID}, InstanceId: {guid}", SeverityLevel.Information);
          var parameters = new OrchestrationParameters
          {
            InvocationID = guid, //context.instanceid
            InputData = OrchParameters.InputData
          };

          // Call the first activity function (unmanaged resources update)
          var unmanagedResUpdateResult = await context.CallActivityAsync<ResultObject>(nameof(UpdateDbAndUnmanagedResources), parameters);
          resultObject.Merge(unmanagedResUpdateResult);
          _telemetryClient.TrackTrace($"Unmanaged resources updated. OperationId: {OrchParameters.InvocationID}", SeverityLevel.Information);

          // Call the second activity function (overwrite network restrictions)
          var mainResUpdateResult = await context.CallActivityAsync<ResultObject>(OverwriteNetworkRestrictionRules.OverwriteNetworkRestrictionsActivityFunction, parameters);
          resultObject.Merge(mainResUpdateResult);
          _telemetryClient.TrackTrace($"Main resources updated. OperationId: {OrchParameters.InvocationID}", SeverityLevel.Information);

          // If there were errors, return early
          if (!resultObject.Success || resultObject.Errors.Count > 0)
          {
            _telemetryClient.TrackTrace($"Error during orchestration. OperationId: {OrchParameters.InvocationID}, Errors: {string.Join(", ", resultObject.Errors)}", SeverityLevel.Error);
            return resultObject;
          }

          // Get overwrite configurations
          var configs = await context.CallActivityAsync<OverriteConfigOutput>(nameof(GetOverwriteConfigs), parameters);
          if (configs.ResourceDependencyInformation.Count <= 0)
          {
            resultObject.Merge(configs.OverwriteConfigResult);
            _telemetryClient.TrackTrace($"No outbound configs overwritten. OperationId: {OrchParameters.InvocationID}", SeverityLevel.Information);
            return resultObject;
          }

          // Process each resource dependency asynchronously
          var tasks = new Task<ResultObject>[configs.ResourceDependencyInformation.Count];
          int i = 0;
          foreach (var resourceDependencyInformation in configs.ResourceDependencyInformation)
          {
            var parameters2 = new OrchestrationParameters
            {
              InvocationID = guid,
              InputData = resourceDependencyInformation
            };
            tasks[i] = context.CallActivityAsync<ResultObject>(OverwriteNetworkRestrictionRules.OverwriteNetworkRestrictionsActivityFunction, parameters2);
            i++;
            _telemetryClient.TrackTrace($"Sending {resourceDependencyInformation.ResourceName} to overwrite network restrictions. OperationId: {OrchParameters.InvocationID}", SeverityLevel.Information);
          }

          // Wait for all resource dependency tasks to complete
          var results = await Task.WhenAll(tasks);
          if (results.Length != configs.ResourceDependencyInformation.Count)
          {
            resultObject.Errors.Add($"Result mismatch found. Expected {configs.ResourceDependencyInformation.Count} results but got {results.Length} results.");
            _telemetryClient.TrackTrace($"Result mismatch. Expected {configs.ResourceDependencyInformation.Count} but got {results.Length}. OperationId: {OrchParameters.InvocationID}", SeverityLevel.Error);
          }
          // Merge results and handle errors or warnings
          foreach (var result in results)
          {
            resultObject.Merge(ConvertErrorToWarning(result));
          }

          // Stop operation telemetry after processing
          _telemetryClient.StopOperation(operation);

          return resultObject;
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "OperationId", OrchParameters.InvocationID },
                              { "InstanceId", guid }
                          });

          // Stop operation telemetry and return result
          _telemetryClient.StopOperation(operation);
          resultObject.Errors.Add($"Exception occurred: {ex.Message}");
          return resultObject;
        }
      }
    }

    [Function(nameof(UpdateDbAndUnmanagedResources))]
    public async Task<ResultObject> UpdateDbAndUnmanagedResources(
    [Microsoft.Azure.Functions.Worker.ActivityTrigger] OrchestrationParameters parameters, FunctionContext context)
    {
      if (parameters == null) throw new ArgumentNullException(nameof(parameters));

      string instanceId = parameters.InvocationID; // context.instanceid
      ResourceDependencyInformation resourceDependencyInformation = parameters.InputData;
      var result = new ResultObject();

      // Start telemetry operation for this function
      using (var depOperation = _telemetryClient.StartOperation<RequestTelemetry>("UpdateDbAndUnManagedResources", instanceId))
      {
        try
        {
          // Adding the current function name and invocation ID for better traceability
          result.FunctionNames.Add(nameof(UpdateDbAndUnmanagedResources));
          result.InvocationIDs.Add(context.InvocationId);

          _telemetryClient.TrackTrace($"Starting DB and unmanaged resource updates. OperationId: {instanceId}", SeverityLevel.Information);

          // Update the database and merge the results
          var updateDbResult = await _dynamicAllowListingHelper.UpdateDb(resourceDependencyInformation);
          result.Merge(updateDbResult);
          _telemetryClient.TrackTrace("Database updated successfully.", SeverityLevel.Information);

          // Update unmanaged resources and merge the results
          var updateUnManagedResourceResult = await _dynamicAllowListingHelper.UpdateUnmanagedResources(resourceDependencyInformation);
          result.Merge(updateUnManagedResourceResult);
          _telemetryClient.TrackTrace("Unmanaged resources updated successfully.", SeverityLevel.Information);

          // Check if there are errors and handle accordingly
          if (result.Errors.Count > 0)
          {
            // Log errors before marking the operation as unsuccessful
            _telemetryClient.TrackTrace($"Errors encountered while updating. OperationId: {instanceId}, Errors: {string.Join(", ", result.Errors)}", SeverityLevel.Error);
          }
          // Return the result with all updates and any errors
          return result;
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "InstanceID", instanceId  }
                          });
          depOperation.Telemetry.Success = false;

          // Add the exception to the result and return it
          result.Errors.Add($"Exception: {ex.Message}");
          return result;
        }
      }
    }

    [Function(nameof(GetOverwriteConfigs))]
    public async Task<OverriteConfigOutput> GetOverwriteConfigs([Microsoft.Azure.Functions.Worker.ActivityTrigger] OrchestrationParameters parameters,
    FunctionContext context)
    {
      if (parameters == null) throw new ArgumentNullException(nameof(parameters));

      ResourceDependencyInformation resourceDependencyInformation = parameters.InputData;
      string instanceId = parameters.InvocationID; // context.instanceid

      var output = new OverriteConfigOutput
      {
        OverwriteConfigResult = new ResultObject()
      };

      HashSet<ResourceDependencyInformation> configs = new HashSet<ResourceDependencyInformation>();
      using (var depOperation = _telemetryClient.StartOperation<RequestTelemetry>("GetOverwriteConfigs", instanceId))
      {
        try
        {
          // Log function details into telemetry
          output.OverwriteConfigResult.FunctionNames.Add(nameof(GetOverwriteConfigs));
          output.OverwriteConfigResult.InvocationIDs.Add(context.InvocationId);
          _telemetryClient.TrackTrace($"Started GetOverwriteConfigs activity for Resource: {resourceDependencyInformation.ResourceName}, InstanceId: {instanceId}", SeverityLevel.Information);

          // Track the activity for GetOutboundOverwriteConfigs
          configs = await _dynamicAllowListingHelper.GetOutboundOverwriteConfigs(resourceDependencyInformation);

          if (configs.Count > 0)
          {
            output.ResourceDependencyInformation = configs;
            _telemetryClient.TrackTrace($"Successfully retrieved {configs.Count} outbound overwrite configurations.", SeverityLevel.Information);
          }
          else
          {
            _telemetryClient.TrackTrace("No outbound overwrite configurations found.", SeverityLevel.Information);
          }
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "InstanceId", instanceId }
                          });
          output.OverwriteConfigResult.Errors.Add($"Exception occurred: {ex.Message}");
          _telemetryClient.TrackTrace($"Exception in GetOverwriteConfigs: {ex.Message}", SeverityLevel.Error);
        }
        finally
        {
          // Mark the operation success status
          depOperation.Telemetry.Success = output.OverwriteConfigResult.Errors.Count == 0;
        }
      }

      return output; // Return the retrieved configs or errors
    }

    private static ResultObject ConvertErrorToWarning(ResultObject resultObject)
    {
      if (resultObject.Errors.Count <= 0)
      {
        return resultObject;
      }
      var updatedResultObject = new ResultObject();
      updatedResultObject.Errors = new List<string>();
      updatedResultObject.Warnings.AddRange(resultObject.Errors);
      updatedResultObject.Warnings.AddRange(resultObject.Warnings);
      updatedResultObject.Information.AddRange(resultObject.Information);
      return updatedResultObject;
    }
    public class OrchestrationParameters
    {
      public required string InvocationID { get; set; }
      public required ResourceDependencyInformation InputData { get; set; }
    }

    public class OverriteConfigOutput
    {
      public HashSet<ResourceDependencyInformation> ResourceDependencyInformation { get; set; } = new HashSet<ResourceDependencyInformation>();
      public required ResultObject OverwriteConfigResult { get; set; }
    }
  }
}