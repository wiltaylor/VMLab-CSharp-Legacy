using VMLab.Helper;

namespace VMLab.Drivers
{
    public interface IDriverDetails
    {
        string Name { get; }

        bool Usable();
        void OnSelect(IocContainer container);
    }
}
