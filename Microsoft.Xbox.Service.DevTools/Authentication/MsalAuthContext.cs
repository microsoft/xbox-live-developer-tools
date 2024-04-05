// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.Xbox.Services.DevTools.Common;

    internal class MsalAuthContext : IAuthContext
    {
        // Added fields
        private readonly string[] scopes = new[] { "Xboxlive.signin", "Xboxlive.offline_access" }; // "Xboxlive.signin", "Xboxlive.offline_access" - Need to change to What? 
        private readonly IPublicClientApplication publicClientApplication;
        private AuthenticationResult authResult;

        public MsalAuthContext(string userName)
        {
            const string ClientId = "b1eab458-325b-45a5-9692-ad6079c1eca8"; // Need to determine
            const string Instance = "https://login.microsoftonline.com/consumers/"; // What is this???

            this.publicClientApplication = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority($"{Instance}{this.Tenant}") 
                .WithDefaultRedirectUri()
                .Build();

            this.UserName = userName;
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.TestAccount; // Changed to TestAccount

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.UDCAuthEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return false; }
        }

        public string Tenant => "9188040d-6c67-4c5b-b112-36a304b66dad"; // Changed Tenant based off JWT

        public async Task<string> AcquireTokenSilentAsync()
        {
            var accounts = await this.publicClientApplication.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            this.authResult = await this.publicClientApplication.AcquireTokenSilent(this.scopes, firstAccount).ExecuteAsync();

            return this.authResult?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            var accounts = await this.publicClientApplication.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            this.authResult = await this.publicClientApplication.AcquireTokenInteractive(this.scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
            Log.WriteLog(this.authResult.AccessToken);
            return this.authResult?.AccessToken;
        }
    }
}