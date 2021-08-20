// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///  Class represents a developer account.
    /// </summary>
    public class TestAccount : IToolAccount
    {
        internal TestAccount(XdtsTokenResponse xtoken)
        {
            object xui = null;

            xtoken.DisplayClaims?.TryGetValue("xui", out xui);
            if (xui == null)
                return;

            JArray array = xui as JArray;
            if (array == null)
                return;

            JObject list = array[0] as JObject;
            if (list == null)
                return;

            if (list.ContainsKey("agg"))
            {
                this.AgeGroup = list["agg"].ToString();
            }

            if (list.ContainsKey("gtg"))
            {
                this.Gamertag = list["gtg"].ToString();
            }

            if (list.ContainsKey("prv"))
            {
                this.PrivilegeString = list["prv"].ToString();
            }

            if (list.ContainsKey("usr"))
            {
                this.RestrictedPrivilegeString = list["usr"].ToString();
            }

            if (list.ContainsKey("xid"))
            {
                this.Xuid = list["xid"].ToString();
            }

            if (list.ContainsKey("uhs"))
            {
                this.UserHash = list["uhs"].ToString();
            }
        }

        internal TestAccount()
        {
        }

        /// <summary>
        /// Age group of test account
        /// </summary>
        public string AgeGroup { get; set; }

        /// <summary>
        /// Gamertag of test account
        /// </summary>
        public string Gamertag { get; set; }

        /// <summary>
        /// Allowed privileges
        /// </summary>
        public string PrivilegeString { get; set; }

        /// <summary>
        /// Restricted privileges
        /// </summary>
        public string RestrictedPrivilegeString { get; set; }

        /// <summary>
        /// Xbox Live User ID of the test account
        /// </summary>
        public string Xuid { get; set; }

        /// <summary>
        /// User hash of the test account
        /// </summary>
        public string UserHash { get; set; }
    }
}
