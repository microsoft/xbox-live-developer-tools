// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Class contains the amount of storage space allocated and used.
    /// </summary>
    public class GlobalStorageQuotaInfo
    {
        /// <summary>
        /// Number of bytes used
        /// </summary>
        [JsonProperty("usedBytes")]
        public UInt64 UsedBytes { get; private set; }

        /// <summary>
        /// Maximum number of bytes that can be used.
        /// Note that this is a soft limit and the used bytes may actually exceed this value.
        /// </summary>
        [JsonProperty("quotaBytes")]
        public UInt64 QuotaBytes { get; private set; }
    }
}
