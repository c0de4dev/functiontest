using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DynamicAllowListingLib
{
  public static class StringHelper
  {
    public static string Truncate(string str, int length = 28)
    {
      int maxLength = Math.Min(str.Length, length);
      return str.Substring(0, maxLength);
    }
    public static string GetSubscriptionId(string resourceId)
    {
      string[] idParts = resourceId.Split('/');
      return idParts[2];
    }
    public static string[] GetSubscriptionIds(string[] resourceIds)
    {
      var sublist = new List<string>();
      foreach (var resourceId in resourceIds)
      {
        var sections = resourceId.Split('/');
        if (sections.Length < 2)
        {
          throw new InvalidDataException($"provided resourceId is not correctly formated! resourceId: {resourceId}");
        }
        sublist.Add(sections[2]);
      }
      return sublist.Distinct().ToArray();
    }
    public static string GetResourceName(string resourceId) => Path.GetFileName(resourceId);
  }
}