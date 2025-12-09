using Ereoz.Abstractions.Logging;
using Ereoz.Abstractions.Messaging;
using Ereoz.Packing;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace Ereoz.Toolkit
{
    public class Updater
    {
        private ILogger _logger;
        private IMessenger _messenger;
        private bool _updateIsDownloaded = false;

        public Updater(ILogger logger, IMessenger messenger)
        {
            _logger = logger;
            _messenger = messenger;
        }

        public void CheckAndUpdate(string releasesDirectory, string dataDirectory, Assembly assembly)
        {
            _messenger.Send(new UpdateInfo("Проверяем обновления..."));

            var updateDirectory = Path.Combine(dataDirectory, "update");

            if (Directory.Exists(updateDirectory))
                Directory.Delete(updateDirectory, true);

            try
            {
                var targetPlatform = ((TargetFrameworkAttribute)assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                                                        .FirstOrDefault())
                                                        .FrameworkDisplayName
                                                        .Replace(" ", "")
                                                        .Replace(".", "")
                                                        .Replace("Framework", "")
                                                        .Replace("NET", "")
                                                        .PadRight(2, '0');
                var targetArchitecture = assembly.GetName()
                                                 .ProcessorArchitecture
                                                 .ToString()
                                                 .Replace("X", "")
                                                 .Replace("x", "")
                                                 .Replace("Amd", "")
                                                 .Replace("MSIL", "00");

                var allReleases = Directory.GetFiles(Path.Combine(releasesDirectory, assembly.GetName().Name)).Where(it => it.Contains($".{targetPlatform}{targetArchitecture}.")).ToArray();

                var countReleases = allReleases.Length;

                if (countReleases < 1)
                {
                    _messenger.Send(new UpdateInfo("Обновлений не найдено."));
                    return;
                }
                else if (countReleases == 1)
                {
                    var newVer = allReleases[0].Split('-');

                    if (VersionCompare(assembly.GetName().Version.ToString(), newVer[newVer.Length - 2]) > 0)
                        DownloadRelease(allReleases[0], updateDirectory);
                }
                else
                {
                    string lastVersionFile = allReleases[0];

                    for (int i = 1; i < countReleases; i++)
                    {
                        var newVer1 = lastVersionFile.Split('-');
                        var newVer2 = allReleases[i].Split('-');

                        if (VersionCompare(newVer1[newVer1.Length - 2], newVer2[newVer2.Length - 2]) > 0)
                            lastVersionFile = allReleases[i];
                    }

                    var newVer = lastVersionFile.Split('-');

                    if (VersionCompare(assembly.GetName().Version.ToString(), newVer[newVer.Length - 2]) > 0)
                        DownloadRelease(lastVersionFile, updateDirectory);
                }
            }
            catch (Exception ex)
            {
                if (Directory.Exists(updateDirectory))
                    Directory.Delete(updateDirectory, true);

                _logger.Error(ex, "Ошибка поиска обновлений");
                _messenger.Send(new UpdateInfo("Ошибка поиска обновлений."));
            }

            if (_updateIsDownloaded)
            {
                _messenger.Send(new UpdateInfo("Новая версия загружена, начинаем процесс обновления..."));

                if (Directory.GetFiles(updateDirectory).Where(fileName => fileName.EndsWith("-setup.exe")).FirstOrDefault() is string newVersionFileName)
                {
                    try
                    {
                        var versionCompare = VersionCompare(Assembly.GetEntryAssembly().GetName().Version.ToString(), FileVersionInfo.GetVersionInfo(newVersionFileName).FileVersion);

                        if (versionCompare > 0)
                        {
                            var baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                            baseDir = baseDir.Substring(0, baseDir.LastIndexOf('\\'));
                            baseDir = baseDir.Substring(0, baseDir.LastIndexOf('\\'));

                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = newVersionFileName;
                            startInfo.Arguments = $"INSTALL_BASE_DIR={baseDir} SHORTCUT_DESKTOP=false SHORTCUT_STARTMENU=false ALL_USERS=false UI_MODE=progress START_APP=true";

                            Process.Start(startInfo);

                            Environment.Exit(0);
                        }
                        else
                        {
                            _logger.Error("Обновление отменено - установлена актуальная версия");
                            Directory.Delete(updateDirectory, true);
                            _messenger.Send(new UpdateInfo("Обновление отменено - установлена актуальная версия."));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Ошибка обновления");
                        Directory.Delete(updateDirectory, true);
                        _messenger.Send(new UpdateInfo("Ошибка обновления."));
                    }
                }
                else
                {
                    _logger.Error("Ошибка обновления");
                    Directory.Delete(updateDirectory, true);
                    _messenger.Send(new UpdateInfo("Ошибка обновления."));
                }
            }
        }

        private void DownloadRelease(string fileName, string updateDir)
        {
            _messenger.Send(new UpdateInfo("Загружаем обновления..."));

            Directory.CreateDirectory(updateDir);

            var zipFile = Path.Combine(updateDir, Path.GetFileName(fileName));

            File.Copy(fileName, zipFile, true);

            new SimpleZip().Unpack(zipFile, updateDir);

            File.Delete(zipFile);

            _updateIsDownloaded = true;
        }

        private int VersionCompare(string currentVersion, string newVersion)
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
    }
}
