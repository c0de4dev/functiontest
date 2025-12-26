using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  public class PerformanceLogger
  {
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    public PerformanceLogger(ILogger logger, TimeProvider? timeProvider = null)
    {
      _logger = logger;
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IDisposable TrackPerformance(string operationName, Dictionary<string, object>? properties = null)
    {
      return new PerformanceTracker(_logger, operationName, properties, _timeProvider);
    }

    private class PerformanceTracker : IDisposable
    {
      private readonly ILogger _logger;
      private readonly string _operationName;
      private readonly Dictionary<string, object>? _properties;
      private readonly TimeProvider _timeProvider;
      private readonly long _startTimestamp;

      public PerformanceTracker(
          ILogger logger,
          string operationName,
          Dictionary<string, object>? properties,
          TimeProvider timeProvider)
      {
        _logger = logger;
        _operationName = operationName;
        _properties = properties;
        _timeProvider = timeProvider;
        _startTimestamp = timeProvider.GetTimestamp();
      }

      public void Dispose()
      {
        var elapsed = _timeProvider.GetElapsedTime(_startTimestamp);
        var elapsedMs = (long)elapsed.TotalMilliseconds;
        var elapsedTicks = elapsed.Ticks;

        var logProperties = new Dictionary<string, object>
        {
          ["Operation"] = _operationName,
          ["DurationMs"] = elapsedMs,
          ["DurationTicks"] = elapsedTicks,
          ["CorrelationId"] = CorrelationContext.CorrelationId
        };

        if (_properties != null)
        {
          foreach (var prop in _properties)
          {
            logProperties[prop.Key] = prop.Value;
          }
        }

        using (_logger.BeginScope(logProperties))
        {
          var level = elapsedMs switch
          {
            > 10000 => LogLevel.Error,
            > 5000 => LogLevel.Warning,
            _ => LogLevel.Information
          };

          _logger.Log(
              level,
              "Performance: {Operation} completed in {DurationMs}ms",
              _operationName,
              elapsedMs);
        }
      }
    }
  }
}