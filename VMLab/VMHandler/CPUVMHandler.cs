using System;
using System.Collections;
using VMLab.Drivers;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class CPUVMHandler : IVMNodeHandler
    {
        public string Name => "CPU";
        public int Priority => 10;

        private IDriver _driver;

        public CPUVMHandler(IDriver driver)
        {
            _driver = driver;
        }

        public void PreProcess(string vmname, object settings, string action)
        {
            if(!(settings is Hashtable))
                throw new InvalidNodeParametersException("Expected settings to be a hash table!", settings);

            var cfg = (Hashtable)settings;

            if(!cfg.ContainsKey("CPUs"))
                throw new InvalidNodeParametersException("Expected hash table to contain CPUs key.", settings);

            if (!cfg.ContainsKey("Cores"))
                throw new InvalidNodeParametersException("Expected hash table to contain Cores key.", settings);

            if (action != "start")
                return;
            
            _driver.SetCPU(vmname, (int)cfg["CPUs"], (int)cfg["Cores"]);
        }

        public void Process(string vmname, object settings, string action) { }

        public void PostProcess(string vmname, object setting, string action) { }
    }
}
