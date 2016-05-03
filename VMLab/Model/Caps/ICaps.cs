namespace VMLab.Model.Caps
{
    public interface ICaps
    {
        bool CanCreateFromTemplate { get; }
        bool CanCreateFromText { get;  }
        bool CanListTemplates { get; }
        string[] SupportedNetworkTypes { get; }
        string[] SupportedNICs { get; }
        string DefaultNIC { get; }
        string[] SupportedDriveBusTypes { get; }
        string[] SupportedDriveType { get; }
    }
}
