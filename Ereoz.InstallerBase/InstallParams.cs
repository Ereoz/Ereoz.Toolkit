using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Ereoz.InstallerBase
{
    public class InstallParams : NotifyPropertyChanged
    {
        private string installDir;
        public string InstallDir { get => Path.Combine(installDir, InstallAppName ?? string.Empty); set { installDir = value; OnPropertyChanged(); OnInstallDirChanged?.Invoke(); } }

        private bool isShortcutDesktop;
        public bool IsShortcutDesktop { get => isShortcutDesktop; set { isShortcutDesktop = value; OnPropertyChanged(); } }

        private bool isShortcutStartMenu;
        public bool IsShortcutStartMenu { get => isShortcutStartMenu; set { isShortcutStartMenu = value; OnPropertyChanged(); } }

        private bool isForAllUser;
        public bool IsForAllUser
        {
            get => isForAllUser;
            set
            {
                if (!value)
                {
                    isForAllUser = value;
                    OnPropertyChanged();
                    return;
                }

                if (!isForAllUser && InstallerRunIsAdmin)
                {
                    isForAllUser = value;
                }
                else
                {
                    isForAllUser = value;
                    string executablePath = Assembly.GetExecutingAssembly().Location;
                    ProcessStartInfo startInfo = new ProcessStartInfo(executablePath);
                    startInfo.Arguments = $"INSTALL_BASE_DIR={installDir} SHORTCUT_DESKTOP={isShortcutDesktop} SHORTCUT_STARTMENU={isShortcutStartMenu} ALL_USERS={isForAllUser} UI_MODE=conf";
                    startInfo.Verb = "runas";

                    try
                    {
                        Process.Start(startInfo);
                        Environment.Exit(0);
                    }
                    catch
                    {
                        isForAllUser = false;
                    }
                }

                OnPropertyChanged();
            }
        }

        private bool isStartAppAfterFinishWork;
        public bool IsStartAppAfterFinishWork { get => isStartAppAfterFinishWork; set { isStartAppAfterFinishWork = value; OnPropertyChanged(); } }

        public string InstallAppName { get; set; }
        public string InstallAppVersion { get; set; }
        public string InstallAppFullName => $"{InstallAppName} {InstallAppVersion}";
        public List<string> FileNames { get; set; }
        public double RequiredSpace { get; set; }

        public event Action OnInstallDirChanged;

        public InstallMode InstallMode { get; set; }
        public UIMode UIMode { get; set; }
        public bool InstallerRunIsAdmin { get; set; }
    }
}
