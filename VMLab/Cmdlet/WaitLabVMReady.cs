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
                while (vmstate != VMState.Ready)
                {
                    vmstate = driver.GetVMState(VMName);

                    if (vmstate == VMState.Shutdown)
                        throw new GuestVMPoweredOffException(
                            "VM Power state is set to shutdown while waiting for it to become available.");

                    Thread.Sleep(env.SleepTimeOut);
                }

            }

            base.ProcessRecord();
        }
    }
}
