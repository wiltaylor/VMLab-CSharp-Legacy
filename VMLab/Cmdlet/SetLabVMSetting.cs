using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Set, "VMLabSetting")]
    public class SetLabVMSetting : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Mandatory = true)]
        public string Value { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            driver.WriteVMSetting(VMName, Name, Value);

            base.ProcessRecord();
        }
    }
}
