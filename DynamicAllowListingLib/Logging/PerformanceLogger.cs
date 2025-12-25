using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DynamicAllowListingLib.Logging
{
  public class PerformanceLogger
  {
    private readonly ILogger _logger;

    public PerformanceLogger(ILogger logger)
    {
      _logger = logger;
    }

    public IDisposable TrackPerformance(string operationName, Dictionary<string, object>? properties = null)
    {
      return new PerformanceTracker(_logger, operationName, properties);
    }

    private class PerformanceTracker : IDisposable
    {
      private readonly ILogger _logger;
      private readonly string _operationName;
      private readonly Dictionary<string, object>? _properties;
      private readonly Stopwatch _stopwatch;

      public PerformanceTracker(ILogger logger, string operationName, Dictionary<string, object>? properties)
      {
        _logger = logger;
        _operationName = operationName;
        _properties = properties;
        _stopwatch = Stopwatch.StartNew();
      }

      public void Dispose()
      {
        _stopwatch.Stop();

        var logProperties = new Dictionary<string, object>
        {
          ["Operation"] = _operationName,
          ["DurationMs"] = _stopwatch.ElapsedMilliseconds,
          ["DurationTicks"] = _stopwatch.ElapsedTicks,
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
          var level = _stopwatch.ElapsedMilliseconds switch
          {
            > 10000 => LogLevel.Error,
            > 5000 => LogLevel.Warning,
            _ => LogLevel.Information
          };

          _logger.Log(
              level,
              "Performance: {Operation} completed in {DurationMs}ms",
              _operationName,
              _stopwatch.ElapsedMilliseconds);
        }
      }
    }
  }
}