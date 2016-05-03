using System.Management.Automation;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Add, "LabIdempotentAction")]
    [Alias("IdempotentAction")]
    public class AddLabIdempotentAction: PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string Name { get; set; }
        [Parameter(Mandatory = false, Position = 2)]
        public string[] RequiredProperties { get; set; }
        [Parameter(Mandatory = false, Position = 3)]
        public string[] OptionalProperties { get; set; }
        [Parameter(Mandatory = true, Position = 4)]
        public ScriptBlock Test { get; set; }
        [Parameter(Mandatory = true, Position = 5)]
        public ScriptBlock Update { get; set; }

        protected override void ProcessRecord()
        {

            var action = new IdempotentAction(Name, Test, Update, RequiredProperties, OptionalProperties);
            var svc = ServiceDiscovery.GetInstance();
            var manager = svc.GetObject<IIdempotentActionManager>();

            manager.AddAction(action);

            base.ProcessRecord();
        }
    }
}
