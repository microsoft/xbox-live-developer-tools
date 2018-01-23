// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.PlayerReset
{
    using Newtonsoft.Json;
    internal class JobSubmitReqeust
    {
        public JobSubmitReqeust(string scid, string xuid)
        {
            UserId = xuid;
            Scid = scid;
        }

        [JsonProperty("userId", Required = Required.Always)]
        public string UserId { get; set; } = "deletedata";

        [JsonProperty("Scid", Required = Required.Always)]
        public string Scid { get; set; }
    }
}
