// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig.Contracts
{
    using System;

    internal class XachInitializeResponse
    {
        public Guid XfusId { get; set; }

        public string XfusToken { get; set; }

        public string XfusUploadWindowLocation { get; set; }
    }
}
