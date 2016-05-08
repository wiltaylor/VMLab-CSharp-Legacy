namespace VMLab.Model
{
    public interface IPackage
    {
        string Name { get; }
        string Version { get; }
        string PackageRoot { get; }
    }

    public class Package :IPackage
    {
        public string Name { get; }
        public string Version { get; }
        public string PackageRoot { get; }

        public Package(string name, string version, string packageroot)
        {
            Name = name;
            Version = version;
            PackageRoot = packageroot;
        }
    }
}
