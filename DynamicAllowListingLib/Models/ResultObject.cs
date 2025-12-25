using System.Collections.Generic;
using System.Linq;

namespace DynamicAllowListingLib
{
  public class ResultObject
  {
    public bool Success
    {
      get
      {
        return !Errors.Any();
      }
    }
    public List<string> Information { get; set; } = new List<string>();
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public OutputData? Data { get; set; } = null;
    public void Merge(ResultObject resultObject)
    {
      Information.AddRange(resultObject.Information);
      Errors.AddRange(resultObject.Errors);
      Warnings.AddRange(resultObject.Warnings);

      if (resultObject.Data != null)
        Data = resultObject.Data;

      FunctionNames.AddRange(resultObject.FunctionNames);
      InvocationIDs.AddRange(resultObject.InvocationIDs);
      OperationIDs.AddRange(resultObject.OperationIDs);
    }
    public class OutputData
    {
      public string? SubnetIds { get; set; } 
      public string? IPs { get; set; }
    }
    public List<string> FunctionNames { get; set; } = new List<string>();

    public List<string> InvocationIDs { get; set; } = new List<string>();
    public List<string> OperationIDs { get; set; } = new List<string>();
  }
}