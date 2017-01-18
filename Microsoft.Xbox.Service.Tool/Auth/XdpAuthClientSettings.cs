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

    public class XdpAuthClientSettings
    {
        public XdpAuthClientSettings(string environmentType)
        {
            if (environmentType == null)
            {
                throw new ArgumentNullException("environmentType");
            }

            environmentType = environmentType.ToUpperInvariant();

            // Default values are for production
            string xdpBaseEndpoint = "https://xdp.xboxlive.com";
            string windowsLiveUriEndpoint = "https://login.live.com";
            string stsAdfsAuthenticationEndpoint = "https://edadfs.partners.extranet.microsoft.com/adfs/ls/";
            this.ActiveDirectoryAuthenticationEndpoint = "http://corp.sts.microsoft.com";
            this.WindowsLiveAuthenticationType = "uri:WindowsLiveID";
            
            // Override values for other environments
            switch (environmentType)
            {
                case "DEV":
                case "DEVELOPMENT":
                    xdpBaseEndpoint = "https://xdp.dnet.xboxlive.com";
                    windowsLiveUriEndpoint = "https://login.live-int.com";
                    stsAdfsAuthenticationEndpoint = "https://edstssit.partners.extranet.microsoft.com/adfs/ls/";
                    this.WindowsLiveAuthenticationType = "uri:WindowsLiveIDINT";
                    break;

                case "PRODUCTION":
                    break;
            }

            this.XdpBaseUri = new Uri(xdpBaseEndpoint);
            this.WindowsLiveUri = new Uri(windowsLiveUriEndpoint);
            this.StsAdfsAuthenticationUri = new Uri(stsAdfsAuthenticationEndpoint);
            this.ActiveDirectoryAuthenticationBaseUri = new Uri(this.ActiveDirectoryAuthenticationEndpoint);
        }

        public Uri XdpBaseUri { get; set; }

        public Uri ActiveDirectoryAuthenticationBaseUri { get; set; }

        public Uri WindowsLiveUri { get; set; }

        public Uri StsAdfsAuthenticationUri { get; set; }

        public string ActiveDirectoryAuthenticationEndpoint { get; set; }

        public string WindowsLiveAuthenticationType { get; set; }
    }
}
