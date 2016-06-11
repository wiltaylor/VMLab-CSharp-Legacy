using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Driver.VMWareWorkstation
{
    public class VixProcess
    {
        public string Name { get; set; }
        public long PID { get; set; }
        public string Owner { get; set; }
        public string CommandLine { get; set; }
    }
}
