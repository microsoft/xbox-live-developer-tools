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
        [Display(Name = "Account ID", Order = 3, ListOmit = true)]
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets the filename associated with a relying party.
        /// </summary>
        [Display(Name = "Filename", Order = 2, ListOrder = 2)]
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the name associated with a relying party.
        /// </summary>
        [Display(Name = "Name", Order = 1, ListOrder = 1)]
        public string Name { get; set; }
    }
}
