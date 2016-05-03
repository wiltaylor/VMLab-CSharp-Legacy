using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Model
{
    public interface IVMNodeHandler
    {
        string Name { get; }
        int Priority { get; }

        void PreProcess(string vmname, object settings, string action);
        void Process(string vmname, object settings, string action);
        void PostProcess(string vmname, object setting, string action);
    }
}
