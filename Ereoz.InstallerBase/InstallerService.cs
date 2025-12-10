using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;

namespace Ereoz.InstallerBase
{
    public static class InstallerService
    {
        public static event Action<string> OnCopiedFileStart;
        public static event Action OnCopiedFileDone;
        public static event Action OnAllDone;
        public static event Action<string> OnError;

        public static InstallMode GetInstallMode(string installDir, string installAppName, string installAppVersion)
        {
            string existFile = Path.Combine(installDir, "bin", $"{installAppName}.exe");

            if (File.Exists(existFile))
            {
                string existVersion = FileVersionInfo.GetVersionInfo(existFile).FileVersion;

                if (VersionCompare(existVersion, installAppVersion) > 0)
                {
                    return InstallMode.Update;
                }
                else if (VersionCompare(existVersion, installAppVersion) == 0)
                {
                    return InstallMode.Repair;
                }
                else
                {
                    return InstallMode.None;
                }
            }
            else
            {
                return InstallMode.Install;
            }
        }

        public static void InstallerWorker(InstallParams installParams)
        {
            Thread thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    ExtractResourcesAndCopyFiles(installParams.InstallDir, installParams.FileNames);

                    if (installParams.IsShortcutDesktop)
                        CreateShortcutDesktop(installParams);

                    if (installParams.IsShortcutStartMenu)
                        CreateShortcutStartMenu(installParams);

                    OnCopiedFileStart?.Invoke("Выполнено.");
                    OnAllDone?.Invoke();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Установка не выполнена: {ex.Message}");
                }
            }));

            thread.IsBackground = true;
            thread.Start();
        }

        private static void ExtractResourcesAndCopyFiles(string installDir, List<string> fileNames)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string rootNamespace = assembly.GetName().Name;

            var installerTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), rootNamespace);

            if (Directory.Exists(installerTemp))
                Directory.Delete(installerTemp, true);

            Directory.CreateDirectory(installerTemp);

            var resourceName = assembly.GetManifestResourceNames().Where(name => name.Contains(".Resources.") && name.Contains("release.pack")).Single();

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                using (FileStream fileStream = File.Create(Path.Combine(installerTemp, "release.pack")))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            FilePacking.Unpack(Path.Combine(installerTemp, "release.pack"), Path.Combine(installDir, "bin"), OnCopiedFileStart, OnCopiedFileDone);

            Directory.Delete(installerTemp, true);
        }

        private static void CreateShortcutDesktop(InstallParams installParams)
        {
            OnCopiedFileStart?.Invoke("Создание ярлыка на рабочем столе...");

            IShellLink link = (IShellLink)new ShellLink();

            //link.SetDescription(installAppName);
            link.SetPath(Path.Combine(installParams.InstallDir, "bin", $"{installParams.InstallAppName}.exe"));
            link.SetWorkingDirectory(Path.Combine(installParams.InstallDir, "bin"));

            IPersistFile file = (IPersistFile)link;
            string desktopPath = Environment.GetFolderPath(installParams.IsForAllUser ? Environment.SpecialFolder.CommonDesktopDirectory : Environment.SpecialFolder.DesktopDirectory);
            file.Save(Path.Combine(desktopPath, $"{installParams.InstallAppName}.lnk"), false);

            OnCopiedFileDone?.Invoke();
        }

        private static void CreateShortcutStartMenu(InstallParams installParams)
        {
            OnCopiedFileStart?.Invoke("Создание группы в меню 'Пуск'...");

            IShellLink link = (IShellLink)new ShellLink();

            //link.SetDescription(installAppName);
            link.SetPath(Path.Combine(installParams.InstallDir, "bin", $"{installParams.InstallAppName}.exe"));
            link.SetWorkingDirectory(Path.Combine(installParams.InstallDir, "bin"));

            IPersistFile file = (IPersistFile)link;
            string startmenuPath = Environment.GetFolderPath(installParams.IsForAllUser ? Environment.SpecialFolder.CommonStartMenu : Environment.SpecialFolder.StartMenu);
            Directory.CreateDirectory(Path.Combine(startmenuPath, installParams.InstallAppName));
            file.Save(Path.Combine(startmenuPath, installParams.InstallAppName, $"{installParams.InstallAppName}.lnk"), false);

            OnCopiedFileDone?.Invoke();
        }

        private static void AddUninstallerToReg(InstallParams installParams)
        {
            OnCopiedFileStart?.Invoke("Добавление информации в систему...");

            string uninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            string applicationKeyPath = uninstallKeyPath + "\\" + installParams.InstallAppName;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(applicationKeyPath))
                {
                    key.SetValue("DisplayName", installParams.InstallAppName);
                    key.SetValue("DisplayVersion", installParams.InstallAppVersion);
                    key.SetValue("DisplayIcon", Path.Combine(installParams.InstallDir, "bin", $"{installParams.InstallAppName}.exe"));
                    key.SetValue("UninstallString", Path.Combine(installParams.InstallDir, Path.Combine(installParams.InstallDir, $"uninstall.exe")));
                }
            }
            catch { }

            OnCopiedFileDone?.Invoke();
        }

        private static int VersionCompare(string currentVersion, string newVersion)
        {
            var curV = currentVersion.Split('.');
            var newV = newVersion.Split('.');

            if (int.Parse(newV[0]) > int.Parse(curV[0]))
            {
                return +1;
            }
            else if (int.Parse(newV[0]) < int.Parse(curV[0]))
            {
                return -1;
            }
            else
            {
                if (int.Parse(newV[1]) > int.Parse(curV[1]))
                {
                    return +1;
                }
                else if (int.Parse(newV[1]) < int.Parse(curV[1]))
                {
                    return -1;
                }
                else
                {
                    if (int.Parse(newV[3]) > int.Parse(curV[3]))
                    {
                        return +1;
                    }
                    else if (int.Parse(newV[3]) < int.Parse(curV[3]))
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }
    }
}
