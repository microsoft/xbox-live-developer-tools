// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Collections.Generic;

    internal class XdtsTokenResponse
    {
        /// <summary>
        /// Gets or sets the issue instant for the token.
        /// </summary>
        public DateTime IssueInstant { get; set; }

        /// <summary>
        /// Gets or sets the expiration date/time for the token.
        /// </summary>
        public DateTime NotAfter { get; set; }

        /// <summary>
        /// Gets or sets the encrypted and signed token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets a collection of claims that are 
        /// non-encrypted and available publicly.
        /// </summary>
        public Dictionary<string, object> DisplayClaims { get; set; }
    }
}