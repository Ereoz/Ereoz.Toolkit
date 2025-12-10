using Ereoz.Abstractions.DI;
using Ereoz.Abstractions.Logging;
using Ereoz.Abstractions.Messaging;
using Ereoz.Abstractions.MVVM;
using Ereoz.DI;
using Ereoz.Logging;
using Ereoz.Messaging;
using Ereoz.MVVM;
using Ereoz.WindowManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Ereoz.Toolkit
{
    public class AppBuilder
    {
        private NavigationManager _navigationManager;
        private bool _isAutoRegisterContracts;
        private bool _isAutoRegisterViewWithViewModels;
        private Window _startWindow;
        private string _appName;
        private Assembly _callingAssembly;

        internal static string FileName { get; private set; }
        internal static string ProductName { get; private set; }
        internal static Version Version { get; private set; }
        internal static string DataFolder { get; private set; }

        public AppBuilder() : this("..\\data") { }

        public AppBuilder(string dataFolder)
        {
            if (string.IsNullOrWhiteSpace(dataFolder))
                throw new ArgumentException("DataFolder must not be null");

            DataFolder = dataFolder;            

            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            _callingAssembly = Assembly.GetCallingAssembly();
            Version = _callingAssembly.GetName().Version;
            FileName = Path.GetFileNameWithoutExtension(_callingAssembly.Location);
            ProductName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(_callingAssembly, typeof(AssemblyProductAttribute), false)).Product;
            _appName = _callingAssembly.GetName().Name;

            ServiceContainer = new ServiceContainer();
            _navigationManager = new NavigationManager(ServiceContainer);

            ServiceContainer.Register<IServiceContainer, ServiceContainer>().AsSingletone(ServiceContainer);
            ServiceContainer.Register<INavigationManager, NavigationManager>().AsSingletone(_navigationManager);
            ServiceContainer.Register<IMessenger, Messenger>().AsSingletone(new Messenger());
            ServiceContainer.Register<ILogger, Logger>();

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

        public void UseLocalDeploy(DeployInfo deployInfo)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VisualStudioEdition")))
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;

                while (_appName != Path.GetFileName(dir.TrimEnd('\\').TrimEnd('/')))
                    dir = Path.GetDirectoryName(dir.TrimEnd('\\').TrimEnd('/'));

                dir = Path.GetDirectoryName(dir.TrimEnd('\\').TrimEnd('/'));

                if (string.IsNullOrWhiteSpace(dir))
                    return;

                var assembly = Assembly.GetExecutingAssembly();
                var installerFileName = "Ereoz.InstallerCreator.exe";
                var resourceInstallerName = assembly.GetManifestResourceNames().Where(it => it.EndsWith(installerFileName)).FirstOrDefault();

                using (Stream stream = assembly.GetManifestResourceStream(resourceInstallerName))
                {
                    if (stream != null)
                    {
                        try
                        {
                            using (FileStream fileStream = new FileStream(Path.Combine(dir, installerFileName), FileMode.Create))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                        catch { }
                    }
                }

                if (!File.Exists(Path.Combine(dir, "InstallerInfo.txt")))
                {
                    var installerInfo = new StringBuilder();

                    if (!string.IsNullOrWhiteSpace(deployInfo.InstallerName))
                        installerInfo.AppendLine($"InstallerName={deployInfo.InstallerName}");

                    installerInfo.AppendLine($"AppName={_appName}");
                    installerInfo.AppendLine($"InstallDirectoryBase={deployInfo.InstallDirectoryBase}");
                    installerInfo.AppendLine($"ReleasesDirectoryBase={deployInfo.ReleasesDirectoryBase}");
                    installerInfo.AppendLine($"DisableChangeBaseDirectory={deployInfo.DisableChangeBaseDirectory}");

                    var targetArchitecture = _callingAssembly
                        .GetName()
                        .ProcessorArchitecture
                        .ToString()
                        .Replace("X", "")
                        .Replace("x", "")
                        .Replace("Amd", "")
                        .Replace("MSIL", "");

                    if (!string.IsNullOrWhiteSpace(targetArchitecture))
                        installerInfo.AppendLine($"RID=win-x{targetArchitecture}");

                    File.WriteAllText(Path.Combine(dir, "InstallerInfo.txt"), installerInfo.ToString());
                }
            }
            else
            {
                ServiceContainer.Resolve<Updater>().CheckAndUpdate(deployInfo.ReleasesDirectoryBase, DataFolder, _callingAssembly);
            }
        }

        public void ShowStartWindow<TWindow, TViewModel>()
            where TWindow : Window
            where TViewModel : ViewModelBase
        {
            _startWindow = ServiceContainer.Resolve<TWindow>();
            _startWindow.DataContext = ServiceContainer.Resolve<TViewModel>();
            _startWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _startWindow.Topmost = true;
            _startWindow.Show();
        }

        public T CreateMainWindow<T>(SettingsBase appSettings = null) where T : Window
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

            var window = _navigationManager.CreateMainWindow<T>(appSettings, DataFolder);
            window.Title = $"{ProductName} - {Version.Major}.{Version.Minor}.{Version.Revision}";

            if (_startWindow != null)
            {
                window.ContentRendered += (s, e) =>
                {
                    _startWindow.Close();
                    _startWindow = null;
                    Application.Current.MainWindow = window;
                };
            }

            return window;
        }
    }
}
