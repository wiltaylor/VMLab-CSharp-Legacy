using System.Diagnostics;
using VMLab.Model;

namespace VMLab.Helper
{
    public interface IFloppyUtil
    {
        void Create(string sourcepath, string imagepath);
    }

    public class FloppyUtil : IFloppyUtil
    {
        private readonly IEnvironmentDetails _environment;

        public FloppyUtil(IEnvironmentDetails env)
        {
            _environment = env;
        }

        public void Create(string sourcepath, string imagepath)
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = $"{_environment.ModuleRootFolder}\\bfi.exe",
                    Arguments = $"-f=\"{imagepath}\" \"{sourcepath}\"",
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
        }
    }
}
