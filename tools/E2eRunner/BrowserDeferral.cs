namespace CuriousContraptions.E2eRunner;

/// <summary>The gate that re-enables a deferred browser test.</summary>
internal enum ReEnableGate { PostCatalogueBrowserPhase = 1 }

/// <summary>What runs a deferred test once it is re-enabled.</summary>
internal enum DeferredRunner { E2eRunnerTests = 1, NodeTest, ChromeSuites }

/// <summary>One deferred test: an E2eRunner.Tests method (`Class.Method`), a node test file or a suite glob (repository paths).</summary>
internal sealed record DeferredTest(DeferredRunner Runner, string Test, string Reason, ReEnableGate Gate, string Owner);

/// <summary>The one list of browser/E2E tests disabled until the post-catalogue browser phase (AGENTS.md browser exception). Local verification builds
/// its E2eRunner.Tests filter from this list (`deferred-filter`), and BrowserDeferralTests checks the list, the filter and that every
/// other test file runs.</summary>
internal static class BrowserDeferral
{
    private const string Owner = "Aidan Morgan (project owner)";
    private const string TypeScript = "Runs tsc 6.0.3 with @types/node 26.0.1 over the Chrome suites, which import Playwright types and flatbuffers: "
        + "the browser-suite TypeScript toolchain, which no required run provisions.";
    private const string Tsx = "Runs runner fixtures through the pinned tsx 4.23.15 loader the Chrome suites use: browser-suite toolchain, "
        + "which no required run provisions.";
    private const string Engine = "Runs the runner engine against a preview with a fixture suite on the pinned tsx 4.23.15 loader: "
        + "browser-suite toolchain, which no required run provisions.";

    public static readonly IReadOnlyList<DeferredTest> Tests =
    [
        new(DeferredRunner.E2eRunnerTests, "TypeCheckTests.EverySuiteAndSharedModuleTypeChecks", TypeScript, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "TypeCheckTests.ASuiteReadingADeletedAccessorFailsTheTypeCheck", TypeScript, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.EnvironmentIsScrubbed", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.HangIsTimedOutAndTheWholeTreeIsKilled", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.FailingAssertionFailsTheFile", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.CloseFindingInAfterHookFailsTheFile", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.FileWithoutTestsIsNoTests", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.CrashFailsTheFile", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.PassingFileIsPassAndCountsDriverSignals", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "SuiteProcessTests.ImportErrorFailsWithNoTests", Tsx, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "IdentityTests.UnchangedRunPasses", Engine, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "IdentityTests.BundleChangedMidRunAbortsWithIdentityChanged", Engine, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "IdentityTests.BundleChangedAndRestoredIsStillIdentityChanged", Engine, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "IdentityTests.SuiteSourceChangedMidRunIsIdentityChanged", Engine, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.E2eRunnerTests, "IdentityTests.EndRehashCatchesAChangeTheWatcherMissed", Engine, ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.NodeTest, "tools/e2e/unit/run-reset-snapshot.test.ts",
            "Browserless checks of the Chrome suites' Run/Reset snapshot comparator; needs the tsx 4.23.15 loader and flatbuffers, the "
            + "browser-suite toolchain, which no required run provisions.", ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.NodeTest, "tools/workshop-close-findings.test.ts",
            "Checks the Chrome suites' session close-finding module (tools/workshop-close-findings.ts, used by WorkshopDriver) through "
            + "node:test's own hook reporting, which differs by Node version (22.23.4 does not fail a file for a suite after-hook "
            + "error): browser-suite toolchain, not game runtime code.", ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.NodeTest, "tools/workshop-console.test.ts",
            "Checks the Chrome suites' console boundary (tools/workshop-console.ts, which classifies browser console lines): "
            + "browser-suite toolchain, not game runtime code.", ReEnableGate.PostCatalogueBrowserPhase, Owner),
        new(DeferredRunner.ChromeSuites, "tools/e2e/*.test.ts",
            "Chrome/Playwright UI suites, run by tools/E2eRunner against the production preview.", ReEnableGate.PostCatalogueBrowserPhase, Owner),
    ];

    /// <summary>The E2eRunner.Tests filter that excludes exactly the deferred E2eRunner.Tests methods.</summary>
    public static string LocalFilter() => string.Join('&', Tests.Where(test => test.Runner == DeferredRunner.E2eRunnerTests)
        .Select(test => $"FullyQualifiedName!=CuriousContraptions.E2eRunner.Tests.{test.Test}"));
}
