// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using Newtonsoft.Json;

    /// <summary>
    /// Title blob info data contract for collection enumeration.
    /// </summary>
    public class TitleBlobInfo
    {
        /// <summary>
        /// Gets or sets the name of the title blob.
        /// </summary>
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the file.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the size in bytes of the title blob.
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the ETag of the title blob.
        /// </summary>
        [JsonProperty("etag")]
        public string ETag { get; set; }
    }
}