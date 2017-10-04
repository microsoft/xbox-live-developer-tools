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
        private ConcurrentDictionary<string, XdtsTokenResponse> cachedTokens = new ConcurrentDictionary<string, XdtsTokenResponse>();
        private IAuthContext authContext;

        public DevAccount Account { get; private set; }

        public DevAccountSource AccountSource
        {
            get { return this.authContext.AccountSource; }
        }

        public AuthClient(IAuthContext context)
        {
            this.authContext = context;
        }

        public bool HasCredential
        {
            get { return this.Account != null; }
        }

        protected bool TryGetCachedToken(string key, out string token)
        {
            token = string.Empty;
            XdtsTokenResponse cachedToken = null;
            if (cachedTokens.TryGetValue(key, out cachedToken)
                && (cachedToken != null && !string.IsNullOrEmpty(cachedToken.Token) && cachedToken.NotAfter >= DateTime.UtcNow))
            {
                Log.WriteLog($"Using token from cache for {key}.");

                token = cachedToken.Token;
                return true;
            }

            return false;
        }

        public virtual async Task<string> GetETokenAsync(string scid, string sandbox)
        {
            string eToken;
            // return cachaed token if we have one and didn't expire
            if (!this.TryGetCachedToken(scid + sandbox, out eToken)
                && (this.authContext != null))
            {
                var aadToken = await this.authContext.AcquireTokenSilentAsync();
                var xtdsToken = await FetchXdtsToken(aadToken, scid, sandbox);
                eToken = xtdsToken.Token;
            }

            return eToken;
        }

        public async Task<DevAccount> SignInAsync(string userName)
        {
            string aadToken = await authContext.AcquireTokenAsync(userName);
            XdtsTokenResponse token = await FetchXdtsToken(aadToken, string.Empty, string.Empty);

            this.Account = new DevAccount(token, this.authContext.AccountSource);

            return this.Account;
        }


        protected async Task<XdtsTokenResponse> FetchXdtsToken(string aadToken, string scid, string sandbox)
        {
            var tokenRequest = new XboxLiveHttpRequest();
            var requestMsg = new HttpRequestMessage(HttpMethod.Post, this.authContext.XtdsEndpoint);

            var requestContent = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, sandbox));
            requestMsg.Content = new StringContent(requestContent);

            // Add the aadToken header without validation as the framework
            // does not like the values returned for aadTokens for MSA accounts.
            requestMsg.Headers.TryAddWithoutValidation("Authorization", aadToken);

            var responseContent = await tokenRequest.SendAsync(requestMsg);
            Log.WriteLog("Fetch xdts Token succeeded.");

            string content = await responseContent.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<XdtsTokenResponse>(content);
            this.cachedTokens[scid + sandbox] = token;

            return token;
        }
    }
}
