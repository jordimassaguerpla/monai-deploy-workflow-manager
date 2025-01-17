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

using Monai.Deploy.WorkflowManager.Filter;

namespace Monai.Deploy.WorkflowManager.Services
{
    /// <summary>
    /// Uri Serivce.
    /// </summary>
    public interface IUriService
    {
        /// <summary>
        /// Gets Relative Uri path with filters as a string.
        /// </summary>
        /// <param name="filter">Filters.</param>
        /// <param name="route">Route.</param>
        /// <returns>Relative Uri string.</returns>
        public string GetPageUriString(PaginationFilter filter, string route);
    }
}
