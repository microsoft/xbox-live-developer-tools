// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    /// <summary>
    /// Metadata of a title storage blob.
    /// </summary>
    public class TitleStorageBlobMetadata
    {
        /// <summary>
        /// Blob path that conforms to a SubPath\file format (example: "foo\bar\blob.txt").
        /// </summary>
        public string Path { get; internal set; }

        /// <summary>
        /// Type of blob data. Possible values are: Binary, Json, and Config.
        /// </summary>
        public TitleStorageBlobType Type { get; internal set; }

        /// <summary>
        /// Gets the number of bytes of the blob data.
        /// </summary>
        public ulong Size { get; internal set; }
    }
}
