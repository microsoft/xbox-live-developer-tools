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

        public Dictionary<string, XdtsTokenResponse> CachedTokens { get; set; } = new Dictionary<string, XdtsTokenResponse>();

        public void UpdateToken(string key, XdtsTokenResponse token)
        {
            lock (TokenLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, this.cacheFile);
                this.CachedTokens[key] = token;
                File.WriteAllText(cacheFilePath, JsonConvert.SerializeObject(this.CachedTokens));
            }
        }

        public bool TryGetCachedToken(string key, out string token)
        {
            token = string.Empty;
            if (this.CachedTokens.TryGetValue(key, out XdtsTokenResponse cachedToken)
                && cachedToken != null && !string.IsNullOrEmpty(cachedToken.Token) && cachedToken.NotAfter >= DateTime.UtcNow)
            {
                Log.WriteLog($"Using token from cache for {key}.");

                token = cachedToken.Token;
                return true;
            }

            return false;
        }

        public bool TryGetCachedToken(string key, out XdtsTokenResponse token)
        {
            token = null;
            if (this.CachedTokens.TryGetValue(key, out XdtsTokenResponse cachedToken)
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
                        .Where(o => string.Compare(o.Value.DisplayClaims["enm"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase) != 0)
                        .ToDictionary(o => o.Key, o => o.Value);
            }
        }

        private void LoadTokenCache()
        {
            lock (TokenLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, this.cacheFile);

                Dictionary<string, XdtsTokenResponse> cache = JsonConvert.DeserializeObject<Dictionary<string, XdtsTokenResponse>>(File.Exists(cacheFilePath) 
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
                    this.CachedTokens = new Dictionary<string, XdtsTokenResponse>();
                }
            }
        }
    }
}
