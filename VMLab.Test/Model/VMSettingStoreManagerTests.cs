using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Test.Model
{
    [TestClass]
    public class VMSettingStoreManagerTests
    {
        public VMSettingStoreManager Manager;
        public Mock<IFileSystem> FileSystem;

        [TestInitialize]
        public void Setup()
        {
            FileSystem = new Mock<IFileSystem>();

            Manager = new VMSettingStoreManager(FileSystem.Object);
        }

        [TestMethod]
        public void CallingGetStoreWillReturnAStoreObject()
        {           
            Assert.IsNotNull(Manager.GetStore("c:\\SomePath"));
        }
    }
}
