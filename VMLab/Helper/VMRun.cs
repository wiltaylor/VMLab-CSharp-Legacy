using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VMLab.Model;

namespace VMLab.Drivers
{
    public interface IVMRun
    {
        string Execute(string command);
        string Execute(string command, int timeout);
    }

    public class VMRun : IVMRun
    {
        private readonly EnvironmentDetails _environment;

        private readonly string[] _retryErrors = { "Error: The VMware Tools are not running in the virtual machine:" };


        public VMRun(EnvironmentDetails env)
        {
            _environment = env;
        }

        public string Execute(string command)
        {
            return Execute(command, -1); //Any time out value of 0 or less is infinte timeout.
        }

        public string Execute(string command, int timeout)
        {
            var result = "";
            const int maxretry = 20;
            var retrycount = 0;

            while (true)
            {
                if(retrycount > maxretry)
                    throw new VMRunFailedToRunException("Maximum retry reached on command!", command);

                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = _environment.VMRunPath,
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

                if (timeout < 0)
                    p.WaitForExit();
                else
                    p.WaitForExit(timeout);

                result = stdout + stderr;

                if (_retryErrors.Any(e => result.Contains(e)))
                {
                    retrycount++;
                    Thread.Sleep(5000);
                    continue;
                }

                break;
            }

            return result;
        }
    }
}
