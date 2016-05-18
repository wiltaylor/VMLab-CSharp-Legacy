using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Driver.VMWareWorkstation
{
    public class VixException : Exception
    {
        public VixException(string message) : base(message)
        {
            
        }
    }
}
