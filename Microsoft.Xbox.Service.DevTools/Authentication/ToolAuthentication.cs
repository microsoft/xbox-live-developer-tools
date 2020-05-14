// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTools.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Class for XboxLive developer account authentication.
    /// </summary>
    public class ToolAuthentication
    {
        private const string CacheFile = "lastUser";
        private static object initLock = new object();

        private ToolAuthentication()
        {
        }

        internal static AuthClient Client { get; set; } = new AuthClient();

        /// <summary>
        /// Load the last signed in user from local cache and set as sign in info.
        /// </summary>
        /// <returns>The DevAccount object represents the last signed in dev account</returns>
        public static DevAccount LoadLastSignedInUser()
        {
            DevAccount result = null;
            try
            {
                string lastSignInUserCacheFile = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);

                if (File.Exists(lastSignInUserCacheFile))
                {
                    result = JsonConvert.DeserializeObject<DevAccount>(File.ReadAllText(lastSignInUserCacheFile));
                }

                if (result!= null)
                {
                    ToolAuthentication.SetAuthInfo(result.AccountSource, result.Name);
                }
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to load last signin user: " + e.Message);
            }

            return result;
        }

        /// <summary>
        /// Attempt to fetch a developer eToken without triggering any UI.
        /// </summary>
        /// <param name="serviceConfigurationId">The target service configuration ID (SCID) for the eToken, when empty, the token won't have access to a particular service configure</param>
        /// <param name="sandbox">The target sandbox for the eToken, when empty, the token won't have any access to a particular sandbox</param>
        /// <returns>Developer eToken for specific serviceConfigurationId and sandbox</returns>
        public static async Task<string> GetDevTokenSilentlyAsync(string serviceConfigurationId, string sandbox)
        {
            return await GetDevTokenSilentlyAsync(serviceConfigurationId, string.IsNullOrEmpty(sandbox) ? null : new string[] { sandbox });
        }

        /// <summary>
        /// Attempt to fetch a developer eToken without triggering any UI.
        /// </summary>
        /// <param name="serviceConfigurationId">The target service configuration ID (SCID) for the eToken,  when empty, the token won't have access to a particular service configure</param>
        /// <param name="sandboxes">The target sandbox list for the eToken, when empty, the token won't have any access to a particular sandbox</param>
        /// <returns>Developer eToken for specific serviceConfigurationId and sandbox</returns>
        public static async Task<string> GetDevTokenSilentlyAsync(string serviceConfigurationId, IEnumerable<string> sandboxes)
        {
            if (Client.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found, call Auth.SignInAsync or Auth.LoadLastSignedInUser first.");
            }

            string etoken = await Client.GetETokenAsync(serviceConfigurationId, sandboxes, false);
            return PrepareForAuthHeader(etoken);
        }

        /// <summary>
        /// Attempt to sign in developer account, UI will be triggered if necessary 
        /// </summary>
        /// <param name="accountSource">The account source where the developer account was registered.</param>
        /// <param name="userName">The user name of the account, optional.</param>
        /// <returns>DevAccount object contains developer account info.</returns>
        public static async Task<DevAccount> SignInAsync(DevAccountSource accountSource, string userName)
        {
            SetAuthInfo(accountSource, userName);

            DevAccount devAccount = await Client.SignInAsync();
            SaveLastSignedInUser(devAccount);

            return devAccount;
        }

        // Test hook
        internal static async Task<DevAccount> SignInAsync(DevAccountSource accountSource, string userName, IAuthContext authContext)
        {
            Client.AuthContext = authContext;

            DevAccount devAccount = await Client.SignInAsync();
            SaveLastSignedInUser(devAccount);

            return devAccount;
        }

        /// <summary>
        /// Sign out the current signed in developer account.
        /// </summary>
        public static void SignOut()
        {
            lock (initLock)
            { 
                if (Client.AuthContext == null)
                {
                    throw new InvalidOperationException("User Info is not found, call Auth.SignInAsync or Auth.LoadLastSignedInUser first.");
                }

                File.Delete(Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile));
                Client.ETokenCache.Value.RemoveUserTokenCache(Client.AuthContext.UserName);
                Client.AuthContext = null;
            }
        }

        internal static void SetAuthInfo(DevAccountSource accountSource, string userName)
        {
            lock (initLock)
            {
                Client.AuthContext = CreateAuthContext(accountSource, userName);
            }
        }

        private static void SaveLastSignedInUser(DevAccount account)
        {
            try
            {
                string lastSignInUserCacheFile = Path.Combine(ClientSettings.Singleton.CacheFolder, CacheFile);
                File.WriteAllText(lastSignInUserCacheFile, JsonConvert.SerializeObject(account));
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to save last signin user: " + e.Message);
            }
        }

        private static IAuthContext CreateAuthContext(DevAccountSource accountSource, string userName)
        {
            switch (accountSource)
            {
                case DevAccountSource.WindowsDevCenter:
                    return new AdalAuthContext(userName);
                case DevAccountSource.XboxDeveloperPortal:
                    throw new ArgumentException("XDP is no longer a supported developer type. Sign in with a Windows Developer Center account.");
                default:
                    throw new ArgumentException("Unsupported developer type");
            }
        }

        internal static string PrepareForAuthHeader(string etoken)
        {
            return "XBL3.0 x=-;" + etoken;
        }
    }
}
