using DynamicAllowListingLib;
using DynamicAllowListingLib.ServiceTagManagers;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Azure.Storage.Queues.Models;
using System.Collections.Generic;

namespace AllowListingAzureFunction
{
  public class ValidateInternalAndThirdPartyServiceTagFunction
  {
    private readonly ISettingValidator<InternalAndThirdPartyServiceTagSetting> _internalAndThirdPartyServiceTagValidator;
    private readonly ISettingValidator<ResourceDependencyInformation> _resourceDependencyInformationValidator;
    private readonly ISettingLoader _settingLoader;
    private readonly TelemetryClient _telemetryClient;

    public ValidateInternalAndThirdPartyServiceTagFunction(ISettingValidator<InternalAndThirdPartyServiceTagSetting> internalAndThirdPartyServiceTagValidator,
    ISettingValidator<ResourceDependencyInformation> resourceDependencyInformationValidator,
    ISettingLoader settingLoader,
    TelemetryClient telemetryClient)
    {
      _internalAndThirdPartyServiceTagValidator = internalAndThirdPartyServiceTagValidator;
      _resourceDependencyInformationValidator = resourceDependencyInformationValidator;
      _settingLoader = settingLoader;
      _telemetryClient = telemetryClient;
    }

    [Function("ValidateInternalAndThirdPartyServiceTags")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing

      // Start logging and tracking the operation with telemetry
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("ValidateInternalAndThirdPartyServiceTags", operationId))
      {
        _telemetryClient.Context.Operation.Id = operationId;
        _telemetryClient.TrackTrace($"Starting internal and third-party service tags validation. OperationId: {operationId}", SeverityLevel.Information);

        try
        {
          string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
          if (string.IsNullOrWhiteSpace(requestBody))
          {
            // Track the validation failure due to empty request body
            _telemetryClient.TrackTrace($"Empty request body received. OperationId: {operationId}", SeverityLevel.Warning);
            return new BadRequestObjectResult("Request body is empty.");
          }

          // Load and validate the settings from the request body
          var settingModel = await _settingLoader.LoadSettingsFromString<InternalAndThirdPartyServiceTagSetting>(requestBody);
          var result = _internalAndThirdPartyServiceTagValidator.Validate(settingModel);

          // If validation fails, return UnprocessableEntity with telemetry logging
          if (!result.Success)
          {
            _telemetryClient.TrackTrace($"Validation failed. OperationId: {operationId}. Errors: {string.Join(", ", result.Errors)}", SeverityLevel.Warning);
            return new UnprocessableEntityObjectResult(result);
          }

          // Successful validation
          _telemetryClient.TrackTrace($"Validation succeeded. OperationId: {operationId}", SeverityLevel.Information);
          return new OkObjectResult(result);
        }
        catch (Exception exception)
        {
          _telemetryClient.TrackException(exception, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });

          // Track detailed error context
          _telemetryClient.TrackTrace($"Error occurred during validation. OperationId: {operationId}. Exception: {exception.Message}", SeverityLevel.Error);

          // Return a generic error response with a user-friendly message
          return new ObjectResult(new { message = "An error occurred while validating the service tags." })
          {
            StatusCode = 500
          };
        }
        finally
        {
          // Final telemetry operation stop, regardless of success or failure
          _telemetryClient.TrackTrace($"Finished validating internal and third-party service tags. OperationId: {operationId}", SeverityLevel.Information);
        }
      }
    }

  }
}