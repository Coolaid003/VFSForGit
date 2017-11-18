﻿using RGFS.Common.FileSystem;

namespace RGFS.Tests.Should
{
    public static class StringShouldExtensions
    {
        public static void ShouldBeAPhysicalFile(this string physicalPath, PhysicalFileSystem fileSystem)
        {
            fileSystem.FileExists(physicalPath).ShouldEqual(true);
        }

        public static void ShouldNotBeAPhysicalFile(this string physicalPath, PhysicalFileSystem fileSystem)
        {
            fileSystem.FileExists(physicalPath).ShouldEqual(false);
        }
    }
}
