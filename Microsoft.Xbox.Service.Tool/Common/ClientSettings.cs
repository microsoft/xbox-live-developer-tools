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
                        singleton = new ClientSettings("");
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
                environment = "PROD";
            }

            Log.WriteLog($"client setting environment: {environment}");

            // Default values are for production
            string xdpBaseEndpoint = "https://xdp.xboxlive.com";
            string windowsLiveUriEndpoint = "https://login.live.com";
            string stsAdfsAuthenticationEndpoint = "https://edadfs.partners.extranet.microsoft.com/adfs/ls/";
            this.ActiveDirectoryAuthenticationEndpoint = "https://login.microsoftonline.com/";
            this.WindowsLiveAuthenticationType = "uri:WindowsLiveID";
            this.OmegaResetToolEndpoint = "https://eraser.xboxlive.com";
            
            // Override values for other environments
            if (environment.ToUpper() == "DNET")
            {
                xdpBaseEndpoint = "https://xdp.dnet.xboxlive.com";
                windowsLiveUriEndpoint = "https://login.live-int.com";
                stsAdfsAuthenticationEndpoint = "https://edstssit.partners.extranet.microsoft.com/adfs/ls/";
                this.WindowsLiveAuthenticationType = "uri:WindowsLiveIDINT";
                this.OmegaResetToolEndpoint = "https://eraser.dnet.xboxlive.com";
                this.UDCAuthEndpoint = "https://devx.microsoft-tst.com/xdts/authorize";
            }

            this.XdpBaseUri = new Uri(xdpBaseEndpoint);
            this.WindowsLiveUri = new Uri(windowsLiveUriEndpoint);
            this.StsAdfsAuthenticationUri = new Uri(stsAdfsAuthenticationEndpoint);
        }

        public Uri XdpBaseUri { get; private set; }

        public Uri WindowsLiveUri { get; private set; }

        public Uri StsAdfsAuthenticationUri { get; private set; }

        public string ActiveDirectoryAuthenticationEndpoint { get; private set; }

        public string WindowsLiveAuthenticationType { get; private set; }

        public string OmegaResetToolEndpoint { get; private set; }

        public string XDTSToolTokenType { get; private set; } = "http://oauth.net/grant_type/jwt/1.0/bearer";

        // TODO: Update this to runtime etoken after it's ready, for now we use design time etoken.
        public string XDTSToolRelyingParty { get; private set; } = "http://developer.xboxlive.com";
        public string AADApplicationId { get; private set; } = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public string AADResource { get; private set; } = "https://developer.microsoft.com/";
        public string UDCAuthEndpoint{ get; private set; } = "https://developer.microsoft.com/xdts/authorize";
    }
}
