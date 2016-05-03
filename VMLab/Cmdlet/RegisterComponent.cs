using System.Collections;
using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Register, "LabComponent")]
    [Alias("Component")]
    public class RegisterComponent : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public Hashtable Properties { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var filesystem = svc.GetObject<IFileSystem>();
            var componentpath = env.ComponentPath;
            var scripthelper = svc.GetObject<IScriptHelper>();
            
            env.UpdateEnvironment(this);

            if(!filesystem.FileExists($"{componentpath}\\{Name}.ps1"))
                throw new ComponentDoesntExist($"Can't find component {Name}", Name);

            var script = ScriptBlock.Create(filesystem.ReadFile($"{componentpath}\\{Name}.ps1"));

            scripthelper.Invoke(script, Properties);

            base.ProcessRecord();
        }
    }
}
