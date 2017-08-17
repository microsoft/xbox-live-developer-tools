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
    using System.Collections.Generic;
    internal class XdtsTokenRequest
    {
        public XdtsTokenRequest(string scid = "", string sandbox = "")
        {
            if (!string.IsNullOrEmpty(scid))
            {
                Properties["Scid"] = scid;
            }

            if (!string.IsNullOrEmpty(sandbox))
            {
                Properties["Sandboxes"] = sandbox;
            }
        }

        public string TokenType { get; set; } = ClientSettings.Singleton.XDTSToolTokenType;

        public string RelyingParty { get; set; } = ClientSettings.Singleton.XDTSToolRelyingParty;

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    }
}
