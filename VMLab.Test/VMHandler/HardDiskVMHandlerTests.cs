using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.Model.Caps;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class HardDiskVMHandlerTests
    {
        public HardDiskVMHandler Node;
        public Mock<IDriver> Driver;
        public Mock<ICaps> Caps;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Caps = new Mock<ICaps>();
            Store = new Mock<IVMSettingsStore>();

            Node = new HardDiskVMHandler(Driver.Object, Caps.Object);

            Caps.Setup(c => c.SupportedDriveType).Returns(new [] {"supportedtype"});
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new [] { "scsi"});

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);

        }

        [TestMethod]
        public void CallingNodeWillNotThrow()
        {
            Node.PreProcess("MyVM", new object[] {}, "otheraction");
            Node.Process("MyVM", new object[] { }, "otheraction");
            Node.PostProcess("MyVM", new object[] { }, "otheraction");
        }

        [TestMethod]
        public void CallingNodeWillCallDriver()
        {
            Node.PreProcess("MyVM", new object[]
            {
                new Hashtable() { { "Bus", "scsi"}, {"Size", 4000}, {"Type", "supportedtype"} } ,
                new Hashtable() { { "Bus", "scsi"}, {"Size", 1000}, {"Type", "supportedtype"} }
            }, "start");

            Driver.Verify(d => d.AddHDD("MyVM", "scsi", 4000, "supportedtype"));
            Driver.Verify(d => d.AddHDD("MyVM", "scsi", 1000, "supportedtype"));
        }

        [TestMethod]
        public void CallingNodeWillNotCreateDisksIfActionIsNotStart()
        {
            Node.PreProcess("MyVM", new object[]
            {
                new Hashtable() { { "Bus", "scsi"}, {"Size", 4000}, {"Type", "supportedtype"} } ,
                new Hashtable() { { "Bus", "scsi"}, {"Size", 1000}, {"Type", "supportedtype"} }
            }, "notstart");

            Driver.Verify(d => d.AddHDD("MyVM", "scsi", 4000, "supportedtype"), Times.Never);
            Driver.Verify(d => d.AddHDD("MyVM", "scsi", 1000, "supportedtype"), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithInvalidSettingsWillThrow()
        {
            Node.PreProcess("MyVM", "not an array of hashtables!", "start");
        }

        [TestMethod]
        public void CallingNodeWithJustASingleHashTableWillNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi"}, {"Size", 4000}, {"Type", "supportedtype"} }, "start");
            Driver.Verify(d => d.AddHDD("MyVM", "scsi", 4000, "supportedtype"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingBusWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Size", 4000 }, { "Type", "supportedtype" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingSizeWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi" }, { "Type", "supportedtype" } }, "start");
        }

        [TestMethod]
        public void CallingNodeWithMissingTypeWillNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi" }, { "Size", 4000 } }, "start");
            Driver.Verify(d => d.AddHDD("MyVM", "scsi", 4000, ""));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithTypeNotSupportedByCapsWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi" }, { "Size", 4000 }, { "Type", "notsupportedtype" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithBusNotSupportedByCapsWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "unsupportedbustype" }, { "Size", 4000 }, { "Type", "supportedtype" } }, "start");
        }

        [TestMethod]
        public void CallingNodeWhenThisIsntFirstTimeStartHasBeenRunWillNotCallDriver()
        {
            Store.Setup(s => s.ReadSetting<bool>("HasBeenProvisioned")).Returns(true);

            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi" }, { "Size", 4000 }, { "Type", "supportedtype" } }, "start");
            Driver.Verify(d => d.AddHDD("MyVM", "scsi", 4000, "supportedtype"), Times.Never);
        }
    }
}
