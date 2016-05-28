using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VixCOM;

namespace VMLab.Driver.VMWareWorkstation
{
    public interface IVix : IDisposable
    {
        void ConnectToVM(string vmx);
        void Clone(string targetvmx, string snapshotname, bool linked);
        string[] GetRunningVMs();
        void LoginToGuest(string username, string password, bool interactive);
        void LoginOutOfGuest();
        bool FileExistInGuest(string path);
        bool DirectoryExistInGuest(string path);
        VixPowerState PowerState();
        void PowerOnVM();
        void PowerOffVM(bool force);
        void ResetVM(bool force);
        void Delete();
        void CopyFileToGuest(string hostfile, string guestfile);
        void CopyFileFromGuest(string hostfile, string guestfile);
        void DeleteFileInGuest(string path);
        void ExecuteCommand(string path, string args, bool activeWindow, bool wait);
        void AddShareFolder(string path, string sharename);
        void EnableSharedFolders();
        void DisableSharedFolders();
        void RemoveSharedFolder(string name);
        void CreateSnapshot(string name, string description, bool capturememory);
        void RemoveSnapshot(string name);
        void RevertToSnapshot(string name);
        string[] GetSnapshots();

        void WaitForToolsInGuest();

    }

    public enum VixPowerState
    {
        Off,
        Ready,
        Pending,
        Suspended,
    }

    public class Vix : IVix
    {
        private readonly VixLibClass _lib;
        private readonly IHost _host;
        private IVM2 _vm;

        public Vix()
        {
            _lib = new VixLibClass();

            var results = default(object);

            var job = _lib.Connect(Constants.VIX_API_VERSION, Constants.VIX_SERVICEPROVIDER_VMWARE_WORKSTATION, null, 0,
                null, null, 0, null, null);
            var err = job.Wait(new[] {Constants.VIX_PROPERTY_JOB_RESULT_HANDLE}, ref results);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException(
                    $"Error when trying to connect to VMWare workstation vix provider! Error code: {err}");
            CloseVixObject(job);

            _host = (IHost) ((object[]) results)[0];
        }

        public void ConnectToVM(string vmx)
        {
            var results = default(object);

            var job = _host.OpenVM(vmx, null);
            var err = job.Wait(new[] {Constants.VIX_PROPERTY_JOB_RESULT_HANDLE}, ref results);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error trying to open vmx file {vmx}! Error code: {err}");

            _vm = (IVM2) ((object[]) results)[0];
            CloseVixObject(job);
        }

        public void Clone(string targetvmx, string snapshotname, bool linked)
        {

            var snapshot = default(ISnapshot);
            var err = _vm.GetNamedSnapshot(snapshotname, out snapshot);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error retriving snapshot {snapshotname}! Error code: {err}");

            var results = default(object);
            var job = _vm.Clone(snapshot, linked ? Constants.VIX_CLONETYPE_LINKED : Constants.VIX_CLONETYPE_FULL,
                targetvmx, 0, null, null);

            err = job.Wait(new[] {Constants.VIX_PROPERTY_JOB_RESULT_HANDLE}, ref results);
            CloseVixObject(job);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error creating clone. Error core: {err}");

            var newvm = (IVM2) ((object[]) results)[0];

            CloseVixObject(newvm);
            CloseVixObject(snapshot);

        }

        public VixPowerState PowerState()
        {
            var result = default(object);
            var err = ((IVixHandle) _vm).GetProperties(new[] {Constants.VIX_PROPERTY_VM_POWER_STATE}, ref result);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error trying to get powerstate of vm. Error code: {err}");

            var state = (int) ((object[]) result)[0];

            if ((state & (Constants.VIX_POWERSTATE_POWERED_ON | Constants.VIX_POWERSTATE_TOOLS_RUNNING)) == (Constants.VIX_POWERSTATE_POWERED_ON | Constants.VIX_POWERSTATE_TOOLS_RUNNING))
                return VixPowerState.Ready;

            if ((state & Constants.VIX_POWERSTATE_POWERED_OFF) == Constants.VIX_POWERSTATE_POWERED_OFF)
                return VixPowerState.Off;

            return (state & Constants.VIX_POWERSTATE_SUSPENDED) == Constants.VIX_POWERSTATE_SUSPENDED? VixPowerState.Suspended : VixPowerState.Pending;
        }

        private void CloseVixObject(object vixObject)
        {
            try
            {
                ((IVixHandle2) vixObject).Close();
            }
            catch (Exception)
            {
                //Close is not supported in this version of Vix COM - Ignore
            }
        }

        public void Dispose()
        {
            CloseVixObject(_vm);
            CloseVixObject(_host);
            CloseVixObject(_lib);
        }

        public string[] GetRunningVMs()
        {
            var result = default(object);
            var job = _host.FindItems(Constants.VIX_FIND_RUNNING_VMS, null, -1, null);
            var err = job.Wait(Constants.VIX_PROPERTY_JOB_RESULT_HANDLE, ref result);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error while getting list of running virtual machines! Error code {err}");

            CloseVixObject(job);

            return ((object[]) result).Cast<string>().ToArray();
        }

        public void LoginToGuest(string username, string password, bool interactive)
        {
            var job = _vm.LoginInGuest(username, password,
                interactive ? Constants.VIX_LOGIN_IN_GUEST_REQUIRE_INTERACTIVE_ENVIRONMENT : 0, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when trying to login to virtual machine. Error code {err}");

            CloseVixObject(job);
        }

        public void LoginOutOfGuest()
        {
            var job = _vm.LogoutFromGuest(null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when trying to logout of virtual machine. Error code {err}");

            CloseVixObject(job);
        }

        public bool FileExistInGuest(string path)
        {
            var results = default(object);
            var job = _vm.FileExistsInGuest(path, null);
            var err = job.Wait(new[] {Constants.VIX_PROPERTY_JOB_RESULT_GUEST_OBJECT_EXISTS}, ref results);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when checking if a file exists! Error code: {err}");

            CloseVixObject(job);
            
            return (bool)((object[])results)[0];
        }

        public bool DirectoryExistInGuest(string path)
        {
            var results = default(object);
            var job = _vm.DirectoryExistsInGuest(path, null);
            var err = job.Wait(new[] { Constants.VIX_PROPERTY_JOB_RESULT_GUEST_OBJECT_EXISTS }, ref results);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when checking if a directory exists! Error code: {err}");

            CloseVixObject(job);

            return (bool)((object[])results)[0];
        }

        public void PowerOnVM()
        {
            var job = _vm.PowerOn(Constants.VIX_VMPOWEROP_NORMAL, null, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error occored while trying to start VM. Error code: {err}");

            CloseVixObject(job);
        }

        public void PowerOffVM(bool force)
        {
            var job = _vm.PowerOff(force ? Constants.VIX_VMPOWEROP_NORMAL : Constants.VIX_VMPOWEROP_FROM_GUEST, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error occored while trying to stop VM. Error code: {err}");

            CloseVixObject(job);
        }

        public void ResetVM(bool force)
        {
            var job = _vm.Reset(force ? Constants.VIX_VMPOWEROP_NORMAL : Constants.VIX_VMPOWEROP_FROM_GUEST, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error occored while trying to stop VM. Error code: {err}");

            CloseVixObject(job);
        }

        public void Delete()
        {
            var job = _vm.Delete(Constants.VIX_VMDELETE_DISK_FILES, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error occored while trying to delete VM. Error code: {err}");

            CloseVixObject(job);
        }

        public void CopyFileToGuest(string hostfile, string guestfile)
        {
            var job = _vm.CopyFileFromHostToGuest(hostfile, guestfile, 0, null, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable to copy file to guest! Error code: {err}");

            CloseVixObject(job);
        }

        public void CopyFileFromGuest(string hostfile, string guestfile)
        {
            var job = _vm.CopyFileFromGuestToHost(guestfile, hostfile, 0, null, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable to copy file from guest! Error code: {err}");

            CloseVixObject(job);
        }

        public void DeleteFileInGuest(string path)
        {
            var job = _vm.DeleteFileInGuest(path, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable to delete file in guest! Error code: {err}");

            CloseVixObject(job);
        }

        public void ExecuteCommand(string path, string args, bool activeWindow, bool wait)
        {
            var flags = 0;

            if (activeWindow)
                flags += Constants.VIX_RUNPROGRAM_ACTIVATE_WINDOW;

            if (!wait)
                flags += Constants.VIX_RUNPROGRAM_RETURN_IMMEDIATELY;

            var job = _vm.RunProgramInGuest(path, args, flags, null, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when trying to execute program in guest. Error code: {err}");
        }

        public void AddShareFolder(string path, string sharename)
        {
            var job = _vm.AddSharedFolder(sharename, path, Constants.VIX_SHAREDFOLDER_WRITE_ACCESS ,null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable to share folder in guest! Error code: {err}");

            CloseVixObject(job);
        }

        public void EnableSharedFolders()
        {
            var job = _vm.EnableSharedFolders(true, 0, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable to enable share folders in guest! Error code: {err}");

            CloseVixObject(job);
        }

        public void DisableSharedFolders()
        {
            var job = _vm.EnableSharedFolders(false, 0, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable to disable share folders on guest! Error code: {err}");

            CloseVixObject(job);
        }

        public void RemoveSharedFolder(string name)
        {
            var job = _vm.RemoveSharedFolder(name, Constants.VIX_SHAREDFOLDER_WRITE_ACCESS, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable remove shared folder from guest! Error code: {err}");

            CloseVixObject(job);
        }

        public void CreateSnapshot(string name, string description, bool capturememory)
        {
            var job = _vm.CreateSnapshot(name, description, capturememory ? Constants.VIX_SNAPSHOT_INCLUDE_MEMORY : 0, null, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Unable to take snapshot! Error code: {err}");

            CloseVixObject(job);
        }

        public void RemoveSnapshot(string name)
        {
            var snapshot = default(ISnapshot);
            var err = _vm.GetNamedSnapshot(name, out snapshot);

            if(_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error raised when searching for snapshot to remove. Error code: {err}");
            
            var job = _vm.RemoveSnapshot(snapshot, 0, null);
            err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error raised when trying to remove snapshot! Error code: {err}");

            CloseVixObject(job);
        }

        public void RevertToSnapshot(string name)
        {
            var snapshot = default(ISnapshot);
            var err = _vm.GetNamedSnapshot(name, out snapshot);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error raised when searching for snapshot to revert to. Error code: {err}");

            var job = _vm.RevertToSnapshot(snapshot, 0, null, null);
            err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error raised when trying to revert to snapshot! Error code: {err}");

            CloseVixObject(job);
        }

        public string[] GetSnapshots()
        {
            var returndata = new List<string>();

            var numofsnapshots = 0;
            var err = _vm.GetNumRootSnapshots(out numofsnapshots);

            for (var i = 0; i < numofsnapshots - 1; i++)
            {
                var snapshot = default(ISnapshot);
                err = _vm.GetRootSnapshot(i, out snapshot);

                if(_lib.ErrorIndicatesFailure(err))
                    throw new VixException($"Unable to get snapshot info. Error code {err}");

                returndata.Add(GetSnapshotName(snapshot));
                returndata.AddRange(GetSubSnasphots(snapshot));
            }

            return returndata.ToArray();
        }

        public void WaitForToolsInGuest()
        {
            var job = _vm.WaitForToolsInGuest(int.MaxValue, null);
            var err = job.WaitWithoutResults();

            if(_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when waiting for tools to become ready! Error Code: {err}");

            CloseVixObject(job);
        }

        private string[] GetSubSnasphots(ISnapshot parent)
        {
            var numchild = 0;
            var err = parent.GetNumChildren(out numchild);
            var returndata = new List<string>();

            if(_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when trying to get snapshot information! Error code: {err}");

            for (var i = 0; i < numchild - 1; i++)
            {
                var snapshot = default(ISnapshot);
                err = parent.GetChild(i, out snapshot);

                if (_lib.ErrorIndicatesFailure(err)) 
                    throw new VixException($"Error when accessing snapshot. Error code: {err}");
                
                returndata.Add(GetSnapshotName(snapshot));
                returndata.AddRange(GetSubSnasphots(snapshot));
            }

            return returndata.ToArray();

        }

        private string GetSnapshotName(ISnapshot snapshot)
        {
            var results = default(object);
            var err = ((IVixHandle)snapshot).GetProperties(new[] { Constants.VIX_PROPERTY_SNAPSHOT_DISPLAYNAME }, ref results);

            if(_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error when trying to retrive snapshot name! Error Code: {err}");

            return ((object[]) results).ToString();

        }

        private T GetProperty<T>(IVixHandle handle, int property)
        {
            var result = default(object);
            var err = handle.GetProperties(property, result);

            if(_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error trying to get property {property}! Error code {err}");

            if (typeof(T).IsArray)
                return (T) result;

            return (T) ((object[]) result)[0];
        }


    }
}
