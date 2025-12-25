using System.Threading.Tasks;

namespace DynamicAllowListingLib
{
  /// <summary>
  /// Interface for REST API helper operations.
  /// </summary>
  public interface IRestHelper
  {
    /// <summary>
    /// Sends a GET request to the specified URL.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string?> DoGET(string url);

    /// <summary>
    /// Sends a PUT request to the specified URL.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <param name="requestBodyJsonAsString">The request body JSON as string.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string?> DoPutAsJson(string url, string requestBodyJsonAsString);

    /// <summary>
    /// Sends a POST request to the specified URL.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <param name="requestBodyJsonAsString">The request body JSON as string.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string?> DoPostAsJson(string url, string requestBodyJsonAsString);

    /// <summary>
    /// Sends a PATCH request to the specified URL.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <param name="requestBodyJsonAsString">The request body JSON as string.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string?> DoPatchAsJson(string url, string requestBodyJsonAsString);

    /// <summary>
    /// Retrieves an access token for the specified URL.
    /// </summary>
    /// <param name="url">The URL to get the access token for.</param>
    /// <returns>The access token.</returns>
    Task<string?> GetAccessToken(string url);

    /// <summary>
    /// Sends a DELETE request to the specified URL.
    /// </summary>
    /// <param name="url">URL to send the DELETE request to.</param>
    Task DoDelete(string url);
  }
}