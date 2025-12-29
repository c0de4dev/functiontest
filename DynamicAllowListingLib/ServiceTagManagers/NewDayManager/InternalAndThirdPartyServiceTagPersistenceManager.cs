using DynamicAllowListingLib.Logging;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
      if (internalAndThirdPartyServiceTagSetting == null)
      {
        _logger.LogSettingsObjectNull();
        throw new ArgumentNullException(nameof(internalAndThirdPartyServiceTagSetting), "Settings object cannot be null.");
      }

      var stopwatch = Stopwatch.StartNew();
      bool azureSubscriptionsUpdated = false;
      bool serviceTagsUpdated = false;

      try
      {
        // Validate settings before proceeding
        if ((internalAndThirdPartyServiceTagSetting.AzureSubscriptions == null || !internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Any()) &&
            (internalAndThirdPartyServiceTagSetting.ServiceTags == null || !internalAndThirdPartyServiceTagSetting.ServiceTags.Any()))
        {
          _logger.LogSettingsEmpty();
          throw new InvalidOperationException("Both AzureSubscriptions and ServiceTags are null or empty. Nothing to update.");
        }

        _logger.LogStartingDatabaseUpdate();

        // Update Azure Subscriptions
        if (internalAndThirdPartyServiceTagSetting.AzureSubscriptions != null && internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Any())
        {
          _logger.LogUpdatingAzureSubscriptions(internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Count);
          await _azureSubscriptionsPersistenceManager.UpdateDatabaseStateTo(internalAndThirdPartyServiceTagSetting.AzureSubscriptions);
          _logger.LogAzureSubscriptionsUpdateCompleted(internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Count);
          azureSubscriptionsUpdated = true;
        }
        else
        {
          // Gap 4.1 fix: Log when Azure Subscriptions update is skipped
          _logger.LogSkippingAzureSubscriptionsUpdate();
        }

        // Update Service Tags
        if (internalAndThirdPartyServiceTagSetting.ServiceTags != null && internalAndThirdPartyServiceTagSetting.ServiceTags.Any())
        {
          _logger.LogUpdatingServiceTags(internalAndThirdPartyServiceTagSetting.ServiceTags.Count);
          await _serviceTagsPersistenceManager.UpdateDatabaseStateTo(internalAndThirdPartyServiceTagSetting.ServiceTags);
          _logger.LogServiceTagsUpdateCompleted(internalAndThirdPartyServiceTagSetting.ServiceTags.Count,0,0);
          serviceTagsUpdated = true;
        }
        else
        {
          // Gap 4.2 fix: Log when Service Tags update is skipped
          _logger.LogSkippingServiceTagsUpdate();
        }

        _logger.LogDatabaseUpdateCompleted();
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        throw;
      }
      finally
      {
        stopwatch.Stop();
        // Gap 4.3 fix: Log operation completion with duration
        _logger.LogPersistenceManagerUpdateCompleted(
            azureSubscriptionsUpdated,
            serviceTagsUpdated,
            stopwatch.ElapsedMilliseconds);
      }
    }

    public async Task<InternalAndThirdPartyServiceTagSetting> GetFromDatabase()
    {
      var internalAndThirdPartyServiceTagSetting = new InternalAndThirdPartyServiceTagSetting();
      try
      {
        // Retrieve Azure Subscriptions
        _logger.LogRetrievingAzureSubscriptions();
        var azureSubscriptions = await _azureSubscriptionsPersistenceManager.GetFromDatabase();
        internalAndThirdPartyServiceTagSetting.AzureSubscriptions = azureSubscriptions ?? new List<AzureSubscription>();
        _logger.LogRetrievedAzureSubscriptions(internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Count);

        // Retrieve Service Tags
        _logger.LogRetrievingServiceTags();
        var serviceTags = await _serviceTagsPersistenceManager.GetFromDatabase();
        internalAndThirdPartyServiceTagSetting.ServiceTags = serviceTags ?? new List<ServiceTag>();
        _logger.LogRetrievedServiceTagsCount(internalAndThirdPartyServiceTagSetting.ServiceTags.Count);

        // Log final result
        _logger.LogDatabaseSettingsRetrieved(
          internalAndThirdPartyServiceTagSetting.AzureSubscriptions.Count,
          internalAndThirdPartyServiceTagSetting.ServiceTags.Count);
      }
      catch (Exception ex)
      {
        _logger.LogOperationException(ex);
        throw;
      }
      return internalAndThirdPartyServiceTagSetting;
    }
  }
}