using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Remove, "LabSharedFolder")]
    public class RemoveLabSharedFolder : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        [Parameter(Mandatory = true)]
        public string ShareName { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            driver.RemoveSharedFolder(VMName, ShareName);

            base.ProcessRecord();
        }
    }
}
