using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace VMLab.Helper
{
    public interface IRegistryHelper
    {
        T GetRegistryValue<T>(string key, string valuename, T defaultvalue);
        void SetRegistryValue<T>(string key, string valuename, T value);
    }

    public class RegistryHelper : IRegistryHelper
    {
        public T GetRegistryValue<T>(string key, string valuename, T defaultvalue)
        {
            return (T)Registry.GetValue(key, valuename, defaultvalue);
        }

        public void SetRegistryValue<T>(string key, string valuename, T value)
        {
            Registry.SetValue(key, valuename, value);
        }
    }
}
