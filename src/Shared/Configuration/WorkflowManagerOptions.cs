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
using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.WorkflowManager.Configuration
{
    public class WorkflowManagerOptions
    {
        /// <summary>
        /// Name of the key for retrieve database connection string.
        /// </summary>
        public const string DatabaseConnectionStringKey = "WorkflowManagerDatabase";

        /// <summary>
        /// Represents the <c>messaging</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("messaging")]
        public MessageBrokerConfiguration Messaging { get; set; }

        /// <summary>
        /// Represents the <c>storage</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("storage")]
        public StorageConfiguration Storage { get; set; }

        /// <summary>
        /// Represents the <c>taskManager</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("taskManager")]
        public TaskManagerConfiguration TaskManager { get; set; }

        /// <summary>
        /// Represents the <c>endpointSettings</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("endpointSettings")]
        public EndpointSettings EndpointSettings { get; set; }

        [ConfigurationKeyName("taskTimeoutMinutes")]
        public double TaskTimeoutMinutes { get; set; } = 60;

        public TimeSpan TaskTimeout { get => TimeSpan.FromMinutes(TaskTimeoutMinutes); }

        /// <summary>
        /// Represents the <c>backgroundServiceSettings</c> section of the configuration file.
        /// </summary>
        public BackgroundServiceSettings BackgroundServiceSettings { get; set; }

        [ConfigurationKeyName("argoTtlStatergySeconds")]
        public int ArgoTtlStatergySeconds { get; set; } = 60 * 60 * 24 * 2; // 2 days before the pods get automatically cleaned up from argo

        [ConfigurationKeyName("minArgoTtlStatergySeconds")]
        public int MinArgoTtlStatergySeconds { get; set; } = 30; // time to get logs before cleanup !

        public WorkflowManagerOptions()
        {
            Messaging = new MessageBrokerConfiguration();
            TaskManager = new TaskManagerConfiguration();
            Storage = new StorageConfiguration();
            EndpointSettings = new EndpointSettings();
            BackgroundServiceSettings = new BackgroundServiceSettings();
        }
    }
}
