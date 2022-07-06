﻿// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Net;
using BoDi;
using FluentAssertions;
using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkflowManager.IntegrationTests.POCO;
using Monai.Deploy.WorkflowManager.IntegrationTests.Support;
using Newtonsoft.Json;

namespace Monai.Deploy.WorkflowManager.IntegrationTests.StepDefinitions
{
    [Binding]
    public class WorkflowApiStepDefinitions
    {
        public WorkflowApiStepDefinitions(ObjectContainer objectContainer)
        {
            var httpClient = objectContainer.Resolve<HttpClient>();
            DataHelper = objectContainer.Resolve<DataHelper>();
            MongoClient = objectContainer.Resolve<MongoClientUtil>();
            ApiHelper = new ApiHelper(httpClient);
            Assertions = new Assertions();
        }

        private ApiHelper ApiHelper { get; }
        private Assertions Assertions { get; }
        private DataHelper DataHelper { get; }
        private MongoClientUtil MongoClient { get; }

        [Given(@"I have an endpoint (.*)")]
        public void GivenIHaveAnEndpoint(string endpoint) => ApiHelper.SetUrl(new Uri(TestExecutionConfig.ApiConfig.BaseUrl + endpoint));

        [Given(@"I send a (.*) request")]
        [When(@"I send a (.*) request")]
        public void WhenISendARequest(string verb)
        {
            ApiHelper.SetRequestVerb(verb);
            _ = ApiHelper.GetResponseAsync().Result;
        }

        [Then(@"I will get a (.*) response")]
        public void ThenIWillGetAResponse(string expectedCode)
        {
            ApiHelper.Response.StatusCode.Should().Be((HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), expectedCode));
        }

        [Then(@"I can see (.*) workflows are returned")]
        [Then(@"I can see (.*) workflow is returned")]
        public void ThenICanSeeWorkflowsAreReturned(int count)
        {
            var result = ApiHelper.Response.Content.ReadAsStringAsync().Result;
            var workflowRevisions = JsonConvert.DeserializeObject<List<WorkflowRevision>>(result);
            Assertions.AssertWorkflowList(DataHelper.WorkflowRevisions, workflowRevisions);
        }

        [Then(@"I can see expected workflow instances are returned")]
        public void ThenICanSeeExpectedWorkflowInstancesAreReturned()
        {
            var result = ApiHelper.Response.Content.ReadAsStringAsync().Result;
            var actualWorkflowInstances = JsonConvert.DeserializeObject<List<WorkflowInstance>>(result);
            Assertions.AssertWorkflowInstanceList(DataHelper.WorkflowInstances, actualWorkflowInstances);
        }

        [Then(@"I can see expected workflow instance is returned")]
        public void ThenICanSeeExpectedWorkflowInstanceIsReturned()
        {
            var result = ApiHelper.Response.Content.ReadAsStringAsync().Result;
            var actualWorkflowInstance = JsonConvert.DeserializeObject<WorkflowInstance>(result);
            Assertions.AssertWorkflowInstance(DataHelper.WorkflowInstances, actualWorkflowInstance);
        }

        [When(@"I have a body (.*)")]
        [Given(@"I have a body (.*)")]
        public void GivenIHaveABody(string name)
        {
            Support.HttpRequestMessageExtensions.AddJsonBody(ApiHelper.Request, DataHelper.GetWorkflowObjectTestData(name));
        }

        [Then(@"the Id (.*) is returned in the response body")]
        public void ThenTheIdIsReturned(string id)
        {
            ApiHelper.Response.Content.ReadAsStringAsync().Result.Should().Be($"{{\"workflow_id\":\"{id}\"}}");
        }

        [Then(@"I will recieve the error message (.*)")]
        public void ThenIWillRecieveTheCorrectErrorMessage(string message)
        {
            ApiHelper.Response.Content.ReadAsStringAsync().Result.Should().Contain(message);
        }

        [Then(@"multiple workflow revisions now exist with correct details")]
        public void ThenMultipleWorkflowRevisionNowExistWithCorrectDetails()
        {
            var workflowRevisions = MongoClient.GetWorkflowRevisionsByWorkflowId(DataHelper.WorkflowRevisions[0].WorkflowId);
            Assertions.AssertWorkflowRevisionDetailsAfterUpdateRequest(workflowRevisions, DataHelper.Workflows, DataHelper.WorkflowRevisions);
        }

        [Then(@"all revisions of the workflow are marked as deleted")]
        public void ThenAllRevisionsOfTheWorkflowAreMarkedAsDeleted()
        {
            var workflowRevisions = MongoClient.GetWorkflowRevisionsByWorkflowId(DataHelper.WorkflowRevisions[0].WorkflowId);
            Assertions.AssertWorkflowMarkedAsDeleted(workflowRevisions);
        }

        [Then(@"the deleted workflow is not returned")]
        public void ThenTheDeletedWorkflowIsNotReturned()
        {
            var result = ApiHelper.Response.Content.ReadAsStringAsync().Result;
            result.Should().Be("[]");
        }
    }
}