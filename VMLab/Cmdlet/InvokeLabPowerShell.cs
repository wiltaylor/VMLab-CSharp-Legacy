using System.Linq;
using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "LabPowerShell")]
    public class InvokeLabPowerShell: PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string VMName { get; set; }

        [Parameter(Mandatory = true, Position =  2)]
        public ScriptBlock Code { get; set; }

        [Parameter(Position =  3)]
        public object DataObject { get; set; }

        [Parameter]
        public string Username { get; set; }

        [Parameter]
        public string Password { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            var results = driver.ExecutePowershell(VMName, Code, Username, Password, DataObject);

            if (results != null)
            {

                if(results.Results != null)
                    WriteObject(results.Results);

                if (results.Errors != null && results.Errors.GetType().IsArray)
                {
                    foreach (var record in ((object[])results.Errors).Cast<PSObject>())
                    {
                        if (record.TypeNames.Any(n => n.Contains("ErrorRecord")))
                        {
                            var remoteexcept = (PSObject)record.Properties["Exception"].Value;

                            var except = new RuntimeException(remoteexcept.Properties["Message"].Value.ToString());

                            var errorID = record.Properties["FullyQualifiedErrorId"].Value.ToString();

                            var newrec = new ErrorRecord(except, errorID, ErrorCategory.ConnectionError, record.Properties["TargetObject"].Value);

                            WriteError(newrec);
                        }
                            
                    }
                }
            }
                

            base.ProcessRecord();
        }
    }
}
