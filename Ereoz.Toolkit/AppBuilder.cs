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
        private bool _isAutoRegisterViewWithViewModels;

        public AppBuilder()
        {
            ServiceContainer = new ServiceContainer();
            _navigationManager = new NavigationManager(ServiceContainer);

            _isAutoRegisterViewWithViewModels = true;
        }

        public static IServiceContainer ServiceContainer { get; private set; }

        public AppBuilder ConfigureDI(Action<IServiceContainer> configure)
        {
            configure(ServiceContainer);
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
            if (_isAutoRegisterViewWithViewModels)
                _navigationManager.AutoRegisterAllViewsWithViewModels(Assembly.GetEntryAssembly());

            return _navigationManager.CreateMainWindow<T>(appSettings);
        }
    }
}
