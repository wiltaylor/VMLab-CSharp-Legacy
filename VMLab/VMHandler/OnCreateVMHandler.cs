using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class OnCreateVMHandler : IVMNodeHandler
    {
        public string Name => "OnCreate";
        public int Priority => 100;

        private readonly IDriver _driver;
        private readonly IScriptHelper _scriptHelper;

        public OnCreateVMHandler(IDriver driver, IScriptHelper scripthelper)
        {
            _driver = driver;
            _scriptHelper = scripthelper;
        }

        public void PreProcess(string vmname, object settings, string action)
        {
            
        }

        public void Process(string vmname, object settings, string action)
        {
            if (action != "start")
                return;

            if(!(settings is ScriptBlock))
                throw new InvalidNodeParametersException("Expecting script block to be passed to OnCreate!", settings);

            var store = _driver.GetVMSettingStore(vmname);
            if (store.ReadSetting<bool>("HasBeenProvisioned"))
                return;

            _scriptHelper.Invoke((ScriptBlock) settings);
        }

        public void PostProcess(string vmname, object setting, string action)
        {
            
        }
    }
}
