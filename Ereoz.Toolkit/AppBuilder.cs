using Ereoz.Abstractions.DI;
using Ereoz.DI;
using Ereoz.MVVM;
using Ereoz.WindowManagement;
using System;
using System.Reflection;
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
            if(_isAutoRegisterContracts)
                ((ServiceContainer)ServiceContainer).AutoRegisterAllContracts();

            if (_isAutoRegisterViewWithViewModels)
                _navigationManager.AutoRegisterAllViewsWithViewModels();

            return _navigationManager.CreateMainWindow<T>(appSettings);
        }
    }
}
