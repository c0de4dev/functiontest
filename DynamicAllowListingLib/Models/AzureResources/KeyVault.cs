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

namespace DynamicAllowListingLib.Models.AzureResources
{
  public class KeyVault : IAzureResource
  {
    public const string ResourceIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.KeyVault/vaults\\/([a-zA-Z0-9-]+)";
    public string Type => AzureResourceType.KeyVault;
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public bool PrintOut { get; set; } = false;
    object? IAzureResource.Properties
    {
      get => Props!;
      set => Props = (Properties)value!;
    }
    //custom properties to be cast
    private Properties? Props { get; set; } = new Properties();
    public class Properties
    {
      [JsonProperty(PropertyName = "provisioningState")]
      public string ProvisioningState { get; set; } = null!;
      [JsonProperty(PropertyName = "networkAcls")]
      public NetworkAcls NetworkAcls { get; set; } = new NetworkAcls();
    }
    public class NetworkAcls
    {
      [JsonProperty(PropertyName = "defaultAction")]
      public string DefaultAction { get; set; } = "Deny";
      [JsonProperty(PropertyName = "bypass")]
      public string Bypass { get; set; } = "AzureServices";
      [JsonProperty(PropertyName = "virtualNetworkRules")]
      public List<VirtualNetworkRule> VirtualNetworkRules { get; set; } = new List<VirtualNetworkRule>();
      [JsonProperty(PropertyName = "ipRules")]
      public IList<IpAddressOrRange> IpRules { get; set; } = new List<IpAddressOrRange>();
    }
    public class KeyVaultArmProperties
    {
      [JsonProperty(PropertyName = "properties")]
      public Properties Properties { get; set; } = new Properties();
    }
    public class VirtualNetworkRule
    {
      public VirtualNetworkRule(string id)
      {
        this.Id = id;
      }
      [JsonProperty(PropertyName = "id")]
      public string Id { get; set; }
    }

    public async Task<ResultObject> AppendNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger, IRestHelper restHelper)
    {
      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: false);
    }
    public IEnumerable<IpSecurityRestrictionRule> GenerateIpRestrictionRules(ILogger logger)
    {
      throw new InvalidOperationException("KeyVault type cannot GenerateIpRestrictionRules for itself.");
    }
    public async Task<ResultObject> OverWriteNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger, IRestHelper restHelper)
    {
      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: true);
    }

    public async Task<KeyVaultArmProperties?> GetExistingKeyvaultArmProperties(IRestHelper restHelper, ILogger logger)
    {
      // Construct the URL for the API request
      string url = $"https://management.azure.com{Id}?api-version=2022-07-01";
      KeyVaultArmProperties? keyvaultArmProperties = null;
      try
      {
        // Make the GET request to fetch KeyVault properties
        string response = await restHelper.DoGET(url);
        // Deserialize the response into KeyVaultArmProperties object
        keyvaultArmProperties = JsonConvert.DeserializeObject<KeyVaultArmProperties>(response);
        // If the response is null, log the error and throw an exception
        if (keyvaultArmProperties == null)
        {
          string errorMessage = $"Failed to resolve Keyvault properties for resource {Id}. The response was null.";
          logger.LogError(errorMessage);
          throw new NoNullAllowedException(errorMessage);
        }
        logger.LogInformation($"Successfully fetched Keyvault properties for resource {Id}");
      }
      catch (Exception ex)
      {
        // Log the exception details
        string errorMessage = $"Error occurred while fetching Keyvault properties for resource {Id}. Exception:{ex.Message}";
        logger.LogError(ex, errorMessage);
      }        
      // Return the KeyVaultArmProperties object
      return keyvaultArmProperties;
    }

    internal async Task<ResultObject> ApplyConfig(NetworkRestrictionSettings networkRestrictionSettings,
     ILogger logger,
     IRestHelper restHelper,
     bool overwrite)
    {
      var resultObject = new ResultObject();
      try
      {
        // Fetch the existing KeyVault properties
        var existingConfig = await GetExistingKeyvaultArmProperties(restHelper, logger);

        // Check if the existingConfig is null before attempting to access properties
        if (existingConfig?.Properties == null)
        {
          logger.LogError("KeyVault properties are null for resource {Id}.", Id);
          throw new InvalidOperationException($"KeyVault properties are null for resource {Id}.");
        }

        // Access the ProvisioningState safely
        string provisioningState = existingConfig.Properties?.ProvisioningState ?? "Unknown"; // Default to "Unknown" if null

        // Check provisioning state before proceeding with update
        if (!provisioningState.Equals("Succeeded", StringComparison.OrdinalIgnoreCase))
        {
          string errorMessage = $"Unable to update {Id} as provisioningState is '{provisioningState}'. It should be 'Succeeded'.Please check and retry after sometime.";
          logger.LogError(errorMessage);
          resultObject.Errors.Add(errorMessage);
          return resultObject;
        }
        logger.LogInformation("Existing config on {id}: {config}", Id, JsonConvert.SerializeObject(existingConfig, Formatting.Indented));

        // Convert the firewall settings from network restriction settings
        var configRulesToBeApplied = ConvertToCosmosFirewallSettings(networkRestrictionSettings);

        // Overwrite or merge configuration based on the flag
        if (overwrite)
        {
          logger.LogInformation("Overwriting existing config with new network ACL rules.");
          if (existingConfig.Properties != null)
          {
            existingConfig.Properties.NetworkAcls.DefaultAction = "Deny";
            existingConfig.Properties.NetworkAcls.IpRules = configRulesToBeApplied.Properties.NetworkAcls.IpRules.ToList();
            existingConfig.Properties.NetworkAcls.VirtualNetworkRules = configRulesToBeApplied.Properties.NetworkAcls.VirtualNetworkRules.ToList();
          }
        }
        else
        {
          if (existingConfig.Properties != null)
          {
            // Check if the current config already allows all IPs
            if (existingConfig.Properties.NetworkAcls.DefaultAction == "Allow")
            {
              string infoMessage = $"Did not update resource {networkRestrictionSettings.ResourceId} as it already allows all.";
              resultObject.Information.Add(infoMessage);
              logger.LogInformation(infoMessage);
              return resultObject;
            }

            // Merge new rules with existing ones (making sure no duplicates)
            logger.LogInformation("Merging new IP and Virtual Network rules into existing config.");
            var mergedIpAddressOrRanges = existingConfig.Properties.NetworkAcls.IpRules.MergeIntoUniqueList(configRulesToBeApplied.Properties.NetworkAcls.IpRules);
            existingConfig.Properties.NetworkAcls.IpRules = mergedIpAddressOrRanges.ToList();

            var mergedVirtualNetworkRules = existingConfig.Properties.NetworkAcls.VirtualNetworkRules.MergeIntoUniqueList(configRulesToBeApplied.Properties.NetworkAcls.VirtualNetworkRules);
            existingConfig.Properties.NetworkAcls.VirtualNetworkRules = mergedVirtualNetworkRules.ToList();
            existingConfig.Properties.NetworkAcls.DefaultAction = "Deny";
          }
        }
        // If print-only mode, return the result without applying changes
        if (PrintOut)
        {
          // Ensure that NetworkAcls and its properties are not null
          var networkAcls = existingConfig?.Properties?.NetworkAcls;

          if (networkAcls == null)
          {
            logger.LogError("Network Acls are null for resource {Id}.", Id);
            //throw new InvalidOperationException($"Network Acls are null for resource {Id}.");
          }

          // Safely join IPs, checking for null or empty lists
          var ipRules = networkAcls?.IpRules?.Select(x => x.value) ?? Enumerable.Empty<string>();
          var subnetIds = networkAcls?.VirtualNetworkRules?.Select(x => x.Id) ?? Enumerable.Empty<string>();

          resultObject.Data = new ResultObject.OutputData
          {
            IPs = string.Join(',', ipRules),
            SubnetIds = string.Join(',', subnetIds)
          };

          return resultObject;
        }
        // Prepare the updated config to send as a patch request
        string jsonString = JsonConvert.SerializeObject(existingConfig, Formatting.Indented);
        string url = $"https://management.azure.com{Id}?api-version=2022-07-01";

        // Send the patch request to update the config
        logger.LogInformation("Patching config on {id}: {config}", Id, jsonString);
        await restHelper.DoPatchAsJson(url, jsonString);

        // Successfully sent update request
        resultObject.Information.Add($"Update request successfully sent to {Id}.");
        return resultObject;
      }
      catch (Exception ex)
      {        
        // Handle and log any errors that occur during the process
        string errorMessage = $"Error occurred while applying config to resource {Id}: {ex.Message}";
        resultObject.Errors.Add(errorMessage);
        logger.LogError(ex, errorMessage);
        return resultObject;
      }
    }

    internal KeyVaultArmProperties ConvertToCosmosFirewallSettings(NetworkRestrictionSettings networkRestrictionSettings)
    {
      var kvProps = new KeyVaultArmProperties();
      if (networkRestrictionSettings.IpSecRules == null)
      {
        return kvProps;
      }

      foreach (var ipSecurityRestrictionRule in networkRestrictionSettings.IpSecRules)
      {
        if (ipSecurityRestrictionRule.IpAddress != null && IpAddressHelper.IsValidKeyVaultFirewallIp(ipSecurityRestrictionRule.IpAddress))
        {
          kvProps.Properties.NetworkAcls.IpRules.Add(ConvertToIpAddressOrRange(ipSecurityRestrictionRule));
        }

        if (ipSecurityRestrictionRule.VnetSubnetResourceId != null)
        {
          kvProps.Properties.NetworkAcls.VirtualNetworkRules.Add(ConvertToVirtualNetworkRule(ipSecurityRestrictionRule));
        }
      }

      return kvProps;
    }
    public (string, string) ConvertRulesToPrintOut(NetworkRestrictionSettings networkRestrictionSettings)
    {
      string IPstr = string.Join(',', networkRestrictionSettings.IpSecRules!.Where(x => x.IpAddress != null && IpAddressHelper.IsValidKeyVaultFirewallIp(x.IpAddress)).Select(x => x.IpAddress));
      string SubnetIds = string.Join(',', networkRestrictionSettings.IpSecRules!.Where(x => x.VnetSubnetResourceId != null).Select(x => x.VnetSubnetResourceId));
      return (IPstr, SubnetIds);
    }
    internal VirtualNetworkRule ConvertToVirtualNetworkRule(IpSecurityRestrictionRule ipSecurityRestrictionRule) =>
    new VirtualNetworkRule(ipSecurityRestrictionRule.VnetSubnetResourceId!);

    internal IpAddressOrRange ConvertToIpAddressOrRange(IpSecurityRestrictionRule ipSecurityRestrictionRule)
      => new IpAddressOrRange() { value = ipSecurityRestrictionRule.IpAddress! };

    public class IpAddressOrRange
    {
      public IpAddressOrRange() { }
    
      public IpAddressOrRange(string? ipAddressOrRangeProperty = null) {

        value = ipAddressOrRangeProperty;
      }
      public string? value { get; set; }
    }

  }
}
