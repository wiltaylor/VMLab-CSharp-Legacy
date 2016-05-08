using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsData.Sync, "LabPackages")]
    public class SyncLabPackages : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var manager = svc.GetObject<IPackageManager>();

            env.UpdateEnvironment(this);

            manager.ScanPackages();

            base.ProcessRecord();
        }
    }
}
