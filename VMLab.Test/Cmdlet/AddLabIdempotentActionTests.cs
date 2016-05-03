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
    public class AddLabIdempotentActionTests
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Add-LabIdempotentAction", typeof(AddLabIdempotentAction), string.Empty));

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
            Command = new Command("Add-LabIdempotentAction");
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
            Command.Parameters.Add(new CommandParameter("RequiredProperties", new string[]{ "test" }));
            Command.Parameters.Add(new CommandParameter("OptionalProperties", new string[]{ "test 2"}));
            Command.Parameters.Add(new CommandParameter("Test", ScriptBlock.Create("")));
            Command.Parameters.Add(new CommandParameter("Update", ScriptBlock.Create("")));

            Pipe.Invoke();

            Manager.Verify(m => m.AddAction(It.IsAny<IIdempotentAction>()));
        }

        [TestMethod]
        public void CallingCmdletWillCallIdepodentManagerWithoutProperties()
        {
            Command.Parameters.Add(new CommandParameter("Name", "MyObject"));
            Command.Parameters.Add(new CommandParameter("Test", ScriptBlock.Create("")));
            Command.Parameters.Add(new CommandParameter("Update", ScriptBlock.Create("")));

            Pipe.Invoke();

            Manager.Verify(m => m.AddAction(It.IsAny<IIdempotentAction>()));
        }

    }
}
