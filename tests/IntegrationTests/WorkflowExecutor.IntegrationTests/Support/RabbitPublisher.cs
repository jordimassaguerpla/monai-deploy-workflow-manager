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

using Monai.Deploy.Messaging.Messages;
using RabbitMQ.Client;

namespace Monai.Deploy.WorkflowManager.IntegrationTests.Support
{
    public class RabbitPublisher
    {
        public RabbitPublisher(IModel channel, string exchange, string routingKey)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            Channel = channel;
            Channel.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true);
        }

        private string Exchange { get; set; }

        private string RoutingKey { get; set; }

        private IModel Channel { get; set; }

        public void PublishMessage(Message message)
        {
            var propertiesDictionary = new Dictionary<string, object>
            {
                { "CreationDateTime", message.CreationDateTime.ToString("o") }
            };

            var properties = Channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = message.ContentType;
            properties.MessageId = message.MessageId;
            properties.AppId = message.ApplicationId;
            properties.CorrelationId = message.CorrelationId;
            properties.DeliveryMode = 2;
            properties.Headers = propertiesDictionary;
            properties.Type = message.MessageDescription;

            Channel.BasicPublish(exchange: Exchange,
                routingKey: RoutingKey,
                basicProperties: properties,
                body: message.Body);
        }

        public void CloseConnection()
        {
            Channel.Close();
        }
    }
}
