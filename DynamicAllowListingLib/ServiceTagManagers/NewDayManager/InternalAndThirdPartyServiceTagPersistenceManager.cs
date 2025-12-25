using DynamicAllowListingLib.Logger;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.ServiceTagManagers.NewDayManager
{
  public interface IInternalAndThirdPartyServiceTagPersistenceManager
  {
    /// <summary>
    /// Update database state to the provided items. WARNING: Anything not present in the object is lost.
    /// </summary>
    /// <param name="internalAndThirdPartyServiceTagSetting"><see cref="InternalAndThirdPartyServiceTagSetting"/> object to update database with.</param>
    Task UpdateDatabaseStateTo(InternalAndThirdPartyServiceTagSetting internalAndThirdPartyServiceTagSetting);
    /// <summary>
    /// Create and return <see cref="InternalAndThirdPartyServiceTagSetting"/> object from database.
    /// </summary>
    /// <returns><see cref="InternalAndThirdPartyServiceTagSetting"/> object.</returns>
    Task<InternalAndThirdPartyServiceTagSetting> GetFromDatabase();
  }

  public class InternalAndThirdPartyServiceTagPersistenceManager : IInternalAndThirdPartyServiceTagPersistenceManager
  {
    private readonly IPersistenceManager<AzureSubscription> _azureSubscriptionsPersistenceManager;
    private readonly IPersistenceManager<ServiceTag> _serviceTagsPersistenceManager;
    private readonly ILogger<InternalAndThirdPartyServiceTagPersistenceManager> _logger;

    public InternalAndThirdPartyServiceTagPersistenceManager(
      IPersistenceManager<AzureSubscription> azureSubscriptionsPersistenceManager,
      IPersistenceManager<ServiceTag> serviceTagsPersistenceManager,
      ILogger<InternalAndThirdPartyServiceTagPersistenceManager> logger)
    {
      _azureSubscriptionsPersistenceManager = azureSubscriptionsPersistenceManager;
      _serviceTagsPersistenceManager = serviceTagsPersistenceManager;
      _logger = logger;
    }

    public async Task UpdateDatabaseStateTo(InternalAndThirdPartyServiceTagSetting internalAndThirdPartyServiceTagSetting)
    {
      FunctionLogger.MethodStart(_logger, nameof(UpdateDatabaseStateTo));

      if (internalAndThirdPartyServiceTagSetting == null)
      {
        FunctionLogger.MethodError(_logger, "Provided settings object is null.");
        throw new ArgumentNullException(nameof(internalAndThirdPartyServiceTagSetting), "Settings object cannot be null.");
      }
      try
      {        
        // Validate settings before proceeding
        if ((internalAndThirdPartyServiceTagSetting.AzureSubscriptions == null || !internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Any()) &&
            (internalAndThirdPartyServiceTagSetting.ServiceTags == null || !internalAndThirdPartyServiceTagSetting.ServiceTags.Any()))
        {
          string errorMessage = "Both AzureSubscriptions and ServiceTags are null or empty. Nothing to update.";
          FunctionLogger.MethodWarning(_logger, errorMessage);
          throw new InvalidOperationException(errorMessage);
        }
        FunctionLogger.MethodInformation(_logger, "Starting database state update.");

        // Update Azure Subscriptions
        if (internalAndThirdPartyServiceTagSetting.AzureSubscriptions != null && internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Any())
        {
          FunctionLogger.MethodInformation(_logger, $"Updating Azure Subscriptions. Count: {internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Count}");
          await _azureSubscriptionsPersistenceManager.UpdateDatabaseStateTo(internalAndThirdPartyServiceTagSetting.AzureSubscriptions);
        }
        // Update Service Tags
        if (internalAndThirdPartyServiceTagSetting.ServiceTags != null && internalAndThirdPartyServiceTagSetting.ServiceTags.Any())
        {
          FunctionLogger.MethodInformation(_logger, $"Updating Service Tags. Count: {internalAndThirdPartyServiceTagSetting.ServiceTags.Count}");
          await _serviceTagsPersistenceManager.UpdateDatabaseStateTo(internalAndThirdPartyServiceTagSetting.ServiceTags);
        }
        FunctionLogger.MethodInformation(_logger, "Database state update completed successfully.");
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
    }

    public async Task<InternalAndThirdPartyServiceTagSetting> GetFromDatabase()
    {
      FunctionLogger.MethodStart(_logger, nameof(GetFromDatabase));
      var internalAndThirdPartyServiceTagSetting = new InternalAndThirdPartyServiceTagSetting();
      try
      {
        // Retrieve Azure Subscriptions
        FunctionLogger.MethodInformation(_logger, "Retrieving Azure Subscriptions from database.");
        var azureSubscriptions = await _azureSubscriptionsPersistenceManager.GetFromDatabase();
        internalAndThirdPartyServiceTagSetting.AzureSubscriptions = azureSubscriptions ?? new List<AzureSubscription>();
        FunctionLogger.MethodInformation(_logger, $"Retrieved {internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Count} Azure Subscriptions from database.");

        // Retrieve Service Tags
        FunctionLogger.MethodInformation(_logger, "Retrieving Service Tags from database.");
        var serviceTags = await _serviceTagsPersistenceManager.GetFromDatabase();
        internalAndThirdPartyServiceTagSetting.ServiceTags = serviceTags ?? new List<ServiceTag>();
        FunctionLogger.MethodInformation(_logger, $"Retrieved {internalAndThirdPartyServiceTagSetting.ServiceTags.Count} Service Tags from database.");

        // Log final result
        string successLog = $"Successfully retrieved database settings. " +
                            $"Number of Azure Subscriptions: {internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Count}, " +
                            $"Number of Service Tags: {internalAndThirdPartyServiceTagSetting.ServiceTags.Count}";
        FunctionLogger.MethodInformation(_logger, successLog);
      }
      catch (Exception ex)
      {
        FunctionLogger.MethodException(_logger, ex);
        throw;
      }
      return internalAndThirdPartyServiceTagSetting;
    }
  }
}