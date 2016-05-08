using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using Moq;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Test.Model
{

    [TestClass]
    public class EnvironmentDetailsTest
    {
        public IEnvironmentDetails Environment;
        public Mock<ICmdletPathHelper> CmdletHelper;
        public Mock<IFileSystem> FileSystem;
        public Mock<IRegistryHelper> RegistryHelper;

        [TestInitialize()]
        public void Setup()
        {
            FileSystem = new Mock<IFileSystem>();
            CmdletHelper = new Mock<ICmdletPathHelper>();
            RegistryHelper = new Mock<IRegistryHelper>();

            Environment = new EnvironmentDetails(FileSystem.Object, CmdletHelper.Object, RegistryHelper.Object);

        }

        [TestMethod]
        public void CallingUniqueFolderWillReturnADifferentResultEachTime()
        {
            Assert.AreNotEqual(Environment.UniqueIdentifier(), Environment.UniqueIdentifier());
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillGetPathFromCmdletHelper()
        {
            CmdletHelper.Setup(c => c.GetPath(It.IsAny<PSCmdlet>())).Returns("c:\\testpath");
            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.IsTrue(Environment.WorkingDirectory == "c:\\testpath");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillCreateWorkingDirectoryIfItDoesntExist()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            FileSystem.Verify(f => f.CreateFolder($"{appdata}\\VMLab"));
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillNotCreateFolderIfItAlreadyExists()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            FileSystem.Verify(f => f.CreateFolder($"{appdata}\\VMLab"), Times.Never);
        }

        [TestMethod]
        public void CallingUpdateUpdateEnvironmentWillNotReadFileIfItDoesntExist()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);
            
            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            FileSystem.Verify(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json"), Times.Never);
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateTemplateDirectory()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json")).Returns("{\"TemplateDirectory\":\"c:\\\\templates\"}");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.TemplateDirectory, "c:\\templates");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateTemplateDirectoryWithAEmptyStringIfItIsNotInSettingsFile()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json")).Returns("{\"AnotherSetting\":\"AnotherValue\"}");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.TemplateDirectory, "");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateWillUpdateScratchDirectory()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json")).Returns("{\"ScratchDirectory\":\"c:\\\\scratch\"}");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.ScratchDirectory, "c:\\scratch");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateScratchDirectoryWithTempIfNotSetInSettingsFile()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var temp = System.Environment.GetEnvironmentVariable("Temp");

            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json")).Returns("{\"AnotherSetting\":\"AnotherValue\"}");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.ScratchDirectory, temp);
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateScratchDirectoryWithTempIfSettingsFileDoesntExist()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var temp = System.Environment.GetEnvironmentVariable("Temp");

            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.ScratchDirectory, temp);
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateFloppyToolPathToModuleAssemblyFolder()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var moduledir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetAssembly(typeof(EnvironmentDetails)).CodeBase).Path));
            
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.ModuleRootFolder, moduledir);
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMRunPath()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            RegistryHelper.Setup(
                r =>
                    r.GetRegistryValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                        "InstallPath", "<Not Installed>")).Returns("c:\\vmwarefolder");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMRunPath, "c:\\vmwarefolder\\vmrun.exe");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMRunPathToNotInstalledIfItIsntFound()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            RegistryHelper.Setup(
                r =>
                    r.GetRegistryValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                        "InstallPath", "<Not Installed>")).Returns("<Not Installed>");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMRunPath, "<Not Installed>");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMwareDiskExePath()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            RegistryHelper.Setup(
                r =>
                    r.GetRegistryValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                        "InstallPath", "<Not Installed>")).Returns("c:\\vmwarefolder");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMwareDiskExe, "c:\\vmwarefolder\\vmware-vdiskmanager.exe");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMwareDiskExePathToNotInstalledIfItIsntFound()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            RegistryHelper.Setup(
                r =>
                    r.GetRegistryValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                        "InstallPath", "<Not Installed>")).Returns("<Not Installed>");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMwareDiskExe, "<Not Installed>");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMwareExePath()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            RegistryHelper.Setup(
                r =>
                    r.GetRegistryValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                        "InstallPath", "<Not Installed>")).Returns("c:\\vmwarefolder");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMwareExe, "c:\\vmwarefolder\\vmware.exe");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMwareExePathToNotInstalledIfItIsntFound()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            RegistryHelper.Setup(
                r =>
                    r.GetRegistryValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                        "InstallPath", "<Not Installed>")).Returns("<Not Installed>");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMwareExe, "<Not Installed>");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMRootFolder()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(false);

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMRootFolder, "_VM");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillUpdateVMRootFolderToValueFromSettingFileIfItIsSet()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json")).Returns("{\"VMRootFolder\":\"_VM_\"}");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMRootFolder, "_VM_");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillRetrunDefaultValueIfSettingFileExistsButIsntSet()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json")).Returns("{\"AnotherSetting\":\"AnotherValue\"}");

            Environment.UpdateEnvironment(new Mock<PSCmdlet>().Object);

            Assert.AreEqual(Environment.VMRootFolder, "_VM");
        }

        [TestMethod]
        public void CallingUpdateEnvironmentWillStoreTheCurrentCmdlet()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{appdata}\\VMLab\\Settings.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{appdata}\\VMLab\\Settings.json")).Returns("{\"AnotherSetting\":\"AnotherValue\"}");

            var obj = new Mock<PSCmdlet>().Object;

            Environment.UpdateEnvironment(obj);

            Assert.AreSame(Environment.Cmdlet, obj); 

        }

        [TestMethod]
        public void CallingPersistEnvironmentWillCreateSettingsFolderIfItDoesntExist()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(false);

            Environment.PersistEnvironment();

            FileSystem.Verify(f => f.CreateFolder($"{appdata}\\VMLab"));
        }

        [TestMethod]
        public void CallingPersistEnvironmentWillWriteTemplateDirectoryToSettingsFile()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);

            Environment.TemplateDirectory = "c:\\templates";
            Environment.PersistEnvironment();

            FileSystem.Verify(f => f.SetFile($"{appdata}\\VMLab\\settings.json", It.IsRegex("TemplateDirectory")));
        }

        [TestMethod]
        public void CallingPersistEnvironmentWillWriteVMRootFolderToSettingsFile()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);

            Environment.VMRootFolder = "_Test";
            Environment.PersistEnvironment();

            FileSystem.Verify(f => f.SetFile($"{appdata}\\VMLab\\settings.json", It.IsRegex("VMRootFolder")));
        }

        [TestMethod]
        public void CallingPersistEnvironmentWillWriteScratchFolderToSettingsFile()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);

            Environment.ScratchDirectory = "c:\\scratch";
            Environment.PersistEnvironment();

            FileSystem.Verify(f => f.SetFile($"{appdata}\\VMLab\\settings.json", It.IsRegex("ScratchDirectory")));
        }

        [TestMethod]
        public void CallingPersistEnvironmentWillWriteComponentFolderToSettingsFile()
        {
            var appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            FileSystem.Setup(f => f.FolderExists($"{appdata}\\VMLab")).Returns(true);

            Environment.ScratchDirectory = "c:\\scratch";
            Environment.PersistEnvironment();

            FileSystem.Verify(f => f.SetFile($"{appdata}\\VMLab\\settings.json", It.IsRegex("ComponentPath")));
        }
    }
}
