using System;
using System.Management.Automation;
using System.Threading;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Wait, "LabVM")]
    public class WaitLabVMReady: PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string VMName { get; set; }

        [Parameter]
        public SwitchParameter Shutdown { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var driver = svc.GetObject<IDriver>();

            env.UpdateEnvironment(this);

            var vmstate = driver.GetVMState(VMName);

            if (Shutdown)
            {
                while (vmstate != VMState.Shutdown)
                {
                    vmstate = driver.GetVMState(VMName);
                    Thread.Sleep(env.SleepTimeOut);
                }

            }
            else
            {
                driver.WaitVMReady(VMName);
            }

            base.ProcessRecord();
        }
    }
}
