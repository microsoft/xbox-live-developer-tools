// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xbox.Services.DevTools.Common;

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DevTools.Common;


    internal class XdtsTokenCache
    {
        private const string CacheFile = "xdts.cache";

        private static readonly object tokenLock = new object();

        public Dictionary<string, XdtsTokenResponse> CachedTokens { get; set; } = new Dictionary<string, XdtsTokenResponse>();

        public XdtsTokenCache()
        {
            Directory.CreateDirectory(ClientSettings.Singleton.CacheFolder);

            LoadTokenCache();
        }

        public void UpdateToken(string key, XdtsTokenResponse token)
        {
            lock (tokenLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);
                CachedTokens[key] = token;
                File.WriteAllText(cacheFilePath, JsonConvert.SerializeObject(this.CachedTokens));
            }
        }

        public bool TryGetCachedToken(string key, out string token)
        {
            token = string.Empty;
            if (CachedTokens.TryGetValue(key, out XdtsTokenResponse cachedToken)
                && cachedToken != null && !string.IsNullOrEmpty(cachedToken.Token) && cachedToken.NotAfter >= DateTime.UtcNow)
            {
                Log.WriteLog($"Using token from cache for {key}.");

                token = cachedToken.Token;
                return true;
            }

            return false;
        }

        public static string GetCacheKey(string userName, DevAccountSource accountSource, string scid, IEnumerable<string> sandboxes)
        {
            string keyFullstring = userName + accountSource.ToString() + scid + (sandboxes == null? String.Empty: String.Join(" ", sandboxes));

            return keyFullstring.GetHashCode().ToString();
        }

        public void RemoveUserTokenCache(string userName)
        {
            lock (tokenLock)
            {
                CachedTokens = CachedTokens
                        .Where(o => string.Compare(o.Value.DisplayClaims["enm"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase) != 0)
                        .ToDictionary(o => o.Key, o => o.Value);
            }
        }

        private void LoadTokenCache()
        {
            lock (tokenLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);

                Dictionary<string, XdtsTokenResponse> cache = JsonConvert.DeserializeObject<Dictionary<string, XdtsTokenResponse>>(File.Exists(cacheFilePath) 
                    ? File.ReadAllText(cacheFilePath): string.Empty);

                if (cache != null)
                {
                    // Remove expired token, to prevent cache over grow
                    CachedTokens = cache
                        .Where(o => o.Value.NotAfter > DateTime.Now)
                        .ToDictionary(o => o.Key, o => o.Value);
                }
                else
                {
                    CachedTokens = new Dictionary<string, XdtsTokenResponse>();
                }
            }
        }
    }
}
