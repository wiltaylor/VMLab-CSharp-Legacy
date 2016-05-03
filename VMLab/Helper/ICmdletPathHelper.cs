using System.Management.Automation;

namespace VMLab.Helper
{
    public interface ICmdletPathHelper
    {
        string GetPath(PSCmdlet path);
    }

    public class CmdletPathHelper : ICmdletPathHelper
    {
        public string GetPath(PSCmdlet path)
        {
            return path.SessionState.Path.CurrentFileSystemLocation.Path;
        }
    }
}
