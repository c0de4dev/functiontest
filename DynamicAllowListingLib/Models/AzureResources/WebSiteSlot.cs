namespace DynamicAllowListingLib.Models.AzureResources
{
  public class WebSiteSlot : WebSite, IAzureResource
  {
    public override string Type => AzureResourceType.WebSiteSlot;
  }
}
