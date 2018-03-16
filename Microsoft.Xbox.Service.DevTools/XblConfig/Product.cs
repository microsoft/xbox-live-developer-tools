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
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets any alternate IDs for this product.
        /// </summary>
        public IEnumerable<AlternateId> AlternateIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this product is a test product or not.
        /// </summary>
        public bool IsTest { get; set; }

        /// <summary>
        /// Gets or sets the Microsoft Account application ID.
        /// </summary>
        public string MsaAppId { get; set; }

        /// <summary>
        /// Gets or sets the package family name ID.
        /// </summary>
        public string PfnId { get; set; }

        /// <summary>
        /// Gets or sets the primary service configuration ID.
        /// </summary>
        public Guid PrimaryServiceConfigId { get; set; }

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Gets or sets the title ID.
        /// </summary>
        public uint TitleId { get; set; }

        /// <summary>
        /// Gets or sets the Xbox Live tier.
        /// </summary>
        public XboxLiveTier XboxLiveTier { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            string alternateIds = $"AlternateIds:{Environment.NewLine}";
            foreach (AlternateId alternateId in this.AlternateIds)
            {
                alternateIds += $"  {alternateId.AlternateIdType + ":", -22}{alternateId.Value}{Environment.NewLine}";
            }

            return $@"ProductId:              {this.ProductId}
AccountId:              {this.AccountId}
MsaAppId:               {this.MsaAppId}
PfnId:                  {this.PfnId}
PrimaryServiceConfigId: {this.PrimaryServiceConfigId}
TitleId:                {this.TitleId}
XboxLiveTier:           {this.XboxLiveTier}
{alternateIds}";
        }
    }
}
