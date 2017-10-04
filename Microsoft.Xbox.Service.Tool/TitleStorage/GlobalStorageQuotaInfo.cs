// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json;
    using System;

    public class GlobalStorageQuotaInfo
    {
        [JsonProperty("usedBytes")]
        public UInt64 UsedBytes { get; private set; }

        [JsonProperty("quotaBytes")]
        public UInt64 QuotaBytes { get; private set; }
    }
}
