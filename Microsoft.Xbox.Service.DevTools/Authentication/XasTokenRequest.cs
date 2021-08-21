// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System.Collections.Generic;
    using DevTools.Common;

    internal class XasTokenRequest
    {
        public string TokenType { get; set; } = ClientSettings.Singleton.XDTSToolTokenType;

        public string RelyingParty { get; set; } = ClientSettings.Singleton.XDTSToolRelyingParty;

        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
