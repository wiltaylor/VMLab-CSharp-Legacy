using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Cmdlet;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Test.Cmdlet
{
    [TestClass]
    public class StopLabVMTests
    {
        public Mock<IServiceDiscovery> SVC;
        public Mock<IDriver> Driver;
        public Mock<IEnvironmentDetails> Environment;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Stop-LabVM", typeof(StopLabVM), string.Empty));

            Runspace = RunspaceFactory.CreateRunspace(Config);
            Runspace.Open();
        }

        [ClassCleanup]
        public static void FixtureTearDown()
        {
            Runspace.Close();
        }

        [TestInitialize]
        public void Setup()
        {
            Pipe = Runspace.CreatePipeline();
            Command = new Command("Stop-LabVM");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            Driver = new Mock<IDriver>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IDriver>()).Returns(Driver.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingStopLabWillCallDriverToStopVM()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));

            Pipe.Invoke();
            
            Driver.Verify(d => d.StopVM("MyVM", false));
        }

        [TestMethod]
        public void CallingStopLabWithForceWillCallDriverWithForce()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Force", SwitchParameter.Present));

            Pipe.Invoke();

            Driver.Verify(d => d.StopVM("MyVM", true));
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
