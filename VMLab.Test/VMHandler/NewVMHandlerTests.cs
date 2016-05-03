using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class NewVMHandlerTests
    {
        public Mock<IDriver> Driver;
        public NewVMHandler Node;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Store = new Mock<IVMSettingsStore>();

            Node = new NewVMHandler(Driver.Object);

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);
        }

        [TestMethod]
        public void CallingNodeWillNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)"}, {"Manifest", "Manifest json in this file."} },"start");
            Node.Process("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "start");
            Node.PostProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "start");
        }

        [TestMethod]
        public void CallingNodeWillMakeCallToDriverToMakeNewVM()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "start");

            Driver.Verify(d => d.CreateVM("MyVM", "vm test script here (i.e. vmx)", "Manifest json in this file."));
        }

        [TestMethod]
        public void CallingNodeWillMakeNotCallToDriverToMakeNewVmifVMAlreadyExists()
        {
            Driver.Setup(d => d.GetProvisionedVMs()).Returns(new[] { "MyVM" });

            Node.PreProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "start");

            Driver.Verify(d => d.CreateVM("MyVM", "vm test script here (i.e. vmx)", "Manifest json in this file."), Times.Never);
        }

        [TestMethod]
        public void CallingNodeWillMakeNoCallToDriverToMakeNewVmIfActivityIsNotStart()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "notstart");

            Driver.Verify(d => d.CreateVM("MyVM", "vm test script here (i.e. vmx)", "Manifest json in this file."), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWillWithInvalidTypeForSettingsWillThrow()
        {
            Node.PreProcess("MyVM", "wrong data type", "notstart");
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingNodeWillWillThrowIfVMScriptIsMissing()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Manifest", "Manifest json in this file." } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWillWillThrowIfManifestIsMissing()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" } }, "start");
        }

        [TestMethod]
        public void CallingNodeWillSetHasBeenProvisionedDuringPostProcess()
        {
            Node.PostProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "start");

            Store.Verify(s => s.WriteSetting("HasBeenProvisioned", true));
        }

        [TestMethod]
        public void CallingNodeWontSetHasBeenProvisionedIfActionIsNotStart()
        {
            Node.PostProcess("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "notstart");

            Store.Verify(s => s.WriteSetting("HasBeenProvisioned", true), Times.Never);
        }

        [TestMethod]
        public void CallingNodeWillCallDriverToStartVMDuringProcessPhase()
        {
            Node.Process("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "start");

            Driver.Verify(d => d.StartVM("MyVM"));
        }

        [TestMethod]
        public void CallingNodeWillNotCalldDriverDuringProcessPhaseIfActionIsNotStart()
        {
            Node.Process("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "notstart");

            Driver.Verify(d => d.StartVM("MyVM"), Times.Never);
        }

        [TestMethod]
        public void CallingNodeWillCallStopOnDriverIfActionIsStop()
        {
            Driver.Setup(d => d.GetVMState("MyVM")).Returns(VMState.Ready);
            Node.Process("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "stop");

            Driver.Verify(d => d.StopVM("MyVM", false));
        }

        [TestMethod]
        public void CallingTemplateNodeWillDestroyVMIfActionDestroyIsCalled()
        {
            
            Node.Process("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "destroy");

            Driver.Verify(d => d.RemoveVM("MyVM"));
        }

        [TestMethod]
        public void CallingNodeWithStopActionWillCallStopVMIfItIsRunning()
        {
            Driver.Setup(d => d.GetVMState("MyVM")).Returns(VMState.Ready);
            Node.Process("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "destroy");

            Driver.Verify(d => d.StopVM("MyVM", true));
        }

        [TestMethod]
        public void CallingNodeWithStopActionWillNotCallStopVMIfItIsntRunning()
        {
            Driver.Setup(d => d.GetVMState("MyVM")).Returns(VMState.Shutdown);
            Node.Process("MyVM", new Hashtable() { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." } }, "destroy");

            Driver.Verify(d => d.StopVM("MyVM", false), Times.Never);
        }

        [TestMethod]
        public void CallingNodeWithStartActionWillNotStartVMIfKeepPoweredOffIsSetToTrue()
        {
            Node.Process("MyVM", new Hashtable { { "VMScript", "vm test script here (i.e. vmx)" }, { "Manifest", "Manifest json in this file." }, { "KeepPoweredOff", true } }, "start");

            Driver.Verify(d => d.StartVM("MyVM"), Times.Never);
        }
    }
}
