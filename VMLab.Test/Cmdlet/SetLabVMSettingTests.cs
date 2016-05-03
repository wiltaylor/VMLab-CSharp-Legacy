﻿using System.Management.Automation;
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
    public class SetLabVMSettingTests
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Set-LabVMSetting", typeof(SetLabVMSetting), string.Empty));

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
            Command = new Command("Set-LabVMSetting");
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
            Command.Parameters.Add(new CommandParameter("Name", "MySetting"));
            Command.Parameters.Add(new CommandParameter("Value", "MyValue"));

            Pipe.Invoke();
            
            Driver.Verify(d => d.WriteVMSetting("MyVM", "MySetting", "MyValue"));
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Command.Parameters.Add(new CommandParameter("VMName", "MyVM"));
            Command.Parameters.Add(new CommandParameter("Name", "MySetting"));
            Command.Parameters.Add(new CommandParameter("Value", "MyValue"));

            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
