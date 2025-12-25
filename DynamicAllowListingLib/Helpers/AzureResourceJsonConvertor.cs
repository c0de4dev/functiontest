using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DynamicAllowListingLib.Helpers
{
  public interface IAzureResourceJsonConvertor { }

  /// <summary>
  /// Convert Azure resource graph explorer response to the resource type
  /// </summary>
  public class AzureResourceJsonConvertor : JsonConverter, IAzureResourceJsonConvertor
  {
    private readonly IAzureResourceClassProvider _classProvider;

    public AzureResourceJsonConvertor(IAzureResourceClassProvider classProvider)
    {
      _classProvider = classProvider;
    }

    public override bool CanConvert(Type objectType)
    {
      return typeof(List<IAzureResource>).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
      try
      {
        JObject item = JObject.Load(reader);
        var resources = new List<IAzureResource>();

        //get api response rows
        var rows = item["data"];
        if (rows != null && rows.HasValues)
        {
          foreach (var row in rows)
          {
            //cast to full object
            var model = GetModel(row);
            resources.Add(model);
          }
        }
        return resources;
      }
      catch
      {
        throw;
      }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
      if (writer == null) throw new ArgumentNullException(nameof(writer));
      if (value == null) throw new ArgumentNullException(nameof(value));
      throw new NotImplementedException();
    }

    internal IAzureResource GetModel(JToken row)
    {
      // Check if 'row' is null or missing required keys
      if (row == null)
      {
        throw new ArgumentNullException(nameof(row), "The input row cannot be null.");
      }

      var type = row["type"]?.ToString() ?? throw new InvalidOperationException("'type' field is missing or null.");


      //get correspondent class
      var resourceInstance = _classProvider.GetResourceClass(type);

      if (resourceInstance == null)
      {
        throw new InvalidOperationException($"No resource class found for type '{type}'.");
      }

      // Safely extract properties from 'row'
      string id = row["id"]?.ToString() ?? throw new InvalidOperationException("'id' field is missing or null.");
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

      //cast to full object
      string fullObjectString = JsonConvert.SerializeObject(data);
      Type objectType = resourceInstance.GetType();
      var model = JsonConvert.DeserializeObject(fullObjectString, objectType);

#pragma warning disable CS8600,CS8602 // Converting null literal or possible null value to non-nullable type.
      ((IAzureResource)model).Properties =
        JsonConvert.DeserializeObject(row["properties"].ToString(), resourceInstance.Properties!.GetType());
#pragma warning restore CS8600,CS8602 // Converting null literal or possible null value to non-nullable type.

      return (IAzureResource)model;
    }
  }
}