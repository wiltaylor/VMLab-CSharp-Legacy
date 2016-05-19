using System;
using System.IO;
using System.Reflection;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;
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
            container.RegisterType<IVMwareDiskExe, VMwareDiskExe>();
            container.RegisterType<IVMwareExe, VMwareExe>();
            container.RegisterType<IVix, Vix>();

            try
            {
                var modulefolder = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetAssembly(typeof(EnvironmentDetails)).CodeBase).Path));
                Assembly.LoadFile($"{modulefolder}\\Interop.VixCOM.dll");
            }
            catch (Exception)
            {
                //already loaded.
            }

        }
    }
}
