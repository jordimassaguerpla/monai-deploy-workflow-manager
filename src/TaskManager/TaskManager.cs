﻿// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Concurrent;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.WorkflowManager.Common;
using Monai.Deploy.WorkflowManager.Configuration;
using Monai.Deploy.WorkflowManager.Contracts.Rest;
using Monai.Deploy.WorkflowManager.TaskManager.API;
using Monai.Deploy.WorkflowManager.TaskManager.Logging;
using TaskStatus = Monai.Deploy.Messaging.Events.TaskStatus;

namespace Monai.Deploy.WorkflowManager.TaskManager
{
    public class TaskManager : IHostedService, IDisposable, IMonaiService
    {
        private bool _disposedValue;
        private readonly ILogger<TaskManager> _logger;
        private readonly IOptions<WorkflowManagerOptions> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IServiceScope _scope;
        private readonly IMessageBrokerPublisherService _messageBrokerPublisherService;
        private readonly IMessageBrokerSubscriberService _messageBrokerSubscriberService;
        private readonly BlockingCollection<MessageBase> _messageQueueStub;
        private readonly IDictionary<string, ITaskRunner> _activeExecutions;
        private long _activeJobs;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public string ServiceName => "MONAI Deploy Task Manager";

        public TaskManager(
            ILogger<TaskManager> logger,
            IOptions<WorkflowManagerOptions> options,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _scope = _serviceScopeFactory.CreateScope();

            _messageBrokerPublisherService = _scope.ServiceProvider.GetRequiredService<IMessageBrokerPublisherService>() ?? throw new ServiceNotFoundException(nameof(IMessageBrokerPublisherService));
            _messageBrokerSubscriberService = _scope.ServiceProvider.GetRequiredService<IMessageBrokerSubscriberService>() ?? throw new ServiceNotFoundException(nameof(IMessageBrokerSubscriberService));

            _messageQueueStub = new BlockingCollection<MessageBase>();
            _activeExecutions = new Dictionary<string, ITaskRunner>();
            _activeJobs = 0;
        }

        internal void QueueTask(MessageBase message)
        {
            _messageQueueStub.Add(message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(async () =>
            {
                await BackgroundProcessing(cancellationToken).ConfigureAwait(false);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceStarted(ServiceName);

            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            Status = ServiceStatus.Stopped;

            return Task.CompletedTask;
        }

        private async Task BackgroundProcessing(CancellationToken cancellationToken)
        {
            _logger.ServiceRunning(ServiceName);
            while (!cancellationToken.IsCancellationRequested)
            {
                MessageBase? message = null;
                try
                {
                    message = _messageQueueStub.Take(cancellationToken);
                    using var loggingScope = _logger.BeginScope($"Message Type={message.MessageDescription}, ID={message.MessageId}. Correlation ID={message.CorrelationId}");

                    if (message is JsonMessage<TaskDispatchEvent> taskDispatchMessage)
                    {
                        await HandleDispatchTask(taskDispatchMessage).ConfigureAwait(false);
                    }
                    else if (message is JsonMessage<RunnerCompleteEvent> runnerCompleteEvent)
                    {
                        await HandleRunnerComplete(runnerCompleteEvent).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.UnsupportedEvent(message.MessageDescription);
                        _messageBrokerSubscriberService.Reject(message, true);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.ServiceCancelledWithException(ServiceName, ex);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.ServiceDisposed(ServiceName, ex);
                }
                catch (Exception ex)
                {
                    _logger.ErrorProcessingMessage(message?.MessageId, message?.CorrelationId, ex);
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        //TODO: change application ID, task topic
        private async Task HandleRunnerComplete(JsonMessage<RunnerCompleteEvent> message)
        {
            Guard.Against.Null(message, nameof(message));

            try
            {
                message.Body.Validate();
            }
            catch (MessageValidationException ex)
            {
                _logger.InvalidMessageReceived(message.MessageId, message.CorrelationId, ex);
                await HandleMessageException(message, message.Body.WorkflowId, message.Body.TaskId, message.Body.ExecutionId, ex.Message, false).ConfigureAwait(false);
            }

            if (!_activeExecutions.TryGetValue(message.Body.ExecutionId, out var runner))
            {
                _logger.NoActiveExecutorWithTheId(message.Body.ExecutionId);
                return;
            }

            var status = await runner.GetStatus(message.Body.Identity).ConfigureAwait(false);

            var updateMessage = new JsonMessage<TaskUpdateEvent>(new TaskUpdateEvent
            {
                CorrelationId = message.CorrelationId,
                ExecutionId = message.Body.ExecutionId,
                Reason = status.FailureReason,
                Status = status.Status,
                WorkflowId = message.Body.WorkflowId,
                TaskId = message.Body.TaskId,
                Message = status.Errors,
            }, "appId", message.CorrelationId);

            try
            {
                _logger.SendingTaskUpdateMessage("md.tasks.update", updateMessage.Body.Reason);
                await _messageBrokerPublisherService.Publish("md.tasks.update", updateMessage.ToMessage()).ConfigureAwait(false);
                _logger.TaskUpdateMessageSent("md.tasks.update");
            }
            catch (Exception ex)
            {
                _logger.ErrorSendingMessage("md.tasks.update", ex);
            }

            Interlocked.Decrement(ref _activeJobs);
        }

        private async Task HandleDispatchTask(JsonMessage<TaskDispatchEvent> message)
        {
            Guard.Against.Null(message, nameof(message));

            try
            {
                message.Body.Validate();
            }
            catch (MessageValidationException ex)
            {
                _logger.InvalidMessageReceived(message.MessageId, message.CorrelationId, ex);
                await HandleMessageException(message, message.Body.WorkflowId, message.Body.TaskId, message.Body.ExecutionId, ex.Message, false).ConfigureAwait(false);
                return;
            }

            if (!TryReserveResourceForExecution())
            {
                _logger.NoResourceAvailableForExecution();
                _messageBrokerSubscriberService.Reject(message, true);
                return;
            }

            ITaskRunner? taskRunner;
            try
            {
                taskRunner = typeof(ITaskRunner).CreateInstance<ITaskRunner>(serviceProvider: _scope.ServiceProvider, typeString: message.Body.TaskAssemblyName, _serviceScopeFactory, message.Body);
            }
            catch (Exception ex)
            {
                _logger.UnsupportedRunner(message.Body.TaskAssemblyName, ex);
                await HandleMessageException(message, message.Body.WorkflowId, message.Body.TaskId, message.Body.ExecutionId, ex.Message, false).ConfigureAwait(false);
                return;
            }

            try
            {
                var executionStatus = await taskRunner.ExecuteTask().ConfigureAwait(false);
                await AcceptTaskWithUpdate(message, executionStatus).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorExecutingTask(ex);
                await HandleMessageException(message, message.Body.WorkflowId, message.Body.TaskId, message.Body.ExecutionId, ex.Message, false).ConfigureAwait(false);
            }
        }

        private async Task AcceptTaskWithUpdate(JsonMessage<TaskDispatchEvent> message, ExecutionStatus executionStatus)
        {
            Guard.Against.Null(message, nameof(message));
            Guard.Against.Null(executionStatus, nameof(executionStatus));

            try
            {
                _logger.SendingAckMessage(message.MessageDescription);
                _messageBrokerSubscriberService.Acknowledge(message);
                _logger.AckMessageSent(message.MessageDescription);
            }
            catch (Exception ex)
            {
                _logger.ErrorSendingMessage(message.MessageDescription, ex);
            }

            var updateMessage = new JsonMessage<TaskUpdateEvent>(new TaskUpdateEvent
            {
                CorrelationId = message.CorrelationId,
                ExecutionId = message.Body.ExecutionId,
                Reason = executionStatus.FailureReason,
                Status = executionStatus.Status,
                WorkflowId = message.Body.WorkflowId,
                TaskId = message.Body.TaskId,
                Message = executionStatus.Errors,
            }, "appId", message.CorrelationId);

            try
            {
                _logger.SendingTaskUpdateMessage("md.tasks.update", updateMessage.Body.Reason);
                await _messageBrokerPublisherService.Publish("md.tasks.update", updateMessage.ToMessage()).ConfigureAwait(false);
                _logger.TaskUpdateMessageSent("md.tasks.update");
            }
            catch (Exception ex)
            {
                _logger.ErrorSendingMessage("md.tasks.update", ex);
            }

        }

        private bool TryReserveResourceForExecution()
        {
            var activeJobs = Interlocked.Read(ref _activeJobs);
            if (activeJobs >= _options.Value.TaskManager.MaximumNumberOfConcurrentJobs)
            {
                return false;
            }

            var expectedActiveJobs = ++activeJobs;
            return Interlocked.CompareExchange(ref _activeJobs, expectedActiveJobs, activeJobs) != activeJobs;
        }

        //TODO: change application ID, task topic
        private async Task HandleMessageException(MessageBase message, string workflowId, string taskId, string executionId, string errors, bool requeue)
        {
            if (message is null)
            {
                return;
            }

            using var loggerScope = _logger.BeginScope($"Workflow ID={workflowId}, Execution ID={executionId}, Task ID={taskId}, Correlation ID={message.CorrelationId}");

            try
            {
                _logger.SendingRejectMessageNoRequeue(message.MessageDescription);
                _messageBrokerSubscriberService.Reject(message, requeue);
                _logger.RejectMessageNoRequeueSent(message.MessageDescription);
            }
            catch (Exception ex)
            {
                _logger.ErrorSendingMessage(message.MessageDescription, ex);
            }

            var updateMessage = new JsonMessage<TaskUpdateEvent>(new TaskUpdateEvent
            {
                CorrelationId = message.CorrelationId,
                ExecutionId = executionId,
                Reason = FailureReason.InvalidMessage,
                Status = TaskStatus.Failed,
                WorkflowId = workflowId,
                TaskId = taskId,
                Message = errors,
            }, "appId", message.CorrelationId);

            try
            {
                _logger.SendingTaskUpdateMessage("md.tasks.update", updateMessage.Body.Reason);
                await _messageBrokerPublisherService.Publish("md.tasks.update", updateMessage.ToMessage()).ConfigureAwait(false);
                _logger.TaskUpdateMessageSent("md.tasks.update");
            }
            catch (Exception ex)
            {
                _logger.ErrorSendingMessage("md.tasks.update", ex);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _scope.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
