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

        public Lazy<XstsTokenCache> XTokenCache { get; } = new Lazy<XstsTokenCache>();

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
                    XdtsTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, scid, sandboxes);
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

        public virtual async Task<XdtsTokenResponse> GetXTokenAsync(string scid, string sandbox, bool forceRefresh)
        {
            if (this.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found.");
            }

            XdtsTokenResponse xToken = null;

            if (!forceRefresh)
            {
                // return cachaed token if we have one and didn't expire
                string cacheKey =
                    XstsTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, scid, sandbox);
                this.XTokenCache.Value.TryGetCachedToken(cacheKey, out xToken);
            }

            if (xToken == null)
            {
                var msaToken = await this.AuthContext.AcquireTokenSilentAsync();
                var xstsToken = await this.FetchXstsToken(msaToken, scid, sandbox);
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

            string aadToken = await this.AuthContext.AcquireTokenAsync();
            XdtsTokenResponse token = await this.FetchXdtsToken(aadToken, string.Empty, null);

            var account = new DevAccount(token, this.AuthContext.AccountSource, tenant);
            return account;
        }

        public async Task<TestAccount> SignInTestAccountAsync()
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
            XdtsTokenResponse token = await this.FetchXstsToken(msaToken, string.Empty, null);

            var account = new TestAccount(token);
            return account;
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

                string key = XdtsTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, scid, sandboxes);
                this.ETokenCache.Value.UpdateToken(key, token);

                return token;
            }
        }

        protected async Task<XdtsTokenResponse> FetchXstsToken(string msaToken, string scid, string sandbox)
        {
            // if no sandbox provided, use XDKS.1 for login
            if (string.IsNullOrEmpty(sandbox))
                sandbox = "XDKS.1";

            // Get XASU token with MSA token
            HttpClient hc = new HttpClient();
            var hrm = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(ClientSettings.Singleton.XASUEndpoint)
            };

            hrm.Headers.TryAddWithoutValidation("x-xbl-contract-version", "0");

            string requestBody = "{\"RelyingParty\":\"http://auth.xboxlive.com\",\"TokenType\" : \"JWT\",\"Properties\":" +
                                 "{\"AuthMethod\":\"RPS\",\"SiteName\" : \"user.auth.xboxlive.com\",\"RpsTicket\":" +
                                 "\"d=" + msaToken + "\"}}";
            hrm.Content = new StringContent(requestBody);
            hrm.Content.Headers.ContentType.MediaType = "application/json";

            var response = await hc.SendAsync(hrm);
            response.EnsureSuccessStatusCode();
            Log.WriteLog("Fetch XASU token succeeded.");

            var token = await response.Content.DeserializeJsonAsync<XdtsTokenResponse>();

            // Get XSTS Token
            hrm = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(ClientSettings.Singleton.XSTSEndpoint)
            };

            hrm.Headers.TryAddWithoutValidation("x-xbl-contract-version", "0");

            requestBody = "{\"RelyingParty\":\"http://xboxlive.com\",\"TokenType\":\"JWT\",\"Properties\":{\"UserTokens\":[\"" + token.Token + "\"],\"SandboxId\":\"" + sandbox + "\",}}";

            hrm.Content = new StringContent(requestBody);
            hrm.Content.Headers.ContentType.MediaType = "application/json";

            response = await hc.SendAsync(hrm);
            response.EnsureSuccessStatusCode();
            Log.WriteLog("Fetch XSTS token succeeded.");

            token = await response.Content.DeserializeJsonAsync<XdtsTokenResponse>();

            string key = XstsTokenCache.GetCacheKey(this.AuthContext.UserName, this.AuthContext.AccountSource, this.AuthContext.Tenant, scid, sandbox);
            this.XTokenCache.Value.UpdateToken(key, token);

            return token;
        }
    }
}
