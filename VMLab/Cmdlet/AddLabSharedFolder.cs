using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Add, "LabSharedFolder")]
    public class AddLabSharedFolder : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        [Parameter(Mandatory = true)]
        public string HostPath { get; set; }

        [Parameter(Mandatory = true)]
        public string GuestPath { get; set; }

        [Parameter(Mandatory = true)]
        public string ShareName { get; set; }
        
        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            driver.AddSharedFolder(VMName, HostPath, ShareName, GuestPath);
            
            base.ProcessRecord();
        }
    }
}
