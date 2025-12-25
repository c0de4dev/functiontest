using DynamicAllowListingLib;
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
  public class ValidateResourceDependencyInformationFunction
  {
    private readonly ISettingValidator<ResourceDependencyInformation> _resourceDependencyInformationValidator;
    private readonly TelemetryClient _telemetryClient;

    public ValidateResourceDependencyInformationFunction(ISettingValidator<ResourceDependencyInformation> resourceDependencyInformationValidator,
    TelemetryClient telemetryClient)
    {
      _resourceDependencyInformationValidator = resourceDependencyInformationValidator;
      _telemetryClient = telemetryClient;
    }

    [Function("ValidateResourceDependencyInformation")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing

      // Start logging and tracking the operation with telemetry
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("ValidateResourceDependencyInformation", operationId))
      {
        _telemetryClient.Context.Operation.Id = operationId;
        _telemetryClient.TrackTrace($"Starting resource dependency validation. OperationId: {operationId}", SeverityLevel.Information);

        try
        {
          // Get request string
          string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

          if (string.IsNullOrWhiteSpace(requestBody))
          {
            // Track the empty request scenario
            _telemetryClient.TrackTrace($"Empty request body received. OperationId: {operationId}", SeverityLevel.Warning);
            return new BadRequestObjectResult("Request body is empty.");
          }

          // Parse JSON to object
          var parseJsonResult = requestBody.TryParseJson(out ResourceDependencyInformation model);
          if (!parseJsonResult.Success)
          {
            // Track parsing failure with telemetry
            _telemetryClient.TrackTrace($"JSON parsing failed. OperationId: {operationId}.", SeverityLevel.Warning);
            return new UnprocessableEntityObjectResult(parseJsonResult);
          }

          // Log successful parsing
          _telemetryClient.TrackTrace($"Successfully parsed JSON. Model: {model?.ToString()}", SeverityLevel.Information);

          // Check if the parsed model is null before validating
          if (model == null)
          {
            // Track the null model scenario
            _telemetryClient.TrackTrace($"Parsed model is null. OperationId: {operationId}", SeverityLevel.Warning);
            return new UnprocessableEntityObjectResult("Parsed model is null.");
          }

          // Run validation rules
          var validationResults = _resourceDependencyInformationValidator.Validate(model);
          if (!validationResults.Success)
          {
            // Track validation errors with details
            _telemetryClient.TrackTrace($"Validation failed. OperationId: {operationId}. Errors: {string.Join(", ", validationResults.Errors)}", SeverityLevel.Warning);
            return new UnprocessableEntityObjectResult(validationResults);
          }

          // Successful validation
          _telemetryClient.TrackTrace($"Validation succeeded. OperationId: {operationId}", SeverityLevel.Information);
          return new OkObjectResult(validationResults);
        }
        catch (Exception exception)
        {
          _telemetryClient.TrackException(exception, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });

          // Track the error in telemetry
          _telemetryClient.TrackTrace($"Error occurred during resource dependency validation. OperationId: {operationId}. Exception: {exception.Message}", SeverityLevel.Error);

          // Return a generic error response
          return new ObjectResult(new { message = "An error occurred while validating the resource dependency information." })
          {
            StatusCode = 500
          };
        }
        finally
        {
          // Final telemetry operation stop
          _telemetryClient.TrackTrace($"Finished resource dependency validation. OperationId: {operationId}", SeverityLevel.Information);
        }
      }
    }

  }
}