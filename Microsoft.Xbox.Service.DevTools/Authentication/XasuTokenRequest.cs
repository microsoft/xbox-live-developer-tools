// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    internal class XasuTokenRequest : XasTokenRequest
    {
        public XasuTokenRequest()
        {
            this.TokenType = "JWT";
            this.RelyingParty = "http://auth.xboxlive.com";
            this.Properties["AuthMethod"] = "RPS";
        }
    }
}
