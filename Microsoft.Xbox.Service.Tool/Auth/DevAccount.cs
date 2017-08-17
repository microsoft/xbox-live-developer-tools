// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    public enum DevAccountSource
    {
        XboxDeveloperPortal,

        UniversalDeveloperCenter
    }

    public class DevAccount
    {
        internal DevAccount(XdtsTokenResponse etoken, DevAccountSource accountSource)
        {
            object value;
            if (etoken.DisplayClaims.TryGetValue("eid", out value))
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
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string AccountId { get; private set; }

        public string AccountType { get; private set; }

        public string AccountMoniker { get; private set; }

        public DevAccountSource AccountSource { get; internal set; }

    }
}
