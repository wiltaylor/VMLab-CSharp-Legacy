using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Get, "LabVMState")]
    public class GetLabVMState : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            WriteObject(driver.GetVMState(VMName));

            base.ProcessRecord();
        }
    }
}
