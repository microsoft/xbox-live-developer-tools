// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal class AuthClient
    {
        public IAuthContext AuthContext { get; set; }

        public DevAccount Account { get; private set; }

        public Lazy<XdtsTokenCache> ETokenCache { get; } = new Lazy<XdtsTokenCache>();

        public bool HasCredential
        {
            get { return this.Account != null; }
        }

        public virtual async Task<string> GetETokenAsync(string scid, string sandbox, bool forceRefresh)
        {
            if (AuthContext == null)
            {
                throw new XboxLiveException(XboxLiveErrorStatus.AuthenticationFailure, "User Info is not found.");
            }

            string eToken = null;

            if (!forceRefresh)
            {
                // return cachaed token if we have one and didn't expire
                string cacheKey =
                    XdtsTokenCache.GetCacheKey(AuthContext.UserName, AuthContext.AccountSource, scid, sandbox);
                this.ETokenCache.Value.TryGetCachedToken(cacheKey, out eToken);
            }

            if (string.IsNullOrEmpty(eToken))
            {
                var aadToken = await this.AuthContext.AcquireTokenSilentAsync();
                var xtdsToken = await FetchXdtsToken(aadToken, scid, sandbox);
                eToken = xtdsToken.Token;
            }

            return eToken;
        }

        public async Task<DevAccount> SignInAsync()
        {
            if (AuthContext == null)
            {
                throw new XboxLiveException(XboxLiveErrorStatus.AuthenticationFailure, "User Info is not found.");
            }

            string aadToken = await AuthContext.AcquireTokenAsync();
            XdtsTokenResponse token = await FetchXdtsToken(aadToken, string.Empty, string.Empty);

            this.Account = new DevAccount(token, this.AuthContext.AccountSource);

            return this.Account;
        }


        protected async Task<XdtsTokenResponse> FetchXdtsToken(string aadToken, string scid, string sandbox)
        {
            var tokenRequest = new XboxLiveHttpRequest(false, null, null);
            var requestMsg = new HttpRequestMessage(HttpMethod.Post, this.AuthContext.XtdsEndpoint);

            var requestContent = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, sandbox));
            requestMsg.Content = new StringContent(requestContent);

            // Add the aadToken header without validation as the framework
            // does not like the values returned for aadTokens for MSA accounts.
            requestMsg.Headers.TryAddWithoutValidation("Authorization", aadToken);

            var responseContent = await tokenRequest.SendAsync(requestMsg);
            Log.WriteLog("Fetch xdts Token succeeded.");

            string content = await responseContent.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<XdtsTokenResponse>(content);
            
            string key = XdtsTokenCache.GetCacheKey(AuthContext.UserName, AuthContext.AccountSource, scid, sandbox);
            this.ETokenCache.Value.UpdateToken(key, token);

            return token;
        }
    }
}
