using DynamicAllowListingLib;
using DynamicAllowListingLib.ServiceTagManagers;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using Azure.Storage.Queues.Models;
using System.Collections.Generic;
namespace AllowListingAzureFunction
{
  public class UpdateInternalAndThirdPartyServiceTagsDatabase
  {
    private readonly IInternalAndThirdPartyServiceTagPersistenceManager _internalAndThirdPartyServiceTagPersistenceManager;
    private readonly ISettingValidator<InternalAndThirdPartyServiceTagSetting> _internalAndThirdPartyServiceTagValidator;
    private readonly ISettingLoader _settingLoader;
    private readonly TelemetryClient _telemetryClient;

    public UpdateInternalAndThirdPartyServiceTagsDatabase(IInternalAndThirdPartyServiceTagPersistenceManager internalAndThirdPartyServiceTagPersistenceManager,
      ISettingValidator<InternalAndThirdPartyServiceTagSetting> internalAndThirdPartyServiceTagValidator,
      ISettingLoader settingLoader,
      TelemetryClient telemetryClient)
    {
      _internalAndThirdPartyServiceTagPersistenceManager = internalAndThirdPartyServiceTagPersistenceManager;
      _internalAndThirdPartyServiceTagValidator = internalAndThirdPartyServiceTagValidator;
      _settingLoader = settingLoader;
      _telemetryClient = telemetryClient;
    }

    [Function("UpdateInternalAndThirdPartyServiceTagsDatabase")]
    public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("UpdateInternalAndThirdPartyServiceTagsDatabase", operationId))
      {
        _telemetryClient.Context.Operation.Id = operationId;

        try
        {
          // Log request start with operationId for traceability
          _telemetryClient.TrackTrace($"Received request to update service tags database. OperationId: {operationId}", SeverityLevel.Information);

          string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
          InternalAndThirdPartyServiceTagSetting settingModel;

          if (!string.IsNullOrEmpty(requestBody))
          {
            // Log the size of the request body
            _telemetryClient.TrackTrace($"Request body size: {requestBody.Length} bytes", SeverityLevel.Information);

            // Load settings from the provided request body
            settingModel = await _settingLoader.LoadSettingsFromString<InternalAndThirdPartyServiceTagSetting>(requestBody);
            var validationResult = _internalAndThirdPartyServiceTagValidator.Validate(settingModel);

            if (!validationResult.Success)
            {
              _telemetryClient.TrackTrace($"Validation failed for settings: {string.Join(", ", validationResult.Errors)}", SeverityLevel.Warning);
              return new UnprocessableEntityObjectResult(validationResult);
            }

            _telemetryClient.TrackTrace("Settings successfully validated.", SeverityLevel.Information);
          }
          else
          {
            // Log fallback when no request body is provided
            _telemetryClient.TrackTrace("No request body found, loading settings from file.", SeverityLevel.Information);

            // Load settings from file
            settingModel = await _settingLoader.LoadSettingsFromFile<InternalAndThirdPartyServiceTagSetting>(
                InternalAndThirdPartyServiceTagSettingFileHelper.GetFilePath());
          }

          // Update the database state with the settings
          await _internalAndThirdPartyServiceTagPersistenceManager.UpdateDatabaseStateTo(settingModel);

          // Return success response
          _telemetryClient.TrackTrace("Service tags database updated successfully.", SeverityLevel.Information);
          return new OkObjectResult(new ResultObject());
        }
        catch (InvalidDataException invalidEx)
        {
          _telemetryClient.TrackException(invalidEx, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });
          return new BadRequestObjectResult($"Invalid data: {invalidEx.Message}");
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
          _telemetryClient.TrackException(fileNotFoundEx, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });
          return new StatusCodeResult(StatusCodes.Status404NotFound); // Not found response
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "OperationId", operationId }
                          });
          return new StatusCodeResult(StatusCodes.Status500InternalServerError); // Internal server error response
        }
        finally
        {
          // Final telemetry operation stop
          _telemetryClient.StopOperation(operation);
        }
      }
    }


  }
}