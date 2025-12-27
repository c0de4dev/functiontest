using AllowListingAzureFunction.Logging;
using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Services;
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
  public class CheckProvisioningSucceeded
  {
    private const string FunctionName = nameof(CheckProvisioningSucceeded);

    private readonly IDynamicAllowListingService _dynamicAllowListingHelper;
    private readonly ISettingValidator<ResourceDependencyInformation> _validator;
    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<CheckProvisioningSucceeded> _logger;

    public CheckProvisioningSucceeded(
        IDynamicAllowListingService dynamicAllowListingHelper,
        ISettingValidator<ResourceDependencyInformation> validator,
        ICustomTelemetryService telemetry,
        ILogger<CheckProvisioningSucceeded> logger)
    {
      _dynamicAllowListingHelper = dynamicAllowListingHelper ?? throw new ArgumentNullException(nameof(dynamicAllowListingHelper));
      _validator = validator ?? throw new ArgumentNullException(nameof(validator));
      _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(FunctionName)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
      var operationId = Guid.NewGuid().ToString();

      // Set correlation context for library layer
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
          return new BadRequestResult();
        }

        _logger.LogRequestBodyReceived(operationId, requestBody.Length);

        // Parse JSON to object
        _logger.LogParsingRequestJson(operationId);
        var parseJsonResult = requestBody.TryParseJson(out ResourceDependencyInformation model);

        if (!parseJsonResult.Success)
        {
          _logger.LogJsonParsingFailed(operationId, string.Join(", ", parseJsonResult.Errors));
          operation.SetFailed("JSON parsing failed");
          return new UnprocessableEntityObjectResult(parseJsonResult);
        }

        _logger.LogJsonParsedSuccessfully(operationId, model?.ResourceName ?? "Unknown");
        operation.AddProperty("ResourceName", model?.ResourceName ?? "Unknown");
        operation.AddProperty("ResourceId", model?.ResourceId ?? "Unknown");

        // Run validation rules
        _logger.LogValidatingModel(operationId);
        var validationResults = _validator.Validate(model!);

        if (!validationResults.Success)
        {
          _logger.LogModelValidationFailed(operationId, validationResults.Errors.Count);
          operation.SetFailed("Validation failed");
          return new UnprocessableEntityObjectResult(validationResults);
        }

        _logger.LogModelValidationSucceeded(operationId, model!.ResourceName ?? "Unknown");

        // Check provisioning status
        _logger.LogInformation(
            "Checking provisioning status | ResourceName: {ResourceName} | ResourceId: {ResourceId} | OperationId: {OperationId}",
            model.ResourceName ?? "Unknown",
            model.ResourceId ?? "Unknown",
            operationId);

        var dalResult = await _dynamicAllowListingHelper.CheckProvisioningSucceeded(model);

        // Log result
        if (dalResult.Success)
        {
          _logger.LogHttpFunctionCompleted(FunctionName, operationId, 200, (long)operation.Elapsed.TotalMilliseconds);
          operation.SetSuccess();
        }
        else
        {
          _logger.LogOperationCompletedWithErrors(operationId, dalResult.Errors.Count);
          _logger.LogOperationErrors(operationId, string.Join("; ", dalResult.Errors));
          operation.AddMetric("ErrorCount", dalResult.Errors.Count);
          operation.SetFailed($"{dalResult.Errors.Count} provisioning check errors");
        }

        return new OkObjectResult(dalResult);
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
        // Clear correlation context at end of request
        CorrelationContext.Clear();
      }
    }
  }
}