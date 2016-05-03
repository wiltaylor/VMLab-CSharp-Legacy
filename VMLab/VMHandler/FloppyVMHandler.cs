using VMLab.Drivers;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class FloppyVMHandler : IVMNodeHandler
    {
        public string Name => "Floppy";
        public int Priority => 15;

        private readonly IDriver _driver;

        public FloppyVMHandler(IDriver driver)
        {
            _driver = driver;
        }


        public void PreProcess(string vmname, object settings, string action)
        {
            var store = _driver.GetVMSettingStore(vmname);
            if (store.ReadSetting<bool>("HasBeenProvisioned"))
                return;
            
            if (action != "start")
                return;

           _driver.AddFloppy(vmname, settings.ToString());
        }

        public void Process(string vmname, object settings, string action)
        {
            
        }

        public void PostProcess(string vmname, object setting, string action)
        {
            
        }
    }
}
