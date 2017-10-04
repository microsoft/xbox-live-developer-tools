// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Newtonsoft.Json.Serialization;

namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public partial class TitleStorage
    {
        public class TitleStorageBlobMetadataResult
        {
            public IEnumerable<TitleStorageBlobMetadata> Items { get; internal set; } = new List<TitleStorageBlobMetadata>();

            public bool HasNext {
                get { return !string.IsNullOrEmpty(this.ContinuationToken); }
            }

            public async Task<TitleStorageBlobMetadataResult> GetNextAsync(uint maxItems)
            {
                if (!this.HasNext)
                {
                    return new TitleStorageBlobMetadataResult();
                }

                return await GetGlobalStorageBlobMetaData(this.ServiceConfigurationId, this.Sandbox, this.Path, maxItems, 0, this.ContinuationToken);
            }

            public uint TotalItems { get; internal set; } = 0;
            internal string ContinuationToken { get; set; }
            internal string ServiceConfigurationId { get; set; }
            internal string Sandbox { get; set; }
            internal string Path { get; set; }
        }
    }

}
