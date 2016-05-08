using System.Diagnostics;
using VMLab.Model;

namespace VMLab.Driver.VMWareWorkstation
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
