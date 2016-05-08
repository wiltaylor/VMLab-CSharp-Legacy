using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Web.Helpers;
using VMLab.Helper;

namespace VMLab.Model
{
    public interface IEnvironmentDetails
    {
        string WorkingDirectory { get; set; }
        string TemplateDirectory { get; set; }
        string VMRootFolder { get; set; }
        string VMRunPath { get; set; }
        string ScratchDirectory { get; set; }
        string ModuleRootFolder { get; set; }
        string VMwareDiskExe { get; set; }
        string VMwareExe { get; set; }
        string CurrentAction { get; set; }
        string SelectedDriver { get; set; }
        int SleepTimeOut { get; set; }
        IDictionary<string, object> PackageRepository { get; set; }
        PSCmdlet Cmdlet { get; set; }
        string ComponentPath { get; set; }
        string UniqueIdentifier();
        void UpdateEnvironment(PSCmdlet cmdlet);
        void PersistEnvironment();
    }

    public class EnvironmentDetails : IEnvironmentDetails
    {
        public string WorkingDirectory { get; set; }
        public string TemplateDirectory { get; set; }
        public string VMRootFolder { get; set; }
        public string VMRunPath { get; set; }
        public string ScratchDirectory { get; set; }
        public string ModuleRootFolder { get; set; }
        public string VMwareDiskExe { get; set; }
        public string VMwareExe { get; set; }
        public string CurrentAction { get; set; }
        public string SelectedDriver { get; set; }
        public int SleepTimeOut { get; set; }
        public IDictionary<string, object> PackageRepository { get; set; }

        public PSCmdlet Cmdlet { get; set; }
        public string ComponentPath { get; set; }

        private readonly IFileSystem _fileSystem;
        private readonly ICmdletPathHelper _cmdletPathHelper;
        private readonly IRegistryHelper _registryHelper;

        public EnvironmentDetails(IFileSystem fileSystem, ICmdletPathHelper cmdletPathHelper, IRegistryHelper registry)
        {
            _fileSystem = fileSystem;
            _cmdletPathHelper = cmdletPathHelper;
            _registryHelper = registry;
        }

        public string UniqueIdentifier()
        {
            return Guid.NewGuid().ToString();
        }

        public void UpdateEnvironment(PSCmdlet cmdlet)
        {
            Cmdlet = cmdlet;

            if(cmdlet != null)
                WorkingDirectory = _cmdletPathHelper.GetPath(cmdlet);   

            var settingsFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\VMLab";

            if (!_fileSystem.FolderExists(settingsFolder))
            {
                _fileSystem.CreateFolder(settingsFolder);
            }

            ModuleRootFolder = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetAssembly(typeof(EnvironmentDetails)).CodeBase).Path));

            var vmfolder =
                _registryHelper.GetRegistryValue(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                    "InstallPath", "<Not Installed>");

            if (vmfolder != "<Not Installed>")
            {
                VMRunPath = $"{vmfolder}\\vmrun.exe";
                VMwareDiskExe = $"{vmfolder}\\vmware-vdiskmanager.exe";
                VMwareExe = $"{vmfolder}\\vmware.exe"; ;
            }
            else
            {
                VMRunPath = vmfolder;
                VMwareDiskExe = vmfolder;
                VMwareExe = vmfolder;
            }

            ScratchDirectory = Environment.GetEnvironmentVariable("Temp");
            VMRootFolder = "_VM";

            if (!_fileSystem.FileExists($"{settingsFolder}\\Settings.json"))
                return;

            dynamic settings = Json.Decode(_fileSystem.ReadFile($"{settingsFolder}\\Settings.json"));

            TemplateDirectory = !string.IsNullOrEmpty(settings.TemplateDirectory) ? settings.TemplateDirectory : string.Empty;

            ScratchDirectory = !string.IsNullOrEmpty(settings.ScratchDirectory) ? settings.ScratchDirectory : Environment.GetEnvironmentVariable("Temp");

            VMRootFolder = !string.IsNullOrEmpty(settings.VMRootFolder) ? settings.VMRootFolder : "_VM";

            ComponentPath = !string.IsNullOrEmpty(settings.ComponentPath) ? settings.ComponentPath : string.Empty;

            SleepTimeOut = settings.SleepTimeOut ?? 5000;

            SelectedDriver = !string.IsNullOrEmpty(settings.SelectedDriver) ? settings.SelectedDriver : string.Empty;

            PackageRepository = settings.PackageRepository == null
                ? settings.PackageRepository
                : new Dictionary<string, object>();
        }

        public void PersistEnvironment()
        {
            var settingsFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\VMLab";

            if (!_fileSystem.FolderExists(settingsFolder))
            {
                _fileSystem.CreateFolder(settingsFolder);
            }

            var data = new Dictionary<string, object>();

            data.Add("TemplateDirectory",TemplateDirectory);
            data.Add("VMRootFolder", VMRootFolder);
            data.Add("ScratchDirectory", ScratchDirectory);
            data.Add("ComponentPath", ComponentPath);
            data.Add("PackageRepository", PackageRepository);
            data.Add("SleepTimeOut", SleepTimeOut);

            _fileSystem.SetFile($"{settingsFolder}\\settings.json", Json.Encode(data));
        }
    }
}
