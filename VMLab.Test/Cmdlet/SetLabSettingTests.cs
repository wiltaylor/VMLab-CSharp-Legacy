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
    public class SetLabSettingTests
    {
        public Mock<IEnvironmentDetails> Environment;
        public Mock<IServiceDiscovery> SVC;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Set-LabSetting", typeof(SetLabSetting), string.Empty));

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
            Command = new Command("Set-LabSetting");
            Pipe.Commands.Add(Command);

            Environment = new Mock<IEnvironmentDetails>();
            SVC = new Mock<IServiceDiscovery>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingCmdletWithTemplateDirectoryWillSetDirectoryInEnvironment()
        {
            Command.Parameters.Add(new CommandParameter("Setting", "TemplateDirectory"));
            Command.Parameters.Add(new CommandParameter("Value", "c:\\templates"));

            var result = Pipe.Invoke();

            Environment.VerifySet(e => e.TemplateDirectory = "c:\\templates");
        }

        [TestMethod]
        public void CallingCmdletWithVMRootFolderWillSetDirectoryInEnvironment()
        {
            Command.Parameters.Add(new CommandParameter("Setting", "VMRootFolder"));
            Command.Parameters.Add(new CommandParameter("Value", "_MYVM"));

            var result = Pipe.Invoke();

            Environment.VerifySet(e => e.VMRootFolder = "_MYVM");
        }

        [TestMethod]
        public void CallingCmdletWithScratchDirectoryWillSetDirectoryInEnvironment()
        {
            Command.Parameters.Add(new CommandParameter("Setting", "ScratchDirectory"));
            Command.Parameters.Add(new CommandParameter("Value", "c:\\scratch"));

            var result = Pipe.Invoke();

            Environment.VerifySet(e => e.ScratchDirectory = "c:\\scratch");
        }

        [TestMethod]
        public void CallingCmdletWithComponentDirectoryWillSetDirectoryInEnvironment()
        {
            Command.Parameters.Add(new CommandParameter("Setting", "ComponentDirectory"));
            Command.Parameters.Add(new CommandParameter("Value", "c:\\components"));

            var result = Pipe.Invoke();

            Environment.VerifySet(e => e.ComponentPath = "c:\\components");
        }

        [TestMethod]
        public void CallingCmdletWillCallEnvironmentToPersistData()
        {
            Command.Parameters.Add(new CommandParameter("Setting", "TemplateDirectory"));
            Command.Parameters.Add(new CommandParameter("Value", "c:\\templates"));

            var result = Pipe.Invoke();

            Environment.Verify(e => e.PersistEnvironment());
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("Setting", "TemplateDirectory"));
            Command.Parameters.Add(new CommandParameter("Value", "c:\\templates"));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }

    }
}
