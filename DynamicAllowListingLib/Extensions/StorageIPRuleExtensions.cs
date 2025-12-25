using Microsoft.Azure.Management.Storage.Models;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Extensions
{
  public static class StorageIPRuleExtensions
  {
    /// <summary>
    /// Create new list with unique items from the merge
    /// </summary>
    /// <param name="source"></param>
    /// <param name="listToMerge"></param>
    /// <returns>Unique list of <see cref="IPRule"/> items</returns>
    public static IList<IPRule> MergeIntoUniqueList(this IList<IPRule> source, IList<IPRule> listToMerge)
    {
      var uniqueList = new List<IPRule>(source);
      foreach (var item in listToMerge)
      {
        if (!source.ContainsIPAddressOrRange(item))
        {
          uniqueList.Add(item);
        }
      }
      return uniqueList;
    }

    public static bool ContainsIPAddressOrRange(this IList<IPRule> source, IPRule iPRule)
    {
      foreach (var item in source)
      {
        if (item.IPAddressOrRange.Equals(iPRule.IPAddressOrRange))
        {
          return true;
        }
      }
      return false;
    }
  }
}
