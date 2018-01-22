// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTool.PlayerReset
{
    /// <summary>
    /// Status of provider status of player reset job.
    /// </summary>
    public enum ResetProviderStatus
    {
        /// <summary>
        /// The job has queued.
        /// </summary>
        Queued,

        /// <summary>
        /// The job has not started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Resetting is in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// The job has successfully completed.
        /// </summary>
        CompletedSuccess,

        /// <summary>
        /// The job was partial success.
        /// </summary>
        CompletedPartialSuccess,

        /// <summary>
        /// The job has finished but some error.
        /// </summary>
        CompletedError,

        /// <summary>
        /// The job was abandoned.
        /// </summary>
        Abandoned
    }
}
