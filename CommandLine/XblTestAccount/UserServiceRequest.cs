// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTools.Authentication;

    /// <summary>
    /// Issues a request to an Xbox Live user service with the signed in test account's token.
    /// </summary>
    /// <remarks>
    /// This deliberately does not use the library's XboxLiveHttpRequest, which attaches a Partner
    /// Center developer eToken. The parental and privacy services require a user XSTS token, so the
    /// request is built here instead. The cost is that the library's correlation id logging and
    /// retry policy do not apply, which is why the 401 retry below is implemented locally.
    /// </remarks>
    internal static class UserServiceRequest
    {
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
            string authHeader = await ToolAuthentication.GetTestTokenSilentlyAsync(sandbox, forceTokenRefresh);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(method, uri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("x-xbl-contract-version", contractVersion);

                if (body != null)
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    string content = response.Content == null
                        ? string.Empty
                        : await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.Unauthorized && !forceTokenRefresh)
                    {
                        throw new RetryableUnauthorizedException();
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException(
                            $"The service returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {content}".TrimEnd());
                    }

                    return content;
                }
            }
        }

        /// <summary>
        /// Signals an HTTP 401 that is worth retrying with a freshly minted token.
        /// </summary>
        private class RetryableUnauthorizedException : Exception
        {
        }
    }
}
