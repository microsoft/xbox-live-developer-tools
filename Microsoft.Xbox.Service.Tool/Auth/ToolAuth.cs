// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xbox.Services.Tool
{
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for XboxLive developer account authentication.
    /// </summary>
    public class Auth
    {
        private static object initLock = new object();

        internal static AuthClient Client {get;set;} = new AuthClient();

        /// <summary>
        /// Load the last signed in user from local cache.
        /// </summary>
        /// <returns>The DevAccount object repesnets the last signed in dev account</returns>
        public static DevAccount LoadLastSignedInUser()
        {
            
            DevAccount result = null;
            try
            {
                string lastSignInUserCacheFile = Path.Combine(ClientSettings.Singleton.CacheFolder, "lastUser");

                result = JsonConvert.DeserializeObject<DevAccount>(File.Exists(lastSignInUserCacheFile)
                    ? File.ReadAllText(lastSignInUserCacheFile) : string.Empty);

                Auth.SetAuthInfo(result.Name, result.AccountSource);
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to load last signin user: " + e.Message);
            }
            return result;
        }

        /// <summary>
        /// Set user info for futuer authtication. 
        /// </summary>
        /// <param name="accountSource">The account source where the developer account was registered.</param>
        /// <param name="userName">The user name of the account, optional.</param>
        public static void SetAuthInfo(string userName, DevAccountSource accountSource)
        {
            lock (initLock)
            {
                Client.AuthContext = CretaeAuthContext(userName, accountSource);
            }
        }

        /// <summary>
        /// Attempt to fetch a developer eToken without triggering any UI.
        /// </summary>
        /// <param name="serviceConfigurationId">The target service configuration ID (SCID) for the eToken, could be empty</param>
        /// <param name="sandbox">The target sandbox id for the eToken, could be empty</param>
        /// <returns>Developer eToken for specific serviceConfigurationId and sandbox</returns>
        public static async Task<string> GetETokenSilentlyAsync(string serviceConfigurationId, string sandbox)
        {
            if (Client.AuthContext == null)
            {
                throw new XboxLiveException(XboxLiveErrorStatus.AuthenticationFailure, "User Info is not found, call Auth.SetAuthInfo or LoadLastSignedInUser first.");
            }

            try
            {
                string etoken = await Client.GetETokenAsync(serviceConfigurationId, sandbox, false);
                return PrepareForAuthHeader(etoken);
            }
            catch (AdalException exception)
            {
                throw new XboxLiveException(exception.Message, XboxLiveErrorStatus.AuthenticationFailure, exception);
            }
            catch (MsalException exception)
            {
                throw new XboxLiveException(exception.Message, XboxLiveErrorStatus.AuthenticationFailure, exception);
            }
        }

        /// <summary>
        /// Attempt to sign in developer account, UI will be triggered if necessary 
        /// </summary>
        /// <returns>DevAccount object contains developer account info.</returns>
        public static async Task<DevAccount> SignIn()
        {
            if (Client.AuthContext == null)
            {
                throw new XboxLiveException(XboxLiveErrorStatus.AuthenticationFailure, "User Info is not found, call Auth.SetAuthInfo first.");
            }

            try
            {
                DevAccount devAccount = await Client.SignInAsync();
                SaveLastSignedInUser(devAccount);

                return devAccount;
            }
            catch (AdalException exception)
            {
                throw new XboxLiveException(exception.Message, XboxLiveErrorStatus.AuthenticationFailure, exception);
            }
            catch (MsalException exception)
            {
                throw new XboxLiveException(exception.Message, XboxLiveErrorStatus.AuthenticationFailure, exception);
            }
        }

        /// <summary>
        /// Sign out the current signed in developer account.
        /// </summary>
        public static void SignOut()
        {
            lock (initLock)
            {
                Client.ETokenCache.Value.RemoveUserTokenCache(Client.AuthContext.UserName);
                Client.AuthContext = null;
            }
        }

        private static void SaveLastSignedInUser(DevAccount account)
        {
            try
            {
                string lastSignInUserCacheFile = Path.Combine(ClientSettings.Singleton.CacheFolder, "lastUser");
                File.WriteAllText(lastSignInUserCacheFile, JsonConvert.SerializeObject(account));
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to save last signin user: " + e.Message);
            }
        }

        private static IAuthContext CretaeAuthContext(string userName, DevAccountSource accountSource)
        {
            switch (accountSource)
            {
                case DevAccountSource.WindowsDevCenter:
                    return new AdalAuthContext(userName);
                case DevAccountSource.XboxDeveloperPortal:
                    return new MsalAuthContext(userName);
                default:
                    throw new XboxLiveException("Unsupported developer type");
            }
        }

        internal static string PrepareForAuthHeader(string etoken)
        {
            return "XBL3.0 x=-;" + etoken;
        }

    }
}
