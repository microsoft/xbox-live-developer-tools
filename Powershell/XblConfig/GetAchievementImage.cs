// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets the details of an achievement image by its asset ID.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AchievementImage")]
    public class GetAchievementImage : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The service configuration ID.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The service configuration ID.", Position = 0, ValueFromPipeline = true)]
        public Guid Scid { get; set; }

        /// <summary>
        /// <para type="description">The ID of the image.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The ID of the image.", Position = 1, ValueFromPipeline = true)]
        public Guid AssetId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Getting achievement image.");

            ConfigResponse<AchievementImage> response = ConfigurationManager.GetAchievementImageAsync(this.Scid, this.AssetId).Result;
            this.WriteObject(response.Result);
        }
    }
}
