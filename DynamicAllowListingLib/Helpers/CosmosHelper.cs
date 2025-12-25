using Microsoft.Azure.Cosmos;

namespace DynamicAllowListingLib.Helpers
{
  /// <summary>
  /// Helper class for Cosmos DB configuration.
  /// </summary>
  public static class CosmosHelper
  {
    /// <summary>
    /// Gets the default Cosmos DB client options.
    /// Uses Gateway mode to avoid known issues with direct mode.
    /// </summary>
    /// <returns>Configured CosmosClientOptions.</returns>
    /// <remarks>
    /// We avoid using direct mode due to a known bug.
    /// Refer to: https://github.com/vercel/cosmosdb-server/issues/29#issuecomment-751289730
    /// </remarks>
    public static CosmosClientOptions GetDefaultCosmosClientOptions() => new CosmosClientOptions()
    {
      ConnectionMode = ConnectionMode.Gateway
    };
  }
}