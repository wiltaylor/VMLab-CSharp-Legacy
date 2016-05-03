using System.Collections.Generic;
using Microsoft.Practices.Unity;
using VMLab.Drivers;
using VMLab.Model;
using VMLab.Model.Caps;
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
            if (_instance == null)
                _instance = new ServiceDiscovery();

            return _instance;
        }

        public T GetObject<T>()
        {
            return _container.Resolve<T>();
        }

        public IEnumerable<T> GetAllObject<T>()
        {
            return _container.ResolveAll<T>();
        }

        private ServiceDiscovery()
        {
            _container = new UnityContainer();
            TypeRegistaration();
        }

        private void TypeRegistaration()
        {
            _container.RegisterType<IDriver, VMwareDriver>();
            _container.RegisterType<IVMwareHypervisor, VMwareHypervisor>();
            _container.RegisterType<ICaps, VMwareCaps>();
            _container.RegisterType<IFileSystem, FileSystem>();
            _container.RegisterType<ILog, Log>(new ContainerControlledLifetimeManager());
            _container.RegisterType<IVMRun, VMRun>();
            _container.RegisterType<IVMwareDiskExe, VMwareDiskExe>();
            _container.RegisterType<IVMwareExe, VMwareExe>();
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
        }
    }
}
