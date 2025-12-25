using System;
using System.Threading;

namespace DynamicAllowListingLib.Logging
{
  public static class CorrelationContext
  {
    private static readonly AsyncLocal<string?> _correlationId = new AsyncLocal<string?>();

    public static string CorrelationId
    {
      get => _correlationId.Value ?? GenerateCorrelationId();
      set => _correlationId.Value = value;
    }

    private static string GenerateCorrelationId()
    {
      var newId = Guid.NewGuid().ToString();
      _correlationId.Value = newId;
      return newId;
    }

    public static void Clear()
    {
      _correlationId.Value = null;
    }
  }
}