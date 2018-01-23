// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xbox.Services.DevTools.Common;

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System.Collections.Generic;
    using DevTools.Common;

    internal class XdtsTokenRequest
    {
        public XdtsTokenRequest(string scid, IEnumerable<string> sandboxes)
        {
            if (!string.IsNullOrEmpty(scid))
            {
                Properties["Scid"] = scid;
            }

            if (sandboxes!=null)
            {
                Properties["Sandboxes"] = string.Join(" ", sandboxes);
            }
        }

        public string TokenType { get; set; } = ClientSettings.Singleton.XDTSToolTokenType;

        public string RelyingParty { get; set; } = ClientSettings.Singleton.XDTSToolRelyingParty;

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    }
}
