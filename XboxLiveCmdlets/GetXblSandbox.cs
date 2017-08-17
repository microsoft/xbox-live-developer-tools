// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using Microsoft.Tools.WindowsDevicePortal;
    using Microsoft.Win32;
    using System;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "XblSandbox")]
    public class GetXblSandbox : XboxliveCmdlet
    {
        [Parameter]
        public string ConsoleName { get; set; }

        [Parameter]
        public string UserName { get; set; }

        [Parameter]
        public string Password { get; set; }

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
                if (string.IsNullOrEmpty(ConsoleName))
                {
                    WriteObject(GetLocalSandboxObject());
                }
                else
                {
                    string url = "https://" + ConsoleName + ":11443";
                    var result = WdpConnections.GetXboxLiveSandboxAsync(url, UserName, Password).Result;

                    WriteObject(result, false);
                }
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                var deviceEx = innerEx as DevicePortalException;
                if (deviceEx != null)
                {
                    WriteError(new ErrorRecord(ex, $"Get-XblSandbox failed, reason: {deviceEx.Reason}, code: {deviceEx.StatusCode}", ErrorCategory.InvalidOperation, null));
                }
                else
                {
                    WriteError(new ErrorRecord(innerEx, "Get-XblSandbox failed: " + innerEx.Message,
                        ErrorCategory.InvalidOperation, null));
                }
                
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Get-XblSandbox failed: " + ex.Message, ErrorCategory.InvalidOperation, null));
            }
        }

    }
}
