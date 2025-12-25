using Microsoft.Azure.Cosmos;

namespace DynamicAllowListingLib.Helpers
{
  public static class CosmosHelper
  {
    public static CosmosClientOptions GetDefaultCosmosClientOptions() => new CosmosClientOptions()
    {
      // We avoid using direct mode due to a known bug
      // refer https://github.com/vercel/cosmosdb-server/issues/29#issuecomment-751289730
      ConnectionMode = ConnectionMode.Gateway
    };
  }
}
