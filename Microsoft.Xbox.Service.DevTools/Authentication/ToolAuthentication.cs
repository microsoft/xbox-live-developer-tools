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
        private const string TestAccountCacheFile = "lastTestUser";
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
                    ToolAuthentication.SetAuthInfo(result.AccountSource, result.Name, result.Tenant);
                }
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to load last signin user: " + e.Message);
            }

            return result;
        }

        /// <summary>
        /// Load the last signed in test account from local cache and set as sign in info.
        /// </summary>
        /// <returns>The TestAccount object represents the last signed in test account, or null if there isn't one.</returns>
        public static TestAccount LoadLastSignedInTestAccount()
        {
            TestAccount result = null;
            try
            {
                string lastSignInTestUserCacheFile = Path.Combine(ClientSettings.Singleton.CacheFolder, TestAccountCacheFile);

                if (File.Exists(lastSignInTestUserCacheFile))
                {
                    result = JsonConvert.DeserializeObject<TestAccount>(File.ReadAllText(lastSignInTestUserCacheFile));
                }

                if (result != null && !string.IsNullOrEmpty(result.UserName))
                {
                    ToolAuthentication.SetAuthInfo(DevAccountSource.TestAccount, result.UserName, "consumers");
                }
                else
                {
                    result = null;
                }
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to load last signin test account: " + e.Message);
                result = null;
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
        /// Attempt to fetch a test xToken without triggering any UI.
        /// </summary>
        /// <param name="sandbox">The target sandbox for the XToken</param>
        /// <returns>Developer eToken for specific serviceConfigurationId and sandbox</returns>
        public static async Task<string> GetTestTokenSilentlyAsync(string sandbox)
        {
            return await GetTestTokenSilentlyAsync(sandbox, false);
        }

        /// <summary>
        /// Attempt to fetch a test xToken without triggering any UI.
        /// </summary>
        /// <param name="sandbox">The target sandbox for the XToken</param>
        /// <param name="forceRefresh">True to ignore any cached token and mint a new one. Callers
        /// that change privileges need this, because the service rejects tokens issued before the
        /// change even though they have not expired.</param>
        /// <returns>Developer eToken for specific serviceConfigurationId and sandbox</returns>
        public static async Task<string> GetTestTokenSilentlyAsync(string sandbox, bool forceRefresh)
        {
            if (Client.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found, call Auth.SignInTestAccountAsync or Auth.LoadLastSignedInTestAccount first.");
            }

            var xtoken = await Client.GetXTokenAsync(sandbox, forceRefresh);
            return PrepareForAuthHeader(xtoken);
        }

        /// <summary>
        /// Attempt to fetch the current state of the signed in test account without triggering any UI.
        /// </summary>
        /// <param name="sandbox">The target sandbox for the XToken</param>
        /// <param name="forceRefresh">True to ignore any cached token and mint a new one. Callers
        /// that want to observe a privilege or privacy change need this, because the claims are
        /// only as fresh as the token they came from.</param>
        /// <returns>A TestAccount carrying the claims of the token that was fetched.</returns>
        public static async Task<TestAccount> GetTestAccountSilentlyAsync(string sandbox, bool forceRefresh)
        {
            if (Client.AuthContext == null)
            {
                throw new InvalidOperationException("User Info is not found, call Auth.SignInTestAccountAsync or Auth.LoadLastSignedInTestAccount first.");
            }

            XasTokenResponse xtoken = await Client.GetXTokenAsync(sandbox, forceRefresh);
            return new TestAccount(xtoken)
            {
                UserName = Client.AuthContext.UserName,
                Sandbox = sandbox
            };
        }

        /// <summary>
        /// Attempt to sign in developer account, UI will be triggered if necessary
        /// </summary>
        /// <param name="accountSource">The account source where the developer account was registered.</param>
        /// <param name="userName">The user name of the account, optional.</param>
        /// <param name="tenant">The tenant of the account, optional.</param>
        /// <returns>DevAccount object contains developer account info.</returns>
        public static async Task<DevAccount> SignInAsync(DevAccountSource accountSource, string userName, string tenant = "common")
        {
            SetAuthInfo(accountSource, userName, tenant);

            DevAccount devAccount = await Client.SignInAsync(tenant);
            SaveLastSignedInUser(devAccount);

            return devAccount;
        }

        // Test hook
        internal static async Task<DevAccount> SignInAsync(DevAccountSource accountSource, string userName, IAuthContext authContext)
        {
            Client.AuthContext = authContext;

            DevAccount devAccount = await Client.SignInAsync("common");
            SaveLastSignedInUser(devAccount);

            return devAccount;
        }

        /// <summary>
        /// Attempt to sign in a test account. A cached credential is reused when one is available,
        /// so no UI is triggered unless interaction is actually required.
        /// </summary>
        /// <param name="userName">The user name of the account, optional.</param>
        /// <param name="sandbox">The target sandbox for the test account.</param>
        /// <returns>TestAccount object contains test account info.</returns>
        public static async Task<TestAccount> SignInTestAccountAsync(string userName, string sandbox)
        {
            return await SignInTestAccountAsync(userName, sandbox, false);
        }

        /// <summary>
        /// Attempt to sign in a test account, UI will be triggered if necessary.
        /// </summary>
        /// <param name="userName">The user name of the account, optional.</param>
        /// <param name="sandbox">The target sandbox for the test account.</param>
        /// <param name="forceInteractiveSignIn">When true, any cached credential is ignored and the sign in UI is always shown.</param>
        /// <returns>TestAccount object contains test account info.</returns>
        public static async Task<TestAccount> SignInTestAccountAsync(string userName, string sandbox, bool forceInteractiveSignIn)
        {
            SetAuthInfo(DevAccountSource.TestAccount, userName, "consumers");

            TestAccount testAccount = await Client.SignInTestAccountAsync(sandbox, forceInteractiveSignIn);
            SaveLastSignedInTestAccount(testAccount);

            return testAccount;
        }

        // Test hook
        internal static async Task<TestAccount> SignInTestAccountAsync(string sandbox, IAuthContext authContext)
        {
            Client.AuthContext = authContext;

            TestAccount testAccount = await Client.SignInTestAccountAsync(sandbox, false);
            SaveLastSignedInTestAccount(testAccount);

            return testAccount;
        }

        /// <summary>
        /// Sign out the current signed in test account.
        /// </summary>
        public static void SignOutTestAccount()
        {
            lock (initLock)
            {
                string testAccountCacheFilePath = Path.Combine(ClientSettings.Singleton.CacheFolder, TestAccountCacheFile);
                if (File.Exists(testAccountCacheFilePath))
                {
                    File.Delete(testAccountCacheFilePath);
                }

                // The xsts cache only ever holds test account tokens, so clearing it does not
                // affect a signed in Partner Center account.
                Client.XTokenCache.Value.Clear();

                if (Client.AuthContext != null && Client.AuthContext.AccountSource == DevAccountSource.TestAccount)
                {
                    Client.AuthContext = null;
                }
            }
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

        internal static void SetAuthInfo(DevAccountSource accountSource, string userName, string tenant)
        {
            lock (initLock)
            {
                Client.AuthContext = CreateAuthContext(accountSource, userName, tenant);
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

        private static void SaveLastSignedInTestAccount(TestAccount account)
        {
            try
            {
                string lastSignInTestUserCacheFile = Path.Combine(ClientSettings.Singleton.CacheFolder, TestAccountCacheFile);
                File.WriteAllText(lastSignInTestUserCacheFile, JsonConvert.SerializeObject(account));
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to save last signin test account: " + e.Message);
            }
        }

        private static IAuthContext CreateAuthContext(DevAccountSource accountSource, string userName, string tenant)
        {
            switch (accountSource)
            {
                case DevAccountSource.WindowsDevCenter:
                    return new MsalDevAuthContext(userName); 
                case DevAccountSource.XboxDeveloperPortal:
                    throw new ArgumentException("XDP is no longer a supported developer type. Sign in with a Windows Developer Center account.");
                case DevAccountSource.TestAccount:
                    return new MsalTestAuthContext(userName); 
                default:
                    throw new ArgumentException("Unsupported developer type");
            }
        }

        internal static string PrepareForAuthHeader(string etoken)
        {
            return "XBL3.0 x=-;" + etoken;
        }

        internal static string PrepareForAuthHeader(XasTokenResponse token)
        {
            TestAccount ta = new TestAccount(token);
            return $"XBL3.0 x={ta.UserHash};{token.Token}";
        }
    }
}
