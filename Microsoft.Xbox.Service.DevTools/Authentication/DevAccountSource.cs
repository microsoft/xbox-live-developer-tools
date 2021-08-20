﻿// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    /// <summary>
    /// Enum type of XboxLive dev account source.
    /// </summary>
    public enum DevAccountSource
    {
        /// <summary>
        /// DEPRECATED Account is from Xbox developer portal: xdp.xboxlive.com
        /// </summary>
        XboxDeveloperPortal = 0,

        /// <summary>
        /// Account is from Windows Dev Center: developer.microsoft.com/windows
        /// </summary>
        WindowsDevCenter = 1,

        /// <summary>
        /// Account is a test account generated from Windows Dev Center: developer.microsoft.com/windows
        /// </summary>
        TestAccount = 2
    }
}