using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class MemoryVMHandlerTests
    {
        public MemoryVMHandler Node;
        public Mock<IDriver> Driver;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();

            Node = new MemoryVMHandler(Driver.Object);
        }

        [TestMethod]
        public void CallingNodeWillNotThrow()
        {
            Node.PreProcess("MyVM", 1024, "SomeOtherAction");
            Node.Process("MyVM", 1024, "SomeOtherAction");
            Node.PostProcess("MyVM", 1024, "SomeOtherAction");
        }

        [TestMethod]
        public void CallingNodeWillCallDriverToSetMemory()
        {
            Node.PreProcess("MyVM", 1024, "start");
            Driver.Verify(d => d.SetMemory("MyVM", 1024));
        }

        [TestMethod]
        public void CallingNodeWillNotSetCPUIfActionIsntStart()
        {
            Node.PreProcess("MyVM", 1024, "notstart");
            Driver.Verify(d => d.SetMemory("MyVM", 1024), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNodeParametersException))]
        public void CallingNodeWillThrowIfSettingsIsNotAnInt()
        {
            Node.PreProcess("MyVM", "not int", "start");
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingNodeWillThrowIfSettingsIsLessThanOne()
        {
            Node.PreProcess("MyVM", -1, "start");
        }
    }
}
