using System.Collections;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.Model.Caps;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class NetworkVMHandlerTests
    {

        public NetworkVMHandler Node;
        public Mock<IDriver> Driver;
        public Mock<ICaps> Caps;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Caps = new Mock<ICaps>();
            Store = new Mock<IVMSettingsStore>();

            Node = new NetworkVMHandler(Driver.Object, Caps.Object);

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);

            Caps.Setup(c => c.SupportedNetworkTypes).Returns(new[] {"SupportedNetworkType"});
            Caps.Setup(c => c.SupportedNICs).Returns(new[] {"SupportedNICType"});
        }

        [TestMethod]
        public void CallingNodeWillNotThrow()
        {
            Node.PreProcess("MyVM", new object[] {}, "otheraction");
            Node.Process("MyVM", new object[] { }, "otheraction");
            Node.PostProcess("MyVM", new object[] { }, "otheraction");
        }

        [TestMethod]
        public void CallingNodeWillCallDriverToAddNetworkCards()
        {
            Node.PreProcess("MyVM", new []
            {
                new Hashtable() { {"NetworkType", "SupportedNetworkType"}, {"NICType", "SupportedNICType"} },
                new Hashtable() { {"NetworkType", "SupportedNetworkType"}, {"NICType", "SupportedNICType"} }
            }, "start");

            Driver.Verify(d => d.AddNetwork("MyVM", "SupportedNetworkType", "SupportedNICType", null), Times.Exactly(2));
        }

        [TestMethod]
        public void CallingNodeWithExtraPropertiesWillBePassedAsDynamicObject()
        {
            Node.PreProcess("MyVM", new[]
            {
                new Hashtable() { {"NetworkType", "SupportedNetworkType"}, {"NICType", "SupportedNICType"}, {"ExtraProperty1", "Value1"} },
                new Hashtable() { {"NetworkType", "SupportedNetworkType"}, {"NICType", "SupportedNICType"} }
            }, "start");

            dynamic extra = new ExpandoObject();
            extra.ExtraProperty1 = "Value1";

            Driver.Verify(d => d.AddNetwork("MyVM", "SupportedNetworkType", "SupportedNICType", (object)extra));
            Driver.Verify(d => d.AddNetwork("MyVM", "SupportedNetworkType", "SupportedNICType", null));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWillThrowIfInvalidTypeIsPassedAsSettings()
        {
            Node.PreProcess("MyVM", "This is not an array of hashtables", "start");
        }

        [TestMethod]
        public void CallingNodeWithJustASingleHashTableWillNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { {"NetworkType", "SupportedNetworkType"}, {"NICType", "SupportedNICType"} }, "start");

            Driver.Verify(d => d.AddNetwork("MyVM", "SupportedNetworkType", "SupportedNICType", null));
        }

        [TestMethod]
        public void CallingNodeWithActionThatIsntStartWontCallDriver()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "NetworkType", "SupportedNetworkType" }, { "NICType", "SupportedNICType" } }, "notstart");

            Driver.Verify(d => d.AddNetwork("MyVM", "SupportedNetworkType", "SupportedNICType", null), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingNetworkTypeWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "NICType", "SupportedNICType" } }, "start");
        }

        [TestMethod]
        public void CallingNodeWithMissingNICTypeWillNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "NetworkType", "SupportedNetworkType" } }, "start");
            Driver.Verify(d => d.AddNetwork("MyVM", "SupportedNetworkType", "", null));
        }
        
        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithUnsupportedNetworkTypeWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "NetworkType", "UnsupportedNetworkType" }, { "NICType", "SupportedNICType" } }, "start");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithUnsupportedNICTypeWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() { { "NetworkType", "SupportedNetworkType" }, { "NICType", "UnsupportedNICType" } }, "start");
        }

        [TestMethod]
        public void CallingNodeWhenThisIsntFirstTimeStartHasBeenRunWillNotCallDriver()
        {
            Store.Setup(s => s.ReadSetting<bool>("HasBeenProvisioned")).Returns(true);
            Node.PreProcess("MyVM", new Hashtable() { { "NetworkType", "SupportedNetworkType" }, { "NICType", "SupportedNICType" } }, "start");

            Driver.Verify(d => d.AddNetwork("MyVM", "SupportedNetworkType", "SupportedNICType", null), Times.Never);
        }
    }
}
