using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.PowerShell;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Get, "LabTemplate")]
    public class GetLabTemplate : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);
         
            WriteObject(driver.GetTemplates().Cast<IDictionary<string,object>>(), true);

            base.ProcessRecord();
        }
    }
}
