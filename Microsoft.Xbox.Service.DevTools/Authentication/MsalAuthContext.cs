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
        private readonly string[] scopes = new[] { "User.Read" };
        private MsalTokenCache tokenCache = new MsalTokenCache();
        private IPublicClientApplication publicClientApplication;
        private IUser cachedUser;

        public MsalAuthContext(string userName)
        {
            this.publicClientApplication = new PublicClientApplication(
                ClientSettings.Singleton.MsalXboxLiveClientId,
                ClientSettings.Singleton.ActiveDirectoryAuthenticationEndpoint,
                this.tokenCache.TokenCache);
            this.UserName = userName;
            this.cachedUser = this.publicClientApplication.Users.SingleOrDefault(
                user => string.Compare(user.DisplayableId, userName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.XboxDeveloperPortal;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.XmintAuthEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return this.publicClientApplication.Users.FirstOrDefault() != null; }
        }

        public async Task<string> AcquireTokenSilentAsync()
        {
            if (this.cachedUser == null)
            {
                throw new InvalidOperationException("No cached user found, please call SignInAsync to sign in a user.");
            }

            AuthenticationResult result = await this.publicClientApplication.AcquireTokenSilentAsync(this.scopes, this.cachedUser);

            return result?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            AuthenticationResult result = string.IsNullOrEmpty(this.UserName)? 
                await this.publicClientApplication.AcquireTokenAsync(this.scopes) :
                await this.publicClientApplication.AcquireTokenAsync(this.scopes, this.UserName);
            this.cachedUser = result.User;
            return result.AccessToken;
        }
    }
}
