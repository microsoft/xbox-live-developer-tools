// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    /// <summary>
    /// Represents the version and namespace of a configuration document schema.
    /// </summary>
    public class SchemaVersion
    {
        /// <summary>
        /// Gets or sets the version of the schema.
        /// </summary>
        [Display(Name = "Version", Order = 1, ListOrder = 1)]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the namespace for this version of the schema.
        /// </summary>
        [Display(Name = "Namespace", Order = 2, ListOrder = 2)]
        public string Namespace { get; set; }
    }
}
