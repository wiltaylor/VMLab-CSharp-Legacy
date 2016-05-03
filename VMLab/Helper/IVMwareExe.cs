using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerShell.Commands;
using VMLab.Model;

namespace VMLab.Helper
{
    public interface IVMwareExe
    {
        void ShowVM(string vmx);
    }

    public class VMwareExe : IVMwareExe
    {
        private readonly IEnvironmentDetails _environment;

        public VMwareExe(IEnvironmentDetails env)
        {
            _environment = env;
        }

        public void ShowVM(string vmx)
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = _environment.VMwareExe,
                    Arguments = $"-t -q \"{vmx}\"",
                }
            };

            p.Start();
        }
    }
}
