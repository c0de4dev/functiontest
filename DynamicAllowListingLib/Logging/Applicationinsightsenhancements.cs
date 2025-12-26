using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// Interface for custom telemetry service operations.
  /// </summary>
  public interface ICustomTelemetryService
  {
    /// <summary>
    /// Tracks a custom metric.
    /// </summary>
    void TrackMetric(string name, double value, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a custom event.
    /// </summary>
    void TrackEvent(string name, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);

    /// <summary>
    /// Tracks an exception with additional context.
    /// </summary>
    void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);

    /// <summary>
    /// Tracks an HTTP dependency call.
    /// </summary>
    void TrackDependency(string dependencyType, string target, string name, string data, DateTimeOffset startTime, TimeSpan duration, string resultCode, bool success);

    /// <summary>
    /// Tracks the start of an operation and returns an operation tracker.
    /// </summary>
    IOperationTracker StartOperation(string operationName, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Flushes all telemetry to Application Insights.
    /// </summary>
    void Flush();
  }

  /// <summary>
  /// Interface for tracking operation duration and outcome.
  /// </summary>
  public interface IOperationTracker : IDisposable
  {
    /// <summary>
    /// Gets the operation start time.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the current elapsed time.
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Marks the operation as successful.
    /// </summary>
    void SetSuccess();

    /// <summary>
    /// Marks the operation as failed with an optional error message.
    /// </summary>
    void SetFailed(string? errorMessage = null);

    /// <summary>
    /// Adds additional properties to the operation.
    /// </summary>
    void AddProperty(string key, string value);

    /// <summary>
    /// Adds a metric to the operation.
    /// </summary>
    void AddMetric(string key, double value);
  }

  /// <summary>
  /// Application Insights telemetry service with enhanced custom metrics and event tracking.
  /// </summary>
  public class ApplicationInsightsTelemetryService : ICustomTelemetryService
  {
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ApplicationInsightsTelemetryService> _logger;
    private readonly TimeProvider _timeProvider;

    // Metric names for common operations
    public static class MetricNames
    {
      public const string OperationDuration = "OperationDuration";
      public const string HttpRequestDuration = "HttpRequestDuration";
      public const string DatabaseOperationDuration = "DatabaseOperationDuration";
      public const string ResourceProcessingCount = "ResourceProcessingCount";
      public const string ErrorCount = "ErrorCount";
      public const string RetryCount = "RetryCount";
      public const string BatchSize = "BatchSize";
      public const string ThroughputPerSecond = "ThroughputPerSecond";
    }

    // Event names for common operations
    public static class EventNames
    {
      public const string ServiceOperationStarted = "DynamicAllowListing.ServiceOperationStarted";
      public const string ServiceOperationCompleted = "DynamicAllowListing.ServiceOperationCompleted";
      public const string ServiceOperationFailed = "DynamicAllowListing.ServiceOperationFailed";
      public const string ResourceProcessed = "DynamicAllowListing.ResourceProcessed";
      public const string NetworkRestrictionApplied = "DynamicAllowListing.NetworkRestrictionApplied";
      public const string ConfigurationValidated = "DynamicAllowListing.ConfigurationValidated";
      public const string DatabaseOperationCompleted = "DynamicAllowListing.DatabaseOperationCompleted";
      public const string HttpRequestCompleted = "DynamicAllowListing.HttpRequestCompleted";
      public const string CacheHit = "DynamicAllowListing.CacheHit";
      public const string CacheMiss = "DynamicAllowListing.CacheMiss";
    }

    public ApplicationInsightsTelemetryService(
        TelemetryClient telemetryClient,
        ILogger<ApplicationInsightsTelemetryService> logger,
        TimeProvider? timeProvider = null)
    {
      _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
      var metric = new MetricTelemetry(name, value);

      AddCorrelationProperties(metric.Properties);

      if (properties != null)
      {
        foreach (var prop in properties)
        {
          metric.Properties[prop.Key] = prop.Value;
        }
      }

      _telemetryClient.TrackMetric(metric);
      _logger.LogMetricTracked(name, value);
    }

    /// <inheritdoc />
    public void TrackEvent(string name, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
      var eventTelemetry = new EventTelemetry(name);

      AddCorrelationProperties(eventTelemetry.Properties);

      if (properties != null)
      {
        foreach (var prop in properties)
        {
          eventTelemetry.Properties[prop.Key] = prop.Value;
        }
      }

      if (metrics != null)
      {
        foreach (var metric in metrics)
        {
          eventTelemetry.Metrics[metric.Key] = metric.Value;
        }
      }

      _telemetryClient.TrackEvent(eventTelemetry);
      _logger.LogEventTracked(name);
    }

    /// <inheritdoc />
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
      var exceptionTelemetry = new ExceptionTelemetry(exception);

      AddCorrelationProperties(exceptionTelemetry.Properties);
      exceptionTelemetry.Properties["ExceptionType"] = exception.GetType().FullName ?? "Unknown";

      if (exception.InnerException != null)
      {
        exceptionTelemetry.Properties["InnerExceptionType"] = exception.InnerException.GetType().FullName ?? "Unknown";
        exceptionTelemetry.Properties["InnerExceptionMessage"] = exception.InnerException.Message;
      }

      if (properties != null)
      {
        foreach (var prop in properties)
        {
          exceptionTelemetry.Properties[prop.Key] = prop.Value;
        }
      }

      if (metrics != null)
      {
        foreach (var metric in metrics)
        {
          exceptionTelemetry.Metrics[metric.Key] = metric.Value;
        }
      }

      _telemetryClient.TrackException(exceptionTelemetry);
      _logger.LogExceptionTracked(exception.GetType().Name, exception.Message);
    }

    /// <inheritdoc />
    public void TrackDependency(string dependencyType, string target, string name, string data, DateTimeOffset startTime, TimeSpan duration, string resultCode, bool success)
    {
      var dependency = new DependencyTelemetry(dependencyType, target, name, data, startTime, duration, resultCode, success);

      AddCorrelationProperties(dependency.Properties);

      _telemetryClient.TrackDependency(dependency);
      _logger.LogDependencyTracked(dependencyType, target, name, duration.TotalMilliseconds, success);
    }

    /// <inheritdoc />
    public IOperationTracker StartOperation(string operationName, IDictionary<string, string>? properties = null)
    {
      var tracker = new OperationTracker(this, operationName, properties, _timeProvider);

      TrackEvent(EventNames.ServiceOperationStarted, new Dictionary<string, string>
      {
        ["OperationName"] = operationName
      });

      return tracker;
    }

    /// <inheritdoc />
    public void Flush()
    {
      _telemetryClient.Flush();
    }

    private static void AddCorrelationProperties(IDictionary<string, string> properties)
    {
      properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("o");

      if (Activity.Current != null)
      {
        properties["TraceId"] = Activity.Current.TraceId.ToString();
        properties["SpanId"] = Activity.Current.SpanId.ToString();
        properties["CorrelationId"] = Activity.Current.TraceId.ToString();
      }
    }

    /// <summary>
    /// Internal operation tracker implementation.
    /// </summary>
    private class OperationTracker : IOperationTracker
    {
      private readonly ApplicationInsightsTelemetryService _telemetryService;
      private readonly string _operationName;
      private readonly TimeProvider _timeProvider;
      private readonly long _startTimestamp;
      private readonly Dictionary<string, string> _properties;
      private readonly Dictionary<string, double> _metrics;
      private bool _success = true;
      private string? _errorMessage;
      private bool _disposed;

      public OperationTracker(
          ApplicationInsightsTelemetryService telemetryService,
          string operationName,
          IDictionary<string, string>? initialProperties,
          TimeProvider timeProvider)
      {
        _telemetryService = telemetryService;
        _operationName = operationName;
        _timeProvider = timeProvider;
        _startTimestamp = timeProvider.GetTimestamp();
        _properties = new Dictionary<string, string>
        {
          ["OperationName"] = operationName
        };
        _metrics = new Dictionary<string, double>();

        if (initialProperties != null)
        {
          foreach (var prop in initialProperties)
          {
            _properties[prop.Key] = prop.Value;
          }
        }
      }

      public DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

      public TimeSpan Elapsed => _timeProvider.GetElapsedTime(_startTimestamp);

      public void SetSuccess()
      {
        _success = true;
        _errorMessage = null;
      }

      public void SetFailed(string? errorMessage = null)
      {
        _success = false;
        _errorMessage = errorMessage;
      }

      public void AddProperty(string key, string value)
      {
        _properties[key] = value;
      }

      public void AddMetric(string key, double value)
      {
        _metrics[key] = value;
      }

      public void Dispose()
      {
        if (_disposed) return;
        _disposed = true;

        var elapsed = _timeProvider.GetElapsedTime(_startTimestamp);
        var elapsedMs = (long)elapsed.TotalMilliseconds;

        _properties["Success"] = _success.ToString();
        _properties["DurationMs"] = elapsedMs.ToString();

        if (!string.IsNullOrEmpty(_errorMessage))
        {
          _properties["ErrorMessage"] = _errorMessage;
        }

        _metrics[MetricNames.OperationDuration] = elapsedMs;

        var eventName = _success ? EventNames.ServiceOperationCompleted : EventNames.ServiceOperationFailed;
        _telemetryService.TrackEvent(eventName, _properties, _metrics);

        // Also track duration as a metric
        _telemetryService.TrackMetric(MetricNames.OperationDuration, elapsedMs,
            new Dictionary<string, string>
            {
              ["OperationName"] = _operationName,
              ["Success"] = _success.ToString()
            });
      }
    }
  }

  /// <summary>
  /// Extension methods for common telemetry operations.
  /// </summary>
  public static class TelemetryExtensions
  {
    /// <summary>
    /// Tracks an HTTP request completion.
    /// </summary>
    public static void TrackHttpRequest(
        this ICustomTelemetryService telemetry,
        string method,
        string url,
        int statusCode,
        long durationMs,
        bool success)
    {
      telemetry.TrackEvent(ApplicationInsightsTelemetryService.EventNames.HttpRequestCompleted,
          new Dictionary<string, string>
          {
            ["Method"] = method,
            ["Url"] = url,
            ["StatusCode"] = statusCode.ToString(),
            ["Success"] = success.ToString()
          },
          new Dictionary<string, double>
          {
            [ApplicationInsightsTelemetryService.MetricNames.HttpRequestDuration] = durationMs
          });
    }

    /// <summary>
    /// Tracks a database operation completion.
    /// </summary>
    public static void TrackDatabaseOperation(
        this ICustomTelemetryService telemetry,
        string operation,
        string documentId,
        long durationMs,
        bool success)
    {
      telemetry.TrackEvent(ApplicationInsightsTelemetryService.EventNames.DatabaseOperationCompleted,
          new Dictionary<string, string>
          {
            ["Operation"] = operation,
            ["DocumentId"] = documentId,
            ["Success"] = success.ToString()
          },
          new Dictionary<string, double>
          {
            [ApplicationInsightsTelemetryService.MetricNames.DatabaseOperationDuration] = durationMs
          });
    }

    /// <summary>
    /// Tracks a resource processing event.
    /// </summary>
    public static void TrackResourceProcessed(
        this ICustomTelemetryService telemetry,
        string resourceId,
        string resourceType,
        string operation,
        bool success)
    {
      telemetry.TrackEvent(ApplicationInsightsTelemetryService.EventNames.ResourceProcessed,
          new Dictionary<string, string>
          {
            ["ResourceId"] = resourceId,
            ["ResourceType"] = resourceType,
            ["Operation"] = operation,
            ["Success"] = success.ToString()
          });
    }

    /// <summary>
    /// Tracks batch operation metrics.
    /// </summary>
    public static void TrackBatchOperation(
        this ICustomTelemetryService telemetry,
        string operationName,
        int totalItems,
        int successCount,
        int failedCount,
        long durationMs)
    {
      var successRate = totalItems > 0 ? (double)successCount / totalItems * 100 : 0;
      var throughput = durationMs > 0 ? (double)totalItems / durationMs * 1000 : 0;

      telemetry.TrackEvent($"DynamicAllowListing.BatchOperation.{operationName}",
          new Dictionary<string, string>
          {
            ["OperationName"] = operationName,
            ["TotalItems"] = totalItems.ToString(),
            ["SuccessCount"] = successCount.ToString(),
            ["FailedCount"] = failedCount.ToString()
          },
          new Dictionary<string, double>
          {
            [ApplicationInsightsTelemetryService.MetricNames.OperationDuration] = durationMs,
            [ApplicationInsightsTelemetryService.MetricNames.BatchSize] = totalItems,
            [ApplicationInsightsTelemetryService.MetricNames.ThroughputPerSecond] = throughput,
            ["SuccessRate"] = successRate
          });
    }
  }

  /// <summary>
  /// Logger extensions for telemetry service operations.
  /// Event ID Range: 10000-10099
  /// </summary>
  public static partial class TelemetryLoggerExtensions
  {
    [LoggerMessage(
        EventId = 10000,
        Level = LogLevel.Debug,
        Message = "Metric tracked | Name: {MetricName} | Value: {Value}")]
    public static partial void LogMetricTracked(
        this ILogger logger,
        string metricName,
        double value);

    [LoggerMessage(
        EventId = 10001,
        Level = LogLevel.Debug,
        Message = "Event tracked | Name: {EventName}")]
    public static partial void LogEventTracked(
        this ILogger logger,
        string eventName);

    [LoggerMessage(
        EventId = 10002,
        Level = LogLevel.Debug,
        Message = "Exception tracked | Type: {ExceptionType} | Message: {Message}")]
    public static partial void LogExceptionTracked(
        this ILogger logger,
        string exceptionType,
        string message);

    [LoggerMessage(
        EventId = 10003,
        Level = LogLevel.Debug,
        Message = "Dependency tracked | Type: {DependencyType} | Target: {Target} | Name: {Name} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogDependencyTracked(
        this ILogger logger,
        string dependencyType,
        string target,
        string name,
        double durationMs,
        bool success);
  }
}