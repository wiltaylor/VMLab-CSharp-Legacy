using System;
using System.Collections;
using System.Linq;
using VMLab.Drivers;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class CredentialVMHandler : IVMNodeHandler
    {
        public string Name => "Credential";
        public int Priority => 15;

        private readonly IDriver _driver;

        public CredentialVMHandler(IDriver driver)
        {
            _driver = driver;
        }

        public void PreProcess(string vmname, object settings, string action)
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
                foreach (var c in ((object[]) settings).Cast<Hashtable>())
                {
                    if(!c.ContainsKey("Username"))
                        throw new InvalidNodeParametersException("Expected Username key!", c);

                    if (!c.ContainsKey("Password"))
                        throw new InvalidNodeParametersException("Expected Password key!", c);

                    _driver.AddCredential(vmname, c["Username"].ToString(), c["Password"].ToString());
                }
            }catch (InvalidCastException)
            {
                throw new InvalidNodeParametersException("Expecting array of hash tables containing Username and Password keys.", settings);
            }
        }

        public void Process(string vmname, object settings, string action) { }

        public void PostProcess(string vmname, object setting, string action) { }
    }
}
