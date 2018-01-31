// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System.IO;
    using Microsoft.Identity.Client;
    using Microsoft.Xbox.Services.DevTools.Common;

    internal class MsalTokenCache
    {
        private const string CacheFile = "msal.cache";
        private static readonly object FileLock = new object();

        public MsalTokenCache()
        {
            this.TokenCache.SetBeforeAccess(this.BeforeAccessNotification);
            this.TokenCache.SetAfterAccess(this.AfterAccessNotification);

            Directory.CreateDirectory(ClientSettings.Singleton.CacheFolder);
        }

        public TokenCache TokenCache { get; } = new TokenCache();

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);

                args.TokenCache.Deserialize(File.Exists(cacheFilePath)
                    ? File.ReadAllBytes(cacheFilePath)
                    : null);
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.HasStateChanged)
            {
                lock (FileLock)
                {
                    string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);

                    File.WriteAllBytes(cacheFilePath, args.TokenCache.Serialize());
                    args.TokenCache.HasStateChanged = false;
                }
            }
        }
    }
}
