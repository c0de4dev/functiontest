using DynamicAllowListingLib.Helpers;
using DynamicAllowListingLib.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DynamicAllowListingLib.Models.AzureResources.SqlServer.SqlProperties;

namespace DynamicAllowListingLib.Models.AzureResources
{
  public class SqlServer : IAzureResource
  {

    public const string ResourceIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.Sql/servers\\/([a-zA-Z0-9-]+)";
    public const int IpRulesLimit = 128;
    public string Type => AzureResourceType.SqlServer;
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }

    public bool PrintOut { get; set; } = false;

    //custom properties to be cast
    private SqlProperties Properties { get; set; } = new SqlProperties();

    object? IAzureResource.Properties
    {
      get => Properties!;
      set => Properties = (SqlProperties)value!;
    }

    public class SqlProperties

    {
      [JsonProperty(PropertyName = "publicNetworkAccess")]
      public string PublicNetworkAccess { get; set; } = String.Empty;

      public List<SqlFirewallRule> FirewallRules { get; set; } = new List<SqlFirewallRule>();

      public List<SqlVirtualNetworkRule> VNetRules { get; set; } = new List<SqlVirtualNetworkRule>();

      public class SqlFirewallRule
      {

        public string? Name { get; set; }
        public SqlFirewallRuleProperties Properties { get; set; } = new SqlFirewallRuleProperties();

        public class SqlFirewallRuleProperties
        {

          [JsonProperty(PropertyName = "startIpAddress")]
          public string? StartIPAddress { get; set; }

          [JsonProperty(PropertyName = "endIpAddress")]
          public string? EndIPAddress { get; set; }
        }
      }

      public class SqlVirtualNetworkRule
      {

        public string? Name { get; set; }
        public SqlVirtualNetworkRuleProperties Properties { get; set; } = new SqlVirtualNetworkRuleProperties();

        public class SqlVirtualNetworkRuleProperties
        {
          [JsonProperty(PropertyName = "virtualNetworkSubnetId")]
          public string? SubnetId { get; set; }
        }

      }
    }

    public async Task<ResultObject> AppendNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger, IRestHelper restHelper)
    {
      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: false);
    }

    public IEnumerable<IpSecurityRestrictionRule> GenerateIpRestrictionRules(ILogger logger)
    {
      throw new InvalidOperationException("SQL Server type cannot GenerateIpRestrictionRules for itself.");
    }

    public async Task<ResultObject> OverWriteNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger, IRestHelper restHelper)
    {
      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: true);
    }

    internal async Task<ResultObject> ApplyConfig(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger, IRestHelper restHelper, bool overwrite)
    {
      var resultObject = new ResultObject();

      //check if generated ip rules exceeds the limit 128
      if (!(await IsValid(resultObject, networkRestrictionSettings)))
        return resultObject;

      //generate desired rules
      var generatedRules = GenerateRules(networkRestrictionSettings);

      //do not apply if just print 
      if (PrintOut)
      {
        resultObject.Data = new ResultObject.OutputData();
        resultObject.Data.IPs = FormatIpRules(generatedRules.FirewallRules);
        resultObject.Data.SubnetIds = string.Join(',', generatedRules.VNetRules.Select(x => x.Properties.SubnetId));
        return resultObject;
      }

      //set existing rules
      var existingRules = await GetExistingRules(restHelper, logger);

      await DisablePublicAccess(restHelper);
      await CreateFwRulesIfNotExists(existingRules, generatedRules, restHelper, logger, resultObject);
      await CreateVnetRuleIfNotExistss(existingRules, generatedRules, restHelper, logger, resultObject);

      if (overwrite)
      {
        await DeleteLeftOverFwRules(existingRules, generatedRules, restHelper, resultObject);
        await DeleteLeftOverVnetRules(existingRules, generatedRules, restHelper, resultObject);
      }

      return resultObject;
    }

    private string? FormatIpRules(List<SqlFirewallRule> firewallRules)
    {
      if (firewallRules == null || firewallRules.Count <= 0)
        return string.Empty;

      var jsonFormattedRules = new List<JsonFormattedIpRule>();
      foreach (var rule in firewallRules)
      {
        jsonFormattedRules.Add(new JsonFormattedIpRule
        {
          RuleName = rule.Name,
          StartIPAddress = rule.Properties.StartIPAddress,
          EndIPAddress = rule.Properties.EndIPAddress
        });
      }

      return JsonConvert.SerializeObject(jsonFormattedRules, Formatting.Indented);
    }

    private async Task DeleteLeftOverVnetRules(SqlProperties existingRules, SqlProperties generatedRules, IRestHelper restHelper, ResultObject resultObject)
    {
      //Delete leftover rules
      var leftoverVnetRules = existingRules.VNetRules.Where(x =>
        !generatedRules.VNetRules.Any(e => e.Properties.SubnetId == x.Properties.SubnetId));

      foreach (var vnetRule in leftoverVnetRules)
      {
        try
        {
          var firewalUrl = $"https://management.azure.com{Id}/virtualNetworkRules/{vnetRule.Name}?api-version=2021-11-01";
          await restHelper.DoDelete(firewalUrl);
        }
        catch (Exception)
        {
          resultObject.Warnings.Add($"Firewall IP Rule could not be deleted! Please be sure the delete lock on resource group removed before operation! RuleName: {vnetRule.Name}.");
          //do not try to delete others. warn and continue.
          continue;
        }
      }
    }

    private async Task DeleteLeftOverFwRules(SqlProperties existingRules, SqlProperties generatedRules, IRestHelper restHelper, ResultObject resultObject)
    {
      //Delete undesired rules.
      var leftoverFwRules = existingRules.FirewallRules.Where(x =>
        !generatedRules.FirewallRules.Any(e => e.Properties.StartIPAddress == x.Properties.StartIPAddress) ||
        !generatedRules.FirewallRules.Any(e => e.Properties.EndIPAddress == x.Properties.EndIPAddress));

      foreach (var fwRule in leftoverFwRules)
      {
        try
        {
          var firewalUrl = $"https://management.azure.com{Id}/firewallRules/{fwRule.Name}?api-version=2021-11-01";
          await restHelper.DoDelete(firewalUrl);
        }
        catch (Exception)
        {
          resultObject.Warnings.Add($"Firewall IP Rules could not be deleted! Please be sure delete lock on resource group removed before operation! RuleName: {fwRule.Name}.");
          //do not try to delete others. warn and continue.
          break;
        }
      }
    }

    private async Task DisablePublicAccess(IRestHelper restHelper)
    {
      if (this.Properties.PublicNetworkAccess == "Enabled")
        return; 

      var vnetUrl = $"https://management.azure.com{Id}?api-version=2021-11-01";
      var requestModel = new SqlServerRequest(Location!);
      var requestJsonModel = JsonConvert.SerializeObject(requestModel, Formatting.Indented);
      await restHelper.DoPutAsJson(vnetUrl, requestJsonModel);
    }

    private async Task CreateVnetRuleIfNotExistss(SqlProperties existingRules, SqlProperties generatedRules, IRestHelper restHelper, ILogger logger, ResultObject resultObject)
    {
      var addedVnetRules = new List<string>();
      foreach (var vnetRule in generatedRules.VNetRules)
      {
        //add if FW rule doesn't exists
        if (!existingRules.VNetRules.Any(x => x.Properties.SubnetId == vnetRule.Properties.SubnetId))
        {
          try
          {
            var vnetUrl = $"https://management.azure.com{Id}/virtualNetworkRules/{vnetRule.Name}?api-version=2021-11-01";
            var requestJsonModel = JsonConvert.SerializeObject(vnetRule, Formatting.Indented);
            var result = await restHelper.DoPutAsJson(vnetUrl, requestJsonModel);
            addedVnetRules.Add(vnetRule.Properties.SubnetId!);
          }
          catch (Exception ex)
          {
            resultObject.Errors.Add($"Subnet rule can't be added to the SQL Firewall! Error Message: {ex.Message}");
            throw;
          }
        }
      }
      logger.LogInformation($"{addedVnetRules.Count} Subnet rules added. Rules:{string.Join("," + Environment.NewLine, addedVnetRules)}");
    }

    private async Task CreateFwRulesIfNotExists(SqlProperties existingRules, SqlProperties generatedRules, IRestHelper restHelper, ILogger logger, ResultObject resultObject)
    {
      var addedFwRules = new List<string>();
      foreach (var fwRule in generatedRules.FirewallRules)
      {
        //add if subnet doesn't exists
        if (!existingRules.FirewallRules.Any(x =>
            x.Properties.StartIPAddress == fwRule.Properties.StartIPAddress &&
            x.Properties.EndIPAddress == fwRule.Properties.EndIPAddress))
        {
          //add rule
          try
          {
            var firewalUrl = $"https://management.azure.com{Id}/firewallRules/{fwRule.Name}?api-version=2021-11-01";
            var requestJsonModel = JsonConvert.SerializeObject(fwRule, Formatting.Indented);
            var result = await restHelper.DoPutAsJson(firewalUrl, requestJsonModel);
            addedFwRules.Add($"{fwRule.Properties.StartIPAddress}-{fwRule.Properties.EndIPAddress}");
          }
          catch (Exception ex)
          {
            resultObject.Errors.Add($"IP Rule can not be added to the SQL Firewall! Error Message: {ex.Message}");
            throw;
          }
        }        
      }
      logger.LogInformation($"{addedFwRules.Count} Firewall rules added. Rules:{string.Join("," + Environment.NewLine, addedFwRules)}");
    }


    private Task<bool> IsValid(ResultObject resultObject, NetworkRestrictionSettings rules)
    {
      if (rules.IpSecRules == null || rules.IpSecRules.Count <= IpRulesLimit)
        return Task.FromResult(true);

      resultObject.Errors.Add($"The maximum number of server-level IP firewall rules is limited to 128! Generated IP firewall rules: {rules.IpSecRules.Count}.");
      return Task.FromResult(false);
    }

    internal async Task<SqlProperties> GetExistingRules(IRestHelper restHelper, ILogger logger)
    {
      var properties = new SqlProperties();

      string firewallurl = $"https://management.azure.com{Id}/firewallRules?api-version=2021-11-01";
      string? firewallResponse = await restHelper.DoGET(firewallurl);
      SqlServerFirewallRulesResponse? existingFirewallRules = null;
      if (!string.IsNullOrEmpty(firewallResponse))
      {
        existingFirewallRules = JsonConvert.DeserializeObject<SqlServerFirewallRulesResponse>(firewallResponse);
      }
      
      // Validate firewall rules existence
      if (existingFirewallRules?.Value == null)
      {
        logger.LogWarning($"No firewall rules found for resource {Id}. Returning default value.");
      }
      else
      {
        properties.FirewallRules = existingFirewallRules.Value;
      }

      string vneturl = $"https://management.azure.com{Id}/virtualNetworkRules?api-version=2021-11-01";
      string? vnetResponse = await restHelper.DoGET(vneturl);
      SqlServerVNetRulesResponse? existingVNetRules = null;
      if (!string.IsNullOrEmpty(vnetResponse))
      {
        existingVNetRules = JsonConvert.DeserializeObject<SqlServerVNetRulesResponse>(vnetResponse);
      }

      // Validate virtual network rules existence
      // Validate virtual network rules existence
      if (existingVNetRules?.Value == null)
      {
        logger.LogWarning($"No virtual network rules found for resource {Id}. Returning default value.");
      }
      else
      {
        properties.VNetRules = existingVNetRules.Value;
      }
      return properties;
    }

    internal SqlProperties GenerateRules(NetworkRestrictionSettings networkRestrictionSettings)
    {
      var properties = new SqlProperties();
      properties.FirewallRules = GenerateFirewallRules(networkRestrictionSettings);
      properties.VNetRules = GenerateVNetRules(networkRestrictionSettings);
      return properties;
    }

    private List<SqlVirtualNetworkRule> GenerateVNetRules(NetworkRestrictionSettings networkRestrictionSettings)
    {
      var list = new List<SqlVirtualNetworkRule>();
      foreach (var vnetRule in networkRestrictionSettings.IpSecRules!.Where(x => x.VnetSubnetResourceId != null).ToList())
      {
        var generateRule = new SqlVirtualNetworkRule();
        generateRule.Name = vnetRule.Name;
        generateRule.Properties.SubnetId = vnetRule.VnetSubnetResourceId;
        list.Add(generateRule);
      }
      return list;
    }

    private List<SqlFirewallRule> GenerateFirewallRules(NetworkRestrictionSettings networkRestrictionSettings)
    {
      var list = new List<SqlFirewallRule>();
      foreach (var rule in networkRestrictionSettings.IpSecRules!.Where(x => x.IpAddress != null).ToList())
      {

        var range = CalculateIPRange(rule.IpAddress!);
        var generateRule = new SqlFirewallRule();
        generateRule.Name = rule.Name;
        generateRule.Properties!.StartIPAddress = range.Item1;
        generateRule.Properties!.EndIPAddress = range.Item2;
        list.Add(generateRule);
      }
      return list;
    }

    public (string, string) ConvertRulesToPrintOut(NetworkRestrictionSettings networkRestrictionSettings)
    {
      //generate range for each rule
      string IPRules = string.Empty;
      string SubnetIds = string.Empty;
      var jsonFormattedRules = new List<JsonFormattedIpRule>();

      foreach (var rule in networkRestrictionSettings.IpSecRules!)
      {
        if (rule.IpAddress != null)
        {
          var range = CalculateIPRange(rule.IpAddress!);
          jsonFormattedRules.Add(new JsonFormattedIpRule
          {
            RuleName = rule.Name,
            StartIPAddress = range.Item1,
            EndIPAddress = range.Item2
          });         
        }
        if (rule.VnetSubnetResourceId != null)
        {
          SubnetIds += $"{rule.VnetSubnetResourceId},";
        }
      }
      IPRules = JsonConvert.SerializeObject(jsonFormattedRules, Formatting.Indented);
      SubnetIds = SubnetIds.TrimEnd(',');
       
      return (IPRules, SubnetIds);
    }

    private (string, string) CalculateIPRange(string cidrRange)
    {
      if (!cidrRange.Contains("/"))
        cidrRange = cidrRange + "/32";

      string[] parts = cidrRange.Split('.', '/');

      uint ipnum = (Convert.ToUInt32(parts[0]) << 24) |
        (Convert.ToUInt32(parts[1]) << 16) |
        (Convert.ToUInt32(parts[2]) << 8) |
        Convert.ToUInt32(parts[3]);

      int maskbits = Convert.ToInt32(parts[4]);
      uint mask = 0xffffffff;
      mask <<= (32 - maskbits);

      uint ipstart = ipnum & mask;
      uint ipend = ipnum | (mask ^ 0xffffffff);

      return (Toip(ipstart), Toip(ipend));
    }

    static string Toip(uint ip)
    {
      return String.Format("{0}.{1}.{2}.{3}", ip >> 24, (ip >> 16) & 0xff, (ip >> 8) & 0xff, ip & 0xff);
    }
  }

  public class SqlServerFirewallRulesResponse
  {
    public List<SqlFirewallRule> Value { get; set; } = new List<SqlFirewallRule>();
  }
  public class SqlServerVNetRulesResponse
  {
    public List<SqlVirtualNetworkRule> Value { get; set; } = new List<SqlVirtualNetworkRule>();
  }

  public class SqlServerRequest
  {
    public SqlServerRequest(string location)
    {
      this.Location = location;
    }
    public string Location { get; set; }
    public RequestProperties Properties { get; set; } = new RequestProperties();
    public class RequestProperties
    {
      public string PublicNetworkAccess { get; set; } = "Enabled";
    }
  }


  public class JsonFormattedIpRule
  {
    [JsonProperty(PropertyName = "rule_name")]
    public string? RuleName { get; set; }

    [JsonProperty(PropertyName = "start_ip_address")]
    public string? StartIPAddress { get; set; }

    [JsonProperty(PropertyName = "end_ip_address")]
    public string? EndIPAddress { get; set; }
  }

}

