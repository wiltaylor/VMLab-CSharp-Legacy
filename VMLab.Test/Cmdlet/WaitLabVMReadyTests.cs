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
    public class WaitLabVMReadyTests
    {
        public Mock<IDriver> Driver;
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Wait-LabVM", typeof(WaitLabVMReady), string.Empty));

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
            Command = new Command("Wait-LabVM");
            Pipe.Commands.Add(Command);


            Driver = new Mock<IDriver>();
            SVC = new Mock<IServiceDiscovery>();
            Environment = new Mock<IEnvironmentDetails>();

            ServiceDiscovery.UnitTestInject(SVC.Object);

            SVC.Setup(s => s.GetObject<IDriver>()).Returns(Driver.Object);
            SVC.Setup(s => s.GetObject<IEnvironmentDetails>()).Returns(Environment.Object);
        }

        [TestMethod]
        public void CallingCmdletWillCallDriver()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Environment.Setup(e => e.SleepTimeOut).Returns(0);

            Pipe.Invoke();

            Driver.Verify(d => d.WaitVMReady("MyVM"));
        }

        [TestMethod]
        public void CallingCmdletWithShutdownSwitchWillWaitForShutdownInstead()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add("Shutdown");
            var seq = new MockSequence();
            Driver.InSequence(seq).Setup(d => d.GetVMState("MyVM")).Returns(VMState.Other);
            Driver.InSequence(seq).Setup(d => d.GetVMState("MyVM")).Returns(VMState.Ready);
            Driver.InSequence(seq).Setup(d => d.GetVMState("MyVM")).Returns(VMState.Shutdown);
            Environment.Setup(e => e.SleepTimeOut).Returns(0);

            Pipe.Invoke();

            Driver.Verify(d => d.GetVMState("MyVM"), Times.Exactly(3));
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

