// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    /// <summary>
    /// Represents the types of configuration documents in Xbox Live.
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// The document type is tied to a specific sandbox.
        /// </summary>
        Sandbox,

        /// <summary>
        /// The document type is tied to an account.
        /// </summary>
        Account
    }
}
