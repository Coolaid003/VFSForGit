using GVFS.Common;
using GVFS.Common.FileSystem;
using GVFS.Common.Tracing;
using System;
using System.IO;

namespace GVFS.Platform.Mac
{
    public class MacProductUpgraderInfo : ProductUpgraderInfoImpl
    {
        private string protectedDataDirectory = "/usr/local/vfsforgit_upgrader";

        public override string GetUpgradeNonProtectedDirectory()
        {
            return GVFSPlatform.Instance.GetDataRootForGVFSComponent(ProductUpgraderInfo.UpgradeDirectoryName);
        }

        public override string GetUpgradeProtectedDirectory()
        {
            return this.protectedDataDirectory;
        }
    }
}
