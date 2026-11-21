using CuriousContraptions.Coverage;
using Xunit;

namespace CuriousContraptions.CoverageTests;

public class CurrentCatalogueTests
{
    private const string Contract = """
        ### CAT-001-I · ball
        [Exact D/I/V acceptance](../requirements.md#current-cat-001). **Mode records:** Configuration=Fixed.
        ### CAT-014-I · bowling
        [Exact D/I/V acceptance](../requirements.md#current-cat-014). **Mode records:** Configuration=Fixed. *Delivery note (Story 6.1): two lanes, rest/bounce, Receiver, Save/Load, Reset.*
        ### CAT-024-I · electrical_nand
        [Exact D/I/V acceptance](../requirements.md#current-cat-024). **Mode records:** Configuration=Fixed, Logic=Nand.
        ## Exact fixture proof children
        | <a id="fix-001-001"></a>FIX-001-001 | lesson/ball | ball / True | CAT-001-V | FIX-001-001 |
        | <a id="fix-024-001"></a>FIX-024-001 | lesson/gate | electrical_nand / False | CAT-024-V | FIX-024-001 |
        """;
    private const string Content = """
        [{"id":"lesson","availability":"Planned","parts":[{"id":"ball","kind":"ball","locked":true},
        {"id":"gate","kind":"electrical_nand","locked":false}]}]
        """;
    /// <summary>The same fixtures in the typed placement shape of a Playable lesson.</summary>
    private const string PlayableContent = """
        [{"id":"lesson","availability":"Playable","placements":[{"slot":"ball","instance":{"part":"ball","locked":true}},
        {"slot":"gate","instance":{"part":"electrical_nand","locked":false}}]}]
        """;
    private static readonly WorkOrderId[] Ids = [new("CAT-001"), new("CAT-014"), new("CAT-024")];
    private static CurrentCatalogue Parse(string text = Contract, string suffix = "") =>
        CurrentCatalogue.Parse(text, Ids, id => (new("docs/planning/elements/" + id.Value + "-fixture.md"),
            "# " + id.Value + " · fixture\nDeclaration" + suffix));

    [Fact]
    public void AcceptanceContentChangesInvalidateOnlyAffectedCatalogueProof()
    {
        var requirements = string.Join("\n", Ids.Select(id => "<a id=\"current-cat-" + id.Value[4..] + "\"></a>\nAcceptance " + id.Value));
        var before = Parse().BindAcceptance(requirements);
        var after = Parse().BindAcceptance(requirements.Replace("Acceptance CAT-014", "Changed acceptance CAT-014"));
        Assert.Equal(before.Elements[0].Source.Hash, after.Elements[0].Source.Hash);
        Assert.NotEqual(before.Elements[1].Source.Hash, after.Elements[1].Source.Hash);
        Assert.Equal(before.Elements[2].Source.Hash, after.Elements[2].Source.Hash);
        var linked = requirements.Replace("Acceptance CAT-001", "Acceptance CAT-001 [shared](#shared)") + "\n<a id=\"shared\"></a>Shared behavior";
        Assert.NotEqual(Parse().BindAcceptance(linked).Elements[0].Source.Hash,
            Parse().BindAcceptance(linked.Replace("Shared behavior", "Changed shared behavior")).Elements[0].Source.Hash);
        Assert.Equal(Parse().BindAcceptance(linked).Elements[1].Source.Hash,
            Parse().BindAcceptance(linked.Replace("Shared behavior", "Changed shared behavior")).Elements[1].Source.Hash);
        Assert.Throws<InvalidDataException>(() => Parse().BindAcceptance(requirements.Replace("current-cat-014", "missing-014")));
        Assert.Throws<InvalidDataException>(() => Parse().BindAcceptance(requirements + "\n<a id=\"current-cat-014\"></a>Duplicate"));
    }

    [Fact]
    public void ActualCurrentContractRetainsAllIdentitiesModesAndFixtures()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, CurrentCatalogue.ConsumersPath.Value))) root = root.Parent;
        Assert.NotNull(root);
        var plan = CurrentCatalogue.Read(root);
        Assert.Equal(72, plan.Elements.Length);
        Assert.Equal(104, plan.Elements.Sum(element => element.Modes.Length));
        Assert.Equal(72, plan.Elements.Sum(element => element.Modes.Count(mode => mode.Dimension == ModeDimension.Configuration)));
        // 260 Planned parts plus 44 Playable placements, including the authored popped-goal Balloon.
        Assert.Equal(304, plan.Fixtures.Length);
    }

    [Fact]
    public void PlannedIdentitiesModesAndFixturesSurviveUnavailableResources()
    {
        var plan = Parse();
        Assert.Equal(3, plan.Elements.Length);
        Assert.Equal(4, plan.Elements.Sum(element => element.Modes.Length));
        Assert.Equal(new PlannedMode(ModeDimension.Logic, ModeChoice.Nand), plan.Elements[2].Modes[1]);
        Assert.Equal("CAT-001-I", plan.Elements[0].DeliveryOwner.Value);
        Assert.Equal("CAT-001-V", plan.Fixtures[0].ProofCriterion.Value);
        Assert.Equal("FIX-001-001", plan.Fixtures[0].Id.Value);
        Assert.Equal("lesson/ball", plan.Fixtures[0].Instance.Value);
        plan.ValidateFixtures(Content);
        var (artifacts, admitted) = plan.ValidateResources([Ball], [BallResource], _ => "Id: \"ball\"");
        var artifact = Assert.Single(artifacts);
        Assert.Equal("elements/Ball/BallElementDeclaration.cs", artifact.Path.Value);
        Assert.Equal(BallResource, Assert.Single(admitted).Value);
        Assert.Equal(3, plan.Elements.Length);
        Assert.False(ElementAudit.Analyze(plan.Elements.Select(element => element.Source), [],
            ElementAudit.Seed(plan.Elements.Select(element => element.Source), [])).CompletionProven);
    }

    [Theory]
    [InlineData("Configuration=Fixed.", "Configuration=Fixed")]
    [InlineData("Configuration=Fixed.", "")]
    [InlineData("Configuration=Fixed.", "Configuration=Fixed. **Mode records:** Configuration=Fixed.")]
    [InlineData("Logic=Nand", "Logic=nand")]
    [InlineData("Logic=Nand", "Logic=999")]
    [InlineData("Logic=Nand", "Unknown=Nand")]
    [InlineData("Logic=Nand", "Logic=Blue")]
    [InlineData("Logic=Nand", "Logic=Nand, Logic=Nand")]
    public void MalformedDuplicateAndUnknownModesReject(string before, string after) =>
        Assert.Throws<InvalidDataException>(() => Parse(Contract.Replace(before, after)));

    [Fact]
    public void CatalogueMembershipAndAcceptanceCannotBeSilentlyDroppedOrMerged()
    {
        Assert.Throws<InvalidDataException>(() => Parse(Contract.Replace("CAT-014-I · bowling", "CAT-001-I · bowling")));
        Assert.Throws<InvalidDataException>(() => Parse(Contract.Replace("CAT-014-I · bowling", "CAT-014-I · ball")));
        Assert.Throws<InvalidDataException>(() => Parse(Contract.Replace("current-cat-014", "current-cat-001")));
        Assert.Throws<InvalidDataException>(() => CurrentCatalogue.Parse(Contract, Ids[..2], id =>
            (new("declaration.md"), "# " + id.Value + " · declaration")));
        Assert.Throws<InvalidDataException>(() => CurrentCatalogue.Parse(Contract, Ids, _ =>
            (new("wrong.md"), "# CAT-999 · wrong")));
    }

    [Fact]
    public void FixtureKindLockIdentityAndProofCriterionDriftReject()
    {
        var plan = Parse();
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(Content.Replace("\"locked\":true", "\"locked\":false")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(Content.Replace("\"kind\":\"ball\"", "\"kind\":\"bowling\"")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(Content.Replace("\"id\":\"ball\"", "\"id\":\"unknown\"")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures("[]"));
        plan.ValidateFixtures(PlayableContent);
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(PlayableContent.Replace("\"part\":\"ball\"", "\"part\":\"bowling\"")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(PlayableContent.Replace("\"slot\":\"ball\"", "\"slot\":\"fixture_1\"")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(PlayableContent.Replace("Playable", "Planned")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(Content.Replace("Planned", "Playable")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(Content.Replace("Planned", "Unknown")));
        Assert.Throws<InvalidDataException>(() => plan.ValidateFixtures(Content.Replace("\"availability\":\"Planned\",", "")));
        Assert.Throws<InvalidDataException>(() => Parse(Contract.Replace("CAT-001-V", "CAT-014-V")));
        Assert.Throws<InvalidDataException>(() => Parse(Contract + "\n" + Contract.Split('\n').Last()));
    }

    private static readonly RequirementId Ball = new("ball");
    private static readonly RequirementId Bowling = new("bowling");
    private static readonly SourcePath BallResource = new("elements/Ball/BallElementDeclaration.cs");
    private static readonly SourcePath OtherResource = new("elements/Other/OtherElementDeclaration.cs");
    public enum ResourceRejection { Missing, NoId, TwoIds, Undeclared, Uncatalogued, SharedId, DeclaredWithoutResource, DuplicateDeclared }

    [Theory]
    [InlineData(ResourceRejection.Missing, "'elements/Ball/BallElementDeclaration.cs' is missing")]
    [InlineData(ResourceRejection.NoId, "'elements/Ball/BallElementDeclaration.cs' declares 0 Ids")]
    [InlineData(ResourceRejection.TwoIds, "'elements/Ball/BallElementDeclaration.cs' declares 2 Ids")]
    [InlineData(ResourceRejection.Undeclared, "Id 'bowling' is not a declared part identity")]
    [InlineData(ResourceRejection.Uncatalogued, "Id 'unknown' has no current catalogue declaration")]
    [InlineData(ResourceRejection.SharedId, "'ball' is declared by both 'elements/Ball/BallElementDeclaration.cs' and 'elements/Other/OtherElementDeclaration.cs'")]
    [InlineData(ResourceRejection.DeclaredWithoutResource, "have no element declaration: bowling.")]
    [InlineData(ResourceRejection.DuplicateDeclared, "Missing or duplicate declared part identity")]
    public void CatalogueResourceRejectionNamesTheResourceAndIdentity(ResourceRejection rejection, string message)
    {
        var plan = Parse();
        Func<(SourceArtifact[], IReadOnlyDictionary<RequirementId, SourcePath>)> validate = rejection switch
        {
            ResourceRejection.Missing => () => plan.ValidateResources([Ball], [BallResource], _ => null),
            ResourceRejection.NoId => () => plan.ValidateResources([Ball], [BallResource], _ => "Scene = null"),
            ResourceRejection.TwoIds => () => plan.ValidateResources([Ball], [BallResource], _ => "Id: \"ball\"\nId: \"ball\""),
            ResourceRejection.Undeclared => () => plan.ValidateResources([Ball], [BallResource], _ => "Id: \"bowling\""),
            ResourceRejection.Uncatalogued => () => plan.ValidateResources([new("unknown")], [BallResource], _ => "Id: \"unknown\""),
            ResourceRejection.SharedId => () => plan.ValidateResources([Ball], [BallResource, OtherResource], _ => "Id: \"ball\""),
            ResourceRejection.DeclaredWithoutResource => () => plan.ValidateResources([Ball, Bowling], [BallResource], _ => "Id: \"ball\""),
            ResourceRejection.DuplicateDeclared => () => plan.ValidateResources([Ball, Ball], [BallResource], _ => "Id: \"ball\""),
            _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Unknown resource rejection.")
        };
        Assert.Contains(message, Assert.Throws<InvalidDataException>(() => validate()).Message);
    }

    [Fact]
    public void CatalogueResourceBindsByItsOwnIdNotItsFileNameAndAcceptsCrLf()
    {
        var (_, admitted) = Parse().ValidateResources([Ball, Bowling], [OtherResource, BallResource],
            path => path == OtherResource ? "Id: \"ball\"\r\n" : "Id: \"bowling\"\r\n");
        Assert.Equal(OtherResource, admitted[Ball]);
        Assert.Equal(BallResource, admitted[Bowling]);
    }

    [Fact]
    public void DeclaredPartIdentitiesComeFromEveryBundleElementId()
    {
        const string bundle = "{\"Elements\": [{\"Metadata\": {\"Id\": \"ball\"}}, {\"Metadata\": {\"Id\": \"spring\"}}]}";
        Assert.Equal([Ball, new RequirementId("spring")], CurrentCatalogue.DeclaredPartIdentities(bundle));
        Assert.Contains("is not JSON",
            Assert.Throws<InvalidDataException>(() => CurrentCatalogue.DeclaredPartIdentities("enum PartId {}")).Message);
        Assert.Contains("has no Elements list",
            Assert.Throws<InvalidDataException>(() => CurrentCatalogue.DeclaredPartIdentities("{\"Parts\": []}")).Message);
        Assert.Contains("without a Metadata.Id",
            Assert.Throws<InvalidDataException>(() => CurrentCatalogue.DeclaredPartIdentities("{\"Elements\": [{\"Metadata\": {\"Id\": 1}}]}")).Message);
        Assert.Contains("missing or duplicate part identity",
            Assert.Throws<InvalidDataException>(() => CurrentCatalogue.DeclaredPartIdentities(bundle.Replace("\"spring\"", "\"ball\""))).Message);
        Assert.Contains("missing or duplicate part identity",
            Assert.Throws<InvalidDataException>(() => CurrentCatalogue.DeclaredPartIdentities("{\"Elements\": []}")).Message);
    }

    [Fact]
    public void ActualPartIdentityTableDeclaresEveryCatalogueResource()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, CurrentCatalogue.ConsumersPath.Value))) root = root.Parent;
        Assert.NotNull(root);
        var plan = CurrentCatalogue.Read(root);
        var resources = Directory.EnumerateDirectories(Path.Combine(root.FullName, "elements"))
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*ElementDeclaration.cs"))
            .Select(path => new SourcePath(Path.GetRelativePath(root.FullName, path).Replace('\\', '/')))
            .OrderBy(path => path.Value, StringComparer.Ordinal).ToArray();
        var declared = CurrentCatalogue.DeclaredPartIdentities(File.ReadAllText(
            Path.Combine(root.FullName, CurrentCatalogue.PartIdentityPath.Value)));
        Assert.Equal(declared.OrderBy(id => id.Value, StringComparer.Ordinal),
            plan.Admitted.Keys.OrderBy(id => id.Value, StringComparer.Ordinal));
        Assert.Equal(resources, plan.Admitted.Values.OrderBy(path => path.Value, StringComparer.Ordinal));
        foreach (var (id, path) in plan.Admitted)
            Assert.Contains("Id: \"" + id.Value + "\"", File.ReadAllText(Path.Combine(root.FullName, path.Value)));
        Assert.Equal(plan.Admitted.Values.OrderBy(path => path.Value, StringComparer.Ordinal),
            plan.AvailableResources.Select(artifact => artifact.Path));
    }

    [Fact]
    public void ChangedDeclarationAndOldResourceHashesRemainStale()
    {
        var original = Parse().Elements.Select(element => element.Source).ToArray();
        var inventory = CoverageAudit.Seed(original);
        var changed = Parse(suffix: " revised").Elements.Select(element => element.Source);
        var report = CoverageAudit.Analyze(changed, inventory);
        Assert.Equal(3, report.Changed);
        Assert.False(report.SourceInventoryCurrent);
        var oldResources = CoverageAudit.Seed(original.Select(source => source with
            { Location = "elements/" + source.Key.Id.Value + "/ElementDeclaration.cs", Hash = RequirementDiscovery.Hash("old resource") }));
        Assert.False(CoverageAudit.Analyze(original, oldResources).SourceInventoryCurrent);
        Assert.False(CoverageAudit.Analyze(original, inventory).CompletionProven);
    }

    private static readonly SourcePath NamedDeclaration = new("docs/planning/elements/EL-239-baseball.md");
    private const string NamedText = """
        # EL-239 · Baseball
        Current declaration.

        ## Package source binding

        **Package:** baseball.
        **Named identity:** [EL-239](../invest/named-elements.md#element-239).
        **Acceptance:** [todo-198](../requirements.md#todo-198), [sequence-task-530](../requirements.md#sequence-task-530).
        **Decision:** [S636](../invest/scope-corrections.md#s636).
        **Mode records:** Configuration=Fixed.
        """;
    private const string NamedRequirements = """
        <a id="todo-198"></a> [ ] <a id="sequence-task-530"></a> Baseball distinct role.
        <a id="sequence-task-531"></a> Unrelated task acceptance.
        """;
    private const string NamedLedger = """
        <a id="element-239"></a>
        ### element-239 · EL-239 · Baseball
        Source [existing record](../requirements.md#todo-198). Distinct mass and bounce.
        Owner **S636** (conditional design decision).
        <a id="element-240"></a> Unrelated identity.
        """;
    private const string NamedDecision = """
        <a id="s636"></a>
        ### S636 · Conditional adoption
        Adopt only with a visible bounce distinction.
        <a id="s637"></a> Unrelated decision.
        """;
    private static PlannedNamedPackage[] Named(string text = NamedText, string requirements = NamedRequirements,
        string? ledger = NamedLedger, string? decision = NamedDecision, SourcePath? path = null) =>
        NamedPackageSources.Parse([(path ?? NamedDeclaration, text)], requirements,
            source => source == NamedPackageSources.LedgerPath ? ledger :
                source == NamedPackageSources.DecisionPath ? decision : null);

    [Fact]
    public void NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias()
    {
        var named = Assert.Single(Named());
        Assert.Equal(new RequirementId("baseball"), named.Package);
        Assert.Equal(new WorkOrderId("EL-239"), named.Identity);
        Assert.Equal(new SourceKey(RequirementOrigin.Task, new("sequence-task-530")), named.Source.Key);
        Assert.Equal(new PlannedMode(ModeDimension.Configuration, ModeChoice.Fixed), Assert.Single(named.Modes));
        var original = Parse();
        var plan = original with { NamedPackages = [named] };
        var resource = new SourcePath("elements/Baseball/BaseballElementDeclaration.cs");
        var (_, admitted) = plan.ValidateResources([Ball, named.Package], [BallResource, resource],
            path => path == resource ? "Id: \"baseball\"" : "Id: \"ball\"");
        Assert.Equal(resource, admitted[named.Package]);
        Assert.Equal(original.Elements, plan.Elements);
        Assert.Equal(original.Fixtures, plan.Fixtures);
        var unfamiliar = Assert.Single(Named(NamedText.Replace("baseball", "quartz").Replace("239", "987"),
            ledger: NamedLedger.Replace("239", "987"), path: new("docs/planning/elements/EL-987-quartz.md")));
        Assert.Equal(new RequirementId("quartz"), unfamiliar.Package);
        Assert.Equal(new WorkOrderId("EL-987"), unfamiliar.Identity);
        Assert.Equal(named.Source.Key, unfamiliar.Source.Key);
        var actualRoot = new DirectoryInfo(AppContext.BaseDirectory);
        while (actualRoot is not null && !File.Exists(Path.Combine(actualRoot.FullName, CurrentCatalogue.ConsumersPath.Value)))
            actualRoot = actualRoot.Parent;
        Assert.NotNull(actualRoot);
        var actual = CurrentCatalogue.Read(actualRoot);
        Assert.Contains(actual.NamedPackages, package => package.Package == named.Package && package.Source.Key == named.Source.Key);
        Assert.DoesNotContain(actual.Elements, element => element.Source.Key.Id == named.Package);
    }

    public enum NamedRejection
    {
        MissingLedger, MissingDecision, MissingAnchor, DuplicateAnchor, EmptyDecision, WrongLedger,
        WrongTodo, WrongTask, WrongTitle, WrongFilename, MissingBinding, DuplicateBinding,
        UnknownMode, MissingConfiguration, DuplicateMode, UnknownPackage, CatalogueCollision
    }

    [Theory]
    [InlineData(NamedRejection.MissingLedger)]
    [InlineData(NamedRejection.MissingDecision)]
    [InlineData(NamedRejection.MissingAnchor)]
    [InlineData(NamedRejection.DuplicateAnchor)]
    [InlineData(NamedRejection.EmptyDecision)]
    [InlineData(NamedRejection.WrongLedger)]
    [InlineData(NamedRejection.WrongTodo)]
    [InlineData(NamedRejection.WrongTask)]
    [InlineData(NamedRejection.WrongTitle)]
    [InlineData(NamedRejection.WrongFilename)]
    [InlineData(NamedRejection.MissingBinding)]
    [InlineData(NamedRejection.DuplicateBinding)]
    [InlineData(NamedRejection.UnknownMode)]
    [InlineData(NamedRejection.MissingConfiguration)]
    [InlineData(NamedRejection.DuplicateMode)]
    [InlineData(NamedRejection.UnknownPackage)]
    [InlineData(NamedRejection.CatalogueCollision)]
    public void NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings(NamedRejection rejection)
    {
        Action act = rejection switch
        {
            NamedRejection.MissingLedger => () => Named(ledger: null),
            NamedRejection.MissingDecision => () => Named(decision: null),
            NamedRejection.MissingAnchor => () => Named(decision: NamedDecision.Replace("s636", "s999")),
            NamedRejection.DuplicateAnchor => () => Named(decision: NamedDecision + "\n<a id=\"s636\"></a>Duplicate."),
            NamedRejection.EmptyDecision => () => Named(decision: "<a id=\"s636\"></a>\n<a id=\"s637\"></a>Other."),
            NamedRejection.WrongLedger => () => Named(ledger: NamedLedger.Replace("EL-239", "EL-240")),
            NamedRejection.WrongTodo => () => Named(ledger: NamedLedger.Replace("todo-198", "todo-199")),
            NamedRejection.WrongTask => () => Named(NamedText.Replace("sequence-task-530", "sequence-task-531")),
            NamedRejection.WrongTitle => () => Named(NamedText.Replace("# EL-239 ·", "# EL-240 ·")),
            NamedRejection.WrongFilename => () => Named(path: new("docs/planning/elements/EL-240-baseball.md")),
            NamedRejection.MissingBinding => () => Named(NamedText.Replace("## Package source binding", "## Other")),
            NamedRejection.DuplicateBinding => () => Named(NamedText + "\n\n## Package source binding\n"),
            NamedRejection.UnknownMode => () => Named(NamedText.Replace("Fixed", "Unknown")),
            NamedRejection.MissingConfiguration => () => Named(NamedText.Replace("Configuration=Fixed", "Logic=And")),
            NamedRejection.DuplicateMode => () => Named(NamedText.Replace("Configuration=Fixed", "Configuration=Fixed, Configuration=Fixed")),
            NamedRejection.UnknownPackage => () => (Parse() with { NamedPackages = Named() })
                .ValidateResources([new("unknown")], [OtherResource], _ => "Id: \"unknown\""),
            NamedRejection.CatalogueCollision => () => (Parse() with { NamedPackages = Named(NamedText.Replace("baseball.", "ball.")) })
                .ValidateResources([Ball], [BallResource], _ => "Id: \"ball\""),
            _ => throw new ArgumentOutOfRangeException(nameof(rejection))
        };
        Assert.Throws<InvalidDataException>(act);
    }

    public enum NamedCollision { None, Package, Identity, Task }

    [Theory]
    [InlineData(NamedCollision.None)]
    [InlineData(NamedCollision.Package)]
    [InlineData(NamedCollision.Identity)]
    [InlineData(NamedCollision.Task)]
    public void NamedPackageUniquenessIsDiscriminatingForEachKey(NamedCollision collision)
    {
        var first = Assert.Single(Named());
        var identity = collision == NamedCollision.Identity ? "239" : "987";
        var package = collision == NamedCollision.Package ? "baseball" : "quartz";
        var todo = collision == NamedCollision.Task ? "todo-198" : "todo-199";
        var task = collision == NamedCollision.Task ? "sequence-task-530" : "sequence-task-531";
        var text = NamedText.Replace("239", identity).Replace("baseball.", package + ".")
            .Replace("todo-198", todo).Replace("sequence-task-530", task);
        // Parse each source against its own valid authority document. Two declarations
        // of one identity can reference different task sources in different documents;
        // neither individual document is invalid, but their union must reject.
        var second = Assert.Single(Named(text,
            $"<a id=\"{todo}\"></a> [ ] <a id=\"{task}\"></a> Independent acceptance.",
            NamedLedger.Replace("239", identity).Replace("todo-198", todo),
            path: new("docs/planning/elements/EL-" + identity + "-quartz.md")));
        PlannedNamedPackage[] sources = [first, second];
        Assert.Equal(collision == NamedCollision.Package ? 1 : 2, sources.Select(source => source.Package).Distinct().Count());
        Assert.Equal(collision == NamedCollision.Identity ? 1 : 2, sources.Select(source => source.Identity).Distinct().Count());
        Assert.Equal(collision == NamedCollision.Task ? 1 : 2, sources.Select(source => source.Source.Key).Distinct().Count());
        if (collision == NamedCollision.None)
        {
            NamedPackageSources.ValidateUnique(sources);
            Assert.Equal(2, ParseTogether().Length);
            return;
        }
        var expected = collision switch
        {
            NamedCollision.Package => "Duplicate named package binding.",
            NamedCollision.Identity => "Duplicate named identity binding.",
            NamedCollision.Task => "Duplicate named task source binding.",
            _ => throw new ArgumentOutOfRangeException(nameof(collision))
        };
        Assert.Equal(expected, Assert.Throws<InvalidDataException>(() => NamedPackageSources.ValidateUnique(sources)).Message);
        // With distinct identities these contracts also share one valid current ledger.
        // Exercise Parse's integration with the same production uniqueness validator.
        if (collision != NamedCollision.Identity)
            Assert.Equal(expected, Assert.Throws<InvalidDataException>(() => ParseTogether()).Message);

        PlannedNamedPackage[] ParseTogether()
        {
            var requirements = NamedRequirements.Split('\n')[0];
            if (collision != NamedCollision.Task)
                requirements += $"\n<a id=\"{todo}\"></a> [ ] <a id=\"{task}\"></a> Independent acceptance.";
            var ledger = NamedLedger + "\n" + NamedLedger.Replace("239", identity).Replace("todo-198", todo);
            return NamedPackageSources.Parse([(NamedDeclaration, NamedText),
                (new("docs/planning/elements/EL-" + identity + "-quartz.md"), text)], requirements,
                path => path == NamedPackageSources.LedgerPath ? ledger : NamedDecision);
        }
    }

    public enum DecisionMismatch { UnrelatedExisting, MissingOwner, AmbiguousOwner, RepeatedOwner }

    [Theory]
    [InlineData(DecisionMismatch.UnrelatedExisting)]
    [InlineData(DecisionMismatch.MissingOwner)]
    [InlineData(DecisionMismatch.AmbiguousOwner)]
    [InlineData(DecisionMismatch.RepeatedOwner)]
    public void NamedDecisionMustMatchExactlyOneLedgerOwner(DecisionMismatch mismatch)
    {
        const string owner = "Owner **S636**";
        var ledger = mismatch switch
        {
            DecisionMismatch.MissingOwner => NamedLedger.Replace(owner, "Decision unspecified"),
            DecisionMismatch.AmbiguousOwner => NamedLedger.Replace(owner, owner + " and Owner **S638**"),
            DecisionMismatch.RepeatedOwner => NamedLedger.Replace(owner, owner + " and " + owner),
            _ => NamedLedger
        };
        var text = mismatch == DecisionMismatch.UnrelatedExisting
            ? NamedText.Replace("S636", "S638").Replace("s636", "s638") : NamedText;
        var decisions = NamedDecision + "\n<a id=\"s638\"></a>Existing unrelated driven-wheel decision.";
        var error = Assert.Throws<InvalidDataException>(() => Named(text, ledger: ledger, decision: decisions));
        Assert.Equal(mismatch == DecisionMismatch.UnrelatedExisting
            ? "Named decision differs from its ledger owner."
            : "Missing or ambiguous named ledger decision owner.", error.Message);
        Assert.Equal(new NamedSourceReference(NamedPackageSources.DecisionPath, new("s636")),
            Assert.Single(Named(decision: decisions)).Decision);
    }

    [Fact]
    public void CurrentNamedDecisionCannotDetachItsAcceptanceAndFreshness()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, CurrentCatalogue.ConsumersPath.Value))) root = root.Parent;
        Assert.NotNull(root);
        var text = File.ReadAllText(Path.Combine(root.FullName, NamedDeclaration.Value));
        var requirements = File.ReadAllText(Path.Combine(root.FullName, RequirementDiscovery.RequirementsPath.Value));
        var ledger = File.ReadAllText(Path.Combine(root.FullName, NamedPackageSources.LedgerPath.Value));
        var decisions = File.ReadAllText(Path.Combine(root.FullName, NamedPackageSources.DecisionPath.Value));
        var source = Assert.Single(Named(text, requirements, ledger, decisions)).Source;
        Assert.Equal(new SourceKey(RequirementOrigin.Task, new("sequence-task-530")), source.Key);
        // S638 exists in the real current source; selecting it must not replace S636's obligation.
        Assert.Contains("<a id=\"s638\"></a>", decisions);
        Assert.Equal("Named decision differs from its ledger owner.", Assert.Throws<InvalidDataException>(() =>
            Named(text.Replace("S636", "S638").Replace("s636", "s638"), requirements, ledger, decisions)).Message);
        var revised = Assert.Single(Named(text, requirements, ledger,
            decisions.Replace("<a id=\"s636\"></a>", "<a id=\"s636\"></a>\nAdditional Baseball acceptance."))).Source;
        var unrelated = Assert.Single(Named(text, requirements, ledger,
            decisions.Replace("<a id=\"s638\"></a>", "<a id=\"s638\"></a>\nAdditional wheel acceptance."))).Source;
        var inventory = CoverageAudit.Seed([source]);
        Assert.Equal(1, CoverageAudit.Analyze([revised], inventory).Changed);
        Assert.False(CoverageAudit.Analyze([revised], inventory).SourceInventoryCurrent);
        Assert.True(CoverageAudit.Analyze([unrelated], inventory).SourceInventoryCurrent);
    }

    public enum NamedChange { Task, Declaration, Ledger, Decision, UnrelatedTask, UnrelatedLedger, UnrelatedDecision }

    [Theory]
    [InlineData(NamedChange.Task, 1)]
    [InlineData(NamedChange.Declaration, 1)]
    [InlineData(NamedChange.Ledger, 1)]
    [InlineData(NamedChange.Decision, 1)]
    [InlineData(NamedChange.UnrelatedTask, 1)]
    [InlineData(NamedChange.UnrelatedLedger, 0)]
    [InlineData(NamedChange.UnrelatedDecision, 0)]
    public void NamedAcceptanceChangesInvalidateOnlyItsTaskProof(NamedChange change, int changed)
    {
        var before = NamedSources(NamedRequirements, NamedText, NamedLedger, NamedDecision);
        var requirements = change switch
        {
            NamedChange.Task => NamedRequirements.Replace("Baseball distinct role.", "Revised Baseball role."),
            NamedChange.UnrelatedTask => NamedRequirements.Replace("Unrelated task acceptance.", "Revised other task."),
            _ => NamedRequirements
        };
        var declaration = change == NamedChange.Declaration ? NamedText.Replace("Current declaration.", "Revised declaration.") : NamedText;
        var ledger = change switch
        {
            NamedChange.Ledger => NamedLedger.Replace("Distinct mass and bounce.", "Revised mass and bounce."),
            NamedChange.UnrelatedLedger => NamedLedger.Replace("Unrelated identity.", "Revised other identity."),
            _ => NamedLedger
        };
        var decision = change switch
        {
            NamedChange.Decision => NamedDecision.Replace("visible bounce", "visible roll"),
            NamedChange.UnrelatedDecision => NamedDecision.Replace("Unrelated decision.", "Revised other decision."),
            _ => NamedDecision
        };
        var after = NamedSources(requirements, declaration, ledger, decision);
        var report = CoverageAudit.Analyze(after, CoverageAudit.Seed(before));
        Assert.Equal(changed, report.Changed);
        Assert.Equal(changed == 0, report.SourceInventoryCurrent);
        // Examine each obligation through the actual audit result, preserving unrelated CAT and task proof.
        foreach (var source in before)
        {
            var expected = changed == 1 && source.Key == new SourceKey(RequirementOrigin.Task,
                new(change == NamedChange.UnrelatedTask ? "sequence-task-531" : "sequence-task-530"));
            var single = CoverageAudit.Analyze(after.Where(item => item.Key == source.Key), CoverageAudit.Seed([source]));
            Assert.Equal(expected ? 1 : 0, single.Changed);
        }
        Assert.False(report.CompletionProven);
    }

    private static SourceRequirement[] NamedSources(string requirements, string declaration, string ledger, string decision)
    {
        var named = Assert.Single(Named(declaration, requirements, ledger, decision));
        return [..RequirementDiscovery.Todo(requirements).Select(source => source.Key == named.Source.Key ? named.Source : source),
            ..Parse().Elements.Select(element => element.Source)];
    }
}
