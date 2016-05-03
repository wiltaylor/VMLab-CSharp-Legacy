using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Test.Drivers
{
    [TestClass]
    public class VMwareHyperVisorTest
    {
        public IVMwareHypervisor Hypervisor;
        public Mock<IFileSystem> FileSystem;
        public Mock<IVMRun> VMrun;
        public Mock<IVMwareExe> VMwareExe;
        public Mock<IVMwareDiskExe> VMwareDiskExe;

        [TestInitialize()]
        public void Setup()
        {
            VMrun = new Mock<IVMRun>();
            FileSystem = new Mock<IFileSystem>();
            VMwareExe = new Mock<IVMwareExe>();
            VMwareDiskExe = new Mock<IVMwareDiskExe>();
            Hypervisor = new VMwareHypervisor(VMrun.Object, FileSystem.Object, VMwareExe.Object, VMwareDiskExe.Object);


            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\existing.vmx"))
                .Returns(string.Join(Environment.NewLine, "settingname = \"value\"", "anothersetting = \"anothervalue\""));
        }

        [TestCleanup]
        public void TearDown()
        {

        }

        [TestMethod]
        public void CanCreateLinkedCloneByCallingVMRun()
        {
            FileSystem.Setup(f => f.FileExists("Path\\ToExisting\\VM.vmx")).Returns(true);
            VMrun.Setup(
                m =>
                    m.Execute(
                        "clone \"Path\\ToExisting\\VM.vmx\" \"Path\\ToNewLocation\\vm.vmx\" linked -snapshot=\"Snapshot\""))
                .Returns("Cloned ok");
            Hypervisor.Clone("Path\\ToExisting\\VM.vmx", "Path\\ToNewLocation\\vm.vmx", "Snapshot", CloneType.Linked);
            VMrun.Verify(
                m =>
                    m.Execute(
                        "clone \"Path\\ToExisting\\VM.vmx\" \"Path\\ToNewLocation\\vm.vmx\" linked -snapshot=\"Snapshot\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingCloneOnVMThatDoesntExistWillThrow()
        {
            VMrun.Setup(
                m =>
                    m.Execute(
                        "clone \"c:\\vmthatdoesntexists.vmx\" \"c:\\Path\\ToNewLocation\\vm.vmx\" linked -snapshot=\"Snapshot\""))
                .Returns("Error: Cannot open VM: c:\\vmthatdoesntexists.vmx, The virtual machine cannot be found");
            Hypervisor.Clone("c:\\vmthatdoesntexists.vmx", "Path\\ToNewLocation\\vm.vmx", "Snapshot", CloneType.Linked);
        }

        [TestMethod]
        [ExpectedException(typeof (SnapshotDoesntExistException))]
        public void CallingCloneOnVMWithSnapshotThatDoesntExistsWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("Path\\ToExisting\\VM.vmx")).Returns(true);
            VMrun.Setup(
                m =>
                    m.Execute(
                        "clone \"Path\\ToExisting\\VM.vmx\" \"Path\\ToNewLocation\\vm.vmx\" linked -snapshot=\"NonExistingSnapshot\""))
                .Returns("Error: Invalid snapshot name 'NonExistingSnapshot'");
            Hypervisor.Clone("Path\\ToExisting\\VM.vmx", "Path\\ToNewLocation\\vm.vmx", "NonExistingSnapshot",
                CloneType.Linked);
        }

        [TestMethod]
        [ExpectedException(typeof (VMXAlreadyExistsException))]
        public void CallingCloneWithVMTargetThatAlreadyExistsWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("Path\\ToExisting\\VM.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("Path\\ToNewLocation\\vm.vmx")).Returns(true);
            VMrun.Setup(
                m =>
                    m.Execute(
                        "clone \"Path\\ToExisting\\VM.vmx\" \"Path\\ToNewLocation\\vm.vmx\" linked -snapshot=\"NonExistingSnapshot\""))
                .Returns("Error: Invalid snapshot name 'NonExistingSnapshot'");
            Hypervisor.Clone("Path\\ToExisting\\VM.vmx", "Path\\ToNewLocation\\vm.vmx", "NonExistingSnapshot",
                CloneType.Linked);
        }

        [TestMethod]
        public void CAllingCloneWithFullFlagWillCallVMRunWithExpectedSwitch()
        {
            FileSystem.Setup(f => f.FileExists("Path\\ToExisting\\VM.vmx")).Returns(true);
            VMrun.Setup(
                m =>
                    m.Execute(
                        "clone \"Path\\ToExisting\\VM.vmx\" \"Path\\ToNewLocation\\vm.vmx\" full -snapshot=\"Snapshot\""))
                .Returns("Cloned ok");
            Hypervisor.Clone("Path\\ToExisting\\VM.vmx", "Path\\ToNewLocation\\vm.vmx", "Snapshot", CloneType.Full);
            VMrun.Verify(
                m =>
                    m.Execute(
                        "clone \"Path\\ToExisting\\VM.vmx\" \"Path\\ToNewLocation\\vm.vmx\" full -snapshot=\"Snapshot\""));
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
            VMrun.Setup(v => v.Execute("list")).Returns("Total running VMs: 0");
            Assert.IsTrue(Hypervisor.GetRunningVMs().Length == 0);
        }

        [TestMethod]
        public void CallingGetRunningVMsWillReturnAvmNameWhenOneIsRunning()
        {
            VMrun.Setup(v => v.Execute("list"))
                .Returns(string.Join(Environment.NewLine, "Total running VMs: 1", "c:\\myvm\\myvm.vmx"));
            Assert.IsTrue(Hypervisor.GetRunningVMs().Contains("c:\\myvm\\myvm.vmx"));
        }

        [TestMethod]
        public void CallingFileExistInGuestReturnTrueIfFileExists()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("The file exists.");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Assert.IsTrue(Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object},
                "c:\\windows\\explorer.exe"));
        }

        [TestMethod]
        public void CallingFileExistInGuestReturnFalseIfFileDoesntExist()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("The file does not exist.");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Assert.IsFalse(Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object},
                "c:\\windows\\explorer.exe"));
        }

        [TestMethod]
        public void CallingFileExistsWithBadFirstCredentialButGoodSecondCredentialsReturnsTrueIfFileExists()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("The file exists.");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("Error: Invalid user name or password for the guest OS.");
            var goodcreds = new Mock<IVMCredential>();
            goodcreds.Setup(c => c.Username).Returns("user");
            goodcreds.Setup(c => c.Password).Returns("password");
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");
            Assert.IsTrue(Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {badcreds.Object, goodcreds.Object},
                "c:\\windows\\explorer.exe"));
        }

        [TestMethod]
        [ExpectedException(typeof (BadGuestCredentialsException))]
        public void CallingFileExistsWithAllBadPasswordsThrowsAnException()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("Error: Invalid user name or password for the guest OS.");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("Error: Invalid user name or password for the guest OS.");
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("user");
            badcreds.Setup(c => c.Password).Returns("password");
            var anotherbadcreds = new Mock<IVMCredential>();
            anotherbadcreds.Setup(c => c.Username).Returns("baduser");
            anotherbadcreds.Setup(c => c.Password).Returns("badpassword");
            Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {anotherbadcreds.Object, badcreds.Object},
                "c:\\windows\\explorer.exe");
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingFileExistsWithAnotherTypeOfErrorReturnsApplicationException()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("Error: Something else.");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object}, "c:\\windows\\explorer.exe");
        }

        [TestMethod]
        [ExpectedException(typeof (GuestVMPoweredOffException))]
        public void CallingFileExistsWhenVMIsPoweredOffWillThrowA()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password fileExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\\explorer.exe\""))
                .Returns("Error: The virtual machine is not powered on: \"c:\\myvm\\myvm.vmx\"");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Hypervisor.FileExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object}, "c:\\windows\\explorer.exe");
        }

        [TestMethod]
        public void CallingDirectoryExistInGuestReturnTrueIfFileExists()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\""))
                .Returns("The directory exists.");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Assert.IsTrue(Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object}, "c:\\windows"));
        }

        [TestMethod]
        public void CallingDirectoryExistInGuestReturnFalseIfFileDoesntExist()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\folderthatdoesntexist\""))
                .Returns("The directory does not exist.");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Assert.IsFalse(Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object},
                "c:\\folderthatdoesntexist"));
        }

        [TestMethod]
        public void CallingDirectoryExistsWithBadFirstCredentialButGoodSecondCredentialsReturnsTrueIfFileExists()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\""))
                .Returns("The directory exists.");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\""))
                .Returns("Error: Invalid user name or password for the guest OS.");
            var goodcreds = new Mock<IVMCredential>();
            goodcreds.Setup(c => c.Username).Returns("user");
            goodcreds.Setup(c => c.Password).Returns("password");
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");
            Assert.IsTrue(Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx",
                new[] {badcreds.Object, goodcreds.Object}, "c:\\windows"));
        }

        [TestMethod]
        [ExpectedException(typeof (BadGuestCredentialsException))]
        public void CallingDirectoryExistsWithAllBadPasswordsThrowsAnException()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\""))
                .Returns("Error: Invalid user name or password for the guest OS.");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\""))
                .Returns("Error: Invalid user name or password for the guest OS.");
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("user");
            badcreds.Setup(c => c.Password).Returns("password");
            var anotherbadcreds = new Mock<IVMCredential>();
            anotherbadcreds.Setup(c => c.Username).Returns("baduser");
            anotherbadcreds.Setup(c => c.Password).Returns("badpassword");
            Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx", new[] {anotherbadcreds.Object, badcreds.Object},
                "c:\\windows");
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingDirectoryExistsWithAnotherTypeOfErrorReturnsApplicationException()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\""))
                .Returns("Error: Something else.");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object}, "c:\\windows");
        }

        [TestMethod]
        [ExpectedException(typeof (GuestVMPoweredOffException))]
        public void CallingDirectoryExistsWhenVMIsPoweredOffWillThrowA()
        {
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password directoryExistsInGuest \"c:\\myvm\\myvm.vmx\" \"c:\\windows\""))
                .Returns("Error: The virtual machine is not powered on: \"c:\\myvm\\myvm.vmx\"");
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            Hypervisor.DirectoryExistInGuest("c:\\myvm\\myvm.vmx", new[] {creds.Object}, "c:\\windows");
        }

        [TestMethod]
        [ExpectedException(typeof (VMXDoesntExistException))]
        public void CallingStartVMWithNonExistingVMXWillThrow()
        {
            FileSystem.Setup(f => f.FileExists("c:\\nonexistingvm.vmx")).Returns(false);

            Hypervisor.StartVM("c:\\nonexistingvm.vmx");
        }

        [TestMethod]
        public void CallingStartVMForExistingVMWillResultInStartedVmbyVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("start \"c:\\existingvm.vmx\" nogui")).Returns(string.Empty);
            Hypervisor.StartVM("c:\\existingvm.vmx");
            VMrun.Verify(v => v.Execute("start \"c:\\existingvm.vmx\" nogui"));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingStartVMForExistingVMWillThrowIfThereIsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("start \"c:\\existingvm.vmx\" nogui")).Returns("Error: Some error.");
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
            VMrun.Setup(v => v.Execute("stop \"c:\\existingvm.vmx\" soft")).Returns(string.Empty);
            Hypervisor.StopVM("c:\\existingvm.vmx", false);
            VMrun.Verify(v => v.Execute("stop \"c:\\existingvm.vmx\" soft"));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingStopVMForExistingVMWillThrowIfThereIsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("stop \"c:\\existingvm.vmx\" soft")).Returns("Error: Some error.");
            Hypervisor.StopVM("c:\\existingvm.vmx", false);
        }

        [TestMethod]
        public void CallingStopVMWithForceWillRunExpectedCommandLineWithVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("stop \"c:\\existingvm.vmx\" hard")).Returns(string.Empty);
            Hypervisor.StopVM("c:\\existingvm.vmx", true);
            VMrun.Verify(v => v.Execute("stop \"c:\\existingvm.vmx\" hard"));
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
            VMrun.Setup(v => v.Execute("reset \"c:\\existingvm.vmx\" soft")).Returns(string.Empty);
            Hypervisor.ResetVM("c:\\existingvm.vmx", false);
            VMrun.Verify(v => v.Execute("reset \"c:\\existingvm.vmx\" soft"));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingResetVMForExistingVMWillThrowIfThereIsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("reset \"c:\\existingvm.vmx\" soft")).Returns("Error: Some error.");
            Hypervisor.ResetVM("c:\\existingvm.vmx", false);
        }

        [TestMethod]
        public void CallingResetVMWithForceWillRunExpectedCommandLineWithVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existingvm.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("reset \"c:\\existingvm.vmx\" hard")).Returns(string.Empty);
            Hypervisor.ResetVM("c:\\existingvm.vmx", true);
            VMrun.Verify(v => v.Execute("reset \"c:\\existingvm.vmx\" hard"));
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
        public void CallingRemoveVMOnNonExistingVMWillNotCallVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\nonexisting.vmx")).Returns(false);
            Hypervisor.RemoveVM("c:\\vmfolder\\vmguid\\nonexisting.vmx");
            VMrun.Verify(v => v.Execute("deleteVM \"c:\\vmfolder\\vmguid\\nonexisting.vmx\""), Times.Never);
        }

        [TestMethod]
        public void CallingRemoveVMWillMakeCallToVMRunToRemoveVM()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("deleteVM \"c:\\vmfolder\\vmguid\\existing.vmx\"")).Returns(string.Empty);
            Hypervisor.RemoveVM("c:\\vmfolder\\vmguid\\existing.vmx");
            VMrun.Verify(v => v.Execute("deleteVM \"c:\\vmfolder\\vmguid\\existing.vmx\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingRemoveVMWillThrowIfVMRunReturnsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("deleteVM \"c:\\vmfolder\\vmguid\\existing.vmx\"")).Returns("Error: Some error");
            Hypervisor.RemoveVM("c:\\vmfolder\\vmguid\\existing.vmx");
        }

        [TestMethod]
        public void CallingRemoveVMWillAlsoDeleteVMFolder()
        {
            FileSystem.Setup(f => f.FileExists("c:\\vmfolder\\vmguid\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\vmfolder")).Returns(true);
            VMrun.Setup(v => v.Execute("deleteVM \"c:\\vmfolder\\vmguid\\existing.vmx\"")).Returns(string.Empty);
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
            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\existinghostfile.txt",
                "c:\\onvm\\somefile.txt");

            VMrun.Verify(
                v =>
                    v.Execute(
                        $"-T ws -gu user -gp password CopyFileFromHostToGuest \"c:\\existing.vmx\" \"c:\\existinghostfile.txt\" \"c:\\onvm\\somefile.txt\""));
        }

        [TestMethod]
        [ExpectedException(typeof (FileDoesntExistInGuest))]
        public void CallingCopyFileWithABadGuestPathWillThrow()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existinghostfile.txt")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        $"-T ws -gu user -gp password CopyFileFromHostToGuest \"c:\\existing.vmx\" \"c:\\existinghostfile.txt\" \"c:\\badvm\\somefile.txt\""))
                .Returns("Error: A file was not found");
            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\existinghostfile.txt",
                "c:\\badvm\\somefile.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingCopyFileWillThrowIfAnUnknownErrorOccors()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existinghostfile.txt")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        $"-T ws -gu user -gp password CopyFileFromHostToGuest \"c:\\existing.vmx\" \"c:\\existinghostfile.txt\" \"c:\\badvm\\somefile.txt\""))
                .Returns("Error: something else!");
            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\existinghostfile.txt",
                "c:\\badvm\\somefile.txt");
        }

        [TestMethod]
        public void CallingCopyFileWillBeSuccsefulIfThereIsOneGoodSetOfCredentialsInArray()
        {
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");
            var goodcreds = new Mock<IVMCredential>();
            goodcreds.Setup(c => c.Username).Returns("gooduser");
            goodcreds.Setup(c => c.Password).Returns("goodpassword");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existinghostfile.txt")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        $"-T ws -gu baduser -gp badpassword CopyFileFromHostToGuest \"c:\\existing.vmx\" \"c:\\existinghostfile.txt\" \"c:\\badvm\\somefile.txt\""))
                .Returns("Error: Invalid user name or password for the guest OS");
            VMrun.Setup(
                v =>
                    v.Execute(
                        $"-T ws -gu gooduser -gp goodpassword CopyFileFromHostToGuest \"c:\\existing.vmx\" \"c:\\existinghostfile.txt\" \"c:\\badvm\\somefile.txt\""));

            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {badcreds.Object, goodcreds.Object},
                "c:\\existinghostfile.txt", "c:\\badvm\\somefile.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (BadGuestCredentialsException))]
        public void CallingCopyFileWithArrayFullOfBadPasswordsWillThrow()
        {
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");
            var anotherbadcreds = new Mock<IVMCredential>();
            anotherbadcreds.Setup(c => c.Username).Returns("anotherbaduser");
            anotherbadcreds.Setup(c => c.Password).Returns("anotherbadpassword");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existinghostfile.txt")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        $"-T ws -gu baduser -gp badpassword CopyFileFromHostToGuest \"c:\\existing.vmx\" \"c:\\existinghostfile.txt\" \"c:\\badvm\\somefile.txt\""))
                .Returns("Error: Invalid user name or password for the guest OS");
            VMrun.Setup(
                v =>
                    v.Execute(
                        $"-T ws -gu anotherbaduser -gp anotherbadpassword CopyFileFromHostToGuest \"c:\\existing.vmx\" \"c:\\existinghostfile.txt\" \"c:\\badvm\\somefile.txt\""))
                .Returns("Error: Invalid user name or password for the guest OS");
            ;

            Hypervisor.CopyFileToGuest("c:\\existing.vmx", new[] {badcreds.Object, anotherbadcreds.Object},
                "c:\\existinghostfile.txt", "c:\\badvm\\somefile.txt");
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
        public void CallingCopyFileFromGuestWillMakeCallToVMRunToCopyFile()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password CopyFileFromGuestToHost \"c:\\existing.vmx\" \"c:\\onvm\\somefile.txt\" \"c:\\somefile.txt\""))
                .Returns(string.Empty);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            Hypervisor.CopyFileFromGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\onvm\\somefile.txt",
                "c:\\somefile.txt");
            VMrun.Verify(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password CopyFileFromGuestToHost \"c:\\existing.vmx\" \"c:\\onvm\\somefile.txt\" \"c:\\somefile.txt\""));
        }

        [TestMethod]
        [ExpectedException(typeof (FileDoesntExistInGuest))]
        public void CallingCopyFileFromGuestWillThrowIfFileDoesntExistInGuest()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password CopyFileFromGuestToHost \"c:\\existing.vmx\" \"c:\\badpath\\somefile.txt\" \"c:\\somefile.txt\""))
                .Returns("Error: A file was not found");
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.CopyFileFromGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\badpath\\somefile.txt",
                "c:\\somefile.txt");
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
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingCopyFromGuestWillThrowIfErrorIsReturned()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password CopyFileFromGuestToHost \"c:\\existing.vmx\" \"c:\\somefile.txt\" \"c:\\somefile.txt\""))
                .Returns("Error: Some other unknown error");
            Hypervisor.CopyFileFromGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\somefile.txt",
                "c:\\somefile.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (BadGuestCredentialsException))]
        public void CallingCopyFromGuestWithBadCredentialsWillThrow()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("badpassword");
            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp badpassword CopyFileFromGuestToHost \"c:\\existing.vmx\" \"c:\\somefile.txt\" \"c:\\somefile.txt\""))
                .Returns("Error: Invalid user name or password for the guest OS");
            Hypervisor.CopyFileFromGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\somefile.txt",
                "c:\\somefile.txt");
        }

        [TestMethod]
        public void CallingCopyFromGuestWithAnArrayOfCredentialsSomeGoodSomeBadWillSucceed()
        {
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");
            var goodcreds = new Mock<IVMCredential>();
            goodcreds.Setup(c => c.Username).Returns("gooduser");
            goodcreds.Setup(c => c.Password).Returns("goodpassword");

            FileSystem.Setup(f => f.FolderExists("c:\\")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword CopyFileFromGuestToHost \"c:\\existing.vmx\" \"c:\\somefile.txt\" \"c:\\somefile.txt\""))
                .Returns("Error: Invalid user name or password for the guest OS");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu gooduser -gp goodpassword CopyFileFromGuestToHost \"c:\\existing.vmx\" \"c:\\somefile.txt\" \"c:\\somefile.txt\""))
                .Returns(string.Empty);

            Hypervisor.CopyFileFromGuest("c:\\existing.vmx", new[] {badcreds.Object, goodcreds.Object},
                "c:\\somefile.txt", "c:\\somefile.txt");
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
        public void CallingDeleteFileOnVMWillMakeACalltoVMRun()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.DeleteFileInGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\testfileinguest.txt");
            VMrun.Verify(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password deleteFileInGuest \"c:\\existing.vmx\" \"c:\\testfileinguest.txt\""));
        }

        [TestMethod]
        [ExpectedException(typeof (FileDoesntExistInGuest))]
        public void CallingDeleteFileWillThrowIfFileDoesntExistInGuest()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password deleteFileInGuest \"c:\\existing.vmx\" \"c:\\badfilepath.txt\""))
                .Returns("Error: A file was not found");
            Hypervisor.DeleteFileInGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\badfilepath.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingDeleteFileWillThrowIfUnknownErrorIsReturned()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password deleteFileInGuest \"c:\\existing.vmx\" \"c:\\guestfilepath.txt\""))
                .Returns("Error: Some unknown error was returned.");
            Hypervisor.DeleteFileInGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\guestfilepath.txt");
        }

        [TestMethod]
        [ExpectedException(typeof (BadGuestCredentialsException))]
        public void CallingDeleteFileWillThrowIfCredentialsAreIncorrect()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("baduser");
            creds.Setup(c => c.Password).Returns("badpassword");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword deleteFileInGuest \"c:\\existing.vmx\" \"c:\\guestfilepath.txt\""))
                .Returns("Error: Invalid user name or password for the guest OS");
            Hypervisor.DeleteFileInGuest("c:\\existing.vmx", new[] {creds.Object}, "c:\\guestfilepath.txt");
        }

        [TestMethod]
        public void CallingDeleteFileWillNotThrowWithMixOfGoodAndBadCredentials()
        {
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");

            var goodcreds = new Mock<IVMCredential>();
            goodcreds.Setup(c => c.Username).Returns("gooduser");
            goodcreds.Setup(c => c.Password).Returns("goodpassword");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword deleteFileInGuest \"c:\\existing.vmx\" \"c:\\guestfilepath.txt\""))
                .Returns("Error: Invalid user name or password for the guest OS");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu gooduser -gp goodpassword deleteFileInGuest \"c:\\existing.vmx\" \"c:\\guestfilepath.txt\""))
                .Returns(string.Empty);
            Hypervisor.DeleteFileInGuest("c:\\existing.vmx", new[] {badcreds.Object, goodcreds.Object},
                "c:\\guestfilepath.txt");
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

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"))
                .Returns("Guest program exited with non-zero exit code: 1");
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", false,
                false);
            VMrun.Verify(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"));
        }

        [TestMethod]
        public void CallingExecuteCommandWillRetry5TimesIfUnknownErrorIsReturned()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"))
                .Returns($"Error: Unknown error{Environment.NewLine}");

            try
            {
                Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches",
                    false, false);
            }
            catch (VMRunFailedToRunException)
            {
                //Ignore exception: We are only interested in how many times it retried.
            }

            VMrun.Verify(v => v.Execute("-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"), Times.Exactly(5));

        }

        [TestMethod]
        public void CallingExecuteCommandWithNoWaitSetWillPassNoWaitToVMRun()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" -nowait \"c:\\myapp.exe\" -someswitches"))
                .Returns(string.Empty);
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", true,
                false);
            VMrun.Verify(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" -nowait \"c:\\myapp.exe\" -someswitches"));
        }

        [TestMethod]
        public void CallingExecuteCommandWithInteractiveSwitchWillPassSwitchToVMRun()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" -interactive \"c:\\myapp.exe\" -someswitches"))
                .Returns("Guest program exited with non-zero exit code: 1");
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", false,
                true);
            VMrun.Verify(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" -interactive \"c:\\myapp.exe\" -someswitches"));
        }

        [TestMethod]
        [ExpectedException(typeof (FileDoesntExistInGuest))]
        public void CallingExecuteCommandWillThrowIfProgramCantBeFoundInGuest()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"))
                .Returns("Error: A file was not found");
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", false,
                false);
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingExecuteCommandWillThrowIfUnknownErrorIsReturned()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"))
                .Returns("Error: Some other unknonw error");
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", false,
                false);
        }

        [TestMethod]
        [ExpectedException(typeof (BadGuestCredentialsException))]
        public void CallingExecuteCommandWillThrowIfBadGuestCredentialsArePassed()
        {
            var creds = new Mock<IVMCredential>();
            creds.Setup(c => c.Username).Returns("user");
            creds.Setup(c => c.Password).Returns("password");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu user -gp password runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"))
                .Returns("Error: Invalid user name or password for the guest OS");
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {creds.Object}, "c:\\myapp.exe", "-someswitches", false,
                false);
        }

        [TestMethod]
        public void CallingExecuteCommandWithSomeBadAndGoodCredentialsWillSuccessed()
        {
            var badcreds = new Mock<IVMCredential>();
            badcreds.Setup(c => c.Username).Returns("baduser");
            badcreds.Setup(c => c.Password).Returns("badpassword");
            var goodcreds = new Mock<IVMCredential>();
            goodcreds.Setup(c => c.Username).Returns("gooduser");
            goodcreds.Setup(c => c.Password).Returns("goodpassword");

            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu baduser -gp badpassword runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"))
                .Returns("Error: Invalid user name or password for the guest OS");
            VMrun.Setup(
                v =>
                    v.Execute(
                        "-T ws -gu gooduser -gp goodpassword runProgramInGuest \"c:\\existing.vmx\" \"c:\\myapp.exe\" -someswitches"))
                .Returns("Guest program exited with non-zero exit code: 1");
            Hypervisor.ExecuteCommand("c:\\existing.vmx", new[] {badcreds.Object, goodcreds.Object}, "c:\\myapp.exe",
                "-someswitches", false, false);
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
        public void CallingAddSharedFolderWillMakeACallToVMRunToAddShare()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
            VMrun.Verify(v => v.Execute("addSharedFolder \"c:\\existing.vmx\" \"myfolder\" c:\\myfolder"));
        }

        [TestMethod]
        public void CallingAddSharedFolderWillMakeACallToVMRunToEnableSharing()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
            VMrun.Verify(v => v.Execute("enableSharedFolders \"c:\\existing.vmx\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingAddSharedFolderWillThrowIfEnableSharingReturnsAnError()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("enableSharedFolders \"c:\\existing.vmx\""))
                .Returns("Error: Some unknown error!");
            Hypervisor.AddSharedFolder("c:\\existing.vmx", "c:\\myfolder", "myfolder");
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingAddSharedFolderWillThrowIfaddingShareReturnsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            FileSystem.Setup(f => f.FolderExists("c:\\myfolder")).Returns(true);
            VMrun.Setup(v => v.Execute("addSharedFolder \"c:\\existing.vmx\" \"myfolder\" c:\\myfolder"))
                .Returns("Error: Some unknown error!");
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
        public void CallingRemoveSharedFolderWillMakeCalltoVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.RemoveSharedFolder("c:\\existing.vmx", "myshare");
            VMrun.Verify(v => v.Execute("removeSharedFolder \"c:\\existing.vmx\" \"myshare\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingRemoveSharedFolderWillThrowIfVMRunReturnsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("removeSharedFolder \"c:\\existing.vmx\" \"myshare\""))
                .Returns("Error: Some error");
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
        public void CallingCreateSnapshotWillCallVMRun()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            Hypervisor.CreateSnapshot("c:\\existing.vmx", "mysnapshot");
            VMrun.Verify(v => v.Execute("snapshot \"c:\\existing.vmx\" \"mysnapshot\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingCreateSnapshotWillThrowIfVMRunReturnsError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("snapshot \"c:\\existing.vmx\" \"mysnapshot\""))
                .Returns("Error: Some unknown error");
            Hypervisor.CreateSnapshot("c:\\existing.vmx", "mysnapshot");
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
            VMrun.Verify(v => v.Execute("deleteSnapshot \"c:\\existing.vmx\" \"mysnapshot\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingRemoveSnapshotWillThrowIfVMRunReturnsError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("deleteSnapshot \"c:\\existing.vmx\" \"mysnapshot\""))
                .Returns("Error: Some unknown error");
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
            VMrun.Verify(v => v.Execute("revertToSnapshot \"c:\\existing.vmx\" \"mysnapshot\""));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingRevertToSnapshotWillThrowIfVMRunReturnsError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("revertToSnapshot \"c:\\existing.vmx\" \"mysnapshot\""))
                .Returns("Error: Some unknown error");
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
            VMrun.Verify(v => v.Execute("listSnapshots \"c:\\existing.vmx\""));
        }

        [TestMethod]
        public void CallingGetSnapshotsWillReturnAnArrayOfSnapshots()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("listSnapshots \"c:\\existing.vmx\""))
                .Returns(string.Join(Environment.NewLine, "Total snapshots: 2", "Base", "Template"));
            var snapshots = Hypervisor.GetSnapshots("c:\\existing.vmx");

            Assert.IsTrue(snapshots.Contains("Base"));
            Assert.IsTrue(snapshots.Contains("Template"));
            Assert.IsFalse(snapshots.Contains("Total snapshots: 2"));
        }

        [TestMethod]
        [ExpectedException(typeof (VMRunFailedToRunException))]
        public void CallingGetSnapshotWillThrowIfVMRunReturnsAnError()
        {
            FileSystem.Setup(f => f.FileExists("c:\\existing.vmx")).Returns(true);
            VMrun.Setup(v => v.Execute("listSnapshots \"c:\\existing.vmx\"")).Returns("Error: Unknown error!");
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
