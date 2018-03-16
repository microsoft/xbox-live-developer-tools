// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig.Contracts
{
    using System.Collections.Generic;

    internal class CertRequest
    {
        public Dictionary<string, string> Properties { get; set; }

        public string CertType { get; set; }
    }
}
