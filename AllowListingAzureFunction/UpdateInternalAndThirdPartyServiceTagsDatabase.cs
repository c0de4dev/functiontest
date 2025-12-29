using AllowListingAzureFunction.Logging;
using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.ServiceTagManagers;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AllowListingAzureFunction
{
  public class UpdateInternalAndThirdPartyServiceTagsDatabase
  {
    private const string FunctionName = "UpdateInternalAndThirdPartyServiceTagsDatabase";

    private readonly IInternalAndThirdPartyServiceTagPersistenceManager _internalAndThirdPartyServiceTagPersistenceManager;
    private readonly ISettingValidator<InternalAndThirdPartyServiceTagSetting> _internalAndThirdPartyServiceTagValidator;
    private readonly ISettingLoader _settingLoader;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<UpdateInternalAndThirdPartyServiceTagsDatabase> _logger;

    public UpdateInternalAndThirdPartyServiceTagsDatabase(
        IInternalAndThirdPartyServiceTagPersistenceManager internalAndThirdPartyServiceTagPersistenceManager,
        ISettingValidator<InternalAndThirdPartyServiceTagSetting> internalAndThirdPartyServiceTagValidator,
        ISettingLoader settingLoader,
        ICustomTelemetryService telemetry,
        ILogger<UpdateInternalAndThirdPartyServiceTagsDatabase> logger)
    {
      _internalAndThirdPartyServiceTagPersistenceManager = internalAndThirdPartyServiceTagPersistenceManager;
      _internalAndThirdPartyServiceTagValidator = internalAndThirdPartyServiceTagValidator;
      _settingLoader = settingLoader;
      _telemetry = telemetry;
      _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
      var operationId = Guid.NewGuid().ToString();
      var stopwatch = Stopwatch.StartNew();

      // *** CORRELATION FIX: Set correlation context for HTTP trigger ***
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginFunctionScope(FunctionName, operationId);
      using var operation = _telemetry.StartOperation(FunctionName,
          new Dictionary<string, string> { ["OperationId"] = operationId });

      try
      {
        _logger.LogHttpFunctionStarted(FunctionName, operationId, req.Method);
        _logger.LogHttpRequestReceived(FunctionName, req.Method, req.Path);

        // Read request body
        _logger.LogReadingRequestBody(operationId);
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
          _logger.LogEmptyRequestBody(operationId);
          _logger.LogHttpFunctionErrorResponse(FunctionName, operationId, 400, "Empty request body");
          operation.SetFailed("Empty request body");
          return new BadRequestObjectResult("Request body is empty.");
        }

        _logger.LogRequestBodyReceived(operationId, requestBody.Length);

        // Parse JSON - Gap 2.x fix: Pass logger to enable JSON parsing logging
        _logger.LogParsingRequestJson(operationId);
        var parseResult = requestBody.TryParseJson(out InternalAndThirdPartyServiceTagSetting model, _logger);

        if (!parseResult.Success)
        {
          _logger.LogJsonParsingFailed(operationId, string.Join(", ", parseResult.Errors));
          operation.SetFailed("JSON parsing failed");
          return new UnprocessableEntityObjectResult(parseResult);
        }

        if (model == null)
        {
          _logger.LogJsonParsingFailed(operationId, "Parsed model is null");
          operation.SetFailed("Parsed model is null");
          return new BadRequestObjectResult("Parsed model is null.");
        }

        _logger.LogJsonParsedSuccessfully(operationId, "InternalAndThirdPartyServiceTagSetting");

        // Gap 1.2 fix: Log input summary after successful parsing
        _logger.LogInputSummary(
            operationId,
            model.AzureSubscriptions?.Count ?? 0,
            model.ServiceTags?.Count ?? 0);

        // Validate model
        _logger.LogValidatingModel(operationId);
        var validationResult = _internalAndThirdPartyServiceTagValidator.Validate(model);

        if (!validationResult.Success)
        {
          _logger.LogModelValidationFailed(operationId, validationResult.Errors.Count);
          operation.SetFailed("Validation failed");
          return new BadRequestObjectResult(validationResult);
        }

        _logger.LogModelValidationSucceeded(operationId, "InternalAndThirdPartyServiceTagSetting");

        // Update database
        var dbUpdateStopwatch = Stopwatch.StartNew();
        _logger.LogDatabaseUpdateStarted("ServiceTags", operationId);
        await _internalAndThirdPartyServiceTagPersistenceManager.UpdateDatabaseStateTo(model);
        dbUpdateStopwatch.Stop();
        _logger.LogDatabaseUpdateCompleted("ServiceTags", operationId);

        // Log database update summary
        _logger.LogDatabaseUpdateSummary(
            operationId,
            model.AzureSubscriptions?.Count ?? 0,
            model.ServiceTags?.Count ?? 0,
            dbUpdateStopwatch.ElapsedMilliseconds);

        stopwatch.Stop();

        // Gap 1.1 fix: Log function completed successfully
        _logger.LogHttpFunctionCompletedSuccessfully(FunctionName, operationId, 200, stopwatch.ElapsedMilliseconds);

        operation.SetSuccess();
        return new OkObjectResult(new ResultObject());
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        _logger.LogHttpFunctionFailed(ex, FunctionName, operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = FunctionName
        });

        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
      }
      finally
      {
        // *** CORRELATION FIX: Clear at end of function ***
        CorrelationContext.Clear();
      }
    }
  }
}