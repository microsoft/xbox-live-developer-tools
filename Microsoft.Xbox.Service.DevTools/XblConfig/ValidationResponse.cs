// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the validation response from an attempted documents commit.
    /// </summary>
    public class ValidationResponse
    {
        /// <summary>
        /// Gets or sets a boolean value indicating whether a commit can occur.
        /// </summary>
        public bool CanCommit { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the commit occurred.
        /// </summary>
        public bool Committed { get; set; }

        /// <summary>
        /// Gets or sets the ETag string associated with the commit.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the version associated with the commit.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets a collection of <see cref="ValidationInfo"/> objects describing any warnings or errors.
        /// </summary>
        public IEnumerable<ValidationInfo> ValidationInfo { get; set; }
    }
}
