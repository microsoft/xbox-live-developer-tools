// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the parameters needed for setting up a request.
    /// </summary>
    public class RequestParameters
    {
        /// <summary>
        /// Gets or sets the title ID.
        /// </summary>
        public string TitleId { get; set; }

        /// <summary>
        /// Gets or sets the service configuration ID.
        /// </summary>
        public Guid Scid { get; set; }

        /// <summary>
        /// Gets or sets the account ID.
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets a sandbox.
        /// </summary>
        public ISet<string> Sandboxes { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether to automatically attach an authorization token header.
        /// </summary>
        public bool AutoAttachAuthHeader { get; set; }
    }
}
