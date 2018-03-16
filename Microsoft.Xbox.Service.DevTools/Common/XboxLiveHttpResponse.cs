// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System.Net.Http;

    internal class XboxLiveHttpResponse
    {
        public HttpResponseMessage Response { get; set; }

        public string CorrelationId { get; set; }
    }
}