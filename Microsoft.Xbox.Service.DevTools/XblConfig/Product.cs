// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a product within Dev Center.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Gets or sets the account ID associated with this product.
        /// </summary>
        [Display(Name = "Account ID", Order = 2, ListOmit = true)]
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets any alternate IDs for this product.
        /// </summary>
        [Display(Name = "Alternate IDs", Order = 8, ListOmit = true)]
        public IEnumerable<AlternateId> AlternateIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this product is a test product or not.
        /// </summary>
        [Display(Omit = true, ListOmit = true)]
        public bool IsTest { get; set; }

        /// <summary>
        /// Gets or sets the Microsoft Account application ID.
        /// </summary>
        [Display(Name = "MSA App ID", Order = 3, ListOmit = true)]
        public string MsaAppId { get; set; }

        /// <summary>
        /// Gets or sets the package family name ID.
        /// </summary>
        [Display(Name = "Package Family Name", Order = 4, ListOrder = 2)]
        public string PfnId { get; set; }

        /// <summary>
        /// Gets or sets the primary service configuration ID.
        /// </summary>
        [Display(Name = "SCID", Order = 5, ListOmit = true)]
        public Guid PrimaryServiceConfigId { get; set; }

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        [Display(Name = "Product ID", Order = 1, ListOrder = 1)]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Gets or sets the title ID.
        /// </summary>
        [Display(Name = "Title ID", Order = 6, ListOrder = 3)]
        public uint TitleId { get; set; }

        /// <summary>
        /// Gets or sets the Xbox Live tier.
        /// </summary>
        [Display(Name = "Tier", Order = 7, ListOrder = 4)]
        public XboxLiveTier XboxLiveTier { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        [Display(Name = "Product Name", Order = 9, ListOrder = 1)]
        public string ProductName { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        [Display(Name = "Bound Title ID", Order = 10, ListOrder = 1)]
        public string BoundTitleId { get; set; }
    }
}
