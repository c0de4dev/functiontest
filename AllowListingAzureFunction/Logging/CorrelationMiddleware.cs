using DynamicAllowListingLib.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AllowListingAzureFunction.Middleware
{
  /// <summary>
  /// Middleware that automatically establishes correlation context for all function invocations.
  /// This ensures consistent correlation ID tracking across HTTP triggers, queue triggers,
  /// Cosmos DB triggers, and durable function orchestrators/activities.
  /// </summary>
  public class CorrelationMiddleware : IFunctionsWorkerMiddleware
  {
    private readonly ILogger<CorrelationMiddleware> _logger;

    // Standard correlation headers
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string RequestIdHeader = "X-Request-ID";
    private const string TraceParentHeader = "traceparent";
    private const string ApplicationInsightsRequestIdHeader = "Request-Id";
    private const string ApplicationInsightsOperationIdHeader = "x-ms-request-id";

    public CorrelationMiddleware(ILogger<CorrelationMiddleware> logger)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
      var functionName = context.FunctionDefinition.Name;
      var invocationId = context.InvocationId;
      var startTimestamp = Stopwatch.GetTimestamp();

      // Initialize correlation context based on trigger type
      var correlationId = await ExtractOrGenerateCorrelationIdAsync(context, invocationId);

      CorrelationContext.SetCorrelationId(correlationId);
      CorrelationContext.SetRequestId(invocationId);
      CorrelationContext.SetOperationName(functionName);

      // Start an Activity for distributed tracing
      using var activity = StartActivity(functionName, correlationId);

      using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
      {
        ["FunctionName"] = functionName,
        ["InvocationId"] = invocationId,
        ["CorrelationId"] = correlationId,
        ["TriggerType"] = GetTriggerType(context)
      });

      try
      {
        _logger.LogInformation(
            "Function invocation started | Function: {FunctionName} | InvocationId: {InvocationId} | CorrelationId: {CorrelationId}",
            functionName, invocationId, correlationId);

        await next(context);

        var elapsedMs = GetElapsedMilliseconds(startTimestamp);

        _logger.LogInformation(
            "Function invocation completed | Function: {FunctionName} | InvocationId: {InvocationId} | Duration: {DurationMs}ms | Success: true",
            functionName, invocationId, elapsedMs);
      }
      catch (Exception ex)
      {
        var elapsedMs = GetElapsedMilliseconds(startTimestamp);

        _logger.LogError(ex,
            "Function invocation failed | Function: {FunctionName} | InvocationId: {InvocationId} | Duration: {DurationMs}ms",
            functionName, invocationId, elapsedMs);

        // Re-throw to let the Functions runtime handle the exception
        throw;
      }
      finally
      {
        // Clear correlation context to prevent cross-invocation contamination
        CorrelationContext.Clear();
      }
    }

    private async Task<string> ExtractOrGenerateCorrelationIdAsync(FunctionContext context, string invocationId)
    {
      // First, check if there's an existing Activity with a trace ID
      if (Activity.Current != null && Activity.Current.TraceId != default)
      {
        return Activity.Current.TraceId.ToString();
      }

      // Try to get correlation ID from HTTP request headers
      var httpRequest = await TryGetHttpRequestDataAsync(context);
      if (httpRequest != null)
      {
        var headerCorrelationId = ExtractCorrelationIdFromHeaders(httpRequest.Headers);
        if (!string.IsNullOrEmpty(headerCorrelationId))
        {
          return headerCorrelationId;
        }
      }

      // For queue/cosmos triggers, check binding data for embedded correlation ID
      var bindingData = context.BindingContext.BindingData;

      if (bindingData.TryGetValue("correlationId", out var correlationIdObj) &&
          correlationIdObj is string correlationId &&
          !string.IsNullOrEmpty(correlationId))
      {
        return correlationId;
      }

      // For durable functions, try to get the instance ID as correlation
      if (bindingData.TryGetValue("instanceId", out var instanceIdObj) &&
          instanceIdObj is string instanceId &&
          !string.IsNullOrEmpty(instanceId))
      {
        return instanceId;
      }

      // Default to invocation ID (always available)
      return invocationId;
    }

    private string? ExtractCorrelationIdFromHeaders(HttpHeadersCollection headers)
    {
      // Try standard correlation headers in order of preference
      if (TryGetHeaderValue(headers, CorrelationIdHeader, out var correlationId))
        return correlationId;

      if (TryGetHeaderValue(headers, ApplicationInsightsRequestIdHeader, out var requestId))
        return requestId;

      if (TryGetHeaderValue(headers, ApplicationInsightsOperationIdHeader, out var operationId))
        return operationId;

      if (TryGetHeaderValue(headers, TraceParentHeader, out var traceParent) && !string.IsNullOrEmpty(traceParent))
      {
        // Parse W3C trace context: version-traceid-parentid-flags
        var parts = traceParent.Split('-');
        if (parts.Length >= 2)
        {
          return parts[1];
        }
      }

      return null;
    }

    private static bool TryGetHeaderValue(HttpHeadersCollection headers, string headerName, out string? value)
    {
      value = null;
      if (headers.TryGetValues(headerName, out var values))
      {
        value = values.FirstOrDefault();
        return !string.IsNullOrEmpty(value);
      }
      return false;
    }

    private static async Task<HttpRequestData?> TryGetHttpRequestDataAsync(FunctionContext context)
    {
      try
      {
        var inputData = await context.GetHttpRequestDataAsync();
        return inputData;
      }
      catch
      {
        // Not an HTTP trigger or unable to get request data
        return null;
      }
    }

    private string GetTriggerType(FunctionContext context)
    {
      var entryPoint = context.FunctionDefinition.EntryPoint;

      // Determine trigger type from binding context
      var bindingData = context.BindingContext.BindingData;

      if (bindingData.ContainsKey("req") || bindingData.ContainsKey("httpTrigger"))
        return "HTTP";

      if (bindingData.ContainsKey("queueTrigger") || bindingData.ContainsKey("message"))
        return "Queue";

      if (bindingData.ContainsKey("cosmosDBTrigger") || bindingData.ContainsKey("documents"))
        return "CosmosDB";

      if (bindingData.ContainsKey("instanceId"))
        return "DurableFunction";

      return "Unknown";
    }

    private Activity? StartActivity(string functionName, string correlationId)
    {
      var activity = new Activity($"Function.{functionName}");
      activity.SetIdFormat(ActivityIdFormat.W3C);

      // Set trace ID from correlation ID if possible
      // ActivityTraceId.TryParse is not available in .NET Standard/.NET 6.
      // Instead, check if the correlationId is a valid 32-char hex and set as parent if possible.
      if (!string.IsNullOrEmpty(correlationId) && correlationId.Length == 32)
      {
        // W3C traceparent: 00-<trace-id>-<span-id>-<flags>
        // We can set the parentId in W3C format if possible
        var parentId = $"00-{correlationId}-" + ActivitySpanId.CreateRandom().ToString() + "-01";
        activity.SetParentId(parentId);
      }

      activity.AddTag("function.name", functionName);
      activity.AddTag("correlation.id", correlationId);

      activity.Start();

      return activity;
    }

    private static long GetElapsedMilliseconds(long startTimestamp)
    {
      return (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
    }
  }
}