using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Helpers;
using VMLab.Helper;
using VMLab.Model;
using VMLab.Model.Caps;
using VMLab.Test.Model;

namespace VMLab.Drivers
{
    public class VMwareDriver : IDriver
    {
        private readonly IEnvironmentDetails _environment;
        private readonly ILog _log;
        private readonly IVMwareHypervisor _hypervisor;
        private readonly IVMSettingStoreManager _storeManager;
        private readonly IFileSystem _fileSystem;
        private readonly IFloppyUtil _floppyUtil;

        public ICaps Caps { get; }
        
        public VMwareDriver(IEnvironmentDetails env, ILog log, IVMwareHypervisor hypervisor, ICaps caps, IVMSettingStoreManager storeman, IFileSystem filesystem, IFloppyUtil floppyUtil)
        {
            _environment = env;
            _log = log;
            _hypervisor = hypervisor;
            Caps = caps;
            _storeManager = storeman;
            _fileSystem = filesystem;
            _floppyUtil = floppyUtil;
        }

        private void CheckWorkingEnvironment()
        {
            if (_environment.WorkingDirectory == null)
                throw new ApplicationException("Working directory has not been set!");

            if (!_fileSystem.FolderExists(_environment.WorkingDirectory))
            {
                throw new ApplicationException("Working directory doesn't exist!");
            }

            if (_environment.TemplateDirectory == null)
            {
                throw new ApplicationException("You must set a template directory before calling this method!");
            }

            if (!_fileSystem.FolderExists(_environment.TemplateDirectory))
            {
                throw new ApplicationException("Template directory doesn't exist!");
            }
        }

        public void CreateVMFromTemplate(string vmName, string template, string snapshot)
        {

            _log.Info($"Creating new VM {vmName} from template {template}/{snapshot}");
            CheckWorkingEnvironment();

            if (snapshot == null)
            {
                _log.Error("CreateVMFromTemplate: You can not pass null snapshot!");
                throw new ApplicationException("You can not pass null snapshot!");
            }

            if (_fileSystem.FolderExists(_environment.WorkingDirectory + $"\\{_environment.VMRootFolder}\\" + vmName))
            {
                _log.Error("CreateVMFromTemplate: VM with that name already exists!");
                throw new ApplicationException("VM with that name already exists!");
            }
            
            if (!_fileSystem.FolderExists(_environment.TemplateDirectory + "\\" + template))
            {
                _log.Error("CreateVMFromTemplate: Can't find template with specified name!");
                throw new ApplicationException("Can't find template with specified name");
            }

            if (vmName.Contains("\\") || vmName.Contains("/") || vmName.Contains(":") || vmName.Contains("?") ||
                vmName.Contains("*") || vmName.Contains("<") || vmName.Contains(">"))
            {
                _log.Error("CreateVMFromTemplate: Illegal charectesr in virtual machine name.");
                throw new ApplicationException("Illegal charectesr in virtual machine name.");
            }
            
            if (!_fileSystem.FileExists($"{_environment.TemplateDirectory}\\{template}\\manifest.json"))
            {
                _log.Error("CreateVMFromTemplate: Can't find template manifest file!");
                throw new ApplicationException("Can't find template manifest file!");
            }

            var vmxroot = $"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}\\{vmName}\\{_environment.UniqueIdentifier()}";

            _log.Debug($"Creating directory at {vmxroot}");
            _fileSystem.CreateFolder(vmxroot);

            _hypervisor.Clone($"{_environment.TemplateDirectory}\\{template}\\{template}.vmx", $"{vmxroot}\\{vmName}.vmx", snapshot ,CloneType.Linked);

            _fileSystem.Copy($"{_environment.TemplateDirectory}\\{template}\\manifest.json", $"{vmxroot}\\manifest.json");

            _hypervisor.WriteSetting($"{vmxroot}\\{vmName}.vmx" , "displayName", vmName);
        }

        public void CreateVM(string vmName, string vmxdata, string manifestdata)
        {
            _log.Info($"Creating new VM {vmName}");
            CheckWorkingEnvironment();

            if (GetVMPath(vmName, VMPath.VMFolder) != null)
            {
                _log.Error("CreateVM:Vm with that name already exists!");
                throw new ApplicationException("Vm with that name already exists!");
            }

            if (vmName.Contains("\\") || vmName.Contains("/") || vmName.Contains(":") || vmName.Contains("?") ||
                vmName.Contains("*") || vmName.Contains("<") || vmName.Contains(">"))
            {
                _log.Error("CreateVM:Illegal charectesr in virtual machine name.");
                throw new ApplicationException("Illegal charectesr in virtual machine name.");
            }
                
            var vmfolder = $"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}\\{vmName}\\{_environment.UniqueIdentifier()}";

            _fileSystem.CreateFolder(vmfolder);

            _fileSystem.SetFile($"{vmfolder}\\{vmName}.vmx", vmxdata);
            _fileSystem.SetFile($"{vmfolder}\\manifest.json", manifestdata);
        }

        public IDictionary<string, object>[] GetTemplates()
        {
            if (_environment.TemplateDirectory == null)
                throw new ApplicationException("You must set a template directory before calling this method!");

            var returnData = new List<IDictionary<string, object>>();

            foreach (var t in _fileSystem.GetSubFolders(_environment.TemplateDirectory))
            {
                if (!_fileSystem.FileExists($"{t}\\manifest.json")) continue;

                var obj = Json.Decode<Dictionary<string,object>>(_fileSystem.ReadFile($"{t}\\manifest.json"));

                if (!obj.ContainsKey("Name") || obj["Name"] == null)
                {
                    _log.Error("GetTemplates:Template has an invalid name.");
                    throw new ApplicationException("Template has an invalid name.");
                }


                if (!string.Equals(obj["Name"].ToString(), _fileSystem.GetPathLeaf(t), StringComparison.CurrentCultureIgnoreCase))
                {
                    _log.Error("GetTemplates:Template name doesn't match directory name!");
                    throw new ApplicationException("Template name doesn't match directory name!");
                }

                if (!obj.ContainsKey("OS") || obj["OS"] == null)
                {
                    _log.Error("GetTemplates:Template has an invalid OS.");
                    throw new ApplicationException("Template has an invalid OS.");
                }

                if (!obj.ContainsKey("Description") || obj["Description"] == null)
                {
                    _log.Error("GetTemplates:Template has an invalid Description.");
                    throw new ApplicationException("Template has an invalid Description.");
                }

                if (!obj.ContainsKey("Author") || obj["Author"] == null)
                {
                    _log.Error("GetTemplates:Template has an invalid Author.");
                    throw new ApplicationException("Template has an invalid Author.");
                }

                if (!obj.ContainsKey("Arch") || obj["Arch"] == null)
                {
                    _log.Error("GetTemplates:Template has an invalid Arch.");
                    throw new ApplicationException("Template has an invalid Arch.");
                }

                if (!obj.ContainsKey("GeneratorText") || obj["GeneratorText"] == null)
                {
                    _log.Error("GetTemplates:Template has an invalid GeneratorText.");
                    throw new ApplicationException("Template has an invalid GeneratorText");
                }


                returnData.Add(obj);
            }

            return returnData.ToArray();
        }

        public void AddNetwork(string vmname, string connectionType, string nicType, dynamic properties=null)
        {
            if (Caps.SupportedNetworkTypes.All(t => t != connectionType))
            {
                _log.Error("AddNetwork:Invalid network type. Check Caps for supported types.");
                throw new ApplicationException("Invalid network type. Check Caps for supported types.");
            }

            if (GetVMPath(vmname, VMPath.VMX) == null)
            {
                _log.Error("AddNetwork:VM Doesn't exist.");
                throw new ApplicationException("VM Doesn't exist!");
            }

            if (string.IsNullOrEmpty(nicType))
                nicType = Caps.DefaultNIC;

            if (Caps.SupportedNICs.All(n => n != nicType))
            {
                _log.Error("AddNetwork:Invalid NIC type.");
                throw new ApplicationException("Invalid NIC type");
            }

            var vmx = GetVMPath(vmname, VMPath.VMX);

            var freeNicID =
                _hypervisor.GetFreeNicID(vmx);

            _log.Info($"Adding network card to {vmname}. ConnectionType: {connectionType} NetworkCardType: {nicType}");

            _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.present", "TRUE");

            if (connectionType == "Bridged")
                _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.connectionType", "bridged");

            if (connectionType == "HostOnly")
                _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.connectionType", "hostonly");

            if (connectionType == "NAT")
                _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.connectionType", "nat");

            if (connectionType == "Isolated")
            {
                if (properties == null)
                {
                    _log.Error("AddNetwork:Can't create isolated network without extended properties.");
                    throw new ApplicationException("Can't create isolated network without extended properties!");
                }

                var pvn = _hypervisor.LookUpPVN(properties.NetworkName, $"{GetVMPath(vmname, VMPath.RootVMFolder)}\\pvn.json");
                _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.pvnID", pvn);
                _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.connectionType", "pvn");
            }

            if (connectionType == "VMNet")
            {
                if (properties == null)
                {
                    _log.Error("AddNetwork:Can't create VMNet network without extended properties.");
                    throw new ApplicationException("Can't create VMNet network without extended properties!");
                }

                if (!Regex.IsMatch(properties.VNet, "VMnet[0-9]{1,2}"))
                {
                    _log.Error("AddNetwork:Invalid VNet name expecte VMnet0 to VMnet19.");
                    throw new ApplicationException("Invalid VNet name expecte VMnet0 to VMnet19");
                }

                if (int.Parse(properties.VNet.Replace("VMnet", "")) > 19)
                {
                    _log.Error("AddNetwork:Can't assign VMnet larger than 19.");
                    throw new ApplicationException("Can't assign VMnet larger than 19");
                }


                _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.connectionType", "custom");
                _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.vnet", properties.VNet);
            }

            _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.virtualDev", nicType);
            _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.wakeOnPcktRcv", "FALSE");
            _hypervisor.WriteSetting(vmx, $"ethernet{freeNicID}.addressType", "generated");
        }

        public void SetMemory(string vmname, int qty)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (!_fileSystem.FileExists(vmx))
            {
                _log.Error("SetMemory:Can't set memory on VM that doesn't exist.");
                throw new ApplicationException("Can't set memory on VM that doesn't exist!");
            }

            _log.Info($"Setting memory on {vmname} to {qty}");

            _hypervisor.WriteSetting(vmx, "memsize", qty.ToString());
        }

        public void SetCPU(string vmname, int cpus, int cores)
        {
            if (cpus < 1)
            {
                _log.Error("SetCPU:Must have at least 1 cpu.");
                throw new ApplicationException("Must have at least 1 cpu");
            }

            if (cores < 1)
            {
                _log.Error("SetCPU:Must have at least 1 core.");
                throw new ApplicationException("Must have at least 1 core");
            }

            _log.Info($"Setting cpus on {vmname} to CPUS: {cpus} Cores: {cores}");

            var vmx = GetVMPath(vmname, VMPath.VMX);
            _hypervisor.WriteSetting(vmx, "numvcpus", (cpus * cores).ToString());
            _hypervisor.WriteSetting(vmx, "cpuid.coresPerSocket", cores.ToString());
        }

        public VMState GetVMState(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (!_fileSystem.FileExists(vmx))
            {
                _log.Error("GetVMState:Can't check state of vm that doesnt exist!");

                throw new ApplicationException("Can't check state of vm that doesnt exist!");
            }

            if (_hypervisor.GetRunningVMs().All(v => !string.Equals(v, vmx, StringComparison.CurrentCultureIgnoreCase)))
                return VMState.Shutdown;

            var metadata = Json.Decode(_fileSystem.ReadFile(GetVMPath(vmname, VMPath.Manifest)));

            var credentials = GetCredential(vmname);

            try
            {
                if (metadata.OS == "Windows")
                    if (_hypervisor.FileExistInGuest(vmx, credentials, "c:\\windows\\explorer.exe"))
                        return VMState.Ready;

                if (metadata.OS == "Unix")
                    if (_hypervisor.DirectoryExistInGuest(vmx, credentials, "/dev"))
                        return VMState.Ready;
            }
            catch (GuestVMPoweredOffException)
            {
                return VMState.Shutdown;
            }
            catch (VMRunFailedToRunException)
            {
                return VMState.Other;
            }
            
            return VMState.Other;
        }

        public void AddCredential(string vmname, string username, string password)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (!_fileSystem.FileExists(vmx))
            {
                _log.Error("AddCredential:VM doesn't exist!");
                throw new ApplicationException("VM doesn't exist!");
            }

            _log.Info($"Adding credentials to {vmname} Username: {username}");

            var store = _storeManager.GetStore(GetVMPath(vmname, VMPath.Store));
            var credentials = new List<IVMCredential>();
            var storedata = GetCredential(vmname);//  store.ReadSetting<IVMCredential[]>("Credentials");

            if(storedata != null)
                credentials.AddRange(storedata);

            credentials.Add(new VMCredential(username, password));

            store.WriteSetting("Credentials", credentials.ToArray());
        }

        public IVMCredential[] GetCredential(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (!_fileSystem.FileExists(vmx))
            {
                _log.Error("GetCredential:VM doesn't exist!");
                throw new ApplicationException("VM doesn't exist!");
            }

            var store = _storeManager.GetStore(GetVMPath(vmname, VMPath.Store));
            var credentials = store.ReadSetting<ArrayList>("Credentials");

            if (credentials == null)
                return new IVMCredential[] {};

            return credentials.Cast<IDictionary<string, object>>()
                    .Select(item => new VMCredential(item["Username"].ToString(), item["Password"].ToString()))
                    .Cast<IVMCredential>()
                    .ToArray();
        }

        public void StartVM(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (!_fileSystem.FileExists(vmx))
            {
                _log.Error("StartVM:VM doesn't exist!");
                throw new ApplicationException("VM doesn't exist!");
            }

            _log.Info($"Starting VM {vmname}");

            _hypervisor.StartVM(vmx);
        }

        public void StopVM(string vmname, bool force)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (!_fileSystem.FileExists(vmx))
            {
                _log.Error("StopVM:VM doesn't exist!");
                throw new ApplicationException("VM doesn't exist!");
            }

            _log.Info($"Stoppoing VM {vmname} Force: {force}");

            _hypervisor.StopVM(vmx, force);
        }

        public void ResetVM(string vmname, bool force)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (!_fileSystem.FileExists(vmx))
            {
                _log.Error("ResetVM:VM doesn't exist!");
                throw new ApplicationException("VM doesn't exist!");
            }

            _log.Info($"Restarting VM {vmname} Force: {force}");

            _hypervisor.ResetVM(vmx, force);
        }

        public string GetVMPath(string vmname, VMPath pathtype)
        {
            if (!_fileSystem.FolderExists($"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}\\{vmname}"))
                return null;

            var vmxfolder =
                    (from d in _fileSystem.GetSubFolders($"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}\\{vmname}")
                        where _fileSystem.FileExists($"{d}\\{vmname}.vmx")
                        select $"{d}").FirstOrDefault();

            switch (pathtype)
                {
                case VMPath.VMX:
                    return $"{vmxfolder}\\{vmname}.vmx";

                case VMPath.VMFolder:
                    return $"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}\\{vmname}";

                case VMPath.Manifest:
                    return $"{vmxfolder}\\manifest.json";

                case VMPath.Store:
                    return $"{vmxfolder}\\settings.json";
                case VMPath.VMXFolder:
                    return vmxfolder;
                case VMPath.RootVMFolder:
                    return $"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}";
                    
                default:
                    {
                        _log.Error("GetVMPath:Bad path type. Should not be able to get here!");
                        throw new ArgumentException("Bad path type. Should not be able to get here.");
                    }
                }
        }

        public void WriteVMSetting(string vmname, string setting, string value)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (vmx == null)
            {
                _log.Error("WriteVMSetting:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name.");
            }

            _log.Debug($"Writing setting to VM. VM:{vmname} setting: {setting} Value: {value}");

            _hypervisor.WriteSetting(vmx, setting, value);
        }

        public string ReadVMSetting(string vmname, string setting)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (vmx == null)
            {
                _log.Error("WriteVMSetting:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name.");
            }
            var value = _hypervisor.ReadSetting(vmx, setting);
            _log.Debug($"Reading setting from VM. VM:{vmname} setting: {setting} Value: {value}");

            return value;
        }

        public void ClearVMSetting(string vmname, string setting)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (vmx == null)
            {
                _log.Error("ClearVMSetting:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name.");
            }

            _log.Debug($"Clearing setting in VM. VM:{vmname} setting: {setting}");

            _hypervisor.ClearSetting(vmx, setting);
        }

        public void RemoveVM(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            var vmfolder = GetVMPath(vmname, VMPath.VMFolder);

            if (vmfolder == null)
                return;

            _log.Info($"Removing VM {vmname}");

            _hypervisor.RemoveVM(vmx);
        }

        public string[] GetProvisionedVMs()
        {
            if(!_fileSystem.FolderExists($"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}"))
                return new string[] {};

            return _fileSystem.GetSubFolders($"{_environment.WorkingDirectory}\\{_environment.VMRootFolder}")
                .Select(d => _fileSystem.GetPathLeaf(d)).ToArray();
        }

        public void ShowGUI(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            if (vmx == null)
            {
                _log.Error("ShowGUI:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name!");
            }

            _log.Info($"Showeing GUI for {vmname}");

            _hypervisor.ShowGUI(vmx);
        }

        public void CopyFileToGuest(string vmname, string hostPath, string guestPath)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            if (vmx == null)
            {
                _log.Error("CopyFileToGuest:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name!");
            }

            var creds = GetCredential(vmname);

            _log.Debug($"Copying file to guest. VM: {vmname} HostPath: {hostPath} GuestPath: {guestPath}");

            _hypervisor.CopyFileToGuest(vmx, creds, hostPath, guestPath);
        }

        public void CopyFileFromGuest(string vmname, string guestPath, string hostPath)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            if (vmx == null)
            {
                _log.Error("CopyFileFromGuest:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name!");
            }

            var creds = GetCredential(vmname);

            _log.Debug($"Copying file from guest. VM: {vmname} HostPath: {hostPath} GuestPath: {guestPath}");

            _hypervisor.CopyFileFromGuest(vmx, creds, guestPath, hostPath);
        }

        public void DeleteFileInGuest(string vmname, string path)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            if (vmx == null)
            {
                _log.Error("DeleteFileInGuest:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name!");
            }

            _log.Debug($"Deleting file in guest. VM: {vmname} Path: {path}");

            var creds = GetCredential(vmname);

            _hypervisor.DeleteFileInGuest(vmx, creds, path);
        }

        public void ExecuteCommand(string vmname, string path, string args, bool noWait=false, bool interactive=false, string username = null, string password = null)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            if (vmx == null)
            {
                _log.Error("ExecuteCommand:Can't find VM with that name!");
                throw new ApplicationException("Can't find VM with that name!");
            }

            var creds = GetCredential(vmname);

            if (username != null)
                creds = new IVMCredential[] {new VMCredential(username, password)};

            if (GetVMState(vmname) == VMState.Shutdown)
            {
                _log.Error("ExecuteCommand:Can't send command to VM because it is shutdown!");
                throw new GuestVMPoweredOffException("Can't send command to VM because it is shutdown!");
            }
            
            _log.Debug("Waiting for VM to become ready!");

            while (GetVMState(vmname) != VMState.Ready)
            {
                Thread.Sleep(1000); //sleep for a second before checking vm state.
            }

            _log.Debug($"Executing command! Path: {path} Args: {args} NoWait: {noWait} Interactive: {interactive}");

            _hypervisor.ExecuteCommand(vmx, creds, path, args, noWait, interactive);
        }

        public ICommandResult ExecuteCommandWithResult(string vmname, string[] commands, string username = null, string password = null)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            var creds = GetCredential(vmname);
            var metadata = Json.Decode(_fileSystem.ReadFile(GetVMPath(vmname, VMPath.Manifest)));
            var id = _environment.UniqueIdentifier();
            var stdoutfilename = $"{id}.stdout";
            var stderrfilename = $"{id}.stderr";
            var strlaunchscript = $"{id}-launch.cmd";

            if (username != null)
                creds = new IVMCredential[] { new VMCredential(username, password) };

            var scriptfile = $"{id}.cmd";
            var text = string.Join(Environment.NewLine, "@echo off", string.Join(Environment.NewLine, commands));

            if (metadata.OS == "Unix")
            {
                strlaunchscript = $"{id}-launch.sh";
                scriptfile = $"{id}.sh";
                text = string.Join("\n", "#!/bin/bash", string.Join("\n", commands));
            }

            var stdout = "";
            var stderr = "";
            
            var batchpath = $"{_environment.ScratchDirectory}\\{scriptfile}";
            var remotescriptpath = $"c:\\windows\\temp\\{scriptfile}";
            var command = $"{remotescriptpath} > c:\\windows\\temp\\{stdoutfilename} 2> c:\\windows\\temp\\{stderrfilename}";

            if (metadata.OS == "Unix")
            {
                remotescriptpath = $"/tmp/{scriptfile}";
                command = $"{remotescriptpath} > /tmp/{stdoutfilename} 2> /tmp/{stderrfilename}";
            }

            if (GetVMState(vmname) == VMState.Shutdown)
            {
                _log.Error("ExecuteCommandWithResult:Can't send command to VM because it is shutdown!");
                throw new GuestVMPoweredOffException("Can't send command to VM because it is shutdown!");
            }

            _log.Debug("Waiting for VM to become ready!");

            while (GetVMState(vmname) != VMState.Ready)
            {
                Thread.Sleep(1000); //sleep for a second before checking vm state.
            }

            _log.Debug("VM ready. Creating scripts to copy to vm.");

            _fileSystem.SetFile($"{_environment.ScratchDirectory}\\{strlaunchscript}", command);
            _fileSystem.SetFile(batchpath, text);
            _hypervisor.CopyFileToGuest(vmx, creds, batchpath, remotescriptpath);
            

            if (metadata.OS == "Unix")
            {
                _hypervisor.CopyFileToGuest(vmx, creds, $"{_environment.ScratchDirectory}\\{strlaunchscript}", $"/tmp/{strlaunchscript}");
                _hypervisor.ExecuteCommand(vmx, creds, "/bin/chmod", $"+x {remotescriptpath}", false, false);
                _hypervisor.ExecuteCommand(vmx, creds, "/bin/chmod", $"+x /tmp/{strlaunchscript}", false, false);
                _hypervisor.ExecuteCommand(vmx, creds, $"/tmp/{strlaunchscript}", "", false, false);
                _hypervisor.CopyFileFromGuest(vmx, creds, $"/tmp/{stdoutfilename}", $"{_environment.ScratchDirectory}\\{stdoutfilename}");
                _hypervisor.CopyFileFromGuest(vmx, creds, $"/tmp/{stderrfilename}", $"{_environment.ScratchDirectory}\\{stderrfilename}");
            }

            if (metadata.OS == "Windows")
            {
                _log.Debug("OS: Windows");
                _log.Debug("Executing script in guest!");
                _hypervisor.CopyFileToGuest(vmx, creds, $"{_environment.ScratchDirectory}\\{strlaunchscript}", $"c:\\windows\\temp\\{strlaunchscript}");
                _hypervisor.ExecuteCommand(vmx, creds, "c:\\windows\\system32\\cmd.exe", $"/c c:\\windows\\temp\\{strlaunchscript}", false, false);
                _hypervisor.CopyFileFromGuest(vmx, creds, $"c:\\windows\\temp\\{stdoutfilename}", $"{_environment.ScratchDirectory}\\{stdoutfilename}");
                _hypervisor.CopyFileFromGuest(vmx, creds, $"c:\\windows\\temp\\{stderrfilename}", $"{_environment.ScratchDirectory}\\{stderrfilename}");
            }
            
            if (_fileSystem.FileExists($"{_environment.ScratchDirectory}\\{stdoutfilename}"))
            {
                stdout = _fileSystem.ReadFile($"{_environment.ScratchDirectory}\\{stdoutfilename}");
                _fileSystem.DeleteFile($"{_environment.ScratchDirectory}\\{stdoutfilename}");
            }

            if (_fileSystem.FileExists($"{_environment.ScratchDirectory}\\{stderrfilename}"))
            {
                stderr = _fileSystem.ReadFile($"{_environment.ScratchDirectory}\\{stderrfilename}");
                _fileSystem.DeleteFile($"{_environment.ScratchDirectory}\\{stderrfilename}");
            }

            return new CommandResult(stdout, stderr);
        }

        public IPowershellCommandResult ExecutePowershell(string vmname, ScriptBlock code, string username = null, string password = null, object dataObject = null)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            var creds = GetCredential(vmname);
            var metadata = Json.Decode(_fileSystem.ReadFile(GetVMPath(vmname, VMPath.Manifest)));

            if (metadata.OS != "Windows")
            {
                _log.Error("ExecutePowershell: This action is only supported on windows!");
                throw new ApplicationException("This action is only supported on Windows!");
            }

            if (username != null)
                creds = new IVMCredential[] { new VMCredential(username, password) };

            var id = _environment.UniqueIdentifier();
            var scriptfile = $"{id}.ps1";
            var stdoutfile = $"{id}.stdout";
            var stderrfile = $"{id}.stderr";
            var datafile = $"{id}.xml";
            var result = new PowershellCommandResult();

            if (GetVMState(vmname) == VMState.Shutdown)
            {
                _log.Error("ExecutePowershell:Can't send command to VM because it is shutdown!");
                throw new GuestVMPoweredOffException("Can't send command to VM because it is shutdown!");
            }

            _log.Debug("Waiting for VM to become ready!");

            while (GetVMState(vmname) != VMState.Ready)
            {
                Thread.Sleep(1000); //sleep for a second before checking vm state.
            }

            _log.Debug("VM is ready copying scripts over and executing!");

            
            _fileSystem.SetFile($"{_environment.ScratchDirectory}\\{scriptfile}.cmd", $"C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe -executionpolicy bypass -OutputFormat XML -NoProfile -Noninteractive -File c:\\windows\\temp\\{scriptfile} > c:\\windows\\temp\\{stdoutfile} 2> c:\\windows\\temp\\{stderrfile}");

            if (dataObject != null)
            {
                _fileSystem.SetFile($"{_environment.ScratchDirectory}\\{datafile}", PSSerializer.Serialize(dataObject));
                _hypervisor.CopyFileToGuest(vmx, creds, $"{_environment.ScratchDirectory}\\{datafile}",
                    $"c:\\windows\\temp\\{datafile}");

                _fileSystem.SetFile($"{_environment.ScratchDirectory}\\{scriptfile}", $"$DataObject = import-clixml 'c:\\windows\\temp\\{datafile}'{Environment.NewLine}{code}");

            }
            else
            {
                _fileSystem.SetFile($"{_environment.ScratchDirectory}\\{scriptfile}", code.ToString());
            }
                


            _hypervisor.CopyFileToGuest(vmx, creds, $"{_environment.ScratchDirectory}\\{scriptfile}", $"c:\\windows\\temp\\{scriptfile}");
            _hypervisor.CopyFileToGuest(vmx, creds, $"{_environment.ScratchDirectory}\\{scriptfile}.cmd", $"c:\\windows\\temp\\{scriptfile}.cmd");
            _hypervisor.ExecuteCommand(vmx, creds, "c:\\windows\\system32\\cmd.exe", $"/c c:\\windows\\temp\\{scriptfile}.cmd", false, false);

            if(_hypervisor.FileExistInGuest(vmx, creds, $"c:\\windows\\temp\\{stdoutfile}"))
                _hypervisor.CopyFileFromGuest(vmx, creds, $"c:\\windows\\temp\\{stdoutfile}", $"{_environment.ScratchDirectory}\\{stdoutfile}");

            if (_hypervisor.FileExistInGuest(vmx, creds, $"c:\\windows\\temp\\{stderrfile}"))
                _hypervisor.CopyFileFromGuest(vmx, creds, $"c:\\windows\\temp\\{stderrfile}", $"{_environment.ScratchDirectory}\\{stderrfile}");

            if (_fileSystem.FileExists($"{_environment.ScratchDirectory}\\{stdoutfile}"))
            {
                //This is done because powershell puts #< CLIXML at the top of the file and the Deserialize will throw if it finds it.
                var filedata = _fileSystem.ReadFile($"{_environment.ScratchDirectory}\\{stdoutfile}").Replace($"#< CLIXML{Environment.NewLine}","");

                if(!string.IsNullOrEmpty(filedata))
                    result.Results =PSSerializer.Deserialize(filedata);
            }

            if (_fileSystem.FileExists($"{_environment.ScratchDirectory}\\{stderrfile}"))
            {
                var filedata = _fileSystem.ReadFile($"{_environment.ScratchDirectory}\\{stderrfile}").Replace($"#< CLIXML{Environment.NewLine}", "");

                if (string.IsNullOrEmpty(filedata)) return result;

                try
                {
                    result.Errors = PSSerializer.Deserialize(filedata);
                }
                catch (Exception)
                {
                    result.Errors = filedata;
                }
            }

            return result;
        }

        private IShareFolderDetails[] GetSharedFoldersFromSettings(IVMSettingsStore store)
        {
            var folders = store.ReadSetting<ArrayList>("SharedFolders");
            return folders?.Cast<IDictionary<string, object>>().Select(item => new ShareFolderDetails(item["Name"].ToString(), item["GuestFolder"].ToString(), item["HostFolder"].ToString())).Cast<IShareFolderDetails>().ToArray();
        }

        public void AddSharedFolder(string vmname, string hostpath, string sharename, string guestpath)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            var creds = GetCredential(vmname);
            var meta = Json.Decode(_fileSystem.ReadFile(GetVMPath(vmname, VMPath.Manifest)));
            var store = GetVMSettingStore(vmname);
            var newfolderdetails = new ShareFolderDetails(sharename, guestpath, hostpath);
            var folderarray = new List<IShareFolderDetails> {newfolderdetails};
            var existingfolders = GetSharedFoldersFromSettings(store);

            hostpath = _fileSystem.ConvertPathRelativeToFull(hostpath);

            if(existingfolders != null)
                folderarray.AddRange(existingfolders);

            if (GetVMState(vmname) == VMState.Shutdown)
            {
                _log.Error("AddSharedFolder:Can't send command to VM because it is shutdown!");
                throw new GuestVMPoweredOffException("Can't send command to VM because it is shutdown!");
            }

            _log.Debug("Waiting for VM to become ready!");

            while (GetVMState(vmname) != VMState.Ready)
            {
                Thread.Sleep(1000); //sleep for a second before checking vm state.
            }

            _log.Info($"Adding shared folder to VM! VM: {vmname} ShareName: {sharename} HostPath: {hostpath} GuestPath: {guestpath}");

            _hypervisor.AddSharedFolder(vmx, hostpath, sharename);

            if (meta.OS == "Unix")
            {
                if (meta.MountMode == "Link")
                {
                    _hypervisor.ExecuteCommand(vmx, creds, "/bin/ln", $"/mnt/hgfs/{sharename} {guestpath} -s", false, false);
                }
                else
                {
                    _hypervisor.ExecuteCommand(vmx, creds, "/bin/mkdir", guestpath, false, false);
                    _hypervisor.ExecuteCommand(vmx, creds, "/bin/mount", $"-v -t vmhgfs .host/{sharename} {guestpath}", false, false);
                }
            }

            if (meta.OS == "Windows")
            {
                ExecuteCommandWithResult(vmname, new []{ $"mklink /d \"{guestpath}\" \"\\\\vmware-host\\shared folders\\{sharename}\"" });
            }

            store.WriteSetting("SharedFolders", folderarray.ToArray());
        }

        public void RemoveSharedFolder(string vmname, string sharename)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            var creds = GetCredential(vmname);
            var store = GetVMSettingStore(vmname);
            var meta = Json.Decode(_fileSystem.ReadFile(GetVMPath(vmname, VMPath.Manifest)));
            var folders = GetSharedFoldersFromSettings(store);
            var currentfolder = folders.FirstOrDefault(f => f.Name == sharename);

            folders = folders.Where(f => f.Name != sharename).ToArray();

            if (currentfolder == null)
            {
                _log.Error("RemoveSharedFolder:Can't remove share that doesn't exist!");
                throw new ApplicationException("Can't remove share that doesn't exist!");
            }

            _log.Info($"Removing shared folder from VM! VM: {vmname} ShareName: {sharename}");

            _hypervisor.RemoveSharedFolder(vmx, sharename);

            if (meta.OS == "Windows")
            {
                ExecuteCommandWithResult(vmname, new[] { $"rd /s /q \"{currentfolder.GuestPath}\""});
            }

            if (meta.OS == "Unix")
            {
                if (meta.MountMode == "Link")
                {
                    _hypervisor.ExecuteCommand(vmx, creds, "/bin/rm",
                        $"-fr {currentfolder.GuestPath}", false, false);
                }
                else
                {
                    _hypervisor.ExecuteCommand(vmx, creds, "/bin/unmount",
                        currentfolder.GuestPath, false, false);
                }
            }

            store.WriteSetting("SharedFolders", folders);

        }

        public IShareFolderDetails[] GetSharedFolders(string vmname)
        {
            var store = GetVMSettingStore(vmname);
            return GetSharedFoldersFromSettings(store);
        }

        public void CreateSnapshot(string vmname, string snapshotname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            _log.Info($"Creating snapshot {snapshotname} on {vmname}");

            _hypervisor.CreateSnapshot(vmx, snapshotname);
        }

        public void RemoveSnapshot(string vmname, string snapshotname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            _log.Info($"Removing snapshot {snapshotname} from {vmname}");

            _hypervisor.RemoveSnapshot(vmx, snapshotname);
        }

        public void RevertToSnapshot(string vmname, string snapshotname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            _log.Info($"Reverting to snapshot {snapshotname} on {vmname}");

            _hypervisor.RevertToSnapshot(vmx, snapshotname);
        }

        public string[] GetSnapshots(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            return _hypervisor.GetSnapshots(vmx);
        }

        public void ConvertToFullDisk(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            _log.Info($"Converting disks to full on {vmname}");

            _hypervisor.ConvertToFullDisk(vmx);
        }

        public void ImportTemplate(string path)
        {
            var temppath = $"{_environment.TemplateDirectory}\\{_environment.UniqueIdentifier()}";

            _log.Info($"Importing template from {path}");

            _fileSystem.ExtractArchive(path, temppath);

            if (!_fileSystem.FileExists($"{temppath}\\Manifest.json"))
            {
                _log.Error("ImportTemplate:Can't find manifest in template meaning it is probably corrupt.");
                throw new ApplicationException("Can't find manifest in template meaning it is probably corrupt.");
            }

            var manifest = Json.Decode(_fileSystem.ReadFile($"{temppath}\\Manifest.json"));
            var newfoldername = $"{_environment.TemplateDirectory}\\{manifest.Name}";

            if (_fileSystem.FolderExists(newfoldername))
            {
                _log.Error("ImportTemplate:Can't import template because one with that name already exists.");
                throw new ApplicationException("Can't import template because one with that name already exists!");
            }

            _fileSystem.MoveFolder(temppath, newfoldername);
        }

        public void ExportTemplate(string templatename, string archivepath)
        {
            _log.Info($"Exporting template {templatename} to {archivepath}");
            _fileSystem.CreateArchive($"{_environment.TemplateDirectory}\\{templatename}", archivepath);
        }

        public void ConvertVMToTemplate(string vmname)
        {
            var vmxfolder = GetVMPath(vmname, VMPath.VMXFolder);
            var targetdir = $"{_environment.TemplateDirectory}\\{vmname}";

            if (_fileSystem.FolderExists(targetdir))
            {
                _log.Error("ConvertVMToTemplate:Can't convert to template because a template with that name already exists!");
                throw new ApplicationException("Can't convert to template because a template with that name already exists!");
            }
            _log.Info($"Converting {vmname} to template!");

            _fileSystem.Copy(vmxfolder, targetdir);
        }

        public void AddFloppy(string vmname, string sourcepath)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            var vmxfolder = GetVMPath(vmname, VMPath.VMXFolder);
            var floppypath = $"{vmxfolder}\\{_environment.UniqueIdentifier()}.flp";

            if (vmx == null)
            {
                _log.Error("AddFloppy:Can't add floppy because vm with that name doesn't exist!");
                throw new ApplicationException("Can't add floppy because vm with that name doesn't exist!");
            }

            _log.Info($"Creating and attaching floppy image! VM: {vmname} Source: {sourcepath}");

            _floppyUtil.Create(sourcepath, floppypath);

            var freeid = _hypervisor.GetFreeFloppyID(vmx);

            _hypervisor.WriteSetting(vmx, $"floppy{freeid}.present", "TRUE");
            _hypervisor.WriteSetting(vmx, $"floppy{freeid}.fileType", "file");
            _hypervisor.WriteSetting(vmx, $"floppy{freeid}.fileName", floppypath);
        }

        public void AddHDD(string vmname, string bus, int size, string disktype)
        {
            if (Caps.SupportedDriveBusTypes.All(c => c != bus))
            {
                _log.Error("AddHDD:Unsupported drive type! Please check caps!");
                throw new ApplicationException("Unsupported drive type! Please check caps.");
            }

            if(disktype != "")
                if (Caps.SupportedDriveType.All(c => c != disktype))
                {
                    _log.Error("AddHDD:Unsupported drive type! Please check caps!");
                    throw new ApplicationException("Unsupported drive type! Please check caps.");
                }

            _log.Info($"Adding disk to vm! VM: {vmname} Bus: {bus} Size: {size} DiskType: {disktype}");

            var vmx = GetVMPath(vmname, VMPath.VMX);
            var freeid = _hypervisor.GetFreeDiskID(vmx, bus);
            var diskfile = $"{_environment.UniqueIdentifier()}.vmdk";
            var diskpath = $"{GetVMPath(vmname, VMPath.VMXFolder)}\\{diskfile}";

            _hypervisor.CreateVMDK(diskpath, size, disktype);
            _hypervisor.WriteSetting(vmx, $"{bus}{freeid.Item1}.present", "TRUE");
            _hypervisor.WriteSetting(vmx, $"{bus}{freeid.Item1}:{freeid.Item2}.present", "TRUE");
            _hypervisor.WriteSetting(vmx, $"{bus}{freeid.Item1}:{freeid.Item2}.fileName", diskfile);
        }

        public void AddISO(string vmname, string bus, string path)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);
            var freeid = _hypervisor.GetFreeDiskID(vmx, bus);

            _log.Info($"Adding ISO! VM: {vmname} Bus: {bus} Path: {path}");

            _hypervisor.WriteSetting(vmx, $"{bus}{freeid.Item1}.present", "TRUE");
            _hypervisor.WriteSetting(vmx, $"{bus}{freeid.Item1}:{freeid.Item2}.present", "TRUE");
            _hypervisor.WriteSetting(vmx, $"{bus}{freeid.Item1}:{freeid.Item2}.fileName", path);
            _hypervisor.WriteSetting(vmx, $"{bus}{freeid.Item1}:{freeid.Item2}.deviceType", "cdrom-image");
        }

        public void ClearCDRom(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            _log.Info("Clearing CDRom Images!");

            _hypervisor.ClearCDRom(vmx);
        }

        public void ClearNetworkSettings(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            _log.Info("Clearing NICS!");

            _hypervisor.ClearNetworkSettings(vmx);
        }

        public void ClearFloppy(string vmname)
        {
            var vmx = GetVMPath(vmname, VMPath.VMX);

            _log.Info("Clearing Floppy Images!");

            _hypervisor.ClearFloppy(vmx);
        }

        public IVMSettingsStore GetVMSettingStore(string vmname)
        {
            return _storeManager.GetStore(GetVMPath(vmname, VMPath.Store));
        }

        public void CreateLabFile(string templateName)
        {
            if (_fileSystem.FileExists($"{_environment.WorkingDirectory}\\VMLab.ps1"))
            {
                _log.Error("CreateLabFile:VMLab file already exists!");
                throw new ApplicationException("VMLab file already exists!");
            }

            var template = GetTemplates().FirstOrDefault(t => t["Name"].ToString() == templateName);

            if (template == null)
            {
                _log.Error("CreateLabFile:Can't find Template!");
                throw new ApplicationException("Can't find Template.");
            }

            var lines = new StringBuilder();

            foreach (var i in (ArrayList)template["GeneratorText"])
            {
                lines.AppendLine(i.ToString());
            }

            _fileSystem.SetFile($"{_environment.WorkingDirectory}\\VMLab.ps1", lines.ToString());
        }
    }
}
