// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using DevTools.Common;
    using Newtonsoft.Json;

    internal class AuthClient
    {
        public AuthClient()
        {
        }

        public IAuthContext AuthContext { get; set; }

        public DevAccount Account { get; private set; }

        public Lazy<XdtsTokenCache> ETokenCache { get; } = new Lazy<XdtsTokenCache>();

        public bool HasCredential
        {
            get { return this.Account != null; }
        }

        public virtual async Task<string> GetETokenAsync(string scid, IEnumerable<string> sandboxes, bool forceRefresh)
        {
            if (this.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            if (sandboxes != null && sandboxes.Count() == 0)
            {
                sandboxes = null;
            }

            string eToken = null;

            if (!forceRefresh)
            {
                // return cachaed token if we have one and didn't expire
                string cacheKey =
                    XdtsTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, scid, sandboxes);
                this.ETokenCache.Value.TryGetCachedToken(cacheKey, out eToken);
            }

            if (string.IsNullOrEmpty(eToken))
            {
                var aadToken = await this.AuthContext.AcquireTokenSilentAsync();
                var xtdsToken = await this.FetchXdtsToken(aadToken, scid, sandboxes);
                eToken = xtdsToken.Token;
            }

            return eToken;
        }

        public async Task<DevAccount> SignInAsync()
        {
            if (this.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            string aadToken = await this.AuthContext.AcquireTokenAsync();
            XdtsTokenResponse token = await this.FetchXdtsToken(aadToken, string.Empty, null);

            this.Account = new DevAccount(token, this.AuthContext.AccountSource);

            return this.Account;
        }

        protected async Task<XdtsTokenResponse> FetchXdtsToken(string aadToken, string scid, IEnumerable<string> sandboxes)
        {
            using (var tokenRequest = new XboxLiveHttpRequest())
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

                var token = await response.Content.DeserializeJsonAsync<XdtsTokenResponse>();

                string key = XdtsTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, scid, sandboxes);
                this.ETokenCache.Value.UpdateToken(key, token);

                return token;
            }
        }
    }
}
