﻿using RGFS.Common.FileSystem;
using NUnit.Framework;
using System;
using System.IO;

namespace RGFS.UnitTests.Mock.FileSystem
{
    public class MockFileSystemWithCallbacks : PhysicalFileSystem
    {
        public Func<bool> OnFileExists { get; set; }

        public Func<string, FileMode, FileAccess, Stream> OnOpenFileStream { get; set; }

        public override FileProperties GetFileProperties(string path)
        {
            throw new InvalidOperationException("GetFileProperties has not been implemented.");
        }

        public override bool FileExists(string path)
        {
            if (this.OnFileExists == null)
            {
                throw new InvalidOperationException("OnFileExists should be set if it is expected to be called.");
            }

            return this.OnFileExists();
        }

        public override Stream OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare shareMode, FileOptions options)
        {
            if (this.OnOpenFileStream == null)
            {
                throw new InvalidOperationException("OnOpenFileStream should be set if it is expected to be called.");
            }

            return this.OnOpenFileStream(path, fileMode, fileAccess);
        }
        
        public override void WriteAllText(string path, string contents)
        {
        }

        public override string ReadAllText(string path)
        {
            throw new InvalidOperationException("ReadAllText has not been implemented.");
        }

        public override void DeleteFile(string path)
        {
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            throw new InvalidOperationException("DeleteDirectory has not been implemented.");
        }

        public override void CreateDirectory(string path)
        {
        }
    }
}
