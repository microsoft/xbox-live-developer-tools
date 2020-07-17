// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using Newtonsoft.Json;

    /// <summary>
    /// Response data contract for ListTitleData requests.
    /// </summary>
    public class ListTitleDataResponse
    {
        /// <summary>
        /// Constructs an instance of the ListTitleDataResponse class.
        /// </summary>
        public ListTitleDataResponse()
        {
            this.Blobs = null;
            this.PagingInfo = new PagingInfo()
            {
                ContinuationToken = null,
            };
        }

        /// <summary>
        /// Gets or sets the list of blobs.
        /// </summary>
        [JsonProperty("blobs")]
        public TitleBlobInfo[] Blobs { get; set; }

        /// <summary>
        /// Gets or sets the paging info for the response.
        /// </summary>
        [JsonProperty("pagingInfo")]
        public PagingInfo PagingInfo { get; set; }
    }
}