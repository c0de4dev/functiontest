using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAllowListingLib
{
  public class RestHelper : IRestHelper
  {
    private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;
    private readonly ILogger<RestHelper> _logger;
    private readonly HttpClient _httpClient;

    private AccessToken _cachedToken;

    public RestHelper(ILogger<RestHelper> logger, HttpClient httpClient)
    {
      _logger = logger;
      _httpClient = httpClient;

      // Follow pattern described at https://github.com/App-vNext/Polly/issues/414#issuecomment-371932576
      _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(ShouldRetryRequest)
          .WaitAndRetryAsync(
              retryCount: 9,
              sleepDurationProvider: (retryCount, response, context) =>
              {
                var serverWaitDuration = GetServerWaitDuration(response);
                var clientWaitDuration = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                var waitDuration = TimeSpan.FromMilliseconds(Math.Max(clientWaitDuration.TotalMilliseconds, serverWaitDuration.TotalMilliseconds));
                _logger.LogInformation("HttpRetryPolicy: RetryCount {retryCount}, waitDuration {waitDuration}ms", retryCount, waitDuration);
                return waitDuration;
              },
              onRetryAsync: (response, timespan, retryCount, context) => Task.CompletedTask);
    }

    /*
    public async Task<string> DoGET(string url)
    {
      _logger.LogInformation("HTTP GET URL:{Url}", url);

      var response = await _httpRetryPolicy.ExecuteAsync(async () =>
      {
        var request = await CreateRequestAsync(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
      });

      response.EnsureSuccessStatusCode();
      return await GetValidResponse(response);
    }
    */

    public async Task<string> DoGET(string url)
    {
      var sanitizedUrl = SanitizeString(url);
      try
      {
        _logger.LogInformation("HTTP GET URL: {Url}", sanitizedUrl);

        var response = await _httpRetryPolicy.ExecuteAsync(async () =>
        {
          var request = await CreateRequestAsync(HttpMethod.Get, url);
          return await _httpClient.SendAsync(request);
        });

        // Ensure the response status code indicates success
        response.EnsureSuccessStatusCode();

        return await GetValidResponse(response);
      }
      catch (HttpRequestException ex)
      {
        // Handle HTTP-related exceptions (e.g., connection issues, timeouts)
        _logger.LogError("Error getting access token for URL: {Url}", sanitizedUrl);
        throw new Exception($"HTTP request failed for URL: {sanitizedUrl}", ex);
      }
      catch (TaskCanceledException ex)
      {
        // Handle timeout exceptions
        _logger.LogError(ex, "The HTTP request timed out for URL: {Url}", sanitizedUrl);
        throw new Exception($"HTTP request timed out for URL: {sanitizedUrl}", ex);
      }
      catch (Exception ex)
      {
        // Handle any other exceptions
        _logger.LogError(ex, "An unexpected error occurred while processing URL: {Url}", sanitizedUrl);
        throw new Exception($"Unexpected error while processing URL: {sanitizedUrl}", ex);
      }
    }

    /*
    public async Task<string> DoPutAsJson(string url, string requestBodyJsonAsString)
    {
      _logger.LogInformation("HTTP PUT URL:{Url} RequestBody: {requestBody}", url, requestBodyJsonAsString);

      var response = await _httpRetryPolicy.ExecuteAsync(async () =>
      {
        var request = await CreateRequestAsync(HttpMethod.Put, url, requestBodyJsonAsString);
        return await _httpClient.SendAsync(request);
      });

      response.EnsureSuccessStatusCode();
      return await GetValidResponse(response);
    }
    */

    public async Task<string> DoPutAsJson(string url, string requestBodyJsonAsString)
    {
      var sanitizedUrl = SanitizeString(url);
      try
      {
        _logger.LogInformation("HTTP PUT URL: {Url}, RequestBody: {RequestBody}", sanitizedUrl, requestBodyJsonAsString);

        var response = await _httpRetryPolicy.ExecuteAsync(async () =>
        {
          var request = await CreateRequestAsync(HttpMethod.Put, url, requestBodyJsonAsString);
          return await _httpClient.SendAsync(request);
        });

        // Ensure the response status code is successful (2xx)
        response.EnsureSuccessStatusCode();

        return await GetValidResponse(response);
      }
      catch (HttpRequestException ex)
      {
        _logger.LogError(ex, "HTTP request error while performing PUT operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        // Optionally rethrow the exception with additional context
        throw new Exception($"HTTP PUT operation failed for URL: {sanitizedUrl}. Check inner exception for details.", ex);
      }
      catch (TaskCanceledException ex)
      {
        _logger.LogError(ex, "HTTP request timeout while performing PUT operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        throw new Exception($"HTTP PUT operation timed out for URL: {sanitizedUrl}.", ex);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An unexpected error occurred while performing PUT operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        throw new Exception($"An unexpected error occurred while performing PUT operation for URL: {sanitizedUrl}.", ex);
      }
    }

    /*
    public async Task<string> DoPatchAsJson(string url, string requestBodyJsonAsString)
    {
      _logger.LogInformation("HTTP PATCH URL:{Url} RequestBody: {requestbody}", url, requestBodyJsonAsString);

      var response = await _httpRetryPolicy.ExecuteAsync(async () =>
      {
        var request = await CreateRequestAsync(HttpMethod.Patch, url, requestBodyJsonAsString);
        return await _httpClient.SendAsync(request);
      });

      response.EnsureSuccessStatusCode();
      return await GetValidResponse(response);
    }
    */

    public async Task<string> DoPatchAsJson(string url, string requestBodyJsonAsString)
    {
      var sanitizedUrl = SanitizeString(url);
      try
      {
        _logger.LogInformation("HTTP PATCH URL: {Url}, RequestBody: {RequestBody}", sanitizedUrl, requestBodyJsonAsString);

        // Execute the PATCH request with retry logic
        var response = await _httpRetryPolicy.ExecuteAsync(async () =>
        {
          var request = await CreateRequestAsync(HttpMethod.Patch, url, requestBodyJsonAsString);
          return await _httpClient.SendAsync(request);
        });

        // Ensure the response status code indicates success (2xx)
        response.EnsureSuccessStatusCode();

        return await GetValidResponse(response);
      }
      catch (HttpRequestException ex)
      {
        _logger.LogError(ex, "HTTP request error during PATCH operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        // Re-throw with additional context
        throw new Exception($"HTTP PATCH operation failed for URL: {sanitizedUrl}. See inner exception for details.", ex);
      }
      catch (TaskCanceledException ex)
      {
        _logger.LogError(ex, "HTTP request timeout during PATCH operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        throw new Exception($"HTTP PATCH operation timed out for URL: {sanitizedUrl}.", ex);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Unexpected error occurred during PATCH operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        throw new Exception($"Unexpected error occurred during PATCH operation for URL: {sanitizedUrl}.", ex);
      }
    }

    /*
    public async Task<string> DoPostAsJson(string url, string requestBodyJsonAsString)
    {
      _logger.LogInformation("HTTP POST URL:{Url} RequestBody: {requestbody}", url, requestBodyJsonAsString);

      var response = await _httpRetryPolicy.ExecuteAsync(async () =>
      {
        var request = await CreateRequestAsync(HttpMethod.Post, url, requestBodyJsonAsString);
        return await _httpClient.SendAsync(request);
      });

      response.EnsureSuccessStatusCode();
      return await GetValidResponse(response);
    }
    */

    public async Task<string> DoPostAsJson(string url, string requestBodyJsonAsString)
    {
      var sanitizedUrl = SanitizeString(url);
      try
      {
        _logger.LogInformation("HTTP POST URL: {Url}, RequestBody: {RequestBody}", sanitizedUrl, requestBodyJsonAsString);

        // Execute the POST request with retry logic
        var response = await _httpRetryPolicy.ExecuteAsync(async () =>
        {
          var request = await CreateRequestAsync(HttpMethod.Post, url, requestBodyJsonAsString);
          return await _httpClient.SendAsync(request);
        });

        // Ensure the response status code indicates success (2xx)
        response.EnsureSuccessStatusCode();

        // Extract and return the valid response
        return await GetValidResponse(response);
      }
      catch (HttpRequestException ex)
      {
        _logger.LogError(ex, "HTTP request error during POST operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        // Re-throw the exception with additional context
        throw new Exception($"HTTP POST operation failed for URL: {sanitizedUrl}. Check inner exception for details.", ex);
      }
      catch (TaskCanceledException ex)
      {
        _logger.LogError(ex, "HTTP request timeout during POST operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        throw new Exception($"HTTP POST operation timed out for URL: {sanitizedUrl}.", ex);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Unexpected error occurred during POST operation on URL: {Url}. InnerException: {InnerException}",
            sanitizedUrl, ex.InnerException?.Message);

        throw new Exception($"Unexpected error occurred during POST operation for URL: {sanitizedUrl}.", ex);
      }
    }


    public async Task DoDelete(string url)
    {
      var sanitizedUrl = SanitizeString(url);
      _logger.LogInformation("HTTP DELETE URL: {Url}", url.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", ""));

      var response = await _httpRetryPolicy.ExecuteAsync(async () =>
      {
        var request = await CreateRequestAsync(HttpMethod.Delete, url);
        return await _httpClient.SendAsync(request);
      });

      response.EnsureSuccessStatusCode();
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, string? content = null)
    {
      var sanitizedUrl = SanitizeString(url);
      var request = new HttpRequestMessage(method, url);

      _logger.LogInformation("URL for Request: {Url}", sanitizedUrl);

      var token = await GetAccessToken(url);

      //_logger.LogInformation("TOKEN FOR REQUEST:{token}", token);

      request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      if (content != null)
      {
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");
      }

      return request;
    }

    public async Task<string?> GetAccessTokenOLD(string url)
    {
      try
      {
        var azureServiceTokenProvider = new AzureServiceTokenProvider();
        return await azureServiceTokenProvider.GetAccessTokenAsync(GetBaseUrl(url));
      }
      catch (Exception e)
      {
        var sanitizedUrl = url.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        _logger.LogError(e, "Error getting access token for URL: {Url}", sanitizedUrl);
        throw;
      }
    }

    public async Task<string?> GetAccessToken(string url)
    {
      try
      {
        // Check if the cached token is still valid
        if (_cachedToken.Token != null && _cachedToken.ExpiresOn > DateTimeOffset.UtcNow)
        {
          _logger.LogInformation("Using cached access token.");
          return _cachedToken.Token.ToString();
        }
        else
        {
          var credential = new DefaultAzureCredential();
          string scope = "https://management.azure.com/.default";
          //GetBaseUrl(url);  // Replace with your target resource's scope

          // Get the access token for the managed identity
          _cachedToken = await credential.GetTokenAsync(new TokenRequestContext(new[] { scope }));
          //return await azureServiceTokenProvider.GetAccessTokenAsync(GetBaseUrl(url));
          return _cachedToken.Token.ToString();
        }
      }
      catch (Exception e)
      {
        var sanitizedUrl = url.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        _logger.LogError(e, "Error getting access token for URL: {Url}", sanitizedUrl);
        throw;
      }
    }


    private async Task<string> GetValidResponse(HttpResponseMessage httpResponseMessage)
    {
      if (httpResponseMessage == null)
      {
        throw new ArgumentNullException(nameof(httpResponseMessage), "HttpResponseMessage was null.");
      }

      var responseMsg = await httpResponseMessage.Content.ReadAsStringAsync();
      if (string.IsNullOrEmpty(responseMsg))
      {
        throw new InvalidOperationException("Response content was found to be null or empty.");
      }

      return responseMsg;
    }

    private static bool ShouldRetryRequest(HttpResponseMessage httpResponseMessage)
    {
      if (httpResponseMessage.IsSuccessStatusCode)
      {
        return false;
      }

      return httpResponseMessage.StatusCode switch
      {
        (HttpStatusCode)429 => true,
        HttpStatusCode.GatewayTimeout => true,
        _ => false
      };
    }

    private TimeSpan GetServerWaitDuration(DelegateResult<HttpResponseMessage> response)
    {
      var responseHeaders = response?.Result?.Headers;

      if (responseHeaders == null)
      {
        return TimeSpan.Zero;
      }

      if (responseHeaders.TryGetValues("x-ms-user-quota-remaining", out IEnumerable<string>? quotaRemainingValues))
      {
        int remainingQuota = Int32.Parse(quotaRemainingValues.FirstOrDefault()!);
        _logger.LogInformation("Header x-ms-user-quota-remaining value is {remainingQuota}", remainingQuota);
        if (remainingQuota == 0 && responseHeaders.TryGetValues("x-ms-user-quota-resets-after", out IEnumerable<string>? resetsAfterValues))
        {
          TimeSpan resetsAfter = TimeSpan.Parse(resetsAfterValues.FirstOrDefault()!);
          _logger.LogInformation("Header x-ms-user-quota-resets-after value is {resetsAfter}", resetsAfter);
          // Delay by a random period to avoid bursting when the quota is reset.
          // Follow guidance https://docs.microsoft.com/en-us/azure/governance/resource-graph/concepts/guidance-for-throttled-requests#query-in-parallel
          var delay = (new Random()).Next(1, 5) * resetsAfter;
          return delay;
        }
      }

      var retryAfter = responseHeaders?.RetryAfter;
      if (retryAfter == null)
      {
        _logger.LogInformation("RetryAfter header in response is null. Returning ServerWaitDuration as Zero.");
        return TimeSpan.Zero;
      }

      var waitDuration = retryAfter.Date.HasValue
          ? retryAfter.Date.Value - DateTime.UtcNow
          : retryAfter.Delta.GetValueOrDefault(TimeSpan.Zero);
      _logger.LogInformation("ServerWaitDuration calculated to be {waitDuration}.", waitDuration);
      return waitDuration;
    }

    internal string GetBaseUrl(string url)
    {
      string[] urlParts = url.Split('/');
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(urlParts[0]);
      stringBuilder.Append("//");
      stringBuilder.Append(urlParts[2]);
      stringBuilder.Append("/");
      return stringBuilder.ToString();
    }

    internal static string SanitizeString(string url)
    {
      var sanitizedUrl = url.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
      return sanitizedUrl;
    }
  }
}