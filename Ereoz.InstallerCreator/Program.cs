using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Ereoz.InstallerCreator
{
    internal class Program
    {
        static string appName = string.Empty;
        static string installDirectoryBase = @"C:\EreozApps";
        static string releasesDirectoryBase = @"C:\EreozReleases";
        static bool disableChangeBaseDirectory = false;
        static string installerName;
        static Version version;
        static string rid;

        static bool _wait = false;

        static async Task Main(string[] args)
        {
            string[] inslallInfo = null;

            if (File.Exists("InstallerInfo.txt"))
            {
                inslallInfo = File.ReadAllText("InstallerInfo.txt").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                appName = inslallInfo.FirstOrDefault(it => it.StartsWith("AppName="))?.Substring("AppName=".Length);
                installerName = inslallInfo.FirstOrDefault(it => it.StartsWith("InstallerName="))?.Substring("InstallerName=".Length);
                rid = inslallInfo.FirstOrDefault(it => it.StartsWith("RID="))?.Substring("RID=".Length);

                if (inslallInfo.FirstOrDefault(it => it.StartsWith("InstallDirectoryBase=")) is string installDir)
                    installDirectoryBase = installDir.Substring("InstallDirectoryBase=".Length);

                if (inslallInfo.FirstOrDefault(it => it.StartsWith("ReleasesDirectoryBase=")) is string releasesDir)
                    releasesDirectoryBase = releasesDir.Substring("ReleasesDirectoryBase=".Length);

                if (inslallInfo.FirstOrDefault(it => it.StartsWith("DisableChangeBaseDirectory=")) is string disableChangDir)
                {
                    if (bool.TryParse(disableChangDir.Substring("DisableChangeBaseDirectory=".Length).ToLower(), out bool result))
                        disableChangeBaseDirectory = result;
                }

                var versions = inslallInfo.Where(it => Version.TryParse(it.Trim(), out Version ver));

                if (versions.Count() > 0)
                {
                    Console.WriteLine("Version history:");

                    foreach (var ver in versions)
                        Console.WriteLine(ver);

                    Console.WriteLine();
                }

                Console.Write("Enter version: ");
                version = new Version(Console.ReadLine().Trim());
            }
            else
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Where(arg => arg.ToUpper().StartsWith("InstallerName=")).FirstOrDefault() is string iname)
                        installerName = iname.Substring("InstallerName=".Length);

                    if (args.Where(arg => arg.ToUpper().StartsWith("Version=")).FirstOrDefault() is string ver)
                        version = new Version(ver.Substring("Version=".Length));

                    if (args.Where(arg => arg.ToUpper().StartsWith("Version=")).FirstOrDefault() is string rid_)
                        rid = rid_.Substring("RID=".Length);

                    if (args.Where(it => it.Trim().StartsWith("AppName=")).FirstOrDefault() is string an)
                        appName = an.Substring("AppName=".Length);
                    else
                        throw new Exception("AppName is empty.");

                    if (args.Where(it => it.Trim().StartsWith("InstallDirectoryBase=")).FirstOrDefault() is string idb)
                        installDirectoryBase = idb.Substring("InstallDirectoryBase=".Length);

                    if (args.Where(it => it.Trim().StartsWith("ReleasesDirectoryBase=")).FirstOrDefault() is string rdb)
                        releasesDirectoryBase = rdb.Substring("ReleasesDirectoryBase=".Length);

                    if (args.Where(it => it.Trim().StartsWith("DisableChangeBaseDirectory=")).FirstOrDefault() is string dcbd)
                    {
                        if (bool.TryParse(dcbd.Substring("DisableChangeBaseDirectory=".Length).ToLower(), out bool result))
                            disableChangeBaseDirectory = result;
                    }
                }
                else
                    throw new Exception("AppName is empty.");
            }

            Console.WriteLine($"Creating installer for: {appName}{Environment.NewLine}");

            Console.Write("Extract base installer... ");
            ExtractEreozInstallerBaseFromResources();
            Console.WriteLine("Done");

            Console.Write("Build App... ");
            await BuildApp();
            Console.WriteLine("Done");

            Console.Write("Add release files to base installer... ");
            AddCurrentAppToInstaller();
            Console.WriteLine("Done");

            Console.Write("Build app installer... ");
            BuildInstaller();
            Console.WriteLine("Done");

            if (inslallInfo != null && version != null)
            {
                string addedInfo = version.ToString() + "\n";

                if (inslallInfo.FirstOrDefault(it => it.Trim() == "Versions:") == null)
                    addedInfo = "\nVersions:\n" + addedInfo;

                File.AppendAllText("InstallerInfo.txt", addedInfo);
            }
        }

        static void ExtractEreozInstallerBaseFromResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceInstallerName = assembly.GetManifestResourceNames().Where(it => it.EndsWith("EreozInstaller.zip")).FirstOrDefault();

            var zipFileName = "EreozInstaller.zip";

            using (Stream stream = assembly.GetManifestResourceStream(resourceInstallerName))
            {
                if (stream != null)
                {
                    using (FileStream fileStream = new FileStream(zipFileName, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }

            var projDir = "EreozInstaller";

            if (Directory.Exists(projDir))
                Directory.Delete(projDir, true);

            Directory.CreateDirectory(projDir);

            using (ZipArchive archive = ZipFile.OpenRead(zipFileName))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    var filePath = Path.Combine(projDir, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    entry.ExtractToFile(filePath, true);
                }
            }

            File.Delete(zipFileName);
        }

        static async Task BuildApp()
        {
            Console.OutputEncoding = Encoding.UTF8;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{Path.Combine(appName, $"{appName}.csproj")}\" --configuration Release --output publish --verbosity normal" + (string.IsNullOrEmpty(rid) ? "" : $" --runtime {rid}") + (version == null ? "" : $" /p:Version={version.ToString()}"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Console.WriteLine(e.Data);

                        if (e.Data.Contains("Прошло времени"))
                            _wait = false;
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        Console.WriteLine(e.Data);

                    _wait = false;
                };
                process.Exited += (sender, e) =>
                {
                    _wait = false;
                };

                try
                {
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    _wait = true;
                }
                catch
                {
                    _wait = false;
                }

                while (_wait)
                {
                    await Task.Run(() => Task.Delay(1));
                }
            }
        }

        static void AddCurrentAppToInstaller()
        {
            var projDir = "EreozInstaller";

            Directory.CreateDirectory(Path.Combine(projDir, "Resources"));
            var releaseFilesDir = "publish";
            var appVersion = FileVersionInfo.GetVersionInfo($"{releaseFilesDir}\\{appName}.exe").FileVersion;

            var AppXamlCs = File.ReadAllText(Path.Combine(projDir, "App.xaml.cs"));

            if (!string.IsNullOrWhiteSpace(installerName))
                AppXamlCs = AppXamlCs.Replace("InstallerName = \"EREOZ\"", $"InstallerName = \"{installerName}\"");

            AppXamlCs = AppXamlCs.Replace("InstallDir = string.Empty,", $"InstallDir = @\"{installDirectoryBase}\",");
            AppXamlCs = AppXamlCs.Replace("InstallAppName = string.Empty,", $"InstallAppName = @\"{appName}\",");
            AppXamlCs = AppXamlCs.Replace("InstallAppVersion = string.Empty,", $"InstallAppVersion = @\"{appVersion}\",");
            AppXamlCs = AppXamlCs.Replace("public static bool DisableChangeBaseDirectory = false;", $"public static bool DisableChangeBaseDirectory = {disableChangeBaseDirectory.ToString().ToLower()};");

            var addedFiles = string.Empty;

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(projDir, "Ereoz.InstallerBase.csproj"));
            XmlElement root = doc.DocumentElement;

            double requiredSpace = 0;

            XmlNode propertyGroup = doc.SelectSingleNode("//*[local-name()='PropertyGroup']");
            XmlElement versionElement = doc.CreateElement("Version");
            versionElement.InnerText = appVersion;
            propertyGroup.AppendChild(versionElement);

            var releaseFiles = Directory.GetFiles(releaseFilesDir, "*", SearchOption.AllDirectories)
                                        .Select(it => it.Substring(it.IndexOf("publish") + 8));

            var filePack = "release.pack";

            foreach (string file in releaseFiles)
            {
                requiredSpace += (new FileInfo(Path.Combine(releaseFilesDir, file))).Length;
                addedFiles += $"\n\t\t\tinstallParams.FileNames.Add(@\"{file}\");";
            }

            FilePacking.Pack(releaseFilesDir, releaseFiles, filePack);

            File.Copy(filePack, Path.Combine(projDir, "Resources", filePack));

            XmlElement itemGroupElement = doc.CreateElement("ItemGroup");

            XmlElement embeddedResourceElement = doc.CreateElement("EmbeddedResource");
            embeddedResourceElement.SetAttribute("Include", Path.Combine("Resources", filePack));

            XmlElement copyToOutputDirectoryElement = doc.CreateElement("CopyToOutputDirectory");
            copyToOutputDirectoryElement.InnerText = "Never";

            embeddedResourceElement.AppendChild(copyToOutputDirectoryElement);

            itemGroupElement.AppendChild(embeddedResourceElement);

            root.AppendChild(itemGroupElement);

            doc.Save(Path.Combine(projDir, "Ereoz.InstallerBase.csproj"));

            AppXamlCs = AppXamlCs.Replace("RequiredSpace = 0.0,", $"RequiredSpace = {Math.Round(requiredSpace / 1024 / 1024, 2, MidpointRounding.AwayFromZero).ToString().Replace(',', '.')},");
            AppXamlCs = AppXamlCs.Replace("//placeholder_fileNames.Add", addedFiles);

            File.WriteAllText(Path.Combine(projDir, "App.xaml.cs"), AppXamlCs);
        }

        static void BuildInstaller()
        {
            var projDir = "EreozInstaller";
            var appVersion = FileVersionInfo.GetVersionInfo($"publish\\{appName}.exe").FileVersion;
            var appFullName = $"{appName}-{appVersion}-setup";

            Directory.CreateDirectory(Path.Combine(releasesDirectoryBase, appName));

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{Path.Combine(projDir, "Ereoz.InstallerBase.csproj")}\" --configuration Release /p:AssemblyName={appFullName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }

            var exeFile = Path.Combine(projDir, "bin", "Release", "net40", appFullName + ".exe");
            var zipFile = Path.Combine(projDir, "bin", "Release", "net40", appFullName + ".zip");

            using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(exeFile, Path.GetFileName(exeFile));
            }

            File.Copy(zipFile, $"{Path.Combine(releasesDirectoryBase, appName)}\\{appFullName + ".zip"}", true);

            Directory.Delete(projDir, true);
            Directory.Delete("publish", true);
            File.Delete("release.pack");
        }
    }
}
