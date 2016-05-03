using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Clear, "LabNetwork")]
    public class ClearLabNetwork : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            driver.ClearNetworkSettings(VMName);

            base.ProcessRecord();
        }
    }
}
