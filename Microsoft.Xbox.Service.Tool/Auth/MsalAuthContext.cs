//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************


using System;

namespace Microsoft.Xbox.Services.Tool
{
    using Microsoft.Identity.Client;
    using System.Linq;
    using System.Threading.Tasks;

    internal class MsalAuthContext : IAuthContext
    {
        private readonly string[] Scopes = new[] { "User.Read" };
        private readonly IPublicClientApplication PublicClientApplication = new PublicClientApplication(ClientSettings.Singleton.MsalXboxLiveClientId);

        public DevAccountSource AccountSource { get; } = DevAccountSource.XboxDeveloperPortal;

        public string XtdsEndpoint { get; set; } = ClientSettings.Singleton.XmintAuthEndpoint;

        public bool HasCredential
        {
            get { return this.PublicClientApplication.Users.FirstOrDefault() != null; }
        }

        public async Task<string> AcquireTokenSilentAsync()
        {

            var result = await this.PublicClientApplication.AcquireTokenSilentAsync(Scopes, PublicClientApplication.Users.FirstOrDefault());
            return result.AccessToken;
        }

        public async Task<string> AcquireTokenAsync(string userName)
        {
            var result = await this.PublicClientApplication.AcquireTokenAsync(Scopes, userName);
            return result.AccessToken;
        }
    }
}
