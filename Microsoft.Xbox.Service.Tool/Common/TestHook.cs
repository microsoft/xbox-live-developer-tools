////*********************************************************
////
//// Copyright (c) Microsoft. All rights reserved.
//// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
////
////*********************************************************

namespace Microsoft.Xbox.Services.Tool
{
    using System.Net.Http;

    ///Internal test hook for mocking HttpClient's response
    ///</summary>
    internal class TestHook
    {
        public static HttpMessageHandler MockHttpHandler { set; get; }
    }
}
