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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Messages;

namespace Monai.Deploy.WorkflowManager.Tests
{
    internal class DummyMessageSubscriberHealtCheck : SubscriberServiceHealthCheckRegistrationBase
    {
        public override IHealthChecksBuilder Configure(IHealthChecksBuilder builder, HealthStatus? failureStatus = null, IEnumerable<string> tags = null, TimeSpan? timeout = null) => builder;
    }

    internal class DummyMessagePublisherHealtCheck : PublisherServiceHealthCheckRegistrationBase
    {
        public override IHealthChecksBuilder Configure(IHealthChecksBuilder builder, HealthStatus? failureStatus = null, IEnumerable<string> tags = null, TimeSpan? timeout = null) => builder;
    }

    internal class DummyMessagePublisherRegistrar : PublisherServiceRegistrationBase
    {
        public override IServiceCollection Configure(IServiceCollection services) => services;
    }

    internal class DummyMessageSubscriberRegistrar : SubscriberServiceRegistrationBase
    {
        public override IServiceCollection Configure(IServiceCollection services) => services;
    }

    internal class DummyMessagingService : IMessageBrokerPublisherService, IMessageBrokerSubscriberService
    {
        public string Name => "Dummy Messaging Service";

        public event ConnectionErrorHandler OnConnectionError;

        public void Acknowledge(MessageBase message) => throw new NotImplementedException();

        public void Dispose() => throw new NotImplementedException();

        public Task Publish(string topic, Message message) => throw new NotImplementedException();

        public void Reject(MessageBase message, bool requeue = true) => throw new NotImplementedException();

        public Task RequeueWithDelay(MessageBase message) => throw new NotImplementedException();

        public void Subscribe(string topic, string queue, Action<MessageReceivedEventArgs> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();

        public void Subscribe(string[] topics, string queue, Action<MessageReceivedEventArgs> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();

        public void SubscribeAsync(string topic, string queue, Func<MessageReceivedEventArgs, Task> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();

        public void SubscribeAsync(string[] topics, string queue, Func<MessageReceivedEventArgs, Task> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();
    }
}
