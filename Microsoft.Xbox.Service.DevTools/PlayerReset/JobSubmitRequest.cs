// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.PlayerReset
{
    using Newtonsoft.Json;

    internal class JobSubmitRequest
    {
        public JobSubmitRequest(string scid, string xuid, string sandbox)
        {
            this.UserId = xuid;
            this.Scid = scid;
            this.Sandbox = sandbox;
        }

        [JsonProperty("userId", Required = Required.Always)]
        public string UserId { get; set; } = "deletedata";

        [JsonProperty("Scid", Required = Required.Always)]
        public string Scid { get; set; }

        [JsonProperty("sandbox", NullValueHandling = NullValueHandling.Ignore)]
        public string Sandbox { get; set; }
    }
}
