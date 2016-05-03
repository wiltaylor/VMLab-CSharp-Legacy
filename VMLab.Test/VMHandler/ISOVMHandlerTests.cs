using System.Collections;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;
using VMLab.Model.Caps;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class ISOVMHandlerTests
    {
        public ISOVMHandler Node;
        public Mock<IDriver> Driver;
        public Mock<ICaps> Caps;
        public Mock<IFileSystem> FileSystem;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Caps = new Mock<ICaps>();
            FileSystem = new Mock<IFileSystem>();
            Store = new Mock<IVMSettingsStore>();

            Node = new ISOVMHandler(Driver.Object, Caps.Object, FileSystem.Object);

            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] {"scsi"});

            FileSystem.Setup(f => f.FileExists("c:\\test1.iso")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\test2.iso")).Returns(true);

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
        public void CallingNodeWillCallDriverToAddISO()
        {
            Node.PreProcess("MyVM", new []
            {
                new Hashtable() { { "Bus", "scsi"}, {"Path", "c:\\test1.iso"} },
                new Hashtable() { { "Bus", "scsi"}, {"Path", "c:\\test2.iso"} }
            }, "start");

            Driver.Verify(d => d.AddISO("MyVM", "scsi", "c:\\test1.iso"));
            Driver.Verify(d => d.AddISO("MyVM", "scsi", "c:\\test2.iso"));
        }

        [TestMethod]
        public void CallingNodeWillNotCallDriverIfActionIsntStart()
        {
            Node.PreProcess("MyVM", new[]
            {
                new Hashtable() { { "Bus", "scsi"}, {"Path", "c:\\test1.iso"} },
                new Hashtable() { { "Bus", "scsi"}, {"Path", "c:\\test2.iso"} }
            }, "notstart");

            Driver.Verify(d => d.AddISO("MyVM", "scsi", "c:\\test1.iso"), Times.Never);
            Driver.Verify(d => d.AddISO("MyVM", "scsi", "c:\\test2.iso"), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithInvalidSettingsTypeWillThrow()
        {
            Node.PreProcess("MyVM", "not array of hash tables!", "start");
        }

        [TestMethod]
        public void CallingNodeWithASingleHashtableWillNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi"}, {"Path", "c:\\test1.iso"} }, "start");
            Driver.Verify(d => d.AddISO("MyVM", "scsi", "c:\\test1.iso"));
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingNodeWithMissingBusWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Path", "c:\\test1.iso" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingPathWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithBusThatIsNotInCapsAcceptedBusList()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "notsupported" }, { "Path", "c:\\test1.iso" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof (FileNotFoundException))]
        public void CallingNodeWithBadFilePathWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\badfilepaththatdoesntexist.iso")).Returns(false);
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi" }, { "Path", "c:\\badfilepaththatdoesntexist.iso" } }, "start");
        }

        [TestMethod]
        public void CallingNodeWhenThisIsntFirstTimeStartHasBeenRunWillNotCallDriver()
        {
            Store.Setup(s => s.ReadSetting<bool>("HasBeenProvisioned")).Returns(true);
            Node.PreProcess("MyVM", new Hashtable() { { "Bus", "scsi" }, { "Path", "c:\\test1.iso" } }, "start");
            Driver.Verify(d => d.AddISO("MyVM", "scsi", "c:\\test1.iso"), Times.Never);
        }

    }
}
