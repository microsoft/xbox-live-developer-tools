// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Create a new web service.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RelyingParties")]
    public class GetRelyingParties : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The account ID that owns the relying parties.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID that owns the relying parties.", Position = 0, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Obtaining relying parties.");

            ConfigResponse<IEnumerable<RelyingParty>> response = ConfigurationManager.GetRelyingPartiesAsync(this.AccountId).Result;
            this.WriteObject(response.Result);
        }
    }
}
