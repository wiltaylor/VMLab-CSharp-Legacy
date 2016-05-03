using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class CPUVMHandlerTests
    {
        public CPUVMHandler CPU;
        public Mock<IDriver> Driver;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();

            CPU = new CPUVMHandler(Driver.Object);
        }

        [TestMethod]
        public void CallingNodeWillNotThrow()
        {
            CPU.PreProcess("MyVM", new Hashtable() { {"CPUs",1}, {"Cores", 1} }, "otheraction");
            CPU.Process("MyVM", new Hashtable() { { "CPUs", 1 }, { "Cores", 1 } }, "otheraction");
            CPU.PostProcess("MyVM", new Hashtable() { { "CPUs", 1 }, { "Cores", 1 } }, "otheraction");
        }

        [TestMethod]
        public void CallingNodeWithStartActionWillCallDriverToSetCPU()
        {
            CPU.PreProcess("MyVM", new Hashtable() { { "CPUs", 1 }, { "Cores", 2 } }, "start");

            Driver.Verify(d => d.SetCPU("MyVM", 1, 2));
        }

        [TestMethod]
        public void CallingNodeWillNotSetCPUIfActionIsntStart()
        {
            CPU.PreProcess("MyVM", new Hashtable() { { "CPUs", 1 }, { "Cores", 2 } }, "notstart");

            Driver.Verify(d => d.SetCPU("MyVM", 1, 2), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithSettingsThatAreNotAHashTableWillThrow()
        {
            CPU.PreProcess("MyVM", "Not a hash table!", "notstart");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingCpusKeyWillThrow()
        {
            CPU.PreProcess("MyVM", new Hashtable() { { "Cores", 2 } }, "notstart");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWithMissingCoresKeyWillThrow()
        {
            CPU.PreProcess("MyVM", new Hashtable() { { "CPUs", 1 } }, "notstart");
        }
    }
}
