using System;
using System.Collections;
using System.Linq;
using VMLab.Drivers;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class TemplateVMHandler : IVMNodeHandler
    {
        public string Name => "Template";
        public int Priority => 0;

        private readonly IDriver _driver;

        public TemplateVMHandler(IDriver driver)
        {
            _driver = driver;
        }

        public void PreProcess(string vmname, object settings, string action)
        {
            if(!(settings is Hashtable))
                throw new InvalidNodeParametersException("Expecting parameters to be a hash table!", settings);

            var cfg = (Hashtable)settings;

            if(!cfg.ContainsKey("Name"))
                throw new InvalidNodeParametersException("Expecting parameter to contain Name key.", settings);

            if (!cfg.ContainsKey("Snapshot"))
                throw new InvalidNodeParametersException("Expecting parameter to contain Snapshot key.", settings);

            if (action != "start")
                return;

            if (_driver.GetProvisionedVMs()
                .Any(v => string.Equals(v, vmname, StringComparison.CurrentCultureIgnoreCase)))
                return;
            
            _driver.CreateVMFromTemplate(vmname, cfg["Name"].ToString(), cfg["Snapshot"].ToString());
        }

        public void Process(string vmname, object settings, string action)
        {

            var cfg = (Hashtable)settings;
            var keepPoweredOff = false;

            if (cfg.ContainsKey("KeepPoweredOff"))
                keepPoweredOff = (bool)cfg["KeepPoweredOff"];

            if (action == "start" && !keepPoweredOff)
                _driver.StartVM(vmname);

            if (action == "stop")
            {
                _driver.StopVM(vmname, false);
            }

            if (action != "destroy") return;

            if(_driver.GetVMState(vmname) == VMState.Other || _driver.GetVMState(vmname) == VMState.Ready)
                _driver.StopVM(vmname, true);

            _driver.RemoveVM(vmname);
        }

        public void PostProcess(string vmname, object setting, string action)
        {
            if (action != "start")
                return;

            var store = _driver.GetVMSettingStore(vmname);
            store.WriteSetting("HasBeenProvisioned", true);
        }
    }
}
