// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Microsoft.Xbox.Services.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

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
        public async Task<XboxLiveHttpContent> SendAsync(HttpRequestMessage request)
        {
            var content = new XboxLiveHttpContent();

            HttpResponseMessage response = await this.SendInternalAsync(request, false);

            if (response != null && response.IsSuccessStatusCode)
            {
                ExtractCollrelationId(response, ref content);
                content.Content = response.Content;
                return content;
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Force refresh the token if gets 403, then resend the request.
                response = await this.SendInternalAsync(request, true);
                if (response != null && response.IsSuccessStatusCode)
                {
                    ExtractCollrelationId(response, ref content);
                    content.Content = response.Content;
                    return content;
                }
            }

            // If final HTTP status is not success
            throw new XboxLiveException("Failed to call Xbox Live services", response, null);
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
                string eToken = await Auth.Client.GetETokenAsync(scid, sandbox, refreshToken);
                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", "XBL3.0 x=-;" + eToken);
            }

            try
            {
                return await this.httpClient.SendAsync(request);
            }
            catch (Exception e)
            {
                throw new XboxLiveException("Failed to call Xbox Live services", null, e);
            }
        }

        private static void ExtractCollrelationId(HttpResponseMessage response, ref XboxLiveHttpContent content)
        {
            if (response != null)
            {
                if (response.Headers.TryGetValues("X-XblCorrelationId", out IEnumerable<string> correlationIds))
                {
                    if (correlationIds != null && !string.IsNullOrEmpty(correlationIds.First()))
                    {
                        content.CollrelationId = correlationIds.First();
                    }
                }
            }
        }
    }
}
