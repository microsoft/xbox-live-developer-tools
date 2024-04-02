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
        private readonly string[] scopes = new[] { "https://graph.microsoft.com/.default" }; // Change scope for token acquisition
        private readonly IPublicClientApplication publicClientApplication;
        private AuthenticationResult authResult;

        public MsalAuthContext(string userName)
        {
            const string ClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1"; // XboxLive
            const string Instance = "https://login.microsoftonline.com/";

            this.publicClientApplication = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority($"{Instance}{Tenant}") 
                .WithDefaultRedirectUri()
                .Build();

            this.UserName = userName;
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.WindowsDevCenter; // Change DevAccountSource

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.UDCAuthEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return false; }
        }

        public string Tenant => "f8cdef31-a31e-4b4a-93e4-5f571e91255a";

        public async Task<string> AcquireTokenSilentAsync()
        {
            // AuthenticationResult result = await this.publicClientApplication.AcquireTokenSilentAsync(this.scopes, this.cachedAccount);
            // Warning 1998

            var accounts = await publicClientApplication.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            this.authResult = await publicClientApplication.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync();

            return this.authResult?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            var accounts = await publicClientApplication.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            this.authResult = await publicClientApplication.AcquireTokenInteractive(scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();

            return this.authResult?.AccessToken;
        }
    }
}