﻿using GVFS.FunctionalTests.FileSystemRunners;
using GVFS.FunctionalTests.Tools;
using GVFS.Tests.Should;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GVFS.FunctionalTests.Tests.EnlistmentPerFixture
{
    [TestFixtureSource(typeof(FileSystemRunner), nameof(FileSystemRunner.Runners))]
    public class ParallelHydrationTests : TestsWithEnlistmentPerFixture
    {
        private FileSystemRunner fileSystem;

        public ParallelHydrationTests(FileSystemRunner fileSystem)
                : base(forcePerRepoObjectCache: true)
        {
            this.fileSystem = fileSystem;
        }

        [TestCase]
        [Category(Categories.ExtraCoverage)]
        public void HydrateRepoInParallel()
        {
            GitProcess.Invoke(this.Enlistment.RepoRoot, $"checkout -f {FileConstants.CommitId}");

            ConcurrentBag<string> collection = new ConcurrentBag<string>();
            List<Thread> threads = new List<Thread>();
            foreach (string path in FileConstants.Paths)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        this.fileSystem.ReadAllText(this.Enlistment.GetVirtualPathTo(path));
                        collection.Add(path);
                    }
                    catch (Exception e)
                    {
                        collection.Add($"Exception while hydrating {path}: {e.Message}");
                    }
                });
                threads.Add(thread);
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            for (int i = 0; i < FileConstants.Paths.Count; i++)
            {
                collection.TryTake(out string value).ShouldBeTrue();
                FileConstants.Paths.Contains(value).ShouldBeTrue(message: value);
            }
        }

        private class FileConstants
        {
            public static readonly string CommitId = "b76df49a1e02465ef1c27d9c2a1720b337de99c8";

            /// <summary>
            /// Generate in Git Bash using command:
            /// git ls-tree --full-tree -r --name-only HEAD | awk '{print "\""$1"\","}'
            /// </summary>
            public static HashSet<string> Paths = new HashSet<string>()
            {
                ".gitattributes",
                ".gitignore",
                "AuthoringTests.md",
                "DeleteFileWithNameAheadOfDotAndSwitchCommits/(1).txt",
                "DeleteFileWithNameAheadOfDotAndSwitchCommits/1",
                "DeleteFileWithNameAheadOfDotAndSwitchCommits/test.txt",
                "EnumerateAndReadTestFiles/.B",
                "EnumerateAndReadTestFiles/._",
                "EnumerateAndReadTestFiles/._a",
                "EnumerateAndReadTestFiles/.~B",
                "EnumerateAndReadTestFiles/.~_B",
                "EnumerateAndReadTestFiles/A_100.txt",
                "EnumerateAndReadTestFiles/_C",
                "EnumerateAndReadTestFiles/_a",
                "EnumerateAndReadTestFiles/_aB",
                "EnumerateAndReadTestFiles/a",
                "EnumerateAndReadTestFiles/a.txt",
                "EnumerateAndReadTestFiles/a_1.txt",
                "EnumerateAndReadTestFiles/a_10.txt",
                "EnumerateAndReadTestFiles/a_3.txt",
                "EnumerateAndReadTestFiles/ab_",
                "EnumerateAndReadTestFiles/z_test.txt",
                "EnumerateAndReadTestFiles/zctest.txt",
                "EnumerateAndReadTestFiles/~B",
                "ErrorWhenPathTreatsFileAsFolderMatchesNTFS/full",
                "ErrorWhenPathTreatsFileAsFolderMatchesNTFS/partial",
                "ErrorWhenPathTreatsFileAsFolderMatchesNTFS/virtual",
                "GVFS.sln",
                "GVFS/FastFetch/App.config",
                "GVFS/FastFetch/CheckoutFetchHelper.cs",
                "GVFS/FastFetch/FastFetch.csproj",
                "GVFS/FastFetch/FastFetchVerb.cs",
                "GVFS/FastFetch/FetchHelper.cs",
                "GVFS/FastFetch/Git/DiffHelper.cs",
                "GVFS/FastFetch/Git/GitPackIndex.cs",
                "GVFS/FastFetch/Git/LibGit2Helpers.cs",
                "GVFS/FastFetch/Git/LibGit2Repo.cs",
                "GVFS/FastFetch/Git/LsTreeHelper.cs",
                "GVFS/FastFetch/Git/RefSpecHelpers.cs",
                "GVFS/FastFetch/Git/UpdateRefsHelper.cs",
                "GVFS/FastFetch/GitEnlistment.cs",
                "GVFS/FastFetch/Jobs/BatchObjectDownloadJob.cs",
                "GVFS/FastFetch/Jobs/CheckoutJob.cs",
                "GVFS/FastFetch/Jobs/Data/BlobDownloadRequest.cs",
                "GVFS/FastFetch/Jobs/Data/IndexPackRequest.cs",
                "GVFS/FastFetch/Jobs/Data/TreeSearchRequest.cs",
                "GVFS/FastFetch/Jobs/FindMissingBlobsJob.cs",
                "GVFS/FastFetch/Jobs/IndexPackJob.cs",
                "GVFS/FastFetch/Jobs/Job.cs",
                "GVFS/FastFetch/Program.cs",
                "GVFS/FastFetch/Properties/AssemblyInfo.cs",
                "GVFS/FastFetch/packages.config",
                "GVFS/GVFS.Common/AntiVirusExclusions.cs",
                "GVFS/GVFS.Common/BatchedLooseObjects/BatchedLooseObjectDeserializer.cs",
                "GVFS/GVFS.Common/CallbackResult.cs",
                "GVFS/GVFS.Common/ConcurrentHashSet.cs",
                "GVFS/GVFS.Common/Enlistment.cs",
                "GVFS/GVFS.Common/GVFS.Common.csproj",
                "GVFS/GVFS.Common/GVFSConstants.cs",
                "GVFS/GVFS.Common/GVFSContext.cs",
                "GVFS/GVFS.Common/GVFSEnlistment.cs",
                "GVFS/GVFS.Common/GVFSLock.cs",
                "GVFS/GVFS.Common/Git/CatFileTimeoutException.cs",
                "GVFS/GVFS.Common/Git/DiffTreeResult.cs",
                "GVFS/GVFS.Common/Git/GVFSConfigResponse.cs",
                "GVFS/GVFS.Common/Git/GitCatFileBatchCheckProcess.cs",
                "GVFS/GVFS.Common/Git/GitCatFileBatchProcess.cs",
                "GVFS/GVFS.Common/Git/GitCatFileProcess.cs",
                "GVFS/GVFS.Common/Git/GitObjects.cs",
                "GVFS/GVFS.Common/Git/GitPathConverter.cs",
                "GVFS/GVFS.Common/Git/GitProcess.cs",
                "GVFS/GVFS.Common/Git/GitRefs.cs",
                "GVFS/GVFS.Common/Git/GitTreeEntry.cs",
                "GVFS/GVFS.Common/Git/GitVersion.cs",
                "GVFS/GVFS.Common/Git/HttpGitObjects.cs",
                "GVFS/GVFS.Common/GitHelper.cs",
                "GVFS/GVFS.Common/HeartbeatThread.cs",
                "GVFS/GVFS.Common/IBackgroundOperation.cs",
                "GVFS/GVFS.Common/InvalidRepoException.cs",
                "GVFS/GVFS.Common/MountParameters.cs",
                "GVFS/GVFS.Common/NamedPipes/BrokenPipeException.cs",
                "GVFS/GVFS.Common/NamedPipes/NamedPipeClient.cs",
                "GVFS/GVFS.Common/NamedPipes/NamedPipeMessages.cs",
                "GVFS/GVFS.Common/NamedPipes/NamedPipeServer.cs",
                "GVFS/GVFS.Common/NativeMethods.cs",
                "GVFS/GVFS.Common/Physical/FileSystem/DirectoryItemInfo.cs",
                "GVFS/GVFS.Common/Physical/FileSystem/FileProperties.cs",
                "GVFS/GVFS.Common/Physical/FileSystem/PhysicalFileSystem.cs",
                "GVFS/GVFS.Common/Physical/FileSystem/StreamReaderExtensions.cs",
                "GVFS/GVFS.Common/Physical/Git/BigEndianReader.cs",
                "GVFS/GVFS.Common/Physical/Git/CopyBlobContentTimeoutException.cs",
                "GVFS/GVFS.Common/Physical/Git/EndianHelper.cs",
                "GVFS/GVFS.Common/Physical/Git/GVFSGitObjects.cs",
                "GVFS/GVFS.Common/Physical/Git/GitIndex.cs",
                "GVFS/GVFS.Common/Physical/Git/GitRepo.cs",
                "GVFS/GVFS.Common/Physical/RegistryUtils.cs",
                "GVFS/GVFS.Common/Physical/RepoMetadata.cs",
                "GVFS/GVFS.Common/PrefetchPacks/PrefetchPacksDeserializer.cs",
                "GVFS/GVFS.Common/ProcessHelper.cs",
                "GVFS/GVFS.Common/ProcessPool.cs",
                "GVFS/GVFS.Common/ProcessResult.cs",
                "GVFS/GVFS.Common/Properties/AssemblyInfo.cs",
                "GVFS/GVFS.Common/ReliableBackgroundOperations.cs",
                "GVFS/GVFS.Common/RetryWrapper.cs",
                "GVFS/GVFS.Common/RetryableException.cs",
                "GVFS/GVFS.Common/ReturnCode.cs",
                "GVFS/GVFS.Common/TaskExtensions.cs",
                "GVFS/GVFS.Common/Tracing/ConsoleEventListener.cs",
                "GVFS/GVFS.Common/Tracing/EventMetadata.cs",
                "GVFS/GVFS.Common/Tracing/ITracer.cs",
                "GVFS/GVFS.Common/Tracing/InProcEventListener.cs",
                "GVFS/GVFS.Common/Tracing/JsonEtwTracer.cs",
                "GVFS/GVFS.Common/Tracing/Keywords.cs",
                "GVFS/GVFS.Common/Tracing/LogFileEventListener.cs",
                "GVFS/GVFS.Common/WindowsProcessJob.cs",
                "GVFS/GVFS.Common/packages.config",
                "GVFS/GVFS.FunctionalTests/Category/CategoryConstants.cs",
                "GVFS/GVFS.FunctionalTests/FileSystemRunners/BashRunner.cs",
                "GVFS/GVFS.FunctionalTests/FileSystemRunners/CmdRunner.cs",
                "GVFS/GVFS.FunctionalTests/FileSystemRunners/FileSystemRunner.cs",
                "GVFS/GVFS.FunctionalTests/FileSystemRunners/PowerShellRunner.cs",
                "GVFS/GVFS.FunctionalTests/FileSystemRunners/ShellRunner.cs",
                "GVFS/GVFS.FunctionalTests/FileSystemRunners/SystemIORunner.cs",
                "GVFS/GVFS.FunctionalTests/GVFS.FunctionalTests.csproj",
                "GVFS/GVFS.FunctionalTests/Program.cs",
                "GVFS/GVFS.FunctionalTests/Properties/AssemblyInfo.cs",
                "GVFS/GVFS.FunctionalTests/Properties/Settings.Designer.cs",
                "GVFS/GVFS.FunctionalTests/Properties/Settings.settings",
                "GVFS/GVFS.FunctionalTests/Should/FileSystemShouldExtensions.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/DiagnoseTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/GitCommandsTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/GitFilesTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/MountTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/MoveRenameFileTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/MoveRenameFileTests_2.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/MoveRenameFolderTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/TestsWithEnlistmentPerFixture.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerFixture/WorkingDirectoryTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerTestCase/CaseOnlyFolderRenameTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerTestCase/PersistedSparseExcludeTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerTestCase/PersistedWorkingDirectoryTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerTestCase/PrefetchVerbTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerTestCase/RebaseTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/EnlistmentPerTestCase/TestsWithEnlistmentPerTestCase.cs",
                "GVFS/GVFS.FunctionalTests/Tests/FastFetchTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/LongRunningEnlistment/GitMoveRenameTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/LongRunningEnlistment/GitObjectManipulationTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/LongRunningEnlistment/GitReadAndGitLockTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/LongRunningEnlistment/LongRunningSetup.cs",
                "GVFS/GVFS.FunctionalTests/Tests/LongRunningEnlistment/MultithreadedReadWriteTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/LongRunningEnlistment/TestsWithLongRunningEnlistment.cs",
                "GVFS/GVFS.FunctionalTests/Tests/LongRunningEnlistment/WorkingDirectoryTests.cs",
                "GVFS/GVFS.FunctionalTests/Tests/PrintTestCaseStats.cs",
                "GVFS/GVFS.FunctionalTests/Tools/ControlGitRepo.cs",
                "GVFS/GVFS.FunctionalTests/Tools/GVFSFunctionalTestEnlistment.cs",
                "GVFS/GVFS.FunctionalTests/Tools/GVFSProcess.cs",
                "GVFS/GVFS.FunctionalTests/Tools/GitHelpers.cs",
                "GVFS/GVFS.FunctionalTests/Tools/GitProcess.cs",
                "GVFS/GVFS.FunctionalTests/Tools/NativeMethods.cs",
                "GVFS/GVFS.FunctionalTests/Tools/ProcessHelper.cs",
                "GVFS/GVFS.FunctionalTests/Tools/ProcessResult.cs",
                "GVFS/GVFS.FunctionalTests/Tools/TestConstants.cs",
                "GVFS/GVFS.FunctionalTests/app.config",
                "GVFS/GVFS.FunctionalTests/packages.config",
                "GVFS/GVFS.Hooks/App.config",
                "GVFS/GVFS.Hooks/GVFS.Hooks.csproj",
                "GVFS/GVFS.Hooks/KnownGitCommands.cs",
                "GVFS/GVFS.Hooks/Program.cs",
                "GVFS/GVFS.Hooks/Properties/AssemblyInfo.cs",
                "GVFS/GVFS.Hooks/packages.config",
                "GVFS/GVFS.Mount/GVFS.Mount.csproj",
                "GVFS/GVFS.Mount/InProcessMount.cs",
                "GVFS/GVFS.Mount/MountAbortedException.cs",
                "GVFS/GVFS.Mount/MountVerb.cs",
                "GVFS/GVFS.Mount/Program.cs",
                "GVFS/GVFS.Mount/Properties/AssemblyInfo.cs",
                "GVFS/GVFS.Mount/packages.config",
                "GVFS/GVFS.NativeTests/GVFS.NativeTests.vcxproj",
                "GVFS/GVFS.NativeTests/GVFS.NativeTests.vcxproj.filters",
                "GVFS/GVFS.NativeTests/ReadMe.txt",
                "GVFS/GVFS.NativeTests/include/NtFunctions.h",
                "GVFS/GVFS.NativeTests/include/SafeHandle.h",
                "GVFS/GVFS.NativeTests/include/SafeOverlapped.h",
                "GVFS/GVFS.NativeTests/include/Should.h",
                "GVFS/GVFS.NativeTests/include/TestException.h",
                "GVFS/GVFS.NativeTests/include/TestHelpers.h",
                "GVFS/GVFS.NativeTests/include/TestVerifiers.h",
                "GVFS/GVFS.NativeTests/include/gvflt.h",
                "GVFS/GVFS.NativeTests/include/gvlib_internal.h",
                "GVFS/GVFS.NativeTests/include/stdafx.h",
                "GVFS/GVFS.NativeTests/include/targetver.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_BugRegressionTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_DeleteFileTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_DeleteFolderTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_DirEnumTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_FileAttributeTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_FileEATest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_FileOperationTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_MoveFileTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_MoveFolderTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_MultiThreadsTest.h",
                "GVFS/GVFS.NativeTests/interface/GVFlt_SetLinkTest.h",
                "GVFS/GVFS.NativeTests/interface/NtQueryDirectoryFileTests.h",
                "GVFS/GVFS.NativeTests/interface/PlaceholderUtils.h",
                "GVFS/GVFS.NativeTests/interface/ReadAndWriteTests.h",
                "GVFS/GVFS.NativeTests/interface/TrailingSlashTests.h",
                "GVFS/GVFS.NativeTests/source/GVFlt_BugRegressionTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_DeleteFileTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_DeleteFolderTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_DirEnumTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_FileAttributeTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_FileEATest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_FileOperationTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_MoveFileTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_MoveFolderTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_MultiThreadTest.cpp",
                "GVFS/GVFS.NativeTests/source/GVFlt_SetLinkTest.cpp",
                "GVFS/GVFS.NativeTests/source/NtFunctions.cpp",
                "GVFS/GVFS.NativeTests/source/NtQueryDirectoryFileTests.cpp",
                "GVFS/GVFS.NativeTests/source/PlaceholderUtils.cpp",
                "GVFS/GVFS.NativeTests/source/ReadAndWriteTests.cpp",
                "GVFS/GVFS.NativeTests/source/TrailingSlashTests.cpp",
                "GVFS/GVFS.NativeTests/source/dllmain.cpp",
                "GVFS/GVFS.NativeTests/source/stdafx.cpp",
                "GVFS/GVFS.ReadObjectHook/GVFS.ReadObjectHook.vcxproj",
                "GVFS/GVFS.ReadObjectHook/GVFS.ReadObjectHook.vcxproj.filters",
                "GVFS/GVFS.ReadObjectHook/Version.rc",
                "GVFS/GVFS.ReadObjectHook/main.cpp",
                "GVFS/GVFS.ReadObjectHook/resource.h",
                "GVFS/GVFS.ReadObjectHook/stdafx.cpp",
                "GVFS/GVFS.ReadObjectHook/stdafx.h",
                "GVFS/GVFS.ReadObjectHook/targetver.h",
                "GVFS/GVFS.Service/GVFS.Service.csproj",
                "GVFS/GVFS.Service/GvfsService.cs",
                "GVFS/GVFS.Service/GvfsServiceInstaller.cs",
                "GVFS/GVFS.Service/Program.cs",
                "GVFS/GVFS.Service/Properties/AssemblyInfo.cs",
                "GVFS/GVFS.Service/packages.config",
                "GVFS/GVFS.Tests/GVFS.Tests.csproj",
                "GVFS/GVFS.Tests/NUnitRunner.cs",
                "GVFS/GVFS.Tests/Properties/AssemblyInfo.cs",
                "GVFS/GVFS.Tests/Should/EnumerableShouldExtensions.cs",
                "GVFS/GVFS.Tests/Should/StringExtensions.cs",
                "GVFS/GVFS.Tests/Should/StringShouldExtensions.cs",
                "GVFS/GVFS.Tests/Should/ValueShouldExtensions.cs",
                "GVFS/GVFS.Tests/packages.config",
                "GVFS/GVFS.UnitTests/App.config",
                "GVFS/GVFS.UnitTests/Category/CategoryContants.cs",
                "GVFS/GVFS.UnitTests/Common/GitHelperTests.cs",
                "GVFS/GVFS.UnitTests/Common/GitPathConverterTests.cs",
                "GVFS/GVFS.UnitTests/Common/GitVersionTests.cs",
                "GVFS/GVFS.UnitTests/Common/JsonEtwTracerTests.cs",
                "GVFS/GVFS.UnitTests/Common/ProcessHelperTests.cs",
                "GVFS/GVFS.UnitTests/Common/RetryWrapperTests.cs",
                "GVFS/GVFS.UnitTests/Common/SHA1UtilTests.cs",
                "GVFS/GVFS.UnitTests/Data/backward.txt",
                "GVFS/GVFS.UnitTests/Data/forward.txt",
                "GVFS/GVFS.UnitTests/Data/index_v2",
                "GVFS/GVFS.UnitTests/Data/index_v3",
                "GVFS/GVFS.UnitTests/Data/index_v4",
                "GVFS/GVFS.UnitTests/FastFetch/BatchObjectDownloadJobTests.cs",
                "GVFS/GVFS.UnitTests/FastFetch/DiffHelperTests.cs",
                "GVFS/GVFS.UnitTests/FastFetch/FastFetchTracingTests.cs",
                "GVFS/GVFS.UnitTests/GVFS.UnitTests.csproj",
                "GVFS/GVFS.UnitTests/GVFlt/DotGit/ExcludeFileTests.cs",
                "GVFS/GVFS.UnitTests/GVFlt/DotGit/GitConfigFileUtilsTests.cs",
                "GVFS/GVFS.UnitTests/GVFlt/GVFltActiveEnumerationTests.cs",
                "GVFS/GVFS.UnitTests/GVFlt/GVFltCallbacksTests.cs",
                "GVFS/GVFS.UnitTests/GVFlt/PathUtilTests.cs",
                "GVFS/GVFS.UnitTests/GVFlt/Physical/FileSerializerTests.cs",
                "GVFS/GVFS.UnitTests/Mock/Common/MockEnlistment.cs",
                "GVFS/GVFS.UnitTests/Mock/Common/MockPhysicalGitObjects.cs",
                "GVFS/GVFS.UnitTests/Mock/Common/MockTracer.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/FileSystem/MassiveMockFileSystem.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/FileSystem/MockDirectory.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/FileSystem/MockFile.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/FileSystem/MockFileSystem.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/FileSystem/MockSafeHandle.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/Git/MockBatchHttpGitObjects.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/Git/MockGVFSGitObjects.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/Git/MockGitIndex.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/Git/MockGitRepo.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/Git/MockHttpGitObjects.cs",
                "GVFS/GVFS.UnitTests/Mock/Physical/ReusableMemoryStream.cs",
                "GVFS/GVFS.UnitTests/Physical/Git/GitCatFileBatchProcessTests.cs",
                "GVFS/GVFS.UnitTests/Physical/Git/PhysicalGitObjectsTests.cs",
                "GVFS/GVFS.UnitTests/Prefetch/PrefetchPacksDeserializerTests.cs",
                "GVFS/GVFS.UnitTests/Program.cs",
                "GVFS/GVFS.UnitTests/Properties/AssemblyInfo.cs",
                "GVFS/GVFS.UnitTests/Should/StringShouldExtensions.cs",
                "GVFS/GVFS.UnitTests/Virtual/CommonRepoSetup.cs",
                "GVFS/GVFS.UnitTests/Virtual/DotGit/GitIndexTests.cs",
                "GVFS/GVFS.UnitTests/Virtual/TestsWithCommonRepo.cs",
                "GVFS/GVFS.UnitTests/packages.config",
                "GVFS/GVFS/App.config",
                "GVFS/GVFS/CommandLine/CloneHelper.cs",
                "GVFS/GVFS/CommandLine/CloneVerb.cs",
                "GVFS/GVFS/CommandLine/DiagnoseVerb.cs",
                "GVFS/GVFS/CommandLine/GVFSVerb.cs",
                "GVFS/GVFS/CommandLine/LogVerb.cs",
                "GVFS/GVFS/CommandLine/MountVerb.cs",
                "GVFS/GVFS/CommandLine/PrefetchHelper.cs",
                "GVFS/GVFS/CommandLine/PrefetchVerb.cs",
                "GVFS/GVFS/CommandLine/StatusVerb.cs",
                "GVFS/GVFS/CommandLine/UnmountVerb.cs",
                "GVFS/GVFS/GVFS.csproj",
                "GVFS/GVFS/GitVirtualFileSystem.ico",
                "GVFS/GVFS/Program.cs",
                "GVFS/GVFS/Properties/AssemblyInfo.cs",
                "GVFS/GVFS/Setup.iss",
                "GVFS/GVFS/packages.config",
                "GitCommandsTests/CheckoutNewBranchFromStartingPointTest/test1.txt",
                "GitCommandsTests/CheckoutNewBranchFromStartingPointTest/test2.txt",
                "GitCommandsTests/CheckoutOrhpanBranchFromStartingPointTest/test1.txt",
                "GitCommandsTests/CheckoutOrhpanBranchFromStartingPointTest/test2.txt",
                "GitCommandsTests/DeleteFileTests/1/#test",
                "GitCommandsTests/DeleteFileTests/2/$test",
                "GitCommandsTests/DeleteFileTests/3/)",
                "GitCommandsTests/DeleteFileTests/4/+.test",
                "GitCommandsTests/DeleteFileTests/5/-.test",
                "GitCommandsTests/RenameFileTests/1/#test",
                "GitCommandsTests/RenameFileTests/2/$test",
                "GitCommandsTests/RenameFileTests/3/)",
                "GitCommandsTests/RenameFileTests/4/+.test",
                "GitCommandsTests/RenameFileTests/5/-.test",
                "Protocol.md",
                "Readme.md",
                "Scripts/CreateCommonAssemblyVersion.bat",
                "Scripts/CreateCommonCliAssemblyVersion.bat",
                "Scripts/CreateCommonVersionHeader.bat",
                "Scripts/RunFunctionalTests.bat",
                "Scripts/RunUnitTests.bat",
                "Settings.StyleCop",
            };
        }
    }
}