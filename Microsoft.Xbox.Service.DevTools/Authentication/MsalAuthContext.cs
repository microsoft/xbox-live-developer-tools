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
        private readonly string[] scopes = new[] { "Xboxlive.signin", "Xboxlive.offline_access" };
        private readonly IPublicClientApplication publicClientApplication;
        private IUser cachedAccount;

        public MsalAuthContext(string userName)
        {
            this.publicClientApplication = new PublicClientApplication(ClientSettings.Singleton.MsalXboxLiveClientId, "https://login.microsoftonline.com/consumers/");

            this.UserName = userName;
            this.cachedAccount = this.publicClientApplication.Users.SingleOrDefault(
                user => string.Compare(user.Name, userName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.TestAccount;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.XASUEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return this.publicClientApplication.Users.FirstOrDefault() != null; }
        }

        public string Tenant => "common";

        public async Task<string> AcquireTokenSilentAsync()
        {
            if (this.cachedAccount == null)
            {
                throw new InvalidOperationException("No cached user found, please call SignInAsync to sign in a user.");
            }

            AuthenticationResult result = await this.publicClientApplication.AcquireTokenSilentAsync(this.scopes, this.cachedAccount);

            return result?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            AuthenticationResult result = await this.publicClientApplication.AcquireTokenAsync(this.scopes, this.UserName, UIBehavior.Never, string.Empty);
            this.cachedAccount = result.User;
            return result.AccessToken;
        }
    }
}