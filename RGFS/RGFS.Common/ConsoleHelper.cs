﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace RGFS.Common
{
    public static class ConsoleHelper
    {
        public enum ActionResult
        {
            Success,
            CompletedWithErrors,
            Failure,
        }

        private enum StdHandle
        {
            Stdin = -10,
            Stdout = -11,
            Stderr = -12
        }

        private enum FileType : uint
        {
            Unknown = 0x0000,
            Disk = 0x0001,
            Char = 0x0002,
            Pipe = 0x0003,
            Remote = 0x8000,
        }

        public static bool ShowStatusWhileRunning(
            Func<bool> action,
            string message,
            TextWriter output,
            bool showSpinner,
            string rgfsLogEnlistmentRoot,
            int initialDelayMs = 0)
        {
            Func<ActionResult> actionResultAction =
                () =>
                {
                    return action() ? ActionResult.Success : ActionResult.Failure;
                };

            ActionResult result = ShowStatusWhileRunning(
                actionResultAction, 
                message, 
                output, 
                showSpinner, 
                rgfsLogEnlistmentRoot, 
                initialDelayMs: initialDelayMs);

            return result == ActionResult.Success;
        }

        public static ActionResult ShowStatusWhileRunning(
            Func<ActionResult> action, 
            string message, 
            TextWriter output, 
            bool showSpinner, 
            string rgfsLogEnlistmentRoot, 
            int initialDelayMs)
        {
            ActionResult result = ActionResult.Failure;
            bool initialMessageWritten = false;

            try
            {
                if (!showSpinner)
                {
                    output.Write(message + "...");
                    initialMessageWritten = true;
                    result = action();
                }
                else
                {
                    ManualResetEvent actionIsDone = new ManualResetEvent(false);
                    bool isComplete = false;
                    Thread spinnerThread = new Thread(
                        () =>
                        {
                            int retries = 0;
                            char[] waiting = { '\u2014', '\\', '|', '/' };

                            while (!isComplete)
                            {
                                if (retries == 0)
                                {
                                    actionIsDone.WaitOne(initialDelayMs);
                                }
                                else
                                {
                                    output.Write("\r{0}...{1}", message, waiting[(retries / 2) % waiting.Length]);
                                    initialMessageWritten = true;
                                    actionIsDone.WaitOne(100);
                                }

                                retries++;
                            }

                            if (initialMessageWritten)
                            {
                                // Clear out any trailing waiting character
                                output.Write("\r{0}...", message);
                            }
                        });
                    spinnerThread.Start();

                    try
                    {
                        result = action();
                    }
                    finally
                    {
                        isComplete = true;

                        actionIsDone.Set();
                        spinnerThread.Join();
                    }
                }
            }
            finally
            {
                switch (result)
                {
                    case ActionResult.Success:
                        if (initialMessageWritten)
                        {
                            output.WriteLine("Succeeded");
                        }

                        break;

                    case ActionResult.CompletedWithErrors:
                        if (!initialMessageWritten)
                        {
                            output.Write("\r{0}...", message);
                        }

                        output.WriteLine("Completed with errors.");
                        break;

                    case ActionResult.Failure:
                        if (!initialMessageWritten)
                        {
                            output.Write("\r{0}...", message);
                        }

                        output.WriteLine("Failed" + (rgfsLogEnlistmentRoot == null ? string.Empty : ". " + GetRGFSLogMessage(rgfsLogEnlistmentRoot)));
                        break;
                }
            }

            return result;
        }

        public static bool IsConsoleOutputRedirectedToFile()
        {
            return FileType.Disk == GetFileType(GetStdHandle(StdHandle.Stdout));
        }

        public static string GetRGFSLogMessage(string enlistmentRoot)
        {
            return "Run 'rgfs log " + enlistmentRoot + "' for more info.";
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(StdHandle std);

        [DllImport("kernel32.dll")]
        private static extern FileType GetFileType(IntPtr hdl);
    }
}
