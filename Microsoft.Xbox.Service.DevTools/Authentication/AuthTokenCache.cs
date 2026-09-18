// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Xbox.Services.DevTools.Common;
    using Newtonsoft.Json;

    internal class AuthTokenCache
    {
        private string cacheFile;

        private static readonly object TokenLock = new object();

        public AuthTokenCache(string cacheFile)
        {
            this.cacheFile = cacheFile;

            Directory.CreateDirectory(ClientSettings.Singleton.CacheFolder);

            this.LoadTokenCache();
        }

        public Dictionary<string, XasTokenResponse> CachedTokens { get; set; } = new Dictionary<string, XasTokenResponse>();

        public void UpdateToken(string key, XasTokenResponse token)
        {
            lock (TokenLock)
            {
                this.CachedTokens[key] = token;
                this.SaveTokenCache();
            }
        }

        public bool TryGetCachedToken(string key, out string token)
        {
            token = string.Empty;
            if (this.CachedTokens.TryGetValue(key, out XasTokenResponse cachedToken)
                && cachedToken != null && !string.IsNullOrEmpty(cachedToken.Token) && cachedToken.NotAfter >= DateTime.UtcNow)
            {
                Log.WriteLog($"Using token from cache for {key}.");

                token = cachedToken.Token;
                return true;
            }

            return false;
        }

        public bool TryGetCachedToken(string key, out XasTokenResponse token)
        {
            token = null;
            if (this.CachedTokens.TryGetValue(key, out XasTokenResponse cachedToken)
                && cachedToken != null && !string.IsNullOrEmpty(cachedToken.Token) && cachedToken.NotAfter >= DateTime.UtcNow)
            {
                Log.WriteLog($"Using token from cache for {key}.");

                token = cachedToken;
                return true;
            }

            return false;
        }

        public static string GetCacheKey(string userName, DevAccountSource accountSource, string tenant, string scid, IEnumerable<string> sandboxes)
        {
            return GetCacheKey(userName, accountSource, tenant, scid, sandboxes == null? string.Empty : string.Join(" ", sandboxes));
        }

        public static string GetCacheKey(string userName, DevAccountSource accountSource, string tenant, string scid, string sandbox)
        {
            string keyFullstring = userName + accountSource.ToString() + tenant + scid + sandbox;
            return keyFullstring.GetHashCode().ToString();
        }

        public void RemoveUserTokenCache(string userName)
        {
            lock (TokenLock)
            {
                this.CachedTokens = this.CachedTokens
                        .Where(o => !IsTokenForUser(o.Value, userName))
                        .ToDictionary(o => o.Key, o => o.Value);

                // The cache outlives the process, so dropping the tokens from the dictionary alone
                // would leave them on disk and let the next run serve a token for a signed out user.
                this.SaveTokenCache();
            }
        }

        public void Clear()
        {
            lock (TokenLock)
            {
                this.CachedTokens = new Dictionary<string, XasTokenResponse>();
                this.SaveTokenCache();
            }
        }

        private static bool IsTokenForUser(XasTokenResponse token, string userName)
        {
            // Not every cached token carries an "enm" claim (test account xsts tokens do not),
            // so probe for it rather than indexing straight into the claim dictionary.
            if (token?.DisplayClaims == null || !token.DisplayClaims.TryGetValue("enm", out object name))
            {
                return false;
            }

            return string.Compare(name?.ToString(), userName, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private void SaveTokenCache()
        {
            string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, this.cacheFile);
            File.WriteAllText(cacheFilePath, JsonConvert.SerializeObject(this.CachedTokens));
        }

        private void LoadTokenCache()
        {
            lock (TokenLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, this.cacheFile);

                Dictionary<string, XasTokenResponse> cache = JsonConvert.DeserializeObject<Dictionary<string, XasTokenResponse>>(File.Exists(cacheFilePath) 
                    ? File.ReadAllText(cacheFilePath) : string.Empty);

                if (cache != null)
                {
                    // Remove expired token, to prevent cache over grow
                    this.CachedTokens = cache
                        .Where(o => o.Value.NotAfter > DateTime.Now)
                        .ToDictionary(o => o.Key, o => o.Value);
                }
                else
                {
                    this.CachedTokens = new Dictionary<string, XasTokenResponse>();
                }
            }
        }
    }
}
