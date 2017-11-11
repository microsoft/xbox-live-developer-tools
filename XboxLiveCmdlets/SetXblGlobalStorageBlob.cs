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

    [Cmdlet(VerbsCommon.Set, "XblGlobalStorageBlob")]
    public class SetXblGlobalStorageBlob : XboxliveCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string ServiceConfigurationId { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string Sandbox { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string SourceFile { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public string DestPathAndFileName { get; set; }

        [ValidateSet("Binary", "Json", "Config", IgnoreCase = true)]
        [Parameter(Mandatory = true, Position = 4)]
        public string FileType { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                TitleStorageBlobType fileBlobType;
                if (!Enum.TryParse(this.FileType, true, out fileBlobType))
                {
                    WriteError(new ErrorRecord(new InvalidEnumArgumentException(), "Invalid FileType", ErrorCategory.InvalidArgument, null));
                }

                byte[] blobData = File.ReadAllBytes(this.SourceFile);

                Microsoft.Xbox.Services.Tool.TitleStorage.UploadGlobalStorageBlob(this.ServiceConfigurationId, this.Sandbox, this.DestPathAndFileName, fileBlobType, blobData).Wait();
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
