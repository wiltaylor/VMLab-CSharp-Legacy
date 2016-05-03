using System.Management.Automation;
using System.Management.Automation.Language;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.New, "Lab")]
    public class NewLab : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Template { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            driver.CreateLabFile(Template);

            base.ProcessRecord();
        }
    }
}
