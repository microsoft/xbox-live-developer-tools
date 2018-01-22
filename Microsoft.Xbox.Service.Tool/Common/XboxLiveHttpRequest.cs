// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.Xbox.Services.DevTool.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTool.Authentication;

    internal class XboxLiveHttpRequest : IDisposable
    {
        private HttpClient httpClient;
        private bool autoAttachAuthHeader = false;
        public string scid = null;
        private string sandbox = null;

        public XboxLiveHttpRequest(bool autoAttachAuthHeader, string scid, string sandbox)
        {
            this.autoAttachAuthHeader = autoAttachAuthHeader;
            this.scid = scid;
            this.sandbox = sandbox;

            var requestHandler = TestHook.MockHttpHandler ?? new WebRequestHandler();
            httpClient = new HttpClient(requestHandler);
        }

        // Take a Func<HttpRequestMessage> so that HttpRequestMessage will be construct in caller scope.
        public async Task<XboxLiveHttpResponse> SendAsync(HttpRequestMessage request)
        {
            var xblResposne = new XboxLiveHttpResponse();

            HttpResponseMessage response = await this.SendInternalAsync(request, false);

            if (response != null)
            {
                ExtractCollrelationId(response, ref xblResposne);
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Force refresh the token if gets 403, then resend the request.
                response = await this.SendInternalAsync(request, true);
            }

            xblResposne.Response = response;
            return xblResposne;
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

        private async Task<HttpResponseMessage> SendInternalAsync(HttpRequestMessage request, bool refreshToken)
        {
            if (this.autoAttachAuthHeader)
            {
                string eToken = await Authentication.Client.GetETokenAsync(scid, new string[]{ sandbox }, refreshToken);
                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", "XBL3.0 x=-;" + eToken);
            }

            return await this.httpClient.SendAsync(request);
        }

        private static void ExtractCollrelationId(HttpResponseMessage response, ref XboxLiveHttpResponse xblResponse)
        {
            if (response != null)
            {
                if (response.Headers.TryGetValues("X-XblCorrelationId", out IEnumerable<string> correlationIds))
                {
                    if (correlationIds != null && !string.IsNullOrEmpty(correlationIds.First()))
                    {
                        xblResponse.CollrelationId = correlationIds.First();
                    }
                }
            }
        }
    }
}
