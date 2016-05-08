using Microsoft.Practices.Unity;
using Microsoft.Win32;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model.Caps;

namespace VMLab.Driver.VMWareWorkstation
{
    public class VMwareWorkstationDriverDetails : IDriverDetails
    {
        public string Name => "VMwareWorkstation";
        public bool Usable()
        {
            var vmfolder = Registry.GetValue(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\VMware, Inc.\\VMware Workstation",
                "InstallPath", "<Not Installed>").ToString();

            return vmfolder != "<Not Installed>";
        }

        public void OnSelect(IUnityContainer container)
        {
            container.RegisterType<IDriver, VMwareDriver>();
            container.RegisterType<IVMwareHypervisor, VMwareHypervisor>();
            container.RegisterType<ICaps, VMwareCaps>();
            container.RegisterType<IVMRun, VMRun>();
            container.RegisterType<IVMwareDiskExe, VMwareDiskExe>();
            container.RegisterType<IVMwareExe, VMwareExe>();
            
        }
    }
}
