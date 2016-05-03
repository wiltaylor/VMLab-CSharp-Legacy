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
    public class RemoveLabIdempotentActionTests
    {
        public Mock<IServiceDiscovery> SVC;
        public Mock<IIdempotentActionManager> Manager;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Remove-LabIdempotentAction", typeof(RemoveLabIdempotentAction), string.Empty));

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
            Command = new Command("Remove-LabIdempotentAction");
            Pipe.Commands.Add(Command);

            Manager = new Mock<IIdempotentActionManager>();
            SVC = new Mock<IServiceDiscovery>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IIdempotentActionManager>()).Returns(Manager.Object);
        }

        [TestMethod]
        public void CallingCmdletWillCallIdepodentManager()
        {
            Command.Parameters.Add(new CommandParameter("Name", "MyObject"));

            Pipe.Invoke();

            Manager.Verify(m => m.RemoveAction("MyObject"));
        }
    }
}
