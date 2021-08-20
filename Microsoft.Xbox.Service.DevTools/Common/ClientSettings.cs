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

        private ClientSettings()
        {
            // Default values are for production
            this.ActiveDirectoryAuthenticationEndpoint = "https://login.microsoftonline.com/{0}"; // default should be common
            this.OmegaResetToolEndpoint = "https://eraser.xboxlive.com";
                        
            // Cache folder
            this.CacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft/XboxLiveTool/PROD");
        }

        public static ClientSettings Singleton
        {
            get
            {
                lock (singletonLock)
                {
                    if (singleton == null)
                    {
                        singleton = new ClientSettings();
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

        public string XASUEndpoint { get; private set; } = "https://user.auth.xboxlive.com/user/authenticate";

        public string XSTSEndpoint { get; private set; } = "https://xsts.auth.xboxlive.com/xsts/authorize";

        public string MsalXboxLiveClientId { get; private set; } = "b1eab458-325b-45a5-9692-ad6079c1eca8";
    }
}
