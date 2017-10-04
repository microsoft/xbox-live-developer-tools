// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System;
    using System.Threading.Tasks;

    internal class AadAuthContext : IAuthContext
    {
        private readonly AuthenticationContext authContext =
            new AuthenticationContext(ClientSettings.Singleton.ActiveDirectoryAuthenticationEndpoint + "common");

        public DevAccountSource AccountSource { get; } = DevAccountSource.UniversalDeveloperCenter;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.UDCAuthEndpoint;

        public virtual bool HasCredential
        {
            get { return authContext != null && authContext.TokenCache.Count > 0; }
        }

        public virtual async Task<string> AcquireTokenSilentAsync()
        {
            AuthenticationResult result =
                await this.authContext.AcquireTokenSilentAsync(ClientSettings.Singleton.AADResource,
                    ClientSettings.Singleton.AADApplicationId);
            return result.AccessToken;
        }

        public virtual async Task<string> AcquireTokenAsync(string userName)
        {
            AuthenticationResult result = null;
            if (String.IsNullOrEmpty(userName))
            {
                result = await this.authContext.AcquireTokenAsync(
                    ClientSettings.Singleton.AADResource,
                    ClientSettings.Singleton.AADApplicationId, new Uri("urn:ietf:wg:oauth:2.0:oob"),
                    new PlatformParameters(PromptBehavior.Always)
                    );
            }
            else
            {
                result = await this.authContext.AcquireTokenAsync(
                    ClientSettings.Singleton.AADResource,
                    ClientSettings.Singleton.AADApplicationId, new Uri("urn:ietf:wg:oauth:2.0:oob"),
                    new PlatformParameters(PromptBehavior.Auto),
                    new UserIdentifier(userName, UserIdentifierType.RequiredDisplayableId));
            }

            return result?.AccessToken;
        }
    }
}
