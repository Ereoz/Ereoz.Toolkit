namespace Ereoz.Toolkit
{
    public sealed class DeployInfo
    {
        public string ReleasesDirectoryBase { get; }
        public string InstallDirectoryBase { get; }
        public bool DisableChangeBaseDirectory { get; }
        public string InstallerName { get; }

        public DeployInfo(string releasesDirectoryBase, string installDirectoryBase, bool disableChangeBaseDirectory = false, string installerName = null)
        {
            ReleasesDirectoryBase = releasesDirectoryBase;
            InstallDirectoryBase = installDirectoryBase;
            DisableChangeBaseDirectory = disableChangeBaseDirectory;
            InstallerName = installerName;
        }
    }
}
