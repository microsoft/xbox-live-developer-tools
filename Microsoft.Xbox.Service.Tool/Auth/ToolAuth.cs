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
        private static XdpAuthClient client = null;
        private static object initLock = new object();

        static public bool HasAuthInfo()
        {
            lock (initLock)
            {
                return (client != null && client.HasAuthCookie());
            }
        }

        static private string PrepareForAuthHeader(string etoken)
        {
            return "XBL3.0 x=-;" + etoken;
        }

        static public async Task<string> GetXDPETokenSilentlyAsync(string sandbox = "")
        {
            lock (initLock)
            {
                if (client == null)
                {
                    // GetXDPETokenSilentlyAsync can't be called before a succeful sign in 
                    throw new XboxLiveException("Invalid status: GetXDPETokenSilentlyAsync");
                }
            }

            string etoken = await client.GetETokenSilentlyAsync(sandbox);
            return PrepareForAuthHeader(etoken);
        }

        static public async Task<string> GetXDPEToken(string username, SecureString password, string environment = "", string sandbox = "")
        {
            Log.WriteLog($"GetXDPEToken start, username:{username}, sandbox:{sandbox}, environment:{environment}");
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
                token = await client.GetETokenSilentlyAsync(sandbox);
            }
            catch (XboxLiveException)
            {
                token = await client.GetETokenAsync(username, password, sandbox);
            }

            return PrepareForAuthHeader(token);
        }
    }
}
