// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System;
    using System.IO;

    internal class ClientSettings
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
            this.ActiveDirectoryAuthenticationEndpoint = "https://login.microsoftonline.com/common";
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
                this.XmintAuthEndpoint = "https://xmint.xboxlive.dnet.com/adfs/authorize?rp=https%3A%2F%2Fxdp.dnet.xboxlive.com%2F";
                this.TitleStorageEndpoint = "https://titlestorage.dnet.xboxlive.com";
            }

            this.XdpBaseUri = new Uri(xdpBaseEndpoint);
            this.WindowsLiveUri = new Uri(windowsLiveUriEndpoint);
            this.StsAdfsAuthenticationUri = new Uri(stsAdfsAuthenticationEndpoint);

            // Cache folder
            CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft/XboxLiveTool/"+ environment);
        }

        public string CacheFolder { get; set; } 

        public Uri XdpBaseUri { get; private set; }

        public Uri WindowsLiveUri { get; private set; }

        public Uri StsAdfsAuthenticationUri { get; private set; }

        public string ActiveDirectoryAuthenticationEndpoint { get; private set; }

        public string WindowsLiveAuthenticationType { get; private set; }

        public string OmegaResetToolEndpoint { get; private set; }

        public string TitleStorageEndpoint { get; private set; } = "https://titlestorage.xboxlive.com";

        public string XDTSToolTokenType { get; private set; } = "http://oauth.net/grant_type/jwt/1.0/bearer";

        // TODO: Update this to runtime etoken after it's ready, for now we use design time etoken.
        public string XDTSToolRelyingParty { get; private set; } = "http://developer.xboxlive.com";
        public string AADApplicationId { get; private set; } = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public string AADResource { get; private set; } = "https://developer.microsoft.com/";
        public string UDCAuthEndpoint{ get; private set; } = "https://developer.microsoft.com/xdts/authorize";

        public string MsalXboxLiveClientId = "b1eab458-325b-45a5-9692-ad6079c1eca8";
        public string XmintAuthEndpoint { get; private set; } = "https://xmint.xboxlive.com/adfs/authorize?rp=https%3A%2F%2Fxdp.xboxlive.com%2F";
    }
}
