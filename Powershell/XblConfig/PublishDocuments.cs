// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets a specific relying party document.</para>
    /// </summary>
    [Cmdlet(VerbsData.Publish, "Documents")]
    public class PublishDocuments : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The Service configuration ID.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The Service configuration ID.", Position = 0, ValueFromPipeline = true)]
        public Guid Scid { get; set; }

        /// <summary>
        /// <para type="description">The sandbox to publish from.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The sandbox to publish from.", Position = 1, ValueFromPipeline = true)]
        public string SourceSandbox { get; set; }

        /// <summary>
        /// <para type="description">The sandbox to publish to.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The sandbox to publish to.", Position = 2, ValueFromPipeline = true)]
        public string DestinationSandbox { get; set; }

        /// <summary>
        /// <para type="description">A boolean valid indicating whether to perform a validate-only publish.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A boolean valid indicating whether to perform a validate-only publish.", Position = 3, ValueFromPipeline = true)]
        public bool ValidateOnly { get; set; }

        /// <summary>
        /// <para type="description">The config set version to publish.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The config set version to publish.", Position = 4, ValueFromPipeline = true)]
        public ulong? ConfigSetVersion { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose(this.ValidateOnly ? "Validating." : "Publishing.");

            ConfigResponse<PublishResponse> response;
            if (this.ConfigSetVersion.HasValue)
            {
                response = ConfigurationManager.PublishAsync(this.Scid, this.SourceSandbox, this.DestinationSandbox, this.ValidateOnly, this.ConfigSetVersion.Value).Result;
            }
            else
            {
                response = ConfigurationManager.PublishAsync(this.Scid, this.SourceSandbox, this.DestinationSandbox, this.ValidateOnly).Result;
            }

            this.PrintValidationInfo(response.Result.ValidationResult);
            this.WriteObject($"Status: {response.Result.Status}");
            if (!string.IsNullOrEmpty(response.Result.StatusMessage))
            {
                this.WriteObject($"Status Message: {response.Result.StatusMessage}");
            }
        }
    }
}