using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DynamicAllowListingLib.Helpers
{
  public class ListKeyValuePairJsonConvertor : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
      List<KeyValuePair<string, string>>? list = value as List<KeyValuePair<string, string>>;

      writer.WriteStartArray();
      foreach (var item in list!)
      {
        writer.WriteStartObject();
        writer.WritePropertyName(item.Key);
        writer.WriteValue(item.Value);
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(List<KeyValuePair<string, string>>);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }
  }
}