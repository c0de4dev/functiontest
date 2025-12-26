using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// Provides correlation ID management for request tracing across distributed systems.
  /// This interface can be implemented by middleware in the consuming Azure Functions project.
  /// </summary>
  public interface ICorrelationIdProvider
  {
    /// <summary>
    /// Gets the current correlation ID for the request.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Gets the current request ID (from Application Insights).
    /// </summary>
    string? RequestId { get; }

    /// <summary>
    /// Gets the parent operation ID (for distributed tracing).
    /// </summary>
    string? ParentId { get; }

    /// <summary>
    /// Sets the correlation ID for the current async context.
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Sets up correlation context for background/timer-triggered operations.
    /// </summary>
    void InitializeForBackgroundOperation(string operationName);
  }

  /// <summary>
  /// AsyncLocal-based correlation context for tracking requests across async operations.
  /// Thread-safe and works across async/await boundaries.
  /// </summary>
  public static class CorrelationContext
  {
    private static readonly AsyncLocal<string> _correlationId = new();
    private static readonly AsyncLocal<string?> _requestId = new();
    private static readonly AsyncLocal<string?> _parentId = new();
    private static readonly AsyncLocal<string?> _operationName = new();
    private static readonly AsyncLocal<Activity?> _activity = new();

    /// <summary>
    /// Gets the current correlation ID, generating one if not set.
    /// </summary>
    public static string CorrelationId
    {
      get => _correlationId.Value ?? GenerateCorrelationId();
      private set => _correlationId.Value = value;
    }

    /// <summary>
    /// Gets the current request ID.
    /// </summary>
    public static string? RequestId
    {
      get => _requestId.Value;
      private set => _requestId.Value = value;
    }

    /// <summary>
    /// Gets the parent operation ID.
    /// </summary>
    public static string? ParentId
    {
      get => _parentId.Value;
      private set => _parentId.Value = value;
    }

    /// <summary>
    /// Gets the current operation name.
    /// </summary>
    public static string? OperationName
    {
      get => _operationName.Value;
      private set => _operationName.Value = value;
    }

    /// <summary>
    /// Gets the current Activity for distributed tracing.
    /// </summary>
    public static Activity? CurrentActivity
    {
      get => _activity.Value;
      private set => _activity.Value = value;
    }

    /// <summary>
    /// Sets the correlation ID for the current async context.
    /// </summary>
    public static void SetCorrelationId(string correlationId)
    {
      CorrelationId = correlationId ?? GenerateCorrelationId();
    }

    /// <summary>
    /// Sets the request ID for the current async context.
    /// </summary>
    public static void SetRequestId(string? requestId)
    {
      RequestId = requestId;
    }

    /// <summary>
    /// Sets the parent ID for the current async context.
    /// </summary>
    public static void SetParentId(string? parentId)
    {
      ParentId = parentId;
    }

    /// <summary>
    /// Sets the operation name for the current async context.
    /// </summary>
    public static void SetOperationName(string? operationName)
    {
      OperationName = operationName;
    }

    /// <summary>
    /// Sets up a new Activity for distributed tracing.
    /// </summary>
    public static Activity? StartActivity(string operationName)
    {
      var activity = new Activity(operationName);
      activity.SetTag("CorrelationId", CorrelationId);
      activity.Start();
      CurrentActivity = activity;
      return activity;
    }

    /// <summary>
    /// Initializes correlation context from HTTP headers.
    /// </summary>
    /// <param name="headers">Dictionary of HTTP headers</param>
    public static void InitializeFromHeaders(IDictionary<string, string?> headers)
    {
      // Standard correlation headers
      const string CorrelationIdHeader = "X-Correlation-ID";
      const string RequestIdHeader = "X-Request-ID";
      const string TraceParentHeader = "traceparent";
      const string ApplicationInsightsRequestIdHeader = "Request-Id";
      const string ApplicationInsightsOperationIdHeader = "x-ms-request-id";

      // Try to get correlation ID from various headers
      string? correlationId = null;

      if (headers.TryGetValue(CorrelationIdHeader, out var corrId) && !string.IsNullOrEmpty(corrId))
      {
        correlationId = corrId;
      }
      else if (headers.TryGetValue(ApplicationInsightsRequestIdHeader, out var reqId) && !string.IsNullOrEmpty(reqId))
      {
        correlationId = reqId;
      }
      else if (headers.TryGetValue(ApplicationInsightsOperationIdHeader, out var opId) && !string.IsNullOrEmpty(opId))
      {
        correlationId = opId;
      }
      else if (headers.TryGetValue(TraceParentHeader, out var traceParent) && !string.IsNullOrEmpty(traceParent))
      {
        // Parse W3C trace context: version-traceid-parentid-flags
        var parts = traceParent.Split('-');
        if (parts.Length >= 2)
        {
          correlationId = parts[1];
        }
      }

      // Set correlation ID
      SetCorrelationId(correlationId ?? GenerateCorrelationId());

      // Set request ID if present
      if (headers.TryGetValue(RequestIdHeader, out var requestId))
      {
        SetRequestId(requestId);
      }
    }

    /// <summary>
    /// Initializes correlation context for a background operation.
    /// </summary>
    public static void InitializeForBackgroundOperation(string operationName)
    {
      var correlationId = GenerateCorrelationId();
      SetCorrelationId(correlationId);
      SetOperationName(operationName);
      StartActivity(operationName);
    }

    /// <summary>
    /// Clears the correlation context.
    /// </summary>
    public static void Clear()
    {
      _correlationId.Value = null!;
      _requestId.Value = null;
      _parentId.Value = null;
      _operationName.Value = null;
      _activity.Value?.Stop();
      _activity.Value = null;
    }

    /// <summary>
    /// Generates a new correlation ID.
    /// </summary>
    public static string GenerateCorrelationId()
    {
      return Guid.NewGuid().ToString("N");
    }
  }

  /// <summary>
  /// Logger extension methods for correlation-aware logging.
  /// </summary>
  public static partial class CorrelationLoggerExtensions
  {
    private static readonly EventId CorrelationStarted = new(10100, "CorrelationStarted");
    private static readonly EventId CorrelationCompleted = new(10101, "CorrelationCompleted");
    private static readonly EventId BackgroundOperationStarted = new(10110, "BackgroundOperationStarted");
    private static readonly EventId BackgroundOperationCompleted = new(10111, "BackgroundOperationCompleted");

    /// <summary>
    /// Creates a logging scope with correlation context.
    /// </summary>
    public static IDisposable? BeginCorrelationScope(
        this ILogger logger,
        string operationName,
        IDictionary<string, object>? additionalProperties = null)
    {
      var scopeProperties = new Dictionary<string, object>
      {
        ["CorrelationId"] = CorrelationContext.CorrelationId,
        ["OperationName"] = operationName,
        ["Timestamp"] = DateTimeOffset.UtcNow
      };

      if (!string.IsNullOrEmpty(CorrelationContext.RequestId))
      {
        scopeProperties["RequestId"] = CorrelationContext.RequestId;
      }

      if (!string.IsNullOrEmpty(CorrelationContext.ParentId))
      {
        scopeProperties["ParentId"] = CorrelationContext.ParentId;
      }

      if (additionalProperties != null)
      {
        foreach (var kvp in additionalProperties)
        {
          scopeProperties[kvp.Key] = kvp.Value;
        }
      }

      return logger.BeginScope(scopeProperties);
    }

    /// <summary>
    /// Logs the start of a correlated operation.
    /// </summary>
    [LoggerMessage(
        EventId = 10100,
        Level = LogLevel.Information,
        Message = "Operation started | OperationName: {OperationName} | CorrelationId: {CorrelationId}")]
    public static partial void LogCorrelationStarted(
        this ILogger logger,
        string operationName,
        string correlationId);

    /// <summary>
    /// Logs the completion of a correlated operation.
    /// </summary>
    [LoggerMessage(
        EventId = 10101,
        Level = LogLevel.Information,
        Message = "Operation completed | OperationName: {OperationName} | CorrelationId: {CorrelationId} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogCorrelationCompleted(
        this ILogger logger,
        string operationName,
        string correlationId,
        long durationMs,
        bool success);

    /// <summary>
    /// Logs the start of a background operation.
    /// </summary>
    [LoggerMessage(
        EventId = 10110,
        Level = LogLevel.Information,
        Message = "Background operation started | OperationName: {OperationName} | CorrelationId: {CorrelationId}")]
    public static partial void LogBackgroundOperationStarted(
        this ILogger logger,
        string operationName,
        string correlationId);

    /// <summary>
    /// Logs the completion of a background operation.
    /// </summary>
    [LoggerMessage(
        EventId = 10111,
        Level = LogLevel.Information,
        Message = "Background operation completed | OperationName: {OperationName} | CorrelationId: {CorrelationId} | Duration: {DurationMs}ms | Success: {Success}")]
    public static partial void LogBackgroundOperationCompleted(
        this ILogger logger,
        string operationName,
        string correlationId,
        long durationMs,
        bool success);

    /// <summary>
    /// Logs an error with correlation context.
    /// </summary>
    [LoggerMessage(
        EventId = 10120,
        Level = LogLevel.Error,
        Message = "Operation failed | OperationName: {OperationName} | CorrelationId: {CorrelationId} | Duration: {DurationMs}ms")]
    public static partial void LogCorrelationError(
        this ILogger logger,
        Exception exception,
        string operationName,
        string correlationId,
        long durationMs);
  }

  /// <summary>
  /// Scope-based correlation tracker for automatic cleanup.
  /// </summary>
  public sealed class CorrelationScope : IDisposable
  {
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly string _correlationId;
    private readonly TimeProvider _timeProvider;
    private readonly long _startTimestamp;
    private readonly IDisposable? _loggerScope;
    private bool _disposed;
    private bool _success = true;

    public CorrelationScope(
        ILogger logger,
        string operationName,
        IDictionary<string, object>? additionalProperties = null,
        TimeProvider? timeProvider = null)
    {
      _logger = logger;
      _operationName = operationName;
      _correlationId = CorrelationContext.CorrelationId;
      _timeProvider = timeProvider ?? TimeProvider.System;
      _startTimestamp = _timeProvider.GetTimestamp();
      _loggerScope = logger.BeginCorrelationScope(operationName, additionalProperties);

      _logger.LogCorrelationStarted(operationName, _correlationId);
    }

    /// <summary>
    /// Marks the operation as failed.
    /// </summary>
    public void MarkFailed()
    {
      _success = false;
    }

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;

      var elapsedMs = (long)_timeProvider.GetElapsedTime(_startTimestamp).TotalMilliseconds;
      _logger.LogCorrelationCompleted(
          _operationName,
          _correlationId,
          elapsedMs,
          _success);

      _loggerScope?.Dispose();
    }
  }
}