﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Diagnostics;
using BoDi;
using Microsoft.Extensions.Configuration;
using Monai.Deploy.WorkflowManager.IntegrationTests.POCO;
using Monai.Deploy.WorkflowManager.IntegrationTests.Support;

namespace Monai.Deploy.WorkflowManagerIntegrationTests
{
    /// <summary>
    /// Hooks class for setting up the integration tests.
    /// </summary>
    [Binding]
    public class Hooks
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hooks"/> class.
        /// </summary>
        /// <param name="objectContainer"></param>
        public Hooks(IObjectContainer objectContainer)
        {
            ObjectContainer = objectContainer;
        }

        private static RabbitPublisher? WorkflowPublisher { get; set; }

        private static RabbitConsumer? TaskDispatchConsumer { get; set; }

        private static MongoClientUtil? MongoClient { get; set; }

        private IObjectContainer ObjectContainer { get; set; }

        /// <summary>
        /// Runs before all tests to create static implementions of Rabbit and Mongo clients as well as starting the WorkflowManager using WebApplicationFactory.
        /// </summary>
        [BeforeTestRun(Order = 0)]
        public static void Init()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.Development.json")
                .Build();

            TestExecutionConfig.RabbitConfig.Host = "localhost";
            TestExecutionConfig.RabbitConfig.Port = 15672;
            TestExecutionConfig.RabbitConfig.User = "admin";
            TestExecutionConfig.RabbitConfig.Password = "admin";
            TestExecutionConfig.RabbitConfig.VirtualHost = "monaideploy";
            TestExecutionConfig.RabbitConfig.Exchange = "monaideploy";
            TestExecutionConfig.RabbitConfig.WorkflowRequestQueue = config.GetValue<string>("WorkflowManager:messaging:topics:workflowRequest");
            TestExecutionConfig.RabbitConfig.TaskDispatchQueue = "md.tasks.dispatch";
            TestExecutionConfig.RabbitConfig.TaskCallbackQueue = "md.tasks.callback";
            TestExecutionConfig.RabbitConfig.WorkflowCompleteQueue = config.GetValue<string>("WorkflowManager:messaging:topics:exportComplete");

            TestExecutionConfig.MongoConfig.ConnectionString = config.GetValue<string>("WorkloadManagerDatabase:ConnectionString");
            TestExecutionConfig.MongoConfig.Database = config.GetValue<string>("WorkloadManagerDatabase:DatabaseName");
            TestExecutionConfig.MongoConfig.WorkflowCollection = config.GetValue<string>("WorkloadManagerDatabase:WorkflowCollectionName");
            TestExecutionConfig.MongoConfig.WorkflowInstanceCollection = config.GetValue<string>("WorkloadManagerDatabase:WorkflowInstanceCollectionName");

            WorkflowPublisher = new RabbitPublisher(RabbitConnectionFactory.GetConnectionFactory(), TestExecutionConfig.RabbitConfig.Exchange, TestExecutionConfig.RabbitConfig.WorkflowRequestQueue);
            TaskDispatchConsumer = new RabbitConsumer(RabbitConnectionFactory.GetConnectionFactory(), TestExecutionConfig.RabbitConfig.Exchange, TestExecutionConfig.RabbitConfig.TaskDispatchQueue);
            MongoClient = new MongoClientUtil();
            WebAppFactory.SetupWorkflowManger();
        }

        /// <summary>
        /// Runs before all tests to check that the WorkflowManager consumer is started.
        /// </summary>
        /// <returns>Error if the WorkflowManager consumer is not started.</returns>
        /// <exception cref="Exception"></exception>
        [BeforeTestRun(Order = 1)]
        public static async Task CheckWorkflowConsumerStarted()
        {
            var response = await WebAppFactory.GetConsumers();
            var content = response.Content.ReadAsStringAsync().Result;

            for (var i = 1; i <= 10; i++)
            {
                if (string.IsNullOrEmpty(content) || content == "[]")
                {
                    Debug.Write($"Workflow consumer not started. Recheck times {i}");
                    response = await WebAppFactory.GetConsumers();
                    content = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    Debug.Write("Consumer started. Integration tests will begin!");
                    break;
                }

                if (i == 10)
                {
                    throw new Exception("Workflow Mangaer Consumer not started! Integration tests will not continue");
                }

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Adds Rabbit and Mongo clients to Specflow IoC container for test scenario being executed.
        /// </summary>
        [BeforeScenario]
        public void SetUp()
        {
            ObjectContainer.RegisterInstanceAs(WorkflowPublisher);
            ObjectContainer.RegisterInstanceAs(TaskDispatchConsumer);
            ObjectContainer.RegisterInstanceAs(MongoClient);
        }

        /// <summary>
        /// Runs after all tests to closes Rabbit connections.
        /// </summary>
        [AfterTestRun(Order = 0)]
        public static void TearDownMongo()
        {
            MongoClient.DeleteAllWorkflowDocuments();
            MongoClient.DeleteAllWorkflowInstances();
        }

        /// <summary>
        /// Runs after all tests to closes Rabbit connections.
        /// </summary>
        [AfterTestRun(Order = 1)]
        public static void TearDownRabbit()
        {
            if (WorkflowPublisher != null)
            {
                WorkflowPublisher.CloseConnection();
            }

            if (TaskDispatchConsumer != null)
            {
                TaskDispatchConsumer.CloseConnection();
            }
        }
    }
}