namespace VMLab.Model
{

    public interface IPackageRepository
    {
        string Name { get; }
        string Path { get;  }
    }

    public class PackageRepository : IPackageRepository
    {
        public string Name { get; }
        public string Path { get; }

        public PackageRepository(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }
}
