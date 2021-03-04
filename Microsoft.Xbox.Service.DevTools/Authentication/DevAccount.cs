// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    /// <summary>
    ///  Class represents a developer account.
    /// </summary>
    public class DevAccount
    {
        internal DevAccount(XdtsTokenResponse etoken, DevAccountSource accountSource, string tenant)
        {
            if (etoken.DisplayClaims.TryGetValue("eid", out object value))
            {
                this.Id = value.ToString();
            }

            if (etoken.DisplayClaims.TryGetValue("enm", out value))
            {
                this.Name = value.ToString();
            }

            if (etoken.DisplayClaims.TryGetValue("eai", out value))
            {
                this.AccountId = value.ToString();
            }

            if (etoken.DisplayClaims.TryGetValue("eam", out value))
            {
                this.AccountMoniker = value.ToString();
            }

            if (etoken.DisplayClaims.TryGetValue("eat", out value))
            {
                this.AccountType = value.ToString();
            }

            this.AccountSource = accountSource;
            this.Tenant = tenant;
        }

        internal DevAccount()
        {
        }

        /// <summary>
        ///  ID of the developer account
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User name of the developer account
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Id of the account under which the developer is acting. Also known as publisher ID
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// The account type under which the developer is acting.
        /// </summary>
        public string AccountType { get; set; }

        /// <summary>
        /// The moniker of the account for which the token is issued. 
        /// </summary>
        public string AccountMoniker { get; set; }

        /// <summary>
        /// The account source where the account was registered.
        /// </summary>
        public DevAccountSource AccountSource { get; set; }

        public string Tenant { get; set; }
    }
}
