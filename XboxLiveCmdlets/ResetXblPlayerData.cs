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
        public string XboxUserId { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var task = Microsoft.Xbox.Services.Tool.PlayerReset.ResetPlayerDataAsync(ServiceConfigId, SandboxId, XboxUserId);
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
