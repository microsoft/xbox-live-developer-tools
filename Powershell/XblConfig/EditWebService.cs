// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Update a web service.</para>
    /// </summary>
    [Cmdlet(VerbsData.Edit, "WebService")]
    public class EditWebService : PSCmdletBase
    {
        /// <summary>
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The ID of the web service.", Position = 0, ValueFromPipeline = true)]
        public Guid ServiceId { get; set; }

        /// <summary>
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID that owns the web service.", Position = 1, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <summary>
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The name to give the web service.", Position = 2, ValueFromPipeline = true)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A boolean value allowing your service to retrieve game telemetry data for any of your games.", Position = 3, ValueFromPipeline = true)]
        public bool TelemetryAccess { get; set; }

        /// <summary>
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A boolean value that gives the media provider owning the service the authority to programmatically publish app channels for consumption on console through the OneGuide twist.", Position = 4, ValueFromPipeline = true)]
        public bool AppChannelsAccess { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Updating web service.");

            ConfigResponse<WebService> response = ConfigurationManager.UpdateWebServiceAsync(this.ServiceId, this.AccountId, this.Name, this.TelemetryAccess, this.AppChannelsAccess).Result;
            this.WriteVerbose($"Web service with ID {response.Result.ServiceId} successfully updated.");
            this.WriteObject(response.Result);
        }
    }
}
