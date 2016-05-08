using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Web.Helpers;
using VMLab.Helper;

namespace VMLab.Model
{

    public interface IPackageManager
    {
        void AddRepository(string name, string path);
        IPackageRepository[] GetRepository();
        bool PackageExists(string name, string version);
        void RunPackageAction(string name, string version, string action, string vmname);
        void ScanPackages();
        void RemoveRepository(string name);
        IPackage[] GetPackages();
    }

    public class PackageManager : IPackageManager
    {

        private readonly List<IPackageRepository>  _repos = new List<IPackageRepository>();
        private readonly List<IPackage> _packages = new List<IPackage>();
        
        private readonly IFileSystem _fileSystem;
        private readonly IScriptHelper _scriptHelper;
        private readonly IEnvironmentDetails _environment;

        public PackageManager(IFileSystem filesystem, IScriptHelper scriptHelper, IEnvironmentDetails env)
        {
            _fileSystem = filesystem;
            _scriptHelper = scriptHelper;
            _environment = env;

            if (_environment.PackageRepository == null)
                _environment.PackageRepository = new Dictionary<string, object>();

            foreach (var r in _environment.PackageRepository.Keys)
            {
                AddRepository(r, _environment.PackageRepository[r].ToString());
            }

            ScanPackages();
        }

        public void AddRepository(string name, string path)
        {
            if (_repos.Any(r => r.Name == name))
                throw new DuplicateRepositoryException($"A repository with the name {name} already exists!");

            if (!_fileSystem.FolderExists(path))
                throw new FileNotFoundException("Repository path doesn't exist!");

            _repos.Add(new PackageRepository(name, path));

            if (_environment.PackageRepository == null)
                _environment.PackageRepository = new Dictionary<string, object>();

            _environment.PackageRepository.Add(name, path);
            _environment.PersistEnvironment();

        }

        public void RunPackageAction(string name, string version, string action, string vmname)
        {
            var pkg = _packages.FirstOrDefault(p => p.Name == name && p.Version == version);

            var script = ScriptBlock.Create(_fileSystem.ReadFile($"{pkg.PackageRoot}\\package.ps1"));

            _scriptHelper.Invoke(script, new Dictionary<string, object> { {"Action", action }, {"VMName", vmname} } );
        }

        public void ScanPackages()
        {
            _packages.Clear();

            foreach (var rep in _repos)
            {
                foreach (var pkg in _fileSystem.GetSubFolders(rep.Path))
                {
                    foreach (var ver in _fileSystem.GetSubFolders(pkg))
                    {
                        if (_fileSystem.FileExists($"{ver}\\package.json"))
                        {
                            var data = Json.Decode<Dictionary<string, object>>(_fileSystem.ReadFile($"{ver}\\package.json"));

                            _packages.Add(new Package(data["Name"].ToString(), data["Version"].ToString(), ver));
                        }
                    }
                }
            }
        }

        public void RemoveRepository(string name)
        {
            _repos.RemoveAll(r => r.Name == name);

            if (_environment.PackageRepository == null)
                _environment.PackageRepository = new Dictionary<string, object>();

            _environment.PackageRepository.Remove(name);
            _environment.PersistEnvironment();
        }

        public IPackage[] GetPackages()
        {
            return _packages.ToArray();
        }

        public IPackageRepository[] GetRepository()
        {
            return _repos.ToArray();
        }

        public bool PackageExists(string name, string version)
        {
            return _packages.Any(p => p.Name == name && p.Version == version);
        }
    }
}
