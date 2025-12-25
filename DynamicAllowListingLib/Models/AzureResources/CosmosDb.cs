using DynamicAllowListingLib.Extensions;
using DynamicAllowListingLib.Helpers;
using Microsoft.Azure.Management.CosmosDB.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static DynamicAllowListingLib.Models.VNets;

namespace DynamicAllowListingLib.Models.AzureResources
{
  public class CosmosDb : IAzureResource
  {
    public const string ResourceIdRegex = "/subscriptions/([a-zA-Z0-9-]+)\\/resourceGroups\\/([a-zA-Z0-9-]+)\\/providers\\/Microsoft.DocumentDB/databaseAccounts\\/([a-zA-Z0-9-]+)";
    public string Type => AzureResourceType.CosmosDb;
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
    public class CosmosDbArmProperties
    {
      [JsonProperty(PropertyName = "properties")]
      public Properties Properties { get; set; } = new Properties();
    }
    public class Properties
    {
      [JsonProperty(PropertyName = "provisioningState")]
      public string ProvisioningState { get; set; } = null!;
      [JsonProperty(PropertyName = "isVirtualNetworkFilterEnabled")]
      public bool IsVirtualNetworkFilterEnabled { get; set; }
      [JsonProperty(PropertyName = "virtualNetworkRules")]
      public List<VirtualNetworkRule> VirtualNetworkRules { get; set; } = new List<VirtualNetworkRule>();
      [JsonProperty(PropertyName = "ipRules")]
      public IList<IpAddressOrRange> IpRules { get; set; } = new List<IpAddressOrRange>();
    }

    public IEnumerable<IpSecurityRestrictionRule> GenerateIpRestrictionRules(ILogger logger)
    {
      throw new InvalidOperationException("DocumentDb type cannot GenerateIpRestrictionRules for itself.");
    }

    public async Task<ResultObject> AppendNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger,
      IRestHelper restHelper)
    {
      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: false);
    }

    public async Task<ResultObject> OverWriteNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionSettings, ILogger logger,
      IRestHelper restHelper)
    {
      return await ApplyConfig(networkRestrictionSettings, logger, restHelper, overwrite: true);
    }

    public async Task<CosmosDbArmProperties?> GetExistingCosmosDbArmProperties(IRestHelper restHelper, ILogger logger)
    {
      // Ensure the Id is not null or empty before proceeding.
      if (string.IsNullOrEmpty(Id))
      {
        logger.LogError("Resource Id cannot be null or empty.");
        //throw new ArgumentException("Resource Id cannot be null or empty.");
      }
      var cosmosDbArmProperties = new CosmosDbArmProperties();
      try
      {
        string url = $"https://management.azure.com{Id}?api-version=2021-04-15";
        // Make the GET request
        string response = await restHelper.DoGET(url);
        // Check if the response is null or empty
        if (string.IsNullOrEmpty(response))
        {
          logger.LogError("Received empty or null response for CosmosDbArmProperties for resource {Id}.", Id);
          throw new InvalidOperationException($"Received empty response for resource {Id}.");
        }
        // Deserialize the response into CosmosDbArmProperties
        cosmosDbArmProperties = JsonConvert.DeserializeObject<CosmosDbArmProperties>(response);
        // Handle deserialization failure
        if (cosmosDbArmProperties == null)
        {
          logger.LogError("Failed to deserialize CosmosDbArmProperties for resource {Id}. Response: {Response}", Id, response);
          throw new NoNullAllowedException($"CosmosDbArmProperties could not be resolved for resource {Id}.");
        }
        // Optional: Log the success of the operation
        logger.LogInformation("Successfully retrieved CosmosDbArmProperties for resource {Id}.", Id);
      }
      catch (Exception ex)
      {
        // Log any unexpected errors and add to the result object
        string errorMessage = $"Error while retrieving CosmosDbArmProperties for resource:{Id}. Exception:{ex.Message}";
        logger.LogError(errorMessage);
      }
      return cosmosDbArmProperties;
    }

    internal async Task<ResultObject> ApplyConfig(NetworkRestrictionSettings networkRestrictionSettings,
      ILogger logger,
      IRestHelper restHelper,
      bool overwrite)
    {
      var resultObject = new ResultObject();

      var existingConfig = await GetExistingCosmosDbArmProperties(restHelper, logger);
      string provisioningState = existingConfig!.Properties.ProvisioningState;
      if (!provisioningState.ToLower().Equals("succeeded"))
      {
        logger.LogError("Unable to update {Id} as provisioningState is '{provisioningState}'. It should be 'Succeeded'.", Id, provisioningState);
        resultObject.Errors.Add($"Unable to update {Id} as provisioningState is '{provisioningState}'. It should be 'Succeeded'. Please check and retry after sometime.");
        return resultObject;
      }

      logger.LogInformation("Existing config on {id}: {config}", Id, ConvertToJson(existingConfig));
      var configRulesToBeApplied = ConvertToCosmosFirewallSettings(networkRestrictionSettings);

      if (overwrite)
      {
        // overwrite existing config
        var ipRulesToBeOverWritten = configRulesToBeApplied.Properties.IpRules.Union(GetDefaultIpAddressOrRanges());
        existingConfig.Properties.IpRules = new List<IpAddressOrRange>(ipRulesToBeOverWritten);
        existingConfig.Properties.VirtualNetworkRules = new List<VirtualNetworkRule>(configRulesToBeApplied.Properties.VirtualNetworkRules);
        existingConfig.Properties.IsVirtualNetworkFilterEnabled = existingConfig.Properties.VirtualNetworkRules.Count > 0;
      }
      else
      {
        if (existingConfig.Properties.VirtualNetworkRules.Count <= 0 && existingConfig.Properties.IpRules.Count <= 0)
        {
          resultObject.Information.Add($"Did not update resource {networkRestrictionSettings.ResourceId}. No existing restrictions were found, so it may already allow all.");
          return resultObject;
        }

        var mergedIpAddressOrRanges = existingConfig.Properties.IpRules.MergeIntoUniqueList
          (configRulesToBeApplied.Properties.IpRules);
        existingConfig.Properties.IpRules = new List<IpAddressOrRange>(mergedIpAddressOrRanges);

        var mergedVirtualNetworkRules = existingConfig.Properties.VirtualNetworkRules.MergeIntoUniqueList(configRulesToBeApplied.Properties.VirtualNetworkRules);
        existingConfig.Properties.VirtualNetworkRules = new List<VirtualNetworkRule>(mergedVirtualNetworkRules);
      }

      //check if generated subnet rules are applicable
      if (!(await IsValid(resultObject, existingConfig, restHelper, logger)))
        return resultObject;

      //do not apply if just print 
      if (PrintOut)
      {
        resultObject.Data = new ResultObject.OutputData();
        resultObject.Data.IPs = string.Join(',', existingConfig.Properties.IpRules.Select(x => x.IpAddressOrRangeProperty));
        resultObject.Data.SubnetIds = string.Join(',', existingConfig.Properties.VirtualNetworkRules.Select(x => x.Id));
        return resultObject;
      }
           

      string jsonString = ConvertToJson(existingConfig);
      string url = $"https://management.azure.com{Id}?api-version=2021-04-15";
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

    
    private async Task<bool> IsValid(ResultObject resultObject, CosmosDbArmProperties existingConfig, IRestHelper restHelper, ILogger logger)
    {
      var subnetRulesToCheck = existingConfig.Properties.VirtualNetworkRules.Select(s => s.Id).ToList();
      var subnets = new List<AzSubnet>();
      try
      {
        // Fetch details for each subnet that needs validation
        foreach (var subnetId in subnetRulesToCheck)
        {
          var subnet = await GetSubnetInfo(subnetId, restHelper, logger);
          if (subnet != null)
          {
            subnets.Add(subnet);
          }
        }
        // Identify subnets that are missing the required Service Endpoint for Azure Cosmos DB
        var missingServiceEndpointSubnets = subnets.Where(x => !x.Properties!.ServiceEndpoints.Any(x => x.Service == "Microsoft.AzureCosmosDB"));
        if (missingServiceEndpointSubnets.Count() > 0)
        {
          // If there are subnets missing the required service endpoint, log the issue and return false
          foreach (var invalidSubnet in missingServiceEndpointSubnets)
          {
            string errorMessage = $"Subnet {invalidSubnet.Id} is missing Service Endpoint for 'Microsoft.AzureCosmosDB'. Please enable the Service Endpoint first.";
            resultObject.Errors.Add(errorMessage);
            logger.LogWarning(errorMessage);
          }
          return false;
        }
        // Log success if all subnets are valid
        logger.LogInformation("All subnets are valid and have the required Service Endpoint for 'Microsoft.AzureCosmosDB'.");

        return true;
      }
      catch (Exception ex)
      {
        // Log any unexpected errors and add to the result object
        string errorMessage = $"An error occurred while validating subnets for resource.Exception:{ex.Message}";
        resultObject.Errors.Add(errorMessage);
        logger.LogError(ex, errorMessage);
        return false;
      }
    }

    private async Task<AzSubnet?> GetSubnetInfo(string subnetId, IRestHelper restHelper, ILogger logger)
    {
      // Construct the URL for the API request
      string url = $"https://management.azure.com{subnetId}?api-version=2022-07-01";
      var subnet = new AzSubnet();
      try
      {
        // Make the GET request to fetch subnet information
        string response = await restHelper.DoGET(url);
        // Deserialize the response into the AzSubnet object
        subnet = JsonConvert.DeserializeObject<AzSubnet>(response);
        // If the response is null, log the error and throw an exception
        if (subnet == null)
        {
          string errorMessage = $"Failed to resolve SubnetId: {subnetId}. The response was null.";
          logger.LogError(errorMessage);
          throw new NoNullAllowedException(errorMessage);
        }
        // Log success with subnet details (optional: modify this based on the level of detail required)
        logger.LogInformation($"Successfully fetched subnet info for SubnetId: {subnetId}");
      }
      catch (Exception ex)
      {
        // Log the error with the exception details and throw it again
        string errorMessage = $"Error occurred while fetching subnet info for SubnetId: {subnetId}. {ex.Message}";
        logger.LogError(errorMessage);
      }
      // Return the subnet object
      return subnet;
    }

    internal List<IpAddressOrRange> GetDefaultIpAddressOrRanges() =>
      // Obtained from https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-configure-firewall#allow-requests-from-the-azure-portal
      ConvertToAddressOrRangeList(
        new[]
        {
          "104.42.195.92", "40.76.54.131", "52.176.6.30", "52.169.50.45", "52.187.184.26"
        }
      );

    internal static List<IpAddressOrRange> ConvertToAddressOrRangeList(string[] ipArray)
    {
      return ipArray.Select(ipAddress => new IpAddressOrRange() { IpAddressOrRangeProperty = ipAddress }).ToList();
    }

    internal CosmosDbArmProperties ConvertToCosmosFirewallSettings(NetworkRestrictionSettings networkRestrictionSettings)
    {
      var cdbProps = new CosmosDbArmProperties();
      if (networkRestrictionSettings.IpSecRules == null)
      {
        return cdbProps;
      }

      foreach (var ipSecurityRestrictionRule in networkRestrictionSettings.IpSecRules)
      {
        if (ipSecurityRestrictionRule.IpAddress != null && IpAddressHelper.IsValidCosmosDbFirewallIp(ipSecurityRestrictionRule.IpAddress))
        {
          cdbProps.Properties.IpRules.Add(ConvertToIpAddressOrRange(ipSecurityRestrictionRule));
        }

        if (ipSecurityRestrictionRule.VnetSubnetResourceId != null)
        {
          cdbProps.Properties.VirtualNetworkRules.Add(ConvertToVirtualNetworkRule(ipSecurityRestrictionRule));
        }
      }

      return cdbProps;
    }

    internal VirtualNetworkRule ConvertToVirtualNetworkRule(IpSecurityRestrictionRule ipSecurityRestrictionRule) =>
      new VirtualNetworkRule()
      {
        Id = ipSecurityRestrictionRule.VnetSubnetResourceId,
        IgnoreMissingVNetServiceEndpoint = false
      };

    internal IpAddressOrRange ConvertToIpAddressOrRange(IpSecurityRestrictionRule ipSecurityRestrictionRule)
      => new IpAddressOrRange() { IpAddressOrRangeProperty = ipSecurityRestrictionRule.IpAddress };

    internal string ConvertToJson(CosmosDbArmProperties cosmosDbArmProperties)
    {
      string jsonString = JsonConvert.SerializeObject(cosmosDbArmProperties, Formatting.Indented);
      return jsonString;
    }
    public (string, string) ConvertRulesToPrintOut(NetworkRestrictionSettings networkRestrictionSettings)
    {
      string IPstr = string.Join(',', networkRestrictionSettings.IpSecRules!.Where(x => x.IpAddress != null && IpAddressHelper.IsValidCosmosDbFirewallIp(x.IpAddress)).Select(x => x.IpAddress));
      string SubnetIds = string.Join(',', networkRestrictionSettings.IpSecRules!.Where(x => x.VnetSubnetResourceId != null).Select(x => x.VnetSubnetResourceId));
      return (IPstr, SubnetIds);
    }
  }

  public partial class AzSubnet
  {
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("properties")]
    public SubnetProperty? Properties { get; set; }
  }
  public partial class SubnetProperty
  {
    [JsonProperty("provisioningState")]
    public string? ProvisioningState { get; set; }
    [JsonProperty("serviceEndpoints")]
    public List<ServiceEndpoint> ServiceEndpoints { get; set; } = new List<ServiceEndpoint>();
  }
  public partial class ServiceEndpoint
  {
    [JsonProperty("provisioningState")]
    public string? ProvisioningState { get; set; }
    [JsonProperty("service")]
    public string? Service { get; set; }
    [JsonProperty("locations")]
    public List<string> Locations { get; set; } = new List<string>();
  }
}
