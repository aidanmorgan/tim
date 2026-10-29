using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

public sealed class FastSelectionTests
{
    private const string CounterProject = "elements/Counter/Counter.csproj";
    private const string GateProject = "elements/PoweredGate/PoweredGate.csproj";
    private static string? Current(string path) => File.Exists(Path.Combine(TestRepository.Root, path)) ? TestRepository.Read(path) : null;
    private static ImpactContext Context(string path, string? before, string? after) =>
        new(TestRepository.Root, candidate => candidate == path ? before : Current(candidate),
            candidate => candidate == path ? after : Current(candidate));

    [Fact]
    public void HistoricalMissingHeadIsNotResurrectedFromTheWorkingTree()
    {
        var context = Context(CounterProject, Current(CounterProject), null);
        Assert.Null(context.ReadHead(CounterProject));
        Assert.NotNull(context.ReadBase(CounterProject));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddedAndRemovedPackageReferencesSelectBothEnds(bool removing)
    {
        var plain = TestRepository.Read(CounterProject);
        var linked = plain.Replace("</ItemGroup>", "<ProjectReference Include=\"../PoweredGate/PoweredGate.csproj\" /></ItemGroup>", StringComparison.Ordinal);
        var result = BuildImpact.Parts(CounterProject, Context(CounterProject, removing ? linked : plain, removing ? plain : linked));
        Assert.NotNull(result);
        Assert.Contains(new PartId("counter"), result);
        Assert.Contains(new PartId("powered_gate"), result);
    }

    [Theory]
    [InlineData("../../unknown/unknown.csproj")]
    [InlineData("../PoweredGate/missing.csproj")]
    [InlineData("$(InjectedProject)")]
    public void UnsupportedReferenceCannotNarrow(string reference)
    {
        var plain = TestRepository.Read(CounterProject);
        var changed = plain.Replace("</ItemGroup>", $"<ProjectReference Include=\"{reference}\" /></ItemGroup>", StringComparison.Ordinal);
        Assert.Null(BuildImpact.Parts(CounterProject, Context(CounterProject, plain, changed)));
        Assert.Null(BuildImpact.Parts(CounterProject, Context(CounterProject, changed, plain)));
    }

    [Fact]
    public void MissingAndMalformedProjectsCannotNarrow()
    {
        Assert.Null(BuildImpact.Parts(CounterProject, Context(CounterProject, null, null)));
        Assert.Null(BuildImpact.Parts(CounterProject, Context(CounterProject, "<Project", Current(CounterProject))));
    }

    [Fact]
    public void SharedBuildTargetsCannotNarrow()
    {
        var plain = TestRepository.Read(CounterProject);
        var changed = plain.Replace("</Project>", "<Target Name=\"BeforeBuild\" /></Project>", StringComparison.Ordinal);
        Assert.Null(BuildImpact.Parts(CounterProject, Context(CounterProject, plain, changed)));
    }

    [Fact]
    public void SolutionMembershipUsesBothSidesAndRejectsUnknownProjects()
    {
        const string empty = "<Solution><Folder Name=\"/elements/\" /></Solution>";
        var known = $"<Solution><Folder Name=\"/elements/\"><Project Path=\"{GateProject}\" /></Folder></Solution>";
        Assert.Contains(new PartId("powered_gate"), BuildImpact.Parts(BuildImpact.Solution, Context(BuildImpact.Solution, known, empty))!);
        Assert.Contains(new PartId("powered_gate"), BuildImpact.Parts(BuildImpact.Solution, Context(BuildImpact.Solution, empty, known))!);
        var unknown = known.Replace(GateProject, "elements/PoweredGate/missing.csproj", StringComparison.Ordinal);
        Assert.Null(BuildImpact.Parts(BuildImpact.Solution, Context(BuildImpact.Solution, empty, unknown)));
        Assert.Null(BuildImpact.Parts(BuildImpact.Solution, Context(BuildImpact.Solution, empty, "<Solution><Configurations /></Solution>")));
    }

    [Fact]
    public void CatalogueRowsNarrowButHelperLogicDoesNot()
    {
        var source = TestRepository.Read("tools/E2eRunner/SuiteCatalogue.cs");
        var row = source.Replace("Parallel(SuiteId.Cat005,", "Parallel(SuiteId.Cat005, /* row */", StringComparison.Ordinal);
        Assert.Empty(CatalogueImpact.Changed(source, row)!);
        var changed = source.Replace("""Parallel(SuiteId.Cat005, new PartId("battery"),""", """Parallel(SuiteId.Cat005, new PartId("counter"),""", StringComparison.Ordinal);
        Assert.NotEqual(source, changed);
        Assert.Equal(new[] { SuiteId.Cat005 }, CatalogueImpact.Changed(source, changed));
        Assert.Null(CatalogueImpact.Changed(source, source.Replace("SuiteTiming.Parallel, []", "SuiteTiming.SerialAlone, []", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("engine/gpu/WorkshopPhysicsCompiler.cs")]
    [InlineData("CuriousContraptions.Simulation/Program.cs")]
    [InlineData("Directory.Build.props")]
    public void SharedEngineAndBuildRulesRemainFull(string path) =>
        Assert.Equal(ImpactKind.Full, ImpactRules.Resolve(path).Kind);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ReverseTransitiveConsumersIncludeRemovedEdgesAndTerminateCycles(bool removing, bool cycle)
    {
        const string delay = "elements/DelayBox/DelayBox.csproj";
        const string battery = "elements/Battery/Battery.csproj";
        static string Project(string? reference) => "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>" +
            (reference is null ? "" : $"<ProjectReference Include=\"{reference}\" />") + "</ItemGroup></Project>";
        var before = new Dictionary<string, string>
        {
            [GateProject] = Project(cycle ? "../Battery/Battery.csproj" : null),
            [CounterProject] = Project(removing ? "../PoweredGate/PoweredGate.csproj" : null),
            [delay] = Project("../Counter/Counter.csproj"),
            [battery] = Project("../DelayBox/DelayBox.csproj"),
        };
        var after = new Dictionary<string, string>(before)
        {
            [CounterProject] = Project(removing ? null : "../PoweredGate/PoweredGate.csproj"),
        };
        var context = new ImpactContext(TestRepository.Root,
            path => before.TryGetValue(path, out var old) ? old : Current(path),
            path => after.TryGetValue(path, out var current) ? current : Current(path));
        var parts = BuildImpact.Parts(GateProject, context);
        Assert.NotNull(parts);
        Assert.Equal(new[] { new PartId("battery"), new PartId("counter"), new PartId("delay"), new PartId("powered_gate") }.Order(), parts.Order());
        Assert.DoesNotContain(new PartId("wall"), parts);
    }

    [Fact]
    public void SelectorConsumesPackageSolutionAndCatalogueImpacts()
    {
        var projectSelection = Selector.Select([CounterProject], Context(CounterProject, Current(CounterProject), Current(CounterProject)));
        Assert.Equal(SelectionMode.Selected, projectSelection.Mode);
        Assert.Contains(projectSelection.Suites, suite => suite.Suite == SuiteId.Cat020);
        Assert.All(SuiteCatalogue.SmokePair, id => Assert.Contains(projectSelection.Suites, suite => suite.Suite == id));

        const string empty = "<Solution />";
        var withGate = $"<Solution><Project Path=\"{GateProject}\" /></Solution>";
        var solutionSelection = Selector.Select([BuildImpact.Solution], Context(BuildImpact.Solution, empty, withGate));
        Assert.Equal(SelectionMode.Selected, solutionSelection.Mode);
        Assert.Contains(solutionSelection.Suites, suite => suite.Suite == SuiteId.Cat051);
        Assert.Contains(solutionSelection.Suites, suite => suite.Suite == SuiteId.Cat051OwnSupply);
        Assert.All(SuiteCatalogue.SmokePair, id => Assert.Contains(solutionSelection.Suites, suite => suite.Suite == id));

        const string catalogue = "tools/E2eRunner/SuiteCatalogue.cs";
        var source = TestRepository.Read(catalogue);
        var changed = source.Replace("""Parallel(SuiteId.Cat005, new PartId("battery"),""", """Parallel(SuiteId.Cat005, new PartId("counter"),""", StringComparison.Ordinal);
        var rowSelection = Selector.Select([catalogue], Context(catalogue, source, changed));
        Assert.Equal(SelectionMode.Selected, rowSelection.Mode);
        Assert.Equal(new[] { SuiteId.Cat005, SuiteId.EngineCore2a1, SuiteId.EngineCore2b1 }.Order(),
            rowSelection.Suites.Select(suite => suite.Suite).Order());

        Assert.Equal(SelectionMode.Full, Selector.Select([BuildImpact.Solution],
            Context(BuildImpact.Solution, empty, "<Solution><Project Path=\"unknown.csproj\" /></Solution>")).Mode);
        Assert.Equal(SelectionMode.Full, Selector.Select([catalogue],
            Context(catalogue, source, source.Replace("SuiteTiming.Parallel, []", "SuiteTiming.SerialAlone, []", StringComparison.Ordinal))).Mode);
        Assert.Equal(SelectionMode.Full, Selector.Select([CounterProject], Context(CounterProject, Current(CounterProject), "<broken")).Mode);
    }

    [Fact]
    public void CatalogueAmbiguityAndPreprocessorChangesAreConservative()
    {
        const string path = "tools/E2eRunner/SuiteCatalogue.cs";
        var source = TestRepository.Read(path);
        var helper = source + "\ninternal static class Extra { public static int All = 0; }";
        Assert.NotNull(CatalogueImpact.Changed(helper, helper));
        var duplicate = source.Replace("public static readonly IReadOnlyList<SuiteDeclaration> All =", 
            "public static readonly int All = 1; public static readonly IReadOnlyList<SuiteDeclaration> All =", StringComparison.Ordinal);
        Assert.NotEqual(source, duplicate);
        Assert.Null(CatalogueImpact.Changed(duplicate, duplicate));
        Assert.Null(CatalogueImpact.Changed(source, "#define NEW_CONFIGURATION\n" + source));
        var inactive = source + "\n#if NEVER_ENABLED\nold text\n#endif\n";
        Assert.Null(CatalogueImpact.Changed(inactive, inactive.Replace("old text", "changed text", StringComparison.Ordinal)));
    }
}
