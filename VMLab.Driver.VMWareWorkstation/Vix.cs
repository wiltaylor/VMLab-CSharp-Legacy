using System;
using VixCOM;

namespace VMLab.Driver.VMWareWorkstation
{
    public interface IVix : IDisposable
    {
        void ConnectToVM(string vmx);
        void WaitOnTools(int timeout = int.MaxValue);
        void Clone(string targetvmx, string snapshotname, bool linked);
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
                throw new VixException($"Error when trying to connect to VMWare workstation vix provider! Error code: {err}");
            CloseVixObject(job);

            _host = (IHost) ((object[]) results)[0];
        }

        public void ConnectToVM(string vmx)
        {
            var results = default(object);

            var job = _host.OpenVM(vmx, null);
            var err = job.Wait(new[] { Constants.VIX_PROPERTY_JOB_RESULT_HANDLE }, ref results);

            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error trying to open vmx file {vmx}! Error code: {err}");

            _vm = (IVM2)((object[])results)[0];
            CloseVixObject(job);
        }

        public void Clone(string targetvmx, string snapshotname, bool linked)
        {
            
            var snapshot = default(ISnapshot);
            var err = _vm.GetNamedSnapshot(snapshotname, out snapshot);

            if(_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error retriving snapshot {snapshotname}! Error code: {err}");

            var results = default(object);
            var job = _vm.Clone(snapshot, linked ? Constants.VIX_CLONETYPE_LINKED : Constants.VIX_CLONETYPE_FULL, targetvmx, 0, null, null);

            err = job.Wait(new[] {Constants.VIX_PROPERTY_JOB_RESULT_HANDLE}, ref results);
            CloseVixObject(job);

            if(_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Error creating clone. Error core: {err}");

            var newvm = (IVM2) ((object[]) results)[0];

            CloseVixObject(newvm);
            CloseVixObject(snapshot);

        }

        public void WaitOnTools(int timeout = int.MaxValue)
        {
            var job = _vm.WaitForToolsInGuest(timeout, null);
            var err = job.WaitWithoutResults();

            if (_lib.ErrorIndicatesFailure(err))
            {
                throw new VixException($"Error while waiting for tools to load. Probably timed out!");
            }

            CloseVixObject(job);

        }

        private void CloseVixObject(object vixObject)
        {
            try
            {
                ((IVixHandle2)vixObject).Close();
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
    }
}
