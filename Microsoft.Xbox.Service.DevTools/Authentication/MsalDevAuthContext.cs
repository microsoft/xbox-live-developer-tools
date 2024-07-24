// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.Xbox.Services.DevTools.Common;

    internal class MsalDevAuthContext : IAuthContext
    {
        private readonly string[] scopes = new[] { "https://partner.microsoft.com//.default" }; 
        private readonly IPublicClientApplication clientApp;
        private AuthenticationResult authResult;
        private MsalTokenCache tokenCache = new MsalTokenCache();
        private IAccount userAccount;

        public MsalDevAuthContext(string userName)
        {
            const string ClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1"; 
            const string Instance = "https://login.microsoftonline.com/";

            this.clientApp = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority($"{Instance}{this.Tenant}")
                .WithDefaultRedirectUri()
                .Build();

            this.tokenCache.EnableSerialization(this.clientApp.UserTokenCache);

            this.UserName = userName;
            this.userAccount = this.SearchAccounts().GetAwaiter().GetResult();
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.WindowsDevCenter; 

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.UDCAuthEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return this.HasCredentialAsync().GetAwaiter().GetResult(); }
        }

        public string Tenant => "common";

        public async Task<bool> HasCredentialAsync()
        {
            var accounts = this.clientApp != null
                ? await this.clientApp.GetAccountsAsync()
                : Enumerable.Empty<IAccount>();

            return accounts.Count() > 0;
        }

        public async Task<IAccount> SearchAccounts()
        {
            var accounts = await this.clientApp.GetAccountsAsync();
            this.userAccount = string.IsNullOrEmpty(this.UserName)
                ? accounts.SingleOrDefault()
                : accounts.SingleOrDefault(a => a.Username.Equals(this.UserName, StringComparison.OrdinalIgnoreCase));

            return this.userAccount;
        }

        public async Task<string> AcquireTokenSilentAsync()
        {
            this.authResult = await this.clientApp.AcquireTokenSilent(this.scopes, this.userAccount).ExecuteAsync();

            return this.authResult?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            if (this.userAccount != null)
            {
                this.authResult = await this.clientApp.AcquireTokenInteractive(this.scopes)
                                  .WithAccount(this.userAccount)
                                  .WithPrompt(Prompt.NoPrompt)
                                  .ExecuteAsync();
            }
            else
            {
                this.authResult = await this.clientApp.AcquireTokenInteractive(this.scopes)
                                  .WithPrompt(Prompt.SelectAccount)
                                  .ExecuteAsync();
            }
            
            return this.authResult?.AccessToken;
        }
    }
}