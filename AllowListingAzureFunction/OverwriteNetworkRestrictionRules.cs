using DynamicAllowListingLib;
using DynamicAllowListingLib.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using static AllowListingAzureFunction.UpdateNetworkRestrictionsUsingConfig;
using System.Collections.Generic;

namespace AllowListingAzureFunction
{
  public class OverwriteNetworkRestrictionRules
  {
    public const string OverwriteNetworkRestrictionsActivityFunction = nameof(OverwriteNetworkRestrictionsActivityTrigger);

    private readonly IDynamicAllowListingService _dalService;
    private readonly TelemetryClient _telemetryClient;

    public OverwriteNetworkRestrictionRules(IDynamicAllowListingService dalService, TelemetryClient telemetryClient)
    {
      _dalService = dalService;
      _telemetryClient = telemetryClient;
    }

    [Function(nameof(OverwriteNetworkRestrictionsActivityTrigger))]
    public async Task<ResultObject> OverwriteNetworkRestrictionsActivityTrigger(
      [ActivityTrigger] OrchestrationParameters parameters, FunctionContext context)
    {
      if (parameters == null) throw new ArgumentNullException(nameof(parameters));

      string instanceId = parameters.InvocationID;
      ResourceDependencyInformation resourceDependencyInformation = parameters.InputData;

      ResultObject result = new ResultObject();
      using (var depOperation = _telemetryClient.StartOperation<RequestTelemetry>("OverwriteNetworkRestrictionsActivityTrigger", instanceId))
      {
        try
        {
          result.FunctionNames.Add(nameof(OverwriteNetworkRestrictionsActivityTrigger));
          result.InvocationIDs.Add(context.InvocationId);

          // Log function start in telemetry
          _telemetryClient.TrackTrace($"Started overwriting network restrictions for resource: {resourceDependencyInformation.ResourceName}, Instance ID: {instanceId}", SeverityLevel.Information);

          // Perform the network restrictions overwrite operation
          result = await OverwriteNetworkRestrictions(resourceDependencyInformation);

          // Log the success of the operation
          _telemetryClient.TrackTrace($"Successfully overwrote network restrictions for resource: {resourceDependencyInformation.ResourceName}.", SeverityLevel.Information);

          depOperation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "InstanceId", instanceId }
                          });
          result.Errors.Add($"Error occurred while overwriting network restrictions: {ex.Message}");

          // Log failure in telemetry
          _telemetryClient.TrackTrace($"Error overwriting network restrictions for resource: {resourceDependencyInformation.ResourceName}. Error: {ex.Message}", SeverityLevel.Error);

          depOperation.Telemetry.Success = false;
        }
      }

      // Return the result, containing any success or error messages
      return result;
    }


    [Function(nameof(OverwriteNetworkRestrictionsQueueTrigger))]
    public async Task OverwriteNetworkRestrictionsQueueTrigger(
      [QueueTrigger("network-restriction-configs", Connection = "StorageQueueStorageAccountConnectionString")] ResourceDependencyInformation config,
      FunctionContext context)
    {
      // Retrieve the invocation ID for telemetry tracking
      string instanceId = context.InvocationId.ToString();

      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("OverwriteNetworkRestrictionsQueueTrigger", instanceId))
      {
        try
        {
          // Log the start of the queue item processing
          _telemetryClient.TrackTrace($"Started processing network restriction config for resource: {config.ResourceName}, Instance ID: {instanceId}", SeverityLevel.Information);

          // Call the OverwriteNetworkRestrictions method to perform the overwrite action
          await OverwriteNetworkRestrictions(config);

          // Log success
          _telemetryClient.TrackTrace($"Successfully processed network restriction config for resource: {config.ResourceName}.", SeverityLevel.Information);

          // Set the operation status to success
          operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "InstanceId", instanceId }
                          });

          // Log failure message
          _telemetryClient.TrackTrace($"Error processing network restriction config for resource: {config.ResourceName}. Error: {ex.Message}", SeverityLevel.Error);

          // Set the operation status to failure
          operation.Telemetry.Success = false;
        }
      }
    }


    private async Task<ResultObject> OverwriteNetworkRestrictions(ResourceDependencyInformation resourceDependencyInformation)
    {
      ResultObject result;
      try
      {
        _telemetryClient.TrackTrace($"Overwrite operation started for {resourceDependencyInformation.ResourceName}", SeverityLevel.Information);
        result = await _dalService.OverwriteNetworkRestrictionRulesForMainResource(resourceDependencyInformation); 
        _telemetryClient.TrackTrace($"Overwrite operation completed for {resourceDependencyInformation.ResourceName}", SeverityLevel.Information);

      }
      catch (Exception ex)
      {
        _telemetryClient.TrackException(ex);
        throw;
      }
      return result;
    }
  }
}