using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "LabCommand")]
    public class InvokeLabCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "results")]
        public string[] Commands { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "noresults")]
        public string Path { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "noresults")]
        public string Args { get; set; }

        [Parameter(ParameterSetName = "noresults")]
        public SwitchParameter NoWait { get; set; }

        [Parameter(ParameterSetName = "noresults")]
        public SwitchParameter Interactive { get; set; }

        [Parameter(Mandatory = false)]
        public string Username { get; set; }

        [Parameter(Mandatory = false)]
        public string Password { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            if (!string.IsNullOrEmpty(Path))
            {
                driver.ExecuteCommand(VMName, Path, Args, NoWait, Interactive, Username, Password);
            }

            if (Commands != null && Commands.Length != 0)
            {
                var result = driver.ExecuteCommandWithResult(VMName, Commands, Username, Password);

                if(!string.IsNullOrEmpty(result.STDOut))
                    WriteObject(result.STDOut);
                if(!string.IsNullOrEmpty(result.STDError))
                    WriteWarning(result.STDError);
            }

            base.ProcessRecord();
        }
    }
}
