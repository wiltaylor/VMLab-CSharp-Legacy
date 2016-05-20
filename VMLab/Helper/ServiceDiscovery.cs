using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.Test.Model;
using VMLab.VMHandler;

namespace VMLab.Helper
{
    public class ServiceDiscovery : IServiceDiscovery
    {
        private static IServiceDiscovery _instance;
        private readonly IocContainer _container;

        public string CurrentDriver { get; set; }
        
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
            return _container.GetObject<T>(CurrentDriver);
        }

        public IEnumerable<T> GetAllObject<T>()
        {
            return _container.GetObjects<T>(CurrentDriver);
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
            _container = new IocContainer();
            TypeRegistaration();

            CurrentDriver = "VMwareWorkstation"; //Hack: Till plugin system and selection is properly implimented.
        }

        

        private void TypeRegistaration()
        {
            var modulefolder = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetAssembly(typeof(EnvironmentDetails)).CodeBase).Path));
            //Loading plugins.
            foreach (var file in Directory.EnumerateFiles(modulefolder, "VMLab.Driver.*.dll"))
            {
                Assembly.LoadFile(file);
            }

            _container.Register<IFileSystem, FileSystem>();
            _container.Register<ILog, Log>().Singleton();
            _container.Register<IEnvironmentDetails, EnvironmentDetails>().Singleton();
            _container.Register<ICommandResult, CommandResult>();
            _container.Register<IFloppyUtil, FloppyUtil>();
            _container.Register<IShareFolderDetails, ShareFolderDetails>();
            _container.Register<IVMSettingsStore, VMSettingsStore>();
            _container.Register<IVMSettingStoreManager, VMSettingStoreManager>();
            _container.Register<ICmdletPathHelper, CmdletPathHelper>();
            _container.Register<IRegistryHelper, RegistryHelper>();
            _container.Register<IScriptHelper, ScriptHelper>();
            _container.Register<IIdempotentActionManager, IdempotentActionManager>().Singleton();
            _container.Register<ILabLibManager, LabLibManager>().Singleton();
            _container.Register<IPackageManager, PackageManager>().Singleton();

            _container.Register<IVMNodeHandler, CPUVMHandler>();
            _container.Register<IVMNodeHandler, CredentialVMHandler>();
            _container.Register<IVMNodeHandler, HardDiskVMHandler>();
            _container.Register<IVMNodeHandler, ISOVMHandler>();
            _container.Register<IVMNodeHandler, MemoryVMHandler>();
            _container.Register<IVMNodeHandler, NetworkVMHandler>();
            _container.Register<IVMNodeHandler, NewVMHandler>();
            _container.Register<IVMNodeHandler, OnCreateVMHandler>();
            _container.Register<IVMNodeHandler, OnDestroyVMHandler>();
            _container.Register<IVMNodeHandler, SharedFolderHandler>();
            _container.Register<IVMNodeHandler, TemplateVMHandler>();
            _container.Register<IVMNodeHandler, FloppyVMHandler>();

            var type = typeof (IDriverDetails);
            var alltypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes());

            var drivertypes = alltypes
                .Where(p => type.IsAssignableFrom(p) && p != type);

            foreach (var d in drivertypes)
            {
                _container.Register(type, d);
            }

            SelectDefaultDriver();
        }
    }
}
