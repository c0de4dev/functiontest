using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.ServiceTagManagers
{
  public enum ManagerType
  {
    Azure,
    AzureWeb, //ServiceTags are not supported by the resources like Storage Account and Cosmos Db. Use this tag for Service Tag Supported resources
    Aws,
    NewDay
  }

  public interface IServiceTagManager
  {
    public ManagerType SupportedManager { get; }

    public Task<bool> IsServiceTagExists(string serviceTagName, string requestedSubscriptionId);

    public Task<bool> IsServiceTagExists(string serviceTagName);

    /// <summary>
    /// Get restriction rules for given service tag names. Optionally exclude mandatory rules.
    /// </summary>
    /// <param name="subscriptionId">Rules are applicable within the scope of this azure subscription.</param>
    /// <param name="serviceTags">Names of service tags.</param>
    /// <param name="includeMandatoryRulesForSubscription">True by default. If false, only gets rules for requested tag names.</param>
    /// <returns></returns>
    public Task<HashSet<IpSecurityRestrictionRule>> GenerateRulesByName(string subscriptionId, string[] serviceTags, bool includeMandatoryRulesForSubscription = true);
  }
}