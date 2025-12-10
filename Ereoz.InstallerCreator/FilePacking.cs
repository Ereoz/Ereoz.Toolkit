using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ereoz.InstallerCreator
{
    public static class FilePacking
    {
        public static void Pack(string baseDir, IEnumerable<string> files, string targetFile)
        {
            using (var output = File.Create(targetFile))
            {
                foreach (var file in files)
                {
                    var fullPath = Path.Combine(baseDir, file);
                    var pathBytes = Encoding.UTF8.GetBytes(file);
                    var fileSize = (int)new FileInfo(fullPath).Length;

                    output.Write(BitConverter.GetBytes(pathBytes.Length), 0, 4);
                    output.Write(pathBytes, 0, pathBytes.Length);
                    output.Write(BitConverter.GetBytes(fileSize), 0, 4);

                    using (var input = File.OpenRead(fullPath))
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        public static void Pack(string sourceDir, string targetFile)
        {
            using (var output = File.Create(targetFile))
            {
                foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
                    var pathBytes = Encoding.UTF8.GetBytes(relativePath);
                    var fileSize = (int)new FileInfo(file).Length;

                    output.Write(BitConverter.GetBytes(pathBytes.Length), 0, 4);
                    output.Write(pathBytes, 0, pathBytes.Length);
                    output.Write(BitConverter.GetBytes(fileSize), 0, 4);

                    using (var input = File.OpenRead(file))
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        public static void Unpack(string sourceFile, string targetDir)
        {
            using (var fs = File.OpenRead(sourceFile))
            {
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    while (fs.Position < fs.Length)
                    {
                        var pathSize = reader.ReadInt32();
                        var relativePath = Encoding.UTF8.GetString(reader.ReadBytes(pathSize));
                        var fileSize = reader.ReadInt32();
                        var fullPath = Path.Combine(targetDir, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        File.WriteAllBytes(fullPath, reader.ReadBytes(fileSize));
                    }
                }
            }
        }
    }
}
