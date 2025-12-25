using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Services
{
  public class AzureServiceTagNotFoundException : Exception
  {
    private readonly List<string> _serviceTags;
    public AzureServiceTagNotFoundException(IEnumerable<string> serviceTags)
    {
      _serviceTags = new List<string>(serviceTags);
    }

    public override string Message
    {
      get
      {
        string message = "The following Azure service tags could not be found: " + string.Join(",", _serviceTags);
        return message;
      }
    }
  }
}