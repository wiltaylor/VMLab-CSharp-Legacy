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
    public class InvokeLabActionTest
    {
        public Mock<IServiceDiscovery> SVC;
        public Mock<IFileSystem> FileSystem;
        public Mock<IEnvironmentDetails> Environment;
        public Mock<IScriptHelper> ScriptHelper;
        public Mock<IIdempotentActionManager> IdempotentManager;
        public Mock<ILabLibManager> LibManager;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Invoke-LabAction", typeof(InvokeLabAction), string.Empty));

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
            Command = new Command("Invoke-LabAction");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            FileSystem = new Mock<IFileSystem>();
            Environment = new Mock<IEnvironmentDetails>();
            ScriptHelper = new Mock<IScriptHelper>();
            IdempotentManager = new Mock<IIdempotentActionManager>();
            LibManager = new Mock<ILabLibManager>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IFileSystem>()).Returns(FileSystem.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
            SVC.Setup(s => s.GetObject<IScriptHelper>()).Returns(ScriptHelper.Object);
            SVC.Setup(s => s.GetObject<IIdempotentActionManager>()).Returns(IdempotentManager.Object);
            SVC.Setup(s => s.GetObject<ILabLibManager>()).Returns(LibManager.Object);

        }

        [TestMethod]
        [ExpectedException(typeof(MissingVMLabFileException))]
        public void CallingInvokeLabActionWillThrowIfVMLabFileDoesntExist()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(false);

            try
            {
                Pipe.Invoke();
            }catch (CmdletInvocationException e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        public void CallingInvokeLabActionWillLoadVMLabScript()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\lab\\VMLab.ps1")).Returns("#Example script text");

            Pipe.Invoke();

            FileSystem.Verify(f => f.ReadFile("c:\\lab\\VMLab.ps1"));
        }

        [TestMethod]
        public void CallingInvokeLabActionWillExecuteScript()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\lab\\VMLab.ps1")).Returns("#Example script text");

            Pipe.Invoke();

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()));
        }

        [TestMethod]
        public void CallingInvokeLabActionWillSetEnvironmentActionProperty()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\lab\\VMLab.ps1")).Returns("#Example script text");

            Pipe.Invoke();

            Environment.VerifySet(e => e.CurrentAction = "TestAction");
        }

        [TestMethod]
        public void CallingInvokeLabActionWillClearEnvironmentActionProperty()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\lab\\VMLab.ps1")).Returns("#Example script text");

            Pipe.Invoke();

            Environment.VerifySet(e => e.CurrentAction = null);
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\lab\\VMLab.ps1")).Returns("#Example script text");

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }

        [TestMethod]
        public void CallingCmdletWillClearIdempotentItems()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\lab\\VMLab.ps1")).Returns("#Example script text");

            Pipe.Invoke();

            IdempotentManager.Verify(i => i.ClearAction());
        }

        [TestMethod]
        public void callingCmdletWillClearVMLibsRegistration()
        {
            Command.Parameters.Add(new CommandParameter("Action", "TestAction"));

            Environment.Setup(e => e.WorkingDirectory).Returns("c:\\lab");
            FileSystem.Setup(f => f.FileExists("c:\\lab\\VMLab.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\lab\\VMLab.ps1")).Returns("#Example script text");

            Pipe.Invoke();

            LibManager.Verify(l => l.Reset());
        }
    }
}
