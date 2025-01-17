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

using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkflowManager.IntegrationTests.POCO;
using MongoDB.Driver;
using Polly;
using Polly.Retry;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.WorkflowManager.IntegrationTests.Support
{
    public class MongoClientUtil
    {
        private MongoClient Client { get; set; }
        private IMongoDatabase Database { get; set; }
        private IMongoCollection<WorkflowRevision> WorkflowRevisionCollection { get; set; }
        private IMongoCollection<WorkflowInstance> WorkflowInstanceCollection { get; set; }
        private IMongoCollection<Payload> PayloadCollection { get; set; }
        private RetryPolicy RetryMongo { get; set; }
        private RetryPolicy<List<Payload>> RetryPayload { get; set; }

        public MongoClientUtil()
        {
            Client = new MongoClient(TestExecutionConfig.MongoConfig.ConnectionString);
            Database = Client.GetDatabase($"{TestExecutionConfig.MongoConfig.Database}");
            WorkflowRevisionCollection = Database.GetCollection<WorkflowRevision>($"{TestExecutionConfig.MongoConfig.WorkflowCollection}");
            WorkflowInstanceCollection = Database.GetCollection<WorkflowInstance>($"{TestExecutionConfig.MongoConfig.WorkflowInstanceCollection}");
            PayloadCollection = Database.GetCollection<Payload>($"{TestExecutionConfig.MongoConfig.PayloadCollection}");
            RetryMongo = Policy.Handle<Exception>().WaitAndRetry(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(1000));
            RetryPayload = Policy<List<Payload>>.Handle<Exception>().WaitAndRetry(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(1000));
            CreateCollection("dummy");
        }

        #region WorkflowRevision

        public void CreateWorkflowRevisionDocument(WorkflowRevision workflowRevision)
        {
            RetryMongo.Execute(() =>
            {
                WorkflowRevisionCollection.InsertOne(workflowRevision);
            });
        }

        public void DeleteWorkflowRevisionDocument(string id)
        {
            RetryMongo.Execute(() =>
            {
                WorkflowRevisionCollection.DeleteOne(x => x.Id.Equals(id));
            });
        }

        public void DeleteWorkflowRevisionDocumentByWorkflowId(string workflowId)
        {
            RetryMongo.Execute(() =>
            {
                WorkflowRevisionCollection.DeleteMany(x => x.WorkflowId.Equals(workflowId));
            });
        }

        public void DeleteWorkflowRevisions(string workflowId)
        {
            RetryMongo.Execute(() =>
            {
                WorkflowRevisionCollection.DeleteMany(x => x.WorkflowId.Equals(workflowId));
            });
        }

        public void DeleteAllWorkflowRevisionDocuments()
        {
            RetryMongo.Execute(() =>
            {
                WorkflowRevisionCollection.DeleteMany("{ }");

                var workflows = WorkflowRevisionCollection.Find("{ }").ToList();

                if (workflows.Count > 0)
                {
                    throw new Exception("All workflows are not deleted!");
                }
            });
        }

        public List<WorkflowRevision> GetWorkflowRevisionsByWorkflowId(string workflowId)
        {
            return WorkflowRevisionCollection.Find(x => x.WorkflowId == workflowId).ToList();
        }

        #endregion WorkflowRevision

        #region WorkflowInstances

        public void CreateWorkflowInstanceDocument(WorkflowInstance workflowInstance)
        {
            RetryMongo.Execute(() =>
            {
                WorkflowInstanceCollection.InsertOne(workflowInstance);
            });
        }

        public WorkflowInstance GetWorkflowInstance(string payloadId)
        {
            return WorkflowInstanceCollection.Find(x => x.PayloadId == payloadId).FirstOrDefault();
        }

        public WorkflowInstance GetWorkflowInstanceById(string Id)
        {
            return WorkflowInstanceCollection.Find(x => x.Id == Id).FirstOrDefault();
        }

        public WorkflowInstance GetWorkflowInstanceByWorkflowId(string Id)
        {
            return WorkflowInstanceCollection.Find(x => x.WorkflowId == Id).FirstOrDefault();
        }

        public List<WorkflowInstance> GetWorkflowInstancesByPayloadId(string payloadId)
        {
            return WorkflowInstanceCollection.Find(x => x.PayloadId == payloadId).ToList();
        }

        public void DeleteAllWorkflowInstances()
        {
            RetryMongo.Execute(() =>
            {
                WorkflowInstanceCollection.DeleteMany("{ }");

                var workflowInstances = WorkflowInstanceCollection.Find("{ }").ToList();

                if (workflowInstances.Count > 0)
                {
                    throw new Exception("All workflows instances are not deleted!");
                }
            });
        }

        public void DeleteWorkflowInstance(string id)
        {
            RetryMongo.Execute(() =>
            {
                WorkflowInstanceCollection.DeleteOne(x => x.Id.Equals(id));
            });
        }

        #endregion WorkflowInstances

        #region Payload

        public void CreatePayloadDocument(Payload payload)
        {
            RetryMongo.Execute(() =>
            {
                PayloadCollection.InsertOne(payload);
            });
        }

        public List<Payload> GetPayloadCollectionByPayloadId(string payloadId)
        {
            var res = RetryPayload.Execute(() =>
            {
                var payloadCollection = PayloadCollection.Find(x => x.PayloadId == payloadId).ToList();
                if (payloadCollection.Count != 0)
                {
                    return payloadCollection;
                }
                else
                {
                    throw new Exception("Payload not found");
                }
            });
            return res;
        }

        public void DeletePayloadDocument(string id)
        {
            RetryMongo.Execute(() =>
            {
                PayloadCollection.DeleteOne(x => x.Id.Equals(id));
            });
        }

        public void DeletePayloadDocumentByPayloadId(string payloadId)
        {
            RetryMongo.Execute(() =>
            {
                PayloadCollection.DeleteMany(x => x.PayloadId.Equals(payloadId));
            });
        }

        public void DeleteAllPayloadDocuments()
        {
            RetryMongo.Execute(() =>
            {
                PayloadCollection.DeleteMany("{ }");

                var payloads = PayloadCollection.Find("{ }").ToList();

                if (payloads.Count > 0)
                {
                    throw new Exception("All payloads are not deleted!");
                }
            });
        }

        #endregion Payload

        public void DropDatabase(string dbName)
        {
            Client.DropDatabase(dbName);
        }

        internal void ListAllCollections(ISpecFlowOutputHelper outputHelper, string testFeature)
        {
            var collections = Database.ListCollectionNames().ToList();
            outputHelper.WriteLine($"MongoDB collections found in test feature '{testFeature}': {collections.Count}");
            collections.ForEach(p => outputHelper.WriteLine($"- Collection: {p}"));
        }

        private void CreateCollection(string collectionName)
        {
            RetryMongo.Execute(() =>
            {
                if (!Database.ListCollectionNames().ToList().Contains(collectionName))
                {
                    Database.CreateCollection(collectionName);
                }
            });
        }
    }
}
