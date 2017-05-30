//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace Microsoft.Xbox.Services.Tool
{
    using System.Security;
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

        public static async Task<string> GetXDPEToken(string username, SecureString password)
        {
            Log.WriteLog($"GetXDPEToken start, username:{username}");
            lock (initLock)
            {
                if (Client == null)
                {
                    Client = new XdpAuthClient();
                }
            }

            string token = string.Empty;
            try
            {
                token = await Client.GetETokenAsync("", "");
            }
            catch (XboxLiveException)
            {
                token = await Client.SignInAsync(username, password);
            }

            return PrepareForAuthHeader(token);
        }

        public static async Task<string> GetUDCEToken(string username, SecureString password)
        {
            Log.WriteLog($"GetXDPEToken start, username:{username}");
            lock (initLock)
            {
                if (Client == null)
                {
                    Client = new UDCAuthClient(null);
                }
            }

            string token = string.Empty;
            if (Client.HasCredential)
            {
                token = await Client.GetETokenAsync("", "");
            }
            else
            {
                token = await Client.SignInAsync(username, password);
            }

            return PrepareForAuthHeader(token);
        }

    }
}
