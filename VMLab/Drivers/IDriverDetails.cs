using Microsoft.Practices.Unity;

namespace VMLab.Drivers
{
    public interface IDriverDetails
    {
        string Name { get; }

        bool Usable();
        void OnSelect(IUnityContainer container);
    }
}
