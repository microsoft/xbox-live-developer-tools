//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************


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

        public AuthClient(IAuthContext context)
        {
            this.authContext = context;
        }

        public bool HasCredential
        {
            get { return authContext.HasCredential; }
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

        public async Task<string> GetETokenAsync(string scid, string sandbox)
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

            return new DevAccount(token, this.authContext.AccountSource);
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

            var token = JsonConvert.DeserializeObject<XdtsTokenResponse>(responseContent.Content);
            this.cachedTokens[scid + sandbox] = token;

            return token;
        }
    }
}
