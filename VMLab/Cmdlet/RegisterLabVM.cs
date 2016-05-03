using System.Collections;
using System.Linq;
using System.Management.Automation;
using Microsoft.Practices.ObjectBuilder2;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Register, "LabVM")]
    [Alias("VM")]
    public class RegisterLabVM : PSCmdlet
    {

        [Parameter(Mandatory = true, Position = 1)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public Hashtable Settings { get; set; }

        protected override void BeginProcessing()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            env.UpdateEnvironment(this);

            if (env.CurrentAction == null)
                throw new NullActionException();
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var nodehandlers = svc.GetAllObject<IVMNodeHandler>().ToArray().OrderBy(n => n.Priority);
            var actionhandlers = svc.GetAllObject<IVMActionHandler>().ToArray();

            foreach (var k in Settings.Keys.Cast<string>().Where(k => nodehandlers.All(n => n.Name != k)))
                throw new NodeHandlerDoesntExistException($"Can't set node with setting because no handler exists for it! ({k})", k);

            foreach (var a in actionhandlers.Where(a => a.Name == env.CurrentAction))
            {
                a.Process(Name);
            }

            foreach (var n in nodehandlers.Where(n => Settings.ContainsKey(n.Name)))
                n.PreProcess(Name, Settings[n.Name], env.CurrentAction);

            foreach (var n in nodehandlers.Where(n => Settings.ContainsKey(n.Name)))
                n.Process(Name, Settings[n.Name], env.CurrentAction);

            foreach (var n in nodehandlers.Where(n => Settings.ContainsKey(n.Name)))
                n.PostProcess(Name, Settings[n.Name], env.CurrentAction);         

            base.ProcessRecord();
        }
    }
}
