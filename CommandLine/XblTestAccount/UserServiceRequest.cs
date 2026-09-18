// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTools.Authentication;

    /// <summary>
    /// Issues a request to an Xbox Live user service with the signed in test account's token.
    /// </summary>
    /// <remarks>
    /// This deliberately does not use the library's XboxLiveHttpRequest, which is internal to the
    /// library and retries on 403 rather than the 401 these services answer with once a privilege
    /// changes. The policy it would have supplied is reproduced here: TLS 1.2, the tool user agent,
    /// a single shared HttpClient, and correlation id capture, so that a call to the parental or
    /// privacy service can still be traced with the service team after the fact.
    /// </remarks>
    internal static class UserServiceRequest
    {
        private static readonly HttpClient Client = new HttpClient();

        private static readonly string UserAgent = BuildUserAgent();

        static UserServiceRequest()
        {
            // .Net is supposed to default to the latest TLS version on the machine, but the
            // library pins it explicitly for the same services, so this does too.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Sends a request, retrying once with a freshly minted token if the service rejects the
        /// token it was made with.
        /// </summary>
        /// <param name="sandbox">The sandbox to authenticate against.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="uri">The absolute request URI.</param>
        /// <param name="contractVersion">The value of the x-xbl-contract-version header.</param>
        /// <param name="body">The JSON request body, or null for a request without one.</param>
        /// <returns>The response body.</returns>
        internal static async Task<string> SendAsync(string sandbox, HttpMethod method, string uri, string contractVersion, string body)
        {
            // An XToken carries the privilege claims of the account, so a call that changes
            // privileges invalidates the token it was made with. The service then answers HTTP 401
            // even though the cached token has not expired, so a 401 is retried once with a freshly
            // minted token before it is reported as a failure.
            try
            {
                return await SendOnceAsync(sandbox, method, uri, contractVersion, body, false);
            }
            catch (RetryableUnauthorizedException)
            {
                return await SendOnceAsync(sandbox, method, uri, contractVersion, body, true);
            }
        }

        private static async Task<string> SendOnceAsync(string sandbox, HttpMethod method, string uri, string contractVersion, string body, bool forceTokenRefresh)
        {
            // These services require a user XSTS token. A Partner Center developer eToken is
            // rejected with HTTP 401 even for a read-only GET.
            string authHeader;
            try
            {
                authHeader = await ToolAuthentication.GetTestTokenSilentlyAsync(sandbox, forceTokenRefresh);
            }
            catch (Exception ex)
            {
                // Minting the token is a separate step from the call it authenticates, and it
                // fails for its own reasons, so it is reported as itself rather than as the
                // service refusing the request.
                throw new TestAccountTokenException(sandbox, ex);
            }

            using (var request = new HttpRequestMessage(method, uri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("x-xbl-contract-version", contractVersion);
                request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

                if (body != null)
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                using (HttpResponseMessage response = await Client.SendAsync(request))
                {
                    string content = response.Content == null
                        ? string.Empty
                        : await response.Content.ReadAsStringAsync();

                    // Every call is traced with its correlation id, so that a write to the
                    // parental or privacy service leaves a record of what was asked and which
                    // service transaction answered it.
                    string correlationId = ExtractCorrelationId(response);
                    Trace.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1} returned HTTP {2}. Correlation id: {3}",
                        method.Method,
                        uri,
                        (int)response.StatusCode,
                        correlationId ?? "none"));

                    if (response.StatusCode == HttpStatusCode.Unauthorized && !forceTokenRefresh)
                    {
                        throw new RetryableUnauthorizedException();
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        // The correlation id is carried into the message because it is the only
                        // handle the service team can act on, and the message is what the tool
                        // prints when a call fails.
                        string message = $"The service returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {content}".TrimEnd();

                        if (correlationId != null)
                        {
                            message += $" Correlation id: {correlationId}";
                        }

                        throw new HttpRequestException(message);
                    }

                    return content;
                }
            }
        }

        /// <summary>
        /// Reads whichever correlation header the service answered with, or null if it sent none.
        /// </summary>
        private static string ExtractCorrelationId(HttpResponseMessage response)
        {
            foreach (string header in new[] { "X-XblCorrelationId", "MS-CV" })
            {
                if (response.Headers.TryGetValues(header, out IEnumerable<string> values))
                {
                    string value = values?.FirstOrDefault();
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static string BuildUserAgent()
        {
            AssemblyName assemblyName = Assembly.GetEntryAssembly()?.GetName() ?? Assembly.GetExecutingAssembly().GetName();
            return $"{assemblyName.Name}/{assemblyName.Version}";
        }

        /// <summary>
        /// Signals an HTTP 401 that is worth retrying with a freshly minted token.
        /// </summary>
        private class RetryableUnauthorizedException : Exception
        {
        }
    }
}

