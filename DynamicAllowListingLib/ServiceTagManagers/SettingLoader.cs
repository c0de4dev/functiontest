using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.ServiceTagManagers
{
  public interface ISettingLoader
  {
    public Task<T> LoadSettingsFromFile<T>(string filePath);
    public Task<T> LoadSettingsFromString<T>(string jsonString);
  }
  public class SettingLoader : ISettingLoader
  {
    public Task<T> LoadSettingsFromFile<T>(string filePath)
    {
      if (string.IsNullOrEmpty(filePath))
        throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

      if (!File.Exists(filePath))
        throw new FileNotFoundException($"The specified file was not found: {filePath}", filePath);

      var jsonString = File.ReadAllText(filePath);
      return LoadSettingsFromString<T>(jsonString);
    }

    public Task<T> LoadSettingsFromString<T>(string jsonString)
    {
      if (string.IsNullOrWhiteSpace(jsonString))
        throw new ArgumentException("The JSON string cannot be null or empty.", nameof(jsonString));

      var setting = JsonConvert.DeserializeObject<T>(jsonString)
                    ?? throw new InvalidOperationException($"Deserialization of type {typeof(T)} failed.");


      //var setting = JsonConvert.DeserializeObject<T>(jsonString);
      return Task.FromResult((T)Convert.ChangeType(setting!, typeof(T)));
    }
  }
}