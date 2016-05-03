using System.Collections.Generic;

namespace VMLab.Model
{
    public interface ILabLibManager
    {
        bool TestLib(string name);
        void ImportLib(string name);
        void Reset();
    }


    public class LabLibManager : ILabLibManager
    {
        private readonly List<string> _importedlibs = new List<string>();

        public bool TestLib(string name)
        {
            return _importedlibs.Contains(name);
        }

        public void ImportLib(string name)
        {
            _importedlibs.Add(name);
        }

        public void Reset()
        {
            _importedlibs.Clear();
        }
    }
}
