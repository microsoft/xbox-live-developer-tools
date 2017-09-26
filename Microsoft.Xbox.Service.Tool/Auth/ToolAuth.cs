// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using System.Threading.Tasks;

    public class Auth
    {
        private static object initLock = new object();

        internal static AuthClient Client {get;set;}

        public static bool HasAuthInfo
        {
            get
            {
                lock (initLock)
                {
                    return (Client != null && Client.HasCredential);
                }
            }
        }

        internal static string PrepareForAuthHeader(string etoken)
        {
            return "XBL3.0 x=-;" + etoken;
        }

        public static async Task<string> GetETokenSilentlyAsync(string scid, string sandbox)
        {
            lock (initLock)
            {
                if (Client == null)
                {
                    // GetXDPETokenSilentlyAsync can't be called before a succeful sign in 
                    throw new XboxLiveException("Invalid status: GetETokenSilentlyAsync");
                }
            }

            string etoken = await Client.GetETokenAsync(scid, sandbox);
            return PrepareForAuthHeader(etoken);
        }

        public static async Task<DevAccount> SignIn(DevAccountSource accountType, string userName)
        {
            lock (initLock)
            {
                if (Client == null)
                {
                    switch (accountType)
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

        public static void SignOut()
        {
            lock (initLock)
            {
                Client = null;
            }
        }
    }
}
