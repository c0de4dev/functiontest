using DynamicAllowListingLib.Logging;
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
  /// <summary>
  /// REST API helper for making HTTP requests with retry policies and access token management.
  /// </summary>
  public class RestHelper : IRestHelper
  {
    private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;
    private readonly ILogger<RestHelper> _logger;
    private readonly HttpClient _httpClient;

    public RestHelper(ILogger<RestHelper> logger, HttpClient httpClient)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

      // Follow pattern described at https://github.com/App-vNext/Polly/issues/414#issuecomment-371932576
      _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(ShouldRetryRequest)
          .WaitAndRetryAsync(
              retryCount: 9,
              sleepDurationProvider: (retryCount, response, context) =>
              {
                var serverWaitDuration = GetServerWaitDuration(response);
                var clientWaitDuration = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                var waitDuration = TimeSpan.FromMilliseconds(Math.Max(clientWaitDuration.TotalMilliseconds, serverWaitDuration.TotalMilliseconds));

                var url = context.TryGetValue("Url", out var urlValue) ? urlValue?.ToString() ?? "Unknown" : "Unknown";
                var method = context.TryGetValue("Method", out var methodValue) ? methodValue?.ToString() ?? "Unknown" : "Unknown";

                _logger.LogHttpRetry(method, url, retryCount);

                return waitDuration;
              },
              onRetryAsync: (response, timespan, retryCount, context) => Task.CompletedTask);
    }

    #region Public HTTP Methods

    /// <inheritdoc />
    public async Task<string?> DoGET(string url)
    {
      var sanitizedUrl = SanitizeString(url);

      using (_logger.BeginHttpOperationScope("GET", sanitizedUrl))
      {
        _logger.LogHttpGet(sanitizedUrl);

        try
        {
          var response = await _httpRetryPolicy.ExecuteAsync(
              async (context) =>
              {
                var request = await CreateRequestAsync(HttpMethod.Get, url);
                return await _httpClient.SendAsync(request);
              },
              new Context { ["Url"] = sanitizedUrl, ["Method"] = "GET" });

          response.EnsureSuccessStatusCode();
          var result = await GetValidResponse(response);

          _logger.LogHttpGetSuccess(sanitizedUrl, result?.Length ?? 0);

          return result;
        }
        catch (HttpRequestException ex)
        {
          _logger.LogHttpGetFailed(ex, sanitizedUrl, "HttpRequestException");
          throw new Exception($"HTTP GET operation failed for URL: {sanitizedUrl}. Check inner exception for details.", ex);
        }
        catch (TaskCanceledException ex)
        {
          _logger.LogHttpGetFailed(ex, sanitizedUrl, "Timeout");
          throw new Exception($"HTTP GET operation timed out for URL: {sanitizedUrl}.", ex);
        }
        catch (Exception ex)
        {
          _logger.LogHttpGetFailed(ex, sanitizedUrl, ex.GetType().Name);
          throw new Exception($"Unexpected error during HTTP GET for URL: {sanitizedUrl}.", ex);
        }
      }
    }

    /// <inheritdoc />
    public async Task<string?> DoPostAsJson(string url, string requestBodyJsonAsString)
    {
      var sanitizedUrl = SanitizeString(url);
      var requestBodySize = requestBodyJsonAsString?.Length ?? 0;

      using (_logger.BeginHttpOperationScope("POST", sanitizedUrl))
      {
        _logger.LogHttpPost(sanitizedUrl, requestBodySize);

        try
        {
          var response = await _httpRetryPolicy.ExecuteAsync(
              async (context) =>
              {
                var request = await CreateRequestAsync(HttpMethod.Post, url, requestBodyJsonAsString);
                return await _httpClient.SendAsync(request);
              },
              new Context { ["Url"] = sanitizedUrl, ["Method"] = "POST" });

          response.EnsureSuccessStatusCode();
          var result = await GetValidResponse(response);

          _logger.LogHttpPostSuccess(sanitizedUrl, result?.Length ?? 0);

          return result;
        }
        catch (HttpRequestException ex)
        {
          _logger.LogHttpPostFailed(ex, sanitizedUrl, "HttpRequestException");
          throw new Exception($"HTTP POST operation failed for URL: {sanitizedUrl}. Check inner exception for details.", ex);
        }
        catch (TaskCanceledException ex)
        {
          _logger.LogHttpPostFailed(ex, sanitizedUrl, "Timeout");
          throw new Exception($"HTTP POST operation timed out for URL: {sanitizedUrl}.", ex);
        }
        catch (Exception ex)
        {
          _logger.LogHttpPostFailed(ex, sanitizedUrl, ex.GetType().Name);
          throw new Exception($"Unexpected error during HTTP POST for URL: {sanitizedUrl}.", ex);
        }
      }
    }

    /// <inheritdoc />
    public async Task<string?> DoPutAsJson(string url, string requestBodyJsonAsString)
    {
      var sanitizedUrl = SanitizeString(url);
      var requestBodySize = requestBodyJsonAsString?.Length ?? 0;

      using (_logger.BeginHttpOperationScope("PUT", sanitizedUrl))
      {
        _logger.LogHttpPut(sanitizedUrl, requestBodySize);

        try
        {
          var response = await _httpRetryPolicy.ExecuteAsync(
              async (context) =>
              {
                var request = await CreateRequestAsync(HttpMethod.Put, url, requestBodyJsonAsString);
                return await _httpClient.SendAsync(request);
              },
              new Context { ["Url"] = sanitizedUrl, ["Method"] = "PUT" });

          response.EnsureSuccessStatusCode();
          var result = await GetValidResponse(response);

          _logger.LogHttpPutSuccess(sanitizedUrl, result?.Length ?? 0);

          return result;
        }
        catch (HttpRequestException ex)
        {
          _logger.LogHttpPutFailed(ex, sanitizedUrl, "HttpRequestException");
          throw new Exception($"HTTP PUT operation failed for URL: {sanitizedUrl}. Check inner exception for details.", ex);
        }
        catch (TaskCanceledException ex)
        {
          _logger.LogHttpPutFailed(ex, sanitizedUrl, "Timeout");
          throw new Exception($"HTTP PUT operation timed out for URL: {sanitizedUrl}.", ex);
        }
        catch (Exception ex)
        {
          _logger.LogHttpPutFailed(ex, sanitizedUrl, ex.GetType().Name);
          throw new Exception($"Unexpected error during HTTP PUT for URL: {sanitizedUrl}.", ex);
        }
      }
    }

    /// <inheritdoc />
    public async Task<string?> DoPatchAsJson(string url, string requestBodyJsonAsString)
    {
      var sanitizedUrl = SanitizeString(url);
      var requestBodySize = requestBodyJsonAsString?.Length ?? 0;

      using (_logger.BeginHttpOperationScope("PATCH", sanitizedUrl))
      {
        _logger.LogHttpPatch(sanitizedUrl, requestBodySize);

        try
        {
          var response = await _httpRetryPolicy.ExecuteAsync(
              async (context) =>
              {
                var request = await CreateRequestAsync(HttpMethod.Patch, url, requestBodyJsonAsString);
                return await _httpClient.SendAsync(request);
              },
              new Context { ["Url"] = sanitizedUrl, ["Method"] = "PATCH" });

          response.EnsureSuccessStatusCode();
          var result = await GetValidResponse(response);

          _logger.LogHttpPatchSuccess(sanitizedUrl, result?.Length ?? 0);

          return result;
        }
        catch (HttpRequestException ex)
        {
          _logger.LogHttpPatchFailed(ex, sanitizedUrl, "HttpRequestException");
          throw new Exception($"HTTP PATCH operation failed for URL: {sanitizedUrl}. Check inner exception for details.", ex);
        }
        catch (TaskCanceledException ex)
        {
          _logger.LogHttpPatchFailed(ex, sanitizedUrl, "Timeout");
          throw new Exception($"HTTP PATCH operation timed out for URL: {sanitizedUrl}.", ex);
        }
        catch (Exception ex)
        {
          _logger.LogHttpPatchFailed(ex, sanitizedUrl, ex.GetType().Name);
          throw new Exception($"Unexpected error during HTTP PATCH for URL: {sanitizedUrl}.", ex);
        }
      }
    }

    /// <inheritdoc />
    public async Task DoDelete(string url)
    {
      var sanitizedUrl = SanitizeString(url);

      using (_logger.BeginHttpOperationScope("DELETE", sanitizedUrl))
      {
        _logger.LogHttpDelete(sanitizedUrl);

        try
        {
          var response = await _httpRetryPolicy.ExecuteAsync(
              async (context) =>
              {
                var request = await CreateRequestAsync(HttpMethod.Delete, url);
                return await _httpClient.SendAsync(request);
              },
              new Context { ["Url"] = sanitizedUrl, ["Method"] = "DELETE" });

          response.EnsureSuccessStatusCode();

          _logger.LogHttpDeleteSuccess(sanitizedUrl);
        }
        catch (HttpRequestException ex)
        {
          _logger.LogHttpDeleteFailed(ex, sanitizedUrl, "HttpRequestException");
          throw new Exception($"HTTP DELETE operation failed for URL: {sanitizedUrl}", ex);
        }
        catch (TaskCanceledException ex)
        {
          _logger.LogHttpDeleteFailed(ex, sanitizedUrl, "Timeout");
          throw new Exception($"HTTP DELETE operation timed out for URL: {sanitizedUrl}", ex);
        }
        catch (Exception ex)
        {
          _logger.LogHttpDeleteFailed(ex, sanitizedUrl, ex.GetType().Name);
          throw new Exception($"Unexpected error during HTTP DELETE for URL: {sanitizedUrl}", ex);
        }
      }
    }

    #endregion

    #region Access Token Management

    /// <inheritdoc />
    public async Task<string?> GetAccessToken(string url)
    {
      var baseUrl = GetBaseUrl(url);

      try
      {
        _logger.LogAccessTokenRequest(baseUrl);

        var azureServiceTokenProvider = new AzureServiceTokenProvider();
        var token = await azureServiceTokenProvider.GetAccessTokenAsync(baseUrl);

        _logger.LogAccessTokenRetrieved(baseUrl);

        return token;
      }
      catch (Exception ex)
      {
        _logger.LogAccessTokenFailed(ex, baseUrl);
        throw;
      }
    }

    #endregion

    #region Private Helper Methods

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, string? content = null)
    {
      var request = new HttpRequestMessage(method, url);
      var token = await GetAccessToken(url);
      request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      if (content != null)
      {
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");
      }

      return request;
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
        int remainingQuota = int.Parse(quotaRemainingValues.FirstOrDefault()!);
        _logger.LogQuotaRemaining(remainingQuota);

        if (remainingQuota == 0 && responseHeaders.TryGetValues("x-ms-user-quota-resets-after", out IEnumerable<string>? resetsAfterValues))
        {
          TimeSpan resetsAfter = TimeSpan.Parse(resetsAfterValues.FirstOrDefault()!);
          _logger.LogQuotaResetsAfter(resetsAfter);

          // Delay by a random period to avoid bursting when the quota is reset.
          // Follow guidance https://docs.microsoft.com/en-us/azure/governance/resource-graph/concepts/guidance-for-throttled-requests#query-in-parallel
          var delay = (new Random()).Next(1, 5) * resetsAfter;
          return delay;
        }
      }

      var retryAfter = responseHeaders?.RetryAfter;
      if (retryAfter == null)
      {
        _logger.LogRetryAfterHeaderNull();
        return TimeSpan.Zero;
      }

      var waitDuration = retryAfter.Date.HasValue
          ? retryAfter.Date.Value - DateTime.UtcNow
          : retryAfter.Delta.GetValueOrDefault(TimeSpan.Zero);

      _logger.LogServerWaitDuration(waitDuration);
      return waitDuration;
    }

    internal string GetBaseUrl(string url)
    {
      string[] urlParts = url.Split('/');
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(urlParts[0]);
      stringBuilder.Append("//");
      stringBuilder.Append(urlParts[2]);
      stringBuilder.Append('/');
      return stringBuilder.ToString();
    }

    internal static string SanitizeString(string url)
    {
      var sanitizedUrl = url.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
      return sanitizedUrl;
    }

    #endregion
  }
}