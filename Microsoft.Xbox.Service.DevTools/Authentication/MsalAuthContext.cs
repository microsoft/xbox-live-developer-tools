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
            this.UserName = userName;
            this.publicClientApplication = new PublicClientApplication(ClientSettings.Singleton.MsalXboxLiveClientId, ClientSettings.Singleton.MsalLiveAuthority);
            this.authResult = null;
        }

        public DevAccountSource AccountSource { get; } = DevAccountSource.WindowsDevCenter; // Change DevAccountSource

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.XASUEndpoint;

        public string UserName { get; }

        public bool HasCredential
        {
            get { return this.publicClientApplication.Users.FirstOrDefault() != null; }
        }

        public string Tenant => "consumers";

        public async Task<string> AcquireTokenSilentAsync()
        {
            return null;
        }

        public async Task<string> AcquireTokenAsync()
        {
            AuthenticationResult result = await this.publicClientApplication.AcquireTokenAsync(this.scopes, this.UserName, UIBehavior.Consent, string.Empty);
            // this.cachedAccount = result.User; Cannot support this anymore
            return result.AccessToken;
        }
    }
}