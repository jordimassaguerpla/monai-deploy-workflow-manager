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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.WorkflowManager.Common.Interfaces;
using Monai.Deploy.WorkflowManager.Common.Services;
using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkflowManager.Database.Interfaces;
using Monai.Deploy.WorkflowManager.Storage.Services;
using Moq;
using Xunit;

namespace Monai.Deploy.WorkflowManager.Common.Tests.Services
{
    public class PayloadServiceTests
    {
        private IPayloadService PayloadService { get; set; }

        private readonly Mock<IPayloadRepsitory> _payloadRepository;
        private readonly Mock<IDicomService> _dicomService;
        private readonly Mock<ILogger<PayloadService>> _logger;

        public PayloadServiceTests()
        {
            _payloadRepository = new Mock<IPayloadRepsitory>();
            _dicomService = new Mock<IDicomService>();
            _logger = new Mock<ILogger<PayloadService>>();

            PayloadService = new PayloadService(_payloadRepository.Object, _dicomService.Object, _logger.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidWorkflowAndDicom_ReturnsTrue()
        {
            var workflowRequest = new WorkflowRequestEvent
            {
                Timestamp = DateTime.UtcNow,
                Bucket = "bucket",
                CalledAeTitle = "aetitle",
                CallingAeTitle = "aetitle",
                CorrelationId = Guid.NewGuid().ToString(),
                PayloadId = Guid.NewGuid(),
                Workflows = new List<string> { Guid.NewGuid().ToString() },
                FileCount = 0
            };

            var patientDetails = new PatientDetails
            {
                PatientDob = new DateTime(1996, 02, 05),
                PatientId = Guid.NewGuid().ToString(),
                PatientName = "Steve",
                PatientSex = "male"
            };

            var expected = new Payload
            {
                Timestamp = workflowRequest.Timestamp,
                Bucket = workflowRequest.Bucket,
                FileCount = workflowRequest.FileCount,
                CalledAeTitle = workflowRequest.CalledAeTitle,
                CallingAeTitle = workflowRequest.CallingAeTitle,
                CorrelationId = workflowRequest.CorrelationId,
                PayloadId = workflowRequest.PayloadId.ToString(),
                PatientDetails = patientDetails,
                Workflows = workflowRequest.Workflows
            };


            _payloadRepository.Setup(w => w.CreateAsync(It.IsAny<Payload>())).ReturnsAsync(true);

            _dicomService.Setup(d => d.GetPayloadPatientDetailsAsync(workflowRequest.PayloadId.ToString(), workflowRequest.Bucket)).ReturnsAsync(patientDetails);

            var result = await PayloadService.CreateAsync(workflowRequest);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateAsync_ValidWorkflowPayloadExists_ReturnsExisting()
        {
            var workflowRequest = new WorkflowRequestEvent
            {
                Timestamp = DateTime.UtcNow,
                Bucket = "bucket",
                CalledAeTitle = "aetitle",
                CallingAeTitle = "aetitle",
                CorrelationId = Guid.NewGuid().ToString(),
                PayloadId = Guid.NewGuid(),
                Workflows = new List<string> { Guid.NewGuid().ToString() },
                FileCount = 0
            };

            var patientDetails = new PatientDetails
            {
                PatientDob = new DateTime(1996, 02, 05),
                PatientId = Guid.NewGuid().ToString(),
                PatientName = "Steve",
                PatientSex = "male"
            };

            var expected = new Payload
            {
                Timestamp = workflowRequest.Timestamp,
                Bucket = workflowRequest.Bucket,
                FileCount = workflowRequest.FileCount,
                CalledAeTitle = workflowRequest.CalledAeTitle,
                CallingAeTitle = workflowRequest.CallingAeTitle,
                CorrelationId = workflowRequest.CorrelationId,
                PayloadId = workflowRequest.PayloadId.ToString(),
                PatientDetails = patientDetails,
                Workflows = workflowRequest.Workflows
            };


            _payloadRepository.Setup(p => p.GetByIdAsync(expected.PayloadId)).ReturnsAsync(expected);

            var result = await PayloadService.CreateAsync(workflowRequest);

            Assert.NotNull(result);
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        [Fact]
        public async Task CreateAsync_NullPayload_ReturnsThrowsException() => await Assert.ThrowsAsync<ArgumentNullException>(async () => await PayloadService.CreateAsync(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        [Fact]
        public async Task GetByIdAsync_ValidId_ReturnsPayload()
        {
            var patientDetails = new PatientDetails
            {
                PatientDob = new DateTime(1996, 02, 05),
                PatientId = Guid.NewGuid().ToString(),
                PatientName = "Steve",
                PatientSex = "male"
            };

            var payload = new Payload
            {
                Timestamp = DateTime.UtcNow,
                PatientDetails = patientDetails,
                Bucket = "bucket",
                CalledAeTitle = "aetitle",
                CallingAeTitle = "aetitle",
                CorrelationId = Guid.NewGuid().ToString(),
                PayloadId = Guid.NewGuid().ToString(),
                Workflows = new List<string> { Guid.NewGuid().ToString() },
            };

            _payloadRepository.Setup(p => p.GetByIdAsync(payload.PayloadId)).ReturnsAsync(payload);
            var result = await PayloadService.GetByIdAsync(payload.PayloadId);

            result.Should().BeEquivalentTo(payload);
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        [Fact]
        public async Task GetByIdAsync_NullId_ReturnsThrowsException() => await Assert.ThrowsAsync<ArgumentNullException>(async () => await PayloadService.GetByIdAsync(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        [Fact]
        public async Task GetAll_ReturnsPayloads()
        {
            var patientDetails = new PatientDetails
            {
                PatientDob = new DateTime(1996, 02, 05),
                PatientId = Guid.NewGuid().ToString(),
                PatientName = "Steve",
                PatientSex = "male"
            };

            var payload = new List<Payload>
            {
                new Payload
                {
                    Timestamp = DateTime.UtcNow,
                    PatientDetails = patientDetails,
                    Bucket = "bucket",
                    CalledAeTitle = "aetitle",
                    CallingAeTitle = "aetitle",
                    CorrelationId = Guid.NewGuid().ToString(),
                    PayloadId = Guid.NewGuid().ToString(),
                    Workflows = new List<string> { Guid.NewGuid().ToString() }
                }
            };

            _payloadRepository.Setup(p => p.GetAllAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(() => payload);

            var result = await PayloadService.GetAllAsync();

            result.Should().BeEquivalentTo(payload);
        }
    }
}
