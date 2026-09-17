// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;

    /// <summary>
    /// Signals that a user token could not be minted for the signed in test account, which is a
    /// different failure from the service refusing the call the token would have authenticated.
    /// </summary>
    internal class TestAccountTokenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestAccountTokenException"/> class.
        /// </summary>
        /// <param name="sandbox">The sandbox the token was being minted for.</param>
        /// <param name="innerException">The failure raised while minting the token.</param>
        internal TestAccountTokenException(string sandbox, Exception innerException)
            : base($"Could not obtain a token for the test account in sandbox {sandbox}.", innerException)
        {
            this.Sandbox = sandbox;
        }

        /// <summary>
        /// Gets the sandbox the token was being minted for.
        /// </summary>
        internal string Sandbox { get; }
    }
}
