using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Security;
using System.Security;
using Microsoft.PowerShell.Commands;


namespace XboxLiveCmdlet
{
    [Cmdlet(VerbsCommon.Set, "XBLDevXDPCredential")]
    public class SetXblDevXDPCredential : Cmdlet
    {
        [Parameter (Mandatory = true, Position = 0)]
        public string UserName { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public SecureString Password { set; get; }

        protected override void ProcessRecord()
        {
            try
            {
                // Save the creditial 
                PSCredential cred = new PSCredential(this.UserName, this.Password);

                var task = Microsoft.Xbox.ToolAuth.GetXDPEToken(this.UserName, this.Password);
                task.Wait();
                WriteObject(task.Result);
            }
            catch(AggregateException e)
            {
                var innerEx = e.InnerException;
                WriteError(new ErrorRecord(innerEx, "GetXDPEToken failed", ErrorCategory.SecurityError, null));
                //Wri
            }
            
        }
    }
}
