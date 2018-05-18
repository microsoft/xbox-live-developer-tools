// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    /// <summary>
    /// Represents an alternate ID of a product.
    /// </summary>
    public class AlternateId
    {
        /// <summary>
        /// Gets or sets the type of this alternate ID.
        /// </summary>
        public AlternateIdType AlternateIdType { get; set; }

        /// <summary>
        /// Gets or sets the ID value.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.AlternateIdType} - {this.Value}";
        }
    }
}
