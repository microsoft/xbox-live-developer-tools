//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
namespace Microsoft.Xbox.Services.Tool
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