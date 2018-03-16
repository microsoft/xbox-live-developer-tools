// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig.Contracts
{
    using System;

    internal class Sandbox
    {
        public Guid AccountId { get; set; }

        public Guid DevicePg { get; set; }

        public string SandboxId { get; set; }

        public Guid UserPg { get; set; }
    }
}
