﻿using RGFS.Common.FileSystem;
using RGFS.Common.Git;
using RGFS.Tests.Should;
using RGFS.UnitTests.Mock.Common;
using RGFS.UnitTests.Mock.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;

namespace RGFS.UnitTests.Mock.Git
{
    public class MockGitProcess : GitProcess
    {
        private Dictionary<string, Func<Result>> expectedCommands = new Dictionary<string, Func<Result>>();

        public MockGitProcess(PhysicalFileSystem fileSystem = null) 
            : base(new MockEnlistment(), fileSystem ?? new ConfigurableFileSystem())
        {
        }

        public bool ShouldFail { get; set; }

        public void SetExpectedCommandResult(string command, Func<Result> result)
        {
            this.expectedCommands[command] = result;
        }

        protected override Result InvokeGitImpl(string command, string workingDirectory, string dotGitDirectory, bool useReadObjectHook, Action<StreamWriter> writeStdIn, Action<string> parseStdOutLine, int timeoutMs)
        {
            if (this.ShouldFail)
            {
                return new Result(string.Empty, string.Empty, Result.GenericFailureCode);
            }

            Func<Result> result;
            this.expectedCommands.TryGetValue(command, out result).ShouldEqual(true, "Unexpected command: " + command);
            return result();
        }
    }
}
