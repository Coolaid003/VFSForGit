using GVFS.Common.FileSystem;
using GVFS.Common.Git;
using GVFS.Common.NuGetUpgrader;
using GVFS.Common.Tracing;
using System;
using System.IO;

namespace GVFS.Common
{
    public class ProductUpgraderFactory
    {
        public static bool TryCreateUpgrader(
            out IProductUpgrader newUpgrader,
            ITracer tracer,
            out string error)
        {
            newUpgrader = NuGetUpgrader.NuGetUpgrader.Create(tracer, out error);
            if (newUpgrader != null)
            {
               return true;
            }

            newUpgrader = GitHubUpgrader.Create(tracer, out error);
            if (newUpgrader == null)
            {
                tracer.RelatedError($"{nameof(TryCreateUpgrader)}: Could not create upgrader. {error}");
                return false;
            }

            return true;
        }
    }
}
