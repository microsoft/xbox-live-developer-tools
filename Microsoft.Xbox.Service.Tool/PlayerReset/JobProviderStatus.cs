// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Class contains details of one player reset job provider status
    /// </summary>
    public class JobProviderStatus
    {
        /// <summary>
        /// The Name of the player reset job provider.
        /// </summary>
        [JsonProperty("provider")]
        public string Provider { get; set; }

        /// <summary>
        /// The provider status of reset job.
        /// </summary>
        [JsonProperty("status"),JsonConverter(typeof(StringEnumConverter))]
        public ResetProviderStatus Status { get; set; }

        /// <summary>
        /// The error message of the provider reset job
        /// </summary>
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
