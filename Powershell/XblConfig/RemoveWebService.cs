// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Delete a web service.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "WebService")]
    public class RemoveWebService : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The ID of the web service.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The ID of the web service.", Position = 0, ValueFromPipeline = true)]
        public Guid ServiceId { get; set; }

        /// <summary>
        /// <para type="description">The account ID that owns the web service.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID that owns the web service.", Position = 1, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Deleting web service.");

            ConfigurationManager.DeleteWebServiceAsync(this.AccountId, this.ServiceId).Wait();
            this.WriteVerbose($"Web service with ID {this.ServiceId} successfully deleted.");
        }
    }
}
