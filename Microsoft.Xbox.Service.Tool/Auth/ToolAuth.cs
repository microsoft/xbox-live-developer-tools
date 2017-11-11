// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using System.Threading.Tasks;

    /// <summary>
    /// Class for XboxLive developer account authentication.
    /// </summary>
    public class Auth
    {
        private static object initLock = new object();

        internal static AuthClient Client {get;set;}

        /// <summary>
        /// Get current signed in developer account. Return null if no one signed in.
        /// </summary>
        public DevAccount DevAccount
        {
            get
            {
                lock (initLock)
                {
                    return Client.Account;
                }
            }
        }

        /// <summary>
        /// Return true if an account has already signed in.
        /// </summary>
        public static bool IsSignedIn
        {
            get
            {
                lock (initLock)
                {
                    return (Client != null && Client.HasCredential);
                }
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
            lock (initLock)
            {
                if (Client == null)
                {
                    throw new XboxLiveException("Invalid status: GetETokenSilentlyAsync can't be called before a successful sign in");
                }
            }

            string etoken = await Client.GetETokenAsync(serviceConfigurationId, sandbox);
            return PrepareForAuthHeader(etoken);
        }

        /// <summary>
        /// Attempt to sign in developer account, UI will be triggered if necessary 
        /// </summary>
        /// <param name="accountSource">The account source where the developer account was registered.</param>
        /// <param name="userName">The user name of the account, optional.</param>
        /// <returns>DevAccount object contains developer account info.</returns>
        public static async Task<DevAccount> SignIn(DevAccountSource accountSource, string userName)
        {
            lock (initLock)
            {
                if (Client == null)
                {
                    switch (accountSource)
                    {
                        case DevAccountSource.UniversalDeveloperCenter:
                            Client = new AuthClient(new AadAuthContext());
                            break;
                        case DevAccountSource.XboxDeveloperPortal:
                            Client = new AuthClient(new MsalAuthContext());
                            break;
                        default:
                            throw new XboxLiveException("Unsupported developer type");
                    }
                }
                else if (Client.HasCredential)
                {
                    throw new XboxLiveException("Dev account already signed in");
                }
            }

            return await Client.SignInAsync(userName);
        }

        /// <summary>
        /// Sign out the current signed in developer account.
        /// </summary>
        public static void SignOut()
        {
            lock (initLock)
            {
                Client = null;
            }
        }

        internal Auth()
        {
        }

        internal static string PrepareForAuthHeader(string etoken)
        {
            return "XBL3.0 x=-;" + etoken;
        }
    }
}
