namespace DynamicAllowListingLib.Models.ResourceGraphResponses
{
  public class ResourceIds
  {
    public Datum[] data { get; set; } = null!;
    public class Datum
    {
      public string id { get; set; } = null!;
    }
  }
  public class FrontDoorGraphResult
  {
    public Datum[] data { get; set; } = null!;
    public class Datum
    {
      public string id { get; set; } = null!;
      public string FDID { get; set; } = null!;
    }
  }
}
