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
    public class RegisterComponentTests
    {
        public Mock<IServiceDiscovery> SVC;
        public Mock<IEnvironmentDetails> Environment;
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Register-LabComponent", typeof(RegisterComponent), string.Empty));

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
            Command = new Command("Register-LabComponent");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            Environment = new Mock<IEnvironmentDetails>();
            FileSystem = new Mock<IFileSystem>();
            ScriptHelper = new Mock<IScriptHelper>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
            SVC.Setup(s => s.GetObject<IFileSystem>()).Returns(FileSystem.Object);
            SVC.Setup(s => s.GetObject<IScriptHelper>()).Returns(ScriptHelper.Object);
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\components\\MyComponent.ps1")).Returns("#Sample Script content");
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.FileExists("c:\\components\\MyComponent.ps1")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "MyComponent"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable {}));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }

        [TestMethod]
        public void CallingCmdletWillAccessEnvironmentToRetriveComponentPath()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\components\\MyComponent.ps1")).Returns("#Sample Script content");
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.FileExists("c:\\components\\MyComponent.ps1")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "MyComponent"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable { }));

            Pipe.Invoke();

            Environment.VerifyGet(e => e.ComponentPath);
        }

        [TestMethod]
        [ExpectedException(typeof (ComponentDoesntExist))]
        public void CallingCmdletWillCheckIfComponentFileExistsInComponentDirectoryAndThrowIfItDoesnt()
        {
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.FileExists("c:\\components\\MyComponent.ps1")).Returns(false);

            Command.Parameters.Add(new CommandParameter("Name", "MyComponent"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable {}));

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
        public void CallingCmdletWillReadContentsOfScriptFile()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\components\\MyComponent.ps1")).Returns("#Sample Script content");
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.FileExists("c:\\components\\MyComponent.ps1")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "MyComponent"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable { }));

            Pipe.Invoke();

            FileSystem.Verify(f => f.ReadFile("c:\\components\\MyComponent.ps1"));
        }

        [TestMethod]
        public void CallingCmdletWillExecuteScriptContent()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\components\\MyComponent.ps1")).Returns("#Sample Script content");
            Environment.Setup(e => e.ComponentPath).Returns("c:\\components");
            FileSystem.Setup(f => f.FileExists("c:\\components\\MyComponent.ps1")).Returns(true);
            Command.Parameters.Add(new CommandParameter("Name", "MyComponent"));
            Command.Parameters.Add(new CommandParameter("Properties", new Hashtable { }));

            Pipe.Invoke();

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>(), It.IsAny<Hashtable>()));
        }
    }
}
