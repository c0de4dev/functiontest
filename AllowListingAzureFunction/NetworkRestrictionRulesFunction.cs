using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Linq;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using Microsoft.Azure.Functions.Worker;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Azure.Storage.Queues.Models;

namespace AllowListingAzureFunction
{
  public class NetworkRestrictionRulesFunction
  {        
    private readonly TelemetryClient _telemetryClient;
    public NetworkRestrictionRulesFunction(TelemetryClient telemetryClient)
    {
      _telemetryClient = telemetryClient;
    }

    [Function("DefaultTags")]
    public IActionResult Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "defaultrules/{subscriptionId}")] HttpRequest req,
    string subscriptionId)
    {
      var operationId = Guid.NewGuid().ToString(); // Unique operation ID for tracing
      using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("DefaultTags", operationId))
      {
        // Enrich telemetry with the correlation ID
        _telemetryClient.Context.Operation.Id = operationId;
        try
        {
          // Log incoming subscription ID for debugging
          _telemetryClient.TrackTrace($"Received request for subscriptionId: {subscriptionId}", SeverityLevel.Information);

          // Read the JSON file
          string path = InternalAndThirdPartyServiceTagSettingFileHelper.GetFilePath();
          if (!File.Exists(path))
          {
            _telemetryClient.TrackTrace($"File not found: {path}", SeverityLevel.Error);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
          }

          string jsonContent = File.ReadAllText(path);
          var root = JsonConvert.DeserializeObject<Root>(jsonContent);

          // Log if the root object is null after deserialization
          if (root == null)
          {
            _telemetryClient.TrackTrace("Failed to deserialize JSON content.", SeverityLevel.Error);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
          }

          // Find the subscription
          var sub = root?.AzureSubscriptions.FirstOrDefault(x => x.Id == subscriptionId.Trim());
          if (sub == null)
          {
            _telemetryClient.TrackTrace($"Subscription with Id {subscriptionId} not found.", SeverityLevel.Warning);
            return new BadRequestObjectResult("Please provide a valid subscription Id.");
          }

          string subscriptionName = sub.Name!;

          List<string> subnetIds = new List<string>();
          List<string> addressPrefixes = new List<string>();

          // Process service tags and collect subnet IDs and address prefixes
          foreach (var serviceTag in root!.ServiceTags)
          {
            // Log each service tag processing
            _telemetryClient.TrackTrace($"Processing service tag: {serviceTag.Name}", SeverityLevel.Information);

            // Check if allowed subscriptions are mandatory and valid
            if (serviceTag.AllowedSubscriptions
                .Any(x => string.IsNullOrEmpty(x.IsMandatory) || string.IsNullOrEmpty(x.SubscriptionName)))
            {
              _telemetryClient.TrackTrace($"Service tag {serviceTag.Name} has invalid allowed subscription configuration.", SeverityLevel.Error);
              throw new InvalidDataException($"Service Tag is broken: {serviceTag.Name}");
            }

            // Check if the subscription is allowed and mandatory
            if (serviceTag.AllowedSubscriptions.Any(allowedSubscription =>
                allowedSubscription.SubscriptionName!.Equals(subscriptionName, StringComparison.OrdinalIgnoreCase) &&
                allowedSubscription.IsMandatory!.Equals("True", StringComparison.OrdinalIgnoreCase)))
            {
              if (serviceTag.SubnetIds != null)
              {
                subnetIds.AddRange(serviceTag.SubnetIds);
              }

              if (serviceTag.AddressPrefixes != null)
              {
                addressPrefixes.AddRange(serviceTag.AddressPrefixes);
              }
            }
          }

          // Stop the telemetry operation and return the result
          _telemetryClient.StopOperation(operation);

          return new OkObjectResult(new { AddressPrefixes = addressPrefixes, SubnetIds = subnetIds });
        }
        catch (Exception ex)
        {
          _telemetryClient.TrackException(ex, new Dictionary<string, string>{
                              { "SubscriptionID", subscriptionId },
                              { "OperationId", operationId }
                          });
          // Return a generic error response to the client
          return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
      }
    }

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
    }
    internal class Root
    {
      public List<AzureSubscription> AzureSubscriptions { get; set; } = new List<AzureSubscription>();
      public List<ServiceTag> ServiceTags { get; set; } = new List<ServiceTag>();
    }
  }
}
