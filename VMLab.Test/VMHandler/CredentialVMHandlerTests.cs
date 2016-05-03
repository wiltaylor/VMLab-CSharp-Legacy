using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.VMHandler;

namespace VMLab.Test.VMHandler
{
    [TestClass]
    public class CredentialVMHandlerTests
    {
        public CredentialVMHandler Node;
        public Mock<IDriver> Driver;
        public Mock<IVMSettingsStore> Store;

        [TestInitialize]
        public void Setup()
        {
            Driver = new Mock<IDriver>();
            Store = new Mock<IVMSettingsStore>();

            Node = new CredentialVMHandler(Driver.Object);

            Driver.Setup(d => d.GetVMSettingStore("MyVM")).Returns(Store.Object);
        }

        [TestMethod]
        public void CallingNodeWillNotThrow()
        {
            Node.PreProcess("MyVM", new object[] {}, "otheraction");
            Node.Process("MyVM", new object[] {}, "otheraction");
            Node.PostProcess("MyVM", new object[] {}, "otheraction");
        }

        [TestMethod]
        public void CallingNodeWillMakeCallsToDriverToSetCredentials()
        {
            Node.PreProcess("MyVM",
                new object[]
                {
                    new Hashtable() {{"Username", "User1"}, {"Password", "password1"}},
                    new Hashtable() {{"Username", "User2"}, {"Password", "password2"}}
                }, "start");
            Driver.Verify(d => d.AddCredential("MyVM", "User1", "password1"));
            Driver.Verify(d => d.AddCredential("MyVM", "User2", "password2"));
        }

        [TestMethod]
        public void CallingNodeWillNotCallDriverIfActionIsNotStart()
        {
            Node.PreProcess("MyVM",
                new object[]
                {
                    new Hashtable() {{"Username", "User1"}, {"Password", "password1"}},
                    new Hashtable() {{"Username", "User2"}, {"Password", "password2"}}
                }, "notstart");
            Driver.Verify(d => d.AddCredential("MyVM", "User1", "password1"), Times.Never);
            Driver.Verify(d => d.AddCredential("MyVM", "User2", "password2"), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingNodeWillThrowIfSettingsIsNotATypeOfArray()
        {
            Node.PreProcess("MyVM", "Not Object Array", "start");
        }

        [TestMethod]
        public void CallingNodeWithJustASingleHashTableShouldNotThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() {{"Username", "User1"}, {"Password", "password1"}}, "start");

            Driver.Verify(d => d.AddCredential("MyVM", "User1", "password1"));
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingNodeWithMissingUsernameWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() {{"Password", "password1"}}, "start");
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidNodeParametersException))]
        public void CallingNodeWithMissingPasswordWillThrow()
        {
            Node.PreProcess("MyVM", new Hashtable() {{"Username", "User1"}}, "start");
        }

        [TestMethod]
        public void CallingNodeWhenThisIsntFirstTimeStartHasBeenRunWillNotCallDriver()
        {
            Store.Setup(s => s.ReadSetting<bool>("HasBeenProvisioned")).Returns(true);
            Node.PreProcess("MyVM", new Hashtable() { { "Username", "User1" }, { "Password", "password1" } }, "start");

            Driver.Verify(d => d.AddCredential("MyVM", "User1", "password1"), Times.Never);
        }
    }
}
