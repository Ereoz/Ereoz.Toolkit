using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace Ereoz.InstallerBase
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        InstallParams installParams = new InstallParams
        {
            InstallDir = string.Empty,
            InstallAppName = string.Empty,
            InstallAppVersion = string.Empty,
            RequiredSpace = 0.0,
            IsShortcutDesktop = true,
            IsShortcutStartMenu = true,
            IsForAllUser = false,
            IsStartAppAfterFinishWork = true,
            FileNames = new List<string>(),
            UIMode = UIMode.FullUI,
        };

        public static bool IsDirectExit = false;
        public static bool DisableChangeBaseDirectory = false;

        public static string InstallerName = "EREOZ";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //placeholder_fileNames.Add

            installParams.InstallerRunIsAdmin = IsRunAsAdmin();

            try
            {
                if (e.Args != null && e.Args.Length > 0)
                {
                    if (e.Args.Where(arg => arg.ToUpper().StartsWith("INSTALL_BASE_DIR=")).FirstOrDefault() is string dir)
                        installParams.InstallDir = dir.Replace("INSTALL_BASE_DIR=", "").Replace("install_base_dir=", "");

                    if (e.Args.Where(arg => arg.ToUpper().StartsWith("SHORTCUT_DESKTOP=")).FirstOrDefault() is string desktop)
                        installParams.IsShortcutDesktop = bool.Parse(desktop.Replace("SHORTCUT_DESKTOP=", "").Replace("shortcut_desktop=", ""));

                    if (e.Args.Where(arg => arg.ToUpper().StartsWith("SHORTCUT_STARTMENU=")).FirstOrDefault() is string startmenu)
                        installParams.IsShortcutStartMenu = bool.Parse(startmenu.Replace("SHORTCUT_STARTMENU=", "").Replace("shortcut_startmenu=", ""));

                    if (e.Args.Where(arg => arg.ToUpper().StartsWith("ALL_USERS=")).FirstOrDefault() is string allusers)
                        installParams.IsForAllUser = bool.Parse(allusers.Replace("ALL_USERS=", "").Replace("all_users=", ""));

                    if (e.Args.Where(arg => arg.ToUpper().StartsWith("UI_MODE=")).FirstOrDefault() is string uimodeparam)
                    {
                        string uiMode = uimodeparam.Replace("UI_MODE=", "").Replace("ui_mode=", "").Trim().ToLower();

                        if (uiMode == "full")
                            installParams.UIMode = UIMode.FullUI;
                        else if (uiMode == "conf")
                            installParams.UIMode = UIMode.StartWithConfigure;
                        else if (uiMode == "progress")
                            installParams.UIMode = UIMode.ProgressOnly;
                        else if (uiMode == "silent")
                        {
                            installParams.UIMode = UIMode.Silent;
                            installParams.IsStartAppAfterFinishWork = false;
                        }
                    }

                    if (e.Args.Where(arg => arg.ToUpper().StartsWith("START_APP=")).FirstOrDefault() is string startnow)
                        installParams.IsStartAppAfterFinishWork = bool.Parse(startnow.Replace("START_APP=", "").Replace("start_app=", ""));
                }

                installParams.InstallMode = InstallerService.GetInstallMode(installParams.InstallDir, installParams.InstallAppName, installParams.InstallAppVersion);

                if (installParams.UIMode == UIMode.Silent)
                {
                    if (installParams.InstallMode == InstallMode.None)
                    {
                        App.IsDirectExit = true;
                        Environment.Exit(0);
                    }

                    InstallerService.OnAllDone += () =>
                    {
                        if (installParams.IsStartAppAfterFinishWork)
                        {
                            string appPath = string.Empty;

                            try
                            {
                                appPath = Path.Combine(installParams.InstallDir, "bin", $"{installParams.InstallAppName}.exe");
                                Process.Start(appPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"{ex.Message}\n{appPath}");
                            }
                        }

                        AttachConsole(-1);
                        Console.Write("Выполнено.");
                        App.IsDirectExit = true;
                        Environment.Exit(0);
                    };

                    InstallerService.OnError += (message) =>
                    {
                        AttachConsole(-1);
                        Console.Write(message);
                        App.IsDirectExit = true;
                        Environment.Exit(0);
                    };

                    InstallerService.InstallerWorker(installParams);
                }
                else
                {
                    ShowUI();
                }
            }
            catch (Exception ex)
            {
                var builder = new StringBuilder();

                builder.Append($"\n\n");
                builder.Append($"Ошибка: {ex.Message}.");
                builder.Append($"\n\n");
                builder.Append($"Справочная информация. Возможные параметры запуска (регистр имени параметра допускается любой):");
                builder.Append($"\n\n");
                builder.Append($"   INSTALL_BASE_DIR=<YourPath> - задать базовую директорию установки.\n");
                builder.Append($"                                 Например, INSTALL_BASE_DIR=С:\\EreozApps\n");
                builder.Append($"                                 (к базовой директории автоматически добавится директория с именем приложения);");
                builder.Append($"\n\n");
                builder.Append($"   SHORTCUT_DESKTOP=true       - создать ярлык на рабочем столе;\n");
                builder.Append($"   SHORTCUT_DESKTOP=false      - не создавать ярлык на рабочем столе;");
                builder.Append($"\n\n");
                builder.Append($"   SHORTCUT_STARTMENU=true     - создать группу в меню 'Пуск';\n");
                builder.Append($"   SHORTCUT_STARTMENU=false    - не создавать группу в меню 'Пуск';");
                builder.Append($"\n\n");
                builder.Append($"   UI_MODE=full                - обычный запуск пользовательского интерфейса;");
                builder.Append($"   UI_MODE=conf                - в пользовательском интерфейсе перейти сразу к диалогу конфигурации;\n");
                builder.Append($"   UI_MODE=progress            - в пользовательском интерфейсе показать только окно прогресса;\n");
                builder.Append($"   UI_MODE=silent              - запустить установщик в тихом режиме (без UI интерфейса);\n");
                builder.Append($"\n\n");
                builder.Append($"   START_APP=true              - запустить приложение после завершения работы инсталлера;\n");
                builder.Append($"   START_APP=false             - не запускать приложение после завершения работы инсталлера;");
                builder.Append($"\n\n");
                builder.Append($"Для выхода нажмите любую клавишу...");

                AttachConsole(-1);
                Console.Write(builder.ToString());
                App.IsDirectExit = true;
                Environment.Exit(0);
            }
        }

        private void ShowUI()
        {
            if (installParams.InstallMode == InstallMode.None)
            {
                MessageBox.Show("У Вас уже установлена более новая версия.", "Отмена установки", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                App.IsDirectExit = true;
                Environment.Exit(0);
            }

            var window = new MainWindow();
            var dataContext = new InstallerVM(installParams, window);
            window.SetVM(dataContext);
            window.Show();
        }

        /// <summary>
        /// The function checks whether the current process is run as administrator.
        /// In other words, it dictates whether the primary access token of the 
        /// process belongs to user account that is a member of the local 
        /// Administrators group and it is elevated.
        /// </summary>
        /// <returns>
        /// Returns true if the primary access token of the process belongs to user 
        /// account that is a member of the local Administrators group and it is 
        /// elevated. Returns false if the token does not.
        /// https://www.cyberforum.ru/csharp-beginners/thread1218911.html
        /// </returns>
        internal static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
    }
}
