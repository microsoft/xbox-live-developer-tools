// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System;
    using System.Threading.Tasks;

    internal class AdalAuthContext : IAuthContext
    {
        private AdalTokenCache tokenCache = new AdalTokenCache();

        private AuthenticationContext authContext;
        private UserIdentifier userIdentifier;

        public DevAccountSource AccountSource { get; } = DevAccountSource.WindowsDevCenter;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.UDCAuthEndpoint;

        public virtual bool HasCredential
        {
            get { return authContext != null && authContext.TokenCache.Count > 0; }
        }
        public string UserName { get; }

        public AdalAuthContext(string userName)
        {
            authContext = new AuthenticationContext(ClientSettings.Singleton.ActiveDirectoryAuthenticationEndpoint, tokenCache);
            this.UserName = userName;
            this.userIdentifier = new UserIdentifier(UserName, UserIdentifierType.RequiredDisplayableId);
        }

        public virtual async Task<string> AcquireTokenSilentAsync()
        {
            AuthenticationResult result = await this.authContext.AcquireTokenSilentAsync(ClientSettings.Singleton.AADResource,
                ClientSettings.Singleton.AADApplicationId,
                userIdentifier);

            return result.AccessToken;
        }

        public virtual async Task<string> AcquireTokenAsync()
        {
            AuthenticationResult result = await this.authContext.AcquireTokenAsync(
                ClientSettings.Singleton.AADResource,
                ClientSettings.Singleton.AADApplicationId, new Uri("urn:ietf:wg:oauth:2.0:oob"),
                new PlatformParameters(PromptBehavior.Auto),
                userIdentifier);

            return result?.AccessToken;
        }
    }
}
