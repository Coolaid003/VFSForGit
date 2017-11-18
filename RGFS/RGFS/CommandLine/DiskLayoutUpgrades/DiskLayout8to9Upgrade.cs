﻿using RGFS.Common;
using RGFS.Common.Tracing;
using Microsoft.Isam.Esent;
using Microsoft.Isam.Esent.Collections.Generic;
using System.Collections.Generic;
using System.IO;

namespace RGFS.CommandLine.DiskLayoutUpgrades
{
    public class DiskLayout8to9Upgrade : DiskLayoutUpgrade
    {
        protected override int SourceLayoutVersion
        {
            get { return 8; }
        }

        /// <summary>
        /// Rewrites ESENT RepoMetadata DB to flat JSON file
        /// </summary>
        public override bool TryUpgrade(ITracer tracer, string enlistmentRoot)
        {
            string dotRGFSRoot = Path.Combine(enlistmentRoot, RGFSConstants.DotRGFS.Root);
            if (!this.UpdateRepoMetadata(tracer, dotRGFSRoot))
            {
                return false;
            }

            if (!this.TryIncrementDiskLayoutVersion(tracer, enlistmentRoot, this))
            {
                return false;
            }

            return true;
        }
        
        private bool UpdateRepoMetadata(ITracer tracer, string dotRGFSRoot)
        {
            string esentRepoMetadata = Path.Combine(dotRGFSRoot, EsentRepoMetadataName);
            if (Directory.Exists(esentRepoMetadata))
            {
                try
                {
                    using (PersistentDictionary<string, string> oldMetadata = new PersistentDictionary<string, string>(esentRepoMetadata))
                    {
                        string error;
                        if (!RepoMetadata.TryInitialize(tracer, dotRGFSRoot, out error))
                        {
                            tracer.RelatedError("Could not initialize RepoMetadata: " + error);
                            return false;
                        }

                        foreach (KeyValuePair<string, string> kvp in oldMetadata)
                        {
                            tracer.RelatedInfo("Copying ESENT entry: {0} = {1}", kvp.Key, kvp.Value);
                            RepoMetadata.Instance.SetEntry(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch (IOException ex)
                {
                    tracer.RelatedError("Could not write to new repo metadata: " + ex.Message);
                    return false;
                }
                catch (EsentException ex)
                {
                    tracer.RelatedError("RepoMetadata appears to be from an older version of RGFS and corrupted: " + ex.Message);
                    return false;
                }

                string backupName;
                if (this.TryRenameFolderForDelete(tracer, esentRepoMetadata, out backupName))
                {
                    // If this fails, we leave behind cruft, but there's no harm because we renamed.
                    this.TryDeleteFolder(tracer, backupName);
                    return true;
                }
                else
                {
                    // To avoid double upgrading, we should rollback if we can't rename the old data
                    this.TryDeleteFile(tracer, RepoMetadata.Instance.DataFilePath);
                    return false;
                }
            }

            return true;
        }
    }
}