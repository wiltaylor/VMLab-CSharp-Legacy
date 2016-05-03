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
    public class ImportLabLibraryTests
    {
        public Mock<IServiceDiscovery> SVC;
        public Mock<IEnvironmentDetails> Environment;
        public Mock<ILabLibManager> Manager;
        public Mock<IFileSystem> FileSystem;
        public Mock<IScriptHelper> ScriptHelper;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Import-LabLibrary", typeof(ImportLabLibrary), string.Empty));

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
            Command = new Command("Import-LabLibrary");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            Environment = new Mock<IEnvironmentDetails>();
            Manager = new Mock<ILabLibManager>();
            FileSystem = new Mock<IFileSystem>();
            ScriptHelper = new Mock<IScriptHelper>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
            SVC.Setup(s => s.GetObject<ILabLibManager>()).Returns(Manager.Object);
            SVC.Setup(s => s.GetObject<IFileSystem>()).Returns(FileSystem.Object);
            SVC.Setup(s => s.GetObject<IScriptHelper>()).Returns(ScriptHelper.Object);
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentWhenCalled()
        {
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.ReadFile("c:\\components\\lib_ExistingLabLib.ps1")).Returns("#script");
            FileSystem.Setup(f => f.FileExists("c:\\components\\lib_ExistingLabLib.ps1")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "ExistingLabLib"));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }

        [TestMethod]
        [ExpectedException(typeof(NonExistingLabLibraryException))]
        public void CallingCmdletWillThrowIfLibraryDoesntExist()
        {
            Command.Parameters.Add(new CommandParameter("Name", "NonExistingLabLib"));

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
        public void CallingCmdletWillTestIfLibraryIsAlreadyImported()
        {
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.ReadFile("c:\\components\\lib_ExistingLabLib.ps1")).Returns("#script");
            FileSystem.Setup(f => f.FileExists("c:\\components\\lib_ExistingLabLib.ps1")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "ExistingLabLib"));

            Pipe.Invoke();

            Manager.Verify(m => m.TestLib("ExistingLabLib"));
        }

        [TestMethod]
        public void CallingCmdletWillCallImportOnManagerIfItWasntPreviouslySet()
        {
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.ReadFile("c:\\components\\lib_ExistingLabLib.ps1")).Returns("#script");
            FileSystem.Setup(f => f.FileExists("c:\\components\\lib_ExistingLabLib.ps1")).Returns(true);
            Manager.Setup(m => m.TestLib("ExistingLabLib")).Returns(false);
            Command.Parameters.Add(new CommandParameter("Name", "ExistingLabLib"));

            Pipe.Invoke();

            Manager.Verify(m => m.ImportLib("ExistingLabLib"));
        }

        [TestMethod]
        public void CallingCmdletWillSkipImportingIfLibraryIsAlreadyImported()
        {
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.ReadFile("c:\\components\\lib_ExistingLabLib.ps1")).Returns("#script");
            FileSystem.Setup(f => f.FileExists("c:\\components\\lib_ExistingLabLib.ps1")).Returns(true);
            Manager.Setup(m => m.TestLib("ExistingLabLib")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "ExistingLabLib"));

            Pipe.Invoke();

            Manager.Verify(m => m.ImportLib("ExistingLabLib"), Times.Never);
        }

        [TestMethod]
        public void CallingCmdletWillExecuteScriptIfLibraryWasntPreviouslySet()
        {
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.FileExists("c:\\components\\lib_ExistingLabLib.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\components\\lib_ExistingLabLib.ps1")).Returns("#script");
            Manager.Setup(m => m.TestLib("ExistingLabLib")).Returns(false);
            Command.Parameters.Add(new CommandParameter("Name", "ExistingLabLib"));

            Pipe.Invoke();

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()));
        }

        [TestMethod]
        public void CallingCdmletWillNotExecuteScriptIfLibraryWasPreviouslySet()
        {
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.FileExists("c:\\components\\lib_ExistingLabLib.ps1")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\components\\lib_ExistingLabLib.ps1")).Returns("#script");
            Manager.Setup(m => m.TestLib("ExistingLabLib")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "ExistingLabLib"));

            Pipe.Invoke();

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()), Times.Never);
        }
    }
}
