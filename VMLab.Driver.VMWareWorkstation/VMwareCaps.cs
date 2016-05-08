using VMLab.Model.Caps;

namespace VMLab.Driver.VMWareWorkstation
{
    public class VMwareCaps : ICaps
    {
        public bool CanCreateFromTemplate => true;
        public bool CanCreateFromText => true;
        public bool CanListTemplates => true;
        public string[] SupportedNetworkTypes => new[] { "NAT", "HostOnly", "Bridged", "Isolated", "VMNet" };
        public string[] SupportedNICs => new string[] { "e1000", "e1000e", "vlance", "vmxnet" };
        public string DefaultNIC => "e1000";
        public string[] SupportedDriveBusTypes => new[] {"ide", "sata", "scsi"};
        public string[] SupportedDriveType => new[] {"lsilogic"};
    }
}
