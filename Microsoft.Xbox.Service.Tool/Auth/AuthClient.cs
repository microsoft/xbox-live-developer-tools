//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************


namespace Microsoft.Xbox.Services.Tool
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;

    internal abstract class AuthClient
    {
        protected ConcurrentDictionary<string, XdtsTokenResponse> cachedTokens = new ConcurrentDictionary<string, XdtsTokenResponse>();

        protected bool TryGetCachedToken(string key, out string token)
        {
            token = string.Empty;
            XdtsTokenResponse cachedToken = null;
            if (cachedTokens.TryGetValue(key, out cachedToken)
                && (cachedToken != null && !string.IsNullOrEmpty(cachedToken.Token) && cachedToken.NotAfter >= DateTime.UtcNow))
            {
                Log.WriteLog($"Using token from cache for {key}.");

                token = cachedToken.Token;
                return true;
            }

            return false;
        }

        public abstract bool HasCredential { get; }

        public abstract Task<string> GetETokenAsync(string scid, string sandbox);

        public abstract Task<string> SignInAsync(string emailaddress, SecureString password);

    }
}
