using DynamicAllowListingLib.Extensions;
using DynamicAllowListingLib.Helpers;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Models.AzureResources
{
  public class Storage : IAzureResource
  {
    public const string ResourceIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.Storage/storageAccounts\\/([a-zA-Z0-9-]+)";
    private const int MaxStorageRulesCount = 200; // refer docs https://docs.microsoft.com/en-us/azure/storage/common/storage-network-security?tabs=azure-portal#grant-access-from-an-internet-ip-range

    public string Type => AzureResourceType.Storage;
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public bool PrintOut { get; set; } = false;

    object? IAzureResource.Properties
    {
      get => Props!;
      set => Props = (Properties)value!;
    }

    private Properties? Props { get; set; } = new Properties();

    public class StorageArmProperties
    {
      [JsonProperty(PropertyName = "properties")]
      public Properties Properties { get; set; } = new Properties();
    }

    public class NetworkAcls
    {
      [JsonProperty(PropertyName = "defaultAction")]
      public string DefaultAction { get; set; } = "Deny";

      [JsonProperty(PropertyName = "bypass")]
      public string Bypass { get; set; } = "Metrics, AzureServices";

      [JsonProperty(PropertyName = "virtualNetworkRules")]
      public List<VirtualNetworkRule> VirtualNetworkRules { get; set; } = new List<VirtualNetworkRule>();

      [JsonProperty(PropertyName = "ipRules")]
      public IList<IPRule> IpRules { get; set; } = new List<IPRule>();
    }

    public class Properties
    {
      [JsonProperty(PropertyName = "provisioningState")]
      public string ProvisioningState { get; set; } = null!;

      [JsonProperty(PropertyName = "networkAcls")]
      public NetworkAcls NetworkAcls { get; set; } = new NetworkAcls();
    }

    public IEnumerable<IpSecurityRestrictionRule> GenerateIpRestrictionRules(ILogger logger)
    {
      throw new InvalidOperationException("Storage type cannot GenerateIpRestrictionRules for itself.");
    }

    public async Task<ResultObject> AppendNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger,
        IRestHelper restHelper)
    {
      if (networkRestrictionSettings.IpSecRules!.Count > MaxStorageRulesCount)
      {
        LogMaxRuleCountInfo(networkRestrictionSettings, logger);

        var resultObject = new ResultObject();
        resultObject.Warnings.Add(LogMessageHelper.GetStorageLimitReachedMessage(networkRestrictionSettings.ResourceId!));
        return resultObject;
      }

      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: false);
    }

    public async Task<ResultObject> OverWriteNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger,
        IRestHelper restHelper)
    {
      if (networkRestrictionSettings.IpSecRules!.Count > MaxStorageRulesCount)
      {
        LogMaxRuleCountInfo(networkRestrictionSettings, logger);

        var resultObject = new ResultObject();
        resultObject.Errors.Add(LogMessageHelper.GetStorageLimitReachedMessage(networkRestrictionSettings.ResourceId!));
        return resultObject;
      }

      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: true);
    }

    public async Task<StorageArmProperties> GetExistingStorageArmProperties(IRestHelper restHelper, ILogger logger)
    {
      string url = $"https://management.azure.com{Id}?api-version=2021-08-01";
      string response = await restHelper.DoGET(url);
      var StorageArmProperties = JsonConvert.DeserializeObject<StorageArmProperties>(response);
      if (StorageArmProperties == null)
      {
        logger.LogError("StorageArmProperties could not be resolved for resource {Id}.", Id);
        throw new NoNullAllowedException($"StorageArmProperties could not be resolved for resource {Id}.");
      }
      return StorageArmProperties;
    }

    internal async Task<ResultObject> ApplyConfig(NetworkRestrictionSettings networkRestrictionSettings,
        ILogger logger,
        IRestHelper restHelper,
        bool overwrite)
    {
      var resultObject = new ResultObject();

      var existingConfig = await GetExistingStorageArmProperties(restHelper, logger);
      string provisioningState = existingConfig.Properties.ProvisioningState;

      if (!provisioningState.ToLower().Equals("succeeded"))
      {
        logger.LogError("Unable to update {Id} as provisioningState is '{provisioningState}'. It should be 'Succeeded'.", Id, provisioningState);
        resultObject.Errors.Add($"Unable to update {Id} as provisioningState is '{provisioningState}'. It should be 'Succeeded'. Please check and retry after sometime.");
        return resultObject;
      }

      logger.LogInformation("Existing config on {id}: {config}", Id, ConvertToJson(existingConfig));
      var configRulesToBeApplied = ConvertToStorageFirewallSettings(networkRestrictionSettings);

      if (overwrite)
      {
        // overwrite existing config
        var ipRulesToBeOverWritten = configRulesToBeApplied.Properties.NetworkAcls.IpRules;
        existingConfig.Properties.NetworkAcls.DefaultAction = "Deny";
        existingConfig.Properties.NetworkAcls.IpRules = new List<IPRule>(ipRulesToBeOverWritten);
        existingConfig.Properties.NetworkAcls.VirtualNetworkRules = new List<VirtualNetworkRule>(configRulesToBeApplied.Properties.NetworkAcls.VirtualNetworkRules);
      }
      else
      {
        if (existingConfig.Properties.NetworkAcls.DefaultAction != "Deny")
        {
          resultObject.Information.Add(
              $"Did not update resource {networkRestrictionSettings.ResourceId}. No existing restrictions were found, so it may already allow all.");
          return resultObject;
        }

        var mergedIpAddressOrRanges = existingConfig.Properties.NetworkAcls.IpRules.MergeIntoUniqueList(configRulesToBeApplied.Properties.NetworkAcls.IpRules);
        existingConfig.Properties.NetworkAcls.IpRules = new List<IPRule>(mergedIpAddressOrRanges);

        var mergedVirtualNetworkRules = existingConfig.Properties.NetworkAcls.VirtualNetworkRules.MergeIntoUniqueList(configRulesToBeApplied.Properties.NetworkAcls.VirtualNetworkRules);
        existingConfig.Properties.NetworkAcls.VirtualNetworkRules = new List<VirtualNetworkRule>(mergedVirtualNetworkRules);
      }

      if (PrintOut)
      {
        resultObject.Data = new ResultObject.OutputData();
        resultObject.Data.IPs = string.Join(',', existingConfig.Properties.NetworkAcls.IpRules.Select(x => x.IPAddressOrRange));
        resultObject.Data.SubnetIds = string.Join(',', existingConfig.Properties.NetworkAcls.VirtualNetworkRules.Select(x => x.VirtualNetworkResourceId));
        return resultObject;
      }

      string jsonString = ConvertToJson(existingConfig);
      string url = $"https://management.azure.com{Id}?api-version=2021-08-01";
      try
      {
        logger.LogInformation("Patching config on {id}: {config}", Id, jsonString);
        await restHelper.DoPatchAsJson(url, jsonString);
      }
      catch (Exception ex)
      {
        string message = $"Unable to update resource {Id}. {ex}";
        resultObject.Errors.Add(message);
        logger.LogError(message);
        return resultObject;
      }

      resultObject.Information.Add($"Update request successfully sent to {Id}.");
      return resultObject;
    }

    public StorageArmProperties ConvertToStorageFirewallSettings(NetworkRestrictionSettings networkRestrictionSettings)
    {
      var cdbProps = new StorageArmProperties();
      if (networkRestrictionSettings.IpSecRules == null)
      {
        return cdbProps;
      }

      foreach (var ipSecurityRestrictionRule in networkRestrictionSettings.IpSecRules)
      {
        if (ipSecurityRestrictionRule.IpAddress != null && IpAddressHelper.IsValidStorageFirewallIp(ipSecurityRestrictionRule.IpAddress))
        {
          var ipRules = ConvertToIpAddressesOrRanges(ipSecurityRestrictionRule);
          foreach (var ipRule in ipRules)
          {
            cdbProps.Properties.NetworkAcls.IpRules.Add(ipRule);
          }
        }

        if (ipSecurityRestrictionRule.VnetSubnetResourceId != null)
        {
          cdbProps.Properties.NetworkAcls.VirtualNetworkRules.Add(ConvertToVirtualNetworkRule(ipSecurityRestrictionRule));
        }
      }

      return cdbProps;
    }

    public (string, string) ConvertRulesToPrintOut(NetworkRestrictionSettings networkRestrictionSettings)
    {
      string IPstr = string.Join(',', networkRestrictionSettings.IpSecRules!.Where(x => x.IpAddress != null && IpAddressHelper.IsValidStorageFirewallIp(x.IpAddress))
          .SelectMany(x => ExpandIpAddress(x.IpAddress!)).Select(ip => ip.ToString()));
 
      string SubnetIds = string.Join(',', networkRestrictionSettings.IpSecRules!.Where(x => x.VnetSubnetResourceId != null).Select(x => x.VnetSubnetResourceId));
      return (IPstr, SubnetIds);
    }

    internal VirtualNetworkRule ConvertToVirtualNetworkRule(IpSecurityRestrictionRule ipSecurityRestrictionRule) =>
        new VirtualNetworkRule()
        {
          VirtualNetworkResourceId = ipSecurityRestrictionRule.VnetSubnetResourceId,
          Action = Microsoft.Azure.Management.Storage.Models.Action.Allow,
          State = Microsoft.Azure.Management.Storage.Models.State.Succeeded
        };

    internal List<IPRule> ConvertToIpAddressesOrRanges(IpSecurityRestrictionRule ipSecurityRestrictionRule)
    {
      var ipRules = new List<IPRule>();

      if (ipSecurityRestrictionRule.IpAddress!.EndsWith("/31"))
      {
        var baseAddress = ipSecurityRestrictionRule.IpAddress.Replace("/31", "");
        var nextAddress = IncrementIpAddress(baseAddress);

        ipRules.Add(new IPRule() { IPAddressOrRange = baseAddress, Action = Microsoft.Azure.Management.Storage.Models.Action.Allow });
        ipRules.Add(new IPRule() { IPAddressOrRange = nextAddress, Action = Microsoft.Azure.Management.Storage.Models.Action.Allow });
      }
      else
      {
        ipRules.Add(new IPRule() { IPAddressOrRange = ipSecurityRestrictionRule.IpAddress!.Replace("/32", ""), Action = Microsoft.Azure.Management.Storage.Models.Action.Allow });
      }

      return ipRules;
    }


    internal List<string> ExpandIpAddress(string ipAddressWithCidr)
    {
      var ipAddresses = new List<string>();

      if (ipAddressWithCidr.EndsWith("/31"))
      {
        var baseAddress = ipAddressWithCidr.Replace("/31", "");
        ipAddresses.Add(baseAddress);
        ipAddresses.Add(IncrementIpAddress(baseAddress));
      }      
      else
      {
        ipAddresses.Add(ipAddressWithCidr.Replace("/32", ""));
      }

      return ipAddresses;
    }

    private string IncrementIpAddress(string ipAddress)
    {
      byte[] addressBytes = IPAddress.Parse(ipAddress).GetAddressBytes().Reverse().ToArray();
      uint ipAsUint = BitConverter.ToUInt32(addressBytes, 0);
      var nextAddress = BitConverter.GetBytes(ipAsUint + 1);
      return String.Join(".", nextAddress.Reverse());
    }

    internal string ConvertToJson(StorageArmProperties StorageArmProperties)
    {
      string jsonString = JsonConvert.SerializeObject(StorageArmProperties, Formatting.Indented);
      return jsonString;
    }

    private void LogMaxRuleCountInfo(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger)
    {
      string jsonString = IpSecurityRestrictionRuleHelper.ConvertToJsonString(networkRestrictionSettings.IpSecRules!);
      logger.LogInformation("{generatedIpRulesCount} Ip Rules generated for {resourceName} to apply: Generated Rules {generatedIpRules}",
          networkRestrictionSettings.IpSecRules!.Count(),
          Name,
          jsonString);
    }
  }
}
