using System.Threading.Tasks;

namespace DynamicAllowListingLib
{
  public interface IRestHelper
  {
    /// <summary>
    /// Send a GET request to the specified url.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string> DoGET(string url);

    /// <summary>
    /// Send a PUT request to the specified url.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <param name="requestBodyJsonAsString">The request body JSON as string.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string> DoPutAsJson(string url, string requestBodyJsonAsString);

    /// <summary>
    /// Send a POST request to the specified url.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <param name="requestBodyJsonAsString">The request body JSON as string.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string> DoPostAsJson(string url, string requestBodyJsonAsString);

    /// <summary>
    /// Send a PATCH request to the specified url.
    /// </summary>
    /// <param name="url">URL to send request to.</param>
    /// <param name="requestBodyJsonAsString">The request body JSON as string.</param>
    /// <returns>The response JSON as string.</returns>
    Task<string> DoPatchAsJson(string url, string requestBodyJsonAsString);

    Task<string?> GetAccessToken(string url);
    Task DoDelete(string firewalUrl);
  }
}