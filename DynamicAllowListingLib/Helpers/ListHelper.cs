using System.Collections.Generic;
using System.Linq;

namespace DynamicAllowListingLib
{
  /// <summary>
  /// Generic list comparison and search utilities.
  /// </summary>
  public static class ListHelper<T>
  {
    /// <summary>
    /// Checks if two lists are of equal length and contain equal values.
    /// The order of items in the list matters.
    /// </summary>
    /// <param name="firstList">First list to compare.</param>
    /// <param name="secondList">Second list to compare.</param>
    /// <returns>True if equal, otherwise false.</returns>
    public static bool AreEqual(List<T> firstList, List<T> secondList)
    {
      if (firstList == null || secondList == null)
        return firstList == secondList;

      if (firstList.Count != secondList.Count)
        return false;

      return !firstList.Where((t, index) => !t!.Equals(secondList[index])).Any();
    }

    /// <summary>
    /// Checks if any item in the list contains the specified substring.
    /// </summary>
    /// <param name="listOfItems">List of strings to search.</param>
    /// <param name="itemToFind">Substring to find.</param>
    /// <returns>True if any item contains the substring, otherwise false.</returns>
    public static bool HasItemContaining(List<string> listOfItems, string itemToFind)
    {
      if (listOfItems == null || string.IsNullOrEmpty(itemToFind))
        return false;

      return listOfItems.Any(item => item != null && item.Contains(itemToFind));
    }
  }
}