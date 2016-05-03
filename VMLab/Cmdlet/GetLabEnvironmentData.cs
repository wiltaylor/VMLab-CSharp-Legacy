using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Get, "LabEnvironmentData")]
    public class GetLabEnvironmentData : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            WriteObject(env);

            base.ProcessRecord();
        }
    }
}
