using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Restart, "LabVM")]
    public class RestartLabVM: PSCmdlet
    {
        [Parameter(Mandatory =  true)]
        public string VMName { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            driver.ResetVM(VMName, Force);

            base.ProcessRecord();
        }
    }
}
