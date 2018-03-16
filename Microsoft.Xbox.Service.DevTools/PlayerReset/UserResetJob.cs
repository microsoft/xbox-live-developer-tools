// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.PlayerReset
{
    internal class UserResetJob
    {
        public string JobId { get; set; }

        public string CorrelationId { get; set; }

        public string Sandbox { get; set; }

        public string Scid { get; set; }
    }
}
