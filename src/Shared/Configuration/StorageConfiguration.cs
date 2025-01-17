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

using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Monai.Deploy.Storage.Configuration;

namespace Monai.Deploy.WorkflowManager.Configuration
{
    public class StorageConfiguration : StorageServiceConfiguration
    {
        private readonly IFileSystem _fileSystem;

        public StorageConfiguration() : this(new FileSystem())
        { }

        public StorageConfiguration(IFileSystem fileSystem) => _fileSystem = fileSystem ?? throw new System.ArgumentNullException(nameof(fileSystem));

        /// <summary>
        /// Gets or sets the watermark for disk usage with default value of 75%,
        /// meaning that MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retreiving data via DICOMweb when used disk space
        /// is above the watermark.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("watermarkPercent")]
        public uint Watermark { get; set; } = 75;

        /// <summary>
        /// Gets or sets the reserved disk space for the MONAI Deploy Informatics Gateway with default value of 5GB.
        /// MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retreiving data via DICOMweb when available disk space
        /// is less than the value.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("reserveSpaceGB")]
        public uint ReserveSpaceGB { get; set; } = 5;

        /// <summary>
        /// Gets or sets retry options relate to saving files to temporary storage, processing payloads and uploading payloads to the storage service.
        /// </summary>
        [ConfigurationKeyName("reties")]
        public RetryConfiguration Retries { get; set; } = new RetryConfiguration();

        /// <summary>
        /// Gets or set number of threads used for payload upload. Default is 1;
        /// </summary>
        public int Concurrency { get; set; } = 1;

        /// <summary>
        /// Gets or sets the name of the bucket where payloads are uploaded to.
        /// </summary>
        [ConfigurationKeyName("bucketName")]
        public string StorageServiceBucketName { get; set; } = string.Empty;
    }
}
