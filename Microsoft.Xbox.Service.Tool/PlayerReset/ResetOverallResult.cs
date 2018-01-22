// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTool.PlayerReset
{
    /// <summary>
    /// Over all status of one player resetting job.
    /// </summary>
    public enum ResetOverallResult
    {
        /// <summary>
        /// Job Succeeded
        /// </summary>
        Succeeded = 0,

        /// <summary>
        /// Job has completed but one or more errors occurred.
        /// </summary>
        CompletedError,

        /// <summary>
        /// Job has timeout
        /// </summary>
        Timeout,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown
    }
}
