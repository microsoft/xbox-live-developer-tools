using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XboxLiveCmdlet
{
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
