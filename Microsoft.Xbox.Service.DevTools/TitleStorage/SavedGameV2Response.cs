// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using Newtonsoft.Json;

    /// <summary>
    /// Response data contract for PUT /users/xuid({xuid})/storage/titlestorage/titlegroups/{titleGroupId}/savedgames/{context}/savedgames/{fileNameAndPath}
    /// </summary>
    public class SavedGameV2Response
    {
        /// <summary>
        /// Gets or sets the list of atoms in this saved game smart blob.
        /// </summary>
        [JsonProperty("atoms")]
        public ExtendedAtomInfo[] Atoms { get; set; }
    }
}