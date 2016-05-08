using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Add, "PackageRepository")]
    public class AddLabPackageRepository : PSCmdlet
    {
        [Parameter(Mandatory =  true, Position =  1)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string Path { get; set; }


        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var manager = svc.GetObject<IPackageManager>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            manager.AddRepository(Name, Path);

            base.ProcessRecord();
        }
    }
}
