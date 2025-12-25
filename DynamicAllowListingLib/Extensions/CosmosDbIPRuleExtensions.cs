using Microsoft.Azure.Management.CosmosDB.Models;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Extensions
{
  public static class CosmosDbIPRuleExtensions
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
        if (item.IpAddressOrRangeProperty.Equals(iPRule.IpAddressOrRangeProperty))
        {
          return true;
        }
      }
      return false;
    }
  }
}
