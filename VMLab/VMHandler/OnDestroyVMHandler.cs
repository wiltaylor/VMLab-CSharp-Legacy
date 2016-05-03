using System;
using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class OnDestroyVMHandler : IVMNodeHandler
    {
        public string Name => "OnDestroy";
        public int Priority => 50;

        private readonly IDriver _driver;
        private readonly IScriptHelper _scriptHelper;

        public OnDestroyVMHandler(IDriver driver, IScriptHelper scripthelper)
        {
            _driver = driver;
            _scriptHelper = scripthelper;
        }

        public void PreProcess(string vmname, object settings, string action)
        {
            
        }

        public void Process(string vmname, object settings, string action)
        {
            
        }

        public void PostProcess(string vmname, object setting, string action)
        {
            if (action != "destroy")
                return;

            if (!(setting is ScriptBlock))
                throw new InvalidNodeParametersException("Expecting script block to be passed to OnCreate!", setting);

            _scriptHelper.Invoke((ScriptBlock)setting);
        }
    }
}
