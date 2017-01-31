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
        private static AuthClient client = null;
        private static object initLock = new object();

        static public bool HasAuthInfo()
        {
            lock (initLock)
            {
                return (client != null && client.HasCredential());
            }
        }

        static private string PrepareForAuthHeader(string etoken)
        {
            return "XBL3.0 x=-;" + etoken;
        }

        static public async Task<string> GetETokenSilentlyAsync(string scid, string sandbox)
        {
            lock (initLock)
            {
                if (client == null)
                {
                    // GetXDPETokenSilentlyAsync can't be called before a succeful sign in 
                    throw new XboxLiveException("Invalid status: GetXDPETokenSilentlyAsync");
                }
            }

            string etoken = await client.GetETokenAsync(scid, sandbox);
            return PrepareForAuthHeader(etoken);
        }

        static public async Task<string> GetXDPEToken(string username, SecureString password)
        {
            Log.WriteLog($"GetXDPEToken start, username:{username}");
            lock (initLock)
            {
                if (client == null)
                {
                    client = new XdpAuthClient();
                }
            }

            string token = string.Empty;
            try
            {
                token = await client.GetETokenAsync("", "");
            }
            catch (XboxLiveException)
            {
                token = await client.SignInAsync(username, password);
            }

            return PrepareForAuthHeader(token);
        }

        static public async Task<string> GetUDCEToken(string username, SecureString password)
        {
            Log.WriteLog($"GetXDPEToken start, username:{username}");
            lock (initLock)
            {
                if (client == null)
                {
                    client = new UDCAuthClient();
                }
            }

            string token = string.Empty;
            if (client.HasCredential())
            {
                token = await client.GetETokenAsync("", "");
            }
            else
            {
                token = await client.SignInAsync(username, password);
            }

            return PrepareForAuthHeader(token);
        }
    }
}
