using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using Microsoft.XboxTest.Xdts;
using System.Security;

namespace Microsoft.Xbox
{
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
