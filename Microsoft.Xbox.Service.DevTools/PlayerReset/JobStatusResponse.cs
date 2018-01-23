// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.PlayerReset
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    internal class JobStatusResponse
    {
        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("overallStatus")]
        public string Status { get; set; }

        [JsonProperty("providerStatus")]
        public List<JobProviderStatus> ProviderStatus { get; set; } = new List<JobProviderStatus>();
    }
}
