// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using Microsoft.Xbox.Services.Tool;
    using System;
    using System.Management.Automation;

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
                DevAccountSource accountType = DevAccountSource.WindowsDeveloperCenter;
                if (AccountSource.Equals("XboxDeveloperPortal", StringComparison.OrdinalIgnoreCase)  || AccountSource.Equals("XDP", StringComparison.OrdinalIgnoreCase))
                {
                    accountType = DevAccountSource.XboxDeveloperPortal;
                }
                Microsoft.Xbox.Services.Tool.Auth.SetAuthInfo(UserName, accountType);
                DevAccount account = Microsoft.Xbox.Services.Tool.Auth.SignIn().Result;

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
