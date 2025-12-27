using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Logging
{
  /// <summary>
  /// High-performance structured logging extensions for Azure Functions.
  /// Uses LoggerMessage source generators for optimal performance.
  /// EVENT ID RANGE: 100-999 (Function-level operations)
  /// </summary>
  public static partial class AzureFunctionLoggerExtensions
  {
    
    /// <summary>
    /// Creates a logging scope for resource processing.
    /// </summary>
    public static IDisposable? BeginResourceProcessingScope(
        this ILogger logger,
        string functionName,
        string resourceId,
        string? resourceType = null)
    {
      var scope = new Dictionary<string, object>
      {
        ["FunctionName"] = functionName,
        ["ResourceId"] = resourceId
      };

      if (!string.IsNullOrEmpty(resourceType))
      {
        scope["ResourceType"] = resourceType;
      }

      return logger.BeginScope(scope);
    }
  }
}