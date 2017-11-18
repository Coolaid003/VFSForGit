using RGFS.Common.NamedPipes;
using Newtonsoft.Json;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace RGFS.Common
{
    public partial class RGFSEnlistment : Enlistment
    {
        public const string InvalidRepoUrl = "invalid://repoUrl";

        // New enlistment
        public RGFSEnlistment(string enlistmentRoot, string repoUrl, string gitBinPath, string rgfsHooksRoot)
            : base(
                  enlistmentRoot,
                  Path.Combine(enlistmentRoot, RGFSConstants.WorkingDirectoryRootName),
                  repoUrl,
                  gitBinPath,
                  rgfsHooksRoot)
        {
            this.NamedPipeName = Paths.GetNamedPipeName(this.EnlistmentRoot);
            this.DotRGFSRoot = Path.Combine(this.EnlistmentRoot, RGFSConstants.DotRGFS.Root);
            this.RGFSLogsRoot = Path.Combine(this.EnlistmentRoot, RGFSConstants.DotRGFS.LogPath);

            this.GitObjectsRoot = Path.Combine(enlistmentRoot, RGFSConstants.DotRGFS.GitObjectCachePath);
            this.GitPackRoot = Path.Combine(this.GitObjectsRoot, RGFSConstants.DotGit.Objects.Pack.Name);
        }
        
        // Existing, configured enlistment
        public RGFSEnlistment(string enlistmentRoot, string gitBinPath, string rgfsHooksRoot)
            : this(
                  enlistmentRoot,
                  null,
                  gitBinPath,
                  rgfsHooksRoot)
        {
        }
        
        public string NamedPipeName { get; }

        public string DotRGFSRoot { get; }

        public string RGFSLogsRoot { get; }

        public override string GitObjectsRoot { get; }
        public override string GitPackRoot { get; }

        public static RGFSEnlistment CreateWithoutRepoUrlFromDirectory(string directory, string gitBinRoot, string rgfsHooksRoot)
        {
            if (Directory.Exists(directory))
            {
                string enlistmentRoot = Paths.GetRGFSEnlistmentRoot(directory);
                if (enlistmentRoot != null)
                {
                    return new RGFSEnlistment(enlistmentRoot, InvalidRepoUrl, gitBinRoot, rgfsHooksRoot);
                }
            }

            return null;
        }

        public static RGFSEnlistment CreateFromDirectory(string directory, string gitBinRoot, string rgfsHooksRoot)
        {
            if (Directory.Exists(directory))
            {
                string enlistmentRoot = Paths.GetRGFSEnlistmentRoot(directory);
                if (enlistmentRoot != null)
                {
                    return new RGFSEnlistment(enlistmentRoot, gitBinRoot, rgfsHooksRoot);
                }
            }

            return null;
        }

        public static string GetNewRGFSLogFileName(string logsRoot, string logFileType)
        {
            return Enlistment.GetNewLogFileName(
                logsRoot, 
                "rgfs_" + logFileType);
        }

        public static bool WaitUntilMounted(string enlistmentRoot, bool unattended, out string errorMessage)
        {
            errorMessage = null;
            using (NamedPipeClient pipeClient = new NamedPipeClient(NamedPipeClient.GetPipeNameFromPath(enlistmentRoot)))
            {
                int timeout = unattended ? 300000 : 10000;
                if (!pipeClient.Connect(timeout))
                {
                    errorMessage = "Unable to mount because the RGFS.Mount process is not responding.";
                    return false;
                }

                while (true)
                {
                    string response = string.Empty;
                    try
                    {
                        pipeClient.SendRequest(NamedPipeMessages.GetStatus.Request);
                        response = pipeClient.ReadRawResponse();
                        NamedPipeMessages.GetStatus.Response getStatusResponse =
                            NamedPipeMessages.GetStatus.Response.FromJson(response);

                        if (getStatusResponse.MountStatus == NamedPipeMessages.GetStatus.Ready)
                        {
                            return true;
                        }
                        else if (getStatusResponse.MountStatus == NamedPipeMessages.GetStatus.MountFailed)
                        {
                            errorMessage = string.Format("Failed to mount at {0}", enlistmentRoot);
                            return false;
                        }
                        else
                        {
                            Thread.Sleep(500);
                        }
                    }
                    catch (BrokenPipeException e)
                    {
                        errorMessage = string.Format("Could not connect to RGFS.Mount: {0}", e);
                        return false;
                    }
                    catch (JsonReaderException e)
                    {
                        errorMessage = string.Format("Failed to parse response from RGFS.Mount.\n {0}", e);
                        return false;
                    }
                }
            }
        }
        
        public bool TryCreateEnlistmentFolders()
        {
            try
            {
                Directory.CreateDirectory(this.EnlistmentRoot);

                // The following permissions are typically present on deskop and missing on Server
                //                  
                //   ACCESS_ALLOWED_ACE_TYPE: NT AUTHORITY\Authenticated Users
                //          [OBJECT_INHERIT_ACE]
                //          [CONTAINER_INHERIT_ACE]
                //          [INHERIT_ONLY_ACE]
                //        DELETE
                //        GENERIC_EXECUTE
                //        GENERIC_WRITE
                //        GENERIC_READ
                DirectorySecurity rootSecurity = Directory.GetAccessControl(this.EnlistmentRoot);
                AccessRule authenticatedUsersAccessRule = rootSecurity.AccessRuleFactory(
                    new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                    unchecked((int)(NativeMethods.FileAccess.DELETE | NativeMethods.FileAccess.GENERIC_EXECUTE | NativeMethods.FileAccess.GENERIC_WRITE | NativeMethods.FileAccess.GENERIC_READ)),
                    true,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                // The return type of the AccessRuleFactory method is the base class, AccessRule, but the return value can be cast safely to the derived class.
                // https://msdn.microsoft.com/en-us/library/system.security.accesscontrol.filesystemsecurity.accessrulefactory(v=vs.110).aspx
                rootSecurity.AddAccessRule((FileSystemAccessRule)authenticatedUsersAccessRule);
                Directory.SetAccessControl(this.EnlistmentRoot, rootSecurity);

                Directory.CreateDirectory(this.WorkingDirectoryRoot);
                this.CreateHiddenDirectory(this.DotRGFSRoot);
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }

        public bool TryConfigureAlternate(out string errorMessage)
        {
            try
            {
                if (!Directory.Exists(this.GitObjectsRoot))
                {
                    Directory.CreateDirectory(this.GitObjectsRoot);
                    Directory.CreateDirectory(this.GitPackRoot);
                }

                File.WriteAllText(
                    Path.Combine(this.WorkingDirectoryRoot, RGFSConstants.DotGit.Objects.Info.Alternates),
                    @"..\..\..\" + RGFSConstants.DotRGFS.GitObjectCachePath);
            }
            catch (IOException e)
            {
                errorMessage = e.Message;
                return false;
            }

            errorMessage = null;
            return true;
        }
        
        /// <summary>
        /// Creates a hidden directory @ the given path.
        /// If directory already exists, hides it.
        /// </summary>
        /// <param name="path">Path to desired hidden directory</param>
        private void CreateHiddenDirectory(string path)
        {
            DirectoryInfo dir = Directory.CreateDirectory(path);
            dir.Attributes = FileAttributes.Hidden;
        }
    }
}
