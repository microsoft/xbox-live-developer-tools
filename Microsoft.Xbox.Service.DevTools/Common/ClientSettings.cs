// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System;
    using System.IO;

    internal class ClientSettings
    {
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
            this.ActiveDirectoryAuthenticationEndpoint = "https://login.microsoftonline.com/common";
            this.OmegaResetToolEndpoint = "https://eraser.xboxlive.com";

            // Override values for other environments
            if (environment.ToUpper() == "DNET")
            {
                this.OmegaResetToolEndpoint = "https://eraser.dnet.xboxlive.com";
                this.UDCAuthEndpoint = "https://devx.microsoft-tst.com/xdts/authorize";
                this.TitleStorageEndpoint = "https://titlestorage.dnet.xboxlive.com";
                this.XConEndpoint = "https://config2.mgt.dnet.xboxlive.com/";
                this.XOrcEndpoint = "https://xorc.dnet.xboxlive.com/";
                this.XCertEndpoint = "https://cert.mgt.dnet.xboxlive.com/";
                this.XAchEndpoint = "https://xach.mgt.dnet.xboxlive.com/";
                this.XFusEndpoint = "https://upload.dnet.xboxlive.com/";
            }
            
            // Cache folder
            this.CacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft/XboxLiveTool/" + environment);
        }

        public static ClientSettings Singleton
        {
            get
            {
                lock (singletonLock)
                {
                    if (singleton == null)
                    {
                        singleton = new ClientSettings(string.Empty);
                    }
                }

                return singleton;
            }
        }

        public string CacheFolder { get; set; } 
        
        public string ActiveDirectoryAuthenticationEndpoint { get; private set; }
        
        public string OmegaResetToolEndpoint { get; private set; }

        public string TitleStorageEndpoint { get; private set; } = "https://titlestorage.xboxlive.com";

        public string XDTSToolTokenType { get; private set; } = "http://oauth.net/grant_type/jwt/1.0/bearer";

        // TODO: Update this to runtime etoken after it's ready, for now we use design time etoken.
        public string XDTSToolRelyingParty { get; private set; } = "http://developer.xboxlive.com";

        public string AADApplicationId { get; private set; } = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";

        public string AADResource { get; private set; } = "https://partner.microsoft.com/";

        public string UDCAuthEndpoint { get; private set; } = "https://partner.microsoft.com/xdts/authorize";

        public string XConEndpoint { get; private set; } = "https://config2.mgt.xboxlive.com/";

        public string XOrcEndpoint { get; private set; } = "https://xorc.xboxlive.com/";

        public string XCertEndpoint { get; private set; } = "https://cert.mgt.xboxlive.com/";

        public string XAchEndpoint { get; private set; } = "https://xach.mgt.xboxlive.com/";

        public string XFusEndpoint { get; private set; } = "https://upload.xboxlive.com/";
    }
}
