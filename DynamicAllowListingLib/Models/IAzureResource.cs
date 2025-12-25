using DynamicAllowListingLib.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Models
{
  public interface IAzureResource
  {
    string? Type { get; }
    string? Id { get; }
    string? Name { get; set; }
    string? Location { get; set; }

    /// <summary>
    /// Properties will be mapped to Management Api response
    /// </summary>
    object? Properties { get; set; }
    bool PrintOut { get; set; }

    IEnumerable<IpSecurityRestrictionRule> GenerateIpRestrictionRules(ILogger logger);

    Task<ResultObject> AppendNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger, IRestHelper restHelper);

    Task<ResultObject> OverWriteNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger, IRestHelper restHelper);
    (string, string) ConvertRulesToPrintOut(NetworkRestrictionSettings networkRestrictionSettings);
  }

  public class PublicIpAddress : IAzureResource
  {
    public string Type => AzureResourceType.PublicIpAddress;
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public bool PrintOut { get; set; } = false;
    public object? Properties { get => Props!; set => this.Props = (PublicIpAddressProp)value!; }

    //custom properties to be cast
    public PublicIpAddressProp? Props { get; set; } = new PublicIpAddressProp();

    public class PublicIpAddressProp
    {
      public string? IpAddress { get; set; }
    }
    public IEnumerable<IpSecurityRestrictionRule> GenerateIpRestrictionRules(ILogger logger)
    {
      return IpRulesHelper.GenerateDynamicAllowListingRules(Name!, Props?.IpAddress!);
    }
    public Task<ResultObject> AppendNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger,
      IRestHelper restHelper)
    {
      throw new NotImplementedException();
    }
    public Task<ResultObject> OverWriteNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger,
      IRestHelper restHelper)
    {
      throw new NotImplementedException();
    }
    public (string, string) ConvertRulesToPrintOut(NetworkRestrictionSettings networkRestrictionSettings)
    {
      throw new NotImplementedException();
    }
  }

  public class FrontDoor : PublicIpAddress, IAzureResource
  {
    public new string? Type => AzureResourceType.FrontDoor;
  }

  public class AzureResourceType
  {
    public const string
      WebSite = "microsoft.web/sites",
      WebSiteSlot = "microsoft.web/sites/slots",
      PublicIpAddress = "microsoft.network/publicipaddresses",
      CosmosDb = "microsoft.documentdb/databaseaccounts",
      Storage = "microsoft.storage/storageaccounts",
      KeyVault = "microsoft.keyvault/vaults",
      SqlServer = "microsoft.sql/servers",
      FrontDoor = "microsoft.network/frontdoors";
  }
}