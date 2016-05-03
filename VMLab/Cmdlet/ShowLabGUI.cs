using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Show, "LabGUI")]
    public class ShowLabGUI : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string VMName { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);
            
            driver.ShowGUI(VMName);

            base.ProcessRecord();
        }
    }
}
