using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Helpers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Driver.VMWareWorkstation
{
    public enum CloneType
    {
        Full,
        Linked
    }

    public interface IVMwareHypervisor
    {
        void Clone(string template, string target, string snapshot, CloneType type);
        void WriteSetting(string vmx, string name, string value);
        int GetFreeNicID(string vmx);
        string LookUpPVN(string networkName, string pvnfile);
        string[] GetRunningVMs();
        bool FileExistInGuest(string vmx, IVMCredential[] credentials, string path);
        bool DirectoryExistInGuest(string vmx, IVMCredential[] credentials, string path);
        void StartVM(string vmx);
        void StopVM(string vmx, bool force);
        void ResetVM(string vmx, bool force);
        string ReadSetting(string vmx, string setting);
        void ClearSetting(string vmx, string setting);
        void RemoveVM(string vmx);
        void ShowGUI(string vmx);
        void CopyFileToGuest(string vmx, IVMCredential[] creds, string hostPath, string guestPath);
        void CopyFileFromGuest(string vmx, IVMCredential[] creds, string guestPath, string hostPath);
        void DeleteFileInGuest(string vmx, IVMCredential[] creds, string path);
        void ExecuteCommand(string vmx, IVMCredential[] creds, string path, string args, bool noWait, bool interactive);
        void AddSharedFolder(string vmx, string hostfolder, string sharename);
        void RemoveSharedFolder(string vmx, string sharename);
        void CreateSnapshot(string vmx, string snapshotname);
        void RemoveSnapshot(string vmx, string snapshotname);
        void RevertToSnapshot(string vmx, string snapshotname);
        string[] GetSnapshots(string vmx);
        void ConvertToFullDisk(string vmx);
        int GetFreeFloppyID(string vmx);
        Tuple<int,int> GetFreeDiskID(string vmx, string bus);
        void CreateVMDK(string path, int size, string type);
        void ClearCDRom(string vmx);
        void ClearNetworkSettings(string vmx);
        void ClearFloppy(string vmx);
        void WaitForVMToBeReady(string vmx);
    }

    public class VMwareHypervisor : IVMwareHypervisor
    {
        private readonly IVMRun _vmrun;
        private readonly IFileSystem _filesystem;
        private readonly IVMwareExe _vMwareExe;
        private readonly IVMwareDiskExe _vMwareDiskExe;

        public VMwareHypervisor(IVMRun vmrun, IFileSystem filesystem, IVMwareExe vmwareexe, IVMwareDiskExe vmwarediskexe)
        {
            _vmrun = vmrun;
            _filesystem = filesystem;
            _vMwareExe = vmwareexe;
            _vMwareDiskExe = vmwarediskexe;
        }

        public void Clone(string template, string target, string snapshot, CloneType type)
        {
            if(!_filesystem.FileExists(template))
                throw new VMXDoesntExistException("Can't clone vmx that doesn't exist!", template);

            if(_filesystem.FileExists(target))
                throw new VMXAlreadyExistsException("Can't create clone when vm already exists at destination!", target);

            string results;

            switch (type)
            {
                case CloneType.Full:
                    results = _vmrun.Execute($"clone \"{template}\" \"{target}\" full -snapshot=\"{snapshot}\"");
                    break;
                case CloneType.Linked:
                    results = _vmrun.Execute($"clone \"{template}\" \"{target}\" linked -snapshot=\"{snapshot}\"");
                    break;
                default:
                    throw new NotImplementedException();
            }
            

            if(results.StartsWith("Error: Invalid snapshot name"))
                throw new SnapshotDoesntExistException("Can't clone vm when snapshot doesn't exist!", template, snapshot);

            if (results.Contains("Error:"))
            {
                throw new VMRunFailedToRunException("Unknown error!", results);
            }
        }

        public void WriteSetting(string vmx, string name, string value)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("VMX file doesn't exist!", vmx);

            var filedata = _filesystem.ReadFile(vmx);
            var lines =
                new List<string>(
                    (filedata.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)));

            var updated = false;
            for (var i = 0; i < lines.Count; i++)
                if (lines[i].StartsWith($"{name} = \""))
                {
                    lines[i] = $"{name} = \"{value}\"";
                    updated = true;
                }
                    
            if(!updated)
                lines.Add($"{name} = \"{value}\"");
            
            filedata = string.Join(Environment.NewLine, lines);

            _filesystem.SetFile(vmx, filedata);
        }

        public int GetFreeNicID(string vmx)
        {
            var filedata = _filesystem.ReadFile(vmx);
            var lines = filedata.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var highcount = 0;

            foreach (var id in from l in lines
                               select Regex.Match(l, "ethernet([0-9]{1,2}).present = \"(TRUE|FALSE)\"") 
                               into m where m.Success
                               select int.Parse(m.Groups[1].Value) 
                               into id where id <= highcount
                               select id)
                highcount = id + 1;

            return highcount;
        }

        public string LookUpPVN(string networkName, string pvnfile)
        {
            var pvnlist = new Dictionary<string, string>();

            if (_filesystem.FileExists(pvnfile))
            {
                pvnlist = Json.Decode<Dictionary<string, string>>(_filesystem.ReadFile(pvnfile));

                if (pvnlist.ContainsKey(networkName))
                    return pvnlist[networkName];
            }

            var rand = new Random();
            var randlist = new List<int>();
                
            var newPVN = $"{rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2}" +
                $"-{rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2} {rand.Next(0, 255):X2}";

            pvnlist.Add(networkName, newPVN);

            _filesystem.SetFile(pvnfile, Json.Encode(pvnlist));

            return newPVN;
            
        }

        public string[] GetRunningVMs()
        {
           return _vmrun.Execute("list").Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(s => !s.StartsWith("Total running VMs:")).ToArray();
        }

        public bool FileExistInGuest(string vmx, IVMCredential[] credentials, string path)
        {
            var userfailcount = 0;
            var lastfailmessage = "No VMRun Failed Result";
            foreach (var results in credentials.Select(c => _vmrun.Execute($"-T ws -gu {c.Username} -gp {c.Password} fileExistsInGuest \"{vmx}\" \"{path}\"")))
            {
                if (results.StartsWith("The file exists."))
                    return true;

                if (results.StartsWith("The file does not exist."))
                    return false;

                if (results.StartsWith("Error: Invalid user name or password for the guest OS."))
                {
                    userfailcount++;
                    continue;
                }

                if (results.StartsWith("Error: The virtual machine is not powered on:"))
                    throw new GuestVMPoweredOffException("Guest is not powerd on!");

                if (results.StartsWith("Error:"))
                    lastfailmessage = results;
            }

            if(userfailcount == credentials.Length)
                throw new BadGuestCredentialsException("Bad user name and password passed to guest os!", credentials);
            
            throw new VMRunFailedToRunException("Was unable to determine if file existed in vm!", lastfailmessage);
        }

        public bool DirectoryExistInGuest(string vmx, IVMCredential[] credentials, string path)
        {
            var userfailcount = 0;
            var lastfailmessage = "No VMRun Failed Result";
            foreach (var results in credentials.Select(c => _vmrun.Execute($"-T ws -gu {c.Username} -gp {c.Password} directoryExistsInGuest \"{vmx}\" \"{path}\"")))
            {
                if (results.StartsWith("The directory exists"))
                    return true;

                if (results.StartsWith("The directory does not exist."))
                    return false;

                if (results.StartsWith("Error: Invalid user name or password for the guest OS."))
                {
                    userfailcount++;
                    continue;
                }

                if (results.StartsWith("Error: The virtual machine is not powered on:"))
                    throw new GuestVMPoweredOffException("Guest is not powerd on!");

                if (results.StartsWith("Error:"))
                    lastfailmessage = results;
            }

            if (userfailcount == credentials.Length)
                throw new BadGuestCredentialsException("Bad user name and password passed to guest os!", credentials);

            throw new VMRunFailedToRunException("Was unable to determine if directory existed in vm!", lastfailmessage);
        }

        public void StartVM(string vmx)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't start vmx that doesn't exist!", vmx);

            var result = _vmrun.Execute($"start \"{vmx}\" nogui");

            if(result.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to start vm.", result);
        }

        public void StopVM(string vmx, bool force)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't stop vmx that doesn't exist!", vmx);

            var result = _vmrun.Execute(force ? $"stop \"{vmx}\" hard" : $"stop \"{vmx}\" soft");

            if (result.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to stop vm.", result);
        }

        public void ResetVM(string vmx, bool force)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't reset vmx that doesn't exist!", vmx);

            var result = _vmrun.Execute(force ? $"reset \"{vmx}\" hard" : $"reset \"{vmx}\" soft");

            if (result.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to reset vm.", result);
        }

        public string ReadSetting(string vmx, string setting)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't read setting if vmx doesn't exist!", vmx);

            var lines = _filesystem.ReadFile(vmx)
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            return lines.Select(l => Regex.Match(l, setting + " = \"(.+)\"")).Where(m => m.Success).Select(m => m.Groups[1].Value).FirstOrDefault();
        }

        public void ClearSetting(string vmx, string setting)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't clear setting if vmx doesn't exist!", vmx);

            var lines = _filesystem.ReadFile(vmx)
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(s => !s.StartsWith($"{setting} = \""));

            _filesystem.SetFile(vmx, string.Join(Environment.NewLine, lines.ToArray()));

        }

        public void RemoveVM(string vmx)
        {
            var vmfolder = Path.GetDirectoryName(Path.GetDirectoryName(vmx));

            if (_filesystem.FileExists(vmx))
            {
                var results = _vmrun.Execute($"deleteVM \"{vmx}\"");

                if (results.StartsWith("Error:"))
                    throw new VMRunFailedToRunException("Failed To remove VM.", results);
            }

            if(_filesystem.FolderExists(vmfolder))
                _filesystem.DeleteFolder(vmfolder, true);
        }

        public void ShowGUI(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't clear setting if vmx doesn't exist!", vmx);

            _vMwareExe.ShowVM(vmx);
        }

        public void CopyFileToGuest(string vmx, IVMCredential[] creds, string hostPath, string guestPath)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't copy file into vm if vmx doesn't exist!", vmx);

            if(!_filesystem.FileExists(hostPath))
                throw new FileNotFoundException("Can't find host file!");

            var badcredcount = 0;
            var lasterror = "";

            foreach (var result in creds.Select(c => _vmrun.Execute($"-T ws -gu {c.Username} -gp {c.Password} CopyFileFromHostToGuest \"{vmx}\" \"{hostPath}\" \"{guestPath}\"")))
            {
                if (string.IsNullOrEmpty(result))
                    return;

                if(result.StartsWith("Error: A file was not found"))
                    throw new FileDoesntExistInGuest("Can't copy file to guest!", vmx, guestPath);

                if (result.StartsWith("Error: Invalid user name or password for the guest OS"))
                    badcredcount++;

                if (result.StartsWith("Error:"))
                    lasterror = result;
            }

            if(badcredcount == creds.Length)
                throw new BadGuestCredentialsException("All supplied credentials are invald!", creds);

            throw new VMRunFailedToRunException("Unknown error!", lasterror);

        }

        public void CopyFileFromGuest(string vmx, IVMCredential[] creds, string guestPath, string hostPath)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't copy file from vm if vmx doesn't exist!", vmx);
            if(!_filesystem.FolderExists(Path.GetDirectoryName(hostPath)))
                throw new FileNotFoundException("Invalid host path. Folder target file is being created in doesn't exist!");

            var lasterror = string.Empty;
            var badcredcount = 0;
                
            foreach (var results in creds.Select(c => _vmrun.Execute($"-T ws -gu {c.Username} -gp {c.Password} CopyFileFromGuestToHost \"{vmx}\" \"{guestPath}\" \"{hostPath}\"")))
            {

                if (string.IsNullOrEmpty(results))
                    return;

                if(results.StartsWith("Error: A file was not found")) 
                    throw new FileDoesntExistInGuest("Can't copy file from guest because it doesn't exist!", vmx, guestPath);

                if (results.StartsWith("Error: Invalid user name or password for the guest OS"))
                {
                    badcredcount++;
                    continue;
                }

                if (results.StartsWith("Error:"))
                    lasterror = results;
            }

            if(badcredcount == creds.Length)
                throw new BadGuestCredentialsException("Bad username and password for guest vm", creds);

            throw new VMRunFailedToRunException("Unknown error", lasterror);
        }

        public void DeleteFileInGuest(string vmx, IVMCredential[] creds, string path)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't delete file from vm when vmx doesnt exist!", vmx);

            var lasterror = "";
            var badcredcount = 0;

            foreach (var results in creds.Select(c => _vmrun.Execute($"-T ws -gu {c.Username} -gp {c.Password} deleteFileInGuest \"{vmx}\" \"{path}\"")))
            {
                if (string.IsNullOrEmpty(results))
                    return;

                if(results.StartsWith("Error: A file was not found"))
                    throw new FileDoesntExistInGuest("Can't delete file because it doesnt exist in guest.", vmx, path);

                if (results.StartsWith("Error: Invalid user name or password for the guest OS"))
                {
                    badcredcount++;
                    continue;
                }

                if (results.StartsWith("Error:"))
                    lasterror = results;
            }
            
            if(badcredcount == creds.Length)
                throw new BadGuestCredentialsException("Incorrect guest username or password!", creds);

            throw new VMRunFailedToRunException("Unknown error when deleting file!", lasterror);
        }

        public void ExecuteCommand(string vmx, IVMCredential[] creds, string path, string args, bool noWait, bool interactive)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't execute command when vmx doesn't exist!", vmx);

            var badcredcount = 0;
            var lasterrormessage = "";
            var retrycount = 5;
            var doretry = true;

            var switches = new List<string>();

            if(noWait)
                switches.Add("-nowait");

            if(interactive)
                switches.Add("-interactive");

            var waitswitch = $" {string.Join(" ", switches.ToArray())} ";

            if (waitswitch == "  ")
                waitswitch = " ";

            while (doretry && retrycount > 0)
            {
                doretry = false;
                retrycount--;

                foreach (var c in creds)
                {
                    var results =
                        _vmrun.Execute(
                            $"-T ws -gu {c.Username} -gp {c.Password} runProgramInGuest \"{vmx}\"{waitswitch}\"{path}\" {args}");

                    if (string.IsNullOrEmpty(results))
                        return;

                    if (results.StartsWith("Guest program exited with non-zero exit code"))
                        return;

                    if (results.StartsWith("Error: A file was not found"))
                        throw new FileDoesntExistInGuest("Can't execute command because can't find program in guest!",
                            vmx, path);

                    if (results.StartsWith("Error: Invalid user name or password for the guest OS"))
                    {
                        badcredcount++;
                        continue;
                    }

                    if (results.StartsWith("Error:"))
                        lasterrormessage = results;
                }

                if (lasterrormessage != $"Error: Unknown error{Environment.NewLine}") continue;

                doretry = true;
                Thread.Sleep(1000);
            }

            if(badcredcount == creds.Length)
                throw new BadGuestCredentialsException("Username and password is incorrect!", creds);

            throw new VMRunFailedToRunException("Unknown error", lasterrormessage);
        }

        public void AddSharedFolder(string vmx, string hostfolder, string sharename)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't add folder when vmx doesn't exist!", vmx);

            if(!_filesystem.FolderExists(hostfolder))
                throw new FileNotFoundException("Can't find host folder!");

            var enableresults = _vmrun.Execute($"enableSharedFolders \"{vmx}\"");           
            
            if(!string.IsNullOrEmpty(enableresults) && enableresults.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to enable shared folders on vm!", enableresults);

            var addresults = _vmrun.Execute($"addSharedFolder \"{vmx}\" \"{sharename}\" \"{hostfolder}\"");

            if(!string.IsNullOrEmpty(addresults) && addresults.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to add shared folder on vm!", enableresults);
        }

        public void RemoveSharedFolder(string vmx, string sharename)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't remove shared folder from vm because vmx doesn't exist!", vmx);

            var result = _vmrun.Execute($"removeSharedFolder \"{vmx}\" \"{sharename}\"");

            if(!string.IsNullOrEmpty(result)&& result.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to remove shared folder!", result);
        }

        public void CreateSnapshot(string vmx, string snapshotname)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't create snapshot because vmx is not found!", vmx);

            var results = _vmrun.Execute($"snapshot \"{vmx}\" \"{snapshotname}\"");

            if(!string.IsNullOrEmpty(results) && results.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to create snapshot!", results);
        }

        public void RemoveSnapshot(string vmx, string snapshotname)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't remove snapshot because vmx is not found!", vmx);

            var results = _vmrun.Execute($"deleteSnapshot \"{vmx}\" \"{snapshotname}\"");

            if (!string.IsNullOrEmpty(results) && results.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to remove snapshot!", results);
        }

        public void RevertToSnapshot(string vmx, string snapshotname)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't revert to snapshot because vmx is not found!", vmx);

            var results = _vmrun.Execute($"revertToSnapshot \"{vmx}\" \"{snapshotname}\"");

            if (!string.IsNullOrEmpty(results) && results.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to revert to snapshot!", results);
        }

        public string[] GetSnapshots(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't list snapshots because vmx is not found!", vmx);

            var results = _vmrun.Execute($"listSnapshots \"{vmx}\"");

            if(string.IsNullOrEmpty(results))
                return new string[] {};

            if(results.StartsWith("Error:"))
                throw new VMRunFailedToRunException("Failed to get snapshots from vm!", results);
            
            return results.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(s => !s.StartsWith("Total snapshots:")).ToArray();
        }

        public void ConvertToFullDisk(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't convert to full disk because vmx is not found!", vmx);

            var disks = _filesystem.GetSubFiles(Path.GetDirectoryName(vmx)).Where( s => s.EndsWith(".vmdk"));

            foreach (var d in disks)
            {
                var newpath = $"{Path.GetDirectoryName(d)}{Path.GetFileNameWithoutExtension(d)}-full.vmdk";

                var results = _vMwareDiskExe.Execute($"-r \"{d}\" \"{newpath}\"");

                if(!string.IsNullOrEmpty(results) && results.StartsWith("Error:"))
                    throw new VDiskManException("Failed to expand disk!", results);

                _filesystem.DeleteFile(d);
                _filesystem.MoveFile(newpath, d);
            }
        }

        public int GetFreeFloppyID(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't get free floppy id because vmx is not found!", vmx);

            var data = _filesystem.ReadFile(vmx).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            return (from l in data select Regex.Match(l, "floppy([0-9]{1})") 
                    into m where m.Success
                    select int.Parse(m.Groups[1].Value) into index
                    select index + 1).Concat(new[] {0}).Max();

        }

        public Tuple<int, int> GetFreeDiskID(string vmx, string bus)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't get free drive id because vmx is not found!", vmx);

            if(bus != "ide" && bus != "scsi" && bus != "sata")
                throw new BadBusTypeException("Unexpected bus type passed", bus);

            var busid = 0;
            var nodeid = 0;

            var lines = _filesystem.ReadFile(vmx).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach (var l in lines)
            {
                var m = Regex.Match(l, bus + "([0-9]{1}):([0-9]{1,2})");
                if (m.Success)
                {
                    var bid = int.Parse(m.Groups[1].Value);
                    var nid = int.Parse(m.Groups[2].Value);

                    if (bid > busid)
                    {
                        busid = bid;
                        nodeid = nid + 1;
                    }
                    else if (bid == busid)
                    {
                        if (nid + 1 > nodeid)
                        {
                            nodeid = nid + 1;
                        }
                    }
                }

                //The values below are the maximum number of devices on addapter as per vmware documentation.
                if (bus == "scsi" && nodeid > 14) 
                {
                    busid++;
                    nodeid = 0;
                }

                if (bus == "sata" && nodeid > 29)
                {
                    busid++;
                    nodeid = 0;
                }

                if (bus == "ide" && nodeid > 1)
                {
                    busid++;
                    nodeid = 0;
                }
            }

            return new Tuple<int, int>(busid, nodeid);
        }

        public void CreateVMDK(string path, int size, string type)
        {
            if(type != "ide" && type != "buslogic" && type != "lsilogic")
                throw new ArgumentException("Bad disk type. Expected ide, buslogic or lsilogic.");

            if(!_filesystem.FolderExists(Path.GetDirectoryName(path)))
                throw new FileNotFoundException("Can't find parent folder that disk is being created in!", path);

            var result = _vMwareDiskExe.Execute($"-c -s {size}MB -a {type} -t 0 \"{path}\"");

            if(!string.IsNullOrEmpty(result) && result.StartsWith("Error:"))
                throw new VDiskManException("Error thrown while creating disk!", result);
        }

        public void ClearCDRom(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't clear cdroms because can't find vmx!", vmx);

            var lines = _filesystem.ReadFile(vmx)
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var cdroms = (from l in lines
                          select Regex.Match(l, "(ide|sata|scsi)([0-9]{1}):([0-9]{1,2}).deviceType = \"(cdrom-raw|cdrom-image)\"") into m
                          where m.Success
                          select $"{m.Groups[1].Value}{m.Groups[2].Value}:{m.Groups[3].Value}").ToList();

            _filesystem.SetFile(vmx, string.Join(Environment.NewLine, lines.Where(l => !cdroms.Any(l.StartsWith)).ToArray()));
        }

        public void ClearNetworkSettings(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't clear network settings because can't find vmx!", vmx);

            var text = _filesystem.ReadFile(vmx)
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("ethernet"));

            _filesystem.SetFile(vmx, string.Join(Environment.NewLine, text));
        }

        public void ClearFloppy(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't clear floppy drives because can't find vmx!", vmx);

            var text = new List<string>(_filesystem.ReadFile(vmx)
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("floppy")).ToArray());

            text.Add("floppy0.present = \"FALSE\"");

            _filesystem.SetFile(vmx, string.Join(Environment.NewLine, text));
        }

        public void WaitForVMToBeReady(string vmx)
        {
            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                vix.WaitOnTools();
            }
        }
    }
}
