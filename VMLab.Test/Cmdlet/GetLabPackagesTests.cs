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
    public class GetLabPackagesTests
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Get-LabPackages", typeof(GetLabPackages), string.Empty));

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
            Command = new Command("Get-LabPackages");
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
            Pipe.Invoke();

            Manager.Verify(m => m.GetPackages());
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
