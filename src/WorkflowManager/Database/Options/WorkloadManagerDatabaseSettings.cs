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

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.WorkflowManager.Database.Options
{
    public class WorkloadManagerDatabaseSettings
    {
        [ConfigurationKeyName("ConnectionString")]
        public string ConnectionString { get; set; } = null!;

        [ConfigurationKeyName("DatabaseName")]
        public string DatabaseName { get; set; } = null!;

        [ConfigurationKeyName("PayloadCollectionName")]
        public string PayloadCollectionName { get; set; } = null!;

        [ConfigurationKeyName("WorkflowCollectionName")]
        public string WorkflowCollectionName { get; set; } = null!;

        [ConfigurationKeyName("WorkflowInstanceCollectionName")]
        public string WorkflowInstanceCollectionName { get; set; } = null!;
    }
}
