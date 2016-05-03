using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Register, "LabAction")]
    [Alias("Action")]
    public class RegisterLabAction : PSCmdlet
    {
        [Parameter(Mandatory = true, Position =  1)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public ScriptBlock Code { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var sh = svc.GetObject<IScriptHelper>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            if (env.CurrentAction != Name)
                return;

            sh.Invoke(Code);

            base.ProcessRecord();
        }
    }

}
