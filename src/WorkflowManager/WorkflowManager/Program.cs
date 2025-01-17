/*
 * Copyright 2022 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.Configuration;
using Monai.Deploy.Storage;
using Monai.Deploy.Storage.Configuration;
using Monai.Deploy.WorkflowManager.Configuration;
using Monai.Deploy.WorkflowManager.Database.Interfaces;
using Monai.Deploy.WorkflowManager.Database.Options;
using Monai.Deploy.WorkflowManager.Database.Repositories;
using Monai.Deploy.WorkflowManager.MonaiBackgroundService;
using Monai.Deploy.WorkflowManager.Services;
using Monai.Deploy.WorkflowManager.Services.DataRetentionService;
using Monai.Deploy.WorkflowManager.Services.Http;
using Monai.Deploy.WorkflowManager.Validators;
using MongoDB.Driver;
using NLog;
using NLog.LayoutRenderers;
using NLog.Web;

namespace Monai.Deploy.WorkflowManager
{
#pragma warning disable SA1600 // Elements should be documented

    internal class Program
    {
        protected Program()
        {
        }

        internal static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    var env = builderContext.HostingEnvironment;
                    config
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();
                })
                .ConfigureLogging((builderContext, configureLogging) =>
                {
                    configureLogging.ClearProviders();
                    configureLogging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigureServices(hostContext, services);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.CaptureStartupErrors(true);
                    webBuilder.UseStartup<Startup>();
                })
               .UseNLog();

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddOptions<WorkflowManagerOptions>()
                .Bind(hostContext.Configuration.GetSection("WorkflowManager"))
                .PostConfigure(options =>
                {
                });
            services.AddOptions<MessageBrokerServiceConfiguration>()
                .Bind(hostContext.Configuration.GetSection("WorkflowManager:messaging"))
                .PostConfigure(options =>
                {
                });
            services.AddOptions<StorageServiceConfiguration>()
                .Bind(hostContext.Configuration.GetSection("WorkflowManager:storage"))
                .PostConfigure(options =>
                {
                });
            services.AddOptions<EndpointSettings>()
                .Bind(hostContext.Configuration.GetSection("WorkflowManager:endpointSettings"))
                .PostConfigure(options =>
                {
                });
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WorkflowManagerOptions>, ConfigurationValidator>());

            services.AddSingleton<ConfigurationValidator>();
            services.AddTransient<WorkflowValidator>();

            services.AddSingleton<DataRetentionService>();

#pragma warning disable CS8603 // Possible null reference return.
            services.AddHostedService(p => p.GetService<DataRetentionService>());
#pragma warning restore CS8603 // Possible null reference return.

            // Services
            services.AddTransient<IFileSystem, FileSystem>();
            services.AddHttpClient();

            // Mongo DB
            services.Configure<WorkloadManagerDatabaseSettings>(hostContext.Configuration.GetSection("WorkloadManagerDatabase"));
            services.AddSingleton<IMongoClient, MongoClient>(s => new MongoClient(hostContext.Configuration["WorkloadManagerDatabase:ConnectionString"]));
            services.AddTransient<IWorkflowRepository, WorkflowRepository>();
            services.AddTransient<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
            services.AddTransient<IPayloadRepsitory, PayloadRepository>();
            services.AddTransient<ITasksRepository, TasksRepository>();

            // StorageService
            services.AddMonaiDeployStorageService(hostContext.Configuration.GetSection("WorkflowManager:storage:serviceAssemblyName").Value, HealthCheckOptions.ServiceHealthCheck);

            // MessageBroker
            services.AddMonaiDeployMessageBrokerPublisherService(hostContext.Configuration.GetSection("WorkflowManager:messaging:publisherServiceAssemblyName").Value);
            services.AddMonaiDeployMessageBrokerSubscriberService(hostContext.Configuration.GetSection("WorkflowManager:messaging:subscriberServiceAssemblyName").Value);

            services.AddHostedService(p => p.GetService<DataRetentionService>());

            services.AddWorkflowExecutor(hostContext);

            services.AddHttpContextAccessor();
            services.AddSingleton<IUriService>(p =>
            {
                var accessor = p.GetRequiredService<IHttpContextAccessor>();
                var request = accessor?.HttpContext?.Request;
                var uri = string.Concat(request?.Scheme, "://", request?.Host.ToUriComponent());
                var newUri = new Uri(uri);
                return new UriService(newUri);
            });

            services.AddHostedService<Worker>();
        }

        private static void Main(string[] args)
        {
            var version = typeof(Program).Assembly;
            var assemblyVersionNumber = version.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.1";

            var logger = ConfigureNLog(assemblyVersionNumber);
            logger.Info($"Initializing MONAI Deploy Workflow Manager v{assemblyVersionNumber}");

            var host = CreateHostBuilder(args).Build();
            host.Run();
            logger.Info("MONAI Deploy Workflow Manager shutting down.");

            NLog.LogManager.Shutdown();
        }

        private static Logger ConfigureNLog(string assemblyVersionNumber)
        {
            LayoutRenderer.Register("servicename", logEvent => typeof(Program).Namespace);
            LayoutRenderer.Register("serviceversion", logEvent => assemblyVersionNumber);
            LayoutRenderer.Register("machinename", logEvent => Environment.MachineName);

            return LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
        }

#pragma warning restore SA1600 // Elements should be documented
    }
}
