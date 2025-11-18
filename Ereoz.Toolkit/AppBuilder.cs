using Ereoz.Abstractions.DI;
using Ereoz.Abstractions.Messaging;
using Ereoz.Abstractions.MVVM;
using Ereoz.DI;
using Ereoz.Messaging;
using Ereoz.MVVM;
using Ereoz.WindowManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Ereoz.Toolkit
{
    public class AppBuilder
    {
        private NavigationManager _navigationManager;
        private bool _isAutoRegisterContracts;
        private bool _isAutoRegisterViewWithViewModels;

        public AppBuilder()
        {
            ServiceContainer = new ServiceContainer();
            _navigationManager = new NavigationManager(ServiceContainer);

            ServiceContainer.Register<IServiceContainer, ServiceContainer>().AsSingletone(ServiceContainer);
            ServiceContainer.Register<INavigationManager, NavigationManager>().AsSingletone(_navigationManager);
            ServiceContainer.Register<IMessenger,Messenger>().AsSingletone(new Messenger());

            _isAutoRegisterContracts = true;
            _isAutoRegisterViewWithViewModels = true;
        }

        public static IServiceContainer ServiceContainer { get; private set; }

        public AppBuilder ConfigureDI(Action<IServiceContainer> configure)
        {
            configure(ServiceContainer);
            _isAutoRegisterContracts = false;
            return this;
        }

        public AppBuilder ConfigureMVVM(Action<NavigationManager> configure)
        {
            configure(_navigationManager);
            _isAutoRegisterViewWithViewModels = false;
            return this;
        }

        public T CreateMainWindow<T>(WindowLocation appSettings = null) where T : Window
        {
            List<Type> allTypes = null;

            if (_isAutoRegisterContracts || _isAutoRegisterViewWithViewModels)
            {
                allTypes = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => !type.FullName.StartsWith("System")
                                && !type.FullName.StartsWith("Microsoft")
                                && !type.FullName.StartsWith("Windows")
                                && !type.FullName.StartsWith("Interop")
                                && !type.FullName.StartsWith("Internal")
                                && !type.FullName.StartsWith("StartupHook")
                                && !type.FullName.StartsWith("FxResources")
                                && !type.FullName.StartsWith("ThisAssembly")
                                && !type.FullName.StartsWith("FXAssembly")
                                && !type.FullName.StartsWith("AssemblyRef")
                                && !type.FullName.StartsWith("MatchState")
                                && !type.FullName.StartsWith("EmptyArray")
                                && !type.FullName.StartsWith("<")
                                && !type.FullName.StartsWith("_")
                                && !type.FullName.StartsWith("Ereoz.Messaging"))
                    .ToList();
            }

            if (_isAutoRegisterContracts)
                ((ServiceContainer)ServiceContainer).AutoRegisterAllContracts(allTypes);

            if (_isAutoRegisterViewWithViewModels)
                _navigationManager.AutoRegisterAllViewsWithViewModels(allTypes);

            return _navigationManager.CreateMainWindow<T>(appSettings);
        }
    }
}
