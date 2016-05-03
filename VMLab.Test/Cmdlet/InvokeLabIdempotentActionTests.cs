using System.Collections;
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
    public class InvokeLabIdempotentActionTests
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Invoke-LabIdempotentAction", typeof(InvokeLabIdempotentAction), string.Empty));

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
            Command = new Command("Invoke-LabIdempotentAction");
            Pipe.Commands.Add(Command);

            Manager = new Mock<IIdempotentActionManager>();
            SVC = new Mock<IServiceDiscovery>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IIdempotentActionManager>()).Returns(Manager.Object);
        }

        [TestMethod]
        public void RunningCmdletWillCallManagerToInvokeAction()
        {
            Command.Parameters.Add(new CommandParameter("Name", "MyObject"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable()));

            Pipe.Invoke();

            Manager.Verify(m => m.UpdateAction("MyObject", It.IsAny<Hashtable>()));
        }

        [TestMethod]
        public void RunningCmdletWillCallManagerToTestActionOnlyIfTestSwitchIsSet()
        {
            Command.Parameters.Add(new CommandParameter("Name", "MyObject"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable()));
            Command.Parameters.Add("Test");

            Pipe.Invoke();

            Manager.Verify(m => m.UpdateAction("MyObject", It.IsAny<Hashtable>()), Times.Never);
            Manager.Verify(m => m.TestAction("MyObject", It.IsAny<Hashtable>()));
        }

        [TestMethod]
        [ExpectedException(typeof(IdempotentActionNotConfigured))]
        public void RunningCmdletWillThrowIfThrowOnNotConfiguredSwitchIsPassed()
        {
            Command.Parameters.Add(new CommandParameter("Name", "MyObject"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable()));
            Command.Parameters.Add("ThrowIfNotConfigured");

            Manager.Setup(m => m.UpdateAction("MyObject", It.IsAny<Hashtable>()))
                .Returns(IdempotentActionResult.NotConfigured);

            try
            {
                Pipe.Invoke();
            }
            catch (CmdletInvocationException e)
            {
                throw e.InnerException;
            }
            
        }
    }
}
