// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XboxLiveCmdlet
{
    using System.Diagnostics;
    using System.Management.Automation;

    public class XboxliveCmdlet : PSCmdlet
    {
        private int traceListenerId = -1;
        protected override void BeginProcessing()
        {
            object verbose = null;
            if (this.MyInvocation.BoundParameters.TryGetValue("Verbose", out verbose))
            {
                traceListenerId = Trace.Listeners.Add(new ConsoleTraceListener());
            }
            base.BeginProcessing();
        }

        protected override void EndProcessing()
        {
            if (traceListenerId >= 0)
            {
                Trace.Listeners.RemoveAt(traceListenerId);
            }
            base.EndProcessing();
        }
    }
}
