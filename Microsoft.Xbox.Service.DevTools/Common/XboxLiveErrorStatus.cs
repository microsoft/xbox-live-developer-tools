// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    /// <summary>
    /// Enum type of XboxLive error status.
    /// </summary>
    public enum XboxLiveErrorStatus
    {
        /// <summary>
        /// Unexpected Error, non-transient
        /// </summary>
        UnExpectedError,

        /// <summary>
        /// Fail to sign in, non-transient
        /// </summary>
        AuthenticationFailure,

        /// <summary>
        /// The client is unauthorized to access particular resource, non-transient
        /// </summary>
        Forbidden,

        /// <summary>
        /// The client didn't find particular resource, non-transient
        /// </summary>
        NotFound,

        /// <summary>
        /// Invalid client request, non-transient
        /// </summary>
        BadRequest,

        /// <summary>
        /// Client is sending too many request, transient
        /// </summary>
        TooManyRequsts,

        /// <summary>
        /// Server error, transient
        /// </summary>
        ServerError,

        /// <summary>
        /// Client failed to establish communication with service, transient
        /// </summary>
        NetworkError,

        /// <summary>
        /// The operation was canceled by the user.
        /// </summary>
        UserCancelled,
    }
}
