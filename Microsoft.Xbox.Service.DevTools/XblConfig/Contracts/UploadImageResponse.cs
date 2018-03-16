// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig.Contracts
{
    internal class UploadImageResponse
    {
        public string AbsoluteUri { get; set; }

        public int ChunkNum { get; set; }

        public bool Error { get; set; }

        public int ErrorCode { get; set; }

        public string Location { get; set; }

        public string Message { get; set; }

        public int[] MissingChunks { get; set; }

        public string RawLocation { get; set; }

        public string State { get; set; }
    }
}
