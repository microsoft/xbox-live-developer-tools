// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.Authentication;

    /// <summary>
    /// <para type="synopsis">Sign out the Xbox Live developer account.</para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Disconnect, "DevAccount")]
    public class DisconnectDevAccount : PSCmdletBase
    {
        /// <inheritdoc/>
        protected override void Process()
        {
            DevAccount account = ToolAuthentication.LoadLastSignedInUser();
            if (account != null)
            {
                ToolAuthentication.SignOut();
                this.WriteObject($"Developer account {account.Name} from {account.AccountSource} has successfully signed out.");
            }
            else
            {
                this.WriteObject($"No signed in account found.");
            }
        }
    }
}
