// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTool.Authentication
{
    using Microsoft.Identity.Client;
    using System.IO;
    using Microsoft.Xbox.Services.DevTool.Common;

    internal class MsalTokenCache
    {
        public TokenCache TokenCache { get; } = new TokenCache();

        private const string CacheFile = "msal.cache";

        private static readonly object FileLock = new object();

        public MsalTokenCache()
        {
            TokenCache.SetBeforeAccess(BeforeAccessNotification);
            TokenCache.SetAfterAccess(AfterAccessNotification);

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
