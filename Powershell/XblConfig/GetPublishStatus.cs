// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets the publish status.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PublishStatus")]
    public class GetPublishStatus : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The service configuration ID.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The service configuration ID.", Position = 0, ValueFromPipeline = true)]
        public Guid Scid { get; set; }

        /// <summary>
        /// <para type="description">The sandbox being published to.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The sandbox being published to.", Position = 1, ValueFromPipeline = true)]
        public string Sandbox { get; set; }

        /// <inheritdoc/>
        protected override void Process()
        {
            this.WriteVerbose("Getting publish status.");

            ConfigResponse<PublishResponse> response = ConfigurationManager.GetPublishStatusAsync(this.Scid, this.Sandbox).Result;
            this.WriteObject($"Status: {response.Result.Status}");
            if (!string.IsNullOrEmpty(response.Result.StatusMessage))
            {
                this.WriteObject($"Status Message: {response.Result.StatusMessage}");
            }
        }
    }
}
