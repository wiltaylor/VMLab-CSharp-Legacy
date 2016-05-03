using System;
using VMLab.Drivers;
using VMLab.Model;

namespace VMLab.VMHandler
{
    public class MemoryVMHandler : IVMNodeHandler
    {
        public string Name => "Memory";
        public int Priority => 10;

        private readonly IDriver _drive;

        public MemoryVMHandler(IDriver drive)
        {
            _drive = drive;
        }

        public void PreProcess(string vmname, object settings, string action)
        {
            if (action != "start")
                return;

            if(!(settings is int))
                throw new InvalidNodeParametersException("Expected setting to be a positive int.", settings);

            if((int)settings < 1)
                throw new InvalidNodeParametersException("Expected memory qty to be at least 1mb.", settings);

            _drive.SetMemory(vmname, (int)settings);
        }

        public void Process(string vmname, object settings, string action) { }
        public void PostProcess(string vmname, object setting, string action) { }
    }
}
