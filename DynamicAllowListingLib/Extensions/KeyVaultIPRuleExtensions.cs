using System;
using System.Collections.Generic;
using System.Text;
using static DynamicAllowListingLib.Models.AzureResources.KeyVault;

namespace DynamicAllowListingLib.Extensions
{
  public static class KeyVaultIPRuleExtensions
  {
    /// <summary>
    /// Create new list with unique items from the merge
    /// </summary>
    /// <param name="source"></param>
    /// <param name="listToMerge"></param>
    /// <returns>Unique list of <see cref="IpAddressOrRange"/> items</returns>
    public static IList<IpAddressOrRange> MergeIntoUniqueList(this IList<IpAddressOrRange> source, IList<IpAddressOrRange> listToMerge)
    {
      var uniqueList = new List<IpAddressOrRange>(source);
      foreach (var item in listToMerge)
      {
        if (!source.ContainsIpAddressOrRange(item))
        {
          uniqueList.Add(item);
        }
      }
      return uniqueList;
    }

    public static bool ContainsIpAddressOrRange(this IList<IpAddressOrRange> source, IpAddressOrRange iPRule)
    {
      foreach (var item in source)
      {
        if (item.value!.Equals(iPRule.value))
        {
          return true;
        }
      }
      return false;
    }
  }
}
