// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTool.Authentication
{
    using Microsoft.Identity.Client;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTool.Common;

    internal class MsalAuthContext : IAuthContext
    {
        private readonly string[] Scopes = new[] { "User.Read" };
        private MsalTokenCache tokenCache = new MsalTokenCache();
        private IPublicClientApplication publicClientApplication;
        private IUser cachedUser;

        public DevAccountSource AccountSource { get; } = DevAccountSource.XboxDeveloperPortal;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.XmintAuthEndpoint;

        public string UserName { get; }

        public MsalAuthContext(string userName)
        {
            publicClientApplication = new PublicClientApplication(ClientSettings.Singleton.MsalXboxLiveClientId,
                ClientSettings.Singleton.ActiveDirectoryAuthenticationEndpoint,
                tokenCache.TokenCache);
            this.UserName = userName;
            cachedUser = publicClientApplication.Users.SingleOrDefault(user => string.Compare(user.DisplayableId, userName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public bool HasCredential
        {
            get { return this.publicClientApplication.Users.FirstOrDefault() != null; }
        }

        public async Task<string> AcquireTokenSilentAsync()
        {
            if (cachedUser == null)
            {
                throw new InvalidOperationException("No cached user found, please call SignInAsync to sign in a user.");
            }

            AuthenticationResult result = await this.publicClientApplication.AcquireTokenSilentAsync(Scopes, cachedUser);

            return result?.AccessToken;
        }

        public async Task<string> AcquireTokenAsync()
        {
            AuthenticationResult result = string.IsNullOrEmpty(this.UserName)? 
                await this.publicClientApplication.AcquireTokenAsync(Scopes) :
                await this.publicClientApplication.AcquireTokenAsync(Scopes, UserName);
            this.cachedUser = result.User;
            return result.AccessToken;
        }
    }
}
