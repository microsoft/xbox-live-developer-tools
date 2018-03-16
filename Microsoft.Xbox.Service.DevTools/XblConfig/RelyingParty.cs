// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;

    /// <summary>
    /// Represents relying party information.
    /// </summary>
    public class RelyingParty
    {
        /// <summary>
        ///  Gets or sets the account ID associated with the relying party.
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets the filename associated with a relying party.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the name associated with a relying party.
        /// </summary>
        public string Name { get; set; }
    }
}
