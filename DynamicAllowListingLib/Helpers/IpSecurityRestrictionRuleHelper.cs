using DynamicAllowListingLib.Helpers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DynamicAllowListingLib
{
  public static class IpSecurityRestrictionRuleHelper
  {
    public static bool MatchesAny(IpSecurityRestrictionRule ruleToCheck, HashSet<IpSecurityRestrictionRule> ruleSet)
    {
      bool matchesAny = false;
      foreach (var rule in ruleSet)
      {
        if (rule.Matches(ruleToCheck))
        {
          matchesAny = true;
          break;
        }
      }
      return matchesAny;
    }

    public static HashSet<IpSecurityRestrictionRule> GetIpSecurityRestrictions(JsonDocument document) => GetIpSecurityRestrictionRules(document, "ipSecurityRestrictions");

    public static HashSet<IpSecurityRestrictionRule> GetScmIpSecurityRestrictions(JsonDocument document) => GetIpSecurityRestrictionRules(document, "scmIpSecurityRestrictions");

    private static HashSet<IpSecurityRestrictionRule> GetIpSecurityRestrictionRules(JsonDocument document, string arrayElementName)
    {
      HashSet<IpSecurityRestrictionRule> ipSecurityRestrictionRules = new HashSet<IpSecurityRestrictionRule>();
      if (document.RootElement.TryGetProperty("properties", out JsonElement propertiesJsonElement))
      {
        if (propertiesJsonElement.TryGetProperty(arrayElementName, out JsonElement ipSecurityRestrictionsJsonElement))
        {
          if (ipSecurityRestrictionsJsonElement.ValueKind == JsonValueKind.Array)
          {
            foreach (var restrictionRule in ipSecurityRestrictionsJsonElement.EnumerateArray())
            {
              var ipSecRule = ConvertToIpSecurityRestrictionRule(restrictionRule);
              if (ipSecRule != null)
              {
                ipSecurityRestrictionRules.Add(ipSecRule);
              }
            }
          }
        }
      }
      return ipSecurityRestrictionRules;
    }

    /// <summary>
    /// Convert to <see cref="IpSecurityRestrictionRule"/> if the values are valid.
    /// Validity logic:
    /// - Rule IPAddress is not 'Any'.
    /// - Rule name is not same as a deleted resource.
    /// </summary>
    /// <param name="jsonElement">The rule in JSON format as type <see cref="JsonElement"/></param>
    /// <returns><see cref="IpSecurityRestrictionRule"/> object if rule is valid, else null.</returns>
    public static IpSecurityRestrictionRule? ConvertToIpSecurityRestrictionRule(JsonElement jsonElement)
    {
      string? ruleName = jsonElement.TryGetProperty("name", out JsonElement ruleNameJsonElement) ? ruleNameJsonElement.GetString() : "";

      IpSecurityRestrictionRule? ipSecurityRestrictionRule = new IpSecurityRestrictionRule { Name = ruleName };

      var ipAddressFound = jsonElement.TryGetProperty("ipAddress", out JsonElement ipAddress);
      if (ipAddressFound)
      {
        ipSecurityRestrictionRule.IpAddress = ipAddress.ToString();
        if (ipSecurityRestrictionRule.IpAddress != null && ipSecurityRestrictionRule.IpAddress.Equals("Any"))
        {
          return null;
        }
      }

      var vnetSubnetResourceIdFound =
        jsonElement.TryGetProperty("vnetSubnetResourceId", out JsonElement vnetSubnetResourceId);
      if (vnetSubnetResourceIdFound)
      {
        ipSecurityRestrictionRule.VnetSubnetResourceId = vnetSubnetResourceId.GetString();
      }

      if (!ipAddressFound && !vnetSubnetResourceIdFound)
      {
        return null;
      }

      if (jsonElement.TryGetProperty("action", out JsonElement actionJsonElement))
      {
        ipSecurityRestrictionRule.Action = actionJsonElement.GetString();
      }
      else
      {
        return null;
      }

      if (jsonElement.TryGetProperty("priority", out JsonElement priorityJsonElement))
      {
        ipSecurityRestrictionRule.Priority = priorityJsonElement.GetInt32();
      }
      else
      {
        return null;
      }
      return ipSecurityRestrictionRule;
    }

    public static string ConvertToJsonString(IEnumerable<IpSecurityRestrictionRule> rules)
    {
      JsonSerializerSettings logSettings = new JsonSerializerSettings { Converters = new JsonConverter[] { new ListKeyValuePairJsonConvertor() } };
      return JsonConvert.SerializeObject(
        rules.Select(x => new KeyValuePair<string, string>(x.Name!, x.IpAddress ?? x.VnetSubnetResourceId!)).ToList(),
        logSettings);
    }

  }
}