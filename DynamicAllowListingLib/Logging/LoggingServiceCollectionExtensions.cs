using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// Extension methods for configuring enhanced logging services in the DynamicAllowListingLib library.
  /// These extensions should be called from the consuming Azure Functions project (AllowListingAzureFunction).
  /// </summary>
  public static class LoggingServiceCollectionExtensions
  {
    /// <summary>
    /// Adds enhanced logging services from DynamicAllowListingLib to the service collection.
    /// Call this from AllowListingAzureFunction's Program.cs or Startup.cs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for logging options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDynamicAllowListingLogging(
        this IServiceCollection services,
        Action<EnhancedLoggingOptions>? configureOptions = null)
    {
      var options = new EnhancedLoggingOptions();
      configureOptions?.Invoke(options);

      // Register options
      services.AddSingleton(options);

      // Register Application Insights telemetry service
      if (options.EnableApplicationInsights)
      {
        services.AddSingleton<ICustomTelemetryService>(sp =>
        {
          var telemetryClient = sp.GetRequiredService<TelemetryClient>();
          var logger = sp.GetRequiredService<ILogger<ApplicationInsightsTelemetryService>>();
          return new ApplicationInsightsTelemetryService(telemetryClient, logger);
        });
      }

      // Register enhanced telemetry service
      services.AddSingleton<EnhancedTelemetryService>(sp =>
      {
        var logger = sp.GetRequiredService<ILogger<EnhancedTelemetryService>>();
        var customTelemetry = sp.GetService<ICustomTelemetryService>();
        return new EnhancedTelemetryService(logger, customTelemetry);
      });

      return services;
    }
  }

  /// <summary>
  /// Options for configuring enhanced logging in DynamicAllowListingLib.
  /// </summary>
  public class EnhancedLoggingOptions
  {
    /// <summary>
    /// Gets or sets whether Application Insights integration is enabled.
    /// </summary>
    public bool EnableApplicationInsights { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the slow operation threshold in milliseconds.
    /// </summary>
    public long SlowOperationThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the very slow operation threshold in milliseconds.
    /// </summary>
    public long VerySlowOperationThresholdMs { get; set; } = 10000;
  }
}