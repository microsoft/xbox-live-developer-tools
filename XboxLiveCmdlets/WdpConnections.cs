// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using Microsoft.Tools.WindowsDevicePortal;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading.Tasks;

    class WdpConnections
    {
        static private ConcurrentDictionary<string, DevicePortal> connections = new ConcurrentDictionary<string, DevicePortal>();

        static private DevicePortal GetConnection(string url, string userName, string password)
        {
            DevicePortal connection = connections.GetOrAdd(url, (key) =>
            {
                var newConnection = new DevicePortal(new DefaultDevicePortalConnection(key, userName, password));
                newConnection.UnvalidatedCert += Connection_UnvalidatedCert;
                newConnection.ConnectAsync().Wait();

                if (newConnection.ConnectionHttpStatusCode != HttpStatusCode.OK)
                {
                    throw new DevicePortalException(newConnection.ConnectionHttpStatusCode, "Failed to connect to windows develop portal: " + newConnection.ConnectionFailedDescription);
                }

                return newConnection;
            });

            return connection;
        }

        private static bool Connection_UnvalidatedCert(DevicePortal sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static async public Task<DevicePortal.Sandbox> GetXboxLiveSandboxAsync(string url, string userName = null, string password = null)
        {
            DevicePortal connection = GetConnection(url, userName, password);
            return await connection.GetXboxLiveSandboxAsync();
        }

        static async public Task SetXboxLiveSandboxAsync(string url, string sandbox, string userName = null, string password = null)
        {
            DevicePortal connection = GetConnection(url, userName, password);
            await connection.SetXboxLiveSandboxAsync(sandbox);
            await connection.RebootAsync();
        }
    }
}
