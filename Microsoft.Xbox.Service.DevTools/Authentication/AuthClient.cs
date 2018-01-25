// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xbox.Services.DevTools.Common;

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using Newtonsoft.Json;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using DevTools.Common;
    using System.Collections.Generic;

    internal class AuthClient
    {
        public IAuthContext AuthContext { get; set; }

        public DevAccount Account { get; private set; }

        public Lazy<XdtsTokenCache> ETokenCache { get; } = new Lazy<XdtsTokenCache>();

        public AuthClient()
        {
            
        }

        public bool HasCredential
        {
            get { return this.Account != null; }
        }

        public virtual async Task<string> GetETokenAsync(string scid, IEnumerable<string> sandboxes, bool forceRefresh)
        {
            if (AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            string eToken = null;

            if (!forceRefresh)
            {
                // return cachaed token if we have one and didn't expire
                string cacheKey =
                    XdtsTokenCache.GetCacheKey(AuthContext.UserName, AuthContext.AccountSource, scid, sandboxes);
                this.ETokenCache.Value.TryGetCachedToken(cacheKey, out eToken);
            }

            if (string.IsNullOrEmpty(eToken))
            {
                var aadToken = await this.AuthContext.AcquireTokenSilentAsync();
                var xtdsToken = await FetchXdtsToken(aadToken, scid, sandboxes);
                eToken = xtdsToken.Token;
            }

            return eToken;
        }

        public async Task<DevAccount> SignInAsync()
        {
            if (AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            string aadToken = await AuthContext.AcquireTokenAsync();
            XdtsTokenResponse token = await FetchXdtsToken(aadToken, string.Empty, null);

            this.Account = new DevAccount(token, this.AuthContext.AccountSource);

            return this.Account;
        }

        protected async Task<XdtsTokenResponse> FetchXdtsToken(string aadToken, string scid, IEnumerable<string> sandboxes)
        {
            using (var tokenRequest = new XboxLiveHttpRequest(false, null, null))
            {
                HttpResponseMessage response = (await tokenRequest.SendAsync(() =>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Post, this.AuthContext.XtdsEndpoint);

                    var requestContent = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, sandboxes));
                    requestMsg.Content = new StringContent(requestContent);

                    // Add the aadToken header without validation as the framework
                    // does not like the values returned for aadTokens for MSA accounts.
                    requestMsg.Headers.TryAddWithoutValidation("Authorization", aadToken);

                    return requestMsg;
                })).Response;
                response.EnsureSuccessStatusCode();
                Log.WriteLog("Fetch xdts Token succeeded.");

                string content = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<XdtsTokenResponse>(content);

                string key = XdtsTokenCache.GetCacheKey(AuthContext.UserName, AuthContext.AccountSource, scid, sandboxes);
                this.ETokenCache.Value.UpdateToken(key, token);

                return token;
            }
        }
    }
}
