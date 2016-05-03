using System.Collections.Generic;
using System.Management.Automation;
using System.Security.Cryptography;
using VMLab.Model;
using VMLab.Model.Caps;
using VMLab.Test.Model;

namespace VMLab.Drivers
{
    public enum VMState
    {
        Ready,
        Shutdown,
        Other
    }

    public enum VMPath
    {
        VMX,
        VMFolder,
        VMXFolder,
        Manifest,
        Store,
        RootVMFolder
    }

    public interface IDriver
    {
        ICaps Caps { get; }
        void CreateLabFile(string templateName);
        void CreateVM(string vmName, string vmxdata, string manifestdata);
        void CreateVMFromTemplate(string vmName, string template, string snapshot);
        IDictionary<string, object>[] GetTemplates();
        void AddNetwork(string vmname, string connectionType, string nicType, dynamic properties=null);
        void SetMemory(string vmname, int qty);
        void SetCPU(string vmname, int cpus, int cores);
        VMState GetVMState(string vmname);
        void AddCredential(string vmname, string username, string password);
        IVMCredential[] GetCredential(string vmname);
        void StartVM(string vmname);
        void StopVM(string vmname, bool force);
        void ResetVM(string vmname, bool force);
        string GetVMPath(string vmname, VMPath pathtype);
        void WriteVMSetting(string vmname, string setting, string value);
        string ReadVMSetting(string vmname, string setting);
        void ClearVMSetting(string vmname, string setting);
        void RemoveVM(string vmname);
        string[] GetProvisionedVMs();
        void ShowGUI(string vmname);
        void CopyFileToGuest(string vmname, string hostPath, string guestPath);
        void CopyFileFromGuest(string vmname, string guestPath, string hostPath);
        void DeleteFileInGuest(string vmname, string path);
        void ExecuteCommand(string vmname, string path, string args, bool noWait = false, bool interactive = false, string username = null, string password = null);
        ICommandResult ExecuteCommandWithResult(string vmname, string[] commands, string username = null, string password = null);
        IPowershellCommandResult ExecutePowershell(string vmname, ScriptBlock code, string username = null, string password = null, object dataObject=null);
        void AddSharedFolder(string vmname, string hostpath, string sharename, string guestpath);
        void RemoveSharedFolder(string vmname, string sharename);
        IShareFolderDetails[] GetSharedFolders(string vmname);
        void CreateSnapshot(string vmname, string snapshotname);
        void RemoveSnapshot(string vmname, string snapshotname);
        void RevertToSnapshot(string vmname, string snapshotname);
        string[] GetSnapshots(string vmname);
        void ConvertToFullDisk(string vmname);
        void ImportTemplate(string path);
        void ExportTemplate(string templatename, string archivepath);
        void ConvertVMToTemplate(string vmname);
        void AddFloppy(string vmname, string sourcepath);
        void AddHDD(string vmname, string bus, int size, string disktype);
        void AddISO(string vmname, string bus, string path);
        void ClearCDRom(string vmname);
        void ClearNetworkSettings(string vmname);
        void ClearFloppy(string vmname);
        IVMSettingsStore GetVMSettingStore(string vmname);
    }
}