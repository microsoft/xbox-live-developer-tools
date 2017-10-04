// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Microsoft.Xbox.Services.Tool
{
    using System;

    public partial class TitleStorage
    {
        public class TitleStorageBlobMetadata
        {
            public string Path { get; internal set; }

            public TitleStorageBlobType Type { get; internal set; }

            public UInt64 Size { get; internal set; }
        }
    }

}
