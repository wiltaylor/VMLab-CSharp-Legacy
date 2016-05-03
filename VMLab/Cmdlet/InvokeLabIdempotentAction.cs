using System;
using System.Collections;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Web.Helpers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "LabIdempotentAction")]
    [Alias("CFG")]
    public class InvokeLabIdempotentAction : PSCmdlet
    {
        [Parameter(Mandatory = true, Position =  1)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public Hashtable Properties { get; set; }

        [Parameter]
        public SwitchParameter Test { get; set; }

        [Parameter]
        public SwitchParameter ThrowIfNotConfigured { get; set; }

        protected override void ProcessRecord()
        {
            Console.WriteLine($"Idempotent Action: {Name}");

            //Console.WriteLine($"Properties: {Json.Encode(Properties)}");

            var svc = ServiceDiscovery.GetInstance();
            var manager = svc.GetObject<IIdempotentActionManager>();

            var result = Test ? manager.TestAction(Name, Properties) : manager.UpdateAction(Name, Properties);
            
            Console.WriteLine($"Result: {result}");

            if (ThrowIfNotConfigured && (result != IdempotentActionResult.Ok && result != IdempotentActionResult.RebootRequired))
                throw new IdempotentActionNotConfigured($"Idepotent action is not configured! {Name} - {result}");

            WriteObject(result);

            base.ProcessRecord();
        }
    }
}
