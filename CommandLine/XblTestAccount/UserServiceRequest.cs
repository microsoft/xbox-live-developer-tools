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
    /// This cannot use the library's internal XboxLiveHttpRequest, which retries on 403 instead of
    /// the 401 these services return after privilege changes. It reproduces the needed behavior:
    /// TLS 1.2, a shared HttpClient, the tool user agent, and correlation-id logging.
    /// </remarks>
    internal static class UserServiceRequest
    {
        private static readonly HttpClient Client = new HttpClient();

        private static readonly string UserAgent = BuildUserAgent();

        static UserServiceRequest()
        {
            // Keep TLS behavior aligned with the library's explicit setting for these services.
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
            // Privilege changes can invalidate a still-unexpired token, which surfaces as HTTP 401.
            // Retry once with a fresh token before reporting failure.
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
            // These endpoints require a user XSTS token, not a Partner Center developer eToken.
            string authHeader;
            try
            {
                authHeader = await ToolAuthentication.GetTestTokenSilentlyAsync(sandbox, forceTokenRefresh);
            }
            catch (Exception ex)
            {
                // Token minting failed before the service call.
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

                    // Always log correlation id for service-side traceability.
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
                        // Include correlation id in errors so support can trace the transaction.
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
