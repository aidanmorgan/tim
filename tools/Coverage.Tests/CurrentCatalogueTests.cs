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
        // 276 Planned parts plus 27 Playable placements (domino_effect's locked lamp is a current fixture).
        Assert.Equal(303, plan.Fixtures.Length);
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
        // Every package in the generated bundle, including the Hold Timer (CAT-033) and the supplied gates: Both (CAT-013), Not both
        // (CAT-024), Neither (CAT-025), Either (CAT-026) and Exactly one (CAT-027), and the Weight (CAT-067).
        Assert.Equal(29, plan.Admitted.Count);
        Assert.Equal(new SourcePath("elements/Weight/WeightElementDeclaration.cs"), plan.Admitted[new("weight")]);
        Assert.Equal(new SourcePath("elements/ElectricalXor/ElectricalXorElementDeclaration.cs"), plan.Admitted[new("electrical_xor")]);
        Assert.Equal(new SourcePath("elements/ElectricalNand/ElectricalNandElementDeclaration.cs"), plan.Admitted[new("electrical_nand")]);
        Assert.Equal(new SourcePath("elements/ElectricalNor/ElectricalNorElementDeclaration.cs"), plan.Admitted[new("electrical_nor")]);
        Assert.Equal(new SourcePath("elements/ElectricalOr/ElectricalOrElementDeclaration.cs"), plan.Admitted[new("electrical_or")]);
        Assert.Equal(new SourcePath("elements/Springboard/SpringboardElementDeclaration.cs"), plan.Admitted[new("spring")]);
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
}
