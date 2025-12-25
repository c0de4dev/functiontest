using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DynamicAllowListingLib.Helpers
{
  public interface IAzureResourceJsonConvertor { }

  /// <summary>
  /// Convert Azure resource graph explorer response to the resource type.
  /// Uses structured logging for improved observability.
  /// </summary>
  public class AzureResourceJsonConvertor : JsonConverter, IAzureResourceJsonConvertor
  {
    private readonly IAzureResourceClassProvider _classProvider;
    private readonly ILogger<AzureResourceJsonConvertor>? _logger;

    public AzureResourceJsonConvertor(IAzureResourceClassProvider classProvider)
    {
      _classProvider = classProvider ?? throw new ArgumentNullException(nameof(classProvider));
    }

    public AzureResourceJsonConvertor(
        IAzureResourceClassProvider classProvider,
        ILogger<AzureResourceJsonConvertor> logger)
    {
      _classProvider = classProvider ?? throw new ArgumentNullException(nameof(classProvider));
      _logger = logger;
    }

    public override bool CanConvert(Type objectType)
    {
      return typeof(List<IAzureResource>).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
      var expectedType = objectType.Name;

      using (_logger?.BeginJsonConversionScope("ReadJson"))
      {
        _logger?.LogJsonConversionStart(expectedType);

        try
        {
          JObject item = JObject.Load(reader);
          var resources = new List<IAzureResource>();

          // Get API response rows
          var rows = item["data"];
          if (rows != null && rows.HasValues)
          {
            foreach (var row in rows)
            {
              try
              {
                var model = GetModel(row);
                resources.Add(model);
              }
              catch (InvalidOperationException ex)
              {
                // Log but continue processing other resources
                _logger?.LogJsonConversionFailed(ex, "ResourceRow");
              }
            }
          }

          _logger?.LogJsonConversionComplete(resources.Count);

          return resources;
        }
        catch (Exception ex)
        {
          _logger?.LogJsonConversionFailed(ex, expectedType);
          throw;
        }
      }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
      if (writer == null) throw new ArgumentNullException(nameof(writer));
      if (value == null) throw new ArgumentNullException(nameof(value));
      throw new NotImplementedException("WriteJson is not supported for AzureResourceJsonConvertor.");
    }

    internal IAzureResource GetModel(JToken row)
    {
      // Check if 'row' is null or missing required keys
      if (row == null)
      {
        throw new ArgumentNullException(nameof(row), "The input row cannot be null.");
      }

      var type = row["type"]?.ToString();
      if (string.IsNullOrEmpty(type))
      {
        _logger?.LogMissingRequiredField("type");
        throw new InvalidOperationException("'type' field is missing or null.");
      }

      var id = row["id"]?.ToString();
      if (string.IsNullOrEmpty(id))
      {
        _logger?.LogMissingRequiredField("id");
        throw new InvalidOperationException("'id' field is missing or null.");
      }

      _logger?.LogResourceRowProcessing(type, id);

      // Get correspondent class
      IAzureResource? resourceInstance;
      try
      {
        resourceInstance = _classProvider.GetResourceClass(type);
      }
      catch (NotImplementedException)
      {
        _logger?.LogResourceTypeNotImplemented(type);
        throw;
      }

      if (resourceInstance == null)
      {
        _logger?.LogResourceTypeNotImplemented(type);
        throw new InvalidOperationException($"No resource class found for type '{type}'.");
      }

      // Safely extract properties from 'row'
      string name = row["name"]?.ToString() ?? throw new InvalidOperationException("'name' field is missing or null.");
      string location = row["location"]?.ToString() ?? throw new InvalidOperationException("'location' field is missing or null.");

      // Create wrapper class to hold the values
      dynamic data = new
      {
        Id = id,
        Name = name,
        Location = location,
        Type = type
      };

      // Cast to full object
      string fullObjectString = JsonConvert.SerializeObject(data);
      Type objectType = resourceInstance.GetType();
      var model = JsonConvert.DeserializeObject(fullObjectString, objectType);

      if (model == null)
      {
        throw new InvalidOperationException($"Failed to deserialize resource of type '{type}'.");
      }

#pragma warning disable CS8600,CS8602 // Converting null literal or possible null value to non-nullable type.
      var propertiesToken = row["properties"];
      if (propertiesToken != null && resourceInstance.Properties != null)
      {
        ((IAzureResource)model).Properties =
            JsonConvert.DeserializeObject(propertiesToken.ToString(), resourceInstance.Properties.GetType());
      }
#pragma warning restore CS8600,CS8602

      return (IAzureResource)model;
    }
  }
}