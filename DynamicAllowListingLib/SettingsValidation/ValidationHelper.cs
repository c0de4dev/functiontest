using DynamicAllowListingLib.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace DynamicAllowListingLib.SettingsValidation
{
  public static class ValidationHelper
  {
    /// <summary>
    /// Attempts to parse JSON string into the specified type with comprehensive error handling.
    /// </summary>
    /// <typeparam name="T">The target type for deserialization</typeparam>
    /// <param name="this">The JSON string to parse</param>
    /// <param name="result">The deserialized object if successful</param>
    /// <param name="logger">Optional logger for structured logging</param>
    /// <returns>ResultObject containing any errors encountered during parsing</returns>
    public static ResultObject TryParseJson<T>(this string @this, out T result, ILogger? logger = null)
    {
      ResultObject returnObj = new ResultObject();
      var targetType = typeof(T).Name;
      var inputLength = @this?.Length ?? 0;

      // Log parsing start
      logger?.LogJsonParsingStarted(targetType, inputLength);

      // Handle null or empty input
      if (string.IsNullOrWhiteSpace(@this))
      {
        logger?.LogJsonInputNullOrEmpty(targetType);
        returnObj.Errors.Add("Input JSON string is null or empty.");
        result = default!;
        return returnObj;
      }

      var settings = new JsonSerializerSettings
      {
        Error = (sender, args) =>
        {
          var errorMessage = args.ErrorContext.Error.Message;
          returnObj.Errors.Add(errorMessage);

          // Log each parsing error
          logger?.LogJsonParsingError(targetType, errorMessage);

          args.ErrorContext.Handled = true;
        },
        MissingMemberHandling = MissingMemberHandling.Error
      };

      try
      {
        result = JsonConvert.DeserializeObject<T>(@this, settings)!;

        // Check for null result if deserialization failed silently
        if (result == null)
        {
          logger?.LogJsonDeserializationNull(targetType);
          returnObj.Errors.Add("Deserialization resulted in null. The input JSON may be invalid.");
        }
        else if (!returnObj.Errors.Any())
        {
          // Log successful parsing only if no errors
          logger?.LogJsonParsingCompleted(targetType, inputLength);
        }
      }
      catch (JsonException ex)
      {
        logger?.LogJsonParsingException(ex, targetType);
        returnObj.Errors.Add($"JSON parsing exception: {ex.Message}");
        result = default!;
      }

      // Log final failure summary if there were errors
      if (returnObj.Errors.Any())
      {
        logger?.LogJsonParsingFailed(
            targetType,
            returnObj.Errors.Count,
            string.Join("; ", returnObj.Errors));
      }

      return returnObj;
    }

    /// <summary>
    /// Original method signature maintained for backward compatibility.
    /// Calls the new overload without logging.
    /// </summary>
    public static ResultObject TryParseJson<T>(this string @this, out T result)
    {
      return TryParseJson(@this, out result, logger: null);
    }

    public static bool IsValidAppServicePlanId(string resourceId) =>
      Regex.IsMatch(resourceId, Constants.AppServicePlanResourceIdRegex, RegexOptions.IgnoreCase);

    public static bool IsValidWebSiteId(string resourceId) =>
      Regex.IsMatch(resourceId, Constants.WebSiteResourceIdRegex, RegexOptions.IgnoreCase);
  }
}