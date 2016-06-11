using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        VixPowerState GetVMPowerState(string vmx);
    }

    public class VMwareHypervisor : IVMwareHypervisor
    {
        private readonly IFileSystem _filesystem;
        private readonly IVMwareExe _vMwareExe;
        private readonly IVMwareDiskExe _vMwareDiskExe;
        private readonly IEnvironmentDetails _environment;
        private readonly ICancellableAsyncActionManager _asyncAction;
        private readonly IRetryable _retryable;

        public VMwareHypervisor(IFileSystem filesystem, IVMwareExe vmwareexe, IVMwareDiskExe vmwarediskexe, IEnvironmentDetails environment, ICancellableAsyncActionManager asyncAction, IRetryable retryable)
        {
            _filesystem = filesystem;
            _vMwareExe = vmwareexe;
            _vMwareDiskExe = vmwarediskexe;
            _environment = environment;
            _asyncAction = asyncAction;
            _retryable = retryable;
        }

        public void Clone(string template, string target, string snapshot, CloneType type)
        {
            if(!_filesystem.FileExists(template))
                throw new VMXDoesntExistException("Can't clone vmx that doesn't exist!", template);

            if(_filesystem.FileExists(target))
                throw new VMXAlreadyExistsException("Can't create clone when vm already exists at destination!", target);

            var vix = ServiceDiscovery.GetInstance().GetObject<IVix>();
            vix.ConnectToVM(template);

            switch (type)
            {
                case CloneType.Full:
                    vix.Clone(target, snapshot, false);
                    break;
                case CloneType.Linked:
                    vix.Clone(target, snapshot, true);
                    break;
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

                if(Regex.IsMatch(lines[i], name + ".{1,2}=.{1,2}\".+\"$")) //Bug Fix: where double spaces got created when using vix api.
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
            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                return vix.GetRunningVMs();
            }
        }

        private void LoginToVM(string vmx, IVix vix, IVMCredential[] credentials, bool interactive = false)
        {
            for (var r = 0; r < 5; r++)
            {
                foreach (var c in credentials)
                {
                    try
                    {
                        vix.WaitForToolsInGuest();
                        vix.LoginToGuest(c.Username, c.Password, interactive);
                        return;
                    }
                    catch (GuestVMPoweredOffException e)
                    {
                        throw e;
                    }
                    catch
                    {
                        //skip
                    }
                }
                Thread.Sleep(10000);
            }

            throw new VixException("Unable to login with any credentails!");
        }

        public bool FileExistInGuest(string vmx, IVMCredential[] credentials, string path)
        {
            if (GetVMPowerState(vmx) == VixPowerState.Off)
                throw new GuestVMPoweredOffException("Can't check file exists when VM is powered off!");

            return _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    LoginToVM(vmx, vix, credentials);

                    var result = vix.FileExistInGuest(path);

                    vix.LoginOutOfGuest();

                    return result;
                }
            });
        }

        public bool DirectoryExistInGuest(string vmx, IVMCredential[] credentials, string path)
        {
            if (GetVMPowerState(vmx) == VixPowerState.Off)
                throw new GuestVMPoweredOffException("Can't check directory exists when VM is powered off!");

            return _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    LoginToVM(vmx, vix, credentials);

                    var result = vix.DirectoryExistInGuest(path);

                    vix.LoginOutOfGuest();

                    return result;
                }
            });
        }

        public void StartVM(string vmx)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't start vmx that doesn't exist!", vmx);

            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                vix.PowerOnVM();
            }
        }

        public void StopVM(string vmx, bool force)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't stop vmx that doesn't exist!", vmx);

            _retryable.Run(5000, 10, () =>
            {
                if (GetVMPowerState(vmx) == VixPowerState.Off)
                    return true;

                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);

                    if (!force)
                        vix.WaitForToolsInGuest();

                    vix.PowerOffVM(force);
                }

                return true;
            });

            //Wait for VM to power off.
            while(GetVMPowerState(vmx) != VixPowerState.Off) { Thread.Sleep(1000);}
        }

        public void ResetVM(string vmx, bool force)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't reset vmx that doesn't exist!", vmx);

            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                vix.ResetVM(force);
            }
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
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    vix.Delete();
                }
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

            _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    LoginToVM(vmx, vix, creds);
                    vix.CopyFileToGuest(hostPath, guestPath);
                    vix.LoginOutOfGuest();
                }
            });

        }

        public void CopyFileFromGuest(string vmx, IVMCredential[] creds, string guestPath, string hostPath)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't copy file from vm if vmx doesn't exist!", vmx);
            if(!_filesystem.FolderExists(Path.GetDirectoryName(hostPath)))
                throw new FileNotFoundException("Invalid host path. Folder target file is being created in doesn't exist!");

            _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    LoginToVM(vmx, vix, creds);
                    vix.CopyFileFromGuest(hostPath, guestPath);
                    vix.LoginOutOfGuest();
                }
            });
        }

        public void DeleteFileInGuest(string vmx, IVMCredential[] creds, string path)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't delete file from vm when vmx doesnt exist!", vmx);

            _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    LoginToVM(vmx, vix, creds);
                    vix.DeleteFileInGuest(path);
                    vix.LoginOutOfGuest();
                }
            });

        }



        public void ExecuteCommand(string vmx, IVMCredential[] creds, string path, string args, bool noWait, bool interactive)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't execute command when vmx doesn't exist!", vmx);

            _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    LoginToVM(vmx, vix, creds, interactive);
                    vix.WaitForToolsInGuest();
                    vix.ExecuteCommand(path, args, interactive, !noWait);                   
                    vix.LoginOutOfGuest();
                }
            });
        }

        public void AddSharedFolder(string vmx, string hostfolder, string sharename)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't add folder when vmx doesn't exist!", vmx);

            if(!_filesystem.FolderExists(hostfolder))
                throw new FileNotFoundException("Can't find host folder!");

            _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);
                    vix.WaitForToolsInGuest();
                    vix.EnableSharedFolders();
                    vix.AddShareFolder(hostfolder, sharename);
                }
            });
        }

        public void RemoveSharedFolder(string vmx, string sharename)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't remove shared folder from vm because vmx doesn't exist!", vmx);

            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                vix.WaitForToolsInGuest();
                vix.EnableSharedFolders();
                vix.RemoveSharedFolder(sharename);
            }
        }

        public void CreateSnapshot(string vmx, string snapshotname)
        {
            if(!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't create snapshot because vmx is not found!", vmx);

            var memdump = GetVMPowerState(vmx) == VixPowerState.Ready;
            
            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                vix.CreateSnapshot(snapshotname, $"Snapshot created by VMLab at {DateTime.Now}" ,memdump);
            }
        }

        public void RemoveSnapshot(string vmx, string snapshotname)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't remove snapshot because vmx is not found!", vmx);

            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                vix.RemoveSnapshot(snapshotname);
            }
        }

        public void RevertToSnapshot(string vmx, string snapshotname)
        {

            if (GetVMPowerState(vmx) != VixPowerState.Off)
            {
                StopVM(vmx, true);
            }
                

            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't revert to snapshot because vmx is not found!", vmx);

            var attempts = 5;

            while (attempts > 0)
            {
                try
                {
                    using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                    {
                        vix.ConnectToVM(vmx);
                        vix.RevertToSnapshot(snapshotname);
                        return;
                    }
                }
                catch
                {
                    attempts--;
                    Thread.Sleep(3000);

                    if (attempts == 0)
                        throw;
                }
            }
        }

        public string[] GetSnapshots(string vmx)
        {
            if (!_filesystem.FileExists(vmx))
                throw new VMXDoesntExistException("Can't list snapshots because vmx is not found!", vmx);

            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                return vix.GetSnapshots();
            }

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
            _asyncAction.Execute(delegate
            {
                using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
                {
                    vix.ConnectToVM(vmx);

                    while (true)
                    {
                        try
                        {
                            vix.WaitForToolsInGuest();
                            return;
                        }
                        catch
                        {
                            Thread.Sleep(3000);
                        }
                    }
                }
            });
        }

        public VixPowerState GetVMPowerState(string vmx)
        {
            using (var vix = ServiceDiscovery.GetInstance().GetObject<IVix>())
            {
                vix.ConnectToVM(vmx);
                return vix.PowerState();
            }
        }
    }
}
