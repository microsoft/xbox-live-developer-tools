// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.PlayerReset
{
    using System.Collections.Generic;

    /// <summary>
    /// Class contains player resetting result.
    /// </summary>
    public class UserResetResult
    {
        /// <summary>
        /// The overall status of resetting job.
        /// </summary>
        public ResetOverallResult OverallResult { get; internal set; } = ResetOverallResult.Unknown;

        /// <summary>
        /// A collection of player reset provider status. 
        /// </summary>
        public List<JobProviderStatus> ProviderStatus { get; internal set; } = new List<JobProviderStatus>();

        /// <summary>
        /// The http error message.
        /// </summary>
        public string HttpErrorMessage { get; internal set; } = string.Empty;
    }
}
