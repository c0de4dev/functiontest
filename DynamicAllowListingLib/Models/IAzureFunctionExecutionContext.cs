using System;

namespace DynamicAllowListingLib
{
  public interface IAzureFunctionExecutionContext
  {
    Guid InvocationId { get; set; }
    string FunctionName { get; set; }
    string FunctionDirectory { get; set; }
    string FunctionAppDirectory { get; set; }
  }
}