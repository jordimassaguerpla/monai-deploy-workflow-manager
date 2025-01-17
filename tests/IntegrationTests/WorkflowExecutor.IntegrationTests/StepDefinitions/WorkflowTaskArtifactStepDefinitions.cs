﻿/*
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

using BoDi;
using Monai.Deploy.WorkflowManager.IntegrationTests.Support;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.WorkflowManager.IntegrationTests.StepDefinitions
{
    [Binding]
    public class WorkflowTaskArtifactStepDefinitions
    {
        private Assertions Assertions { get; set; }
        private DataHelper DataHelper { get; set; }
        private readonly ISpecFlowOutputHelper _outputHelper;

        public WorkflowTaskArtifactStepDefinitions(ObjectContainer objectContainer, ScenarioContext scenarioContext, ISpecFlowOutputHelper outputHelper)
        {
            Assertions = new Assertions(objectContainer);
            DataHelper = objectContainer.Resolve<DataHelper>();
            _outputHelper = outputHelper;
        }

        [Then(@"Input artifacts are mapped")]
        public void ThenInputArtifactsAreMapped()
        {
            string PayloadId;

            if (DataHelper.SeededWorkflowInstances == null)
            {
                PayloadId = DataHelper.WorkflowRequestMessage.PayloadId.ToString();
            }
            else
            {
                PayloadId = DataHelper.WorkflowInstances[0].PayloadId;
            }

            _outputHelper.WriteLine($"Retrieving updated workflow instance using the payloadid={PayloadId}");

            var workflowInstances = DataHelper.GetWorkflowInstances(1, PayloadId);

            if (workflowInstances == null)
            {
                throw new Exception($"WorkflowInstance not found for payloadId {PayloadId}");
            }

            _outputHelper.WriteLine("Retrieved workflow instance");

            foreach (var workflowInstance in workflowInstances)
            {
                var workflowRevision = DataHelper.WorkflowRevisions.OrderByDescending(x => x.Revision).First(x => x.WorkflowId.Equals(workflowInstance.WorkflowId));

                var seededWorkflowInstance = DataHelper.SeededWorkflowInstances?.FirstOrDefault(x => x.Id.Equals(workflowInstance.Id));

                foreach (var task in workflowInstance.Tasks)
                {
                    var seededTask = seededWorkflowInstance?.Tasks.FirstOrDefault(x => x.ExecutionId.Equals(task.ExecutionId));

                    if (seededTask == null)
                    {
                        var workflowTask = workflowRevision.Workflow.Tasks.First(x => x.Id.Equals(task.TaskId));

                        Assertions.AssertInputArtifactsForWorkflowInstance(workflowTask, PayloadId, task);
                    }
                }
            }
        }
    }
}
