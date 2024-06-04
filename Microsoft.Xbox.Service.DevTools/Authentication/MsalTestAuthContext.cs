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
        private readonly IPublicClientApplication publicClientApplication;
        private IAccount cachedAccount;

        public MsalTestAuthContext(string userName)
        {
            this.publicClientApplication = PublicClientApplicationBuilder.Create(ClientSettings.Singleton.MsalXboxLiveClientId)
                .WithAuthority($"{ClientSettings.Singleton.MsalLiveAuthority}{this.Tenant}")
                .WithDefaultRedirectUri()
                .Build();
            this.UserName = userName;
            this.InitializeCachedAccount().GetAwaiter().GetResult();
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.TestAccount;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.XASUEndpoint;

        public string UserName { get; }

        public bool HasCredential => this.HasAccountsAsync().GetAwaiter().GetResult();

        public string Tenant => "consumers";

        public async Task<string> AcquireTokenSilentAsync()
        {
            if (this.cachedAccount == null)
            {
                throw new InvalidOperationException("No cached user found, please call SignInAsync to sign in a user.");
            }

            AuthenticationResult authResult = await this.publicClientApplication.AcquireTokenSilent(this.scopes, this.cachedAccount).ExecuteAsync();

            return authResult?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            var accounts = await this.publicClientApplication.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            AuthenticationResult result = await this.publicClientApplication.AcquireTokenInteractive(this.scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
            return result?.AccessToken;
        }

        private async Task InitializeCachedAccount()
        {
            var accounts = await this.publicClientApplication.GetAccountsAsync();
            this.cachedAccount = accounts.SingleOrDefault(
        account => string.Compare(account.Username, this.UserName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public async Task<bool> HasAccountsAsync()
        {
            var accounts = await this.publicClientApplication.GetAccountsAsync();
            return accounts.FirstOrDefault() != null;
        }
    }
}