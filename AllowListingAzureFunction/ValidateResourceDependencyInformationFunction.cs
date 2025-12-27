using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.SettingsValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using AllowListingAzureFunction.Logging;

namespace AllowListingAzureFunction
{
  public class ValidateResourceDependencyInformationFunction
  {
    private const string FunctionName = "ValidateResourceDependencyInformation";

    private readonly ISettingValidator<ResourceDependencyInformation> _resourceDependencyInformationValidator;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<ValidateResourceDependencyInformationFunction> _logger;

    public ValidateResourceDependencyInformationFunction(
        ISettingValidator<ResourceDependencyInformation> resourceDependencyInformationValidator,
        ICustomTelemetryService telemetry,
        ILogger<ValidateResourceDependencyInformationFunction> logger)
    {
      _resourceDependencyInformationValidator = resourceDependencyInformationValidator;
      _telemetry = telemetry;
      _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
      var operationId = Guid.NewGuid().ToString();

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

        // Parse JSON
        _logger.LogParsingRequestJson(operationId);
        var parseJsonResult = requestBody.TryParseJson(out ResourceDependencyInformation model);

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

        _logger.LogJsonParsedSuccessfully(operationId, model.ResourceName ?? "Unknown");
        operation.AddProperty("ResourceName", model.ResourceName ?? "Unknown");

        // Validate model
        _logger.LogValidatingModel(operationId);
        var validationResult = _resourceDependencyInformationValidator.Validate(model);

        if (!validationResult.Success)
        {
          _logger.LogModelValidationFailed(operationId, validationResult.Errors.Count);
          operation.SetFailed("Validation failed");
          return new BadRequestObjectResult(validationResult);
        }

        _logger.LogModelValidationSucceeded(operationId, model.ResourceName ?? "Unknown");
        _logger.LogHttpFunctionCompleted(FunctionName, operationId, 200, (long)operation.Elapsed.TotalMilliseconds);
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
        // *** CORRELATION FIX: Clear at end of function ***
        CorrelationContext.Clear();
      }
    }
  }
}