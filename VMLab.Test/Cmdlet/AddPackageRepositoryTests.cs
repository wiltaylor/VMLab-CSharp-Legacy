using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Cmdlet;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Test.Cmdlet
{
    [TestClass]
    public class AddPackageRepositoryTests
    {
        public Mock<IEnvironmentDetails> Environment;
        public Mock<IServiceDiscovery> SVC;
        public Mock<IPackageManager> Manager;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Add-LabPackageRepository", typeof(AddLabPackageRepository), string.Empty));

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
            Command = new Command("Add-LabPackageRepository");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            Manager = new Mock<IPackageManager>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IPackageManager>()).Returns(Manager.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingCmdletWillPassParametersOnToPackageManager()
        {
            Command.Parameters.Add(new CommandParameter("Name", "MyRepo"));
            Command.Parameters.Add(new CommandParameter("Path", "c:\\repo"));

            Pipe.Invoke();

            Manager.Verify(m => m.AddRepository("MyRepo", "c:\\repo"));
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("Name", "MyRepo"));
            Command.Parameters.Add(new CommandParameter("Path", "c:\\repo"));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
