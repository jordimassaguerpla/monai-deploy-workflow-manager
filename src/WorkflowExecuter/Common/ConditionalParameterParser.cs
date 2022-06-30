﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.Storage.API;
using Monai.Deploy.WorkflowManager.ConditionsResolver.Resolver;
using Monai.Deploy.WorkflowManager.Contracts.Models;
using Monai.Deploy.WorkflowManager.Storage.Services;
using Monai.Deploy.WorkflowManager.WorkfowExecuter.Common;

namespace Monai.Deploy.WorkloadManager.WorkfowExecuter.Common
{
    public enum ParameterContext
    {
        Undefined,
        TaskExecutions,
        Executions,
        DicomSeries
    }

    public class ConditionalParameterParser : IConditionalParameterParser
    {
        private const string ExecutionsTask = "context.executions.task";
        private const string ContextDicomSeries = "context.dicom.series";

        private readonly ILogger<ConditionalParameterParser> _logger;
        private readonly IDicomService _dicom;


        private readonly Regex _squigglyBracketsRegex = new Regex(@"\{{(.*?)\}}");

        public WorkflowInstance? WorkflowInstance { get; private set; } = null;

        public ConditionalParameterParser(ILogger<ConditionalParameterParser> logger, IStorageService storageService, IDicomService dicomService)
        {
            _logger = logger;
            _dicom = dicomService;
        }


        public bool TryParse(string conditions, WorkflowInstance workflowInstance)
        {
            Guard.Against.NullOrEmpty(conditions);
            Guard.Against.Null(workflowInstance);
            try
            {
                conditions = ResolveParameters(conditions, workflowInstance);
                var conditionalGroup = ConditionalGroup.Create(conditions);
                return conditionalGroup.Evaluate();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failure attemping to parse condition", conditions, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Resolves parameters in query string.
        /// </summary>
        /// <param name="conditions">The query string Example: {{ context.executions.task['other task'].'Fred' }}</param>
        /// <param name="workflowInstance">workflow instance to resolve metadata parameter</param>
        /// <returns></returns>
        public string ResolveParameters(string conditions, WorkflowInstance workflowInstance)
        {
            Guard.Against.NullOrEmpty(conditions);
            Guard.Against.Null(workflowInstance);

            WorkflowInstance = workflowInstance;

            try
            {
                var matches = _squigglyBracketsRegex.Matches(conditions);
                if (!matches.Any())
                {
                    WorkflowInstance = null;
                    return conditions;
                }

                var parameters = ParseMatches(matches).Reverse();
                foreach (var parameter in parameters)
                {
                    conditions = conditions
                        .Remove(parameter.Key.Index, parameter.Key.Length)
                        .Insert(parameter.Key.Index, $"'{parameter.Value.Result ?? "null"}'");
                }

                WorkflowInstance = null;
                return conditions;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                WorkflowInstance = null;
                throw e;
            }
        }

        /// <summary>
        /// Parses regex match collection for brackets
        /// </summary>
        /// <param name="matches">regex collection of matches</param>
        /// <returns>
        /// Returns dictionary:
        /// Key: the match will be used to replace resolved match in string via index
        /// Value: is a tuple of resolution.
        /// </returns>
        private Dictionary<Match, (string? Result, ParameterContext Context)> ParseMatches(MatchCollection matches)
        {
            var valuePairs = new Dictionary<Match, (string? Value, ParameterContext Context)>();
            foreach (Match match in matches)
            {
                valuePairs.Add(match, ResolveMatch(match.Value));
            }
            return valuePairs;
        }

        /// <summary>
        /// Resolves a query between two brackets {{ query }}
        /// </summary>
        /// <param name="value">The query Example: {{ context.executions.task['other task'].'Fred' }}</param>
        /// <returns>
        /// Tuple:
        /// Result of the resolution
        /// Context of type of resolution required to resolve query
        /// </returns>
        private (string? Result, ParameterContext Context) ResolveMatch(string value)
        {
            Guard.Against.NullOrWhiteSpace(value);

            value = value.Substring(2, value.Length - 4).Trim();
            var context = ParameterContext.Undefined;
            if (value.StartsWith(ExecutionsTask))
            {
                return ResolveExecutionTasks(value);
            }
            if (value.StartsWith(ContextDicomSeries))
            {
                return ResolveDicom(value);
            }
            return (Result: null, Context: context);
        }

        private (string? Result, ParameterContext Context) ResolveDicom(string value)
        {
            Guard.Against.NullOrWhiteSpace(value);
            Guard.Against.Null(WorkflowInstance);

            var subValue = value.Trim().Substring(ContextDicomSeries.Length, value.Length - ContextDicomSeries.Length);
            var valueArr = subValue.Split('\'');
            var keyId = $"{valueArr[1]}{valueArr[3]}";

            if (subValue.StartsWith(".any"))
            {
                var task = Task.Run(async () => await _dicom.GetAnyValueAsync(keyId, WorkflowInstance.PayloadId, WorkflowInstance.BucketId));
                task.Wait();
                var dicomValue = task.Result;
                return (Result: dicomValue, Context: ParameterContext.DicomSeries);
            }
            if (subValue.StartsWith(".all"))
            {
                var task = Task.Run(async () => await _dicom.GetAllValueAsync(keyId, WorkflowInstance.PayloadId, WorkflowInstance.BucketId));
                task.Wait();
                var dicomValue = task.Result;
                return (Result: dicomValue, Context: ParameterContext.DicomSeries);
            }
            return (Result: null, Context: ParameterContext.DicomSeries);
        }

        private (string? Result, ParameterContext Context) ResolveExecutionTasks(string value)
        {
            var subValue = value.Trim().Substring(ExecutionsTask.Length, value.Length - ExecutionsTask.Length);
            var subValues = subValue.Split('[', ']');
            var id = subValues[1].Trim('\'');
            var task = WorkflowInstance?.Tasks.First(t => t.TaskId == id);

            if (task is null || (task is not null && !task.Metadata.Any()))
            {
                return (Result: null, Context: ParameterContext.TaskExecutions);
            }

            var metadataKey = subValues[2].Split('\'')[1];

            if (task is not null && task.Metadata.ContainsKey(metadataKey))
            {
                var result = task.Metadata[metadataKey];

                if (result is string resultStr)
                {
                    return (Result: resultStr, Context: ParameterContext.TaskExecutions);
                }
            }

            return (Result: null, Context: ParameterContext.TaskExecutions);
        }
    }
}