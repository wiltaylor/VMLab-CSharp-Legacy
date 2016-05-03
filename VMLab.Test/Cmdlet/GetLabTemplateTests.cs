using System.Collections.Generic;
using System.Dynamic;
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
    public class GetLabTemplateTests
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

            Config.Cmdlets.Append(new CmdletConfigurationEntry("Get-LabTemplate", typeof(GetLabTemplate), string.Empty));

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
            Command = new Command("Get-LabTemplate");
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

            var result = new Dictionary<string, object> {{"OS", "Windows"}};


            Driver.Setup(d => d.GetTemplates()).Returns(new IDictionary<string, object>[] {result});

            var anyreturned = false;

            foreach (dynamic item in Pipe.Invoke())
            {
                anyreturned = true;

                Assert.IsTrue(item.OS == "Windows");
            }

            Assert.IsTrue(anyreturned);
        }

        [TestMethod]
        public void CallingCmdletWillUpdateEnvironmentState()
        {
            Pipe.Invoke();

            Environment.Verify(e => e.UpdateEnvironment(It.IsAny<PSCmdlet>()));
        }
    }
}
