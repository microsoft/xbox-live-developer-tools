// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using Microsoft.Tools.WindowsDevicePortal;
    using Microsoft.Win32;
    using System;
    using System.Management.Automation;


    [Cmdlet(VerbsCommon.Get, "XblGlobalStorageBlobMetadata")]
    public class GetXblGlobalStorageBlobMetadata : XboxliveCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string ServiceeConfigurationId { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string Sandbox { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string Path { get; set; }

        [Parameter(Position = 3)]
        public uint MaxItem { get; set; }

        [Parameter(Position = 4)]
        public uint SkipItem { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var metadataResult = Microsoft.Xbox.Services.Tool.TitleStorage.GetGlobalStorageBlobMetaData(this.ServiceeConfigurationId, this.Sandbox, this.Path, this.MaxItem, this.SkipItem).Result;
                Console.WriteLine("");
                Console.WriteLine($"Total item count {metadataResult.TotalItems}, HasNext: {metadataResult.HasNext}");
                Console.WriteLine("");

                WriteObject(metadataResult.Items, false);
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                WriteError(new ErrorRecord(innerEx, "Get-XblGlobalStorageQuota failed: " + innerEx?.Message, ErrorCategory.InvalidOperation, null));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Get-XblGlobalStorageQuota failed: " + ex.Message, ErrorCategory.InvalidOperation, null));
            }
        }

    }
}
