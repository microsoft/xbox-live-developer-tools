//  <copyright file="XdtsTokenResponse.cs" company="Microsoft">
//      Copyright (c) Microsoft. All rights reserved.
//      Internal use only.
//  </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.XboxTest.Xdts
{
    using System;
    using System.Collections.Generic;

    public class XdtsTokenResponse
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