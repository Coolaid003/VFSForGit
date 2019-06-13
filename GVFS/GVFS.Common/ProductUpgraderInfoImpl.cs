using GVFS.Common.FileSystem;
using GVFS.Common.Tracing;
using System;
using System.IO;

namespace GVFS.Common
{
    public abstract class ProductUpgraderInfoImpl
    {
        public abstract string GetUpgradeNonProtectedDirectory();

        public abstract string GetUpgradeProtectedDirectory();

        public string GetUpgradeLogDirectory()
        {
            return Path.Combine(this.GetUpgradeNonProtectedDirectory(), ProductUpgraderInfo.LogDirectory);
        }

        public string GetAssetDownloadsPath()
        {
            return Path.Combine(this.GetUpgradeProtectedDirectory(), ProductUpgraderInfo.DownloadDirectory);
        }

        public string GetUpgradeApplicationDirectory()
        {
            return Path.Combine(this.GetUpgradeProtectedDirectory(), "Tools");
        }
    }
}
