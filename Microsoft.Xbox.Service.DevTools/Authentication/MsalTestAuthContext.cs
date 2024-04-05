// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.Xbox.Services.DevTools.Common;

    internal class MsalTestAuthContext : IAuthContext
    {
        // Added fields
        private readonly string[] scopes = new[] { "Xboxlive.signin", "Xboxlive.offline_access" };  
        private readonly IPublicClientApplication clientApp;
        private AuthenticationResult authResult;

        public MsalTestAuthContext(string userName)
        {
            const string ClientId = "b1eab458-325b-45a5-9692-ad6079c1eca8"; 
            const string Instance = "https://login.microsoftonline.com/consumers/";

            this.clientApp = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority($"{Instance}{this.Tenant}") 
                .WithDefaultRedirectUri()
                .Build();

            this.UserName = userName;
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.TestAccount; 

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.UDCAuthEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return false; }
        }

        public string Tenant => "consumers"; 

        public async Task<string> AcquireTokenSilentAsync()
        {
            var accounts = await this.clientApp.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            this.authResult = await this.clientApp.AcquireTokenSilent(this.scopes, firstAccount).ExecuteAsync();

            return this.authResult?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            var accounts = await this.clientApp.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            this.authResult = await this.clientApp.AcquireTokenInteractive(this.scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
            return this.authResult?.AccessToken;
        }
    }
}