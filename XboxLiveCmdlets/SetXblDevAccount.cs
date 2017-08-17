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

    [Cmdlet(VerbsCommon.Set, "XblDevAccount")]
    public class SetXblDevAccount: XboxliveCmdlet
    {
        [ValidateSet("XboxDeveloperPortal", "UniversalDeveloperCenter", "XDP", "UDC", IgnoreCase = true)]
        [Parameter(Mandatory = true, Position = 0)]
        public string AccountSource { get; set; }

        [Parameter(Position = 1)]
        public string UserName { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                DevAccountSource accountType = DevAccountSource.UniversalDeveloperCenter;
                if (AccountSource.Equals("XboxDeveloperPortal", StringComparison.OrdinalIgnoreCase)  || AccountSource.Equals("XDP", StringComparison.OrdinalIgnoreCase))
                {
                    accountType = DevAccountSource.XboxDeveloperPortal;
                }
                DevAccount account = Microsoft.Xbox.Services.Tool.Auth.SignIn(accountType, UserName).Result;

                WriteObject(account);
            }
            catch (AggregateException e)
            {
                var innerEx = e.InnerException;
                WriteError(new ErrorRecord(innerEx, "Set-XBLDevUdcAccount failed", ErrorCategory.SecurityError, null));
            }
        }

    }
}
