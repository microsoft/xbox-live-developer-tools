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

        public Lazy<AuthTokenCache> ETokenCache { get; } = new Lazy<AuthTokenCache>(() => new AuthTokenCache("xdts.cache"));

        public Lazy<AuthTokenCache> XTokenCache { get; } = new Lazy<AuthTokenCache>(() => new AuthTokenCache("xsts.cache"));

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
                // return cached token if we have one and didn't expire
                string cacheKey = AuthTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, scid, sandboxes);
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

        public virtual async Task<XasTokenResponse> GetXTokenAsync(string sandbox, bool forceRefresh)
        {
            if (this.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            XasTokenResponse xToken = null;

            if (!forceRefresh)
            {
                // return cached token if we have one and didn't expire
                string cacheKey = AuthTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, string.Empty, sandbox);
                this.XTokenCache.Value.TryGetCachedToken(cacheKey, out xToken);
            }

            if (xToken == null)
            {
                var msaToken = await this.AuthContext.AcquireTokenSilentAsync();
                var xstsToken = await this.FetchXstsToken(msaToken, sandbox);
                xToken = xstsToken;
            }

            return xToken;
        }

        public async Task<DevAccount> SignInAsync(string tenant)
        {
            if (this.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            if (this.AuthContext.AccountSource == DevAccountSource.XboxDeveloperPortal)
            {
                throw new InvalidOperationException("XDP logins are deprecated, please use a Partner Center or Test account");
            }

            if (this.AuthContext.AccountSource == DevAccountSource.TestAccount)
            {
                throw new InvalidOperationException("To log in a test account, call the SignInTestAccountAsync method");
            }

            string authToken = await this.AuthContext.AcquireTokenAsync();
            XasTokenResponse token = await this.FetchXdtsToken(authToken, string.Empty, null);

            var account = new DevAccount(token, this.AuthContext.AccountSource, tenant);
            return account;
        }

        public async Task<TestAccount> SignInTestAccountAsync(string sandbox)
        {
            if (this.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            if (this.AuthContext.AccountSource == DevAccountSource.XboxDeveloperPortal)
            {
                throw new InvalidOperationException("XDP logins are deprecated, please use a Partner Center or Test account");
            }

            if (this.AuthContext.AccountSource == DevAccountSource.WindowsDevCenter)
            {
                throw new InvalidOperationException("To log in a Partner Center account, call the SignInAsync method");
            }

            string msaToken = await this.AuthContext.AcquireTokenAsync();
            XasTokenResponse token = await this.FetchXstsToken(msaToken, sandbox);

            var account = new TestAccount(token);
            return account;
        }

        protected async Task<XasTokenResponse> FetchXdtsToken(string aadToken, string scid, IEnumerable<string> sandboxes)
        {
            using (var tokenRequest = new XboxLiveHttpRequest())
            {
                // XdtsTokenRequest reqToken; 
                HttpResponseMessage response = (await tokenRequest.SendAsync(() =>
                {
                    // New Http request
                    var requestMsg = new HttpRequestMessage(HttpMethod.Post, this.AuthContext.XtdsEndpoint);



                    // Add the aadToken header without validation as the framework
                    // does not like the values returned for aadTokens for MSA accounts.
                    /*requestMsg.Headers.TryAddWithoutValidation("Authorization", aadToken);
                    requestMsg.Headers.UserAgent.ParseAdd("GameConfigCoreEditor/10.0.0.0 GameConfigEditor/10.0.0.0");
                    requestMsg.Headers.Expect.ParseAdd("100-continue");
                    
                    if (!requestMsg.Headers.Contains("Accept"))
                    {
                        
                    }*/

                    /*XdtsTokenRequest token = new XdtsTokenRequest(scid, sandboxes);
                    requestMsg.Content = new StringContent(JsonConvertHelper.Serialize<XdtsTokenRequest>(token));*/

                    var requestContent = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, sandboxes));
                    requestMsg.Content = new StringContent(requestContent);
                    requestMsg.Content.Headers.ContentType.MediaType = "application/json";


                    /*reqToken = new XdtsTokenRequest(scid, sandboxes);
                    requestMsg.Content = new StringContent(JsonConvert.SerializeObject(reqToken));*/

                    return requestMsg;
                })).Response;

                response.EnsureSuccessStatusCode();

                string contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType == "text/html")
                {
                    // The response content type is HTML, handle it accordingly
                    // For example, you can read the HTML content directly or log a message
                    Log.WriteLog("HTML!!!!");
                    string htmlContent = await response.Content.ReadAsStringAsync();
                    Log.WriteLog("Received HTML content: " + htmlContent);
                }

                response.Content.Headers.ContentType.MediaType = "application/json";
                response.Content.Headers.ContentLength = 1658;

                Log.WriteLog("Fetch xdts Token succeeded.");

                var token = await response.Content.DeserializeJsonAsync<XasTokenResponse>(); // HERE Error: unexpected error found. Unexpected character encountered while parsing value: <.Path '', line 0, position 0.

                string key = AuthTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, scid, sandboxes);
                this.ETokenCache.Value.UpdateToken(key, token);

                return token;
            }
        }

        protected async Task<XasTokenResponse> FetchXstsToken(string msaToken, string sandbox)
        {
            // Get XASU token
            XasTokenResponse token = null;
            using (var tokenRequest = new XboxLiveHttpRequest())
            {
                HttpResponseMessage response = (await tokenRequest.SendAsync(() =>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Post, ClientSettings.Singleton.XASUEndpoint);

                    XasuTokenRequest xasuTokenRequest = new XasuTokenRequest();
                    xasuTokenRequest.Properties["SiteName"]  = "user.auth.xboxlive.com";
                    xasuTokenRequest.Properties["RpsTicket"] = $"d={msaToken}";

                    var requestContent = JsonConvert.SerializeObject(xasuTokenRequest);
                    requestMsg.Content = new StringContent(requestContent);
                    requestMsg.Content.Headers.ContentType.MediaType = "application/json";

                    return requestMsg;
                })).Response;

                // Get XASU token with MSA token
                response.EnsureSuccessStatusCode();
                Log.WriteLog("Fetch XASU token succeeded.");

                token = await response.Content.DeserializeJsonAsync<XasTokenResponse>();
            }

            // Get XSTS token
            using (var tokenRequest = new XboxLiveHttpRequest())
            {
                HttpResponseMessage response = (await tokenRequest.SendAsync(() =>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Post, ClientSettings.Singleton.XSTSEndpoint);

                    XstsTokenRequest xstsTokenRequest = new XstsTokenRequest(sandbox)
                    {
                        RelyingParty = "http://xboxlive.com"
                    };
                    xstsTokenRequest.Properties["UserTokens"] = new[] { token.Token };

                    var requestContent = JsonConvert.SerializeObject(xstsTokenRequest);
                    requestMsg.Content = new StringContent(requestContent);
                    requestMsg.Content.Headers.ContentType.MediaType = "application/json";

                    return requestMsg;
                })).Response;

                // Get XASU token with MSA token
                response.EnsureSuccessStatusCode();
                Log.WriteLog("Fetch XSTS token succeeded.");

                token = await response.Content.DeserializeJsonAsync<XasTokenResponse>();
            }

            string key = AuthTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, string.Empty, sandbox);
            this.XTokenCache.Value.UpdateToken(key, token);

            return token;
        }
    }
}
