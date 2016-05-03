using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Model
{
    public interface IVMActionHandler
    {
        string Name { get; }
        void Process(string vmname);
    }
}
