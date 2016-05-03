using System;
using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "LabAction")]
    [Alias("lab")]
    public class InvokeLabAction : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string Action { get; set; }

        protected override void BeginProcessing()
        {
            var svc = ServiceDiscovery.GetInstance();
            var filesystem = svc.GetObject<IFileSystem>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            if (!filesystem.FileExists($"{env.WorkingDirectory}\\VMLab.ps1"))
                throw new MissingVMLabFileException("Can't start action when there isn't a vmlab file in the current directory.");
            if (string.IsNullOrEmpty(Action))
                throw new NullActionException();
            
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var filesystem = svc.GetObject<IFileSystem>();
            var env = svc.GetObject<IEnvironmentDetails>();
            var scripthelper = svc.GetObject<IScriptHelper>();
            var idempotentMan = svc.GetObject<IIdempotentActionManager>();
            var libman = svc.GetObject<ILabLibManager>();

            var script = ScriptBlock.Create(filesystem.ReadFile($"{env.WorkingDirectory}\\VMLab.ps1"));
            idempotentMan.ClearAction();
            libman.Reset();

            env.CurrentAction = Action;

            Console.WriteLine($"Starting action {Action}");

            scripthelper.Invoke(script);

            env.CurrentAction = null;

            base.ProcessRecord();
        }
    }
}
