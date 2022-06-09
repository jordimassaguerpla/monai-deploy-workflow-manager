﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using FluentAssertions;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Storage.Configuration;
using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkloadManager.WorkfowExecuter.Common;
using Xunit;

namespace Monai.Deploy.WorkflowManager.WorkflowExecuter.Tests.Common
{
    public class EventMapperTests
    {
        [Fact]
        public void ToTaskDispatchEvent_ValidAeTitleWorkflowRequest_ReturnesTrue()
        {
            var task = new TaskExecution
            {
                ExecutionId = Guid.NewGuid().ToString(),
                TaskType = "taskType",
                TaskPluginArguments = new Dictionary<string, string>
                {
                    { "key", "value" }
                },
                TaskId = Guid.NewGuid().ToString(),
                Status = TaskExecutionStatus.Created,
                InputArtifacts = new Dictionary<string, string>
                {
                    { "key", "value" }
                },
                Metadata = new Dictionary<string, object> { },
                OutputDirectory = "minio/workflowid/taskid"
            };

            var configuration = new StorageServiceConfiguration
            {
                Settings = new Dictionary<string, string>
                {
                    { "securedConnection", "false" },
                     { "endpoint", "localhost" },
                    { "bucket", "test-bucket"}
                }
            };

            var workflowId = Guid.NewGuid().ToString();
            var correlationId = Guid.NewGuid().ToString();

            var expectedTask = new TaskDispatchEvent
            {
                WorkflowInstanceId = workflowId,
                TaskId = task.TaskId,
                ExecutionId = task.ExecutionId.ToString(),
                CorrelationId = correlationId,
                Status = TaskExecutionStatus.Created,
                TaskPluginType = task.TaskType,
                Inputs = new List<Messaging.Common.Storage>
                {
                    new Messaging.Common.Storage
                    {
                        SecuredConnection = bool.Parse(configuration.Settings["securedConnection"]),
                        Endpoint = configuration.Settings["endpoint"],
                        Bucket = configuration.Settings["bucket"],
                        RelativeRootPath = "value",
                        Name = "key"
                    }
                },
                Metadata = task.Metadata,
                TaskPluginArguments = new Dictionary<string, string>
                {
                    { "key", "value" }
                },
                IntermediateStorage = new Messaging.Common.Storage
                {
                    Bucket = configuration.Settings["bucket"],
                    Endpoint = configuration.Settings["endpoint"],
                    Name = task.TaskId,
                    RelativeRootPath = "minio/workflowid/taskid/tmp",
                    SecuredConnection = bool.Parse(configuration.Settings["securedConnection"])
                }
            };

            var taskDispatch = EventMapper.ToTaskDispatchEvent(task, workflowId, correlationId, configuration);

            taskDispatch.Should().BeEquivalentTo(expectedTask, options =>
                options.Excluding(t => t.CorrelationId));
        }
    }
}
