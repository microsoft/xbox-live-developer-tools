// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    internal class XstsTokenRequest : XasTokenRequest
    {
        public XstsTokenRequest(string sandbox)
        {
            this.TokenType = "JWT";

            if (!string.IsNullOrEmpty(sandbox))
            {
                this.Properties["SandboxId"] = sandbox;
            }
        }
    }
}
