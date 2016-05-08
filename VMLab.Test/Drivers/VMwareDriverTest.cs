using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Authentication;
using System.Web.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VMLab.Drivers;
using Moq;
using VMLab.Helper;
using VMLab.Model;
using VMLab.Model.Caps;
using VMLab.Test.Helper;

namespace VMLab.Test.Drivers
{
	[TestClass]
	public class VMwareDriverTest
	{
	    public IDriver Driver;
	    public Mock<IEnvironmentDetails> Environment;
	    public ILog Log;
	    public Mock<IVMwareHypervisor> HyperVisor;
	    public Mock<ICaps> Caps;
	    public Mock<IVMSettingStoreManager> SettingStoreManager;
	    public Mock<IFileSystem> FileSystem;
	    public Mock<IFloppyUtil> FloppyUtil;

        //strings
	    public string TestDirectory;
	    public string ExistingVMFolder;
	    public string ExistingVMXFolder;
        public string ExistingVMVmxPath;
	    public string ExistingVMManifest;
	    public string ExistingVMStore;
        public string ExistingVM;
	    public string TemplateFolder;
	    public string ExistingTemplate;
	    public string ExistingTemplateFolder;
	    public string ExistingTemplateManifest;
	    public string EnvironmentRandomName;
	    public string ExistingFileOnHost;
	    public string ScratchDir;
	    public string ExistingVMRoot;

        [TestInitialize()]
	    public void Setup()
	    {
            

            //String Assignment
            EnvironmentRandomName = "UniqueRandomName";
            TestDirectory = System.Environment.GetEnvironmentVariable("Temp") + "\\VMLABUNITTEST";
	        ExistingVM = "ExistingVM";
	        ExistingVMFolder = TestDirectory + $"\\_VM\\{ExistingVM}";
            ExistingVMXFolder = $"{ExistingVMFolder}\\{EnvironmentRandomName}";
            ExistingVMVmxPath = $"{ExistingVMXFolder}\\{ExistingVM}.vmx";
            ExistingVMManifest = $"{ExistingVMXFolder}\\manifest.json";
            ExistingVMStore = $"{ExistingVMXFolder}\\settings.json";

            TemplateFolder = $"{TestDirectory}\\Templates";
	        ExistingTemplate = "ExistingTemplate";
	        ExistingTemplateFolder = $"{TemplateFolder}\\{ExistingTemplate}";
	        ExistingTemplateManifest = $"{ExistingTemplateFolder}\\manifest.json";
            ExistingVMRoot = $"{TestDirectory}\\_VM";

            ExistingFileOnHost = $"{TestDirectory}\\test.txt";

            ScratchDir = $"{TestDirectory}\\scratch";

            FileSystem = new Mock<IFileSystem>();
            FileSystem.Setup(p => p.FolderExists(TestDirectory)).Returns(true);
            FileSystem.Setup(p => p.FolderExists(ExistingVMFolder)).Returns(true);
            FileSystem.Setup(p => p.FolderExists(ExistingVMXFolder)).Returns(true);
            FileSystem.Setup(p => p.FileExists(ExistingVMVmxPath)).Returns(true);
            FileSystem.Setup(p => p.FileExists(ExistingVMManifest)).Returns(true);
            FileSystem.Setup(p => p.FileExists(ExistingVMStore)).Returns(true);
            FileSystem.Setup(p => p.FileExists(ExistingFileOnHost)).Returns(true);
            FileSystem.Setup(p => p.FolderExists($"{TestDirectory}\\_VM")).Returns(true);

            FileSystem.Setup(p => p.GetSubFolders($"{TestDirectory}\\_VM")).Returns(new[] { ExistingVMFolder});
            FileSystem.Setup(p => p.GetSubFolders(ExistingVMFolder)).Returns(new[] { ExistingVMXFolder });
            FileSystem.Setup(p => p.GetSubFolders(TemplateFolder)).Returns(new[] {ExistingTemplateFolder});
            FileSystem.Setup(p => p.GetPathLeaf(ExistingTemplateFolder)).Returns("ExistingTemplate");
            FileSystem.Setup(p => p.GetPathLeaf(ExistingVMFolder)).Returns("ExistingVM");

            FileSystem.Setup(p => p.FolderExists(TemplateFolder)).Returns(true);
            FileSystem.Setup(p => p.FolderExists(ExistingTemplateFolder)).Returns(true);
            FileSystem.Setup(p => p.FileExists(ExistingTemplateManifest)).Returns(true);

            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: ['#Generator Text', 'Line2'] }");

            FileSystem.Setup(p => p.ReadFile(ExistingTemplateManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: ['#Generator Text', 'Line2'] }");

            Environment = new Mock<IEnvironmentDetails>();
            Environment.SetupProperty(p => p.WorkingDirectory, TestDirectory);
            Environment.SetupProperty(p => p.TemplateDirectory, TemplateFolder);
            Environment.SetupProperty(p => p.VMRootFolder, "_VM");
            Environment.SetupProperty(p => p.ScratchDirectory, ScratchDir);
            Environment.Setup(m => m.UniqueIdentifier()).Returns("UniqueRandomName");
            
            Caps = new Mock<ICaps>();
            Caps.Setup(p => p.SupportedNetworkTypes).Returns(new[] {"Bridged", "HostOnly", "NAT", "Isolated", "VMNet" });
            Caps.Setup(p => p.SupportedNICs).Returns(new[] { "e1000" });
            Caps.Setup(p => p.DefaultNIC).Returns("e1000");

            Log = new FakeLog();

            HyperVisor = new Mock<IVMwareHypervisor>();
            HyperVisor.Setup(h => h.GetFreeNicID(ExistingVMVmxPath)).Returns(0);
            HyperVisor.Setup(h => h.LookUpPVN("ExampleNetwork", $"{ExistingVMRoot}\\pvn.json")).Returns("00 00 00 00 00 00 00 00-00 00 00 00 00 00 00 00");

            SettingStoreManager = new Mock<IVMSettingStoreManager>();

            FloppyUtil = new Mock<IFloppyUtil>();

            Driver = new VMwareDriver(Environment.Object, Log, HyperVisor.Object, Caps.Object, SettingStoreManager.Object, FileSystem.Object, FloppyUtil.Object);

            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });
        }

        [TestCleanup]
	    public void TearDown()
	    {
            if (Directory.Exists(TestDirectory))
            {
                Directory.Delete(TestDirectory, true);
            }
	    }


	    public void DefaultCredentials(Mock<IVMSettingsStore> store)
	    {
            var creds = new ArrayList { new Dictionary<string, object>() { { "Username", "ValidUsername" }, { "Password", "ValidPassword" } } };
	        SetupCredentialsInStore(store, creds);
	    }

	    public void SetupCredentialsInStore(Mock<IVMSettingsStore> store, ArrayList credlist)
	    {
           
	        store.Setup(s => s.ReadSetting<ArrayList>("Credentials")).Returns(credlist);
	    }

        
		[TestMethod]
		public void CanCreateInstanceOfVMwareDriver()
		{
			Assert.IsNotNull(Driver);
		}

        [TestMethod]
        public void CapsIsAssigned()
        {
            Assert.IsNotNull(Driver.Caps);
        }

        #region "VMPaths"
        [TestMethod]
	    public void CallingGetVMPathOnExistingVMAndAskingForVMXWillReturnIt()
	    {
	        Assert.IsTrue(Driver.GetVMPath("ExistingVM", VMPath.VMX) == ExistingVMVmxPath);
	    }

	    [TestMethod]
	    public void CallingGetVMPathOnNonExistingVMWillReturnNull()
	    {
	       Assert.IsNull(Driver.GetVMPath("NonExistingVM", VMPath.VMX));

	    }

        [TestMethod]
        public void CallingGetVMPathOnExistingVMAndAskingForFolderWillReturnIt()
	    {
            Assert.IsTrue(Driver.GetVMPath("ExistingVM", VMPath.VMFolder) == ExistingVMFolder);
        }

        [TestMethod]
	    public void CallingGetVMPathOnNonExistingVMandAskingForFolderWillReturnNull()
	    {
            Assert.IsNull(Driver.GetVMPath("NonExistingVM", VMPath.VMFolder));
        }

        [TestMethod]
	    public void CallingGetVMPathOnExistingVMAndAskingForManifestWillReturnIt()
	    {
            Assert.IsTrue(Driver.GetVMPath("ExistingVM", VMPath.Manifest) == ExistingVMManifest);
        }

        [TestMethod]
	    public void CallingGetVMPathOnNonExistingVMAndAskingForManifestWillReturnNull()
	    {
            Assert.IsNull(Driver.GetVMPath("NonExistingVM", VMPath.Manifest));
        }

	    [TestMethod]
	    public void CallingGetVMPathOnExistingVMAndAskingForStorePathWillReturnIt()
	    {
            Assert.IsTrue(Driver.GetVMPath("ExistingVM", VMPath.Store) == ExistingVMStore);
        }

	    [TestMethod]
	    public void CallingGetVMPathOnNonExistingVMAndASkingForSTorePathWillReturnNull()
	    {
            Assert.IsNull(Driver.GetVMPath("NonExistingVM", VMPath.Store));
        }
        #endregion

        #region "CreateFromTemplate"

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingCreateVMFromTemplateWithNullWorkingDirectoryThrows()
        {
            Environment.SetupProperty(p => p.WorkingDirectory, null);

            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateVMFromTemplateWithWorkingDirectroryThatDoesntExistThrows()
	    {
            Environment.SetupProperty(p => p.WorkingDirectory, "c:\\thisfolder\\doesntexist");

            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        public void CallingCreateVMFromTemplateWithValidNameWillCreateFolder()
        {
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
            FileSystem.Verify(m => m.CreateFolder($"{TestDirectory}\\_VM\\ValidVMName\\{EnvironmentRandomName}"));
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateFromTemplateWithInvalidNameWillThrow()
	    {
            Driver.CreateVMFromTemplate("Valid::?*VMName", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateFromTemplateWithNameOfAlreadyExistingVMWillThrow()
	    {
            FileSystem.Setup(m => m.FolderExists(TestDirectory + "\\_VM\\AlreadyExisting")).Returns(true);
            Driver.CreateVMFromTemplate("AlreadyExisting", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateFromTemplateWhenTemplateDirectoryIsNotSetWillThrow()
	    {
            Environment.SetupProperty(p => p.TemplateDirectory, null);
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateFromTemplateWhenTemplateDirectoryIsSetToNonExistingDirectoryWillThrow()
        {
            Environment.SetupProperty(p => p.TemplateDirectory, "c:\\doesnot\\exist");
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateFromTemplateWhenTemplateDoesntExistWillThrow()
	    {
	        Driver.CreateVMFromTemplate("ValidVMName", "TemplateDoesntExist", "ExistingSnapshot");
	    }

        [TestMethod()]
        public void CallingCreateFromTemplateWithExistingTemplateDoesntThrow()
	    {
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateFromTemplateWithNullForSnapshotNameThrows()
	    {
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", null);
        }

        [TestMethod()]
        public void CallingCreateFromTemplateWillCallCloneOnTemplateVMX()
	    {
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
            HyperVisor.Verify(m => m.Clone(TestDirectory + "\\Templates\\ExistingTemplate\\ExistingTemplate.vmx",
                        $"{TestDirectory}\\_VM\\ValidVMName\\{EnvironmentRandomName}\\ValidVMName.vmx", "ExistingSnapshot", CloneType.Linked));

        }

        [TestMethod()]
        public void CallingCreateFromTemplateWillCopyManifestOverToVMFolder()
	    {
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
            FileSystem.Verify(m => m.Copy(ExistingTemplateManifest, $"{TestDirectory}\\_VM\\ValidVMName\\{EnvironmentRandomName}\\manifest.json"));
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateFromTemplateWillThrowIfTemplateDoesntHaveManifest()
	    {
            FileSystem.Setup(m => m.FileExists($"{TestDirectory}\\Templates\\ExistingTemplate\\manifest.json"))
                .Returns(false);
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
        }

        [TestMethod()]
        public void CallingCreateFromTemplateWillCallRenameVMOnNewVM()
	    {
            Driver.CreateVMFromTemplate("ValidVMName", "ExistingTemplate", "ExistingSnapshot");
            HyperVisor.Verify(m => m.WriteSetting($"{TestDirectory}\\_VM\\ValidVMName\\{EnvironmentRandomName}\\ValidVMName.vmx", "displayName", "ValidVMName"));
        }

        #endregion

        #region "CreateVM and RemoveVM"
        [TestMethod()]
	    public void CallingCreateVMWithValidNameWillCreateVMFolder()
	    {
	        Driver.CreateVM("ValidVMName", "ValidVMXCode", "ValidManifest");
            FileSystem.Verify(m => m.CreateFolder($"{TestDirectory}\\_VM\\ValidVMName\\{EnvironmentRandomName}"));
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingCreateVMWithNameOfExistingVMWillThrow()
	    {
            FileSystem.Setup(m => m.FolderExists(TestDirectory + "\\_VM\\AlreadyExisting")).Returns(true);
            Driver.CreateVM("AlreadyExisting", "ValidVMXCode", "ValidManifest");            
        }

        [TestMethod()]
        public void CallingCreateVMWillCreateVMXFileWithContentInIt()
        {
            Driver.CreateVM("ValidVMName", "ValidVMXCode", "ValidManifest");
            FileSystem.Verify(m => m.SetFile($"{TestDirectory}\\_VM\\ValidVMName\\{EnvironmentRandomName}\\ValidVMName.vmx", "ValidVMXCode"));
        }

        [TestMethod()]
        public void CallingCreateVMWillCreateMetaFileWithContentInIt()
        {
            Driver.CreateVM("ValidVMName", "ValidVMXCode", "ValidManifest");
            FileSystem.Verify(m => m.SetFile($"{TestDirectory}\\_VM\\ValidVMName\\{EnvironmentRandomName}\\manifest.json", "ValidManifest"));
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingCreateVMWithInvalidCharectersInVMNameWillThrow()
	    {
            Driver.CreateVM("Invalid:?*<>\\/VMName", "ValidVMXCode", "ValidManifest");
        }

	    [TestMethod]
	    public void CallingRemoveVMOnExistingVMWillCallHyperVisorToRemoveVM()
	    {
	        Driver.RemoveVM("ExistingVM");
            HyperVisor.Verify(h => h.RemoveVM(ExistingVMVmxPath));
	    }

        [TestMethod]
	    public void CallingRemoveVMOnExistingVMWillCleanUpVMFolder()
	    {
	        Driver.RemoveVM("ExistingVM");
            Assert.IsFalse(Directory.Exists(ExistingVMFolder));
	    }

        [TestMethod]
	    public void CallingRemoveVMOnNonExistingVMWillNotThrow()
	    {
	        Driver.RemoveVM("NonExistingVM");
	    }
        #endregion

        #region "GetTemplates"
        [TestMethod]
	    public void CallingGetTemplatesWhenATemplateExistsWillReturnTemplateObject()
	    {
	        var templates = Driver.GetTemplates();

	        if (templates.Any(t => t["Name"].ToString() == "ExistingTemplate"))
	        {
	            Assert.IsTrue(true);
	            return;
	        }

            Assert.Fail("Could not find template in resturn results.");
	    }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingGetTemplatesWhenTemplatePathIsNotAssignedWillThrow()
        {
            Environment.SetupProperty(p => p.TemplateDirectory, null);
            Driver.GetTemplates();
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingGetTemplateWillOnlyReturnTemplatesWithNamesAssigned()
	    {
            FileSystem.Setup(m => m.ReadFile(ExistingTemplateManifest))
                .Returns(
                    "{ OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64 , GeneratorText: '#Generator Text'}");

            var templates = Driver.GetTemplates();

            foreach (var t in templates)
            {
                Assert.IsNotNull(t["Name"]);
            }
	    }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingGetTemplateWillOnlyReturnTemplatesWithNamesThatMatchFolderNameAssigned()
        {
            FileSystem.Setup(m => m.ReadFile(ExistingTemplateManifest))
            .Returns(
                "{ Name: 'NameThatDoesntMatchFolder', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64 , GeneratorText: '#Generator Text'}");

            var templates = Driver.GetTemplates();

            foreach (var t in templates)
            {
                Assert.IsNotNull(t["Name"]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingGetTemplateWillOnlyReturnTemplatesWithOSAssigned()
	    {
            FileSystem.Setup(m => m.ReadFile(ExistingTemplateManifest))
            .Returns(
                "{ Name: 'NoNameTemplate', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            var templates = Driver.GetTemplates();

            Console.WriteLine(templates.Length);

            foreach (var t in templates)
            {
                Assert.IsNotNull(t["OS"]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingGetTemplteWillOnlyReturnTemplatesWithDescriptionAssigned()
	    {
            FileSystem.Setup(m => m.ReadFile(ExistingTemplateManifest))
                .Returns(
                    "{ Name: 'NoNameTemplate', OS: 'Windows', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            var templates = Driver.GetTemplates();

            Console.WriteLine(templates.Length);

            foreach (var t in templates)
            {
                Assert.IsNotNull(t["Description"]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingGetTemplteWillOnlyReturnTemplatesWithAuthorAssigned()
        {
            FileSystem.Setup(m => m.ReadFile(ExistingTemplateManifest))
                .Returns(
                    "{ Name: 'NoNameTemplate', OS: 'Windows', Description: 'Test description', Arch: 64, GeneratorText: '#Generator Text' }");

            var templates = Driver.GetTemplates();

            Console.WriteLine(templates.Length);

            foreach (var t in templates)
            {
                Assert.IsNotNull(t["Author"]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingGetTemplteWillOnlyReturnTemplatesWithArchAssigned()
        {
            FileSystem.Setup(m => m.ReadFile(ExistingTemplateManifest))
                .Returns(
                    "{ Name: 'NoNameTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', GeneratorText: '#Generator Text'}");

            var templates = Driver.GetTemplates();

            Console.WriteLine(templates.Length);

            foreach (var t in templates)
            {
                Assert.IsNotNull(t["Arch"]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingGetTemplteWillOnlyReturnTemplatesWithGeneratorTextAssigned()
        {
            FileSystem.Setup(m => m.ReadFile(ExistingTemplateManifest))
                .Returns(
                    "{ Name: 'NoNameTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64}");

            var templates = Driver.GetTemplates();

            Console.WriteLine(templates.Length);

            foreach (var t in templates)
            {
                Assert.IsNotNull(t["GeneratorText"]);
            }
        }

        #endregion

        #region "GetProvisionedVMs"
        [TestMethod]
	    public void CallingGetProvisionedVMsWillReturnAListOfCurrentVMNames()
        {
            Assert.IsTrue(Driver.GetProvisionedVMs().Any(s => s == "ExistingVM"));
        }

	    [TestMethod]
	    public void CallingGetPRovisionedVMWhileNoRootVMFolderExistsInCurrentFolderReturnsEmptyArray()
	    {
	        FileSystem.Setup(m => m.FolderExists(TestDirectory + $"\\_VM")).Returns(false);
            Assert.IsTrue(Driver.GetProvisionedVMs().Length == 0);
	    }

        #endregion

        #region "CreateLabFile"

        [TestMethod]
	    public void CallingCreateLabFileWillCreateLabFileInWorkingDirectory()
	    {
	        Driver.CreateLabFile("ExistingTemplate");
            FileSystem.Setup(m => m.SetFile($"{TestDirectory}\\VMLab.ps1", It.IsAny<string>()));

        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingCreateLabFileWillThrowIfLabFileAlreadyExists()
	    {
	        FileSystem.Setup(m => m.FileExists($"{TestDirectory}\\VMLab.ps1")).Returns(true);
	        Driver.CreateLabFile("ExistingTemplate");
	    }

	    [TestMethod]
	    [ExpectedException(typeof (ApplicationException))]
	    public void CallingCreateLabFileWillThrowIfTemplateDoesntExist()
	    {
	        Driver.CreateLabFile("NonExistingTemplate");
	    }

        [TestMethod]
	    public void CallingCreateLabFileWithValidTemplateWillWriteTemplateTextIntoLabFile()
	    {
            Driver.CreateLabFile("ExistingTemplate");
            FileSystem.Verify(m => m.SetFile($"{TestDirectory}\\VMLab.ps1", $"#Generator Text{System.Environment.NewLine}Line2{System.Environment.NewLine}"));
	    }
        #endregion

        #region "AddNetwork"

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingAddNetworkForNonExistingNetworkTypeThrows()
	    {
            Driver.AddNetwork("ExistingVM", "BogusNetworkType", "e1000");
        }

        [TestMethod]
	    public void CallingAddNetworkForBridgedNetworkWontThrow()
	    {
	        Driver.AddNetwork("ExistingVM", "Bridged", "e1000");
	    }

        [TestMethod]
        public void CallingAddNetworkForExistingVMWillNotThrow()
	    {
	        Driver.AddNetwork("ExistingVM", "Bridged", "e1000");
	    }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingAddNetworkForVMDoesntExistWillThrow()
        {
            Driver.AddNetwork("NonExistingVM", "Bridged", "e1000");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingAddNetworkWithInvalidNICWillThrow()
	    {
	        Driver.AddNetwork("ExistingVM", "Bridged", "InvalidNICType");
	    }

        [TestMethod]
        public void CallingAddNetworkWithValidNICWillNotThrow()
	    {
            Driver.AddNetwork("ExistingVM", "Bridged", "e1000");
        }

        [TestMethod]
        public void CallingAddNetworkWithBlankNicWillSelectDefaultNICAndNotThrow()
	    {
            Driver.AddNetwork("ExistingVM", "Bridged", "");
        }

	    [TestMethod]
	    public void CallingAddNetworkWillMakeCallToHyperVisorForFreeNetworkID()
	    {
            Driver.AddNetwork("ExistingVM", "Bridged", "");
	        HyperVisor.Verify(h => h.GetFreeNicID(ExistingVMVmxPath));
	    }

        [TestMethod]
	    public void CallingAddNetworkWillMakeCallToHyperVisorToEnableNIC()
	    {
            Driver.AddNetwork("ExistingVM", "Bridged", "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.present", "TRUE"));
        }

        [TestMethod]
        public void CallingAddNetworkWillMakeCallToHyperVisorToSetNICType()
        {
            Driver.AddNetwork("ExistingVM", "Bridged", "e1000");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.virtualDev", "e1000"));
        }

	    [TestMethod]
	    public void CallingAddNetworkWithBridgeWillCallHyperVisorToSetConnectionType()
	    {
            Driver.AddNetwork("ExistingVM", "Bridged", "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.connectionType", "bridged"));
        }

        [TestMethod]
        public void CallingAddNetworkWillMakeCallToHyperVisorToSetWakeOnPcktRcvToFalse()
        {
            Driver.AddNetwork("ExistingVM", "Bridged", "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.wakeOnPcktRcv", "FALSE"));
        }

	    [TestMethod]
	    public void CallingAddNetworkWillMakeCallToHyperVisorToSetAddressTypeToGenerated()
	    {
            Driver.AddNetwork("ExistingVM", "Bridged", "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.addressType", "generated"));
        }

        [TestMethod]
        public void CallingAddNetworkWithHostOnlyNetworkTypeWillMakeCallToHyperVisorToSetConnectionType()
	    {
            Driver.AddNetwork("ExistingVM", "HostOnly", "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.connectionType", "hostonly"));
        }

        [TestMethod]
        public void CallingAddNetworkWithNATNetworkTypeWillMakeCallToHyperVisorToSetConnectionType()
        {
            Driver.AddNetwork("ExistingVM", "NAT", "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.connectionType", "nat"));
        }

        [TestMethod]
        public void CallingAddNetworkWithIsolatedNetworkTypeWillMakeCallToHyperVisorToSetConnectionType()
        {
            dynamic properties = new ExpandoObject();
            properties.NetworkName = "ExampleNetwork";

            Driver.AddNetwork("ExistingVM", "Isolated", "", properties);
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.connectionType", "pvn"));
        }

        [TestMethod]
        public void CallingAddNetworkWithIsolatedNetworkTypeWillMakeCallToHyperVisorToLookUpPVN()
        {
            dynamic properties = new ExpandoObject();
            properties.NetworkName = "ExampleNetwork";

            Driver.AddNetwork("ExistingVM", "Isolated", "", properties);
            HyperVisor.Verify(h => h.LookUpPVN("ExampleNetwork", $"{ExistingVMRoot}\\pvn.json"));
            
        }

        [TestMethod]
        public void CallingAddNetworkWithIsolatedNetworkTypeWillMakeCallToSetPvnID()
        {
            dynamic properties = new ExpandoObject();
            properties.NetworkName = "ExampleNetwork";
            HyperVisor.Setup(h => h.LookUpPVN("ExampleNetwork", $"{ExistingVMRoot}\\pvn.json")).Returns("00 00 00 00 00 00 00 00-00 00 00 00 00 00 00 00");

            Driver.AddNetwork("ExistingVM", "Isolated", "", properties);
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.pvnID", "00 00 00 00 00 00 00 00-00 00 00 00 00 00 00 00"));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingADdNetworkWithIsolatedAndNullPropertiesThrows()
	    {
            Driver.AddNetwork("ExistingVM", "Isolated", "");
        }

	    [TestMethod]
	    public void CallingAddNetworkWithVMNetTypeWillCallHyperVisorToSetConnectionType()
	    {
            dynamic properties = new ExpandoObject();
            properties.VNet = "VMnet0";

            Driver.AddNetwork("ExistingVM", "VMNet", "", properties);
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.connectionType", "custom"));
        }

        [TestMethod]
        public void CallingAddNetworkWithVMNetTypeWillCallHyperVisorToSetVNet()
        {
            dynamic properties = new ExpandoObject();
            properties.VNet = "VMnet0";

            Driver.AddNetwork("ExistingVM", "VMNet", "", properties);
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ethernet0.vnet", "VMnet0"));

        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingAddNetworkWithVMNetAndNullPropertiesThrows()
        {
            Driver.AddNetwork("ExistingVM", "VMNet", "");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingAddNetworkWithVMNetAndVNetDoesntMatchNamePatternWillThrow()
	    {
            dynamic properties = new ExpandoObject();
            properties.VNet = "BadVNetName";

            Driver.AddNetwork("ExistingVM", "VMNet", "", properties);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingAddNetworkWithVMNetAndVNetLargerThan19WillThrow()
        {
            dynamic properties = new ExpandoObject();
            properties.VNet = "VMnet20";

            Driver.AddNetwork("ExistingVM", "VMNet", "", properties);
        }

        #endregion

        #region "Set Memory"

        [TestMethod]
	    public void CallingSetMemoryWillCallHypervisorToSetMemory()
        {
            Driver.SetMemory("ExistingVM", 1024);
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "memsize", "1024"));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingSetMemoryOnVMThatDoestExistWillThrow()
	    {
            Driver.SetMemory("NonExistingVM", 1024);
        }

        #endregion

        #region "Setting CPU and Cores"

        [TestMethod]
	    public void CallingSetCPUWillMakeCallToHyperVisor()
        {
	        Driver.SetCPU("ExistingVM", 1, 1);

            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "numvcpus", "1"));
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "cpuid.coresPerSocket", "1"));
        }

	    [TestMethod]
	    public void CallingSetCPUWillAssignVcpuToTheTotalNumberOfCoresPerCPU()
	    {
            Driver.SetCPU("ExistingVM", 2, 2);
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "numvcpus", "4"));
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "cpuid.coresPerSocket", "2"));
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingSetCPUWithZeroCPUWillThrow()
	    {
            Driver.SetCPU("ExistingVM", 0, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingSetCPUWithZeroCoresWillThrow()
        {
            Driver.SetCPU("ExistingVM", 1, 0);
        }

        #endregion

        #region "GetVMState"

        [TestMethod]
	    public void CallingVMStateWillReturnShutdownIfVMIsPoweredOff()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] {});

	        Assert.IsTrue(Driver.GetVMState("ExistingVM") == VMState.Shutdown);
	    }

        [TestMethod]
	    public void CallingVMStateWillReturnReadyIfVMToolsAreRunningOnWindows()
	    {

            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<ArrayList>("Credentials")).Returns(new ArrayList { new Dictionary<string, object>() { {"Username", "ValidUsername" }, {"Password", "ValidPassword"} } });

            //Set manifest to windows
            FileSystem.Setup(m => m.ReadFile(ExistingVMManifest))
                .Returns(
                    "{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new[] { ExistingVMVmxPath });

            HyperVisor.Setup(
                h =>
                    h.FileExistInGuest(ExistingVMVmxPath,
                        It.Is<IVMCredential[]>(
                            a => a.Any(c => c.Username == "ValidUsername" && c.Password == "ValidPassword")),
                        "c:\\windows\\explorer.exe")).Returns(true);

            Assert.IsTrue(Driver.GetVMState("ExistingVM") == VMState.Ready);
	    }

	    [TestMethod]
	    public void CallingVMStateWillReturnReadyIfVMToolsAreRunningOnLinux()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<ArrayList>("Credentials")).Returns(new ArrayList { new Dictionary<string, object>() { { "Username", "ValidUsername" }, { "Password", "ValidPassword" } } });

            //Set manifest to linux
            FileSystem.Setup(m => m.ReadFile(ExistingVMManifest))
                .Returns(
                    "{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new[] { ExistingVMVmxPath });

            HyperVisor.Setup(
                h =>
                    h.DirectoryExistInGuest(ExistingVMVmxPath,
                        It.Is<IVMCredential[]>(
                            a => a.Any(c => c.Username == "ValidUsername" && c.Password == "ValidPassword")),
                        "/dev")).Returns(true);

            Assert.IsTrue(Driver.GetVMState("ExistingVM") == VMState.Ready);
        }

        [TestMethod]
        public void CallingVMStateWillReturnOtherIfVMToolsAreNotReadyButMachineIsRunning()
        {

            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new[] { TestDirectory.ToLower() + $"\\_vm\\existingvm\\{EnvironmentRandomName.ToLower()}\\existingvm.vmx" });
            HyperVisor.Setup(
                h =>
                    h.FileExistInGuest(TestDirectory.ToLower() + $"\\_vm\\existingvm\\{EnvironmentRandomName.ToLower()} existingvm.vmx", 
                        It.Is<IVMCredential[]>(a => a.Any(c => c.Username == "ValidUsername" && c.Password == "ValidPassword")),
                        "c:\\windows\\explorer.exe")).Throws<ApplicationException>();

            Assert.IsTrue(Driver.GetVMState("ExistingVM") == VMState.Other);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCredentialException))]
	    public void CallingVMStateWillThrowExceptionIfCredentialsAreIncorrect()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            SetupCredentialsInStore(store, new ArrayList()
            {
                new Dictionary<string,object>() { {"Username", "BadUsername"},{ "Password", "BadPassword" }}
            });

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new[] { TestDirectory.ToLower() + $"\\_vm\\existingvm\\{EnvironmentRandomName.ToLower()}\\existingvm.vmx" });
            HyperVisor.Setup(
                h =>
                    h.FileExistInGuest(ExistingVMVmxPath, It.Is<IVMCredential[]>(
                            a => a.Any(c => c.Username == "BadUsername" && c.Password == "BadPassword")),
                            "c:\\windows\\explorer.exe")).Throws<InvalidCredentialException>();

            Driver.GetVMState("ExistingVM");
	    }
        #endregion

        #region "Credentials"

        [TestMethod]
	    public void CallingAddCredentialOnExistingVMAddsTheCredential()
	    {
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            Driver.AddCredential("ExistingVM", "ValidUsername", "ValidPassword");
            store.Verify(
                s =>
                    s.WriteSetting("Credentials",
                        It.Is<IVMCredential[]>(
                            v => v.Any(c => c.Username == "ValidUsername" && c.Password == "ValidPassword"))));

	    }

        [TestMethod]
	    public void CallingAddCredentialsOnExistingVMWithExistingCredentailsWillAppend()
	    {
            var store = new Mock<IVMSettingsStore>();
  
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<ArrayList>("Credentials")).Returns((ArrayList)(Json.Decode<Dictionary<string,object>>("{\"Credentials\":[{\"Username\":\"ValidUsername\",\"Password\":\"ValidPassword\"}]}")["Credentials"]));   //new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });


            Driver.AddCredential("ExistingVM", "ValidUsername2", "ValidPassword2");
            store.Verify(s => s.WriteSetting("Credentials",
                        It.Is<IVMCredential[]>(
                            v => v.Any(c => c.Username == "ValidUsername" && c.Password == "ValidPassword") &&
                                 v.Any(c => c.Username == "ValidUsername2" && c.Password == "ValidPassword2"))));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingAddCredentialOnNonExistingVMThrows()
	    {
            Driver.AddCredential("NonExistingVM", "ValidUsername", "ValidPassword");
        }

	    [TestMethod]
	    public void CallingGetCredentialsWillReturnCredentialsFromExistingVM()
	    {
            var store = new Mock<IVMSettingsStore>();
	        SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
	        DefaultCredentials(store);

            var creds = Driver.GetCredential("ExistingVM");

	        Assert.IsTrue(creds[0].Username == "ValidUsername");
            Assert.IsTrue(creds[0].Password == "ValidPassword");
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingGetCredentialsOnANonExistingVMWillThrow()
	    {
	        Driver.GetCredential("NonExistingVM");
	    }

        [TestMethod]
        public void CallingGetCredentialsWhenNoCredentialsAreSetReturnsEmptyArray()
	    {
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            Assert.IsTrue(Driver.GetCredential("ExistingVM").Length == 0);
	    }

        #endregion

        #region "VM Power Commands"

        [TestMethod]
	    public void CallingStartVMOnExistingVMWillCallHypervisorToStartIt()
        {
            Driver.StartVM("ExistingVM");
            HyperVisor.Verify(h => h.StartVM(ExistingVMVmxPath));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingStartVMOnNonExistingVMWillThrow()
	    {
            Driver.StartVM("NonExistingVM");
        }

	    [TestMethod]
	    public void CallingStopVMOnExistingVMWillCallHypervisorToStopIt()
	    {
	        Driver.StopVM("ExistingVM", false);
            HyperVisor.Verify(h => h.StopVM(ExistingVMVmxPath, false));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingStopVMOnNonExistingVMWillThrow()
	    {
            Driver.StopVM("NonExistingVM", false);
        }

        [TestMethod]
        public void CallingStopVMOnExistingVMWithForceSetToTrueWillCallHypervisorToStopIt()
        {
            Driver.StopVM("ExistingVM", true);
            HyperVisor.Verify(h => h.StopVM(ExistingVMVmxPath, true));
        }

        [TestMethod]
	    public void CallingRestartVMOnExistingVMWillCallHypervisorToRestartIt()
        {
            Driver.ResetVM("ExistingVM", false);
            HyperVisor.Verify(h => h.ResetVM(ExistingVMVmxPath, false));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingRestartVMOnNonExistingVMWillThrow()
	    {
	        Driver.ResetVM("NonExistingVM", false);
	    }

        [TestMethod]
        public void CallingRestartVMOnExistingVMWithForceWillCallHypervisorToRestartIt()
	    {
            Driver.ResetVM("ExistingVM", true);
            HyperVisor.Verify(h => h.ResetVM(ExistingVMVmxPath, true));
        }
        #endregion

        #region "VM Settings"
        [TestMethod]
	    public void CallingWriteVMSettingWillCallHyperVisorToSetSetting()
        {
            Driver.WriteVMSetting("ExistingVM", "TestSetting", "TestValue");

            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "TestSetting", "TestValue"));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingWriteVMSettingOnNonExistingVMWillThrow()
	    {
            Driver.WriteVMSetting("NonExistingVM", "TestSetting", "TestValue");
        }

	    [TestMethod]
	    public void CallingReadVMSettingWillCallHyperVisorToRetriveSetting()
	    {
	        HyperVisor.Setup(h => h.ReadSetting(ExistingVMVmxPath, "TestSetting")).Returns("ExpectedValue");
	        Assert.IsTrue(Driver.ReadVMSetting("ExistingVM", "TestSetting") == "ExpectedValue");
	    }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingReadVMSettingOnNonExistingVMWillThrow()
	    {
	        Driver.ReadVMSetting("NonExistingVM", "TestSetting");
	    }

        [TestMethod]
	    public void CallingClearVMSettingOnExistingVMWillCallHypervisorToClearIt()
        {
            Driver.ClearVMSetting("ExistingVM", "TestSetting");
            HyperVisor.Verify(h => h.ClearSetting(ExistingVMVmxPath, "TestSetting"));
        }

	    [TestMethod]
	    [ExpectedException(typeof (ApplicationException))]
	    public void CallingClearVMSettingOnNonExistingVMWillThrow()
	    {
            Driver.ClearVMSetting("NonExistingVM", "TestSetting");
        }
        #endregion

        #region "Show VM GUI"
        [TestMethod]
	    public void CallingShowGUIOnExistingVMWillCallHypervisorToShowGUI()
        {
            Driver.ShowGUI(ExistingVM);
            HyperVisor.Verify(h => h.ShowGUI(ExistingVMVmxPath));
        }

	    [TestMethod]
	    [ExpectedException(typeof (ApplicationException))]
	    public void CallingShowGUIOnNonExistingVMWillThrow()
	    {
            Driver.ShowGUI("NonExistingVM");
        }
        #endregion

        #region "Guest File IO"
        [TestMethod]
	    public void CallingCopyFileToGuestOnExistingVMWillCallHypervisor()
        {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            DefaultCredentials(store);


            Driver.CopyFileToGuest(ExistingVM, ExistingFileOnHost, "c:\\temp\\test.txt");
            HyperVisor.Verify(h => h.CopyFileToGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), ExistingFileOnHost, "c:\\temp\\test.txt"));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingCopyFileToGuestOnNonExistingVMWillThrow()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });
            
            Driver.CopyFileToGuest("NonExistingVM", ExistingFileOnHost, "c:\\temp\\test.txt");
	    }

	    [TestMethod]
	    public void CallingCopyFileFromGuestOnExistingVMWillCallHypervisor()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });

            Driver.CopyFileFromGuest(ExistingVM, "c:\\temp\\test.txt", $"{TestDirectory}\\fromguest.txt");
            HyperVisor.Verify(h => h.CopyFileFromGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\temp\\test.txt", $"{TestDirectory}\\fromguest.txt"));
	    }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingCopyFileFromGuestOnNonExistingVMWillThrow()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            DefaultCredentials(store);


            Driver.CopyFileFromGuest("NonExistingVM", "c:\\temp\\test.txt", $"{TestDirectory}\\fromguest.txt");
        }

	    [TestMethod]
	    public void CallingDeleteFileInGuestOnExistingVMWillCallHypervisor()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            DefaultCredentials(store);

            Driver.DeleteFileInGuest(ExistingVM, "c:\\temp\\test.txt");
            HyperVisor.Verify(h => h.DeleteFileInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\temp\\test.txt"));
	    }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingDeleteFileInGuestOnNonExistingVMWillThrow()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            DefaultCredentials(store);

            Driver.DeleteFileInGuest("NonExistingVM", "c:\\temp\\test.txt");
        }
        #endregion

        
        #region "Running Process in guest"
        [TestMethod]
	    public void CallingExecuteCommandOnExistingVMWillCallHypervisor()
        {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommand(ExistingVM, "c:\\test.exe", "some commandline arguments here");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\test.exe", "some commandline arguments here", false, false) );
        }

        [TestMethod]
        [ExpectedException(typeof(GuestVMPoweredOffException))]
        public void CallingExecuteCommandOnExistingVMWillThrowIfVMIsPoweredOff()
        {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();

            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[]{});
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") }, "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommand(ExistingVM, "c:\\test.exe", "some commandline arguments here");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingExecuteCommandOnNonExistingVMWillThrow()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") }, "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommand("NonExisting", "c:\\test.exe", "some commandline arguments here");
        }

	    [TestMethod]
	    public void CallingExecuteCommandOnExistingVMWithNoWaitSetToTrueWillCallHypervisor()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommand(ExistingVM, "c:\\test.exe", "some commandline arguments here", noWait: true);
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\test.exe", "some commandline arguments here", true, false));
        }

	    [TestMethod]
	    public void CallingExecuteCommandOnExistingVMWithInteractiveSetToTrueWillCallHypervisor()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            store.Setup(s => s.ReadSetting<IVMCredential[]>("Credentials")).Returns(new IVMCredential[] { new VMCredential("ValidUsername", "ValidPassword") });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommand(ExistingVM, "c:\\test.exe", "some commandline arguments here", interactive: true);
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\test.exe", "some commandline arguments here", false, true));
        }

        [TestMethod]
	    public void CallingExecuteCommandWithResultsOnExistingVMWillReturnExpectedSTDOut()
        {
            FileSystem.Setup(m => m.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stdout")).Returns(true);
            FileSystem.Setup(m => m.ReadFile($"{ScratchDir}\\{EnvironmentRandomName}.stdout")).Returns("Expected");
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            var result = Driver.ExecuteCommandWithResult(ExistingVM, new [] { "c:\\test.exe Test Args"});
            Assert.IsTrue(result.STDOut == "Expected");
        }

	    [TestMethod]
	    public void CallingExcuteCommandWithResultsOnExistingVMWillReturnExpectedSTDError()
	    {
            FileSystem.Setup(m => m.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns(true);
            FileSystem.Setup(m => m.ReadFile($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns("Expected");
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            var result = Driver.ExecuteCommandWithResult(ExistingVM, new[] { "c:\\test.exe Test Args" });
            Assert.IsTrue(result.STDError == "Expected");
        }

	    [TestMethod]
	    public void CallingExecuteCommandWithResultsOnWindowsWillCreateBatchFile()
	    {
            //Setting Os to windows in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "c:\\test.exe Test Args" });

	        var text = string.Join(System.Environment.NewLine,
	            "@echo off",
	            "c:\\test.exe Test Args");

	        var path = $"{ScratchDir}\\{EnvironmentRandomName}.cmd";

            FileSystem.Verify(
	            m => m.SetFile(path, text));
	    }

        [TestMethod]
        public void CallingExecuteCommandWithResultsOnLinuxWillCreateShScript()
	    {
            //Setting Os to unix in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] {"/bin/somecommand Test args"});

            var text = string.Join("\n",
                "#!/bin/bash",
                "/bin/somecommand Test args");

            var path = $"{ScratchDir}\\{EnvironmentRandomName}.sh";

            FileSystem.Verify(
                m => m.SetFile(path, text));
        }

        [TestMethod]
	    public void CallingExecuteCommandWithresultsOnWindowsWillCopyScriptToGuest()
	    {
            //Setting Os to windows in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            var hostpath = $"{ScratchDir}\\{EnvironmentRandomName}.cmd";
	        var guestpath = $"c:\\windows\\temp\\{EnvironmentRandomName}.cmd";

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "c:\\test.exe Test Args" });

            HyperVisor.Verify(h => h.CopyFileToGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), hostpath, guestpath));

        }

        [TestMethod]
        public void CallingExecuteCommandWithresultsOnLinuxWillCopyScriptToGuest()
        {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            var hostpath = $"{ScratchDir}\\{EnvironmentRandomName}.sh";
            var guestpath = $"/tmp/{EnvironmentRandomName}.sh";

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "some random linux commandline here" });

            HyperVisor.Verify(h => h.CopyFileToGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), hostpath, guestpath));

        }

        [TestMethod]
        public void CallingExecuteCommandWithresultsOnLinuxWillChmodScriptToBeExecutable()
        {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);


            var hostpath = $"{ScratchDir}\\{EnvironmentRandomName}.sh";
            var guestpath = $"/tmp/{EnvironmentRandomName}.sh";

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "some random linux commandline here" });

            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/bin/chmod", $"+x /tmp/{EnvironmentRandomName}.sh", false, false));

        }

        [TestMethod]
	    public void CallingExecuteCommandWithResultsWillCallScriptWithCmdOnWindows()
	    {
            //Setting Os to windows in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "c:\\test.exe Test Args" });
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\system32\\cmd.exe", $"/c c:\\windows\\temp\\{EnvironmentRandomName}-launch.cmd", false, false));
        }

        [TestMethod]
        public void CallingExecuteCommandWithResultsWillCallScriptWithCmdOnLinux()
        {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "some random linux commandline here" });
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"/tmp/{EnvironmentRandomName}-launch.sh", "", false, false));
        }

        [TestMethod]
	    public void CallingExecuteCommandWithResultsWillCopystdoutToHostOnWindows()
	    {
            //Setting Os to windows in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] {"c:\\test.exe Test Args"});

            HyperVisor.Verify(h => h.CopyFileFromGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"c:\\windows\\temp\\{EnvironmentRandomName}.stdout", $"{ScratchDir}\\{EnvironmentRandomName}.stdout" ));
	    }

        [TestMethod]
        public void CallingExecuteCommandWithResultsWillCopystdoutToHostOnLinux()
        {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "some random linux commandline here" });

            HyperVisor.Verify(h => h.CopyFileFromGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"/tmp/{EnvironmentRandomName}.stdout", $"{ScratchDir}\\{EnvironmentRandomName}.stdout"));
        }

        [TestMethod]
        public void CallingExecuteCommandWithResultsWillCopystderrToHostOnWindows()
        {
            //Setting Os to windows in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "c:\\test.exe Test Args" });

            HyperVisor.Verify(h => h.CopyFileFromGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"c:\\windows\\temp\\{EnvironmentRandomName}.stderr", $"{ScratchDir}\\{EnvironmentRandomName}.stderr"));
        }

        [TestMethod]
        public void CallingExecuteCommandWithResultsWillCopystderrToHostOnLinux()
        {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "some random linux commandline here" });

            HyperVisor.Verify(h => h.CopyFileFromGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"/tmp/{EnvironmentRandomName}.stderr", $"{ScratchDir}\\{EnvironmentRandomName}.stderr"));
        }

        [TestMethod]
	    public void CallingExecuteCommandWithResultsWillDeleteSTDOutFileWhenComplete()
	    {
	        FileSystem.Setup(f => f.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stdout")).Returns(true);

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "c:\\test.exe Test Args" });

            FileSystem.Verify(f => f.DeleteFile($"{ScratchDir}\\{EnvironmentRandomName}.stdout"));
        }

        [TestMethod]
        public void CallingExecuteCommandWithResultsWillDeleteSTDErrFileWhenComplete()
        {
            FileSystem.Setup(f => f.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns(true);

            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new[] { "c:\\test.exe Test Args" });

            FileSystem.Verify(f => f.DeleteFile($"{ScratchDir}\\{EnvironmentRandomName}.stderr"));
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingExecutePowershellWillThrowIfCalledOnLinuxVM()
	    {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

	        Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"));
	    }

        [TestMethod]
	    public void CallingExecutePowershellWillCreateScriptFileLocally()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"));
            FileSystem.Verify(f => f.SetFile($"{ScratchDir}\\{EnvironmentRandomName}.ps1", "Write-Host 'test script code'"));
        }

        [TestMethod]
	    public void CallingExecutePowershellWillCopyTheScriptToGuest()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"));
            HyperVisor.Verify(h => h.CopyFileToGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"{ScratchDir}\\{EnvironmentRandomName}.ps1", $"c:\\windows\\temp\\{EnvironmentRandomName}.ps1"));
        }

	    [TestMethod]
	    public void CallingExecutePowershellWillCallHypervisorToExecuteScript()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"));
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), @"c:\windows\system32\cmd.exe", $"/c c:\\windows\\temp\\{EnvironmentRandomName}.ps1.cmd",false, false));
            FileSystem.Verify(f => f.SetFile($"{ScratchDir}\\{EnvironmentRandomName}.ps1.cmd", $"C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe -executionpolicy bypass -OutputFormat XML -NoProfile -Noninteractive -File c:\\windows\\temp\\{EnvironmentRandomName}.ps1 > c:\\windows\\temp\\{EnvironmentRandomName}.stdout 2> c:\\windows\\temp\\{EnvironmentRandomName}.stderr"));
        }

	    [TestMethod]
	    public void CallingExecutePowershellWillCopySTDOutResultsBackToHost()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"c:\\windows\\temp\\{EnvironmentRandomName}.stdout")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"));
            HyperVisor.Verify((h => h.CopyFileFromGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"c:\\windows\\temp\\{EnvironmentRandomName}.stdout", $"{ScratchDir}\\{EnvironmentRandomName}.stdout")));
        }

        [TestMethod]
        public void CallingExecutePowershellWillCopySTDErrResultsBackToHost()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"c:\\windows\\temp\\{EnvironmentRandomName}.stderr")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"));
            HyperVisor.Verify((h => h.CopyFileFromGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"c:\\windows\\temp\\{EnvironmentRandomName}.stderr", $"{ScratchDir}\\{EnvironmentRandomName}.stderr")));
        }

        [TestMethod]
        public void CallingExecutePowershellWillReturnResultsFoundInSTDOutFile()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stdout")).Returns(true);
            //To get this xml run the following command in powershell: "expected" | Export-Clixml c:\temp\example.xml
            FileSystem.Setup(f => f.ReadFile($"{ScratchDir}\\{EnvironmentRandomName}.stdout")).Returns("<Objs Version=\"1.1.0.1\" xmlns=\"http://schemas.microsoft.com/powershell/2004/04\"><S>Expected</S></Objs>");
            Assert.IsTrue(Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'")).Results.Equals("Expected"));
        }

        [TestMethod]
        public void CallingExecutePowershellWillNotReturnIfResultsFoundInSTDOutFileButIsEmpty()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stdout")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{ScratchDir}\\{EnvironmentRandomName}.stdout")).Returns(string.Empty);
            Assert.IsNull(Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'")).Results);
        }

        [TestMethod]
        public void CallingExecutePowershellWillReturnResultsFoundInSTDErrFile()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns("<Objs Version=\"1.1.0.1\" xmlns=\"http://schemas.microsoft.com/powershell/2004/04\"><S>Expected</S></Objs>");
            Assert.IsTrue(Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'")).Errors.Equals("Expected"));
        }

	    [TestMethod]
	    public void CallingExecutePowershellWillReturnResultsIfSTDHasStringInsteadOfXml()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns("Some error message returned from powershell");
            Assert.IsTrue(Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'")).Errors.Equals("Some error message returned from powershell"));
        }

        [TestMethod]
        public void CallingExecutePowershellWillNotReturnIfResultsFoundInSTDErrFileButIsEmpty()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns(true);
            //To get this xml run the following command in powershell: "expected" | Export-Clixml c:\temp\example.xml
            FileSystem.Setup(f => f.ReadFile($"{ScratchDir}\\{EnvironmentRandomName}.stderr")).Returns(string.Empty);
            Assert.IsNull(Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'")).Errors);
        }

        [TestMethod]
	    public void CallingExecuteCommandWithCredentialsUsesPassedCredentialsInsteadOfStored()
	    {
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommand(ExistingVM, "c:\\test.exe", "some commandline arguments here", username: "ManualUsername", password: "ManualPassword");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.Is<IVMCredential[]>(a => a.Any(c => c.Username == "ManualUsername" && c.Password == "ManualPassword")), "c:\\test.exe", "some commandline arguments here", false, false));
	    }

	    [TestMethod]
	    public void CallingExecuteCommandWithResultsWithCredentialsPassedToMethodInsteadOfUsingStored()
	    {
            //Setting up password retrival
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            Driver.ExecuteCommandWithResult(ExistingVM, new [] { "commands to run" }, username: "ManualUsername", password: "ManualPassword");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.Is<IVMCredential[]>(a => a.Any(c => c.Username == "ManualUsername" && c.Password == "ManualPassword")), It.IsAny<string>(), It.IsAny<string>(), false, false));
        }

	    [TestMethod]
	    public void CallingExecutePowershellWithCredentailsPassedToMethodInsteadOfUsingStored()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"), username: "ManualUsername", password: "ManualPassword");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.Is<IVMCredential[]>(a => a.Any(c => c.Username == "ManualUsername" && c.Password == "ManualPassword")), @"c:\windows\system32\cmd.exe", $"/c c:\\windows\\temp\\{EnvironmentRandomName}.ps1.cmd", false, false));
            
        }

	    [TestMethod]
	    public void CallingExecutePowerShellWithObjectPassedInWillWriteItToTempFile()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"), dataObject:"test");
            FileSystem.Verify(f => f.SetFile($"{ScratchDir}\\{EnvironmentRandomName}.xml", "<Objs Version=\"1.1.0.1\" xmlns=\"http://schemas.microsoft.com/powershell/2004/04\">\r\n  <S>test</S>\r\n</Objs>"));
        }

        [TestMethod]
        public void CallingExecutePowerShellWithObjectPassedInWillCopyTempFileToGuest()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"), dataObject: "test");
            HyperVisor.Verify(h => h.CopyFileToGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), $"{ScratchDir}\\{EnvironmentRandomName}.xml", $"c:\\windows\\temp\\{EnvironmentRandomName}.xml"));
        }

	    [TestMethod]
	    public void CallingExecutePowerShellWithObjectPassedInWillAddObjectDeserialisationToTopOfScript()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            Driver.ExecutePowershell(ExistingVM, ScriptBlock.Create("Write-Host 'test script code'"), dataObject: "test");
            FileSystem.Verify(f => f.SetFile($"{ScratchDir}\\{EnvironmentRandomName}.ps1", $"$DataObject = import-clixml 'c:\\windows\\temp\\{EnvironmentRandomName}.xml'{System.Environment.NewLine}Write-Host 'test script code'"));
        }

        
        #endregion

        #region "Shared Folders"

        [TestMethod]
	    public void CallingAddSharedFolderWillMakeCallToHypervisor()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);
            FileSystem.Setup(f => f.ConvertPathRelativeToFull("c:\\hostfolder")).Returns("c:\\hostfolder");

            Driver.AddSharedFolder(ExistingVM, "c:\\hostfolder", "mysharename", "c:\\location");
            HyperVisor.Verify(h => h.AddSharedFolder(ExistingVMVmxPath, "c:\\hostfolder", "mysharename"));
        }

        [TestMethod]
        public void CallingAddSharedFolderWilConvertRelativeFolderPathsToAbsolute()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            FileSystem.Setup(f => f.ConvertPathRelativeToFull(".")).Returns("c:\\hostfolder");

            Driver.AddSharedFolder(ExistingVM, ".", "mysharename", "c:\\location");
            HyperVisor.Verify(h => h.AddSharedFolder(ExistingVMVmxPath, "c:\\hostfolder", "mysharename"));
        }

     //   [TestMethod]
	    //public void CallingAddSharedFolderWillMakeCallToAddLinkedFolderOnWindows()
	    //{
     //       HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
     //       HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

     //       //Setting Os to windows in manifest.
     //       FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
     //           .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

     //       Manager.AddSharedFolder(ExistingVM, "c:\\hostfolder", "mysharename", "c:\\location");
     //       HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\system32\\cmd.exe", "/c mklink /d \"c:\\location\" \"\\vmware-host\\shared folders\\mysharename\"", false, false));
     //   }

        [TestMethod]
        public void CallingAddSharedFolderWillMakeCallToAddLinkedFolderOnLinux()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);

            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            Driver.AddSharedFolder(ExistingVM, "c:\\hostfolder", "mysharename", "/location");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/bin/mkdir", "/location", false, false));
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/bin/mount", "-v -t vmhgfs .host/mysharename /location", false, false));
        }

        [TestMethod]
        public void CallingAddSharedFolderWillMakeCallToAddLinkedFolderOnLinuxWithLinkedFlagSetInManifest()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.DirectoryExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/dev")).Returns(true);

            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text', MountMode:'Link' }");

            Driver.AddSharedFolder(ExistingVM, "c:\\hostfolder", "mysharename", "/location");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/bin/ln", "/mnt/hgfs/mysharename /location -s", false, false));
        }

        [TestMethod]
	    public void CallingAddSharedFolderWillAddSharedFolderInfoToSettingsWhenNoPreviousFoldersSet()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            var store = new Mock<IVMSettingsStore>();
	        SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            Driver.AddSharedFolder(ExistingVM, "c:\\hostfolder", "mysharename", "c:\\location");

            store.Verify(s => s.WriteSetting("SharedFolders", It.Is<IShareFolderDetails[]>( a => a.Any(i => i.Name == "mysharename" && i.HostPath == "c:\\hostfolder" && i.GuestPath == "c:\\location"))));

        }

        [TestMethod]
	    public void CallingAddSharedFolderWillAddSharedFolderInfoToSettingsPreservingPreviousFolders()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            FileSystem.Setup(f => f.ConvertPathRelativeToFull("c:\\hostfolder")).Returns("c:\\hostfolder");
            store.Setup(s => s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
            {
                new Dictionary<string, object>
                {
                    { "Name", "ExistingFolder" },
                    { "GuestFolder", "c:\\existingfolder" },
                    { "HostFolder", "c:\\existinghostfolder" },
                }
            });
                
            Driver.AddSharedFolder(ExistingVM, "c:\\hostfolder", "mysharename", "c:\\location");

            store.Verify(s => s.WriteSetting("SharedFolders", It.Is<IShareFolderDetails[]>(a => a.Any(i => i.Name == "ExistingFolder" && i.HostPath == "c:\\existinghostfolder" && i.GuestPath == "c:\\existingfolder"))));
        }

	    [TestMethod]
	    public void CallingRemoveSharedFolderWillMakeCallToHypervisorToRemoveFolder()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
	        store.Setup(
	            s =>
	                s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
	                {
	                    new Dictionary<string, object>
	                    {
                            { "Name", "mysharename" },
                            { "GuestFolder", "c:\\mysharename" },
                            { "HostFolder", "c:\\mysharenameonhost" },
                        }
                    });

            Driver.RemoveSharedFolder(ExistingVM, "mysharename");
            HyperVisor.Verify(h => h.RemoveSharedFolder(ExistingVMVmxPath, "mysharename"));
	    }

        [TestMethod]
        public void CallingRemoveSharedFolderWillRemoveItFromStore()
        {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(
                s =>
                    s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
                    {
                        new Dictionary<string, object>
                        {
                            { "Name", "mysharename" },
                            { "GuestFolder", "c:\\mysharename" },
                            { "HostFolder", "c:\\mysharenameonhost" },
                        },
                        new Dictionary<string, object>
                        {
                            { "Name", "othersharename" },
                            { "GuestFolder", "c:\\othersharename" },
                            { "HostFolder", "c:\\othersharenameonhost" },
                        }
                    });

            Driver.RemoveSharedFolder(ExistingVM, "mysharename");
            store.Verify(s => s.WriteSetting("SharedFolders", It.Is<IShareFolderDetails[]>(f => f.All(o => o.Name != "mysharename"))));
        }

        [TestMethod]
	    public void CallingRemoveSharedFolderWillRemoveFolderInWindowsGuest()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            //Setting Os to windows in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
	        store.Setup(s => s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
	        {
	            new Dictionary<string, object>
	            {
	                {"Name", "mysharename"},
	                {"GuestFolder", "c:\\existingfolder"},
	                {"HostFolder", "c:\\existinghostfolder"},
	            }
	        });
                   
            Driver.RemoveSharedFolder(ExistingVM, "mysharename");
	    }

        [TestMethod]
        public void CallingRemoveSharedFolderWillRemoveFolderInLinuxGuest()
        {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");

            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(s => s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
            {
                new Dictionary<string, object>
                {
                    {"Name", "mysharename"},
                    {"GuestFolder", "/mysharename"},
                    {"HostFolder", "c:\\mysharenameonhost"},
                }

            });

            Driver.RemoveSharedFolder(ExistingVM, "mysharename");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/bin/unmount", "/mysharename", false, false));
        }


        [TestMethod]
        public void CallingRemoveSharedFolderWillRemoveFolderInLinuxGuestIfLinkIsSetInManifest()
        {
            //Setting Os to linux in manifest.
            FileSystem.Setup(p => p.ReadFile(ExistingVMManifest))
                .Returns("{ 'Name': 'ExistingTemplate', OS: 'Unix', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text', MountMode:'Link' }");

            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
            store.Setup(
                s =>
                    s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
                    {
                        new Dictionary<string, object>
                        {
                            { "Name", "mysharename" },
                            { "GuestFolder", "/mysharename" },
                            { "HostFolder", "c:\\mysharenameonhost" },
                        }
                    });
                
            Driver.RemoveSharedFolder(ExistingVM, "mysharename");
            HyperVisor.Verify(h => h.ExecuteCommand(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "/bin/rm", "-fr /mysharename", false, false));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingRemoveSharedFolderOnNonExistingShareWillThrow()
	    {
            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
	        store.Setup(s => s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
	        {
	            new Dictionary<string, object>
	            {
	                {"Name", "mysharename"},
	                {"GuestFolder", "c:\\mysharename"},
	                {"HostFolder", "c:\\mysharenameonhost"},
	            }

	        });

            Driver.RemoveSharedFolder(ExistingVM, "DifferentShareThatDoesntExist");
        }

	    [TestMethod]
	    public void CallingGetSharedFolderWillReturnShareFolderObjects()
	    {
            HyperVisor.Setup(h => h.GetRunningVMs()).Returns(new string[] { ExistingVMVmxPath });
            HyperVisor.Setup(h => h.FileExistInGuest(ExistingVMVmxPath, It.IsAny<IVMCredential[]>(), "c:\\windows\\explorer.exe")).Returns(true);

            var store = new Mock<IVMSettingsStore>();
            SettingStoreManager.Setup(s => s.GetStore(ExistingVMStore)).Returns(store.Object);
	        store.Setup(s => s.ReadSetting<ArrayList>("SharedFolders")).Returns(new ArrayList
	        {
	            new Dictionary<string, object>
	            {
	                {"Name", "mysharename"},
	                {"GuestFolder", "c:\\mysharename"},
	                {"HostFolder", "c:\\mysharenameonhost"},
	            }

	        });

            Assert.IsTrue(Driver.GetSharedFolders(ExistingVM).Any(i => i.Name == "mysharename" && i.HostPath == "c:\\mysharenameonhost" && i.GuestPath == "c:\\mysharename"));

	    }
        #endregion

        #region "Snapshots"

        [TestMethod]
	    public void CallingCreateSnapshotOnExistingVMWillCallHypervisor()
        {
            Driver.CreateSnapshot(ExistingVM, "MySnapshot");
            HyperVisor.Verify(h => h.CreateSnapshot(ExistingVMVmxPath, "MySnapshot"));
        }

	    [TestMethod]
	    public void CallingRemoveSnapshotOnExistingVMWillCallHypervisor()
	    {
	        Driver.RemoveSnapshot(ExistingVM, "MySnapshot");
            HyperVisor.Verify(h => h.RemoveSnapshot(ExistingVMVmxPath, "MySnapshot"));
	    }

	    [TestMethod]
	    public void CallingRevertToSnapshotOnExistingVMWillCallHypervisor()
	    {
	        Driver.RevertToSnapshot(ExistingVM, "MySnapshot");
            HyperVisor.Verify(h =>h .RevertToSnapshot(ExistingVMVmxPath, "MySnapshot"));
	    }

	    [TestMethod]
	    public void CallingGetSnapshotsOnExistingVMWillReturnResultsFromHypervisor()
	    {
	        HyperVisor.Setup(h => h.GetSnapshots(ExistingVMVmxPath)).Returns(new [] {"MySnapshot", "AnotherSnapshot"});
            Assert.IsTrue(Driver.GetSnapshots(ExistingVM).All(s => s == "MySnapshot" || s == "AnotherSnapshot"));
	    }

        #endregion

        #region "Misc template functions"
        [TestMethod]
	    public void CallingConvertToToFullOnExistingVMWillCallHypervisor()
        {
            Driver.ConvertToFullDisk(ExistingVM);
            HyperVisor.Verify(h => h.ConvertToFullDisk(ExistingVMVmxPath));
        }

	    [TestMethod]
	    public void CallingImportTemplateWillUseFileSystemHelperToExtractTemplate()
	    {
            FileSystem.Setup(f => f.FileExists($"{TemplateFolder}\\{EnvironmentRandomName}\\Manifest.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{TemplateFolder}\\{EnvironmentRandomName}\\Manifest.json")).Returns("{ 'Name': 'NewTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");
            Driver.ImportTemplate("c:\\pathtotemplate.zip");
            FileSystem.Verify(f => f.ExtractArchive("c:\\pathtotemplate.zip", $"{TemplateFolder}\\{EnvironmentRandomName}"));
        }

	    [TestMethod]
	    public void CallingImportTemplateWillReadManifestOfExtractedVMToRenameIt()
	    {
            FileSystem.Setup(f => f.FileExists($"{TemplateFolder}\\{EnvironmentRandomName}\\Manifest.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{TemplateFolder}\\{EnvironmentRandomName}\\Manifest.json")).Returns("{ 'Name': 'NewTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");
            Driver.ImportTemplate("c:\\pathtotemplate.zip");
            FileSystem.Verify(f => f.MoveFolder($"{TemplateFolder}\\{EnvironmentRandomName}", $"{TemplateFolder}\\NewTemplate"));
        }

	    [TestMethod]
	    [ExpectedException(typeof (ApplicationException))]
	    public void CallingImportTemplateWhenTemplateAlreadyExistsWithNameWillThrow()
	    {
	        FileSystem.Setup(f => f.FolderExists($"{TemplateFolder}\\NewTemplate")).Returns(true);
            FileSystem.Setup(f => f.FileExists($"{TemplateFolder}\\{EnvironmentRandomName}\\Manifest.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile($"{TemplateFolder}\\{EnvironmentRandomName}\\Manifest.json")).Returns("{ 'Name': 'NewTemplate', OS: 'Windows', Description: 'Test description', Author: 'Test Author', Arch: 64, GeneratorText: '#Generator Text' }");
            Driver.ImportTemplate("c:\\pathtotemplate.zip");
        }

	    [TestMethod]
	    [ExpectedException(typeof (ApplicationException))]
	    public void CallingImportTemplateWhenManifestIsMissingWillThrow()
	    {
            FileSystem.Setup(f => f.FileExists($"{TemplateFolder}\\{EnvironmentRandomName}\\Manifest.json")).Returns(false);
            Driver.ImportTemplate("c:\\pathtotemplate.zip");
        }

	    [TestMethod]
	    public void CallingExportTemplateOnExistingTemplateWillCallFileSystemToArchive()
	    {
	        Driver.ExportTemplate(ExistingTemplate, "c:\\mytemplate.zip");
	        FileSystem.Verify(f => f.CreateArchive(ExistingTemplateFolder, "c:\\mytemplate.zip"));
	    }

	    [TestMethod]
	    public void CallingConvertVMToTemplateWillCopyTemplateVMXFolderToTemplateDirectory()
	    {
	        Driver.ConvertVMToTemplate(ExistingVM);
            FileSystem.Verify(f => f.Copy(ExistingVMXFolder, $"{TemplateFolder}\\{ExistingVM}"));
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingConvertVMToWhenTemplateWithNameAlreadyExistsWillThrow()
        {
            FileSystem.Setup(f => f.FolderExists($"{TemplateFolder}\\{ExistingVM}")).Returns(true);
            Driver.ConvertVMToTemplate(ExistingVM);
        }

        #endregion

        #region "Disks"

        [TestMethod]
	    public void CallingAddFloppyToExistingVMWillCallFloppyHelperToCreateFloppyImage()
	    {
            Driver.AddFloppy(ExistingVM, "c:\\filestoputonfloppy");
            FloppyUtil.Verify(f => f.Create("c:\\filestoputonfloppy", $"{ExistingVMXFolder}\\{EnvironmentRandomName}.flp"));
        }

	    [TestMethod]
	    public void CallingAddFloppyToExistingVMWillCallHypervisorForFreeFloppyID()
	    {
            Driver.AddFloppy(ExistingVM, "c:\\filestoputonfloppy");
            HyperVisor.Verify(h => h.GetFreeFloppyID(ExistingVMVmxPath));
        }

	    [TestMethod]
	    public void CallingAddFloppyToExistingVMWillCallHypervisorToEnableFloppyInterface()
	    {
            HyperVisor.Setup(h => h.GetFreeFloppyID(ExistingVMVmxPath)).Returns(1);
            Driver.AddFloppy(ExistingVM, "c:\\filestoputonfloppy");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "floppy1.present", "TRUE"));
        }

	    [TestMethod]
	    public void CallingAddFloppyToExistingVMWillCallHypervisorToSetDriveTypeToFile()
	    {
            HyperVisor.Setup(h => h.GetFreeFloppyID(ExistingVMVmxPath)).Returns(1);
            Driver.AddFloppy(ExistingVM, "c:\\filestoputonfloppy");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "floppy1.fileType", "file"));
        }

	    [TestMethod]
	    public void CallingAddFloppyToExistingVMWillCallHypervisorToSetfileNameToDrivePath()
	    {
            HyperVisor.Setup(h => h.GetFreeFloppyID(ExistingVMVmxPath)).Returns(1);
            Driver.AddFloppy(ExistingVM, "c:\\filestoputonfloppy");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "floppy1.fileName", $"{ExistingVMXFolder}\\{EnvironmentRandomName}.flp"));
        }

	    [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
	    public void CallingAddFloppyOnNonExistingVMWillThrow()
	    {
            Driver.AddFloppy("NonExistingVM", "c:\\filestoputonfloppy");
        }

	    [TestMethod]
	    public void CallingAddHddWillCallHypervisorForFreeID()
	    {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "");
	        HyperVisor.Verify(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide"));
	    }

	    [TestMethod]
	    public void CallingAddHddWillCheckIfBusTypeIsSupportedViaCapsAndNotThrowIfSupported()
	    {
	        Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "");
            Caps.VerifyGet(c => c.SupportedDriveBusTypes);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void CallingAddHddWillCheckIfBusTypeIsSupportedViaCapsAndThrowIfNotSupported()
        {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "NotSupported", 100, "");
            Caps.VerifyGet(c => c.SupportedDriveBusTypes);
        }

        [TestMethod]
	    public void CallingAddHddWillCallHypervisorToCreateDrive()
        {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "");

            HyperVisor.Verify(h => h.CreateVMDK($"{ExistingVMXFolder}\\{EnvironmentRandomName}.vmdk", 100, ""));
        }

        [TestMethod]
        public void CallingAddHddWillCheckThatDiskTypeisSupportedViaCaps()
	    {
	        Caps.Setup(c => c.SupportedDriveType).Returns(new[] { "lsilogic" });
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "lsilogic");

            Caps.VerifyGet(c => c.SupportedDriveType);
        }

	    [TestMethod]
	    public void CallingAddHddWillAcceptAnEmptyStringForDriveTypeToAllowHypervisorToChoose()
	    {
            Caps.Setup(c => c.SupportedDriveType).Returns(new[] { "lsilogic" });
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "");
        }

	    [TestMethod]
	    public void CallingAddHddWillCallHypervisorToSetDiskAsPresent()
	    {
            Caps.Setup(c => c.SupportedDriveType).Returns(new[] { "lsilogic" });
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
	        HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ide1:2.present", "TRUE"));
        }

        [TestMethod]
        public void CallingAddHddWillCallHypervisorToSetBuskAsPresent()
        {
            Caps.Setup(c => c.SupportedDriveType).Returns(new[] { "lsilogic" });
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ide1.present", "TRUE"));
        }

        [TestMethod]
        public void CallingAddHddWillCallHypervisorToSetFilePathToNewlyCreatedDisk()
        {
            Caps.Setup(c => c.SupportedDriveType).Returns(new[] { "lsilogic" });
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddHDD(ExistingVM, "ide", 100, "");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ide1:2.fileName", $"{EnvironmentRandomName}.vmdk"));
        }

	    [TestMethod]
	    public void CallingAddIsoWillCallHypervisorToGetFreeID()
	    {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddISO(ExistingVM, "ide", "c:\\mydiskimage.iso");
            HyperVisor.Verify(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide"));
        }

        [TestMethod]
	    public void CallingAddISOWillCallHypervisoToSetDiskBusAsPresent()
	    {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddISO(ExistingVM, "ide", "c:\\mydiskimage.iso");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ide1.present", "TRUE"));
        }

        [TestMethod]
        public void CallingAddISOWillCallHypervisoToSetDiskAsPresent()
        {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddISO(ExistingVM, "ide", "c:\\mydiskimage.iso");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ide1:2.present", "TRUE"));
        }

	    [TestMethod]
	    public void CallingAddISOWillCallHypervisorToSetFilenameToISOPath()
	    {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddISO(ExistingVM, "ide", "c:\\mydiskimage.iso");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ide1:2.fileName", "c:\\mydiskimage.iso"));
        }

        [TestMethod]
        public void CallingAddISOWillCallHypervisorToSetDeviceTypeToCDImage()
        {
            Caps.Setup(c => c.SupportedDriveBusTypes).Returns(new[] { "ide" });
            HyperVisor.Setup(h => h.GetFreeDiskID(ExistingVMVmxPath, "ide")).Returns(new Tuple<int, int>(1, 2));
            Driver.AddISO(ExistingVM, "ide", "c:\\mydiskimage.iso");
            HyperVisor.Verify(h => h.WriteSetting(ExistingVMVmxPath, "ide1:2.deviceType", "cdrom-image"));
        }

	    [TestMethod]
	    public void CallingClearCDRomWillCallHypervisorToClearCDRoms()
	    {
	        Driver.ClearCDRom(ExistingVM);
            HyperVisor.Verify(h => h.ClearCDRom(ExistingVMVmxPath));
	    }

	    [TestMethod]
	    public void CallingClearNetworkSettings()
	    {
	        Driver.ClearNetworkSettings(ExistingVM);
            HyperVisor.Verify(h => h.ClearNetworkSettings(ExistingVMVmxPath));
	    }

        [TestMethod]
	    public void CallingClearFloppys()
        {
            Driver.ClearFloppy(ExistingVM);
            HyperVisor.Verify(h => h.ClearFloppy(ExistingVMVmxPath));
        }

        #endregion
    }
}
