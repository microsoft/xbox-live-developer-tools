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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System.Security;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Net.Http.Headers;
    using System.Collections.Concurrent;

    internal class XDTSTokenRequest
    {
        public XDTSTokenRequest(string scid = "", string sandbox = "")
        {
            if (!string.IsNullOrEmpty(scid))
            {
                Properties["Scid"] = scid;
            }

            if (!string.IsNullOrEmpty(sandbox))
            {
                Properties["Sandboxes"] = sandbox;
            }
        }

        public string TokenType { get; set; } = ClientSettings.Singleton.XDTSToolTokenType;

        public string RelyingParty { get; set; } = ClientSettings.Singleton.XDTSToolRelyingParty;

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    }

    public class UDCAuthClient: AuthClient
    {
        private AuthenticationContext authContext;

        public override bool HasCredential()
        {
            return authContext != null && authContext.ValidateAuthority;
        }

        public override async Task<string> GetETokenAsync(string scid, string sandbox)
        {
            string token;
            // return cachaed token if we have one and didn't expire
            if (!this.TryGetCachedToken(scid+sandbox, out token)
                && (this.authContext != null))
            {
                var authResult = await this.authContext.AcquireTokenSilentAsync(ClientSettings.Singleton.AADResource, ClientSettings.Singleton.AADApplicationId);
                var xtdsToken = await FetchXdtsToken(authResult, scid, sandbox);
                token = xtdsToken.Token;
            }

            return token;
        }

        public override async Task<string> SignInAsync(string emailaddress, SecureString password)
        {
            var authContext = new AuthenticationContext(ClientSettings.Singleton.ActiveDirectoryAuthenticationEndpoint + "common");

            AuthenticationResult authResult = null;
            if (password != null && password.Length != 0)
            {
                var userCred = new UserPasswordCredential(emailaddress, password);
                authResult = await authContext.AcquireTokenAsync(ClientSettings.Singleton.AADResource, ClientSettings.Singleton.AADApplicationId, userCred);
            }
            else
            {
                UserIdentifier userId = new UserIdentifier(emailaddress, UserIdentifierType.RequiredDisplayableId);
                authResult = await authContext.AcquireTokenAsync(ClientSettings.Singleton.AADResource, ClientSettings.Singleton.AADApplicationId, new Uri("urn:ietf:wg:oauth:2.0:oob"), new PlatformParameters(PromptBehavior.Auto), userId);
            }

            var token = await FetchXdtsToken(authResult, string.Empty, string.Empty);

            // now save authContext and token cache
            this.authContext = authContext;

            return token.Token;
        }

        private async Task<XdtsTokenResponse> FetchXdtsToken(AuthenticationResult authResult, string scid, string sandbox)
        {
            var tokenRequest = new XboxLiveHttpRequest();
            var requestMsg = new HttpRequestMessage(HttpMethod.Post, ClientSettings.Singleton.UDCAuthEndpoint);

            var requestContent = JsonConvert.SerializeObject(new XDTSTokenRequest(scid, sandbox));
            requestMsg.Content = new StringContent(requestContent);

            requestMsg.Headers.Authorization = new AuthenticationHeaderValue(authResult.AccessToken);

            string response = await tokenRequest.SendAsync(requestMsg);
            Log.WriteLog("Fetch xdts Token succeeded.");

            var token = JsonConvert.DeserializeObject<XdtsTokenResponse>(response);
            this.cachedTokens[scid + sandbox] = token;

            return token;
        }
    }
}
