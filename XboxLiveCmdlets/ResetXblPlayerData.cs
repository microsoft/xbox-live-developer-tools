// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Reset, "XblPlayerData")]
    public class ResetXblPlayerData : XboxliveCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ServiceConfigId { get; set; }

        [Parameter(Mandatory = true)]
        public string SandboxId { get; set; }

        [Parameter(Mandatory = true )]
        public List<string> XboxUserIds { get; set; }

        protected override void BeginProcessing()
        {
            if (!Microsoft.Xbox.Services.Tool.Auth.HasAuthInfo)
            {
                var errorRecord = new ErrorRecord(new Exception("User did not sign in, use Add-XBLDevXDPAccount command."), "", ErrorCategory.AuthenticationError, null);
                ThrowTerminatingError(errorRecord);
            }

            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            try
            {
                var task = Microsoft.Xbox.Services.Tool.ProgressResetter.ResetProgressAsync(SandboxId, ServiceConfigId, XboxUserIds);
                task.Wait();
                var result = task.Result;

                WriteObject(result, true);
            }
            catch (AggregateException e)
            {
                var innerEx = e.InnerException;
                WriteError(new ErrorRecord(innerEx, "ResetXblProgress failed", ErrorCategory.InvalidOperation, null));
            }

        }
    }
}
