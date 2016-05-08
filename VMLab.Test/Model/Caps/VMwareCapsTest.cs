using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VMLab.Driver.VMWareWorkstation;
using VMLab.Model.Caps;

namespace VMLab.Test.Model.Caps
{
    [TestClass]
    public class VMwareCapsTest
    {
        public ICaps Caps;

        [TestInitialize()]
        public void Setup()
        {
            Caps = new VMwareCaps();
        }

        [TestMethod]
        public void CanCreateVMwareCaps()
        {
            Assert.IsNotNull(Caps);
        }

        [TestMethod]
        public void CanCreateFromTemplate()
        {
            Assert.IsTrue(Caps.CanCreateFromTemplate);
        }

        [TestMethod]
        public void CanCreateFromText()
        {
            Assert.IsTrue(Caps.CanCreateFromText);
        }

        [TestMethod]
        public void CanListTemplates()
        {
            Assert.IsTrue(Caps.CanListTemplates);
        }

        [TestMethod]
        public void SupportsNATNetworkType()
        {
            Assert.IsTrue(Caps.SupportedNetworkTypes.Any(t => t == "NAT"));
        }

        [TestMethod]
        public void SupportsHostOnlyNetworkType()
        {
            Assert.IsTrue(Caps.SupportedNetworkTypes.Any(t => t == "HostOnly"));
        }

        [TestMethod]
        public void SupportsBridgedNetworkType()
        {
            Assert.IsTrue(Caps.SupportedNetworkTypes.Any(t => t == "Bridged"));
        }

        [TestMethod]
        public void SupportsIsolatedNetworkType()
        {
            Assert.IsTrue(Caps.SupportedNetworkTypes.Any(t => t == "Isolated"));
        }

        [TestMethod]
        public void SupportsVMNetNetworkType()
        {
            Assert.IsTrue(Caps.SupportedNetworkTypes.Any(t => t == "VMNet"));
        }

        [TestMethod]
        public void SupportsE1000NICType()
        {
            Assert.IsTrue(Caps.SupportedNICs.Any(n => n == "e1000"));
        }

        [TestMethod]
        public void SupportsE1000eNICType()
        {
            Assert.IsTrue(Caps.SupportedNICs.Any(n => n == "e1000e"));
        }

        [TestMethod]
        public void SupportsVlanceNICType()
        {
            Assert.IsTrue(Caps.SupportedNICs.Any(n => n == "vlance"));
        }

        [TestMethod]
        public void SupportsVmxnetNICType()
        {
            Assert.IsTrue(Caps.SupportedNICs.Any(n => n == "vmxnet"));
        }

        [TestMethod]
        public void DefaultNICTypeIsE1000()
        {
            Assert.IsTrue(Caps.DefaultNIC == "e1000");
        }
    }

}
