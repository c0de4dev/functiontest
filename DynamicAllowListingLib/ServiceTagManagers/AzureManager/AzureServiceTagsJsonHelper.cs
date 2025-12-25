using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DynamicAllowListingLib.Logger;

namespace DynamicAllowListingLib.ServiceTagManagers.AzureManager
{
  // TODO: This is a hacky way of getting azure service tags in order to avoid the
  // 'in preview' rest api
  // https://docs.microsoft.com/en-us/azure/virtual-network/service-tags-overview#use-the-service-tag-discovery-api-public-preview
  // We need to move away from this technique and use native service tags
  // Refer https://newdaycards.atlassian.net/browse/DO-4398


  // create wrapper around WebClient so that we can write test
  public interface IWebClient
  { 
  }
  public class NewDayWebClient : HttpClient, IWebClient { }

  
  public class AzureServiceTagsJsonHelper : IAzureServiceTagsJsonHelper
  {
    private readonly ILogger<AzureServiceTagsJsonHelper> _logger;
    private readonly IMemoryCache _cache;
    private readonly IRestHelper _restHelper;
    private const string CacheKey = "servicetags";
    private const string ExpireKey = "memCacheExpiry";
 

    public AzureServiceTagsJsonHelper(ILogger<AzureServiceTagsJsonHelper> logger, IMemoryCache cache, IRestHelper restHelper)
    {
      _logger = logger;
      _cache = cache;
      _restHelper = restHelper;
    }

    public async Task<string> GetAzureServiceTagsJson(string requestedSubscriptionId)
    {
      FunctionLogger.MethodStart(_logger,nameof(GetAzureServiceTagsJson));
      try
      {
        string azureServiceTagsJson = string.Empty;
        if (!_cache.TryGetValue(CacheKey, out azureServiceTagsJson!))
        {
          // Key not in cache, so get data.
          FunctionLogger.MethodInformation(_logger, "Azure Service Tags Not Available in Memory Cache");
          string serviceTagListUrl = $"https://management.azure.com/subscriptions/{requestedSubscriptionId}/providers/Microsoft.Network/locations/northeurope/serviceTags?api-version=2022-05-01";

          FunctionLogger.MethodInformation(_logger, "Getting Azure Service Tags using REST endpoint");
          azureServiceTagsJson = await _restHelper.DoGET(serviceTagListUrl);

          // Set cache options.
          var cacheEntryOptions = new MemoryCacheEntryOptions()
              // Keep in cache for this time, reset time if accessed.
              .SetAbsoluteExpiration(TimeSpan.FromDays(1));

          // Save data in cache.
          _cache.Set(CacheKey, azureServiceTagsJson, cacheEntryOptions);
          _cache.Set(ExpireKey, DateTime.Now.AddDays(1), cacheEntryOptions);
        }
        else
        {
          FunctionLogger.MethodInformation(_logger, "Got Azure Service Tags from Memory Cache");
        }
        FunctionLogger.MethodInformation(_logger, $"AzureServiceTags data expiry Time:{CheckCachedExpiry()}");
        return azureServiceTagsJson!;
      }
      catch (Exception)
      {
        throw;
      }
    }

    internal static string GetJsonDownloadUrl(string fileContent)
    {
      const string regexString = @"(url=https:\/\/download.microsoft.com.*ServiceTags_Public_\d{8}.json)";
      Regex regex = new Regex(regexString);
      Match match = regex.Match(fileContent);
      string downloadUrl = match.Value.Replace("url=", "");
      return downloadUrl;
    }

    private string CheckCachedExpiry()
    {
      var MemCacheExpiryDate = Convert.ToDateTime(_cache.Get(ExpireKey));
      return MemCacheExpiryDate.ToString();
    }

  }

  public interface IAzureServiceTagsJsonHelper
  {
    Task<string> GetAzureServiceTagsJson(string requestedSubscriptionId);
  }
}
