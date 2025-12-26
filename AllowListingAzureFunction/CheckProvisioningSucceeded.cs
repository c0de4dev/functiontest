using AllowListingAzureFunction.Logging;
using DynamicAllowListingLib;
using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AllowListingAzureFunction.Functions
{
  /// <summary>
  /// HTTP-triggered function to check if resource provisioning has succeeded.
  /// </summary>
  public class CheckProvisioningSucceeded
  {
    private const string FunctionName = nameof(CheckProvisioningSucceeded);

    private readonly IDynamicAllowListingService _dynamicAllowListingService;
    private readonly EnhancedTelemetryService _telemetryService;
    private readonly ILogger<CheckProvisioningSucceeded> _logger;

    public CheckProvisioningSucceeded(
        IDynamicAllowListingService dynamicAllowListingService,
        EnhancedTelemetryService telemetryService,
        ILogger<CheckProvisioningSucceeded> logger)
    {
      _dynamicAllowListingService = dynamicAllowListingService ?? throw new ArgumentNullException(nameof(dynamicAllowListingService));
      _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(FunctionName)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        FunctionContext executionContext)
    {
      var invocationId = executionContext.InvocationId;
      var correlationId = GetCorrelationId(req, invocationId);

      // Initialize correlation context
      CorrelationContext.SetCorrelationId(correlationId);

      using (_logger.BeginFunctionScope(FunctionName, invocationId, correlationId))
      {
        _logger.LogFunctionStarted(FunctionName, invocationId, correlationId);
        _logger.LogHttpRequestReceived(FunctionName, req.Method, req.Path);

        return await _telemetryService.TrackOperationAsync(
            FunctionName,
            async () =>
            {
              // Read and validate request body
              string requestBody;
              using (var reader = new StreamReader(req.Body))
              {
                requestBody = await reader.ReadToEndAsync();
              }

              if (string.IsNullOrWhiteSpace(requestBody))
              {
                _logger.LogHttpValidationFailed(FunctionName, "Request body is empty");
                return CreateErrorResponse("Request body cannot be empty.", 400);
              }

              _logger.LogHttpRequestBody(FunctionName, requestBody.Length);

              // Deserialize the request
              ResourceDependencyInformation? resourceDependencyInformation;
              try
              {
                resourceDependencyInformation = JsonConvert.DeserializeObject<ResourceDependencyInformation>(requestBody);
              }
              catch (JsonException ex)
              {
                _logger.LogHttpValidationFailed(FunctionName, $"JSON deserialization failed: {ex.Message}");
                return CreateErrorResponse($"Invalid JSON format: {ex.Message}", 400);
              }

              if (resourceDependencyInformation == null)
              {
                _logger.LogHttpValidationFailed(FunctionName, "Failed to deserialize request body");
                return CreateErrorResponse("Invalid request body format.", 400);
              }

              var resourceId = resourceDependencyInformation.ResourceId ?? "Unknown";
              var resourceType = resourceDependencyInformation.ResourceType ?? "Unknown";

              _logger.LogProcessingResource(FunctionName, resourceId, resourceType);

              // Execute the provisioning check
              var result = await _dynamicAllowListingService.CheckProvisioningSucceeded(resourceDependencyInformation);

              if (result.Success)
              {
                _logger.LogResourceProcessedSuccess(FunctionName, resourceId);
                return new OkObjectResult(result);
              }
              else
              {
                _logger.LogValidationErrors(FunctionName, resourceId, result.Errors.Count);
                return new BadRequestObjectResult(result);
              }
            });
      }
    }

    private static string GetCorrelationId(HttpRequest req, string fallbackId)
    {
      if (req.Headers.TryGetValue("X-Correlation-ID", out var correlationHeader) &&
          !string.IsNullOrEmpty(correlationHeader))
      {
        return correlationHeader!;
      }

      if (req.Headers.TryGetValue("Request-Id", out var requestIdHeader) &&
          !string.IsNullOrEmpty(requestIdHeader))
      {
        return requestIdHeader!;
      }

      return fallbackId;
    }

    private static IActionResult CreateErrorResponse(string message, int statusCode)
    {
      var result = new ResultObject();
      result.Errors.Add(message);

      return statusCode switch
      {
        400 => new BadRequestObjectResult(result),
        _ => new ObjectResult(result) { StatusCode = statusCode }
      };
    }
  }
}