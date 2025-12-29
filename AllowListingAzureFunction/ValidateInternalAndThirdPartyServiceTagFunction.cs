using AllowListingAzureFunction.Logging;
using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.ServiceTagManagers;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AllowListingAzureFunction
{
  public class ValidateInternalAndThirdPartyServiceTagFunction
  {
    private const string FunctionName = "ValidateInternalAndThirdPartyServiceTags";

    private readonly ISettingValidator<InternalAndThirdPartyServiceTagSetting> _internalAndThirdPartyServiceTagValidator;
    private readonly ISettingValidator<ResourceDependencyInformation> _resourceDependencyInformationValidator;
    private readonly ISettingLoader _settingLoader;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<ValidateInternalAndThirdPartyServiceTagFunction> _logger;

    public ValidateInternalAndThirdPartyServiceTagFunction(
        ISettingValidator<InternalAndThirdPartyServiceTagSetting> internalAndThirdPartyServiceTagValidator,
        ISettingValidator<ResourceDependencyInformation> resourceDependencyInformationValidator,
        ISettingLoader settingLoader,
        ICustomTelemetryService telemetry,
        ILogger<ValidateInternalAndThirdPartyServiceTagFunction> logger)
    {
      _internalAndThirdPartyServiceTagValidator = internalAndThirdPartyServiceTagValidator;
      _resourceDependencyInformationValidator = resourceDependencyInformationValidator;
      _settingLoader = settingLoader;
      _telemetry = telemetry;
      _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
      var operationId = Guid.NewGuid().ToString();

      // Set correlation context for distributed tracing
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

        // Parse JSON
        _logger.LogParsingRequestJson(operationId);
        var parseJsonResult = requestBody.TryParseJson(out InternalAndThirdPartyServiceTagSetting model, _logger);

        if (!parseJsonResult.Success)
        {
          _logger.LogJsonParsingFailed(operationId, string.Join(", ", parseJsonResult.Errors));
          operation.SetFailed("JSON parsing failed");
          return new UnprocessableEntityObjectResult(parseJsonResult);
        }

        if (model == null)
        {
          _logger.LogJsonParsingFailed(operationId, "Parsed model is null");
          operation.SetFailed("Parsed model is null");
          return new BadRequestObjectResult("Parsed model is null.");
        }

        _logger.LogJsonParsedSuccessfully(operationId, nameof(InternalAndThirdPartyServiceTagSetting));

        // Log input summary after successful parsing
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

          // Log validation errors detail
          if (validationResult.Errors.Count > 0)
          {
            _logger.LogOperationErrors(operationId, string.Join("; ", validationResult.Errors));
          }

          operation.SetFailed("Validation failed");
          return new BadRequestObjectResult(validationResult);
        }

        _logger.LogModelValidationSucceeded(operationId, nameof(InternalAndThirdPartyServiceTagSetting));

        // Log if there are warnings even on success
        if (validationResult.Warnings.Count > 0)
        {
          _logger.LogValidationCompletedWithWarnings(operationId, validationResult.Warnings.Count);
        }

        _logger.LogHttpFunctionCompleted(FunctionName, operationId, 200, 0);
        operation.SetSuccess();

        return new OkObjectResult(validationResult);
      }
      catch (Exception ex)
      {
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
        // Clear correlation context at end of function
        CorrelationContext.Clear();
      }
    }
  }
}