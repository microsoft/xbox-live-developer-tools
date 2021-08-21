// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System.Collections.Generic;
    using DevTools.Common;

    internal class XdtsTokenRequest : XasTokenRequest
    {
        public XdtsTokenRequest(string scid, IEnumerable<string> sandboxes)
        {
            if (!string.IsNullOrEmpty(scid))
            {
                this.Properties["Scid"] = scid;
            }

            if (sandboxes!=null)
            {
                this.Properties["Sandboxes"] = string.Join(" ", sandboxes);
            }
        }
    }
}
