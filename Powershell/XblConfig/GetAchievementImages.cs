// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets the details of all achievement images associated with this SCID.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AchievementImages")]
    public class GetAchievementImages : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The service configuration ID.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The service configuration ID.", Position = 0, ValueFromPipeline = true)]
        public Guid Scid { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Getting achievement images.");

            ConfigResponse<IEnumerable<AchievementImage>> response = ConfigurationManager.GetAchievementImagesAsync(this.Scid).Result;
            this.WriteObject(response.Result, true);
        }
    }
}
