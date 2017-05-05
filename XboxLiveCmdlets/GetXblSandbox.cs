//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace XboxLiveCmdlet
{
    using System;
    using System.Management.Automation;
    using Microsoft.Tools.WindowsDevicePortal;
    using Microsoft.Win32;
    using System.Collections.Generic;

    [Cmdlet(VerbsCommon.Get, "XblSandbox")]
    public class GetXblSandbox : XboxliveCmdlet
    {
        [Parameter]
        public string MachineName { get; set; }

        internal static PSObject GetLocalSandboxObject()
        {
            RegistryKey regBase = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

            string sandboxId = "RETAIL";
            var xboxLiveKey = regBase.OpenSubKey(@"SOFTWARE\Microsoft\XboxLive");
            if (xboxLiveKey != null)
            {
                sandboxId = xboxLiveKey.GetValue("Sandbox", "RETAIL").ToString();
            }

            var resultObj = new PSObject();
            resultObj.Members.Add(new PSNoteProperty("Sandbox", sandboxId));

            return resultObj;
        }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrEmpty(MachineName))
                {
                    WriteObject(GetLocalSandboxObject());
                }
                else
                {
                    string url = "https://" + MachineName + ":11443";
                    IDevicePortalConnection connection = new DefaultDevicePortalConnection(url, string.Empty, string.Empty);
                    DevicePortal portal = new DevicePortal(connection);
                    portal.ConnectAsync().Wait();
                    var task = portal.GetXboxLiveSandboxAsync();
                    task.Wait();
                    var result = task.Result;

                    WriteObject(result, false);
                }
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                WriteError(new ErrorRecord(innerEx, "Get-XblSandbox failed: " + innerEx.Message, ErrorCategory.InvalidOperation, null));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Get-XblSandbox failed: " + ex.Message, ErrorCategory.InvalidOperation, null));
            }
        }

    }
}
