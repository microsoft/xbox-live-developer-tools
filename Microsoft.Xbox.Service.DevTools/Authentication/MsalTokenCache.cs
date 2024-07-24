// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System.IO;
    using System.Security.Cryptography;
    using Microsoft.Identity.Client;
    using Microsoft.Xbox.Services.DevTools.Common;

    internal class MsalTokenCache
    {
        private const string CacheFile = "msal.cache";

        private static readonly object FileLock = new object();

        public MsalTokenCache()
        {
            Directory.CreateDirectory(ClientSettings.Singleton.CacheFolder);
        }

        public void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(this.BeforeAccessNotification);
            tokenCache.SetAfterAccess(this.AfterAccessNotification);
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);

                args.TokenCache.DeserializeMsalV3(File.Exists(cacheFilePath)
                    ? ProtectedData.Unprotect(File.ReadAllBytes(cacheFilePath), null, DataProtectionScope.CurrentUser) : null);
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                string cacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);

                File.WriteAllBytes(cacheFilePath, ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser));
            }
        }
    }
}