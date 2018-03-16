// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    /// <summary>
    /// Severity of the validation error.
    /// </summary>
    public enum Severity
    {
        /// <summary>
        /// These don't necessarily prevent an action but indicate something that should be considered.
        /// </summary>
        Informational = 0,

        /// <summary>
        /// Warning level failures. These indicate that something that will cause breaks if published.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error level failures. Blocks the current action.
        /// </summary>
        Error = 2
    }
}
