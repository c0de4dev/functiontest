using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DynamicAllowListingLib.SettingsValidation
{
  public static class ValidationHelper
  {
    public static ResultObject TryParseJson<T>(this string @this, out T result)
    {
      ResultObject returnObj = new ResultObject();
      var settings = new JsonSerializerSettings
      {
        Error = (sender, args) =>
        {
          returnObj.Errors.Add(args.ErrorContext.Error.Message);
          args.ErrorContext.Handled = true;
        },
        MissingMemberHandling = MissingMemberHandling.Error
      };

      result = JsonConvert.DeserializeObject<T>(@this, settings)!;
      // Check for null result if deserialization failed silently
      if (result == null)
      {
        returnObj.Errors.Add("Deserialization resulted in null. The input JSON may be invalid.");
      }

      return returnObj;
    }


    public static bool IsValidAppServicePlanId(string resourceId) => 
      Regex.IsMatch(resourceId, Constants.AppServicePlanResourceIdRegex, RegexOptions.IgnoreCase);


    public static bool IsValidWebSiteId(string resourceId) =>
      Regex.IsMatch(resourceId, Constants.WebSiteResourceIdRegex, RegexOptions.IgnoreCase);
  }
}