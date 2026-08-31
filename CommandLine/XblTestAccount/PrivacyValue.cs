// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    /// <summary>
    /// The values the Xbox Live privacy service accepts for a privacy setting.
    /// </summary>
    internal enum PrivacyValue
    {
        /// <summary>
        /// Anyone is allowed.
        /// </summary>
        Everyone,

        /// <summary>
        /// Only people on the account's friends list are allowed.
        /// </summary>
        PeopleOnMyList,

        /// <summary>
        /// Nobody is allowed.
        /// </summary>
        Blocked,
    }
}
