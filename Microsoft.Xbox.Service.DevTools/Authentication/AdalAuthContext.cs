// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Xbox.Services.DevTools.Common;

    internal class AdalAuthContext : IAuthContext
    {
        private AdalTokenCache tokenCache = new AdalTokenCache();
        private AuthenticationContext authContext;
        private UserIdentifier userIdentifier;

        public AdalAuthContext(string userName, string tenant)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                tenant = "common";
            }

            this.authContext = new AuthenticationContext(
                string.Format(ClientSettings.Singleton.ActiveDirectoryAuthenticationEndpoint, tenant), 
                this.tokenCache);
            this.UserName = userName;
            this.userIdentifier = string.IsNullOrEmpty(userName) ?
                UserIdentifier.AnyUser :
                new UserIdentifier(this.UserName, UserIdentifierType.RequiredDisplayableId);
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.WindowsDevCenter;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.UDCAuthEndpoint;

        public virtual bool HasCredential
        {
            get { return this.authContext != null && this.authContext.TokenCache.Count > 0; }
        }

        public string UserName { get; private set; }

        public virtual async Task<string> AcquireTokenSilentAsync()
        {
            AuthenticationResult result = await this.authContext.AcquireTokenSilentAsync(
                ClientSettings.Singleton.AADResource,
                ClientSettings.Singleton.AADApplicationId,
                this.userIdentifier);

            return result.AccessToken;
        }

        public virtual async Task<string> AcquireTokenAsync()
        {
            AuthenticationResult result = await this.authContext.AcquireTokenAsync(
                ClientSettings.Singleton.AADResource,
                ClientSettings.Singleton.AADApplicationId, new Uri("urn:ietf:wg:oauth:2.0:oob"),
                new PlatformParameters(string.IsNullOrEmpty(this.UserName)? PromptBehavior.SelectAccount : PromptBehavior.Auto),
                this.userIdentifier);
            this.UserName = result.UserInfo.DisplayableId;
            return result?.AccessToken;
        }
    }
}
