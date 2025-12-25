using DynamicAllowListingLib;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;

namespace AllowListingAzureFunction
{
  public class CheckProvisioningSucceeded
  {
    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly ISettingValidator<ResourceDependencyInformation> _validator;
    private readonly TelemetryClient _telemetryClient;
    public CheckProvisioningSucceeded(IDynamicAllowListingService dynamicAllowListingHelper,
      ISettingValidator<ResourceDependencyInformation> validator,
      TelemetryClient telemetryClient)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper;
      _validator = validator;
      _telemetryClient = telemetryClient;
    }

    [Function("CheckProvisioningSucceeded")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {

      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("CheckProvisioningSucceeded", operationId))
      {
        try
        {
          // Get request string
          string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

          // Log incoming request body (be cautious with sensitive data)
          _telemetryClient.TrackTrace($"Received request body: {requestBody.Substring(0, Math.Min(requestBody.Length, 100))}...", SeverityLevel.Information);

          if (string.IsNullOrWhiteSpace(requestBody))
          {
            _telemetryClient.TrackTrace("Request body is empty or whitespace.", SeverityLevel.Warning);
            return new BadRequestObjectResult("Request body is empty.");
          }

          // Parse JSON to object
          var parseJsonResult = requestBody.TryParseJson(out ResourceDependencyInformation model);
          if (!parseJsonResult.Success)
          {
            _telemetryClient.TrackTrace($"Failed to parse JSON", SeverityLevel.Error);
            return new UnprocessableEntityObjectResult(parseJsonResult);
          }

          // Run validation rules
          var validationResults = _validator.Validate(model);
          if (!validationResults.Success)
          {
            _telemetryClient.TrackTrace($"Validation failed. Errors: {string.Join(", ", validationResults.Errors)}", SeverityLevel.Warning);
            return new UnprocessableEntityObjectResult(new { Errors = validationResults.Errors });
          }

          // Apply rules based on the JSON input
          var dalResult = await _dynamicAllowListingHelper.CheckProvisioningSucceeded(model);
          _telemetryClient.TrackTrace("Provisioning check completed successfully.", SeverityLevel.Information);

          return new OkObjectResult(dalResult);


        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });
          return new StatusCodeResult(StatusCodes.Status500InternalServerError); // Internal server error
        }
        finally
        {
          // Stop the operation to finalize telemetry
          _telemetryClient.StopOperation(operation);
        }
      }
    }
  }
}