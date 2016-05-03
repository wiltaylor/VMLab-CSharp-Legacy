using System;
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
    public class RegisterLabVMTests
    {

        public Mock<IServiceDiscovery> SvcDiscovery;
        public Mock<IFileSystem> FSystem;
        public Mock<IVMNodeHandler> NodeHandler;
        public Mock<IEnvironmentDetails> Environment;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Register-LabVM", typeof(RegisterLabVM), string.Empty));

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
            Command = new Command("Register-LabVM");
            Pipe.Commands.Add(Command);

            var state = InitialSessionState.CreateDefault();
            var asspath = (typeof (RegisterLabVM).Assembly.CodeBase).Replace("file://", "").Replace("/", "\\").Remove(0,1);
            state.ImportPSModule(new [] { asspath });

            SvcDiscovery = new Mock<IServiceDiscovery>();
            FSystem = new Mock<IFileSystem>();
            NodeHandler = new Mock<IVMNodeHandler>();
            Environment = new Mock<IEnvironmentDetails>();

            NodeHandler.Setup(h => h.Name).Returns("MySetting");

            Environment.Setup(e => e.CurrentAction).Returns("TestAction");

            SvcDiscovery.Setup(s => s.GetAllObject<IVMNodeHandler>()).Returns(new[] {NodeHandler.Object});

            ServiceDiscovery.UnitTestInject(SvcDiscovery.Object);
            SvcDiscovery.Setup(s => s.GetObject<IFileSystem>()).Returns(FSystem.Object);
            SvcDiscovery.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);

        }

        [TestMethod]
        public void CallingRegisterLabVMDoesntThrowWithEmptySettings()
        {
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable()));

            Pipe.Invoke();
        }

        [TestMethod]
        public void CallingRegisterLabVMWillPullListOfNodeHandlers()
        {
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            Pipe.Invoke();

            SvcDiscovery.Verify(s => s.GetAllObject<IVMNodeHandler>());
        }

        [TestMethod]
        [ExpectedException(typeof(NodeHandlerDoesntExistException))]
        public void CallingRegisterLabVMWillThrowIfHandlerCantBeFound()
        {
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "OtherHandlerThatDoesntExist", "SettingValue" } }));

            try
            {
                Pipe.Invoke();
            }
            catch (CmdletInvocationException e)
            {
                throw e.InnerException;
            }
            
        }

        [TestMethod]
        public void CallingRegisterLabVMWillCallPreProcess()
        {
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            Pipe.Invoke();

            NodeHandler.Verify(n => n.PreProcess("TestVM", "SettingValue", "TestAction"));
        }

        [TestMethod]
        public void CallingRegisterLabVMWillCallProcess()
        {
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            Pipe.Invoke();

            NodeHandler.Verify(n => n.Process("TestVM", "SettingValue", "TestAction"));
        }

        [TestMethod]
        public void CallingRegisterLabVMWillCallPostProcess()
        {
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            Pipe.Invoke();

            NodeHandler.Verify(n => n.PostProcess("TestVM", "SettingValue", "TestAction"));
        }

        [TestMethod]
        public void CallingRegisterLabVMWillCallHanldersInOrderOfPriority()
        {
            var handler1 = new Mock<IVMNodeHandler>();
            var handler2 = new Mock<IVMNodeHandler>();

            handler1.Setup(h => h.Name).Returns("handler1");
            handler2.Setup(h => h.Name).Returns("handler2");

            handler1.Setup (h => h.Priority).Returns(5);
            handler2.Setup(h => h.Priority).Returns(1);

            SvcDiscovery.Setup(s => s.GetAllObject<IVMNodeHandler>()).Returns(new[] { handler1.Object, handler2.Object});


            var seq = new MockSequence();
            handler2.InSequence(seq).Setup(h => h.PreProcess("TestVM", "SettingValue2", "TestAction"));
            handler1.InSequence(seq).Setup(h => h.PreProcess("TestVM", "SettingValue", "TestAction"));

            handler2.InSequence(seq).Setup(h => h.Process("TestVM", "SettingValue2", "TestAction"));
            handler1.InSequence(seq).Setup(h => h.Process("TestVM", "SettingValue", "TestAction"));

            handler2.InSequence(seq).Setup(h => h.PostProcess("TestVM", "SettingValue2", "TestAction"));
            handler1.InSequence(seq).Setup(h => h.PostProcess("TestVM", "SettingValue", "TestAction"));

            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "handler1", "SettingValue" }, { "handler2", "SettingValue2" } }));

            Pipe.Invoke();

            handler1.VerifyAll();
            handler2.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(NullActionException))]
        public void CallingRegisterLabVMWillThrowIfCurrentActionIsNull()
        {
            Environment.Setup(e => e.CurrentAction);
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            try
            {
                Pipe.Invoke();
            }
            catch (CmdletInvocationException e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        public void CallingRegisterLabVMWillPassActionToNodeHandler()
        {
            Environment.Setup(e => e.CurrentAction).Returns("MyAction");

            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            Pipe.Invoke();

            NodeHandler.Verify(n => n.PostProcess("TestVM", "SettingValue", "MyAction"));
        }

        [TestMethod]
        public void CallingRegisterLabWillCallActionHandlersIfTheyExist()
        {
            Environment.Setup(e => e.CurrentAction).Returns("MyAction");
            var ah = new Mock<IVMActionHandler>();
            ah.Setup(a => a.Name).Returns("MyAction");

            SvcDiscovery.Setup(s => s.GetAllObject<IVMActionHandler>()).Returns(new[] {ah.Object});

            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            Pipe.Invoke();

            ah.Verify(a => a.Process("TestVM"));
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("Name", "TestVM"));
            Command.Parameters.Add(new CommandParameter("Settings", new Hashtable { { "MySetting", "SettingValue" } }));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
