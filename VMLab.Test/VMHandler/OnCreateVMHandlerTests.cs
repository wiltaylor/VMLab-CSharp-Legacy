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
    public class OnCreateVMHandlerTests
    {
        public OnCreateVMHandler Node;
        public Mock<IDriver> Driver;
        public Mock<IVMSettingsStore> Store;
        public Mock<IScriptHelper> ScriptHelper;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Store = new Mock<IVMSettingsStore>();
            ScriptHelper = new Mock<IScriptHelper>();

            Node = new OnCreateVMHandler(Driver.Object, ScriptHelper.Object);

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);
        }

        [TestMethod]
        public void CanCallNodeWithoutThrowing()
        {
            Node.PreProcess("MyVM", ScriptBlock.Create("#Empty script"), "notstart");
            Node.Process("MyVM", ScriptBlock.Create("#Empty script"), "notstart");
            Node.PostProcess("MyVM", ScriptBlock.Create("#Empty script"), "notstart");
        }

        [TestMethod]
        public void CallingNodeWillInvokeScriptWithScriptHelper()
        {
            Node.Process("MyVM", ScriptBlock.Create("#Powershell script to execute"), "start");
            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()));

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithInvalidSettingsTypeWillThrow()
        {
            Node.Process("MyVM", "not a script block!", "start");
        }

        [TestMethod]
        public void CallingNodeWillNotCallDriverIfNotFirstPass()
        {
            Store.Setup(s => s.ReadSetting<bool>("HasBeenProvisioned")).Returns(true);
            Node.Process("MyVM", ScriptBlock.Create("#Powershell script to execute"), "start");

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()), Times.Never);
        }

        [TestMethod]
        public void CallingNodeWithActionThatIsNotStartWillNotCallDriver()
        {
            Node.Process("MyVM", ScriptBlock.Create("#Powershell script to execute"), "notstart");

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>()), Times.Never);
        }
    }
}
