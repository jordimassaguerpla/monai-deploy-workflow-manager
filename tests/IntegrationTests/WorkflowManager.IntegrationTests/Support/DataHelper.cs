﻿using Monai.Deploy.Messaging.Events;
using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkflowManager.IntegrationTests.Models;
using Monai.Deploy.WorkflowManager.IntegrationTests.TestData;
using Monai.Deploy.WorkloadManager.Contracts.Models;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.WorkflowManager.IntegrationTests.Support
{
    public class DataHelper
    {
        public WorkflowRequestMessage WorkflowRequestMessage = new WorkflowRequestMessage();
        public List<WorkflowInstance> WorkflowInstances = new List<WorkflowInstance>();
        public TaskUpdateEvent TaskUpdateEvent = new TaskUpdateEvent();
        public List<TaskDispatchEvent> TaskDispatchEvent = new List<TaskDispatchEvent>();
        public List<WorkflowRevision> WorkflowRevisions = new List<WorkflowRevision>();
        private RetryPolicy<List<WorkflowInstance>> RetryWorkflowInstances { get; set; }
        private RetryPolicy<List<TaskDispatchEvent>> RetryTaskDispatches { get; set; }
        private RabbitConsumer TaskDispatchConsumer { get; set; }
        private MongoClientUtil MongoClient { get; set; }

        public DataHelper(RabbitConsumer taskDispatchConsumer, MongoClientUtil mongoClient)
        {
            TaskDispatchConsumer = taskDispatchConsumer;
            MongoClient = mongoClient;
            RetryWorkflowInstances = Policy<List<WorkflowInstance>>.Handle<Exception>().WaitAndRetry(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
            RetryTaskDispatches = Policy<List<TaskDispatchEvent>>.Handle<Exception>().WaitAndRetry(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
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

        public TaskUpdateEvent GetTaskUpdateTestData(string name, string updateStatus)
        {
            var taskUpdateTestData = TaskUpdatesTestData.TestData.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (taskUpdateTestData != null && taskUpdateTestData.TaskUpdateEvent != null)
            {
                if (!taskUpdateTestData.Name.Contains("Missing_Status"))
                {
                    taskUpdateTestData.TaskUpdateEvent.Status = updateStatus.ToLower() switch
                    {
                        "accepted" => TaskExecutionStatus.Accepted,
                        "succeeded" => TaskExecutionStatus.Succeeded,
                        "failed" => TaskExecutionStatus.Failed,
                        "canceled" => TaskExecutionStatus.Canceled,
                        _ => throw new Exception($"updateStatus {updateStatus} is not recognised. Please check and try again."),
                    };
                }

                TaskUpdateEvent = taskUpdateTestData.TaskUpdateEvent;

                return taskUpdateTestData.TaskUpdateEvent;
            }

            throw new Exception($"Task update message not found for {name}");
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
                    throw new Exception($"{count} workflow instances could not be found for payloadId {payloadId}");
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
                            TaskDispatchEvent.Add(message);
                        }
                    }
                }

                if (TaskDispatchEvent.Count == count)
                {
                    return TaskDispatchEvent;
                }
                else
                {
                    throw new Exception($"{count} task dispatch events could not be found");
                }
            });

            return res;
        }
    }
}