// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets a list of web services for a given account.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WebServices")]
    public class GetWebServices : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The account ID associated with the list of sandboxes.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID associated with the list of sandboxes.", Position = 0, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }
        
        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Obtaining web services.");

            ConfigResponse<IEnumerable<WebService>> response = ConfigurationManager.GetWebServicesAsync(this.AccountId).Result;
            this.WriteObject(response.Result);
        }
    }
}
