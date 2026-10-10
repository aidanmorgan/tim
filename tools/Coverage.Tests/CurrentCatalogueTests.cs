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
        [{"id":"lesson","parts":[{"id":"ball","kind":"ball","locked":true},
        {"id":"gate","kind":"electrical_nand","locked":false}]}]
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
        Assert.Equal(302, plan.Fixtures.Length);
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
        var artifact = Assert.Single(plan.ValidateResources([new("ball")], _ => "Id = \"ball\""));
        Assert.Equal("parts/catalog/ball.tres", artifact.Path.Value);
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
        Assert.Throws<InvalidDataException>(() => Parse(Contract.Replace("CAT-001-V", "CAT-014-V")));
        Assert.Throws<InvalidDataException>(() => Parse(Contract + "\n" + Contract.Split('\n').Last()));
    }

    [Fact]
    public void AdmittedResourceAbsenceWrongIdentityAndDuplicateReject()
    {
        var plan = Parse();
        Assert.Throws<InvalidDataException>(() => plan.ValidateResources([new("ball")], _ => null));
        Assert.Throws<InvalidDataException>(() => plan.ValidateResources([new("ball")], _ => "Id = \"bowling\""));
        Assert.Throws<InvalidDataException>(() => plan.ValidateResources([new("unknown")], _ => "Id = \"unknown\""));
        Assert.Throws<InvalidDataException>(() => plan.ValidateResources([new("ball"),new("ball")], _ => "Id = \"ball\""));
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
            { Location = "parts/catalog/" + source.Key.Id.Value + ".tres", Hash = RequirementDiscovery.Hash("old resource") }));
        Assert.False(CoverageAudit.Analyze(original, oldResources).SourceInventoryCurrent);
        Assert.False(CoverageAudit.Analyze(original, inventory).CompletionProven);
    }
}
