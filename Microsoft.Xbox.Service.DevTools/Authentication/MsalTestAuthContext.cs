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
        private readonly string[] scopes = new[] { "Xboxlive.signin", "Xboxlive.offline_access" };  
        private readonly IPublicClientApplication clientApp;
        private AuthenticationResult authResult;
        private MsalTokenCache tokenCache = new MsalTokenCache();
        private IAccount userAccount;

        public MsalTestAuthContext(string userName)
        {
            const string ClientId = "b1eab458-325b-45a5-9692-ad6079c1eca8"; 
            const string Instance = "https://login.microsoftonline.com/consumers/";

            this.clientApp = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority($"{Instance}{this.Tenant}") 
                .WithDefaultRedirectUri()
                .Build();

            this.tokenCache.EnableSerialization(this.clientApp.UserTokenCache);

            this.UserName = userName;
            this.userAccount = this.SearchAccounts().GetAwaiter().GetResult();
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.TestAccount; 

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.XASUEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return this.HasCredentialAsync().GetAwaiter().GetResult(); }
        }

        public string Tenant => "consumers";

        public async Task<bool> HasCredentialAsync()
        {
            var accounts = await this.clientApp.GetAccountsAsync();
            return accounts.FirstOrDefault() != null;
        }

        public async Task<IAccount> SearchAccounts()
        {
            var accounts = await this.clientApp.GetAccountsAsync();
            var cachedAccount = accounts.SingleOrDefault(account => string.Compare(account.Username, this.UserName, StringComparison.OrdinalIgnoreCase) == 0);

            return this.userAccount;
        }

        public async Task<string> AcquireTokenSilentAsync()
        {
            if (this.userAccount == null)
            {
                throw new InvalidOperationException("No cached user found, please call SignInAsync to sign in a user.");
            }

            this.authResult = await this.clientApp.AcquireTokenSilent(this.scopes, this.userAccount).ExecuteAsync();

            return this.authResult?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            this.authResult = await this.clientApp.AcquireTokenInteractive(this.scopes)
                        .WithLoginHint(this.UserName)
                        .WithPrompt(Prompt.ForceLogin)
                        .ExecuteAsync();

            return this.authResult?.AccessToken;
        }

        public async Task<string> AcquireTokenCachedAsync()
        {
            this.authResult = await this.clientApp.AcquireTokenInteractive(this.scopes)
                        .WithLoginHint(this.UserName)
                        .WithPrompt(Prompt.NoPrompt)
                        .ExecuteAsync();

            return this.authResult?.AccessToken;
        }
    }
}