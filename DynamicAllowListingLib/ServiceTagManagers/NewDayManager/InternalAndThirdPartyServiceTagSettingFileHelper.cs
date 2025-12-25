using System;
using System.IO;
using System.Reflection;

namespace DynamicAllowListingLib.ServiceTagManagers.NewDayManager
{
  public class InternalAndThirdPartyServiceTagSettingFileHelper
  {
    private const string ParentFolder = "Data";
    private const string FileName = "InternalAndThirdPartyServiceTagSetting.json";

    public static string GetFilePath()
    {
      // Get the directory of the currently executing assembly
      var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (string.IsNullOrEmpty(binDirectory) || binDirectory == null)
      {
        throw new Exception("Unable to locate Assembly.GetExecutingAssembly.");
      }
      // Construct the full path to the file within the bin directory
      string fullPathToFileInBin = Path.Combine(binDirectory, ParentFolder, FileName);
      if (File.Exists(fullPathToFileInBin))
      {
        return fullPathToFileInBin;
      }
      // Move up to the root directory and check for the file outside the bin folder
      var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));
      string fullPathToFileOutsideBin = Path.Combine(rootDirectory, ParentFolder, FileName);
      if (!File.Exists(fullPathToFileOutsideBin))
      {  
        // If the file cannot be found in either location, throw a more informative exception
        throw new Exception($"Unable to locate {ParentFolder}/{FileName}. Tried {fullPathToFileInBin} and {fullPathToFileOutsideBin}.");
      }
      return fullPathToFileOutsideBin;
    }
  }
}