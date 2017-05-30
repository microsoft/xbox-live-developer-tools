using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Xbox.Services.Tool
{
    internal class AadAuthContext
    {
        private AuthenticationContext authContext =
            new AuthenticationContext(ClientSettings.Singleton.ActiveDirectoryAuthenticationEndpoint + "common");

        public virtual bool HasCredential
        {
            get { return authContext != null && authContext.TokenCache.Count > 0; }
        }

        public virtual Task<string> AcquireTokenSilentAsync(string resource, string clientId)
        {
            return this.authContext.AcquireTokenSilentAsync(resource, clientId)
                .ContinueWith((t => t.Result.AccessToken));
        }

        public virtual Task<string> AcquireTokenAsync(string resource, string clientId, UserCredential clientCredential)
        {
            return this.authContext.AcquireTokenAsync(resource, clientId, clientCredential)
                .ContinueWith((t => t.Result.AccessToken));
        }

        public virtual Task<string> AcquireTokenAsync(string resource, string clientId, Uri redirectUri,
            IPlatformParameters parameters, UserIdentifier userId)
        {
            return this.authContext.AcquireTokenAsync(resource, clientId, redirectUri, parameters, userId)
                .ContinueWith((t => t.Result.AccessToken));
        }


    }
}
