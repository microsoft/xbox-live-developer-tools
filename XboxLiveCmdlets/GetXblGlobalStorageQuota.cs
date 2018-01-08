// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using Microsoft.Tools.WindowsDevicePortal;
    using Microsoft.Win32;
    using System;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "XblGlobalStorageQuota")]
    public class GetXblGlobalStorageQuota : XboxliveCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string ServiceConfigurationId { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string Sandbox { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var quote = Microsoft.Xbox.Services.Tool.TitleStorage.GetGlobalStorageQuotaAsync(this.ServiceConfigurationId, this.Sandbox).Result;
                WriteObject(quote, false);
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
