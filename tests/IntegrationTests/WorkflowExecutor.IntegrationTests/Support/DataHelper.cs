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

using Monai.Deploy.Messaging.Events;
using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkflowManager.IntegrationTests.Models;
using Monai.Deploy.WorkflowManager.Models;
using Monai.Deploy.WorkflowManager.WorkflowExecutor.IntegrationTests.TestData;
using Polly;
using Polly.Retry;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Monai.Deploy.WorkflowManager.IntegrationTests.Support
{
    public class DataHelper
    {
        public WorkflowRequestMessage WorkflowRequestMessage = new WorkflowRequestMessage();
        public List<WorkflowInstance> WorkflowInstances = new List<WorkflowInstance>();
        public PatientDetails PatientDetails { get; set; } = new PatientDetails();
        public TaskUpdateEvent TaskUpdateEvent = new TaskUpdateEvent();
        public ExportCompleteEvent ExportCompleteEvent = new ExportCompleteEvent();
        public List<TaskDispatchEvent> TaskDispatchEvents = new List<TaskDispatchEvent>();
        public List<ExportRequestEvent> ExportRequestEvents = new List<ExportRequestEvent>();
        public List<WorkflowRevision> WorkflowRevisions = new List<WorkflowRevision>();
        public List<Workflow> Workflows = new List<Workflow>();
        public List<Payload> Payload = new List<Payload>();
        private RetryPolicy<List<WorkflowInstance>> RetryWorkflowInstances { get; set; }
        private RetryPolicy<List<TaskDispatchEvent>> RetryTaskDispatches { get; set; }
        private RetryPolicy<List<ExportRequestEvent>> RetryExportRequests { get; set; }
        private RetryPolicy<List<Payload>> RetryPayloadCollections { get; set; }
        private RabbitConsumer TaskDispatchConsumer { get; set; }
        private RabbitConsumer ExportRequestConsumer { get; set; }
        private MongoClientUtil MongoClient { get; set; }
        public string PayloadId { get; private set; }
        public string BucketId { get; internal set; }
        public List<WorkflowInstance> SeededWorkflowInstances { get; internal set; }

        public DataHelper(RabbitConsumer taskDispatchConsumer, RabbitConsumer exportRequestConsumer, MongoClientUtil mongoClient)
        {
            ExportRequestConsumer = exportRequestConsumer;
            TaskDispatchConsumer = taskDispatchConsumer;
            MongoClient = mongoClient;
            RetryWorkflowInstances = Policy<List<WorkflowInstance>>.Handle<Exception>().WaitAndRetry(retryCount: 20, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
            RetryTaskDispatches = Policy<List<TaskDispatchEvent>>.Handle<Exception>().WaitAndRetry(retryCount: 20, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
            RetryExportRequests = Policy<List<ExportRequestEvent>>.Handle<Exception>().WaitAndRetry(retryCount: 20, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
            RetryPayloadCollections = Policy<List<Payload>>.Handle<Exception>().WaitAndRetry(retryCount: 20, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
        }

        public TasksRequest GetTaskRequestTestData(string name)
        {
            var taskRequest = TaskRequestsTestData.TestData.FirstOrDefault(c => c.Name.Equals(name));

            if (taskRequest?.TaskRequest == null)
            {
                throw new Exception($"Task Request {name} does not have any applicable test data, please check and try again!");
            }

            return taskRequest.TaskRequest;
        }

        public WorkflowRevision GetWorkflowRevisionTestData(string name)
        {
            var workflowRevision = WorkflowRevisionsTestData.TestData.FirstOrDefault(c => c.Name.Equals(name));

            if (workflowRevision != null)
            {
                if (workflowRevision.WorkflowRevision != null)
                {
                    WorkflowRevisions.Add(workflowRevision.WorkflowRevision);
                    return workflowRevision.WorkflowRevision;
                }
                else
                {
                    throw new Exception($"Workflow {name} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Workflow {name} does not have any applicable test data, please check and try again!");
            }
        }

        public WorkflowRevision GetWorkflowRevisionTestDataByIndex(int index)
        {
            var workflowRevision = WorkflowRevisionsTestData.TestData[index];

            if (workflowRevision != null)
            {
                if (workflowRevision.WorkflowRevision != null)
                {
                    WorkflowRevisions.Add(workflowRevision.WorkflowRevision);
                    return workflowRevision.WorkflowRevision;
                }
                else
                {
                    throw new Exception($"Workflow at index {index} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Workflow at index {index} does not have any applicable test data, please check and try again!");
            }
        }

        public Workflow GetWorkflowObjectTestData(string name)
        {
            var workflow = WorkflowObjectsTestData.TestData.FirstOrDefault(c => c.Name.Equals(name));

            if (workflow != null)
            {
                if (workflow.Workflow != null)
                {
                    Workflows.Add(workflow.Workflow);
                    return workflow.Workflow;
                }
                else
                {
                    throw new Exception($"Workflow object {name} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Workflow object {name} does not have any applicable test data, please check and try again!");
            }
        }

        public WorkflowInstance GetWorkflowInstanceTestData(string name)
        {
            var workflowInstance = WorkflowInstancesTestData.TestData.FirstOrDefault(c => c.Name.Contains(name));

            if (workflowInstance != null)
            {
                if (workflowInstance.WorkflowInstance != null)
                {
                    WorkflowInstances.Add(workflowInstance.WorkflowInstance);

                    return workflowInstance.WorkflowInstance;
                }
                else
                {
                    throw new Exception($"Workflow Intance {name} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Workflow Intance {name} does not have any applicable test data, please check and try again!");
            }
        }

        public WorkflowInstance GetWorkflowInstanceTestDataByIndex(int index)
        {
            var workflowInstance = WorkflowInstancesTestData.TestData[index];

            if (workflowInstance != null)
            {
                if (workflowInstance.WorkflowInstance != null)
                {
                    WorkflowInstances.Add(workflowInstance.WorkflowInstance);
                    return workflowInstance.WorkflowInstance;
                }
                else
                {
                    throw new Exception($"Workflow at index {index} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Workflow at index {index} does not have any applicable test data, please check and try again!");
            }
        }

        public PatientDetails GetPatientDetailsTestData(string name)
        {
            var patientTestData = PatientsTestData.TestData.FirstOrDefault(c => c.Name.Contains(name));

            if (patientTestData != null)
            {
                if (patientTestData.Patient != null)
                {
                    PatientDetails = patientTestData.Patient;
                    return patientTestData.Patient;
                }
                else
                {
                    throw new Exception($"Patient Details {name} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Patient Details {name} does not have any applicable test data, please check and try again!");
            }
        }

        public WorkflowRequestMessage GetWorkflowRequestTestData(string name)
        {
            var workflowRequest = WorkflowRequestsTestData.TestData.FirstOrDefault(c => c.Name.Contains(name));

            if (workflowRequest != null)
            {
                if (workflowRequest.WorkflowRequestMessage != null)
                {
                    WorkflowRequestMessage = workflowRequest.WorkflowRequestMessage;

                    return workflowRequest.WorkflowRequestMessage;
                }
                else
                {
                    throw new Exception($"Workflow request {name} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Workflow request {name} does not have any applicable test data, please check and try again!");
            }
        }

        internal List<WorkflowRevision> GetWorkflowRevision(string workflowId)
        {
            return MongoClient.GetWorkflowRevisionsByWorkflowId(workflowId);
        }

        public TaskUpdateEvent GetTaskUpdateTestData(string name, string updateStatus)
        {
            var taskUpdateTestData = TaskUpdatesTestData.TestData.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (taskUpdateTestData != null && taskUpdateTestData.TaskUpdateEvent != null)
            {
                taskUpdateTestData.TaskUpdateEvent.Status = updateStatus.ToLower() switch
                {
                    "accepted" => TaskExecutionStatus.Accepted,
                    "succeeded" => TaskExecutionStatus.Succeeded,
                    "failed" => TaskExecutionStatus.Failed,
                    "canceled" => TaskExecutionStatus.Canceled,
                    "partialfail" => TaskExecutionStatus.PartialFail,
                    _ => throw new Exception($"updateStatus {updateStatus} is not recognised. Please check and try again."),
                };

                TaskUpdateEvent = taskUpdateTestData.TaskUpdateEvent;

                return taskUpdateTestData.TaskUpdateEvent;
            }

            throw new Exception($"Task update message not found for {name}");
        }

        public ExportCompleteEvent GetExportCompleteTestData(string name)
        {
            var exportCompleteTestData = ExportCompletesTestData.TestData.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (exportCompleteTestData != null && exportCompleteTestData.ExportCompleteEvent != null)
            {
                ExportCompleteEvent = exportCompleteTestData.ExportCompleteEvent;

                return exportCompleteTestData.ExportCompleteEvent;
            }

            throw new Exception($"Export Complete message not found for {name}");
        }

        public List<WorkflowInstance> GetWorkflowInstances(int count, string payloadId)
        {
            var res = RetryWorkflowInstances.Execute(() =>
            {
                WorkflowInstances = MongoClient.GetWorkflowInstancesByPayloadId(payloadId);

                if (WorkflowInstances.Count == count)
                {
                    return WorkflowInstances;
                }
                else
                {
                    throw new Exception($"{count} workflow instances could not be found for payloadId {payloadId}. Actual count is {WorkflowInstances.Count}");
                }
            });

            return res;
        }

        public WorkflowInstance GetAllWorkflowInstance(string workflowInstanceId)
        {
            return MongoClient.GetWorkflowInstanceById(workflowInstanceId);
        }

        public List<Payload> GetPayloadCollections(string payloadId)
        {
            var res = RetryPayloadCollections.Execute(() =>
            {
                Payload = MongoClient.GetPayloadCollectionByPayloadId(payloadId);

                if (Payload != null)
                {
                    return Payload;
                }
                else
                {
                    throw new Exception($"Payload collection could not be found for payloadId {payloadId}");
                }
            });

            return res;
        }

        public List<TaskDispatchEvent> GetTaskDispatchEvents(int count, List<WorkflowInstance> workflowInstances)
        {
            var res = RetryTaskDispatches.Execute(() =>
            {
                var message = TaskDispatchConsumer.GetMessage<TaskDispatchEvent>();

                if (message != null)
                {
                    foreach (var workflowInstance in workflowInstances)
                    {
                        if (message.WorkflowInstanceId == workflowInstance.Id)
                        {
                            TaskDispatchEvents.Add(message);
                        }
                    }
                }

                if (TaskDispatchEvents.Count == count)
                {
                    return TaskDispatchEvents;
                }
                else
                {
                    throw new Exception($"{count} task dispatch events could not be found, actual amount is {TaskDispatchEvents.Count}");
                }
            });

            return res;
        }

        public List<ExportRequestEvent> GetExportRequestEvents(int count, List<WorkflowInstance> workflowInstances)
        {
            var res = RetryExportRequests.Execute(() =>
            {
                var message = ExportRequestConsumer.GetMessage<ExportRequestEvent>();

                if (message != null)
                {
                    foreach (var workflowInstance in workflowInstances)
                    {
                        if (message.WorkflowInstanceId == workflowInstance.Id)
                        {
                            ExportRequestEvents.Add(message);
                        }
                    }
                }

                if (ExportRequestEvents.Count == count)
                {
                    return ExportRequestEvents;
                }
                else
                {
                    throw new Exception($"{count} export request events could not be found");
                }
            });

            return res;
        }

        public List<ExportRequestEvent> GetAllExportRequestEvents(List<WorkflowInstance> workflowInstances)
        {
            for (int i = 0; i < 5; i++)
            {
                var message = ExportRequestConsumer.GetMessage<ExportRequestEvent>();

                if (message != null)
                {
                    foreach (var workflowInstance in workflowInstances)
                    {
                        if (message.WorkflowInstanceId == workflowInstance.Id)
                        {
                            ExportRequestEvents.Add(message);
                        }
                    }
                }
                Thread.Sleep(500);
            }
            return ExportRequestEvents;
        }

        public List<TaskDispatchEvent> GetTaskDispatchEventByTaskId(List<string> taskIds)
        {
            var res = RetryTaskDispatches.Execute(() =>
            {
                var message = TaskDispatchConsumer.GetMessage<TaskDispatchEvent>();

                if (message != null)
                {
                    foreach (var taskId in taskIds)
                    {
                        if (message.TaskId == taskId)
                        {
                            TaskDispatchEvents.Add(message);
                        }
                    }
                }

                if (TaskDispatchEvents.Count == taskIds.Count)
                {
                    return TaskDispatchEvents;
                }
                else
                {
                    throw new Exception($"{taskIds.Count} task dispatch events could not be found");
                }
            });

            return res;
        }

        public Payload GetPayloadTestData(string name)
        {
            var payload = PayloadsTestData.TestData.FirstOrDefault(c => c.Name.Contains(name));

            if (payload != null)
            {
                if (payload.Payload != null)
                {
                    Payload.Add(payload.Payload);
                    return payload.Payload;
                }
                else
                {
                    throw new Exception($"Payload {name} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Payload {name} does not have any applicable test data, please check and try again!");
            }
        }

        public Payload GetPayloadsTestDataByIndex(int index)
        {
            var payload = PayloadsTestData.TestData[index];

            if (payload != null)
            {
                if (payload.Payload != null)
                {
                    Payload.Add(payload.Payload);
                    return payload.Payload;
                }
                else
                {
                    throw new Exception($"Payload at index {index} does not have any applicable test data, please check and try again!");
                }
            }
            else
            {
                throw new Exception($"Payload at index {index} does not have any applicable test data, please check and try again!");
            }
        }

        public string GetPayloadId(string? payloadId = null)
        {
            return PayloadId = payloadId ?? Guid.NewGuid().ToString();
        }
    }
}
