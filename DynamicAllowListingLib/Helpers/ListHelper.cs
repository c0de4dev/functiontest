using System.Collections.Generic;
using System.Linq;

namespace DynamicAllowListingLib
{
  public static class ListHelper<T>
  {
    /// <summary>
    /// Check if <see cref="List{T}"/> are of equal length and contain equal values. The order of items in list do not matter.
    /// </summary>
    /// <param name="firstList"></param>
    /// <param name="secondList"></param>
    /// <returns>True if equal, else false.</returns>
    public static bool AreEqual(List<T> firstList, List<T> secondList) 
      => firstList.Count == secondList.Count && !firstList.Where((t, index) => !t!.Equals(secondList[index])).Any();

    public static bool HasItemContaining(List<string> listOfItems, string itemToFind) 
      => listOfItems.Any(item => item.Contains(itemToFind));
  }
}