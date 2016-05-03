using System.Linq;
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
    public class GetLabSharedFolderTests
    {
        public Mock<IEnvironmentDetails> Environment;
        public Mock<IServiceDiscovery> SVC;
        public Mock<IDriver> Driver;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Get-LabSharedFolder", typeof(GetLabSharedFolder), string.Empty));

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
            Command = new Command("Get-LabSharedFolder");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            Driver = new Mock<IDriver>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IDriver>()).Returns(Driver.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingCmdletWillReturnShareFolders()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));

            var result = new Mock<IShareFolderDetails>();
            result.Setup(r => r.Name).Returns("myshare");
            result.Setup(r => r.GuestPath).Returns("c:\\guestpath");
            result.Setup(r => r.HostPath).Returns("c:\\hostpath");

            Driver.Setup(d => d.GetSharedFolders("MyVM")).Returns(new[] {result.Object});

            Assert.IsTrue(Pipe.Invoke().Any(o =>
                o.Properties["Name"].Value.ToString() == "myshare" &&
                o.Properties["GuestPath"].Value.ToString() == "c:\\guestpath" &&
                o.Properties["HostPath"].Value.ToString() == "c:\\hostpath"
                ));
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
