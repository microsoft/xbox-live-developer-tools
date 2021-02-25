// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using Newtonsoft.Json;

    /// <summary>
    /// Paging info data contract for collection enumeration.
    /// </summary>
    public class PagingInfo
    {
        /// <summary>
        /// Gets or sets the continuation token for the response.
        /// </summary>
        [JsonProperty("continuationToken")]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Gets or sets the total number of items in the collection.
        /// </summary>
        [JsonProperty("totalItems")]
        public int? TotalItems { get; set; }
    }
}