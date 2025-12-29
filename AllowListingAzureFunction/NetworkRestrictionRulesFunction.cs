using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using AllowListingAzureFunction.Logging;

namespace AllowListingAzureFunction
{
  public class NetworkRestrictionRulesFunction
  {
    private const string FunctionName = "DefaultTags";

    private readonly ICustomTelemetryService _telemetry;
    private readonly ILogger<NetworkRestrictionRulesFunction> _logger;

    public NetworkRestrictionRulesFunction(
        ICustomTelemetryService telemetry,
        ILogger<NetworkRestrictionRulesFunction> logger)
    {
      _telemetry = telemetry;
      _logger = logger;
    }

    [Function(FunctionName)]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "defaultrules/{subscriptionId}")] HttpRequest req,
        string subscriptionId)
    {
      var operationId = Guid.NewGuid().ToString();

      // Set correlation context for HTTP trigger
      CorrelationContext.SetCorrelationId(operationId);

      using var loggerScope = _logger.BeginFunctionScope(FunctionName, operationId);
      using var operation = _telemetry.StartOperation(FunctionName,
          new Dictionary<string, string>
          {
            ["OperationId"] = operationId,
            ["SubscriptionId"] = subscriptionId ?? "Unknown"
          });

      try
      {
        _logger.LogHttpFunctionStarted(FunctionName, operationId, req.Method);
        _logger.LogHttpRequestReceived(FunctionName, req.Method, req.Path);

        operation.AddProperty("SubscriptionId", subscriptionId ?? "Unknown");

        // ============================================================
        // STEP 1: Resolve and read the settings file
        // ============================================================
        string path = InternalAndThirdPartyServiceTagSettingFileHelper.GetFilePath();
        _logger.LogSettingsFilePathResolved(FunctionName, path);

        if (!File.Exists(path))
        {
          _logger.LogHttpFunctionErrorResponse(FunctionName, operationId, 500, $"Settings file not found: {path}");
          operation.SetFailed("Settings file not found");
          return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        string jsonContent = File.ReadAllText(path);
        _logger.LogSettingsFileRead(FunctionName, jsonContent.Length);

        // ============================================================
        // STEP 2: Deserialize the JSON settings
        // ============================================================
        var root = JsonConvert.DeserializeObject<Root>(jsonContent);

        if (root == null)
        {
          _logger.LogHttpFunctionErrorResponse(FunctionName, operationId, 500, "Failed to deserialize settings JSON");
          operation.SetFailed("Failed to deserialize settings JSON");
          return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        _logger.LogSettingsFileLoaded(
            FunctionName,
            root.AzureSubscriptions?.Count ?? 0,
            root.ServiceTags?.Count ?? 0);

        // ============================================================
        // STEP 3: Find the requested subscription
        // ============================================================
        _logger.LogSubscriptionLookup(
            FunctionName,
            subscriptionId ?? "null",
            root.AzureSubscriptions?.Count ?? 0);

        var sub = root.AzureSubscriptions?.FirstOrDefault(x => x.Id == subscriptionId?.Trim());

        if (sub == null)
        {
          _logger.LogHttpFunctionErrorResponse(FunctionName, operationId, 400, $"Subscription not found: {subscriptionId}");
          operation.SetFailed("Subscription not found");
          return new BadRequestObjectResult("Please provide a valid subscription Id.");
        }

        string subscriptionName = sub.Name ?? string.Empty;
        _logger.LogSubscriptionMatched(FunctionName, subscriptionId!, subscriptionName);

        operation.AddProperty("SubscriptionName", subscriptionName);

        // ============================================================
        // STEP 4: Build the response - filter service tags for subscription
        // ============================================================
        var outputList = new DefaultTagJsonObject
        {
          SubscriptionId = sub.Id ?? string.Empty,
          SubscriptionName = subscriptionName
        };

        int totalServiceTags = root.ServiceTags?.Count ?? 0;
        int matchedServiceTags = 0;
        int totalAddresses = 0;

        if (root.ServiceTags != null)
        {
          foreach (var serviceTag in root.ServiceTags)
          {
            // Find the addresses for this subscription
            var addressesForSubscription = serviceTag.AddressesPerSubscription
                ?.FirstOrDefault(x => x.SubscriptionId == subscriptionId)?.Addresses;

            if (addressesForSubscription != null && addressesForSubscription.Any())
            {
              var addressCount = addressesForSubscription.Count;

              outputList.ServiceTags.Add(new DefaultTagJsonServiceTagObject
              {
                Name = serviceTag.Name ?? string.Empty,
                Addresses = addressesForSubscription.ToList()
              });

              matchedServiceTags++;
              totalAddresses += addressCount;

              _logger.LogServiceTagIncluded(FunctionName, serviceTag.Name ?? "Unknown", addressCount);
            }
            else
            {
              _logger.LogServiceTagSkipped(FunctionName, serviceTag.Name ?? "Unknown", subscriptionId!);
            }
          }
        }

        // Log the filtering summary
        _logger.LogServiceTagsFiltered(
            FunctionName,
            subscriptionId!,
            totalServiceTags,
            matchedServiceTags,
            totalAddresses);

        // ============================================================
        // STEP 5: Serialize and return the response
        // ============================================================
        var settings = new JsonSerializerSettings
        {
          ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        var jsonResult = JsonConvert.SerializeObject(outputList, settings);

        // Log the comprehensive response summary
        _logger.LogDefaultTagsResponsePrepared(
            FunctionName,
            subscriptionId!,
            subscriptionName,
            outputList.ServiceTags.Count,
            totalAddresses,
            (long)operation.Elapsed.TotalMilliseconds);

        _logger.LogHttpFunctionCompleted(FunctionName, operationId, 200, (long)operation.Elapsed.TotalMilliseconds);
        operation.SetSuccess();

        return new OkObjectResult(outputList);
      }
      catch (Exception ex)
      {
        _logger.LogHttpFunctionFailed(ex, FunctionName, operationId);
        operation.SetFailed(ex.Message);
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
          ["OperationId"] = operationId,
          ["Function"] = FunctionName,
          ["SubscriptionId"] = subscriptionId ?? "Unknown"
        });

        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
      }
      finally
      {
        // Clear correlation context at end of function
        CorrelationContext.Clear();
      }
    }

    #region Internal Model Classes for JSON Deserialization

    internal class AzureSubscription
    {
      public string? Name { get; set; }
      public string? Id { get; set; }
    }

    internal class AllowedSubscription
    {
      public string? SubscriptionName { get; set; }
      public string? IsMandatory { get; set; }
    }

    internal class ServiceTag
    {
      public string? Name { get; set; }
      public List<string> AddressPrefixes { get; set; } = new List<string>();
      public List<string> SubnetIds { get; set; } = new List<string>();
      public List<AllowedSubscription> AllowedSubscriptions { get; set; } = new List<AllowedSubscription>();
      public List<AddressesPerSubscription> AddressesPerSubscription { get; set; } = new List<AddressesPerSubscription>();
    }

    internal class AddressesPerSubscription
    {
      public string? SubscriptionId { get; set; }
      public List<string> Addresses { get; set; } = new List<string>();
    }

    internal class Root
    {
      public List<AzureSubscription> AzureSubscriptions { get; set; } = new List<AzureSubscription>();
      public List<ServiceTag> ServiceTags { get; set; } = new List<ServiceTag>();
    }

    #endregion

    #region Output Model Classes for JSON Serialization

    public class DefaultTagJsonObject
    {
      public string SubscriptionId { get; set; } = string.Empty;
      public string SubscriptionName { get; set; } = string.Empty;
      public List<DefaultTagJsonServiceTagObject> ServiceTags { get; set; } = new();
    }

    public class DefaultTagJsonServiceTagObject
    {
      public string Name { get; set; } = string.Empty;
      public List<string> Addresses { get; set; } = new();
    }

    #endregion
  }
}