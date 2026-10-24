#!/usr/bin/env bash
# Verbatim body of the "Test C# tools" step in .github/workflows/pages.yml at base 0cba945b, run under bash as GitHub Actions does.
# (Its E2eRunner line deselects 16 toolchain test cases across 15 names; the unfiltered full slnx run executes all 190 E2eRunner tests, 0 skipped.)
set -eo pipefail
          dotnet test tools/Coverage.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse
          dotnet test tools/BundleElements.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse
          dotnet test tools/WorkerAotGuard.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse
          toolchain="TypeCheckTests.EverySuiteAndSharedModuleTypeChecks TypeCheckTests.ASuiteReadingADeletedAccessorFailsTheTypeCheck
            SuiteProcessTests.EnvironmentIsScrubbed SuiteProcessTests.HangIsTimedOutAndTheWholeTreeIsKilled
            SuiteProcessTests.FailingAssertionFailsTheFile SuiteProcessTests.CloseFindingInAfterHookFailsTheFile
            SuiteProcessTests.FileWithoutTestsIsNoTests SuiteProcessTests.CrashFailsTheFile
            SuiteProcessTests.PassingFileIsPassAndCountsDriverSignals SuiteProcessTests.ImportErrorFailsWithNoTests
            IdentityTests.UnchangedRunPasses IdentityTests.BundleChangedMidRunAbortsWithIdentityChanged
            IdentityTests.BundleChangedAndRestoredIsStillIdentityChanged IdentityTests.SuiteSourceChangedMidRunIsIdentityChanged
            IdentityTests.EndRehashCatchesAChangeTheWatcherMissed"
          filter=""
          for test in $toolchain; do filter="$filter&FullyQualifiedName!=CuriousContraptions.E2eRunner.Tests.$test"; done
          dotnet test tools/E2eRunner.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse --filter "${filter#&}"
