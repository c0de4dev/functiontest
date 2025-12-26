using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Logging
{
  public class TelemetryService
  {
    private readonly ILogger<TelemetryService> _logger;
    private readonly ActivitySource _activitySource;

    public TelemetryService(ILogger<TelemetryService> logger)
    {
      _logger = logger;
      _activitySource = new ActivitySource("DynamicAllowListing");
    }

    public async Task<T> TrackOperationAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        Dictionary<string, object>? tags = null)
    {
      using var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal);
      var startTimestamp = TimeProvider.System.GetTimestamp();

      var contextTags = new Dictionary<string, object>
      {
        ["OperationName"] = operationName,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      };

      if (tags != null)
      {
        foreach (var tag in tags)
        {
          contextTags[tag.Key] = tag.Value;
          activity?.SetTag(tag.Key, tag.Value);
        }
      }

      try
      {
        using (_logger.BeginScope(contextTags))
        {
          _logger.LogInformation("Starting operation {OperationName}", operationName);

          var result = await operation();

          var elapsedMs = GetElapsedMilliseconds(startTimestamp);
          activity?.SetTag("success", true);
          activity?.SetTag("duration_ms", elapsedMs);

          if (elapsedMs > 5000)
          {
            _logger.LogSlowOperation(operationName, elapsedMs, 5000);
          }

          _logger.LogInformation(
              "Completed operation {OperationName} in {DurationMs}ms",
              operationName,
              elapsedMs);

          return result;
        }
      }
      catch (Exception ex)
      {
        var elapsedMs = GetElapsedMilliseconds(startTimestamp);
        activity?.SetTag("success", false);
        activity?.SetTag("error", ex.Message);
        activity?.SetTag("duration_ms", elapsedMs);

        _logger.LogError(
            ex,
            "Operation {OperationName} failed after {DurationMs}ms",
            operationName,
            elapsedMs);

        throw;
      }
    }

    public T TrackOperation<T>(
        string operationName,
        Func<T> operation,
        Dictionary<string, object>? tags = null)
    {
      using var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal);
      var startTimestamp = TimeProvider.System.GetTimestamp();

      var contextTags = new Dictionary<string, object>
      {
        ["OperationName"] = operationName,
        ["CorrelationId"] = CorrelationContext.CorrelationId
      };

      if (tags != null)
      {
        foreach (var tag in tags)
        {
          contextTags[tag.Key] = tag.Value;
          activity?.SetTag(tag.Key, tag.Value);
        }
      }

      try
      {
        using (_logger.BeginScope(contextTags))
        {
          _logger.LogInformation("Starting operation {OperationName}", operationName);

          var result = operation();

          var elapsedMs = GetElapsedMilliseconds(startTimestamp);
          activity?.SetTag("success", true);
          activity?.SetTag("duration_ms", elapsedMs);

          _logger.LogInformation(
              "Completed operation {OperationName} in {DurationMs}ms",
              operationName,
              elapsedMs);

          return result;
        }
      }
      catch (Exception ex)
      {
        var elapsedMs = GetElapsedMilliseconds(startTimestamp);
        activity?.SetTag("success", false);
        activity?.SetTag("error", ex.Message);
        activity?.SetTag("duration_ms", elapsedMs);

        _logger.LogError(
            ex,
            "Operation {OperationName} failed after {DurationMs}ms",
            operationName,
            elapsedMs);

        throw;
      }
    }

    private static long GetElapsedMilliseconds(long startTimestamp)
    {
      return (long)TimeProvider.System.GetElapsedTime(startTimestamp).TotalMilliseconds;
    }
  }
}