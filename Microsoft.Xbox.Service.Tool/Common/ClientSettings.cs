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
    using System;

    public class ClientSettings
    {

        public static ClientSettings Singleton
        {
            get
            {
                lock(singletonLock)
                {
                    if (singleton == null)
                    {
                        singleton = new ClientSettings("DNET");
                    }
                }

                return singleton;
            }
        }

        private static object singletonLock = new object();
        private static ClientSettings singleton;


        private ClientSettings(string environment)
        {
            if (string.IsNullOrEmpty(environment))
            {
                environment = "PRODUCTION";
            }

            Log.WriteLog($"client setting environment: {environment}");

            // Default values are for production
            string xdpBaseEndpoint = "https://xdp.xboxlive.com";
            string windowsLiveUriEndpoint = "https://login.live.com";
            string stsAdfsAuthenticationEndpoint = "https://edadfs.partners.extranet.microsoft.com/adfs/ls/";
            this.ActiveDirectoryAuthenticationEndpoint = "http://corp.sts.microsoft.com";
            this.WindowsLiveAuthenticationType = "uri:WindowsLiveID";
            this.OmegaResetToolEndpoint = "https://jobs.xboxlive.com";
            
            // Override values for other environments
            if (environment.ToUpper() == "DNET")
            {
                xdpBaseEndpoint = "https://xdp.dnet.xboxlive.com";
                windowsLiveUriEndpoint = "https://login.live-int.com";
                stsAdfsAuthenticationEndpoint = "https://edstssit.partners.extranet.microsoft.com/adfs/ls/";
                this.WindowsLiveAuthenticationType = "uri:WindowsLiveIDINT";
                this.OmegaResetToolEndpoint = "https://jobs.dnet.xboxlive.com";
            }

            this.XdpBaseUri = new Uri(xdpBaseEndpoint);
            this.WindowsLiveUri = new Uri(windowsLiveUriEndpoint);
            this.StsAdfsAuthenticationUri = new Uri(stsAdfsAuthenticationEndpoint);
            this.ActiveDirectoryAuthenticationBaseUri = new Uri(this.ActiveDirectoryAuthenticationEndpoint);
        }

        public Uri XdpBaseUri { get; private set; }

        public Uri ActiveDirectoryAuthenticationBaseUri { get; private set; }

        public Uri WindowsLiveUri { get; private set; }

        public Uri StsAdfsAuthenticationUri { get; private set; }

        public string ActiveDirectoryAuthenticationEndpoint { get; private set; }

        public string WindowsLiveAuthenticationType { get; private set; }

        public string OmegaResetToolEndpoint { get; private set; }
    }
}
