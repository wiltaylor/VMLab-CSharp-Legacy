using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class FloppyVMHandlerTests
    {
        public FloppyVMHandler Node;
        public Mock<IDriver> Driver;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Store = new Mock<IVMSettingsStore>();

            Node = new FloppyVMHandler(Driver.Object);

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);
        }

        [TestMethod]
        public void WhenCallingNodeItWillNotThrow()
        {
            Node.PreProcess("MyVM", "c:\\sourcefiles", "SomeOtherAction");
            Node.Process("MyVM", "c:\\sourcefiles", "SomeOtherAction");
            Node.PostProcess("MyVM", "c:\\sourcefiles", "SomeOtherAction");
        }

        [TestMethod]
        public void WhenCallingNodeWithStartActionItWillCallDriver()
        {
            Node.PreProcess("MyVM", "c:\\sourcefiles", "start");
            Driver.Verify(d => d.AddFloppy("MyVM", "c:\\sourcefiles"));
        }

        [TestMethod]
        public void WhenCallingNodeWithOtherActionItWillNotCallDriver()
        {
            Node.PreProcess("MyVM", "c:\\sourcefiles", "otheraction");
            Driver.Verify(d => d.AddFloppy("MyVM", "c:\\sourcefiles"), Times.Never);
        }

        [TestMethod]
        public void WhenCallingNodeWithStartActionItWontCallDriverIfHasBeenProvisionedHasBeenSet()
        {
            Store.Setup(s => s.ReadSetting<bool>("HasBeenProvisioned")).Returns(true);
            Node.PreProcess("MyVM", "c:\\sourcefiles", "start");
            Driver.Verify(d => d.AddFloppy("MyVM", "c:\\sourcefiles"), Times.Never);
        }
    }
}
