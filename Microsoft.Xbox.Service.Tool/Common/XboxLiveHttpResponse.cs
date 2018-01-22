// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTool.Common
{
    using System.Net.Http;

    internal class XboxLiveHttpResponse
    {
        public HttpResponseMessage Response { get; set; }
        public string CollrelationId { get; set; }
    }
}