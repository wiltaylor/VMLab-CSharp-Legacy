using System.Collections;
using System.Collections.Generic;
using System.Web.Helpers;
using VMLab.Helper;

namespace VMLab.Model
{
    public interface IVMSettingsStore
    {
        void WriteSetting<T>(string name, T value);
        T ReadSetting<T>(string name);
    }

    public class VMSettingsStore : IVMSettingsStore
    {

        private readonly string _path;
        private readonly IFileSystem _filesystem;

        public VMSettingsStore(string path, IFileSystem filesystem)
        {
            _path = path;
            _filesystem = filesystem;
        }

        public void WriteSetting<T>(string name, T value)
        {
            var settings = new Dictionary<string, object>();

            if (_filesystem.FileExists(_path))
            {
                settings = Json.Decode<Dictionary<string, object>>(_filesystem.ReadFile(_path));
            }

            if (settings.ContainsKey(name))
            {
                settings[name] = value;
            }
            else
            {
                settings.Add(name, value);
            }

            _filesystem.SetFile(_path, Json.Encode(settings));

        }

        public T ReadSetting<T>(string name)
        {
            if(!_filesystem.FileExists(_path))
                return default(T);

            var data = Json.Decode<Dictionary<string, object>>(_filesystem.ReadFile(_path));

            if(!data.ContainsKey(name))
                return default(T);

            return (T)data[name];   
        }
    }
}
