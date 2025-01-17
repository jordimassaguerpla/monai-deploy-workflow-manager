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

using System.Globalization;
using System.Runtime.Serialization;
using Ardalis.GuardClauses;

namespace Monai.Deploy.WorkflowManager.TaskManager.API
{
    public static class ServiceNotFoundExceptionGuardExtension
    {
        public static void NullService<T>(this IGuardClause guardClause, T service, string parameterName)
        {
            Guard.Against.Null(guardClause, nameof(guardClause));
            Guard.Against.NullOrWhiteSpace(parameterName, nameof(parameterName));

            if (service is null)
            {
                throw new ServiceNotFoundException(parameterName);
            }
        }
    }

    [Serializable]
    public class ServiceNotFoundException : Exception
    {
        private static readonly string MessageFormat = "Required service '{0}' cannot be found or cannot be initialized.";

        public ServiceNotFoundException(string serviceName)
            : base(string.Format(CultureInfo.InvariantCulture, MessageFormat, serviceName))
        {
        }

        public ServiceNotFoundException(string serviceName, Exception innerException)
            : base(string.Format(CultureInfo.InvariantCulture, MessageFormat, serviceName), innerException)
        {
        }

        private ServiceNotFoundException()
        {
        }

        protected ServiceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
