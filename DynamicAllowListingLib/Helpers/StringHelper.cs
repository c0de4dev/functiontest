using DynamicAllowListingLib.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DynamicAllowListingLib
{
  /// <summary>
  /// Utility class for string operations related to Azure resource management.
  /// </summary>
  public static class StringHelper
  {
    /// <summary>
    /// Truncates a string to the specified maximum length.
    /// </summary>
    /// <param name="str">The string to truncate.</param>
    /// <param name="length">Maximum length (default: 28).</param>
    /// <returns>The truncated string.</returns>
    public static string Truncate(string? str, int length = 28)
    {
      if (string.IsNullOrEmpty(str))
        return string.Empty;

      int maxLength = Math.Min(str.Length, length);
      return str.Substring(0, maxLength);
    }

    /// <summary>
    /// Extracts the subscription ID from an Azure resource ID.
    /// </summary>
    /// <param name="resourceId">The full Azure resource ID.</param>
    /// <returns>The subscription ID.</returns>
    /// <exception cref="InvalidDataException">Thrown when the resource ID format is invalid.</exception>
    public static string GetSubscriptionId(string resourceId)
    {
      return GetSubscriptionId(resourceId, null);
    }

    /// <summary>
    /// Extracts the subscription ID from an Azure resource ID with optional logging.
    /// </summary>
    /// <param name="resourceId">The full Azure resource ID.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <returns>The subscription ID.</returns>
    /// <exception cref="InvalidDataException">Thrown when the resource ID format is invalid.</exception>
    public static string GetSubscriptionId(string resourceId, ILogger? logger)
    {
      logger?.LogSubscriptionIdExtraction(resourceId ?? "null");

      if (string.IsNullOrEmpty(resourceId))
      {
        logger?.LogInvalidResourceIdFormat(resourceId ?? "null");
        throw new InvalidDataException("Resource ID cannot be null or empty.");
      }

      string[] idParts = resourceId.Split('/');

      if (idParts.Length < 3)
      {
        logger?.LogInvalidResourceIdFormat(resourceId);
        throw new InvalidDataException($"Resource ID format is invalid. Expected at least 3 segments: {resourceId}");
      }

      return idParts[2];
    }

    /// <summary>
    /// Extracts unique subscription IDs from an array of resource IDs.
    /// </summary>
    /// <param name="resourceIds">Array of Azure resource IDs.</param>
    /// <returns>Array of distinct subscription IDs.</returns>
    /// <exception cref="InvalidDataException">Thrown when any resource ID format is invalid.</exception>
    public static string[] GetSubscriptionIds(string[] resourceIds)
    {
      return GetSubscriptionIds(resourceIds, null);
    }

    /// <summary>
    /// Extracts unique subscription IDs from an array of resource IDs with optional logging.
    /// </summary>
    /// <param name="resourceIds">Array of Azure resource IDs.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <returns>Array of distinct subscription IDs.</returns>
    /// <exception cref="InvalidDataException">Thrown when any resource ID format is invalid.</exception>
    public static string[] GetSubscriptionIds(string[] resourceIds, ILogger? logger)
    {
      if (resourceIds == null || resourceIds.Length == 0)
      {
        logger?.LogSubscriptionIdsExtracted(0);
        return Array.Empty<string>();
      }

      var sublist = new List<string>();

      foreach (var resourceId in resourceIds)
      {
        if (string.IsNullOrEmpty(resourceId))
          continue;

        var sections = resourceId.Split('/');

        if (sections.Length < 3)
        {
          logger?.LogInvalidResourceIdFormat(resourceId);
          throw new InvalidDataException($"Provided resourceId is not correctly formatted! resourceId: {resourceId}");
        }

        sublist.Add(sections[2]);
      }

      var distinctIds = sublist.Distinct().ToArray();
      logger?.LogSubscriptionIdsExtracted(distinctIds.Length);

      return distinctIds;
    }

    /// <summary>
    /// Extracts the resource name from an Azure resource ID.
    /// </summary>
    /// <param name="resourceId">The full Azure resource ID.</param>
    /// <returns>The resource name (last segment of the path).</returns>
    public static string GetResourceName(string resourceId)
    {
      if (string.IsNullOrEmpty(resourceId))
        return string.Empty;

      return Path.GetFileName(resourceId);
    }

    /// <summary>
    /// Sanitizes a string by removing newline characters.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string SanitizeForLogging(string input)
    {
      if (string.IsNullOrEmpty(input))
        return string.Empty;

      return input
          .Replace(Environment.NewLine, "")
          .Replace("\n", "")
          .Replace("\r", "");
    }
  }
}