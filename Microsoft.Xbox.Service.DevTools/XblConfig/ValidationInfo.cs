// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the validation information associated with an attempted commit.
    /// </summary>
    public class ValidationInfo
    {
        /// <summary>
        /// Gets or sets the name of the document.
        /// </summary>
        public string DocumentName { get; set; }

        /// <summary>
        /// Gets or sets the message detailing the validation information.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets an error associated with the validation information.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        public Severity Severity { get; set; }

        /// <summary>
        /// Gets or sets information detailing the context of where the validation information comes from.
        /// </summary>
        public Dictionary<string, string> Context { get; set; }
    }
}
