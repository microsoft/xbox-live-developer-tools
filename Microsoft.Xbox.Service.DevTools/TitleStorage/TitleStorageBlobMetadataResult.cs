// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Class contains title storage metadata collection result.
    /// </summary>
    public class TitleStorageBlobMetadataResult
    {
        /// <summary>
        /// Collection of TitleStorageBlobMetadata objects returned by a service request.
        /// </summary>
        public IEnumerable<TitleStorageBlobMetadata> Items { get; internal set; } = Enumerable.Empty<TitleStorageBlobMetadata>();

        /// <summary>
        /// If current result collection has next page.
        /// </summary>
        public bool HasNext
        {
            get { return !string.IsNullOrEmpty(this.ContinuationToken); }
        }

        /// <summary>
        /// Total count of the items.
        /// </summary>
        public uint TotalItems { get; internal set; } = 0;

        internal string ContinuationToken { get; set; }

        internal string ServiceConfigurationId { get; set; }

        internal string Sandbox { get; set; }

        internal string Path { get; set; }

        /// <summary>
        /// Get next page of the current result collection.
        /// </summary>
        /// <param name="maxItems">The maximum number of items to return. (Optional)</param>
        /// <returns>TitleStorageBlobMetadataResult object contains result collection.</returns>
        public async Task<TitleStorageBlobMetadataResult> GetNextAsync(uint maxItems)
        {
            if (!this.HasNext)
            {
                return new TitleStorageBlobMetadataResult();
            }

            return await TitleStorage.GetGlobalStorageBlobMetaDataAsync(this.ServiceConfigurationId, this.Sandbox, this.Path, maxItems, 0, this.ContinuationToken);
        }
    }
}
