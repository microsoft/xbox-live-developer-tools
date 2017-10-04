// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using System.Net.Http;

    internal class XboxLiveHttpContent
    {
        public HttpContent Content { get; set; }
        public string CollrelationId { get; set; }
    }
}