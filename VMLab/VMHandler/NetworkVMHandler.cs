using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.Model.Caps;

namespace VMLab.VMHandler
{
    public class NetworkVMHandler : IVMNodeHandler
    {
        public string Name => "Network";
        public int Priority => 15;

        private readonly IDriver _driver;
        private readonly ICaps _caps;

        public NetworkVMHandler(IDriver driver, ICaps caps)
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

            try
            {
                if (settings is Hashtable)
                    settings = new[] {settings};

                foreach (var item in ((object[]) settings).Cast<Hashtable>())
                {
                    var nictype = "";

                    if(!item.ContainsKey("NetworkType"))
                        throw new InvalidNodeParametersException("Expected hashtable to have key NetworkType", item);

                    if (item.ContainsKey("NICType"))
                        nictype = (string) item["NICType"];

                    var nettype = (string) item["NetworkType"];

                    if(_caps.SupportedNetworkTypes.All(n => n != nettype))
                        throw new InvalidNodeParametersException("Invalid network type. Please check hypervisor caps for supported network types!", item);

                    if(nictype != "" && _caps.SupportedNICs.All(n => n != nictype))
                        throw new InvalidNodeParametersException("Invalid NIC type. Please check hypervisor caps for supported NIC types!", item);

                    var extra = new ExpandoObject() as IDictionary<string, object>;
                    foreach (var k in item.Keys.Cast<string>().Where(k => k != "NetworkType" && k != "NICType"))
                        extra.Add(k, item[k]);

                    

                    if (extra.Count == 0)
                        extra = null;


                    _driver.AddNetwork(vmname, nettype, nictype, extra);    
                }

            }
            catch (InvalidCastException)
            {
                throw new InvalidNodeParametersException("Expected array of hashtables", settings);
            }
        }

        public void Process(string vmname, object settings, string action) { }

        public void PostProcess(string vmname, object setting, string action) { }
    }
}
