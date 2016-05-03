using System;
using System.Collections;
using System.Linq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.Model.Caps;

namespace VMLab.VMHandler
{
    public class HardDiskVMHandler : IVMNodeHandler
    {
        public string Name => "HardDisk";
        public int Priority => 15;

        private readonly IDriver _driver;
        private readonly ICaps _caps;

        public HardDiskVMHandler(IDriver driver, ICaps caps)
        {
            _driver = driver;
            _caps = caps;
        }

        public void PreProcess(string vmname, object settings, string action)
        {
            if (action != "start")
                return;

            var store = _driver.GetVMSettingStore(vmname);
            if (store.ReadSetting<bool>("HasBeenProvisioned"))
                return;

            if (settings is Hashtable)
                settings = new[] {settings};

            try
            {
                foreach (var item in ((object[])settings).Cast<Hashtable>())
                {
                    var disktype = "";

                    if(!item.ContainsKey("Bus"))
                        throw new InvalidNodeParametersException("Expected Bus Key in hashtable!", item);
                    if (!item.ContainsKey("Size"))
                        throw new InvalidNodeParametersException("Expected Size Key in hashtable!", item);
                    if (item.ContainsKey("Type"))
                        disktype = (string) item["Type"];

                    var bus = (string) item["Bus"];

                    if(_caps.SupportedDriveBusTypes.All(b => b != bus))
                        throw new InvalidNodeParametersException("Unsupported drive bus type. Please check hypervisor caps for supported types!", item);

                    if (disktype != "" && _caps.SupportedDriveType.All(b => b != disktype))
                        throw new InvalidNodeParametersException("Unsupported drive type. Please either leave blank to select default or see hypervisor caps for supported types!", item);


                    _driver.AddHDD(vmname, bus, (int)item["Size"], disktype);
                }
            }
            catch (InvalidCastException)
            {
                
                throw new InvalidNodeParametersException("Expected an array of hash tables!", settings);
            }

        }

        public void Process(string vmname, object settings, string action) { }

        public void PostProcess(string vmname, object setting, string action) { }
    }
}
