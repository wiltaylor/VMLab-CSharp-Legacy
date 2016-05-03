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
    public class InvokeLabCommandTests
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Invoke-LabCommand", typeof(InvokeLabCommand), string.Empty));

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
            Command = new Command("Invoke-LabCommand");
            Pipe.Commands.Add(Command);

            Driver = new Mock<IDriver>();
            SVC = new Mock<IServiceDiscovery>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IDriver>()).Returns(Driver.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingCmdletWithPathAndArgumentsCallsThem()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Path", "c:\\test.exe"));
            Command.Parameters.Add(new CommandParameter("Args", "-test"));

            Pipe.Invoke();

            Driver.Verify(d => d.ExecuteCommand("MyVM", "c:\\test.exe", "-test", false, false, null, null));
        }

        [TestMethod]
        public void CallingCmdletWithNoWaitWillCallDriverwithNoWait()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Path", "c:\\test.exe"));
            Command.Parameters.Add(new CommandParameter("Args", "-test"));
            Command.Parameters.Add(new CommandParameter("NoWait", SwitchParameter.Present));

            Pipe.Invoke();

            Driver.Verify(d => d.ExecuteCommand("MyVM", "c:\\test.exe", "-test", true, false, null, null));
        }

        [TestMethod]
        public void CallingCmdletWithInteractiveWillCallDriverWithInteractive()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Path", "c:\\test.exe"));
            Command.Parameters.Add(new CommandParameter("Args", "-test"));
            Command.Parameters.Add(new CommandParameter("Interactive", SwitchParameter.Present));

            Pipe.Invoke();

            Driver.Verify(d => d.ExecuteCommand("MyVM", "c:\\test.exe", "-test", false, true, null, null));
        }

        [TestMethod]
        public void CallingCmdletWithCommandsItWillReturnResults()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Commands", new[] { "Command 1", "Command 2" }));

            var result = new Mock<ICommandResult>();
            result.Setup(r => r.STDOut).Returns("Test results");

            Driver.Setup(d => d.ExecuteCommandWithResult("MyVM", It.IsAny<string[]>(), null, null)).Returns(result.Object);

            Assert.IsTrue(Pipe.Invoke().Any(c => c.ToString() == "Test results"));
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Path", "c:\\test.exe"));
            Command.Parameters.Add(new CommandParameter("Args", "-test"));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }

        [TestMethod]
        public void CallingCmdletWithPathAndArgsAndCredentialsWillPassCredentialsToDriver()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Path", "c:\\test.exe"));
            Command.Parameters.Add(new CommandParameter("Args", "-test"));
            Command.Parameters.Add(new CommandParameter("Username", "ValidUsername"));
            Command.Parameters.Add(new CommandParameter("Password", "ValidPassword"));
            
            Pipe.Invoke();

            Driver.Verify(d => d.ExecuteCommand("MyVM", "c:\\test.exe", "-test", false, false, "ValidUsername", "ValidPassword"));
        }

        [TestMethod]
        public void CallingCmdletWithCommandsAndCredentialsWillPassCredentialsToDriver()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Commands", new[] { "Command 1", "Command 2" }));
            Command.Parameters.Add(new CommandParameter("Username", "ValidUsername"));
            Command.Parameters.Add(new CommandParameter("Password", "ValidPassword"));

            var result = new Mock<ICommandResult>();
            result.Setup(r => r.STDOut).Returns("Test results");

            Driver.Setup(d => d.ExecuteCommandWithResult("MyVM", It.IsAny<string[]>(), "ValidUsername", "ValidPassword")).Returns(result.Object);

            Pipe.Invoke();

            Driver.Verify(d => d.ExecuteCommandWithResult("MyVM", It.IsAny<string[]>(), "ValidUsername", "ValidPassword"));
        }
    }
}
