using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Remove, "LabPackageRepository")]
    public class RemoveLabPackageRepository : PSCmdlet
    {
        [Parameter(Mandatory =  true, Position = 1)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var manager = svc.GetObject<IPackageManager>();

            env.UpdateEnvironment(this);

            manager.RemoveRepository(Name);
            manager.ScanPackages();

            base.ProcessRecord();
        }
    }

}
