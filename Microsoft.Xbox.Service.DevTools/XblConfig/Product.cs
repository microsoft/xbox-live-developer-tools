// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a product within Dev Center.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Gets or sets the account ID associated with this product.
        /// </summary>
        [Display(Name = "Account ID", Order = 7)]
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets any alternate IDs for this product.
        /// </summary>
        [Display(Name = "Alternate IDs", Order = 6)]
        public IEnumerable<AlternateId> AlternateIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this product is a test product or not.
        /// </summary>
        [Display(AutoGenerateField = false)]
        public bool IsTest { get; set; }

        /// <summary>
        /// Gets or sets the Microsoft Account application ID.
        /// </summary>
        [Display(Name = "MSA App ID", Order = 4)]
        public string MsaAppId { get; set; }

        /// <summary>
        /// Gets or sets the package family name ID.
        /// </summary>
        [Display(Name = "PFN ID", Order = 3)]
        public string PfnId { get; set; }

        /// <summary>
        /// Gets or sets the primary service configuration ID.
        /// </summary>
        [Display(Name = "SCID", Order = 1)]
        public Guid PrimaryServiceConfigId { get; set; }

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        [Display(AutoGenerateField = false)]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Gets or sets the title ID.
        /// </summary>
        [Display(Name = "Title ID", Order = 2)]
        public uint TitleId { get; set; }

        /// <summary>
        /// Gets or sets the Xbox Live tier.
        /// </summary>
        [Display(Name = "Xbox Live tier", Order = 5)]
        public XboxLiveTier XboxLiveTier { get; set; }
    }
}
