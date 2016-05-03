using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VMLab.Cmdlet;
using VMLab.Model;

namespace VMLab.Test.Model
{
    [TestClass]
    public class IdempotentActionTests
    {

        public static Runspace Runspace;
        public static RunspaceConfiguration Config;
        public Pipeline Pipe;
        public Command Command;

        [ClassInitialize]
        public static void FixtureSetup(TestContext context)
        {
            Config = RunspaceConfiguration.Create();
            Runspace = RunspaceFactory.CreateRunspace(Config);
            Runspace.Open();

            Runspace.DefaultRunspace = Runspace;
        }

        [ClassCleanup]
        public static void FixtureTearDown()
        {
            Runspace.Close();
            Runspace.DefaultRunspace = null;
        }


        [TestMethod]
        public void RunningTestActionWillReturnOkIfTestReturnsTrue()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create("$true"), ScriptBlock.Create(""), new string[]{ }, new string[]{ });

            Assert.IsTrue(action.TestAction(new Hashtable()) == IdempotentActionResult.Ok);
        }

        [TestMethod]
        public void RunningTestActionWillReturnRebootRequiredIfTestReturnsRebootString()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create("'reboot'"), ScriptBlock.Create(""), new string[] { }, new string[] { });

            Assert.IsTrue(action.TestAction(new Hashtable()) == IdempotentActionResult.RebootRequired);
        }

        [TestMethod]
        public void RunningTestActionWillReturnUnknownIfNothingIsPipedOut()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create(""), ScriptBlock.Create(""), new string[] { }, new string[] { });

            Assert.IsTrue(action.TestAction(new Hashtable()) == IdempotentActionResult.NotConfigured);
        }

        [TestMethod]
        public void RunningTestActionWillReturnFailedIfStringThatIsntRebootIsPipedOut()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create("'SomeErrorMessage'"), ScriptBlock.Create(""), new string[] { }, new string[] { });

            Assert.IsTrue(action.TestAction(new Hashtable()) == IdempotentActionResult.Failed);
        }

        [TestMethod]
        public void RunningTestActionWillReturnNotConfiguredIfUpdateReturnsFalse()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create(""), ScriptBlock.Create("$false"), new string[] { }, new string[] { });

            Assert.IsTrue(action.TestAction(new Hashtable()) == IdempotentActionResult.NotConfigured);
        }


        [TestMethod]
        public void RunningUpdateActionWillReturnOkIfTestReturnsTrue()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create(""), ScriptBlock.Create("$true"), new string[] { }, new string[] { });

            Assert.IsTrue(action.UpdateAction(new Hashtable()) == IdempotentActionResult.Ok);
        }

        [TestMethod]
        public void RunningUpdateActionWillReturnRebootRequiredIfTestReturnsRebootString()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create(""), ScriptBlock.Create("'reboot'"), new string[] { }, new string[] { });

            Assert.IsTrue(action.UpdateAction(new Hashtable()) == IdempotentActionResult.RebootRequired);
        }

        [TestMethod]
        public void RunningUpdateActionWillReturnUnknownIfNothingIsPipedOut()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create(""), ScriptBlock.Create(""), new string[] { }, new string[] { });

            Assert.IsTrue(action.UpdateAction(new Hashtable()) == IdempotentActionResult.NotConfigured);
        }

        [TestMethod]
        public void RunningUpdateActionWillReturnFailedIfStringThatIsntRebootIsPipedOut()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create(""), ScriptBlock.Create("'SomeErrorMessage'"), new string[] { }, new string[] { });

            Assert.IsTrue(action.UpdateAction(new Hashtable()) == IdempotentActionResult.Failed);
        }

        [TestMethod]
        public void RunningUpdateActionWillReturnNotConfiguredIfUpdateReturnsFalse()
        {
            var action = new IdempotentAction("TestAction", ScriptBlock.Create(""), ScriptBlock.Create("$false"), new string[] { }, new string[] { });

            Assert.IsTrue(action.UpdateAction(new Hashtable()) == IdempotentActionResult.NotConfigured);
        }

    }
}
