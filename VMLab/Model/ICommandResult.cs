using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Test.Model
{
    public interface ICommandResult
    {
        string STDOut { get; }
        string STDError { get; }
    }

    public class CommandResult : ICommandResult
    {
        public CommandResult(string stdout, string stderr)
        {
            STDOut = stdout;
            STDError = stderr;
        }

        public string STDOut { get; }
        public string STDError { get; }
    }
}
