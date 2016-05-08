using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "LabPackage")]
    public class InvokeLabPackage: PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string VMName { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public string Version { get; set; }

        [Parameter(Mandatory = true, Position = 4)]
        public string ActionName { get; set; }


        protected override void ProcessRecord()
        {

            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var manager = svc.GetObject<IPackageManager>();

            env.UpdateEnvironment(this);

            manager.RunPackageAction(Name, Version, ActionName, VMName);

            base.ProcessRecord();
        }
    }
}
