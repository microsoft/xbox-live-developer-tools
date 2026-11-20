// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    /// <summary>
    /// A person the signed in account follows.
    /// </summary>
    internal class FollowedPerson
    {
        /// <summary>
        /// Gets or sets the XUID of the person.
        /// </summary>
        public string Xuid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the account has marked the person as a favorite.
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the person follows the account back.
        /// </summary>
        public bool IsFollowingCaller { get; set; }
    }
}
