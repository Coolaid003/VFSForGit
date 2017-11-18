﻿using CommandLine;
using RGFS.Common;
using RGFS.Common.NamedPipes;
using System.Diagnostics;

namespace RGFS.CommandLine
{
    [Verb(UnmountVerb.UnmountVerbName, HelpText = "Unmount a RGFS virtual repo")]
    public class UnmountVerb : RGFSVerb
    {
        private const string UnmountVerbName = "unmount";

        [Value(
            0,
            Required = false,
            Default = "",
            MetaName = "Enlistment Root Path",
            HelpText = "Full or relative path to the RGFS enlistment root")]
        public override string EnlistmentRootPath { get; set; }

        [Option(
            RGFSConstants.VerbParameters.Unmount.SkipLock,
            Default = false,
            Required = false,
            HelpText = "Force unmount even if the lock is not available.")]
        public bool SkipLock { get; set; }

        public bool SkipUnregister { get; set; }

        protected override string VerbName
        {
            get { return UnmountVerbName; }
        }

        public override void Execute()
        {
            string root = Paths.GetRGFSEnlistmentRoot(this.EnlistmentRootPath);
            if (root == null)
            {
                this.ReportErrorAndExit(
                    "Error: '{0}' is not a valid RGFS enlistment",
                    this.EnlistmentRootPath);
            }

            if (!this.SkipLock)
            {
                this.AcquireLock(root);
            }

            string errorMessage = null;
            if (!this.ShowStatusWhileRunning(
                () => { return this.Unmount(root, out errorMessage); },
                "Unmounting"))
            {
                this.ReportErrorAndExit(errorMessage);
            }

            if (!this.Unattended && !this.SkipUnregister)
            {
                if (!this.ShowStatusWhileRunning(
                    () => { return this.UnregisterRepo(root, out errorMessage); },
                    "Unregistering automount"))
                {
                    this.Output.WriteLine("    WARNING: " + errorMessage);
                }
            }
        }

        private bool Unmount(string enlistmentRoot, out string errorMessage)
        {
            errorMessage = string.Empty;

            string pipeName = Paths.GetNamedPipeName(enlistmentRoot);
            string rawGetStatusResponse = string.Empty;

            try
            {
                using (NamedPipeClient pipeClient = new NamedPipeClient(pipeName))
                {
                    if (!pipeClient.Connect())
                    {
                        errorMessage = "Unable to connect to RGFS.Mount";
                        return false;
                    }

                    pipeClient.SendRequest(NamedPipeMessages.GetStatus.Request);
                    rawGetStatusResponse = pipeClient.ReadRawResponse();
                    NamedPipeMessages.GetStatus.Response getStatusResponse =
                        NamedPipeMessages.GetStatus.Response.FromJson(rawGetStatusResponse);

                    switch (getStatusResponse.MountStatus)
                    {
                        case NamedPipeMessages.GetStatus.Mounting:
                            errorMessage = "Still mounting, please try again later";
                            return false;

                        case NamedPipeMessages.GetStatus.Unmounting:
                            errorMessage = "Already unmounting, please wait";
                            return false;

                        case NamedPipeMessages.GetStatus.Ready:
                            break;

                        case NamedPipeMessages.GetStatus.MountFailed:
                            break;

                        default:
                            errorMessage = "Unrecognized response to GetStatus: " + rawGetStatusResponse;
                            return false;
                    }

                    pipeClient.SendRequest(NamedPipeMessages.Unmount.Request);
                    string unmountResponse = pipeClient.ReadRawResponse();

                    switch (unmountResponse)
                    {
                        case NamedPipeMessages.Unmount.Acknowledged:
                            string finalResponse = pipeClient.ReadRawResponse();
                            if (finalResponse == NamedPipeMessages.Unmount.Completed)
                            {
                                errorMessage = string.Empty;
                                return true;
                            }
                            else
                            {
                                errorMessage = "Unrecognized final response to unmount: " + finalResponse;
                                return false;
                            }

                        case NamedPipeMessages.Unmount.NotMounted:
                            errorMessage = "Unable to unmount, repo was not mounted";
                            return false;

                        case NamedPipeMessages.Unmount.MountFailed:
                            errorMessage = "Unable to unmount, previous mount attempt failed";
                            return false;

                        default:
                            errorMessage = "Unrecognized response to unmount: " + unmountResponse;
                            return false;
                    }
                }
            }
            catch (BrokenPipeException e)
            {
                errorMessage = "Unable to communicate with RGFS: " + e.ToString();
                return false;
            }
        }

        private bool UnregisterRepo(string rootPath, out string errorMessage)
        {
            errorMessage = string.Empty;
            NamedPipeMessages.UnregisterRepoRequest request = new NamedPipeMessages.UnregisterRepoRequest();
            request.EnlistmentRoot = rootPath;

            using (NamedPipeClient client = new NamedPipeClient(this.ServicePipeName))
            {
                if (!client.Connect())
                {
                    errorMessage = "Unable to unregister repo because RGFS.Service is not responding. " + RGFSVerb.StartServiceInstructions;
                    return false;
                }

                try
                {
                    client.SendRequest(request.ToMessage());
                    NamedPipeMessages.Message response = client.ReadResponse();
                    if (response.Header == NamedPipeMessages.UnregisterRepoRequest.Response.Header)
                    {
                        NamedPipeMessages.UnregisterRepoRequest.Response message = NamedPipeMessages.UnregisterRepoRequest.Response.FromMessage(response);

                        if (message.State != NamedPipeMessages.CompletionState.Success)
                        {
                            errorMessage = message.ErrorMessage;
                            return false;
                        }
                        else
                        {
                            errorMessage = string.Empty;
                            return true;
                        }
                    }
                    else
                    {
                        errorMessage = string.Format("RGFS.Service responded with unexpected message: {0}", response);
                        return false;
                    }
                }
                catch (BrokenPipeException e)
                {
                    errorMessage = "Unable to communicate with RGFS.Service: " + e.ToString();
                    return false;
                }
            }
        }

        private void AcquireLock(string enlistmentRoot)
        {
            string pipeName = Paths.GetNamedPipeName(enlistmentRoot);
            using (NamedPipeClient pipeClient = new NamedPipeClient(pipeName))
            {
                try
                {
                    if (!pipeClient.Connect())
                    {
                        this.ReportErrorAndExit("Unable to connect to RGFS while acquiring lock to unmount.  Try 'rgfs status' to verify if the repo is mounted.");
                        return;
                    }

                    Process currentProcess = Process.GetCurrentProcess();
                    string result = null;
                    if (!RGFSLock.TryAcquireRGFSLockForProcess(
                            this.Unattended,
                            pipeClient, 
                            "rgfs unmount", 
                            currentProcess.Id, 
                            ProcessHelper.IsAdminElevated(),
                            currentProcess,
                            enlistmentRoot,
                            out result))
                    {
                        this.ReportErrorAndExit("Unable to acquire the lock prior to unmount. " + result);
                    }
                }
                catch (BrokenPipeException)
                {
                    this.ReportErrorAndExit("Unable to acquire the lock prior to unmount.  Try 'rgfs status' to verify if the repo is mounted.");
                }
            }
        }
    }
}
