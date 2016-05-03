using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class TemplateVMHandlerTests
    {
        public Mock<IDriver> Driver;
        public TemplateVMHandler Node;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Store = new Mock<IVMSettingsStore>();

            Node = new TemplateVMHandler(Driver.Object);

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);

        }

        [TestMethod]
        public void CallingTemplateNodeWillNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "SomeOtherAction");
            Node.Process("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "SomeOtherAction");
            Node.PostProcess("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "SomeOtherAction");
        }

        [TestMethod]
        public void CallingTemplateNodeWillCallDriverToCreateTemplate()
        {
            Node.PreProcess("MyVM", new Hashtable { {"Name", "MyTemplate"}, {"Snapshot", "Base"} } , "start");

            Driver.Verify(d => d.CreateVMFromTemplate("MyVM", "MyTemplate", "Base"));
        }

        [TestMethod]
        public void CallingTemplateNodeWillNotCallDriverToCreateTempalteIfVMAlreadyExists()
        {
            Driver.Setup(d => d.GetProvisionedVMs()).Returns(new [] {"MyVM"});

            Node.PreProcess("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "start");

            Driver.Verify(d => d.CreateVMFromTemplate("MyVM", "MyTemplate", "Base"), Times.Never);
        }

        [TestMethod]
        public void CallingTemplateNodeWillNotCallDriverToCreateTemplateIfActionIsNotStart()
        {
            Node.PreProcess("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "notstart");

            Driver.Verify(d => d.CreateVMFromTemplate("MyVM", "MyTemplate", "Base"), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingTemplateNodeWithAnInvalidPropertyTypeWillThrow()
        {
            Node.PreProcess("MyVM", "Using a string instead of a hashtable!", "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingTemplateNodeWithAPropertyHashMissingNameWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable { { "Snapshot", "Base" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingTemplateNodeWithAPropertyHashMissingSnapshotWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable { { "Name", "MyTemplate" } }, "start");
        }

        [TestMethod]
        public void CallingTemplateNodeWillSetHasBeenProvisionedDuringPostProcess()
        {
            Node.PostProcess("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "start");

            Store.Verify(s => s.WriteSetting("HasBeenProvisioned", true));
        }

        [TestMethod]
        public void CallingTemplateNodeWontSetHasBeenProvisionedIfActionIsNotStart()
        {
            Node.PostProcess("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "notstart");

            Store.Verify(s => s.WriteSetting("HasBeenProvisioned", true), Times.Never);
        }

        [TestMethod]
        public void CallingTemplateNodeWillCallDriverToStartVMDuringProcessPhase()
        {
            Node.Process("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "start");

            Driver.Verify(d => d.StartVM("MyVM"));
        }

        [TestMethod]
        public void CallingTemplateNodeWillNotCalldDriverDuringProcessPhaseIfActionIsNotStart()
        {
            Node.Process("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "notstart");

            Driver.Verify(d => d.StartVM("MyVM"), Times.Never);
        }

        [TestMethod]
        public void CallingTemplateNodeWillStopVMIfActionStopIsCalled()
        {
            Node.Process("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "stop");

            Driver.Verify(d => d.StopVM("MyVM", false));
        }

        [TestMethod]
        public void CallingTemplateNodeWillDestroyVMIfActionDestroyIsCalled()
        {
            Node.Process("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "destroy");

            Driver.Verify(d => d.RemoveVM("MyVM"));
        }

        [TestMethod]
        public void CallingNodeWithStopActionWillCallStopVMIfItIsRunning()
        {
            Driver.Setup(d => d.GetVMState("MyVM")).Returns(VMState.Ready);
            Node.Process("MyVM", new Hashtable() { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "destroy");

            Driver.Verify(d => d.StopVM("MyVM", true));
        }

        [TestMethod]
        public void CallingNodeWithStopActionWillNotCallStopVMIfItIsntRunning()
        {
            Driver.Setup(d => d.GetVMState("MyVM")).Returns(VMState.Shutdown);
            Node.Process("MyVM", new Hashtable() { { "Name", "MyTemplate" }, { "Snapshot", "Base" } }, "destroy");

            Driver.Verify(d => d.StopVM("MyVM", false), Times.Never);
        }

        [TestMethod]
        public void CallingNodeWithStartActionWillNotStartVMIfKeepPoweredOffIsSetToTrue()
        {
            Node.Process("MyVM", new Hashtable { { "Name", "MyTemplate" }, { "Snapshot", "Base" }, { "KeepPoweredOff", true} }, "start");

            Driver.Verify(d => d.StartVM("MyVM"), Times.Never);
        }
    }
}
