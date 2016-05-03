using System.Collections.Generic;

namespace VMLab.Helper
{
    public interface IServiceDiscovery
    {
        IEnumerable<T> GetAllObject<T>();
        T GetObject<T>();
    }
}