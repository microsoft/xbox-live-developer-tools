// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.IO;

namespace XboxLiveCmdlet
{

    using System;
    using System.IO;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.Tool;

    [Cmdlet(VerbsCommon.Get, "XblGlobalStorageBlob")]
    public class GetXblGlobalStorageBlob : XboxliveCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string ServiceConfigurationId { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string Sandbox { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string PathAndFileName { get; set; }

        [ValidateSet("Binary", "Json", "Config", IgnoreCase = true)]
        [Parameter(Mandatory = true, Position = 3)]
        public string FileType { get; set; }

        [Parameter(Mandatory = true, Position = 4)]
        public string OutFile { get; set; }

        [Parameter]
        public SwitchParameter ForceOverwrite { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                TitleStorageBlobType fileBlobType;
                if (!Enum.TryParse(this.FileType, true, out fileBlobType))
                {
                    WriteError(new ErrorRecord(new InvalidEnumArgumentException(), "Invalid FileType", ErrorCategory.InvalidArgument, null));
                    return;
                }

                //Check if file exist if no ForceOverWrite present. 
                if (!this.ForceOverwrite.IsPresent && File.Exists(this.OutFile))
                {
                    WriteError(new ErrorRecord(new IOException(), $"OutFile {this.OutFile} already exsit, pass in ForceOverwrite if you would like to overwrite", ErrorCategory.WriteError, null));
                    return;
                }

                byte[] data = Microsoft.Xbox.Services.Tool.TitleStorage.DownloadGlobalStorageBlob(this.ServiceConfigurationId, this.Sandbox, this.PathAndFileName, fileBlobType).Result;
                
                File.WriteAllBytes(this.OutFile, data);
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
