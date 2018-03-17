// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the response of an attempted publish.
    /// </summary>
    public class PublishResponse
    {
        /// <summary>
        /// Gets or sets the status of the publish job.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the status message of the publish job.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets a list of validation results from the attempted publish.
        /// </summary>
        public IEnumerable<ValidationInfo> ValidationResult { get; set; }

        /// <summary>
        /// Gets or sets the changeset version of the source sandbox.
        /// </summary>
        public string SourceVersion { get; set; }

        /// <summary>
        /// Gets or sets the changeset version of the publish.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the publish job ID.
        /// </summary>
        public Guid? JobId { get; set; }
    }
}
