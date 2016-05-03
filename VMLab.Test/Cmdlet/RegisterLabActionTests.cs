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
    public class RegisterLabActionTests
    {
        public Mock<IScriptHelper> ScriptHelper;
        public Mock<IServiceDiscovery> SVC;
        public Mock<IEnvironmentDetails> Environment;

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Register-LabAction", typeof(RegisterLabAction), string.Empty));

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
            Command = new Command("Register-LabAction");
            Pipe.Commands.Add(Command);

            SVC = new Mock<IServiceDiscovery>();
            ScriptHelper = new Mock<IScriptHelper>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);
            SVC.Setup(s => s.GetObject<IScriptHelper>()).Returns(ScriptHelper.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);

            Environment.Setup(e => e.CurrentAction).Returns("start");

        }

        [TestMethod]
        public void CallingCmdletWillExecuteCode()
        {
            Command.Parameters.Add(new CommandParameter("Name", "start"));
            Command.Parameters.Add(new CommandParameter("Code", ScriptBlock.Create("#empty code block")));

            Pipe.Invoke();

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()));
        }

        [TestMethod]
        public void CallingCmdletWillNoExecuteCodeIfItDoesntTargetCurrentAction()
        {
            Environment.Setup(e => e.CurrentAction).Returns("notstart");

            Command.Parameters.Add(new CommandParameter("Name", "start"));
            Command.Parameters.Add(new CommandParameter("Code", ScriptBlock.Create("#empty code block")));

            Pipe.Invoke();

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()), Times.Never);
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("Name", "start"));
            Command.Parameters.Add(new CommandParameter("Code", ScriptBlock.Create("#empty code block")));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
