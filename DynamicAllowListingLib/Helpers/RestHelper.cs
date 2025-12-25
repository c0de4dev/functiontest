using Azure.Core;
using Azure.Identity;
using DynamicAllowListingLib.Logging;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private AccessToken _cachedToken;

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
          throw new Exception($"HTTP GET request failed for URL: {sanitizedUrl}", ex);
        }
        catch (TaskCanceledException ex)
        {
          _logger.LogHttpGetFailed(ex, sanitizedUrl, "Timeout");
          throw new Exception($"HTTP GET request timed out for URL: {sanitizedUrl}", ex);
        }
        catch (Exception ex)
        {
          _logger.LogHttpGetFailed(ex, sanitizedUrl, ex.GetType().Name);
          throw new Exception($"Unexpected error during HTTP GET for URL: {sanitizedUrl}", ex);
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
          throw new Exception($"HTTP POST request failed for URL: {sanitizedUrl}. Check inner exception for details.", ex);
        }
        catch (TaskCanceledException ex)
        {
          _logger.LogHttpPostFailed(ex, sanitizedUrl, "Timeout");
          throw new Exception($"HTTP POST request timed out for URL: {sanitizedUrl}.", ex);
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
          throw new Exception($"HTTP PATCH operation failed for URL: {sanitizedUrl}. See inner exception for details.", ex);
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
        // Check if the cached token is still valid
        if (_cachedToken.Token != null && _cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
        {
          var expiresInMinutes = (_cachedToken.ExpiresOn - DateTimeOffset.UtcNow).TotalMinutes;
          _logger.LogAccessTokenCached(expiresInMinutes);
          return _cachedToken.Token;
        }

        _logger.LogAccessTokenRequest(baseUrl);

        // Retrieve new access token using DefaultAzureCredential
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(new[] { $"{baseUrl}/.default" });
        _cachedToken = await credential.GetTokenAsync(tokenRequestContext);

        _logger.LogAccessTokenRefreshed(_cachedToken.ExpiresOn);

        return _cachedToken.Token;
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
      var sanitizedUrl = SanitizeString(url);
      var request = new HttpRequestMessage(method, url);

      _logger.LogHttpRequestCreation(method.Method, sanitizedUrl);

      var token = await GetAccessToken(url);
      request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      if (content != null)
      {
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");
        _logger.LogHttpRequestCreatedWithContent(method.Method, sanitizedUrl, content.Length);
      }

      return request;
    }

    private static async Task<string> GetValidResponse(HttpResponseMessage response)
    {
      var content = await response.Content.ReadAsStringAsync();
      return content;
    }

    private static bool ShouldRetryRequest(HttpResponseMessage response)
    {
      return response.StatusCode == HttpStatusCode.TooManyRequests ||
             response.StatusCode == HttpStatusCode.ServiceUnavailable ||
             response.StatusCode == HttpStatusCode.GatewayTimeout ||
             response.StatusCode == HttpStatusCode.RequestTimeout ||
             (int)response.StatusCode >= 500;
    }

    private static TimeSpan GetServerWaitDuration(DelegateResult<HttpResponseMessage> response)
    {
      if (response.Result?.Headers.RetryAfter != null)
      {
        if (response.Result.Headers.RetryAfter.Delta.HasValue)
        {
          return response.Result.Headers.RetryAfter.Delta.Value;
        }

        if (response.Result.Headers.RetryAfter.Date.HasValue)
        {
          return response.Result.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
        }
      }

      return TimeSpan.Zero;
    }

    private static string GetBaseUrl(string url)
    {
      var uri = new Uri(url);
      return $"{uri.Scheme}://{uri.Host}";
    }

    private static string SanitizeString(string input)
    {
      if (string.IsNullOrEmpty(input))
        return string.Empty;

      return input
          .Replace(Environment.NewLine, "")
          .Replace("\n", "")
          .Replace("\r", "");
    }

    #endregion
  }
}