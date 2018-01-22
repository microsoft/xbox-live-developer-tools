// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTool.Common
{
    using System.Net.Http;

    /// <summary>
    ///Internal test hook for mocking HttpClient's response
    /// </summary>
    internal class TestHook
    {
        public static HttpMessageHandler MockHttpHandler { set; get; }
    }
}
