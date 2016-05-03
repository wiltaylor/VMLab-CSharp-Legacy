using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsData.Import, "LabLibrary")]
    [Alias("LabLib")]
    public class ImportLabLibrary : PSCmdlet
    {
        [Parameter(Mandatory =  true, Position =  1)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var filesystem = svc.GetObject<IFileSystem>();
            var manager = svc.GetObject<ILabLibManager>();
            var script = svc.GetObject<IScriptHelper>();

            env.UpdateEnvironment(this);

            if(!filesystem.FileExists($"{env.ComponentPath}\\lib_{Name}.ps1"))
                throw new NonExistingLabLibraryException($"Can't find library named {Name}");

            if (!manager.TestLib(Name))
            {
                manager.ImportLib(Name);
                script.Invoke(ScriptBlock.Create(filesystem.ReadFile($"{env.ComponentPath}\\lib_{Name}.ps1")));
            }

            base.ProcessRecord();
        }
    }
}
