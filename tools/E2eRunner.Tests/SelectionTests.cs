using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>AC3: the Tier 2 selection, table-driven over the real repository with base/head overrides.</summary>
public sealed class SelectionTests
{
    private static string? Head(string path) { var full = Path.Combine(TestRepository.Root, path); return File.Exists(full) ? File.ReadAllText(full) : null; }

    private static Selection Select(string[] paths, Func<string, string?>? baseReader = null, Func<string, string?>? headOverride = null) =>
        Selector.Select(paths, new ImpactContext(TestRepository.Root, baseReader ?? Head, headOverride));

    private static string[] Names(Selection selection) => [.. selection.Suites.Select(suite => SuiteCatalogue.FileName(suite.Suite)).Order(StringComparer.Ordinal)];
    private static readonly string[] Smoke = ["engine-core-2a1", "engine-core-2b1"];

    [Fact]
    public void CounterDeclarationSelectsOwnIntegrationConnectedAndSmoke()
    {
        var selection = Select(["elements/Counter/CounterElementDeclaration.cs"]);
        Assert.Equal(SelectionMode.Selected, selection.Mode);
        Assert.Equal(["cat-002", "cat-005", "cat-015a", "cat-017", "cat-020", "cat-023b", "cat-037", "cat-051", "cat-052",
            "engine-core-2a1", "engine-core-2b1", "palette-placement"], Names(selection));
        var lines = selection.Suites.Select(suite => suite.ReasonLine).ToArray();
        Assert.Contains("cat-020\town: elements/Counter/CounterElementDeclaration.cs changes counter; integration: places counter", lines);
        Assert.Contains("cat-052\tconnected: counter.signal_in Signal pressure_plate.signal_out", lines);
        Assert.Contains("cat-005\tconnected: counter.power_in Electrical battery.power_out", lines);
        Assert.Contains("palette-placement\tintegration: places every palette part (counter)", lines);
        Assert.Contains("engine-core-2b1\tsmoke pair", lines);
        Assert.False(selection.Tier3Required);
    }

    [Theory]
    [InlineData("parts/scenes/ball.tscn", "ball")]
    [InlineData("parts/catalog/ball.tres", "ball")]
    [InlineData("elements/SignalLamp/SignalLampElementDeclaration.cs", "lamp")]
    [InlineData("parts/catalog/basket.tres", "basket")]
    public void ElementWithoutAPrimarySuiteIsATypedError(string path, string part)
    {
        var refusal = Assert.Throws<RunnerException>(() => Select([path]));
        Assert.Equal(RunnerError.NoPrimarySuite, refusal.Error);
        Assert.Contains($"changes {part}", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PartPreplacedByADeclaredLevelSelectsThatLevelsSuites()
    {
        // domino_effect's locked ball is a Bowling ball: changing it must select the suites that declare that level (B1, spec D6).
        var selection = Select(["parts/catalog/bowling.tres"]);
        Assert.Equal(SelectionMode.Selected, selection.Mode);
        var lines = selection.Suites.Select(suite => suite.ReasonLine).ToArray();
        Assert.Contains("cat-023b\tintegration: level domino_effect places bowling", lines);
        Assert.Contains("clock-master\tintegration: level domino_effect places bowling", lines);
        Assert.Contains("cat-014\town: parts/catalog/bowling.tres changes bowling; integration: places bowling", lines);
    }

    [Fact]
    public void SharedSceneMapsToEveryReferencingPart()
    {
        var context = new ImpactContext(TestRepository.Root, Head);
        Assert.Equal(["ball", "balloon", "tennis"], context.ScenePart("parts/scenes/ball.tscn").Select(part => part.Value));
    }

    [Fact]
    public void OneChangedLevelSelectsTheSuitesDeclaringIt()
    {
        var current = TestRepository.Read(ImpactContext.LevelsPath);
        var edited = current.Replace("\"id\": \"clear_pipe\"", "\"id\": \"clear_pipe\", \"edited\": true", StringComparison.Ordinal);
        Assert.NotEqual(current, edited);
        var selection = Select([ImpactContext.LevelsPath], headOverride: path => path == ImpactContext.LevelsPath ? edited : null);
        Assert.Equal(["cat-048b", .. Smoke], Names(selection));
        Assert.Contains("cat-048b\tlevel: declares clear_pipe", selection.Suites.Select(suite => suite.ReasonLine));
    }

    [Fact]
    public void GeneratedAtOnlyBundleChangeHasNoBrowserImpact()
    {
        var current = TestRepository.Read(ImpactContext.BundlePath);
        var edited = System.Text.RegularExpressions.Regex.Replace(current, "\"GeneratedAt\": \"[^\"]+\"", "\"GeneratedAt\": \"2030-01-01T00:00:00Z\"");
        Assert.NotEqual(current, edited);
        var selection = Select([ImpactContext.BundlePath], headOverride: path => path == ImpactContext.BundlePath ? edited : null);
        Assert.Equal(Smoke, Names(selection));
        Assert.Equal(ImpactKind.NoBrowserImpact, Assert.Single(selection.Paths).Kind);
    }

    [Fact]
    public void ChangedBundleElementSelectsThatElement()
    {
        var current = TestRepository.Read(ImpactContext.BundlePath);
        var edited = current.Replace("\"DisplayNameKey\": \"part_counter\"", "\"DisplayNameKey\": \"part_counter_v2\"", StringComparison.Ordinal);
        Assert.NotEqual(current, edited);
        var selection = Select([ImpactContext.BundlePath], headOverride: path => path == ImpactContext.BundlePath ? edited : null);
        Assert.Equal("counter", Assert.Single(selection.Paths).Detail);
        Assert.Contains("cat-020", Names(selection));
    }

    [Fact]
    public void UnparsableBundleIsFull() =>
        Assert.Equal(SelectionMode.Full, Select([ImpactContext.BundlePath], headOverride: path => path == ImpactContext.BundlePath ? "{" : null).Mode);

    [Fact]
    public void SharedConsoleModuleSelectsItsImporters()
    {
        var selection = Select(["tools/workshop-console.mjs"]);
        Assert.Equal(["anim-1a", "capacity-9r", "cat-002", "cat-014", "cat-017", "cat-020", "cat-023a", "cat-023b", "cat-030", "cat-037", "cat-048a",
            "cat-048b", "cat-049", "cat-050", "cat-051", "cat-051-own-supply", "cat-052", "engine-core-2a1", "engine-core-2b1", "engine-core-2b2", "engine-core-2b3",
            "engine-core-2b4", "engine-core-2c"], Names(selection));
    }

    private static readonly Dictionary<string, FullReason> FullPaths = new(StringComparer.Ordinal)
    {
        ["tools/e2e/workshop-driver.ts"] = FullReason.SharedDriverInfrastructure,
        ["tools/e2e/workshop-wire.ts"] = FullReason.SharedDriverInfrastructure,
        ["tools/workshop-close-findings.mjs"] = FullReason.SharedDriverInfrastructure,
        ["engine/gpu/WorkshopSimulation.cs"] = FullReason.MaterialEngine,
        ["engine/gpu/NewSolverStage.cs"] = FullReason.MaterialEngine,
        ["CuriousContraptions.Simulation/Program.cs"] = FullReason.SharedRuntime,
        ["CuriousContraptions.web/wwwroot/workshop-client.js"] = FullReason.SharedRuntime,
        ["ui/Workshop.cs"] = FullReason.AllInteraction,
        ["ui/Workshop.cs.uid"] = FullReason.AllInteraction,
        ["ui/WorkshopNewPanel.cs"] = FullReason.Unknown,
        ["notes.txt"] = FullReason.Unknown,
        ["tools/e2e/cat-099.test.ts"] = FullReason.Unknown,
        ["tools/E2eRunner/Scheduler.cs"] = FullReason.RunnerItself,
        ["elements/Counter/Counter.csproj"] = FullReason.Build,
        ["tools/Preview/Program.cs"] = FullReason.SharedDriverInfrastructure,
    };

    public static TheoryData<string> FullPathCases => [.. FullPaths.Keys];

    [Theory]
    [MemberData(nameof(FullPathCases))]
    public void SharedOrUnknownChangeSelectsTheFullRun(string path)
    {
        var reason = FullPaths[path];
        var selection = Select([path]);
        Assert.Equal(SelectionMode.Full, selection.Mode);
        Assert.True(selection.Tier3Required);
        Assert.Equal(reason, Assert.Single(selection.Paths).Full);
        Assert.Equal(SuiteCatalogue.All.Count, selection.Suites.Count);
        Assert.All(selection.Suites, suite => Assert.StartsWith($"full: {path} ({reason}", suite.Reasons[0], StringComparison.Ordinal));
    }

    [Fact]
    public void WorkshopAreaFileSelectsTheAreaSuitesAndSmoke()
    {
        var selection = Select(["ui/WorkshopBenchPlacement.cs", "engine/PartRegistry.cs"]);
        Assert.Equal(["engine-core-2a1", "engine-core-2b1", "palette-placement"], Names(selection));
        Assert.Contains("palette-placement\tarea: Palette (engine/PartRegistry.cs); area: BenchPlacement (ui/WorkshopBenchPlacement.cs)",
            selection.Suites.Select(suite => suite.ReasonLine));
    }

    [Fact]
    public void DocsOnlySelectsTheSmokePair()
    {
        var selection = Select(["docs/delivery-workflow.md", "README.md", "_bmad-output/planning-artifacts/epics.md", "tools/e2e/README.md"]);
        Assert.Equal(Smoke, Names(selection));
        Assert.All(selection.Paths, path => Assert.Equal(ImpactKind.NoBrowserImpact, path.Kind));
    }

    [Fact]
    public void EmptySetSelectsTheSmokePair()
    {
        var selection = Select([]);
        Assert.Equal(SelectionMode.Selected, selection.Mode);
        Assert.Equal(Smoke, Names(selection));
    }

    [Fact]
    public void RenameOutOfElementsIsFull()
    {
        var selection = Select(["elements/Counter/CounterElementDeclaration.cs", "misc/CounterElementDeclaration.cs"]);
        Assert.Equal(SelectionMode.Full, selection.Mode);
        Assert.Contains(selection.Paths, path => path.Path == "misc/CounterElementDeclaration.cs" && path.Full == FullReason.Unknown);
    }

    [Fact]
    public void ChangedSuiteSelectsItself() =>
        Assert.Equal(["cat-049", .. Smoke], Names(Select(["tools/e2e/cat-049.test.ts"])));

    [Theory]
    [InlineData("parts/catalog/springboard.tres", "spring")]
    [InlineData("parts/catalog/counter.tres", "counter")]
    [InlineData("parts/scenes/springboard.tscn", "spring")]
    public void CatalogueAndScenesMapByContent(string path, string part) =>
        Assert.Equal(part, Assert.Single(Select([path]).Paths).Detail);

    [Fact]
    public void RuleTableResolvesEveryTrackedPathWithoutOverlap()
    {
        var failures = new List<string>();
        foreach (var path in TestRepository.ListFiles())
        {
            try { ImpactRules.Resolve(path); }
            catch (InvalidOperationException error) { failures.Add(error.Message); }
        }
        Assert.Empty(failures);
    }

    [Fact]
    public void AreaTableMatchesEngineUiAndScenes()
    {
        var files = TestRepository.ListFiles("engine", "ui", "scenes").Where(path => !path.StartsWith("engine/gpu/", StringComparison.Ordinal)).ToArray();
        Assert.Equal("", string.Join(", ", files.Where(path => ImpactRules.AreaOf(path) is null)));
        Assert.Equal("", string.Join(", ", ImpactRules.AreaFiles.Keys.Where(path => !files.Contains(path))));
    }

    [Theory]
    [InlineData("/Users/x/repo/README.md")]
    [InlineData("docs/../README.md")]
    [InlineData("docs\\x.md")]
    [InlineData("Tools/e2e/cat-049.test.ts")]
    [InlineData("tools/e2e/CAT-049.test.ts")]
    [InlineData("docs//x.md")]
    public void ChangedListRejectsNonRepositoryOrNonCanonicalPaths(string path)
    {
        var refusal = Assert.Throws<RunnerException>(() => ChangeSet.Validate(TestRepository.Root, path));
        Assert.Equal(RunnerError.InvalidChangedPath, refusal.Error);
    }

    [Fact]
    public void ChangedListAcceptsCanonicalAndDeletedPaths()
    {
        Assert.Equal("tools/e2e/cat-049.test.ts", ChangeSet.Validate(TestRepository.Root, "tools/e2e/cat-049.test.ts"));
        Assert.Equal("tools/e2e/gone.test.ts", ChangeSet.Validate(TestRepository.Root, "tools/e2e/gone.test.ts"));
    }
}
