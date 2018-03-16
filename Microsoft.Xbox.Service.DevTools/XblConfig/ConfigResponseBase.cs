// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    /// <summary>
    /// Represents the abstract base class for configuiration responses.
    /// </summary>
    public abstract class ConfigResponseBase
    {
        /// <summary>
        /// Gets the correlation ID or correlation vector for the response.
        /// </summary>
        public string CorrelationId { get; protected internal set; }
    }
}
