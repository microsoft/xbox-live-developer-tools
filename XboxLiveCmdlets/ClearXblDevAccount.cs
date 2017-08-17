// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using System;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Clear, "XblDevAccount")]
    public class ClearXblDevAccount: XboxliveCmdlet
    {
        protected override void ProcessRecord()
        {
            try
            {
                Microsoft.Xbox.Services.Tool.Auth.SignOut();
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Clear-XBLDevAccount failed", ErrorCategory.SecurityError, null));
            }
        }

    }
}
