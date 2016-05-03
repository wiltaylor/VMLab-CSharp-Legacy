using System;
using System.Collections;
using System.Linq;
using VMLab.Drivers;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class SharedFolderHandler : IVMNodeHandler
    {
        public string Name => "SharedFolder";
        public int Priority => 50;

        private readonly IDriver _driver;

        public SharedFolderHandler(IDriver driver)
        {
            _driver = driver;
        }

        public void PreProcess(string vmname, object settings, string action) { }

        public void Process(string vmname, object settings, string action)
        {
            if (action != "start")
                return;

            var store = _driver.GetVMSettingStore(vmname);

            if (store.ReadSetting<bool>("HasBeenProvisioned"))
                return;

            if (settings is Hashtable)
                settings = new[] { settings };

            try
            {
                foreach (var i in ((object[])settings).Cast<Hashtable>())
                {
                    if (!i.ContainsKey("HostPath"))
                        throw new InvalidNodeParametersException("Expected HostPath key!", i);

                    if (!i.ContainsKey("GuestPath"))
                        throw new InvalidNodeParametersException("Expected GuestPath key!", i);

                    if (!i.ContainsKey("ShareName"))
                        throw new InvalidNodeParametersException("Expected GuestPath key!", i);

                    _driver.AddSharedFolder(vmname, (string)i["HostPath"], (string)i["ShareName"],
                        (string)i["GuestPath"]);
                }
            }
            catch (InvalidCastException)
            {
                throw new InvalidNodeParametersException("Expected object array of hashtables to be passed!", settings);
            }
        }

        public void PostProcess(string vmname, object setting, string action) { }
    }
}
