// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System.IO;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Xbox.Services.DevTools.Common;

    internal class AdalTokenCache : TokenCache
    {
        private const string CacheFile = "adal.cache";

        private static readonly object FileLock = new object();

        public AdalTokenCache()
        {
            this.AfterAccess = this.AfterAccessNotification;
            this.BeforeAccess = this.BeforeAccessNotification;

            Directory.CreateDirectory(ClientSettings.Singleton.CacheFolder);
        }

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
