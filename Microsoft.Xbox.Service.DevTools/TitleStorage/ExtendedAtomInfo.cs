// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using Newtonsoft.Json;

    /// <summary>
    /// Response Atom info data contract for SavedGameV2Response contract.
    /// </summary>
    public class ExtendedAtomInfo
    {
        /// <summary>
        /// Gets or sets atom name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets atom path and file name
        /// </summary>
        [JsonProperty("atom")]
        public string Atom { get; set; }

        /// <summary>
        /// Gets or sets atom size
        /// </summary>
        [JsonProperty("size")]
        public ulong Size { get; set; }
    }
}
