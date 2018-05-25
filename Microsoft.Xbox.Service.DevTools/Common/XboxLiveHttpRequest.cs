// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using DevTools.Authentication;

    internal class XboxLiveHttpRequest : IDisposable
    {
        private RequestParameters requestParameters;
        private HttpClient httpClient;

        public XboxLiveHttpRequest(RequestParameters requestParameters)
        {
            this.requestParameters = requestParameters;
            HttpMessageHandler requestHandler = TestHook.MockHttpHandler ?? new WebRequestHandler();
            this.httpClient = new HttpClient(requestHandler);
        }

        public XboxLiveHttpRequest(bool autoAttachAuthHeader, Guid scid, params string[] sandboxes)
            : this(new RequestParameters()
            {
                AutoAttachAuthHeader = autoAttachAuthHeader,
                Scid = scid,
                Sandboxes = new HashSet<string>(sandboxes)
            })
        {
        }

        public XboxLiveHttpRequest(bool autoAttachAuthHeader, string scid, params string[] sandboxes)
            : this(new RequestParameters()
            {
                AutoAttachAuthHeader = autoAttachAuthHeader,
                Scid = new Guid(scid),
                Sandboxes = new HashSet<string>(sandboxes)
            })
        {
        }

        public XboxLiveHttpRequest()
            : this(new RequestParameters())
        {
        }
        
        static XboxLiveHttpRequest()
        {
            AssemblyName executingAssemblyName = Assembly.GetExecutingAssembly().GetName();
            AssemblyName entryAssemblyName = Assembly.GetEntryAssembly()?.GetName();
            UserAgent = $"{executingAssemblyName.Name}/{executingAssemblyName.Version}";
            if (entryAssemblyName != null)
            {
                UserAgent += $" {entryAssemblyName.Name}/{entryAssemblyName.Version}";
            }
        }

        public static string UserAgent { get; set; }

        // Take a Func<HttpRequestMessage> so that HttpRequestMessage will be constructed in caller scope.
        // It is needed for retry as HttpRequestMessage will be disposed after send; cannot be reused.
        public async Task<XboxLiveHttpResponse> SendAsync(Func<HttpRequestMessage> requestGenerator)
        {
            XboxLiveHttpResponse xblResponse = new XboxLiveHttpResponse();

            HttpResponseMessage response = await this.SendInternalAsync(requestGenerator(), false);

            if (response != null)
            {
                ExtractCorrelationId(response, ref xblResponse);
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Force refresh the token if gets 403, then resend the request.
                response = await this.SendInternalAsync(requestGenerator(), true);
            }

            xblResponse.Response = response;
            return xblResponse;
        }

        // Part of the IDisposable implementation.
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
            if (this.requestParameters.AutoAttachAuthHeader)
            {
                string scid = (this.requestParameters.Scid == Guid.Empty) ? null : this.requestParameters.Scid.ToString();
                string eToken = await ToolAuthentication.Client.GetETokenAsync(scid, this.requestParameters.Sandboxes, refreshToken);
                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", "XBL3.0 x=-;" + eToken);
            }

            request.Headers.UserAgent.ParseAdd(UserAgent);

            if (!request.Headers.Contains("Accept"))
            {
                request.Headers.Accept.ParseAdd("application/json");
            }

            return await this.httpClient.SendAsync(request);
        }

        private static void ExtractCorrelationId(HttpResponseMessage response, ref XboxLiveHttpResponse xblResponse)
        {
            if (response != null)
            {
                if (response.Headers.TryGetValues("X-XblCorrelationId", out IEnumerable<string> correlationIds))
                {
                    if (correlationIds != null && !string.IsNullOrEmpty(correlationIds.First()))
                    {
                        xblResponse.CorrelationId = correlationIds.First();
                    }
                }
                else if (response.Headers.TryGetValues("MS-CV", out IEnumerable<string> correlationVectors))
                {
                    if (correlationVectors != null && !string.IsNullOrEmpty(correlationVectors.First()))
                    {
                        xblResponse.CorrelationId = correlationVectors.First();
                    }
                }
            }
        }
    }
}
