using System;
using System.Collections;
using System.IO;
using System.Linq;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;
using VMLab.Model.Caps;

namespace VMLab.VMHandler
{
    public class ISOVMHandler : IVMNodeHandler
    {
        public string Name => "ISO";
        public int Priority => 15;

        private readonly IDriver _driver;
        private readonly ICaps _caps;
        private readonly IFileSystem _filesystem;

        public ISOVMHandler(IDriver driver, ICaps caps, IFileSystem filesystem)
        {
            _driver = driver;
            _caps = caps;
            _filesystem = filesystem;
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
                foreach (var item in ((object[]) settings).Cast<Hashtable>())
                {
                    if(!item.ContainsKey("Bus"))
                        throw new InvalidNodeParametersException("Expected Bus Key in hash table but didn't find one!", item);

                    if (!item.ContainsKey("Path"))
                        throw new InvalidNodeParametersException("Expected Path Key in hash table but didn't find one!", item);

                    var bus = item["Bus"].ToString();
                    var path = item["Path"].ToString();

                    if(_caps.SupportedDriveBusTypes.All(c => c != bus))
                        throw new InvalidNodeParametersException("Bus type is not a supported type as per the hypervisor caps.", item);

                    if(!_filesystem.FileExists(path))
                        throw new FileNotFoundException("ISO path supplied doesn't exist!");

                    _driver.AddISO(vmname, bus, path);
                }
            }
            catch(InvalidCastException)
            {
                throw new InvalidNodeParametersException("Expected array of hashtables!", settings);
            }

        }

        public void Process(string vmname, object settings, string action) { }
        public void PostProcess(string vmname, object setting, string action) { }
    }
}
