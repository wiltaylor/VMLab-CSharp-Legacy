using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Driver.VMWareWorkstation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;
using VMLab.Test.Helper;

namespace VMLab.Test.Drivers
{
    [TestClass]
    public class VMwareHyperVisorTest
    {
        public IVMwareHypervisor Hypervisor;
        public Mock<IFileSystem> FileSystem;
        public Mock<IVMwareExe> VMwareExe;
        public Mock<IVMwareDiskExe> VMwareDiskExe;
        public Mock<IServiceDiscovery> SVC;
        public Mock<IEnvironmentDetails> Env;
        public Mock<IVix> Vix;

        [TestInitialize()]
        public void Setup()
        {
            FileSystem = new Mock<IFileSystem>();
            VMwareExe = new Mock<IVMwareExe>();
            VMwareDiskExe = new Mock<IVMwareDiskExe>();
            Vix = new Mock<IVix>();
            SVC = new Mock<IServiceDiscovery>();
            Env = new Mock<IEnvironmentDetails>();
            Hypervisor = new VMwareHypervisor(FileSystem.Object, VMwareExe.Object, VMwareDiskExe.Object, Env.Object, new FakeCancellableAsyncActionManager());
            
            ServiceDiscovery.UnitTestInject(SVC.Object);
            SVC.Setup(s => s.GetObject<IVix>()).Returns(Vix.Object);

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "settingname = \"value\"", "anothersetting = \"anothervalue\""));
        }

        [TestMethod]
        public void CanCreateLinkedCloneByCallingVMRun()
        {
            FileSystem.Setup(f => f.FileExists("Path\\ToExisting\\VM.vmx")).Returns(true);

            Hypervisor.Clone("Path\\ToExisting\\VM.vmx", "Path\\ToNewLocation\\vm.vmx", "Snapshot", CloneType.Linked);

            Vix.Verify(v => v.Clone("Path\\ToNewLocation\\vm.vmx", "Snapshot", true));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingCloneOnVMThatDoesntExistWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmthatdoesntexists.vmx")).Returns(false);
            Hypervisor.Clone("c:\\vmthatdoesntexists.vmx", "Path\\ToNewLocation\\vm.vmx", "Snapshot", CloneType.Linked);
        }

        [TestMethod]
        [ExpectedException(typeof (VMXAlreadyExistsException))]
        public void CallingCloneWithVMTargetThatAlreadyExistsWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("Path\\ToExisting\\VM.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("Path\\ToNewLocation\\vm.vmx")).Returns(true);
            Hypervisor.Clone("Path\\ToExisting\\VM.vmx", "Path\\ToNewLocation\\vm.vmx", "NonExistingSnapshot",
                CloneType.Linked);
        }

        [TestMethod]
        public void CallingCloneWithFullFlagWillCallVMRunWithExpectedSwitch()
        {
            FileSystem.Setup(f => f.FileExists("Path\\ToExisting\\VM.vmx")).Returns(true);

            Hypervisor.Clone("Path\\ToExisting\\VM.vmx", "Path\\ToNewLocation\\vm.vmx", "Snapshot", CloneType.Full);
            Vix.Verify(v => v.Clone("Path\\ToNewLocation\\vm.vmx", "Snapshot", false));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingWriteSettingWillThrowIfFileDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\noneixstingfile.vmx")).Returns(false);
            Hypervisor.WriteSetting("c:\\nonexistingfile.vmx", "MySetting", "Value");
        }

        [TestMethod]
        public void CallingWriteSettingWillReadTheContentsOfTheVMXIntoMemory()
        {

            Hypervisor.WriteSetting("c:\\existing.vmx", "MySetting", "Value");

            FileSystem.Verify(f => f.ReadFile("c:\\existing.vmx"));
        }

        [TestMethod]
        public void CallingWriteSettingWillAppendSettingsThatDontExistInTheFile()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "settingname = \"value\"", "anothersetting = \"anothervalue\""));
            Hypervisor.WriteSetting("c:\\existing.vmx", "MySetting", "Value");
            FileSystem.Verify(
                f =>
                    f.SetFile("c:\\existing.vmx",
                        string.Join(Environment.NewLine, "settingname = \"value\"", "anothersetting = \"anothervalue\"",
                            "MySetting = \"Value\"")));
        }

        [TestMethod]
        public void CallingWriteSettingWillUpdateSettingThatAlreadyExistsInFile()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "settingname = \"value\"", "anothersetting = \"anothervalue\""));
            Hypervisor.WriteSetting("c:\\existing.vmx", "settingname", "NewName");
            FileSystem.Verify(
                f =>
                    f.SetFile("c:\\existing.vmx",
                        string.Join(Environment.NewLine, "settingname = \"NewName\"",
                            "anothersetting = \"anothervalue\"")));
        }

        [TestMethod]
        public void CallingGetFreeNICIDWillReturnZeroIfNoNICsSet()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "settingname = \"value\"", "anothersetting = \"anothervalue\""));
            Assert.IsTrue(Hypervisor.GetFreeNicID("c:\\existing.vmx") == 0);
        }

        [TestMethod]
        public void CallingGetFreeNICIDWillReturnOneHigherThanExistingNICIDs()
        {
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "ethernet0.present = \"TRUE\"",
                    "anothersetting = \"anothervalue\""));
            Assert.IsTrue(Hypervisor.GetFreeNicID("c:\\existing.vmx") == 1);
        }

        [TestMethod]
        public void CallingLookupPVNWillReturnANewPvnifAPVNFileCantBeFoundInVMDirectory()
        {
            FileSystem.Setup(f => f.FileExists("C:\\ExistingVMFolder\\VM\\PVN.json")).Returns(false);
            var result = Hypervisor.LookUpPVN("NetworkDoesntExist", "C:\\ExistingVMFolder\\VM\\PVN.json");
            Assert.IsTrue(Regex.IsMatch(result,
                "[0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2}-[0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2} [0-9A-F]{2}"));
        }

        [TestMethod]
        public void CallingLookupPVNWillReturnExistingNetworkIfItIsStoredInPVNFile()
        {
            FileSystem.Setup(f => f.FileExists("C:\\ExistingVMFolder\\VM\\PVN.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("C:\\ExistingVMFolder\\VM\\PVN.json"))
                .Returns("{\"ExistingNetwork\":\"00 00 00 00 00 00 00 00-00 00 00 00 00 00 00 00\"}");
            Assert.IsTrue(Hypervisor.LookUpPVN("ExistingNetwork", "C:\\ExistingVMFolder\\VM\\PVN.json") ==
                          "00 00 00 00 00 00 00 00-00 00 00 00 00 00 00 00");
        }

        [TestMethod]
        public void CallingLookUpPVNWillStoreNewNetworkInPVNFileIfItDidntExist()
        {
            FileSystem.Setup(f => f.FileExists("C:\\ExistingVMFolder\\VM\\PVN.json")).Returns(false);
            var result = Hypervisor.LookUpPVN("NetworkDoesntExist", "C:\\ExistingVMFolder\\VM\\PVN.json");
            FileSystem.Verify(
                f => f.SetFile("C:\\ExistingVMFolder\\VM\\PVN.json", "{\"NetworkDoesntExist\":\"" + result + "\"}"));
        }

        [TestMethod]
        public void CallingGetRunningVMsWhenNoVMsAreRunningWillReturnAnEmptyArray()
        {
            Vix.Setup(v => v.GetRunningVMs()).Returns(new string[] {});
            Assert.IsTrue(Hypervisor.GetRunningVMs().Length == 0);
        }

        [TestMethod]
        public void CallingGetRunningVMsWillReturnAvmNameWhenOneIsRunning()
        {
            Vix.Setup(v => v.GetRunningVMs()).Returns(new[] {"c:\\myvm\\myvm.vmx"});
            Assert.IsTrue(Hypervisor.GetRunningVMs().Contains("c:\\myvm\\myvm.vmx"));
        }

        [TestMethod]
        public void CallingFileExistInGuestReturnTrueIfFileExists()
        {
            Vix.Setup(v => v.FileExistInGuest("c:\\windows\\explorer.exe")).Returns(true);
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Assert.IsTrue(Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object},
                "c:\\windows\\explorer.exe"));
        }

        [TestMethod]
        public void CallingFileExistInGuestReturnFalseIfFileDoesntExist()
        {
            Vix.Setup(v => v.FileExistInGuest("c:\\windows\\explorer.exe")).Returns(false);
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Assert.IsFalse(Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object},
                "c:\\windows\\explorer.exe"));
        }

        [TestMethod]
        public void CallingFileExistsWithBadFirstCredentialButGoodSecondCredentialsReturnsTrueIfFileExists()
        {
            Vix.Setup(v => v.FileExistInGuest("c:\\windows\\explorer.exe")).Returns(true);
            Vix.Setup(v => v.LoginToGuest("baduser", "badpassword", false)).Throws(new VixException(""));
            var goodcreds = new Mock<IVMCredential>();
            goodcreds.Setup(c => c.Username).Returns("user");
            goodcreds.Setup(c => c.Password).Returns("password");
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Assert.IsTrue(Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {badcreds.Object, goodcreds.Object},
                "c:\\windows\\explorer.exe"));
        }


       [TestMethod]
        [ExpectedException(typeof (GuestVMPoweredOffException))]
        public void CallingFileExistsWhenVMIsPoweredOffWillThrow()
       {
           Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Off);
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object}, "c:\\windows\\explorer.exe");
        }

        [TestMethod]
        public void CallingDirectoryExistInGuestReturnTrueIfDirectoryExists()
        {
            Vix.Setup(v => v.DirectoryExistInGuest("c:\\windows")).Returns(true);
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Assert.IsTrue(Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object}, "c:\\windows"));
        }

        [TestMethod]
        public void CallingDirectoryExistInGuestReturnFalseIfDirectoryDoesntExist()
        {
            Vix.Setup(v => v.DirectoryExistInGuest("c:\\folderthatdoesntexist")).Returns(false);
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Assert.IsFalse(Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object},
                "c:\\folderthatdoesntexist"));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingStartVMWithNonExistingVMXWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexistingvm.vmx")).Returns(false);

            Hypervisor.StartVM("c:\\nonexistingvm.vmx");
        }

        [TestMethod]
        public void CallingStartVMForExistingVMWillResultInStartedVmByVix()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Hypervisor.StartVM("c:\\existingvm.vmx");
            Vix.Verify(v => v.ConnectToVM("c:\\existingvm.vmx"));
            Vix.Verify(v => v.PowerOnVM());
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingStartVMForExistingVMWillThrowIfThereIsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Vix.Setup(v => v.PowerOnVM()).Throws(new VixException(""));
            Hypervisor.StartVM("c:\\existingvm.vmx");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingStopVMWithNonExistingVMXWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexistingvm.vmx")).Returns(false);

            Hypervisor.StopVM("c:\\nonexistingvm.vmx", false);
        }

        [TestMethod]
        public void CallingStopVMForExistingVMWillResultInStopCalledByVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Hypervisor.StopVM("c:\\existingvm.vmx", false);
            Vix.Verify(v => v.ConnectToVM("c:\\existingvm.vmx"));
            Vix.Verify(v => v.PowerOffVM(false));
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingStopVMForExistingVMWillThrowIfThereIsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Vix.Setup(v => v.PowerOffVM(false)).Throws(new VixException(""));
            Hypervisor.StopVM("c:\\existingvm.vmx", false);
        }

        [TestMethod]
        public void CallingStopVMWithForceWillCallVixWithForceParameter()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Hypervisor.StopVM("c:\\existingvm.vmx", true);
            Vix.Verify(v => v.PowerOffVM(true));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingResetVMWithNonExistingVMXWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexistingvm.vmx")).Returns(false);

            Hypervisor.ResetVM("c:\\nonexistingvm.vmx", false);
        }

        [TestMethod]
        public void CallingResetVMForExistingVMWillResultInStopCalledByVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Hypervisor.ResetVM("c:\\existingvm.vmx", false);
            Vix.Verify(v => v.ResetVM(false));
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingResetVMForExistingVMWillThrowIfThereIsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Vix.Setup(v => v.ResetVM(false)).Throws(new VixException(""));
            Hypervisor.ResetVM("c:\\existingvm.vmx", false);
        }

        [TestMethod]
        public void CallingResetVMWithForceWillCallVixWithForceParameter()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            Hypervisor.ResetVM("c:\\existingvm.vmx", true);
            Vix.Verify(v => v.ResetVM(true));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingReadSettingWillThrowIfVMXFileDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ReadSetting("c:\\nonexisting.vmx", "MySetting");
        }

        [TestMethod]
        public void CallingReadSettingWillReturnNullIfSettingDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "MySetting = \"Value\""));
            Assert.IsNull(Hypervisor.ReadSetting("c:\\existing.vmx", "SomeOtherSetting"));
        }

        [TestMethod]
        public void CallingReadSettingWillReturnValueIfItDoesExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "MySetting = \"Value\"", "MyotherSetting = \"Blabla\""));
            Assert.IsTrue(Hypervisor.ReadSetting("c:\\existing.vmx", "MySetting") == "Value");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingClearSettingOnVMXThatDoesntExistThrows()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ClearSetting("c:\\nonexisting.vmx", "MySetting");
        }

        [TestMethod]
        public void CallingClearSettingOnSettingThatDoesExistWillRemoveSetting()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "MySetting = \"Value\"", "MyotherSetting = \"Blabla\""));
            Hypervisor.ClearSetting("c:\\existing.vmx", "MySetting");
            FileSystem.Verify(
                f => f.SetFile("c:\\existing.vmx", string.Join(Environment.NewLine, "MyotherSetting = \"Blabla\"")));
        }

        [TestMethod]
        public void CallingClearSettingOnSettingThatDoesntExistSettingsAreNotChanged()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "MySetting = \"Value\"", "MyotherSetting = \"Blabla\""));
            Hypervisor.ClearSetting("c:\\existing.vmx", "NonExistingSetting");
            FileSystem.Verify(
                f =>
                    f.SetFile("c:\\existing.vmx",
                        string.Join(Environment.NewLine, "MySetting = \"Value\"", "MyotherSetting = \"Blabla\"")));
        }

        [TestMethod]
        public void CallingRemoveVMWillMakeCallToVMRunToRemoveVM()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\existing.vmx")).Returns(true);
            Hypervisor.RemoveVM("c:\\vmfolder\\vmguid\\existing.vmx");
            Vix.Verify(v => v.ConnectToVM("c:\\vmfolder\\vmguid\\existing.vmx"));
            Vix.Verify(v => v.Delete());
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingRemoveVMWillThrowIfVixThrowsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.Delete()).Throws(new VixException(""));
            Hypervisor.RemoveVM("c:\\vmfolder\\vmguid\\existing.vmx");
        }

        [TestMethod]
        public void CallingRemoveVMWillAlsoDeleteVMFolder()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\vmfolder")).Returns(true);
            Hypervisor.RemoveVM("c:\\vmfolder\\vmguid\\existing.vmx");
            FileSystem.Verify(f => f.DeleteFolder("c:\\vmfolder", true));
        }

        [TestMethod]
        public void CallingRemoveVMWillStillRemoveFolderIfVMXDoesntExistButFolderDoes()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\nonexisting.vmx")).Returns(false);
            FileSystem.Setup(f => f.FolderExists("c:\\vmfolder")).Returns(true);
            Hypervisor.RemoveVM("c:\\vmfolder\\vmguid\\nonexisting.vmx");
            FileSystem.Verify(f => f.DeleteFolder("c:\\vmfolder", true));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingShowGUIOnNonExistingVMXWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ShowGUI("c:\\nonexisting.vmx");
        }

        [TestMethod]
        public void CallingShowGUIOnExistingVMItWillCallVMWareToShowIt()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.ShowGUI("c:\\existing.vmx");
            VMwareExe.Verify(v => v.ShowVM("c:\\existing.vmx"));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingCopyFileToGuestWillThrowIfVMXDoesntExist()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.CopyFileToGuest("c:\\nonexisting.vmx", new[] {creds.Object}, "c:\\somefile.txt",
                "c:\\onvm\\somefile.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (FileNotFoundException))]
        public void CallingCopyFileToGuestWillThrowIfHostFileDoesntExist()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\nonexistinghostfile.txt")).Returns(false);
            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\nonexistinghostfile.txt",
                "c:\\onvm\\somefile.txt");
        }

        [TestMethod]
        public void CallingCopyFileToGuestWillCallVMRunToCopyFile()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existinghostfile.txt")).Returns(true);
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\existinghostfile.txt",
                "c:\\onvm\\somefile.txt");
            Vix.Verify(v => v.CopyFileToGuest("c:\\existinghostfile.txt", "c:\\onvm\\somefile.txt"));
           
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingCopyFileWithABadGuestPathWillThrow()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existinghostfile.txt")).Returns(true);
            Vix.Setup(v => v.CopyFileToGuest("c:\\existinghostfile.txt", "c:\\badvm\\somefile.txt")).Throws(new VixException(""));
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\existinghostfile.txt",
                "c:\\badvm\\somefile.txt");
        }


        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingCopyFileFromGuestWillThrowIfVMXDoesntExist()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.CopyFileFromGuest("c:\\nonexisting.vmx", new[] {creds.Object}, "c:\\onvm\\somefile.txt",
                "c:\\somefile.txt");
        }

        [TestMethod]
        public void CallingCopyFileFromGuestWillMakeCallToVix()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            Hypervisor.CopyFileFromGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\onvm\\somefile.txt",
                "c:\\somefile.txt");
            Vix.Verify(v => v.CopyFileFromGuest("c:\\somefile.txt", "c:\\onvm\\somefile.txt"));
        }

        [TestMethod]
        [ExpectedException(typeof (FileNotFoundException))]
        public void CallingCopyFileFromGuestWillThrowIfHostFolderDoesntExist()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            FileSystem.Setup(f => f.FolderExists("c:\\badfolderonhost")).Returns(false);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.CopyFileFromGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\somefile.txt",
                "c:\\badfolderonhost\\somefile.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingDeleteFileOnVMThatDoesntExistThrows()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.DeleteFileInGuest("c:\\nonexisting.vmx", new[] {creds.Object}, "c:\\testfileinguest.txt");
        }

        [TestMethod]
        public void CallingDeleteFileOnVMWillMakeACallVix()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.DeleteFileInGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\testfileinguest.txt");
            Vix.Verify(v => v.DeleteFileInGuest("c:\\testfileinguest.txt"));
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingDeleteFileWillThrowIfFileDoesntExistInGuest()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.DeleteFileInGuest("c:\\badfilepath.txt")).Throws(new VixException(""));
            Hypervisor.DeleteFileInGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\badfilepath.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingExecuteCommandWillThrowIfVMXDoesntExist()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ExecuteCommand("c:\\nonexisting.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches",
                false, false);
        }

        [TestMethod]
        public void CallingExecuteCommandWillCallVMRunToExecuteIt()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", false,
                false);
            Vix.Verify(v => v.ExecuteCommand("c:\\myapp.exe", "-someswitches", false, true));
        }

        [TestMethod]
        public void CallingExecuteCommandWithNoWaitSetWillPassNoWaitToVix()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", true,
                false);
            Vix.Verify(v => v.ExecuteCommand("c:\\myapp.exe", "-someswitches", false, false));
        }

        [TestMethod]
        public void CallingExecuteCommandWithInteractiveSwitchWillMakeVixDoInteractiveLogon()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", false,
                true);
            Vix.Verify(v => v.LoginToGuest("user", "password", true));
        }
        
        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingAddSharedFolderWithVMXThatDoesntExistWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.AddSharedFolder("c:\\nonexisting.vmx", "c:\\myfolder", "myfolder");
        }

        [TestMethod]
        [ExpectedException(typeof (FileNotFoundException))]
        public void CallingAddSharedFolderWhenHostFolderDoesntExistWillThrow()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(false);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
        }

        [TestMethod]
        public void CallingAddSharedFolderWillMakeACallToVixToAddShare()
        {
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
            Vix.Verify(v => v.AddShareFolder("c:\\myfolder", "myfolder"));
        }

        [TestMethod]
        public void CallingAddSharedFolderWillMakeACallToVMRunToEnableSharing()
        {
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
            Vix.Verify(v => v.EnableSharedFolders());
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingAddSharedFolderWillThrowIfEnableSharingReturnsAnError()
        {
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.EnableSharedFolders()).Throws(new VixException(""));
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingAddSharedFolderWillThrowIfaddingShareReturnsAnError()
        {
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            Vix.Setup(v => v.AddShareFolder("c:\\myfolder", "myfolder")).Throws(new VixException(""));
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingRemoveSharedFolderWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.RemoveSharedFolder("c:\\nonexisting.vmx", "myshare");
        }

        [TestMethod]
        public void CallingRemoveSharedFolderWillMakeCalltoVix()
        {
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.RemoveSharedFolder("c:\\existing.vmx", "myshare");
            Vix.Verify(v => v.RemoveSharedFolder("myshare"));
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingRemoveSharedFolderWillThrowIfVMRunReturnsAnError()
        {
            Vix.Setup(v => v.PowerState()).Returns(VixPowerState.Ready);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.RemoveSharedFolder("myshare")).Throws(new VixException(""));
            Hypervisor.RemoveSharedFolder("c:\\existing.vmx", "myshare");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingCreateSnapshotWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.CreateSnapshot("c:\\nonexisting.vmx", "mysnapshot");
        }

        [TestMethod]
        public void CallingCreateSnapshotWillCallVix()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.CreateSnapshot("c:\\existing.vmx", "mysnapshot");
            Vix.Verify(v => v.CreateSnapshot("mysnapshot", It.IsAny<string>(), It.IsAny<bool>()));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingRemoveSnapshotWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.RemoveSnapshot("c:\\nonexisting.vmx", "mysnapshot");
        }

        [TestMethod]
        public void CallingRemoveSnapshotWillCallVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.RemoveSnapshot("c:\\existing.vmx", "mysnapshot");
            Vix.Verify(v => v.RemoveSnapshot("mysnapshot"));
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingRemoveSnapshotWillThrowIfVixReturnsError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.RemoveSnapshot("mysnapshot")).Throws(new VixException(""));
            Hypervisor.RemoveSnapshot("c:\\existing.vmx", "mysnapshot");
        }

        //

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingRevertToSnapshotWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.RevertToSnapshot("c:\\nonexisting.vmx", "mysnapshot");
        }

        [TestMethod]
        public void CallingRevertToSnapshotWillCallVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.RevertToSnapshot("c:\\existing.vmx", "mysnapshot");
            Vix.Verify(v => v.RevertToSnapshot("mysnapshot"));
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingRevertToSnapshotWillThrowIfVixReturnsError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.RevertToSnapshot("mysnapshot")).Throws(new VixException(""));
            Hypervisor.RevertToSnapshot("c:\\existing.vmx", "mysnapshot");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingGetSnapshotsWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.GetSnapshots("c:\\nonexisting.vmx");
        }

        [TestMethod]
        public void CallingGetSnapshotsWillCallVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.GetSnapshots("c:\\existing.vmx");
            Vix.Verify(v => v.GetSnapshots());
        }

        [TestMethod]
        public void CallingGetSnapshotsWillReturnAnArrayOfSnapshots()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.GetSnapshots()).Returns(new[] {"Base", "Template"});
            var snapshots = Hypervisor.GetSnapshots("c:\\existing.vmx");

            Assert.IsTrue(snapshots.Contains("Base"));
            Assert.IsTrue(snapshots.Contains("Template"));
        }

        [TestMethod]
        [ExpectedException(typeof (VixException))]
        public void CallingGetSnapshotWillThrowIfVMRunReturnsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Vix.Setup(v => v.GetSnapshots()).Throws(new VixException(""));
            Hypervisor.GetSnapshots("c:\\existing.vmx");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingConvertToFullDiskWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ConvertToFullDisk("c:\\nonexisting.vmx");
        }

        [TestMethod]
        public void CallingConvertToFullDiskWillGetListOfAllHardDisksInVMXFolder()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.ConvertToFullDisk("c:\\existing.vmx");
            FileSystem.Verify(f => f.GetSubFiles("c:\\"));
        }

        [TestMethod]
        public void CallingConvertToFullDiskWillExecuteDiskCommandsAgainstVMDKFilesInFolder()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.GetSubFiles("c:\\"))
                .Returns(new[] {"C:\\existing.vmx", "c:\\mydisk.vmdk", "someother.log", "c:\\anotherdisk.vmdk"});
            Hypervisor.ConvertToFullDisk("c:\\existing.vmx");
            VMwareDiskExe.Verify(v => v.Execute("-r \"c:\\mydisk.vmdk\" \"c:\\mydisk-full.vmdk\""));
            VMwareDiskExe.Verify(v => v.Execute("-r \"c:\\anotherdisk.vmdk\" \"c:\\anotherdisk-full.vmdk\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VDiskManException))]
        public void CallingConvertToFullDiskWillThrowIfDiskReturnsError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.GetSubFiles("c:\\"))
                .Returns(new[] {"C:\\existing.vmx", "c:\\mydisk.vmdk", "someother.log", "c:\\anotherdisk.vmdk"});
            VMwareDiskExe.Setup(v => v.Execute("-r \"c:\\mydisk.vmdk\" \"c:\\mydisk-full.vmdk\""))
                .Returns("Error: Some random unknown error");
            VMwareDiskExe.Setup(v => v.Execute("-r \"c:\\anotherdisk.vmdk\" \"c:\\anotherdisk-full.vmdk\""))
                .Returns("Error: Some random unknown error");
            Hypervisor.ConvertToFullDisk("c:\\existing.vmx");
        }

        [TestMethod]
        public void CallingConvertToFullDiskWillDeleteExistingDisk()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.GetSubFiles("c:\\"))
                .Returns(new[] {"C:\\existing.vmx", "c:\\mydisk.vmdk", "someother.log", "c:\\anotherdisk.vmdk"});
            VMwareDiskExe.Setup(v => v.Execute("-r \"c:\\mydisk.vmdk\" \"c:\\mydisk-full.vmdk\"")).Returns(string.Empty);
            VMwareDiskExe.Setup(v => v.Execute("-r \"c:\\anotherdisk.vmdk\" \"c:\\anotherdisk-full.vmdk\""))
                .Returns(string.Empty);
            Hypervisor.ConvertToFullDisk("c:\\existing.vmx");
            FileSystem.Verify(f => f.DeleteFile("c:\\mydisk.vmdk"));
            FileSystem.Verify(f => f.DeleteFile("c:\\anotherdisk.vmdk"));
        }

        [TestMethod]
        public void CallingConvertToFullDiskWillRenameNewDiskToOldDiskName()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.GetSubFiles("c:\\"))
                .Returns(new[] {"C:\\existing.vmx", "c:\\mydisk.vmdk", "someother.log", "c:\\anotherdisk.vmdk"});
            VMwareDiskExe.Setup(v => v.Execute("-r \"c:\\mydisk.vmdk\" \"c:\\mydisk-full.vmdk\"")).Returns(string.Empty);
            VMwareDiskExe.Setup(v => v.Execute("-r \"c:\\anotherdisk.vmdk\" \"c:\\anotherdisk-full.vmdk\""))
                .Returns(string.Empty);
            Hypervisor.ConvertToFullDisk("c:\\existing.vmx");
            FileSystem.Verify(f => f.MoveFile("c:\\mydisk-full.vmdk", "c:\\mydisk.vmdk"));
            FileSystem.Verify(f => f.MoveFile("c:\\anotherdisk-full.vmdk", "c:\\anotherdisk.vmdk"));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingGetFloppyIDWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.GetFreeFloppyID("c:\\nonexisting.vmx");
        }

        [TestMethod]
        public void CallingGetFloppyIDWithNoFloppiesInVMXWillReturnZero()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\""));
            Assert.IsTrue(Hypervisor.GetFreeFloppyID("c:\\existing.vmx") == 0);
        }

        [TestMethod]
        public void CallingGetFloppyIDWithExistingFloppiesWillReturnOneAboveHighestFound()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\"",
                    "floppy0.present = \"TRUE\"", "floppy1.present = \"TRUE\""));
            Assert.IsTrue(Hypervisor.GetFreeFloppyID("c:\\existing.vmx") == 2);
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingGetFreeDiskIDWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.GetFreeDiskID("c:\\nonexisting.vmx", "ide");
        }

        [TestMethod]
        [ExpectedException(typeof (BadBusTypeException))]
        public void CallingGetFreeDiskIDWillThrowIfInvalidBusIsPassed()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.GetFreeDiskID("c:\\existing.vmx", "notsataideorscsi");
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithIdeWillReturnZeroZeroIfNoDisksExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "ide");

            Assert.IsTrue(value.Item1 == 0);
            Assert.IsTrue(value.Item2 == 0);
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithIdeWillReturnOneAboveExistingDiskId()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\"",
                    "ide0:0.present = \"TRUE\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "ide");

            Assert.IsTrue(value.Item1 == 0);
            Assert.IsTrue(value.Item2 == 1);
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithIdeWillIncrementBusIDAfterTwoDisksAreOnTheSameBus()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\"",
                    "ide0:0.present = \"TRUE\"", "ide0:1.present = \"TRUE\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "ide");

            Assert.IsTrue(value.Item1 == 1);
            Assert.IsTrue(value.Item2 == 0);
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithSataWillReturnZeroZeroIfNoDisksExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "sata");

            Assert.IsTrue(value.Item1 == 0);
            Assert.IsTrue(value.Item2 == 0);
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithSataWillReturnNextDiskID()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\"", "sata0:0.present = \"TRUE\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "sata");

            Assert.IsTrue(value.Item1 == 0);
            Assert.IsTrue(value.Item2 == 1);
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithSataWillReturnNextDiskIDOnNewBusWhenMaximumDiskForAdapterIsReached()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\"", "sata0:0.present = \"TRUE\"", "sata0:29.present = \"TRUE\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "sata");

            Assert.IsTrue(value.Item1 == 1);
            Assert.IsTrue(value.Item2 == 0);
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithSCSIWillReturnZeroZeroIfNoDiskExists()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "scsi");

            Assert.IsTrue(value.Item1 == 0);
            Assert.IsTrue(value.Item2 == 0);
        }

        [TestMethod]
        public void CallingGetFreeDiskIDWithSCSIWillReturnNextIDOnExistingDisk()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\"", "scsi0:0.present = \"TRUE\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "scsi");

            Assert.IsTrue(value.Item1 == 0);
            Assert.IsTrue(value.Item2 == 1);
        }

        [TestMethod]
        public void CallingGetfreeDiskIDWithSCSIWillReturnIDOnNewBusWhenMaximumDiskForAdapterIsReached()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "somesetting = \"foo\"", "anothersetting = \"somethingelse\"", "scsi0:0.present = \"TRUE\"", "scsi0:14.present = \"TRUE\""));

            var value = Hypervisor.GetFreeDiskID("c:\\existing.vmx", "scsi");

            Assert.IsTrue(value.Item1 == 1);
            Assert.IsTrue(value.Item2 == 0);
        }

        [TestMethod]
        public void CallingCreateVMDKWillCallVMdisk()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            Hypervisor.CreateVMDK("c:\\newdisk.vmdk", 100, "ide");
            VMwareDiskExe.Verify(v => v.Execute("-c -s 100MB -a ide -t 0 \"c:\\newdisk.vmdk\""));
        }

        [TestMethod]
        [ExpectedException(typeof(VDiskManException))]
        public void CallingCreateVMDKWillThrowIfErrorIsReturned()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            VMwareDiskExe.Setup(v => v.Execute("-c -s 100MB -a ide -t 0 \"c:\\newdisk.vmdk\"")).Returns("Error: Some unknown error message!");
            Hypervisor.CreateVMDK("c:\\newdisk.vmdk", 100, "ide");
        }

        [TestMethod]
        [ExpectedException(typeof (FileNotFoundException))]
        public void CallingCreateVMDKWillThrowIfParentFolderOfDiskPathDoesntExist()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(false);
            Hypervisor.CreateVMDK("c:\\newdisk.vmdk", 100, "ide");
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CallingCreateVMDKWillThrowIfDiskTypeIsInvalid()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            Hypervisor.CreateVMDK("c:\\newdisk.vmdk", 100, "Not ide buslogic or lsilogic");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingClearCDRomWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ClearCDRom("c:\\nonexisting.vmx");
        }

        [TestMethod]
        public void CallingClearCDRomWillRemoveAllRawCDRomsFromVMX()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "sata0.present = \"TRUE\"", "othersetting = \"something\"", "sata0:1.present = \"TRUE\"",
                    "sata0:1.deviceType = \"cdrom-raw\""));
            
            Hypervisor.ClearCDRom("c:\\existing.vmx");
            FileSystem.Verify(f =>f.SetFile("c:\\existing.vmx", string.Join(Environment.NewLine, "sata0.present = \"TRUE\"", "othersetting = \"something\"")));
        }

        [TestMethod]
        public void CallingClearCDRomWillRemoveISOAllCDRomsFromVMX()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "sata0.present = \"TRUE\"", "othersetting = \"something\"", "sata0:1.present = \"TRUE\"",
                    "sata0:1.deviceType = \"cdrom-image\""));

            Hypervisor.ClearCDRom("c:\\existing.vmx");
            FileSystem.Verify(f => f.SetFile("c:\\existing.vmx", string.Join(Environment.NewLine, "sata0.present = \"TRUE\"", "othersetting = \"something\"")));
        }

        [TestMethod]
        [ExpectedException(typeof(VMXDoesntExistException))]
        public void CallingClearNetworkSettingsWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ClearNetworkSettings("c:\\nonexisting.vmx");
        }

        [TestMethod]
        public void CallingClearNetworkSettingsWillRemoveAllNetworksSettingsFromVMX()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "ethernet0.present = \"TRUE\"", "othersetting = \"something\"", "ethernet1.present = \"TRUE\"",
                    "ethernet0.connectionType = \"custom\""));

            Hypervisor.ClearNetworkSettings("c:\\existing.vmx");
            FileSystem.Verify(f => f.SetFile("c:\\existing.vmx", string.Join(Environment.NewLine, "othersetting = \"something\"")));
        }

        [TestMethod]
        [ExpectedException(typeof(VMXDoesntExistException))]
        public void CallingClearFloppyWillThrowIfVMXDoesntExist()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexisting.vmx")).Returns(false);
            Hypervisor.ClearFloppy("c:\\nonexisting.vmx");
        }

        [TestMethod]
        public void CallingClearFloppyWillRemoveAllFloppyDrivesFromVMX()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "floppy0.present = \"TRUE\"", "othersetting = \"something\"", "floppy1.present = \"TRUE\"",
                    "floppy0.somesetting = \"custom\""));

            Hypervisor.ClearFloppy("c:\\existing.vmx");
            FileSystem.Verify(f => f.SetFile("c:\\existing.vmx", string.Join(Environment.NewLine, "othersetting = \"something\"", "floppy0.present = \"FALSE\"")));
        }
    }
}
