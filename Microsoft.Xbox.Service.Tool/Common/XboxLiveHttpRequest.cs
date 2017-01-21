//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace Microsoft.Xbox.Services.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    internal class XboxLiveHttpRequest : IDisposable
    {
        private HttpClient httpClient;
        private HttpMessageHandler requestHandler;

        public XboxLiveHttpRequest()
        {
            this.requestHandler = /*TestHook.MockHttpHandler ??*/ new WebRequestHandler();
            httpClient = new HttpClient(requestHandler);
        }

        // Take a Func<HttpRequestMessage> so that HttpRequestMessage will be construct in caller scope.
        public async Task<string> SendAsync(HttpRequestMessage request)
        {
            var response = await this.httpClient.SendAsync(request);

            if (response != null && response.IsSuccessStatusCode)
            {
                return await response.Content?.ReadAsStringAsync();
            }

            throw new XboxLiveException("Failed to call xbox live services", response, null);
        }

        /// <summary>
        /// Part of the IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.httpClient.Dispose();
            }
        }
    }
}
