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

    public class ToolAuth
    {
       
        static public async Task<string> GetXDPEToken(string username, SecureString password, string environment = "", string sandbox = null)
        {
            XdpAuthClient client = new XdpAuthClient(new XdpAuthClientSettings(environment));
            XdpETokenResponse response = await client.GetEToken(username, password, sandbox);
            if (response.Data != null && response.Data.Token != null)
            {
                return response.Data.Token;
            }
            return "";
        }
    }
}
