using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.Test.Model;
using VMLab.VMHandler;

namespace VMLab.Helper
{
    public class ServiceDiscovery : IServiceDiscovery
    {
        private static IServiceDiscovery _instance;
        private readonly IUnityContainer _container;
        
        public static void UnitTestInject(IServiceDiscovery instance)
        {
            _instance = instance;
        }

        public static IServiceDiscovery GetInstance()
        {
            return _instance ?? (_instance = new ServiceDiscovery());
        }

        public T GetObject<T>()
        {
            return _container.Resolve<T>();
        }

        public IEnumerable<T> GetAllObject<T>()
        {
            return _container.ResolveAll<T>();
        }

        public void SelectDriver(string name)
        {
            var driver = GetAllObject<IDriverDetails>().FirstOrDefault(d => d.Name == name);
            var env = GetObject<IEnvironmentDetails>();

            if (driver == null)
                throw new VMLabDriverNotFoundException($"Can't find driver with name {name}");

            if (!driver.Usable())
                throw new VMLabDriverUnusableOnThisSystem("Driver is not usable on this system!");

            env.SelectedDriver = name;
                
            driver.OnSelect(_container);
        }

        public void SelectDefaultDriver()
        {
            var env = GetObject<IEnvironmentDetails>();
            env.UpdateEnvironment(null);

            if (string.IsNullOrEmpty(env.SelectedDriver))
            {
                foreach (var d in GetAllObject<IDriverDetails>().Where(d => d.Usable()))
                {
                    SelectDriver(d.Name);
                    return;
                }
            }
            else
            {
                SelectDriver(env.SelectedDriver);
            }

        }

        private ServiceDiscovery()
        {
            _container = new UnityContainer();
            TypeRegistaration();
        }

        

        private void TypeRegistaration()
        {
            var modulefolder = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetAssembly(typeof(EnvironmentDetails)).CodeBase).Path));
            //Loading plugins.
            foreach (var file in Directory.EnumerateFiles(modulefolder, "VMLab.Driver.*.dll"))
            {
                Assembly.LoadFile(file);
            }

            _container.RegisterType<IFileSystem, FileSystem>();
            _container.RegisterType<ILog, Log>(new ContainerControlledLifetimeManager());
            _container.RegisterType<IEnvironmentDetails, EnvironmentDetails>(new ContainerControlledLifetimeManager());
            _container.RegisterType<ICommandResult, CommandResult>();
            _container.RegisterType<IFloppyUtil, FloppyUtil>();
            _container.RegisterType<IShareFolderDetails, ShareFolderDetails>();
            _container.RegisterType<IVMSettingsStore, VMSettingsStore>();
            _container.RegisterType<IVMSettingStoreManager, VMSettingStoreManager>();
            _container.RegisterType<ICmdletPathHelper, CmdletPathHelper>();
            _container.RegisterType<IRegistryHelper, RegistryHelper>();
            _container.RegisterType<IVMSettingStoreManager, VMSettingStoreManager>();
            _container.RegisterType<IScriptHelper, ScriptHelper>();
            _container.RegisterType<IIdempotentActionManager, IdempotentActionManager>(new ContainerControlledLifetimeManager());
            _container.RegisterType<ILabLibManager, LabLibManager>(new ContainerControlledLifetimeManager());
            _container.RegisterType<IPackageManager, PackageManager>(new ContainerControlledLifetimeManager());

            _container.RegisterType<IVMNodeHandler, CPUVMHandler>("CPUVMHandler");
            _container.RegisterType<IVMNodeHandler, CredentialVMHandler>("CredentialVMHandler");
            _container.RegisterType<IVMNodeHandler, HardDiskVMHandler>("HardDiskVMHandler");
            _container.RegisterType<IVMNodeHandler, ISOVMHandler>("ISOVMHandler");
            _container.RegisterType<IVMNodeHandler, MemoryVMHandler>("MemoryVMHandler");
            _container.RegisterType<IVMNodeHandler, NetworkVMHandler>("NetworkVMHandler");
            _container.RegisterType<IVMNodeHandler, NewVMHandler>("NewVMHandler");
            _container.RegisterType<IVMNodeHandler, OnCreateVMHandler>("OnCreateVMHandler");
            _container.RegisterType<IVMNodeHandler, OnDestroyVMHandler>("OnDestroyVMHandler");
            _container.RegisterType<IVMNodeHandler, SharedFolderHandler>("SharedFolderHandler");
            _container.RegisterType<IVMNodeHandler, TemplateVMHandler>("TemplateVMHandler");
            _container.RegisterType<IVMNodeHandler, FloppyVMHandler>("FloppyVMHandler");

            var type = typeof (IDriverDetails);
            var alltypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes());

            var drivertypes = alltypes
                .Where(p => type.IsAssignableFrom(p) && p != type);

            foreach (var d in drivertypes)
            {
                _container.RegisterType(type, d, d.Name, new ContainerControlledLifetimeManager());
            }

            SelectDefaultDriver();
        }
    }
}
