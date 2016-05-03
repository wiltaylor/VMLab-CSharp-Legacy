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
    public class AddLabSharedFolderTests
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Add-LabSharedFolder", typeof(AddLabSharedFolder), string.Empty));

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
            Command = new Command("Add-LabSharedFolder");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            Driver = new Mock<IDriver>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IDriver>()).Returns(Driver.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingAddSharedFolderWillCallDriver()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("GuestPath", "c:\\guestfolder"));
            Command.Parameters.Add(new CommandParameter("HostPath", "c:\\hostfolder"));
            Command.Parameters.Add(new CommandParameter("ShareName", "myshare"));

            Pipe.Invoke();

            Driver.Verify(d => d.AddSharedFolder("MyVM", "c:\\hostfolder", "myshare", "c:\\guestfolder"));
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("GuestPath", "c:\\guestfolder"));
            Command.Parameters.Add(new CommandParameter("HostPath", "c:\\hostfolder"));
            Command.Parameters.Add(new CommandParameter("ShareName", "myshare"));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
