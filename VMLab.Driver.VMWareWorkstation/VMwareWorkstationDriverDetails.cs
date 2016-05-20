using System;
using System.IO;
using System.Reflection;
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

        public void OnSelect(IocContainer container)
        {
            container.Register<IDriver, VMwareDriver>(Name);
            container.Register<IVMwareHypervisor, VMwareHypervisor>(Name);
            container.Register<ICaps, VMwareCaps>(Name);
            container.Register<IVMwareDiskExe, VMwareDiskExe>(Name);
            container.Register<IVMwareExe, VMwareExe>(Name);
            container.Register<IVix, Vix>(Name);

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
