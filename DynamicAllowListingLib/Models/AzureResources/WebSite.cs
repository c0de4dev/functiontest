using DynamicAllowListingLib.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.Models.AzureResources
{
  public class WebSite : IAzureResource
  {
    public virtual string Type => AzureResourceType.WebSite;
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public bool PrintOut { get; set; } = false;
    public object? Properties
    {
      get => Props!;
      set => this.Props = (WebSiteProperties)value!;
    }
    //custom properties to be cast
    private WebSiteProperties? Props { get; set; } = new WebSiteProperties();
    public class WebSiteProperties
    {
      public string? PossibleOutboundIpAddresses { get; set; }
      public string? VirtualNetworkSubnetId { get; set; }
    }

    public IEnumerable<IpSecurityRestrictionRule> GenerateIpRestrictionRules(ILogger logger)
    {
      try
      {
        // Check if VirtualNetworkSubnetId is null or empty
        if (string.IsNullOrEmpty(Props?.VirtualNetworkSubnetId))
        {
          logger.LogInformation(
              "VirtualNetworkSubnetId is null or empty. Generating dynamic allow-listing rules. WebAppName: {AppName}, {PossibleOutboundIpAddresses}",
              Name,
              Props?.PossibleOutboundIpAddresses
          );

          return IpRulesHelper.GenerateDynamicAllowListingRules(Name!, Props?.PossibleOutboundIpAddresses!);
        }
        // Log when VirtualNetworkSubnetId is provided
        logger.LogInformation(
            "VirtualNetworkSubnetId is provided. Generating dynamic allow-listing rule for VNet. WebAppName: {AppName}, {VirtualNetworkSubnetId}",
            Name,
            Props.VirtualNetworkSubnetId
        );

        return new List<IpSecurityRestrictionRule>
        {
            IpRulesHelper.GenerateDynamicAllowListingRuleForVnet(Name!, Props.VirtualNetworkSubnetId!)
        };
      }
      catch (Exception ex)
      {        // Log unexpected exceptions
        logger.LogError(ex, "An error occurred while generating IP restriction rules. Context: {AppName}, {Props}",
            Name,
            Props
        );

        throw; // Re-throw exception to allow for upstream handling
      }
    }

    public async Task<ResultObject> AppendNetworkRestrictionRules(NetworkRestrictionSettings networkRestrictionsToAppend,
      ILogger logger,
      IRestHelper restHelper)
    {
      // Initialize result object
      var resultObject = new ResultObject();

      // Validate input
      if (networkRestrictionsToAppend.ResourceId == null)
      {
        resultObject.Errors.Add("Main resource id was found to be null.");
        logger.LogWarning("Resource ID is null. Unable to append network restriction rules.");
        return resultObject;
      }

      // Prepare request parameters
      string resourceIdUri = networkRestrictionsToAppend.ResourceId;
      string url = $"https://management.azure.com{resourceIdUri}/config/web?api-version=2021-02-01";

      var resourceIdParts = resourceIdUri.Split('/');
      var resourceName = resourceIdParts[resourceIdParts.Length - 1];

      logger.LogInformation("Starting to append network restriction rules. ResourceID: {ResourceIdUri}, {ResourceName}", resourceIdUri, resourceName);
      try
      {
        // Fetch existing configuration
        logger.LogDebug("Sending GET request to {Url}.", url);
        string? response = await restHelper.DoGET(url);

        // Ensure we keep existing rules.
        NetworkRestrictionSettings? networkRestrictionSettings = null;
        if (string.IsNullOrEmpty(response))
        {
          resultObject.Errors.Add("Failed to fetch existing configuration. Response was null or empty.");
          logger.LogWarning("GET request to {Url} returned null or empty response.", url);
          return resultObject;
        }
        using (var document = JsonDocument.Parse(response))
        {
          // Fetch existing IP Security Restrictions
          var existingIpSecurityRestrictions = IpSecurityRestrictionRuleHelper.GetIpSecurityRestrictions(document);
          logger.LogInformation("Fetched {Count} existing IP security restrictions. ResourceID: {ResourceIdUri}", existingIpSecurityRestrictions.Count, resourceIdUri);

          if (existingIpSecurityRestrictions.Count > 0)
          {
            networkRestrictionSettings = new NetworkRestrictionSettings
            {
              IpSecRules = networkRestrictionsToAppend.IpSecRules != null
                ? new HashSet<IpSecurityRestrictionRule>(ConsolidateIpAddresses(networkRestrictionsToAppend.IpSecRules))
                : new HashSet<IpSecurityRestrictionRule>()
            };
            networkRestrictionSettings.IpSecRules.UnionWith(existingIpSecurityRestrictions);
          }

          // Fetch existing SCM IP Security Restrictions
          var existingScmIpSecurityRestrictions =
            IpSecurityRestrictionRuleHelper.GetScmIpSecurityRestrictions(document);
          logger.LogInformation("Fetched {Count} existing SCM IP security restrictions. ResourceID: {ResourceIdUri}", existingScmIpSecurityRestrictions.Count, resourceIdUri);

          if (existingScmIpSecurityRestrictions.Count > 0)
          {
            if (networkRestrictionSettings != null)
            {
              networkRestrictionSettings.ScmIpSecRules = networkRestrictionsToAppend.ScmIpSecRules != null
                          ? new HashSet<IpSecurityRestrictionRule>(ConsolidateIpAddresses(networkRestrictionsToAppend.ScmIpSecRules))
                          : new HashSet<IpSecurityRestrictionRule>();
            }
            else
            {
              networkRestrictionSettings = new NetworkRestrictionSettings
              {
                ScmIpSecRules = networkRestrictionsToAppend.ScmIpSecRules != null
                  ? new HashSet<IpSecurityRestrictionRule>(ConsolidateIpAddresses(networkRestrictionsToAppend.ScmIpSecRules))
                  : new HashSet<IpSecurityRestrictionRule>()
              };
            }

            networkRestrictionSettings.ScmIpSecRules.UnionWith(existingScmIpSecurityRestrictions);
          }
        }

        // No rules to update
        if (networkRestrictionSettings == null)
        {
          string message = $"No existing restrictions found for resource {resourceIdUri}. The resource may already allow all traffic.";
          logger.LogInformation(message);
          resultObject.Information.Add(message);
          return resultObject;
        }

        // Check if limits are reached
        var limitReachedMessages = GetMessagesIfLimitReached(resourceIdUri, resourceName, networkRestrictionSettings, logger);
        if (limitReachedMessages.Count() > 0)
        {
          logger.LogWarning("IP restriction limit reached for {ResourceIdUri}. Message: {Messages}", resourceIdUri, limitReachedMessages);
          resultObject.Warnings.AddRange(limitReachedMessages);
          return resultObject;
        }

        // Apply configuration
        await ApplyConfig(response, url, resourceName, networkRestrictionSettings, logger, restHelper);
        string successMessage = LogMessageHelper.GetSuccessfullyUpdatedConfigMessage(resourceIdUri);
        logger.LogInformation("Successfully updated network restriction rules for {ResourceIdUri}.", resourceIdUri);
        resultObject.Information.Add(successMessage);
      }
      catch (Exception ex)
      {
        string errorMessage = $"Unable to update web config for {resourceIdUri}";
        resultObject.Warnings.Add(errorMessage);
        logger.LogError(ex, "{ErrorMessage}. Context: {ResourceIdUri}", errorMessage, resourceIdUri);
        resultObject.Warnings.Add(ex.ToString());
      }

      return resultObject;
    }

    public async Task<ResultObject> OverWriteNetworkRestrictionRules(NetworkRestrictionSettings updateNetworkRestrictionSettings,
      ILogger logger,
      IRestHelper restHelper)
    {
      // Initialize the result object
      var resultObject = new ResultObject();

      if (updateNetworkRestrictionSettings.ResourceId == null)
      {
        var errorMessage = "Main resource ID was found to be null.";
        logger.LogWarning(errorMessage);
        resultObject.Errors.Add(errorMessage);
        return resultObject;
      }

      // Prepare the URL and resource context
      string resourceIdUri = updateNetworkRestrictionSettings.ResourceId;
      var resourceIdParts = resourceIdUri.Split('/');
      var resourceName = resourceIdParts[resourceIdParts.Length - 1];

      string url = $"https://management.azure.com{resourceIdUri}/config/web?api-version=2021-02-01";

      logger.LogInformation("Starting to overwrite network restriction rules. ResourceID: {ResourceIdUri}, {ResourceName}",
    resourceIdUri, resourceName);
      try
      {
        // Fetch the current configuration
        logger.LogDebug("Sending GET request to {Url}.", url);
        string? response = await restHelper.DoGET(url);

        // Prepare new network restriction settings
        logger.LogDebug("Preparing new network restriction settings for {ResourceName}.", resourceName);
        var networkRestrictionSettings = new NetworkRestrictionSettings
        {
          IpSecRules = updateNetworkRestrictionSettings.IpSecRules != null
            ? updateNetworkRestrictionSettings.IpSecRules.ToHashSet()
            : new HashSet<IpSecurityRestrictionRule>(),
          ScmIpSecRules = updateNetworkRestrictionSettings.ScmIpSecRules != null
            ? updateNetworkRestrictionSettings.ScmIpSecRules.ToHashSet()
            : new HashSet<IpSecurityRestrictionRule>()
        };

        // Consolidate IP addresses for optimization
        networkRestrictionSettings.IpSecRules = ConsolidateIpAddresses(networkRestrictionSettings.IpSecRules);
        networkRestrictionSettings.ScmIpSecRules = ConsolidateIpAddresses(networkRestrictionSettings.ScmIpSecRules);

        // Check for restriction limits
        var limitReachedMessages = GetMessagesIfLimitReached(resourceIdUri, resourceName, networkRestrictionSettings, logger);
        if (limitReachedMessages.Count() > 0)
        {
          logger.LogWarning(
                "Limit reached for IP security restrictions. ResourceID: {ResourceIdUri}, ResourceName: {ResourceName}, {Messages}",
                resourceIdUri, resourceName, limitReachedMessages);

          resultObject.Errors.AddRange(limitReachedMessages);
          return resultObject;
        }
        // Apply the new configuration
        logger.LogInformation(
            "Applying network restriction configuration for {ResourceIdUri}, {ResourceName}.",
            resourceIdUri, resourceName);

        await ApplyConfig(response!, url, resourceName, networkRestrictionSettings, logger, restHelper);
        logger.LogInformation("Apply config success for {resourceName}", resourceName);
        string successMessage = LogMessageHelper.GetSuccessfullyUpdatedConfigMessage(resourceIdUri);

        logger.LogInformation("Successfully applied network restriction configuration. Context: {ResourceIdUri}, {ResourceName}", resourceIdUri, resourceName);
        resultObject.Information.Add(successMessage);
      }
      catch (Exception exception)
      {
        logger.LogError("Apply config error for {resourceName}. Exception: {exception}", resourceName, exception);
        resultObject.Errors.Add(exception.ToString());
      }

      return resultObject;
    }

    private HashSet<IpSecurityRestrictionRule> ConsolidateIpAddresses(HashSet<IpSecurityRestrictionRule> ipSecRules)
    {
      var consolidatedRules = new HashSet<IpSecurityRestrictionRule>();

      foreach (var ipRule in ipSecRules)
      {
        //skip if ServiceTag or Subnet
        if (ipRule.Tag == "ServiceTag" || !string.IsNullOrEmpty(ipRule.VnetSubnetResourceId))
        {
          consolidatedRules.Add(ipRule.Clone());
          continue;
        }

        //newday service tags grouping
        if (int.TryParse(ipRule.Name!.Split('.').Last(), out int index))
        {
          var ruleName = ipRule.Name!.Remove(ipRule.Name!.LastIndexOf('.'));
          var rule = consolidatedRules.Where(x => x.Name == ruleName && string.IsNullOrEmpty(x.VnetSubnetResourceId)).FirstOrDefault();
          if (rule != null)
          {
            //combine ip rules
            rule.IpAddress = string.Concat(rule.IpAddress, ",", ipRule.IpAddress);
          }
          else
          {
            ipRule.Name = ruleName;
            consolidatedRules.Add(ipRule.Clone());
          }
          continue;
        }

        var resourceRule = consolidatedRules.Where(x => x.Name == ipRule.Name).FirstOrDefault();
        if (resourceRule != null)
        {
          resourceRule.IpAddress = string.Concat(resourceRule.IpAddress, ",", ipRule.IpAddress);
        }
        else
        {
          consolidatedRules.Add(ipRule.Clone());
        }
      }

      return SplitRules(consolidatedRules);
    }

    private HashSet<IpSecurityRestrictionRule> SplitRules(HashSet<IpSecurityRestrictionRule> consolidatedRules)
    {
      var result = new HashSet<IpSecurityRestrictionRule>();
      int divideBy = 8;
      foreach (var rule in consolidatedRules)
      {
        if (string.IsNullOrEmpty(rule.IpAddress))
        {
          result.Add(rule);
          continue;
        }

        var ipAddresses = rule.IpAddress!.Split(',').Distinct().ToArray();
        if (ipAddresses.Count() > divideBy)
        {
          var ipGroups = Split(ipAddresses, divideBy);
          int index = 0;
          foreach (var ips in ipGroups)
          {
            //add another record for the new rule
            var newRule = new IpSecurityRestrictionRule
            {
              Name = string.Concat(rule.Name, ".", index.ToString()),
              IpAddress = string.Join(",", ips),
              Action = rule.Action,
              Priority = rule.Priority,
              Tag = rule.Tag
            };
            result.Add(newRule);
            index++;
          }
        }
        else
        {
          result.Add(rule);
        }
      }
      return result;
    }

    public IEnumerable<IEnumerable<T>> Split<T>(T[] array, int size)
    {
      for (var i = 0; i < (float)array.Length / size; i++)
      {
        yield return array.Skip(i * size).Take(size);
      }
    }

    private async Task ApplyConfig(string getResponse,
      string postUrl,
      string resourceName,
      NetworkRestrictionSettings networkRestrictionSettings,
      ILogger logger,
      IRestHelper restHelper)
    {
      var existingWebConfig = JsonConvert.DeserializeObject<WebConfigModel>(getResponse);
      // Check if the deserialized object and its properties are null
      if (existingWebConfig?.Properties?.IpSecurityRestrictions == null)
      {
        logger.LogWarning("No existing IP security rules found for {resourceName} before apply.", resourceName);
      }
      else
      {
        //log existing Ip Rules
        JsonSerializerSettings logSettings = new JsonSerializerSettings { Converters = new[] { new ListKeyValuePairJsonConvertor() } };
        logger.LogInformation("{existingIpRulesCount} Existing Ip Rules detected for {resourceName} before apply, Existing Rules: {existingIpRules} ",
          existingWebConfig.Properties?.IpSecurityRestrictions?.Count ?? 0,
          resourceName,
          IpSecurityRestrictionRuleHelper.ConvertToJsonString(existingWebConfig.Properties?.IpSecurityRestrictions!));


        //log generated Ip Rules
        logger.LogInformation("{generatedIpRulesCount} Ip Rules generated for {resourceName} to apply: Generated Rules {generatedIpRules}",
          networkRestrictionSettings.IpSecRules!.Count(),
          resourceName,
          IpSecurityRestrictionRuleHelper.ConvertToJsonString(networkRestrictionSettings.IpSecRules!));


        //log ip rules that will be removed from resource
        var willbeRemovedIpRules = existingWebConfig.Properties?.IpSecurityRestrictions?.Select(e => e.IpAddress ?? e.VnetSubnetResourceId)
          .Except(networkRestrictionSettings.IpSecRules!.Select(x => x.IpAddress ?? x.VnetSubnetResourceId))
          .ToList();

        if (willbeRemovedIpRules!.Where(x => x != "Any").Any()) // rules that will be removed apart from 'Any'
        {
          var willbeRemovedRestrictions = existingWebConfig.Properties?.IpSecurityRestrictions?.Where(x => willbeRemovedIpRules!.Contains(x.IpAddress ?? x.VnetSubnetResourceId));
          logger.LogInformation("{willBeRemovedIpRulesCount} Rules will be removed from the {resourceName}. Ip Rules to be removed: {willBeRemovedIpRules}",
            willbeRemovedIpRules!.Count(),
            resourceName,
            IpSecurityRestrictionRuleHelper.ConvertToJsonString(willbeRemovedRestrictions!));
        }
      }

      //put ip rules
      var IpRulesModel = new WebConfigModel
      {
        Properties = new Properties
        {
          IpSecurityRestrictions = networkRestrictionSettings.IpSecRules != null && networkRestrictionSettings.IpSecRules?.Count > 0 ? networkRestrictionSettings.IpSecRules.ToList() : null,
          ScmIpSecurityRestrictions = networkRestrictionSettings.ScmIpSecRules != null && networkRestrictionSettings.ScmIpSecRules?.Count > 0 ? networkRestrictionSettings.ScmIpSecRules.ToList() : null
        }
      };
      var jsonBody = JsonConvert.SerializeObject(IpRulesModel, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

      var applySuccess = await Policy
        .HandleResult<bool>(r => r != true)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(5))
        .ExecuteAsync(() => ApplyAndVerify(
          restHelper,
          postUrl,
          jsonBody,
          resourceName,
          networkRestrictionSettings,
          logger));

      if (!applySuccess)
      {
        logger.LogError("Apply operation failed for {resourceName}", resourceName);
        throw new InvalidOperationException($"Apply operation failed for {resourceName}");
      }
    }

    private async Task<bool> ApplyAndVerify(
      IRestHelper restHelper,
      string postUrl,
      string jsonBody,
      string resourceName,
      NetworkRestrictionSettings networkRestrictionSettings,
      ILogger logger)
    {
      await restHelper.DoPutAsJson(postUrl, jsonBody);

      //check if it ip rules are successfully applied
      var getResponseAfterApply = await restHelper.DoGET(postUrl);
      if (string.IsNullOrEmpty(getResponseAfterApply))
      {
        logger.LogError("Failed to retrieve web config after apply for {resourceName}.", resourceName);
        return false;
      }
      return ValidateIfApplied(resourceName, getResponseAfterApply, networkRestrictionSettings!, logger);
    }

    internal bool ValidateIfApplied(string resourceName, string getResponse, NetworkRestrictionSettings settings, ILogger logger)
    {
      if (string.IsNullOrEmpty(getResponse))
        throw new InvalidOperationException($"Resource web config get request failed for {resourceName}");

      var webconfigAfterApply = JsonConvert.DeserializeObject<WebConfigModel>(getResponse);

      if (webconfigAfterApply?.Properties?.IpSecurityRestrictions == null)
      {
        logger.LogError("No IP security restrictions found in the web config for {resourceName}.", resourceName);
        return false;
      }

      JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings { Converters = new[] { new ListKeyValuePairJsonConvertor() } };

      logger.LogInformation("{rulesCountAfterApply} rules detected for {resourceName} after apply, Rules: {rulesAfterApply} ",
        webconfigAfterApply.Properties?.IpSecurityRestrictions?.Count,
        resourceName,
        JsonConvert.SerializeObject(
          webconfigAfterApply.Properties?.IpSecurityRestrictions!.Select(x =>
            new KeyValuePair<string, string>(x.Name!, x.IpAddress ?? x.VnetSubnetResourceId!))
            .ToList(),
          jsonSerializerSettings));

      var differences = ValidateIpRuleDifferences(settings.IpSecRules!.ToList(), webconfigAfterApply.Properties?.IpSecurityRestrictions);
      if (!differences.Item1)
      {
        logger.LogError("Ip rules could not be applied as expected for {resourceName}, Expected rules count: {expectedIpRulesCount}, Existing rules count: {existingIpRulesCount}",
          resourceName,
          settings.IpSecRules?.Count,
          webconfigAfterApply.Properties?.IpSecurityRestrictions?.Count);
        logger.LogError("({resourceName}) Missing Ip Restrictions: {missingIp  Restrictions}." + Environment.NewLine + "Ip Restrictions exist on the resource but not generated by DAL: {existingIpRestrictions}",
          resourceName,
          JsonConvert.SerializeObject(differences.Item2),
          JsonConvert.SerializeObject(differences.Item3));
        return false;
      }

      return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="desiredIpRules">Generated Ip Rules</param>
    /// <param name="actualIpRules">Ip Rules after apply operation</param>
    /// <returns>Item1(Are all rules successfully applied)</returns>
    /// <returns>Item2(Missing Ip Rules if there any)</returns>
    /// <returns>Item3(Ip Rules exists on resource but not defined)</returns>
    internal (bool, List<KeyValuePair<string, string>>, List<KeyValuePair<string, string>>) ValidateIpRuleDifferences(
 List<IpSecurityRestrictionRule>? desiredIpRules,
 List<IpSecurityRestrictionRule>? actualIpRules)
    {
      if (desiredIpRules == null || actualIpRules == null)
        throw new NullReferenceException("IpRules cannot be compared!");

      var missingRestrictions = new List<KeyValuePair<string, string>>();
      var existOnResourceRestrictions = new List<KeyValuePair<string, string>>();

      // Convert lists to dictionaries safely, considering null values in the list or for IpAddress/VnetSubnetResourceId
      var desiredIpRulesDic = desiredIpRules?.ToDictionary(x => x.IpAddress ?? x.VnetSubnetResourceId!) ?? new Dictionary<string, IpSecurityRestrictionRule>();
      var actualIpRulesDic = actualIpRules?.ToDictionary(x => x.IpAddress ?? x.VnetSubnetResourceId!) ?? new Dictionary<string, IpSecurityRestrictionRule>();

      // Check if all generated ip rules (desired) are in applied ip rules (actual)
      var valid = desiredIpRulesDic.Keys.Intersect(actualIpRulesDic.Keys).Count() == desiredIpRulesDic.Count;
      if (!valid)
      {
        var missingIpList = desiredIpRulesDic.Keys.Except(actualIpRulesDic.Keys).ToList();
        missingRestrictions = desiredIpRulesDic
          .Where(x => missingIpList.Contains(x.Key))
          .Select(x => new KeyValuePair<string, string>(x.Value.Name!, x.Value.IpAddress ?? x.Value.VnetSubnetResourceId!)).ToList();

        var existOnResourceIpList = actualIpRulesDic.Keys.Except(desiredIpRulesDic.Keys).ToList();
        existOnResourceRestrictions = actualIpRulesDic
          .Where(x => existOnResourceIpList.Contains(x.Key))
          .Select(x => new KeyValuePair<string, string>(x.Value.Name!, x.Value.IpAddress ?? x.Value.VnetSubnetResourceId!)).ToList();
      }

      return (valid, missingRestrictions, existOnResourceRestrictions);
    }

    internal IEnumerable<string> GetMessagesIfLimitReached(string resourceIdUri,
      string resourceName,
      NetworkRestrictionSettings networkRestrictionSettings,
      ILogger logger)
    {
      var errorMessages = new List<string>();

      if (networkRestrictionSettings.IpSecRules != null && networkRestrictionSettings.IpSecRules.Count > 512)
      {
        string jsonString = IpSecurityRestrictionRuleHelper.ConvertToJsonString(networkRestrictionSettings.IpSecRules);
        logger.LogInformation("{generatedIpRulesCount} Ip Rules generated for {resourceName} to apply: Generated Rules {generatedIpRules}",
          networkRestrictionSettings.IpSecRules.Count(),
          resourceName,
          jsonString);

        errorMessages.Add(LogMessageHelper.GetWebAppRulesLimitReachedMessage(resourceIdUri, jsonString));
      }

      if (networkRestrictionSettings.ScmIpSecRules != null && networkRestrictionSettings.ScmIpSecRules.Count > 512)
      {
        string jsonString = IpSecurityRestrictionRuleHelper.ConvertToJsonString(networkRestrictionSettings.ScmIpSecRules);
        logger.LogInformation("{generatedIpRulesCount} Ip Rules generated for {resourceName} to apply: Generated Rules {generatedIpRules}",
          networkRestrictionSettings.ScmIpSecRules.Count(),
          resourceName,
          jsonString);

        errorMessages.Add(LogMessageHelper.GetWebAppRulesLimitReachedMessage(resourceIdUri, jsonString));
      }
      return errorMessages;
    }

    public (string, string) ConvertRulesToPrintOut(NetworkRestrictionSettings networkRestrictionSettings)
    {
      throw new NotImplementedException();
    }
  }
}