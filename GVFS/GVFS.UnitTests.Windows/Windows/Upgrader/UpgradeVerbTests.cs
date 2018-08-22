﻿using GVFS.CommandLine;
using GVFS.Common;
using GVFS.Tests.Should;
using GVFS.UnitTests.Category;
using GVFS.UnitTests.Windows.Mock.Upgrader;
using NUnit.Framework;
using System.Collections.Generic;

namespace GVFS.UnitTests.Windows.Upgrader
{
    [TestFixture]
    public class UpgradeVerbTests : UpgradeTests
    {
        private MockProcessLauncher ProcessWrapper { get; set; }
        private UpgradeVerb UpgradeVerb { get; set; }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            this.ProcessWrapper = new MockProcessLauncher(exitCode: 0, hasExited: true, startResult: true);
            this.UpgradeVerb = new UpgradeVerb(
                this.Upgrader,
                this.Tracer,
                this.PrerunChecker,
                this.ProcessWrapper,
                this.Output);
            this.UpgradeVerb.Confirmed = false;
        }

        [TestCase]
        public void UpgradeAvailabilityReporting()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.Upgrader.PretendNewReleaseAvailableAtRemote(
                        upgradeVersion: NewerThanLocalVersion,
                        remoteRing: ProductUpgrader.RingType.Slow);
                },
                expectedReturn: ReturnCode.Success,
                expectedOutput: new List<string>
                {
                    "New GVFS version available: " + NewerThanLocalVersion,
                    "Run gvfs upgrade --confirm to install it"
                },
                expectedErrors: null);
        }

        [TestCase]
        public void DowngradePrevention()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.Upgrader.PretendNewReleaseAvailableAtRemote(
                        upgradeVersion: OlderThanLocalVersion,
                        remoteRing: ProductUpgrader.RingType.Slow);
                },
                expectedReturn: ReturnCode.Success,
                expectedOutput: new List<string>
                {
                    "Checking for GVFS upgrades...Succeeded",
                    "Great news, you're all caught up on upgrades in the Slow ring!"
                },
                expectedErrors: null);
        }

        [TestCase]
        public void LaunchInstaller()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.UpgradeVerb.Confirmed = true;
                },
                expectedReturn: ReturnCode.Success,
                expectedOutput: new List<string>
                {
                    "New GVFS version available: " + NewerThanLocalVersion,
                    "Launching upgrade tool...Succeeded"
                },
                expectedErrors:null);

            this.ProcessWrapper.IsLaunched.ShouldBeTrue();
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void InvalidLocalRing()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.Upgrader.LocalRingConfig = ProductUpgrader.RingType.Invalid;
                },
                expectedReturn: ReturnCode.GenericError,
                expectedOutput: new List<string>
                {
                    "Checking for GVFS upgrades...Failed",
                    "Invalid upgrade ring type(Invalid) specified in Git config."
                },
                expectedErrors: new List<string>
                {
                    "Invalid upgrade ring type(Invalid) specified in Git config."
                });
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void FetchReleaseInfo()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.Upgrader.SetFailOnAction(MockProductUpgrader.ActionType.FetchReleaseInfo);
                },
                expectedReturn: ReturnCode.GenericError,
                expectedOutput: new List<string>
                {
                    "Error fetching upgrade release info."
                },
                expectedErrors: new List<string>
                {
                    "Upgrade checks failed. Error fetching upgrade release info"
                });
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void CopyTools()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.Upgrader.SetFailOnAction(MockProductUpgrader.ActionType.CopyTools);
                    this.UpgradeVerb.Confirmed = true;
                },
                expectedReturn: ReturnCode.GenericError,
                expectedOutput: new List<string>
                {
                    "Could not launch installer. Unable to copy upgrader tools"
                },
                expectedErrors: new List<string>
                {
                    "Could not launch installer. Unable to copy upgrader tools"
                });
        }

        [TestCase]
        public void ProjFSPreCheck()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.PrerunChecker.SetReturnFalseOnCheck(MockInstallerPrerunChecker.FailOnCheckType.ProjFSEnabled);
                },
                expectedReturn: ReturnCode.GenericError,
                expectedOutput: new List<string>
                {
                    "Unsupported ProjFS configuration.",
                    "Check your team's documentation for how to upgrade."
                },
                expectedErrors: new List<string>
                {
                    "Unsupported ProjFS configuration."
                });
        }

        [TestCase]
        public void ElevatedRunPreCheck()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.PrerunChecker.SetReturnFalseOnCheck(MockInstallerPrerunChecker.FailOnCheckType.IsElevated);
                },
                expectedReturn: ReturnCode.GenericError,
                expectedOutput: new List<string>
                {
                    "The installer needs to be run from an elevated command prompt.",
                    "Please open an elevated (administrator) command prompt and run gvfs upgrade again."
                },
                expectedErrors: new List<string>
                {
                    "The installer needs to be run from an elevated command prompt."
                });
        }

        [TestCase]
        public void UnAttendedModePreCheck()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.PrerunChecker.SetReturnTrueOnCheck(MockInstallerPrerunChecker.FailOnCheckType.UnattendedMode);
                },
                expectedReturn: ReturnCode.GenericError,
                expectedOutput: new List<string>
                {
                    "Cannot run upgrade, when GVFS is running in unattended mode."
                },
                expectedErrors: new List<string>
                {
                    "Cannot run upgrade, when GVFS is running in unattended mode."
                });
        }

        [TestCase]
        public void DeveloperMachinePreCheck()
        {
            this.ConfigureRunAndVerify(
                configure: () =>
                {
                    this.PrerunChecker.SetReturnTrueOnCheck(MockInstallerPrerunChecker.FailOnCheckType.IsDevelopmentVersion);
                },
                expectedReturn: ReturnCode.GenericError,
                expectedOutput: new List<string>
                {
                    "Cannot run upgrade when development version of GVFS is installed."
                },
                expectedErrors: new List<string>
                {
                    "Cannot run upgrade when development version of GVFS is installed."
                });
        }

        protected override void RunUpgrade()
        {
            try
            {
                this.UpgradeVerb.Execute();
            }
            catch (GVFSVerb.VerbAbortedException)
            {
                // ignore. exceptions are expected while simulating some failures.
            }
        }

        protected override ReturnCode ExitCode()
        {
            return this.UpgradeVerb.ReturnCode;
        }
    }
}