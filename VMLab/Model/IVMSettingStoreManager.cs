using System;
using VMLab.Helper;

namespace VMLab.Model
{
    public interface IVMSettingStoreManager
    {
        IVMSettingsStore GetStore(string path);
    }

    public class VMSettingStoreManager : IVMSettingStoreManager
    {

        private IFileSystem _filesystem;

        public VMSettingStoreManager(IFileSystem filesystem)
        {
            _filesystem = filesystem;
        }

        public IVMSettingsStore GetStore(string path)
        {
            return new VMSettingsStore(path, _filesystem);
        }
    }
}
