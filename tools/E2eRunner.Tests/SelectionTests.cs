using CuriousContraptions.Abstractions;
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
            "cat-052-lamp", "cat-052-two-plates", "engine-core-2a1", "engine-core-2b1", "palette-placement"], Names(selection));
        Assert.Contains("cat-052-lamp\tintegration: places counter", selection.Suites.Select(suite => suite.ReasonLine));
        var lines = selection.Suites.Select(suite => suite.ReasonLine).ToArray();
        Assert.Contains("cat-020\town: elements/Counter/CounterElementDeclaration.cs changes counter; integration: places counter", lines);
        Assert.Contains("cat-052\tconnected: counter.ActivationIn Signal pressure_plate.ActivationOut", lines);
        Assert.Contains("cat-005\tconnected: counter.PowerIn Electrical battery.Supply", lines);
        Assert.Contains("palette-placement\tintegration: places every palette part (counter)", lines);
        Assert.Contains("engine-core-2b1\tsmoke pair", lines);
        Assert.False(selection.Tier3Required);
    }

    [Theory]
    [InlineData("parts/scenes/ball.tscn", "ball")]
    [InlineData("parts/catalog/ball.tres", "ball")]
    [InlineData("elements/SignalLamp/SignalLampElementDeclaration.cs", "lamp")]
    [InlineData("parts/catalog/basket.tres", "basket")]
    // PKG-3: the moved scenes map to their parts by content; their primary suites are cat-022 (Story 9.5a) and cat-063 (C-Switch).
    [InlineData("elements/DelayBox/delay.tscn", "delay")]
    [InlineData("elements/ImpactSwitch/switch.tscn", "switch")]
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
        Assert.Equal(["cat-048b", .. Smoke, "tube-snapping"], Names(selection));
        Assert.Contains("cat-048b\tlevel: declares clear_pipe", selection.Suites.Select(suite => suite.ReasonLine));
    }

    [Fact]
    public void TheGeneratedBundleIsReadWithNamedDomainsAndDirections()
    {
        var bundle = ImpactContext.BundleElements(TestRepository.Read(ImpactContext.BundlePath));
        Assert.NotNull(bundle);
        var counter = bundle.Single(element => element.Part == PartId.Counter);
        Assert.Equal([new("ActivationIn", SimulationDomain.Signal, PortDirection.Input), new("ActivationOut", SimulationDomain.Signal, PortDirection.Output),
            new("PowerIn", SimulationDomain.Electrical, PortDirection.Input), new ImpactContext.BundlePort("Supply", SimulationDomain.Electrical, PortDirection.Output)],
            counter.Ports);
        Assert.Equal("res://parts/scenes/counter.tscn", counter.ScenePath);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"Elements\": [{\"Metadata\": {\"Id\": \"counter\", \"ScenePath\": \"res://x.tscn\"}, \"Ports\": [{\"PortId\": \"PowerIn\", \"Domain\": 2, \"Direction\": \"Input\"}]}]}")]
    [InlineData("{\"Elements\": [{\"Metadata\": {\"Id\": \"counter\", \"ScenePath\": \"res://x.tscn\"}, \"Ports\": [{\"PortId\": \"PowerIn\", \"Domain\": \"Voltage\", \"Direction\": \"Input\"}]}]}")]
    public void AnElementChangeWithAnUnreadableBundleIsFull(string bundle)
    {
        var selection = Select(["elements/Counter/CounterElementDeclaration.cs"], headOverride: path => path == ImpactContext.BundlePath ? bundle : null);
        Assert.Equal(SelectionMode.Full, selection.Mode);
        Assert.Contains("is missing or does not parse", Assert.Single(selection.Paths).Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedConsoleModuleSelectsItsImporters()
    {
        var selection = Select(["tools/workshop-console.mjs"]);
        Assert.Equal(["anim-1a", "capacity-9r", "capacity-9r5b", "cat-002", "cat-014", "cat-017", "cat-020", "cat-023a", "cat-023b", "cat-030", "cat-037", "cat-048a",
            "cat-048b", "cat-049", "cat-050", "cat-050-joined", "cat-051", "cat-051-own-supply", "cat-052", "cat-052-lamp", "cat-052-two-plates", "engine-core-2a1", "engine-core-2b1", "engine-core-2b2", "engine-core-2b3",
            "engine-core-2b4", "engine-core-2c", "pkg-1", "pkg-3", "tube-snapping"], Names(selection));
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
        // Story 9R.6: tube-snapping also exercises bench placement (the typed mouth snap, engine/TubeMouthSnap.cs).
        // PKG-1 and PKG-3: pkg-1 and pkg-3 place their packaged parts from the palette onto the bench.
        Assert.Equal(["engine-core-2a1", "engine-core-2b1", "palette-placement", "pkg-1", "pkg-3", "tube-snapping"], Names(selection));
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
    [InlineData("parts/scenes/clock.tscn", "clock")]
    [InlineData("elements/Springboard/springboard.tscn", "spring")]
    [InlineData("elements/Funnel/funnel.tscn", "funnel")]
    [InlineData("elements/Pipe/pipe.tscn", "pipe")]
    [InlineData("elements/PipeBend45/pipe_bend_45.tscn", "pipe_bend_45")]
    [InlineData("elements/PipeBend90/pipe_bend_90.tscn", "pipe_bend_90")]
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
