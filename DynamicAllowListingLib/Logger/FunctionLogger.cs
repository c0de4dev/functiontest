using Microsoft.Extensions.Logging;
using System;

namespace DynamicAllowListingLib.Logger
{
  public static partial class FunctionLogger
  {
    // Define a logger for informational messages
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Function {functionName} started at {startTime}")]
    public static partial void LogFunctionStart(ILogger logger, string functionName, DateTime startTime);

    // Define a logger for errors with exception logging
    [LoggerMessage(EventId = 2000, Level = LogLevel.Error, Message = "Function {functionName} encountered an error: {errorMessage}")]
    public static partial void LogFunctionError(ILogger logger, string functionName, string errorMessage, Exception ex);

    // Define a logger for successful completion
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Function {functionName} completed successfully")]
    public static partial void LogFunctionCompletion(ILogger logger, string functionName);


    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Inside Method: {methodName}")]
    public static partial void MethodStart(ILogger logger, string methodName);

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Exception: , Message: {message}")]
    public static partial void MethodException(ILogger logger, Exception ex, string message = "Exception Occured");

    [LoggerMessage(EventId = 6000, Level = LogLevel.Information, Message = "{message}")]
    public static partial void MethodInformation(ILogger logger, string message);

    [LoggerMessage(EventId = 7000, Level = LogLevel.Warning, Message = "{message}")]
    public static partial void MethodWarning(ILogger logger, string message);

    [LoggerMessage(EventId = 8000, Level = LogLevel.Error, Message = "{message}")]
    public static partial void MethodError(ILogger logger, string message);
  }
}
