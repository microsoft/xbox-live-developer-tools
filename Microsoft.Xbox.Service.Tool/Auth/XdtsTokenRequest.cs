// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
