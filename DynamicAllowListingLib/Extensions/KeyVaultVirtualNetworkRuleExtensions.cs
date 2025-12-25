using System;
using System.Collections.Generic;
using System.Text;
using static DynamicAllowListingLib.Models.AzureResources.KeyVault;

namespace DynamicAllowListingLib.Extensions
{
  public static class KeyVaultVirtualNetworkRuleExtensions
  {
    /// <summary>
    /// Create new list with unique items from the merge
    /// </summary>
    /// <param name="source"></param>
    /// <param name="listToMerge"></param>
    /// <returns>Unique list of <see cref="VirtualNetworkRule"/> items</returns>
    public static IList<VirtualNetworkRule> MergeIntoUniqueList(this IList<VirtualNetworkRule> source, IList<VirtualNetworkRule> listToMerge)
    {
      var uniqueList = new List<VirtualNetworkRule>(source);
      foreach (var item in listToMerge)
      {
        if (!source.ContainsVirtualNetworkRule(item))
        {
          uniqueList.Add(item);
        }
      }
      return uniqueList;
    }

    public static bool ContainsVirtualNetworkRule(this IList<VirtualNetworkRule> source, VirtualNetworkRule VirtualNetworkRule)
    {
      foreach (var item in source)
      {
        if (item.Id.Equals(VirtualNetworkRule.Id))
        {
          return true;
        }
      }
      return false;
    }
  }
}
