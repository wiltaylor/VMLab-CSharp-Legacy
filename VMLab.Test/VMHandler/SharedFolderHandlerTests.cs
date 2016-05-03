using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class SharedFolderHandlerTests
    {

        public SharedFolderHandler Node;
        public Mock<IDriver> Driver;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Store = new Mock<IVMSettingsStore>();
            Node = new SharedFolderHandler(Driver.Object);

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);
        }

        [TestMethod]
        public void CallingNodeWillNotThrow()
        {
            Node.PreProcess("MyVM", new object[] {}, "otheraction" );
            Node.Process("MyVM", new object[] { }, "otheraction");
            Node.PostProcess("MyVM", new object[] { }, "otheraction");
        }

        [TestMethod]
        public void CallingNodeWillCallDriverToAddShareFolder()
        {
            Node.Process("MyVM", new []
            {
                new Hashtable() { { "HostPath", "c:\\TestFolder1" }, { "GuestPath", "c:\\inguest1" }, { "ShareName","guestshare1" } },
                new Hashtable() { { "HostPath", "c:\\TestFolder2" }, { "GuestPath", "c:\\inguest2" }, { "ShareName","guestshare2" } }
            }, "start");

            Driver.Verify(d => d.AddSharedFolder("MyVM", "c:\\TestFolder1", "guestshare1", "c:\\inguest1"));
            Driver.Verify(d => d.AddSharedFolder("MyVM", "c:\\TestFolder2", "guestshare2", "c:\\inguest2"));
        }

        [TestMethod]
        public void CallingNodeWillNotCallDriverIfActionIsntStart()
        {
            Node.Process("MyVM", new[]
            {
                new Hashtable() { { "HostPath", "c:\\TestFolder1" }, { "GuestPath", "c:\\inguest1" }, { "ShareName","guestshare1" } },
                new Hashtable() { { "HostPath", "c:\\TestFolder2" }, { "GuestPath", "c:\\inguest2" }, { "ShareName","guestshare2" } }
            }, "notstart");

            Driver.Verify(d => d.AddSharedFolder("MyVM", "c:\\TestFolder1", "guestshare1", "c:\\inguest1"), Times.Never);
            Driver.Verify(d => d.AddSharedFolder("MyVM", "c:\\TestFolder2", "guestshare2", "c:\\inguest2"), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithInvalidSettingTypeWillThrow()
        {
            Node.Process("MyVM", "Not a object array of hash tables!", "start");
        }

        [TestMethod]
        public void CallingNodeWithJustASingleHashTableWillNotThrow()
        {
            Node.Process("MyVM", new Hashtable() { { "HostPath", "c:\\TestFolder1" }, { "GuestPath", "c:\\inguest1" }, { "ShareName","guestshare1" } }, "start");

            Driver.Verify(d => d.AddSharedFolder("MyVM", "c:\\TestFolder1", "guestshare1", "c:\\inguest1"));
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingNodeWithMissingHostPathWillThrow()
        {
            Node.Process("MyVM", new Hashtable() { { "GuestPath", "c:\\inguest1" }, { "ShareName", "guestshare1" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingGuestPathWillThrow()
        {
            Node.Process("MyVM", new Hashtable() { { "HostPath", "c:\\TestFolder1" }, { "ShareName", "guestshare1" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingShareNameWillThrow()
        {
            Node.Process("MyVM", new Hashtable() { { "HostPath", "c:\\TestFolder1" }, { "GuestPath", "c:\\inguest1" } }, "start");
        }

        [TestMethod]
        public void CallingNodeWhenThisIsntFirstTimeStartHasBeenRunWillNotCallDriver()
        {
            Store.Setup(s => s.ReadSetting<bool>("HasBeenProvisioned")).Returns(true);

            Node.Process("MyVM", new Hashtable() { { "HostPath", "c:\\TestFolder1" }, { "GuestPath", "c:\\inguest1" }, { "ShareName", "guestshare1" } }, "start");

            Driver.Verify(d => d.AddSharedFolder("MyVM", "c:\\TestFolder1", "guestshare1", "c:\\inguest1"), Times.Never);
        }
    }
}
