using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Test.Model
{
    [TestClass]
    public class VMSettingStoreTests
    {

        public Mock<IFileSystem> FileSystem;
        public VMSettingsStore Store;
        
        [TestInitialize]
        public void Setup()
        {
            FileSystem = new Mock<IFileSystem>();    

            Store = new VMSettingsStore("c:\\store.json", FileSystem.Object);
        }

        [TestMethod]
        public void CallingWriteSettingDoesntThrowWhenCalled()
        {
            Store.WriteSetting("My Setting", "myvalue");
        }

        [TestMethod]
        public void CallingWriteSettingWhenFileDoesntExistWillCreateANewOneWithSetting()
        {
            FileSystem.Setup(f => f.FileExists("c:\\store.json")).Returns(false);
            Store.WriteSetting("My Setting", "myvalue");         
            FileSystem.Verify(f => f.ReadFile("c:\\store.json"), Times.Never);
            FileSystem.Verify(f => f.SetFile("c:\\store.json", "{\"My Setting\":\"myvalue\"}"));
        }

        [TestMethod]
        public void CallingWriteSettingWillOverwriteExistingSettings()
        {
            FileSystem.Setup(f => f.FileExists("c:\\store.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\store.json")).Returns("{\"My Setting\":\"myoldvalue\",\"OtherSetting\":2}");
            Store.WriteSetting("My Setting", "myvalue");
            
            FileSystem.Verify(f => f.SetFile("c:\\store.json", "{\"My Setting\":\"myvalue\",\"OtherSetting\":2}"));
        }

        [TestMethod]
        public void CallingReadSettingDoesntThrowWhenCalled()
        {
            Store.ReadSetting<string>("MyString");
        }

        [TestMethod]
        public void CallingReadSettingWillReturnValueStoredInJson()
        {
            FileSystem.Setup(f => f.FileExists("c:\\store.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\store.json")).Returns("{\"Other setting\":\"myvalue\",\"MySetting\":2}");

            Assert.IsTrue(Store.ReadSetting<int>("MySetting") == 2);
        }

        [TestMethod]
        public void CallingReadSettingWhenFileDoesntExistWillReturnDefaultValue()
        {
            FileSystem.Setup(f => f.FileExists("c:\\store.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\store.json")).Returns("{\"Other setting\":\"myvalue\",\"MySetting\":2}");

            Assert.IsTrue(Store.ReadSetting<string>("nonexistingsetting") == null);
        }
    }
}
