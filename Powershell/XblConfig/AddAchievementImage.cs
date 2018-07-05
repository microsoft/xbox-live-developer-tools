// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Uploads an achievement image to a specific SCID.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AchievementImage")]
    public class AddAchievementImage : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The service configuration ID.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The service configuration ID.", Position = 0, ValueFromPipeline = true)]
        public Guid Scid { get; set; }

        /// <summary>
        /// <para type="description">The file to upload.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The file to upload.", Position = 1, ValueFromPipeline = true)]
        public string Filename { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Uploading achievement image.");

            using (FileStream stream = File.OpenRead(this.Filename))
            {
                ConfigResponse<AchievementImage> response = ConfigurationManager.UploadAchievementImageAsync(this.Scid, Path.GetFileName(stream.Name), stream).Result;
                this.WriteObject(response.Result);
            }
        }
    }
}
