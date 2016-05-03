using System.Diagnostics;
using VMLab.Model;

namespace VMLab.Helper
{
    public interface IVMwareDiskExe
    {
        string Execute(string command);
    }

    public class VMwareDiskExe : IVMwareDiskExe
    {
        private readonly IEnvironmentDetails _environment;

        public VMwareDiskExe(IEnvironmentDetails environment)
        {
            _environment = environment;
        }

        public string Execute(string command)
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = _environment.VMwareDiskExe,
                    Arguments = command,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            p.Start();

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();

            p.WaitForExit();

            return stdout + stderr;
        }
    }
}
