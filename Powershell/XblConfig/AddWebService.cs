// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Create a new web service.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "WebService")]
    public class AddWebService : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The name to give the web service.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The name to give the web service.", Position = 0, ValueFromPipeline = true)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">A boolean value allowing your service to retrieve game telemetry data for any of your games.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A boolean value allowing your service to retrieve game telemetry data for any of your games.", Position = 1, ValueFromPipeline = true)]
        public bool TelemetryAccess { get; set; }

        /// <summary>
        /// <para type="description">A boolean value that gives the media provider owning the service the authority to programmatically publish app channels for consumption on console through the OneGuide twist.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A boolean value that gives the media provider owning the service the authority to programmatically publish app channels for consumption on console through the OneGuide twist.", Position = 2, ValueFromPipeline = true)]
        public bool AppChannelsAccess { get; set; }

        /// <summary>
        /// <para type="description">The account ID that owns the web service.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID that owns the web service.", Position = 3, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Creating web service.");

            ConfigResponse<WebService> response = ConfigurationManager.CreateWebServiceAsync(this.AccountId, this.Name, this.TelemetryAccess, this.AppChannelsAccess).Result;
            this.WriteVerbose($"Web service created with ID {response.Result.ServiceId}");
            this.WriteObject(response.Result);
        }
    }
}