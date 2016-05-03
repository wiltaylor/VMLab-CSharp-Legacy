using Microsoft.VisualStudio.TestTools.UnitTesting;
using VMLab.Model;

namespace VMLab.Test.Model
{
    [TestClass]
    public class LabLibManagerTests
    {

        public LabLibManager Manager;

        [TestInitialize]
        public void Setup()
        {
            Manager = new LabLibManager();
        }

        [TestMethod]
        public void CallingTestOnManagerWillReturnFalseIfLibHasntBeenSet()
        {
            Assert.IsFalse(Manager.TestLib("NewLib"));
        }

        [TestMethod]
        public void CallingImportWithImportLibWillReturnTrueWhenTestIsCalled()
        {
            Manager.ImportLib("mytestlib");
            Assert.IsTrue(Manager.TestLib("mytestlib"));

        }

        [TestMethod]
        public void CallingResetWillClearOutImportedLibs()
        {
            Manager.ImportLib("mytestlib");
            Manager.Reset();
            Assert.IsFalse(Manager.TestLib("mytestlib"));
        }
    }
}
