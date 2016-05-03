using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class OnDestroyVMTests
    {
        public OnDestroyVMHandler Node;
        public Mock<IDriver> Driver;
        public Mock<IScriptHelper> ScriptHelper;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            ScriptHelper = new Mock<IScriptHelper>();

            Node = new OnDestroyVMHandler(Driver.Object, ScriptHelper.Object);
        }

        [TestMethod]
        public void CanCallNodeWithoutThrowing()
        {
            Node.PreProcess("MyVM", ScriptBlock.Create("#Empty script"), "notdestroy");
            Node.Process("MyVM", ScriptBlock.Create("#Empty script"), "notdestroy");
            Node.PostProcess("MyVM", ScriptBlock.Create("#Empty script"), "notdestroy");
        }

        [TestMethod]
        public void CallingNodeWillInvokeScriptWithScriptHelper()
        {
            Node.PostProcess("MyVM", ScriptBlock.Create("#Powershell script to execute"), "destroy");
            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()));

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithInvalidSettingsTypeWillThrow()
        {
            Node.PostProcess("MyVM", "not a script block!", "destroy");
        }

        [TestMethod]
        public void CallingNodeWithActionThatIsNotDestroytWillNotCallDriver()
        {
            Node.PostProcess("MyVM", ScriptBlock.Create("#Powershell script to execute"), "notdestroy");

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()), Times.Never);
        }
    }
}
