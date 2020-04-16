// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using Microsoft.Xbox.Services.DevTools.Authentication;

    /// <summary>
    /// <para type="synopsis">Logs in a dev account.</para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Connect, "DevAccount")]
    public class ConnectDevAccount : PSCmdletBase
    {
        private enum AccountSourceOption
        {
            WindowsDevCenter = DevAccountSource.WindowsDevCenter,
        }

        /// <summary>
        /// <para type="description">The user name of the account.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The user name of the account.", Position = 0, ValueFromPipeline = true)]
        public string UserName { get; set; }

        /// <summary>
        /// <para type="description">The account source where the developer account was registered. Can be either 'WindowsDevCenter'.</para>
        /// </summary>
        [ValidateSet("WindowsDevCenter")]
        public string AccountSource { get; set; } = "WindowsDevCenter";

        /// <inheritdoc/>
        protected override void BeginProcessing()
        {
            this.RequiresAuthorization = false;
        }

        /// <inheritdoc/>
        protected override void Process()
        {
            if (!Enum.TryParse(this.AccountSource, out AccountSourceOption source))
            {
                throw new ArgumentException("Invalid account source. Must be either 'WindowsDevCenter'.", nameof(this.AccountSource));
            }

            DevAccount devAccount = ToolAuthentication.SignInAsync((DevAccountSource)source, this.UserName).Result;
            this.WriteObject($"Developer account {devAccount.Name} has successfully signed in.");
            this.WriteObject(devAccount);
        }
    }
}
