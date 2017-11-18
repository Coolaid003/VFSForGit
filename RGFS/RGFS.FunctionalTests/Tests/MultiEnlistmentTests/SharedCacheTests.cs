﻿using RGFS.FunctionalTests.FileSystemRunners;
using RGFS.FunctionalTests.Tools;
using RGFS.Tests.Should;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RGFS.FunctionalTests.Tests.MultiEnlistmentTests
{
    [TestFixture]
    [Ignore("TODO 1081003: Re-enable shared cache")]
    public class SharedCacheTests : TestsWithMultiEnlistment
    {
        private const string WellKnownFile = "Readme.md";

        // This branch and commit sha should point to the same place.
        private const string WellKnownBranch = "FunctionalTests/20170602";
        private const string WellKnownCommitSha = "b407df4e21261e2bf022ef7031fabcf21ee0e14d";

        private string pathToObjectCache;
        private string localRgfsRoot;

        [SetUp]
        public void SetCacheLocation()
        {
            this.localRgfsRoot = Path.Combine(Properties.Settings.Default.EnlistmentRoot, "..", Guid.NewGuid().ToString("N"));
            this.pathToObjectCache = Path.Combine(this.localRgfsRoot, ".rgfsCache", "gitObjects");
        }

        [TearDown]
        public void DeleteCache()
        {
            this.DeleteSharedCache();
        }

        [TestCase]
        public void SecondCloneDoesNotDownloadAdditionalObjects()
        {
            RGFSFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment(this.pathToObjectCache);
            File.ReadAllText(Path.Combine(enlistment1.RepoRoot, WellKnownFile));
            
            string[] allObjects = Directory.EnumerateFiles(enlistment1.ObjectRoot, "*", SearchOption.AllDirectories).ToArray();

            RGFSFunctionalTestEnlistment enlistment2 = this.CreateNewEnlistment(this.pathToObjectCache);
            File.ReadAllText(Path.Combine(enlistment2.RepoRoot, WellKnownFile));

            enlistment2.ObjectRoot.ShouldEqual(enlistment1.ObjectRoot, "Sanity: Object roots are expected to match.");
            Directory.EnumerateFiles(enlistment2.ObjectRoot, "*", SearchOption.AllDirectories)
                .ShouldMatchInOrder(allObjects);
        }
        
        [TestCase]
        public void ParallelReadsInASharedCache()
        {
            RGFSFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment(this.pathToObjectCache);
            RGFSFunctionalTestEnlistment enlistment2 = this.CreateNewEnlistment(this.pathToObjectCache);
            RGFSFunctionalTestEnlistment enlistment3 = null;

            Task task1 = Task.Run(() => this.HydrateEntireRepo(enlistment1));
            Task task2 = Task.Run(() => this.HydrateEntireRepo(enlistment2));
            Task task3 = Task.Run(() => enlistment3 = this.CreateNewEnlistment(this.pathToObjectCache));

            task1.Wait();
            task2.Wait();
            task3.Wait();

            enlistment1.Status().ShouldContain("Mount status: Ready");
            enlistment2.Status().ShouldContain("Mount status: Ready");
            enlistment3.Status().ShouldContain("Mount status: Ready");
        }

        [TestCase]
        public void DeleteCacheBeforeMount()
        {
            RGFSFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment(this.pathToObjectCache);
            RGFSFunctionalTestEnlistment enlistment2 = this.CreateNewEnlistment(this.pathToObjectCache);

            enlistment1.UnmountRGFS();

            this.DeleteSharedCache();

            enlistment1.MountRGFS();

            Task task1 = Task.Run(() => this.HydrateEntireRepo(enlistment1));
            Task task2 = Task.Run(() => this.HydrateEntireRepo(enlistment2));
            task1.Wait();
            task2.Wait();

            enlistment1.Status().ShouldContain("Mount status: Ready");
            enlistment2.Status().ShouldContain("Mount status: Ready");
        }

        [TestCase]
        public void DeleteCacheAtOffsetBetweenHydrations()
        {
            RGFSFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment(this.pathToObjectCache);

            Task task1 = Task.Run(() =>
            {
                this.HydrateEntireRepo(enlistment1);
            });

            while (!task1.IsCompleted)
            {
                try
                {
                    this.DeleteSharedCache();
                    Thread.Sleep(100);
                }
                catch (IOException)
                {
                    // Hydration may have handles into the cache, so failing this delete is expected.
                }
            }

            enlistment1.Status().ShouldContain("Mount status: Ready");
        }

        [TestCase]
        public void DownloadingACommitWithoutTreesDoesntBreakNextClone()
        {
            RGFSFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment(this.pathToObjectCache);
            GitProcess.Invoke(enlistment1.RepoRoot, "cat-file -s " + WellKnownCommitSha).ShouldEqual("293\n");

            RGFSFunctionalTestEnlistment enlistment2 = this.CreateNewEnlistment(this.pathToObjectCache, WellKnownBranch);
            enlistment2.Status().ShouldContain("Mount status: Ready");
        }

        private void HydrateEntireRepo(RGFSFunctionalTestEnlistment enlistment)
        {
            List<string> allFiles = Directory.EnumerateFiles(enlistment.RepoRoot, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < allFiles.Count; ++i)
            {
                File.ReadAllText(allFiles[i]);
            }
        }

        private void DeleteSharedCache()
        {
            CmdRunner.DeleteDirectoryWithRetry(this.localRgfsRoot);
        }
    }
}
