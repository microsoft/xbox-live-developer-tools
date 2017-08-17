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
    using System.Security;
    using Microsoft.Xbox.Services.Tool;

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
