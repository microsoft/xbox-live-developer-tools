// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System;

    /// <summary>
    /// Represents errors that occur when working with Xbox Live services.
    /// </summary>
    [Serializable]
    public class XboxLiveException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="XboxLiveException"/> object with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public XboxLiveException(string message)
            : base(message)
        {
        }
    }
}
