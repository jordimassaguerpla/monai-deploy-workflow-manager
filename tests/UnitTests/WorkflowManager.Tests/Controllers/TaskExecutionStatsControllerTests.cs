﻿/*
 * Copyright 2023 MONAI Consortium
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
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.WorkflowManager.Configuration;
using Monai.Deploy.WorkflowManager.Shared.Filter;
using Monai.Deploy.WorkflowManager.Shared.Services;
using Monai.Deploy.WorkflowManager.Shared.Wrappers;
using Moq;
using Xunit;
using System.Linq;
using System.Net;
using Monai.Deploy.WorkflowManager.Database;
using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkflowManager.ControllersShared;

namespace Monai.Deploy.WorkflowManager.Test.Controllers
{
    public class ExecutionStatsControllerTests
    {
        private TaskStatsController StatsController { get; set; }

        private readonly Mock<ITaskExecutionStatsRepository> _repo;
        private readonly Mock<ILogger<TaskStatsController>> _logger;
        private readonly Mock<IUriService> _uriService;
        private readonly IOptions<WorkflowManagerOptions> _options;
        private readonly ExecutionStats[] _executionStats;

        public ExecutionStatsControllerTests()
        {
            _options = Options.Create(new WorkflowManagerOptions());
            _repo = new Mock<ITaskExecutionStatsRepository>();
            _logger = new Mock<ILogger<TaskStatsController>>();
            _uriService = new Mock<IUriService>();

            StatsController = new TaskStatsController(_options, _uriService.Object, _logger.Object, _repo.Object);
            var startTime = new DateTime(2023, 4, 4);
            _executionStats = new ExecutionStats[]
            {
                new ExecutionStats
                {
                    ExecutionId = Guid.NewGuid().ToString(),
                    StartedUTC = startTime,
                    WorkflowInstanceId= "workflow",
                    TaskId = "task",
                },
            };
            _repo.Setup(w => w.GetStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_executionStats);
            _repo.Setup(w => w.GetStatsStatusCountAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_executionStats.Count());
            _repo.Setup(w => w.GetStatsTotalCompleteExecutionsCountAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_executionStats.Count());
        }

        [Fact]
        public async Task GetListAsync_PayloadsExist_ReturnsList()
        {
            _uriService.Setup(s => s.GetPageUriString(It.IsAny<PaginationFilter>(), It.IsAny<string>())).Returns(() => "unitTest");

            var result = await StatsController.GetStatsAsync(new TimeFilter(), "", "");

            var objectResult = Assert.IsType<OkObjectResult>(result);

            var responseValue = (StatsPagedResponse<IEnumerable<ExecutionStatDTO>>)objectResult.Value;
            responseValue.Data.First().ExecutionId.Should().Be(_executionStats.First().ExecutionId);
            responseValue.FirstPage.Should().Be("unitTest");
            responseValue.LastPage.Should().Be("unitTest");
            responseValue.PageNumber.Should().Be(1);
            responseValue.PageSize.Should().Be(10);
            responseValue.TotalPages.Should().Be(1);
            responseValue.TotalRecords.Should().Be(1);
            responseValue.Succeeded.Should().Be(true);
            responseValue.PreviousPage.Should().Be(null);
            responseValue.NextPage.Should().Be(null);
            responseValue.Errors.Should().BeNullOrEmpty();
        }
        [Fact]
        public async Task GetStatsOverviewAsync_ServiceException_ReturnProblem()
        {
            _repo.Setup(w => w.GetStatsStatusSucceededCountAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception());

            var result = await StatsController.GetOverviewAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);

            const string expectedInstance = "tasks/statsoverview";
            Assert.StartsWith(expectedInstance, ((ProblemDetails)objectResult.Value).Instance);
        }

        [Fact]
        public async Task GetStatsAsync_ServiceException_ReturnProblem()
        {
            _repo.Setup(w => w.GetStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception());

            var result = await StatsController.GetStatsAsync(new TimeFilter(), "", "");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);

            const string expectedInstance = "tasks/stats";
            Assert.StartsWith(expectedInstance, ((ProblemDetails)objectResult.Value).Instance);
        }

        [Fact]
        public async Task GetStatsAsync_Pass_All_Arguments_To_GetStatsAsync_In_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);
            var PageNumber = 15;
            var PageSize = 9;

            var result = await StatsController.GetStatsAsync(new TimeFilter { StartTime = startTime, EndTime = endTime, PageNumber = PageNumber, PageSize = PageSize }, "workflow", "task");

            _repo.Verify(v => v.GetStatsAsync(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<int>(i => i.Equals(PageSize)),
                It.Is<int>(i => i.Equals(PageNumber)),
                It.Is<string>(s => s.Equals("workflow")),
                It.Is<string>(s => s.Equals("task")))
            );
        }

        [Fact]
        public async Task GetStatsAsync_Pass_All_Arguments_To_GetStatsStatusSucceededCountAsync_in_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetStatsAsync(new TimeFilter { StartTime = startTime, EndTime = endTime }, "workflow", "task");

            _repo.Verify(v => v.GetStatsStatusSucceededCountAsync(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<string>(s => s.Equals("workflow")),
                It.Is<string>(s => s.Equals("task"))));
        }


        [Fact]
        public async Task GetStatsAsync_Pass_All_Arguments_To_GetStatsStatusFailedCountAsync_in_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetStatsAsync(new TimeFilter { StartTime = startTime, EndTime = endTime }, "workflow", "task");

            _repo.Verify(v => v.GetStatsStatusFailedCountAsync(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<string>(s => s.Equals("workflow")),
                It.Is<string>(s => s.Equals("task"))));
        }

        [Fact]
        public async Task GetStatsAsync_Pass_All_Arguments_To_GetStatsCountAsync_in_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetStatsAsync(new TimeFilter { StartTime = startTime, EndTime = endTime }, "workflow", "ta");

            _repo.Verify(v => v.GetStatsStatusCountAsync(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<string>(s => s.Equals("Accepted")),
                It.Is<string>(s => s.Equals("workflow")),
                It.Is<string>(s => s.Equals("ta"))));
        }

        [Fact]
        public async Task GetOverviewAsync_Pass_All_Arguments_To_GetAverageStats_in_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetOverviewAsync(startTime, endTime);

            _repo.Verify(v => v.GetAverageStats(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<string>(s => s.Equals("")),
                It.Is<string>(s => s.Equals(""))));
        }

        [Fact]
        public async Task GetOverviewAsync_Pass_All_Arguments_To_GetStatsCountAsync_in_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetOverviewAsync(startTime, endTime);

            _repo.Verify(v => v.GetStatsStatusCountAsync(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<string>(s => s.Equals("")),
                It.Is<string>(s => s.Equals("")),
                It.Is<string>(s => s.Equals(""))));
        }

        [Fact]
        public async Task GetOverviewAsync_Pass_All_Arguments_To_GetStatsStatusSucceededCountAsync_in_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetOverviewAsync(startTime, endTime);

            _repo.Verify(v => v.GetStatsStatusSucceededCountAsync(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<string>(s => s.Equals("")),
                It.Is<string>(s => s.Equals(""))));
        }

        [Fact]
        public async Task GetOverviewAsync_Pass_All_Arguments_To_GetStatsStatusFailedCountAsync_in_Repo()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetOverviewAsync(startTime, endTime);

            _repo.Verify(v => v.GetStatsStatusFailedCountAsync(
                It.Is<DateTime>(d => d.Equals(startTime)),
                It.Is<DateTime>(d => d.Equals(endTime)),
                It.Is<string>(s => s.Equals("")),
                It.Is<string>(s => s.Equals(""))));
        }

        [Fact]
        public async Task GetStatsAsync_Only_TaskId_Set_ReturnProblem()
        {

            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetStatsAsync(new TimeFilter { StartTime = startTime, EndTime = endTime }, workflowId: "", taskId: "taskid");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.BadRequest, objectResult.StatusCode);

            const string expectedInstance = "tasks/stats";
            Assert.StartsWith(expectedInstance, ((ProblemDetails)objectResult.Value).Instance);
        }

        [Fact]
        public async Task GetStatsAsync_Only_Find_Matching_Results()
        {
            var startTime = new DateTime(2023, 4, 4);
            var endTime = new DateTime(2023, 4, 5);

            var result = await StatsController.GetStatsAsync(new TimeFilter { StartTime = startTime, EndTime = endTime }, workflowId: "workflow", taskId: "task");

            var objectResult = Assert.IsType<OkObjectResult>(result);
            var pagegedResults = objectResult.Value as StatsPagedResponse<IEnumerable<ExecutionStatDTO>>;
            Assert.Equal(1, pagegedResults.TotalRecords);
        }
    }
}
