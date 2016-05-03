using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Remove, "LabIdempotentAction")]
    public class RemoveLabIdempotentAction : PSCmdlet
    {
        [Parameter(Mandatory = true, Position =  1)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var manager = svc.GetObject<IIdempotentActionManager>();

            manager.RemoveAction(Name);

            base.ProcessRecord();
        }
    }
}
