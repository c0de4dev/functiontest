using DynamicAllowListingLib.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// Enhanced telemetry service that provides comprehensive operation tracking,
  /// performance monitoring, and integration with Application Insights.
  /// Replaces and extends the original TelemetryService with additional capabilities.
  /// </summary>
  public class EnhancedTelemetryService
  {
    private readonly ILogger<EnhancedTelemetryService> _logger;
    private readonly ICustomTelemetryService? _customTelemetry;
    private readonly ActivitySource _activitySource;

    // Performance thresholds
    private const long SlowOperationThresholdMs = 5000;
    private const long VerySlowOperationThresholdMs = 10000;

    public EnhancedTelemetryService(
        ILogger<EnhancedTelemetryService> logger,
        ICustomTelemetryService? customTelemetry = null)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _customTelemetry = customTelemetry;
      _activitySource = new ActivitySource("DynamicAllowListing.Enhanced", "1.0.0");
    }

    /// <summary>
    /// Tracks an async operation with comprehensive logging, metrics, and distributed tracing.
    /// </summary>
    public async Task<T> TrackOperationAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        ResourceDependencyInformation? resourceInfo = null,
        Dictionary<string, object>? additionalTags = null)
    {
      using var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal);
      var stopwatch = Stopwatch.StartNew();
      var operationId = Guid.NewGuid().ToString("N")[..8];

      // Build context for logging
      var context = BuildOperationContext(operationName, operationId, resourceInfo, additionalTags);

      // Set activity tags for distributed tracing
      SetActivityTags(activity, context);

      using (_logger.BeginScope(context))
      {
        _logger.LogEnhancedOperationStart(operationName, operationId);

        try
        {
          var result = await operation();

          stopwatch.Stop();
          var durationMs = stopwatch.ElapsedMilliseconds;


          // Set success tags
          activity?.SetTag("success", true);
          activity?.SetTag("duration_ms", durationMs);

          _logger.LogEnhancedOperationComplete(operationName, operationId, durationMs, true);

          // Track metrics
          TrackOperationMetrics(operationName, durationMs, true, resourceInfo);

          return result;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          var durationMs = stopwatch.ElapsedMilliseconds;

          // Set error tags
          activity?.SetTag("success", false);
          activity?.SetTag("error", ex.Message);
          activity?.SetTag("error_type", ex.GetType().Name);
          activity?.SetTag("duration_ms", durationMs);

          _logger.LogEnhancedOperationFailed(ex, operationName, operationId, durationMs);

          // Track error metrics
          TrackOperationMetrics(operationName, durationMs, false, resourceInfo);
          _customTelemetry?.TrackException(ex, new Dictionary<string, string>
          {
            ["OperationName"] = operationName,
            ["OperationId"] = operationId,
            ["DurationMs"] = durationMs.ToString()
          });

          throw;
        }
      }
    }

    /// <summary>
    /// Tracks a synchronous operation with comprehensive logging, metrics, and distributed tracing.
    /// </summary>
    public T TrackOperation<T>(
        string operationName,
        Func<T> operation,
        ResourceDependencyInformation? resourceInfo = null,
        Dictionary<string, object>? additionalTags = null)
    {
      using var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal);
      var stopwatch = Stopwatch.StartNew();
      var operationId = Guid.NewGuid().ToString("N")[..8];

      var context = BuildOperationContext(operationName, operationId, resourceInfo, additionalTags);
      SetActivityTags(activity, context);

      using (_logger.BeginScope(context))
      {
        _logger.LogEnhancedOperationStart(operationName, operationId);

        try
        {
          var result = operation();

          stopwatch.Stop();
          var durationMs = stopwatch.ElapsedMilliseconds;

          activity?.SetTag("success", true);
          activity?.SetTag("duration_ms", durationMs);

          _logger.LogEnhancedOperationComplete(operationName, operationId, durationMs, true);
          TrackOperationMetrics(operationName, durationMs, true, resourceInfo);

          return result;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          var durationMs = stopwatch.ElapsedMilliseconds;

          activity?.SetTag("success", false);
          activity?.SetTag("error", ex.Message);
          activity?.SetTag("duration_ms", durationMs);

          _logger.LogEnhancedOperationFailed(ex, operationName, operationId, durationMs);
          TrackOperationMetrics(operationName, durationMs, false, resourceInfo);

          throw;
        }
      }
    }

    /// <summary>
    /// Tracks a void async operation.
    /// </summary>
    public async Task TrackOperationAsync(
        string operationName,
        Func<Task> operation,
        ResourceDependencyInformation? resourceInfo = null,
        Dictionary<string, object>? additionalTags = null)
    {
      await TrackOperationAsync(operationName, async () =>
      {
        await operation();
        return true;
      }, resourceInfo, additionalTags);
    }

    /// <summary>
    /// Tracks an HTTP dependency call.
    /// </summary>
    public async Task<T> TrackHttpDependencyAsync<T>(
        string operationName,
        string method,
        string url,
        Func<Task<(T Result, int StatusCode)>> operation)
    {
      var startTime = DateTimeOffset.UtcNow;
      var stopwatch = Stopwatch.StartNew();
      var sanitizedUrl = url;

      using var activity = _activitySource.StartActivity($"HTTP {method}", ActivityKind.Client);
      activity?.SetTag("http.method", method);
      activity?.SetTag("http.url", sanitizedUrl);

      using (_logger.BeginHttpDependencyScope(method, sanitizedUrl))
      {
        try
        {
          var (result, statusCode) = await operation();

          stopwatch.Stop();
          var success = statusCode >= 200 && statusCode < 400;

          activity?.SetTag("http.status_code", statusCode);
          activity?.SetTag("success", success);

          _customTelemetry?.TrackDependency(
              "HTTP",
              new Uri(url).Host,
              operationName,
              sanitizedUrl,
              startTime,
              stopwatch.Elapsed,
              statusCode.ToString(),
              success);

          _logger.LogHttpDependencyComplete(method, sanitizedUrl, statusCode, stopwatch.ElapsedMilliseconds, success);

          return result;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();

          activity?.SetTag("error", ex.Message);
          activity?.SetTag("success", false);

          _customTelemetry?.TrackDependency(
              "HTTP",
              new Uri(url).Host,
              operationName,
              sanitizedUrl,
              startTime,
              stopwatch.Elapsed,
              "Exception",
              false);

          _logger.LogHttpDependencyFailed(ex, method, sanitizedUrl, stopwatch.ElapsedMilliseconds);

          throw;
        }
      }
    }

    /// <summary>
    /// Tracks a database dependency call.
    /// </summary>
    public async Task<T> TrackDatabaseDependencyAsync<T>(
        string operationName,
        string databaseName,
        string? documentId,
        Func<Task<T>> operation)
    {
      var startTime = DateTimeOffset.UtcNow;
      var stopwatch = Stopwatch.StartNew();

      using var activity = _activitySource.StartActivity($"CosmosDB {operationName}", ActivityKind.Client);
      activity?.SetTag("db.system", "cosmosdb");
      activity?.SetTag("db.name", databaseName);
      activity?.SetTag("db.operation", operationName);

      using (_logger.BeginDatabaseDependencyScope(operationName, databaseName, documentId))
      {
        try
        {
          var result = await operation();

          stopwatch.Stop();

          activity?.SetTag("success", true);

          _customTelemetry?.TrackDependency(
              "CosmosDB",
              databaseName,
              operationName,
              documentId ?? "N/A",
              startTime,
              stopwatch.Elapsed,
              "Success",
              true);

          _logger.LogDatabaseDependencyComplete(operationName, databaseName, documentId ?? "N/A", stopwatch.ElapsedMilliseconds, true);

          return result;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();

          activity?.SetTag("success", false);
          activity?.SetTag("error", ex.Message);

          _customTelemetry?.TrackDependency(
              "CosmosDB",
              databaseName,
              operationName,
              documentId ?? "N/A",
              startTime,
              stopwatch.Elapsed,
              "Exception",
              false);

          _logger.LogDatabaseDependencyFailed(ex, operationName, databaseName, documentId ?? "N/A", stopwatch.ElapsedMilliseconds);

          throw;
        }
      }
    }

    /// <summary>
    /// Creates an operation tracker for manual tracking scenarios.
    /// </summary>
    public IOperationTracker StartManualOperation(string operationName, ResourceDependencyInformation? resourceInfo = null)
    {
      return _customTelemetry?.StartOperation(operationName, new Dictionary<string, string>
      {
        ["ResourceId"] = resourceInfo?.ResourceId ?? "N/A",
        ["ResourceName"] = resourceInfo?.ResourceName ?? "N/A"
      }) ?? new NoOpOperationTracker();
    }

    private Dictionary<string, object> BuildOperationContext(
        string operationName,
        string operationId,
        ResourceDependencyInformation? resourceInfo,
        Dictionary<string, object>? additionalTags)
    {
      var context = new Dictionary<string, object>
      {
        ["OperationName"] = operationName,
        ["OperationId"] = operationId,
        ["CorrelationId"] = Activity.Current?.TraceId.ToString() ?? operationId,
        ["Timestamp"] = DateTimeOffset.UtcNow
      };

      if (resourceInfo != null)
      {
        context["ResourceId"] = resourceInfo.ResourceId ?? "Unknown";
        context["ResourceName"] = resourceInfo.ResourceName ?? "Unknown";
        context["ResourceType"] = resourceInfo.ResourceType ?? "Unknown";
        context["SubscriptionId"] = resourceInfo.RequestSubscriptionId ?? "Unknown";
      }

      if (additionalTags != null)
      {
        foreach (var tag in additionalTags)
        {
          context[tag.Key] = tag.Value;
        }
      }

      return context;
    }

    private static void SetActivityTags(Activity? activity, Dictionary<string, object> context)
    {
      if (activity == null) return;

      foreach (var tag in context)
      {
        activity.SetTag(tag.Key, tag.Value?.ToString());
      }
    }

    private void TrackOperationMetrics(
        string operationName,
        long durationMs,
        bool success,
        ResourceDependencyInformation? resourceInfo)
    {
      _customTelemetry?.TrackMetric(
          ApplicationInsightsTelemetryService.MetricNames.OperationDuration,
          durationMs,
          new Dictionary<string, string>
          {
            ["OperationName"] = operationName,
            ["Success"] = success.ToString(),
            ["ResourceType"] = resourceInfo?.ResourceType ?? "Unknown"
          });

      if (!success)
      {
        _customTelemetry?.TrackMetric(
            ApplicationInsightsTelemetryService.MetricNames.ErrorCount,
            1,
            new Dictionary<string, string>
            {
              ["OperationName"] = operationName
            });
      }
    }

    /// <summary>
    /// No-op operation tracker for when custom telemetry is not available.
    /// </summary>
    private class NoOpOperationTracker : IOperationTracker
    {
      public DateTimeOffset StartTime => DateTimeOffset.UtcNow;
      public TimeSpan Elapsed => TimeSpan.Zero;

      public void SetSuccess() { }
      public void SetFailed(string? errorMessage = null) { }
      public void AddProperty(string key, string value) { }
      public void AddMetric(string key, double value) { }
      public void Dispose() { }
    }
  }

  /// <summary>
  /// Logger extensions for enhanced telemetry service.
  /// Event ID Range: 10400-10499
  /// </summary>
  public static partial class EnhancedTelemetryLoggerExtensions
  {
    // ============================================================
    // Operation Lifecycle (EventIds 10400-10419)
    // ============================================================

    [LoggerMessage(
        EventId = 10400,
        Level = LogLevel.Information,
        Message = "Operation started | Name: {OperationName} | OperationId: {OperationId}")]
    public static partial void LogEnhancedOperationStart(
        this ILogger logger,
        string operationName,
        string operationId);

    [LoggerMessage(
        EventId = 10401,
        Level = LogLevel.Information,
        Message = "Operation completed | Name: {OperationName} | OperationId: {OperationId} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogEnhancedOperationComplete(
        this ILogger logger,
        string operationName,
        string operationId,
        long durationMs,
        bool success);

    [LoggerMessage(
        EventId = 10402,
        Level = LogLevel.Error,
        Message = "Operation failed | Name: {OperationName} | OperationId: {OperationId} | Duration: {DurationMs}ms")]
    public static partial void LogEnhancedOperationFailed(
        this ILogger logger,
        Exception exception,
        string operationName,
        string operationId,
        long durationMs);

    // ============================================================
    // Performance Warnings (EventIds 10420-10439)
    // ============================================================

    [LoggerMessage(
        EventId = 10420,
        Level = LogLevel.Warning,
        Message = "Slow operation detected | Name: {OperationName} | Duration: {DurationMs}ms | Threshold: {ThresholdMs}ms")]
    public static partial void LogSlowOperation(
        this ILogger logger,
        string operationName,
        long durationMs,
        long thresholdMs);

    [LoggerMessage(
        EventId = 10421,
        Level = LogLevel.Warning,
        Message = "Very slow operation detected | Name: {OperationName} | Duration: {DurationMs}ms | Threshold: {ThresholdMs}ms")]
    public static partial void LogVerySlowOperation(
        this ILogger logger,
        string operationName,
        long durationMs,
        long thresholdMs);

    // ============================================================
    // Dependency Tracking (EventIds 10440-10459)
    // ============================================================

    [LoggerMessage(
        EventId = 10440,
        Level = LogLevel.Information,
        Message = "HTTP dependency completed | Method: {Method} | Url: {Url} | StatusCode: {StatusCode} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogHttpDependencyComplete(
        this ILogger logger,
        string method,
        string url,
        int statusCode,
        long durationMs,
        bool success);

    [LoggerMessage(
        EventId = 10441,
        Level = LogLevel.Error,
        Message = "HTTP dependency failed | Method: {Method} | Url: {Url} | Duration: {DurationMs}ms")]
    public static partial void LogHttpDependencyFailed(
        this ILogger logger,
        Exception exception,
        string method,
        string url,
        long durationMs);

    [LoggerMessage(
        EventId = 10442,
        Level = LogLevel.Information,
        Message = "Database dependency completed | Operation: {Operation} | Database: {Database} | DocumentId: {DocumentId} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogDatabaseDependencyComplete(
        this ILogger logger,
        string operation,
        string database,
        string documentId,
        long durationMs,
        bool success);

    [LoggerMessage(
        EventId = 10443,
        Level = LogLevel.Error,
        Message = "Database dependency failed | Operation: {Operation} | Database: {Database} | DocumentId: {DocumentId} | Duration: {DurationMs}ms")]
    public static partial void LogDatabaseDependencyFailed(
        this ILogger logger,
        Exception exception,
        string operation,
        string database,
        string documentId,
        long durationMs);

    // ============================================================
    // Scoped Logging Helpers
    // ============================================================

    /// <summary>
    /// Creates a logging scope for HTTP dependency operations.
    /// </summary>
    public static IDisposable? BeginHttpDependencyScope(
        this ILogger logger,
        string method,
        string url)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["DependencyType"] = "HTTP",
        ["HttpMethod"] = method,
        ["Url"] = url,
        ["CorrelationId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }

    /// <summary>
    /// Creates a logging scope for database dependency operations.
    /// </summary>
    public static IDisposable? BeginDatabaseDependencyScope(
        this ILogger logger,
        string operation,
        string database,
        string? documentId)
    {
      return logger.BeginScope(new Dictionary<string, object>
      {
        ["DependencyType"] = "CosmosDB",
        ["Operation"] = operation,
        ["Database"] = database,
        ["DocumentId"] = documentId ?? "N/A",
        ["CorrelationId"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
        ["Timestamp"] = DateTimeOffset.UtcNow
      });
    }
  }
}