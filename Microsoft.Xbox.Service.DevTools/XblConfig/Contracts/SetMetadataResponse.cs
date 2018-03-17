// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig.Contracts
{
    using System;

    internal class SetMetadataResponse
    {
        public bool Error { get; set; }

        public Guid Id { get; set; }

        public int ChunkSize { get; set; }

        public bool ResumeRestart { get; set; }

        public int[] ChunkList { get; set; }

        public string Message { get; set; }

        public int BlobPartitions { get; set; }

        public string ServerLocation { get; set; }
    }
}
