using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Get, "LabVMSetting")]
    public class GetLabVMSetting : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            WriteObject(driver.ReadVMSetting(VMName, Name));

            base.ProcessRecord();
        }
    }
}
