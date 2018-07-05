// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.Authentication;

    /// <summary>
    /// <para type="synopsis">Gets the details of the currently signed in dev account.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "DevAccount")]
    public class GetDevAccount : PSCmdletBase
    {
        /// <inheritdoc/>
        protected override void BeginProcessing()
        {
            this.RequiresAuthorization = false;
        }

        /// <inheritdoc/>
        protected override void Process()
        {
            DevAccount account = ToolAuthentication.LoadLastSignedInUser();
            if (account != null)
            {
                this.WriteObject($"Developer account {account.Name} from {account.AccountSource} is currently signed in.");
                this.WriteObject(account);
            }
            else
            {
                this.WriteObject($"No signed in account found.");
            }
        }
    }
}
