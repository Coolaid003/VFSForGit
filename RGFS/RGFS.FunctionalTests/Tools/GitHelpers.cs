﻿using RGFS.Tests.Should;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RGFS.FunctionalTests.Tools
{
    public static class GitHelpers
    {
        public const string AlwaysExcludeFilePath = @".git\info\always_exclude";

        public static void CheckGitCommand(string virtualRepoRoot, string command, params string[] expectedLinesInResult)
        {
            ProcessResult result = GitProcess.InvokeProcess(virtualRepoRoot, command);
            result.Errors.ShouldBeEmpty();
            foreach (string line in expectedLinesInResult)
            {
                result.Output.ShouldContain(line);
            }
        }

        public static void CheckGitCommandAgainstRGFSRepo(string virtualRepoRoot, string command, params string[] expectedLinesInResult)
        {
            ProcessResult result = InvokeGitAgainstRGFSRepo(virtualRepoRoot, command);
            result.Errors.ShouldBeEmpty();
            foreach (string line in expectedLinesInResult)
            {
                result.Output.ShouldContain(line);
            }
        }

        public static ProcessResult InvokeGitAgainstRGFSRepo(string rgfsRepoRoot, string command, bool cleanErrors = true)
        {
            ProcessResult result = GitProcess.InvokeProcess(rgfsRepoRoot, command);

            string errors = result.Errors;
            if (cleanErrors)
            {
                string[] lines = errors.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                errors = string.Join("\r\n", lines.Where(line => !line.StartsWith("Waiting for ")));

                if (errors.Length > 0 && string.IsNullOrWhiteSpace(errors))
                {
                    errors = string.Empty;
                }
            }

            return new ProcessResult(
                result.Output,
                errors,
                result.ExitCode);
        }

        public static void ValidateGitCommand(
            RGFSFunctionalTestEnlistment enlistment,
            ControlGitRepo controlGitRepo,
            string command,
            params object[] args)
        {
            command = string.Format(command, args);
            string controlRepoRoot = controlGitRepo.RootPath;
            string rgfsRepoRoot = enlistment.RepoRoot;

            ProcessResult expectedResult = GitProcess.InvokeProcess(controlRepoRoot, command);
            ProcessResult actualResult = GitHelpers.InvokeGitAgainstRGFSRepo(rgfsRepoRoot, command);

            ErrorsShouldMatch(command, expectedResult, actualResult);
            actualResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ShouldMatchInOrder(expectedResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), LinesAreEqual, command + " Output Lines");

            if (command != "status")
            {
                ValidateGitCommand(enlistment, controlGitRepo, "status");
            }
        }

        public static ManualResetEventSlim AcquireRGFSLock(
            RGFSFunctionalTestEnlistment enlistment,
            int resetTimeout = Timeout.Infinite)
        {
            return RunGitCommandWithWaitAndStdIn(enlistment, resetTimeout: resetTimeout, command: "hash-object --stdin", stdinToQuit: "dummy");
        }

        public static ManualResetEventSlim RunGitCommandWithWaitAndStdIn(
            RGFSFunctionalTestEnlistment enlistment,
            int resetTimeout,
            string command,
            string stdinToQuit)
        {
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(initialState: false);

            ProcessStartInfo processInfo = new ProcessStartInfo(Properties.Settings.Default.PathToGit);
            processInfo.WorkingDirectory = enlistment.RepoRoot;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardInput = true;
            processInfo.Arguments = command;

            Process holdingProcess = Process.Start(processInfo);
            StreamWriter stdin = holdingProcess.StandardInput;

            enlistment.WaitForLock("git " + command);

            Task.Run(
                () =>
                {
                    resetEvent.Wait(resetTimeout);

                    // Make sure to let the holding process end.
                    if (stdin != null)
                    {
                        stdin.WriteLine(stdinToQuit);
                        stdin.Close();
                    }

                    if (holdingProcess != null)
                    {
                        if (!holdingProcess.HasExited)
                        {
                            holdingProcess.Kill();
                        }

                        holdingProcess.Dispose();
                    }

                    resetEvent.Set();
                });

            return resetEvent;
        }

        public static void ErrorsShouldMatch(string command, ProcessResult expectedResult, ProcessResult actualResult)
        {
            actualResult.Errors.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ShouldMatchInOrder(expectedResult.Errors.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), LinesAreEqual, command + " Errors Lines");
        }

        private static bool LinesAreEqual(string actualLine, string expectedLine)
        {
            return actualLine.Equals(expectedLine);
        }
    }
}
