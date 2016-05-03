using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Set, "LabSetting")]
    public class SetLabSetting : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateSet("TemplateDirectory", "VMRootFolder", "ScratchDirectory", "ComponentDirectory")]
        public string Setting { get; set; }

        [Parameter(Mandatory = true)]
        public string Value { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            if(Setting == "TemplateDirectory")
                env.TemplateDirectory = Value;

            if (Setting == "VMRootFolder")
                env.VMRootFolder = Value;

            if (Setting == "ScratchDirectory")
                env.ScratchDirectory = Value;

            if (Setting == "ComponentDirectory")
                env.ComponentPath = Value;

            env.PersistEnvironment();

            base.ProcessRecord();
        }
    }
}
