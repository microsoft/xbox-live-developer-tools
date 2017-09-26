// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal class XboxLiveHttpContent
    {
        public string Content { get; set; }
        public string CollrelationId { get; set; }
    }

    internal class XboxLiveHttpRequest : IDisposable
    {
        private readonly HttpClient httpClient;

        public XboxLiveHttpRequest()
        {
            var requestHandler = TestHook.MockHttpHandler ?? new WebRequestHandler();
            httpClient = new HttpClient(requestHandler);
        }

        // Take a Func<HttpRequestMessage> so that HttpRequestMessage will be construct in caller scope.
        public async Task<XboxLiveHttpContent> SendAsync(HttpRequestMessage request)
        {
            var content = new XboxLiveHttpContent();
            var response = await this.httpClient.SendAsync(request);

            if (response != null && response.IsSuccessStatusCode)
            {
                content.Content = await response.Content?.ReadAsStringAsync();
                IEnumerable<string> correlationIds = null;
                if (response.Headers.TryGetValues("X-XblCorrelationId", out correlationIds))
                {
                    if (correlationIds != null && !string.IsNullOrEmpty(correlationIds.First()))
                    {
                        content.CollrelationId = correlationIds.First();
                    }
                }
                return content;
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
