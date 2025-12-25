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
      var stopwatch = Stopwatch.StartNew();

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

          stopwatch.Stop();
          activity?.SetTag("success", true);
          activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

          if (stopwatch.ElapsedMilliseconds > 5000)
          {
            _logger.LogSlowOperation(operationName, stopwatch.ElapsedMilliseconds, 5000);
          }

          _logger.LogInformation(
              "Completed operation {OperationName} in {DurationMs}ms",
              operationName,
              stopwatch.ElapsedMilliseconds);

          return result;
        }
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        activity?.SetTag("success", false);
        activity?.SetTag("error", ex.Message);
        activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

        _logger.LogError(
            ex,
            "Operation {OperationName} failed after {DurationMs}ms",
            operationName,
            stopwatch.ElapsedMilliseconds);

        throw;
      }
    }

    public T TrackOperation<T>(
        string operationName,
        Func<T> operation,
        Dictionary<string, object>? tags = null)
    {
      using var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal);
      var stopwatch = Stopwatch.StartNew();

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

          stopwatch.Stop();
          activity?.SetTag("success", true);
          activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

          _logger.LogInformation(
              "Completed operation {OperationName} in {DurationMs}ms",
              operationName,
              stopwatch.ElapsedMilliseconds);

          return result;
        }
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        activity?.SetTag("success", false);
        activity?.SetTag("error", ex.Message);
        activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

        _logger.LogError(
            ex,
            "Operation {OperationName} failed after {DurationMs}ms",
            operationName,
            stopwatch.ElapsedMilliseconds);

        throw;
      }
    }
  }
}