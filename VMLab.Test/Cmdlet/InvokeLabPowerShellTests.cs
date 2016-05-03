using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Cmdlet;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;
using VMLab.Test.Model;

namespace VMLab.Test.Cmdlet
{
    [TestClass]
    public class InvokeLabPowerShellTests
    {
        public Mock<IDriver> Driver;
        public Mock<IServiceDiscovery> SVC;
        public Mock<IEnvironmentDetails> Environment;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Invoke-LabPowerShell", typeof(InvokeLabPowerShell), string.Empty));

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
            Command = new Command("Invoke-LabPowerShell");
            Pipe.Commands.Add(Command);

            Driver = new Mock<IDriver>();
            SVC = new Mock<IServiceDiscovery>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IDriver>()).Returns(Driver.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingCmdletWillReturnObjects()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Code", ScriptBlock.Create("#example script")));

            var results = new Mock<IPowershellCommandResult>();
            results.Setup(r => r.Results).Returns("ExampleData");

            Driver.Setup(d => d.ExecutePowershell("MyVM", It.IsAny<ScriptBlock>(), null, null, null)).Returns(results.Object);

            var returndata = Pipe.Invoke();

            Assert.IsTrue(returndata[0].BaseObject.ToString() == "ExampleData");
        }

        [TestMethod]
        public void CallingCmdletWithDataObjectWillReturnObjects()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Code", ScriptBlock.Create("#example script")));
            Command.Parameters.Add(new CommandParameter("DataObject", "My Test Object"));

            var results = new Mock<IPowershellCommandResult>();
            results.Setup(r => r.Results).Returns("ExampleData");

            Driver.Setup(d => d.ExecutePowershell("MyVM", It.IsAny<ScriptBlock>(), null, null, "My Test Object")).Returns(results.Object);

            var returndata = Pipe.Invoke();

            Assert.IsTrue(returndata[0].BaseObject.ToString() == "ExampleData");
        }

        [TestMethod]
        public void CallingCmdletWithUsernameAndPasswordWillReturnObjects()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Code", ScriptBlock.Create("#example script")));
            Command.Parameters.Add(new CommandParameter("Username", "ValidUserName"));
            Command.Parameters.Add(new CommandParameter("Password", "ValidPassword"));

            var results = new Mock<IPowershellCommandResult>();
            results.Setup(r => r.Results).Returns("ExampleData");

            Driver.Setup(d => d.ExecutePowershell("MyVM", It.IsAny<ScriptBlock>(), "ValidUserName", "ValidPassword", null)).Returns(results.Object);

            var returndata = Pipe.Invoke();

            Assert.IsTrue(returndata[0].BaseObject.ToString() == "ExampleData");
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Code", ScriptBlock.Create("#example script")));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
