using System;

namespace DynamicAllowListingLib
{
  public class AzureFunctionExecutionContext : IAzureFunctionExecutionContext
  {
    public AzureFunctionExecutionContext(Microsoft.Azure.WebJobs.ExecutionContext executionContext)
    {
      InvocationId = executionContext.InvocationId;
      FunctionName = executionContext.FunctionName;
      FunctionDirectory = executionContext.FunctionDirectory;
      FunctionAppDirectory = executionContext.FunctionAppDirectory;
    }
    public Guid InvocationId { get; set; }
    public string FunctionName { get; set; }
    public string FunctionDirectory { get; set; }
    public string FunctionAppDirectory { get; set; }
  }
}