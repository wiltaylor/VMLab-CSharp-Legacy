using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Get, "LabPackages")]
    public class GetLabPackages : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var manager = svc.GetObject<IPackageManager>();

            env.UpdateEnvironment(this);

            WriteObject(manager.GetPackages(), true);

            base.ProcessRecord();
        }
    }
}
