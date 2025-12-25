using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DynamicAllowListingLib;
using DynamicAllowListingLib.Helpers;
using DynamicAllowListingLib.Models;
using DynamicAllowListingLib.Models.AzureResources;
using DynamicAllowListingLib.Services;
using DynamicAllowListingLib.ServiceTagManagers;
using DynamicAllowListingLib.ServiceTagManagers.AzureManager;
using DynamicAllowListingLib.ServiceTagManagers.Model;
using DynamicAllowListingLib.ServiceTagManagers.NewDayManager;
using DynamicAllowListingLib.SettingsValidation.InternalAndThirdPartyValidator;
using DynamicAllowListingLib.SettingsValidation.ResourceDependencyValidator;
using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.ApplicationInsights.WorkerService;

class Program
{
  static void Main(string[] args)
  {
    var host = new HostBuilder()
        .ConfigureFunctionsWebApplication()
        .ConfigureServices((context, services) =>
        {
          // Configuration from host context
          var configuration = context.Configuration;

          string appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? throw new InvalidOperationException("Application Insights connection string is missing.");

          services.AddApplicationInsightsTelemetryWorkerService((ApplicationInsightsServiceOptions options) => options.ConnectionString = appInsightsConnectionString);
          services.ConfigureFunctionsApplicationInsights();


          // Application Insights
          //services.AddApplicationInsightsTelemetryWorkerService();
          //services.ConfigureFunctionsApplicationInsights();

          // Service registration from original Startup.cs
          services.AddScoped<ISettingLoader, SettingLoader>();
          services.AddScoped<IServiceTagManagerProvider, ServiceTagManagerProvider>();

          // Service Tag Managers
          services.AddScoped<IServiceTagManager, InternalAndThirdPartyServiceTagsManager>();
          services.AddScoped<IServiceTagManager, AzureServiceTagsManager>();
          services.AddScoped<IServiceTagManager, AzureWebServiceTagManager>();

          // Validators
          services.AddScoped<ISettingValidator<ResourceDependencyInformation>, ResourceDependencyInformationValidator>();
          services.AddScoped<ISettingValidator<InternalAndThirdPartyServiceTagSetting>, InternalAndThirdPartyServiceTagValidator>();

          services.AddScoped<IAzureServiceTagsJsonHelper, AzureServiceTagsJsonHelper>();
          services.AddScoped<IRestHelper, RestHelper>();
          
          services.AddScoped<IResourceGraphExplorerService, ResourceGraphExplorerService>();
          services.AddScoped<IDynamicAllowListingService, DynamicAllowListingService>();

          // Register CosmosClient as a singleton
          services.AddSingleton((s) =>
          {
            var cosmosDbConnectionString = configuration["CosmosDBConnectionString"];
            if (string.IsNullOrEmpty(cosmosDbConnectionString))
            {
              throw new Exception("CosmosDBConnectionString was found to be null or empty.");
            }
            return new CosmosClient(cosmosDbConnectionString);
          });

          // Register CosmosClient as a singleton
          services.AddSingleton((s) =>
          {
            var cosmosDbConnectionString = configuration["CosmosDBConnectionString"];
            if (string.IsNullOrEmpty(cosmosDbConnectionString))
            {
              throw new Exception("CosmosDBConnectionString was found to be null or empty.");
            }
             
            var databaseName = configuration["DatabaseName"] ?? Constants.DatabaseName;
            if (string.IsNullOrEmpty(databaseName))
            {
              throw new Exception("DatabaseName was found to be null or empty.");
            }

            return new CosmosDbSettings
            {
              CosmosDBConnectionString = cosmosDbConnectionString,
              DatabaseName = databaseName
            };
          });

          // Register HttpClient as a singleton
          services.AddHttpClient();

          // Persistence Managers
          services.AddScoped<IInternalAndThirdPartyServiceTagPersistenceManager, InternalAndThirdPartyServiceTagPersistenceManager>();
          services.AddScoped<IPersistenceManager<AzureSubscription>, AzureSubscriptionsPersistenceManager>();
          services.AddScoped<IPersistenceManager<ServiceTag>, ServiceTagsPersistenceManager>();

          // Persistence Services setup with Cosmos DB settings
          services.AddScoped<IResourceDependencyInformationPersistenceService, ResourceDependencyInformationPersistenceService> ();


          // IP Restriction Services
          services.AddScoped<IIpRestrictionService<HashSet<AzureSubscription>>, IpRestrictionServiceForAzureSubscription>();
          services.AddScoped<IIpRestrictionService<HashSet<ServiceTag>>, IpRestrictionServiceForServiceTag>();

          // Azure Resource Services
          services.AddScoped<IAzureResourceServiceFactory, AzureResourceServiceFactory>();
          services.AddTransient<IAzureResourceService, AzureResourceService>();

          services.AddScoped<IIpRestrictionRuleGeneratorService, IpRestrictionRuleGeneratorService>();
          services.AddScoped<IAzureResourceClassProvider, AzureResourceClassProvider>();
          services.AddScoped<IAzureResourceJsonConvertor, AzureResourceJsonConvertor>();

          services.AddScoped<IAzureResource, WebSite>();
          services.AddScoped<IAzureResource, PublicIpAddress>();
          services.AddScoped<IAzureResource, CosmosDb>();
          services.AddScoped<IAzureResource, Storage>();
          services.AddScoped<IAzureResource, KeyVault>();
          services.AddScoped<IAzureResource, SqlServer>();
          services.AddScoped<IAzureResource, FrontDoor>();

          services.AddScoped<IWebClient, NewDayWebClient>();
        })
        .ConfigureLogging(logging =>
        {
          logging.Services.Configure<LoggerFilterOptions>(options =>
          {
            LoggerFilterRule? defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
              options.Rules.Remove(defaultRule);
            }
          });
        })
        .Build();

    host.Run();
  }
 
}
