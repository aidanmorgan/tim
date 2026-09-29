using System.Text.Json;
using CuriousContraptions.Coverage;
using Xunit;

namespace CuriousContraptions.CoverageTests;

public class ElementManifestTests
{
    private static SourceRequirement Catalogue(RequirementId id)=>
        new(new(RequirementOrigin.Catalogue,id),"parts/catalog/"+id.Value+".tres",
            "Shared title",RequirementDiscovery.Hash(id.Value));
    private static readonly RequirementId First=new("first");
    private static readonly RequirementId Second=new("second");
    private static readonly RequirementId Fixture=new("lesson/ball");
    private static FixtureRequirement Binding(SourceRequirement catalogue)=>
        new(new(new(RequirementOrigin.Fixture,Fixture),"content/puzzles.json#lesson/ball",
            "Fixture",RequirementDiscovery.Hash("fixture")),catalogue.Key);
    private static (SourceRequirement[] Sources,FixtureRequirement[] Fixtures) Inputs()
    {
        var first=Catalogue(First);var second=Catalogue(Second);var fixture=Binding(first);
        return ([first,second,fixture.Source],[fixture]);
    }
    [Fact]
    public void SameTitleVariantsRemainSeparateAndFixtureBindsToActualCatalogueIdentity()
    {
        var input=Inputs();var manifest=ElementAudit.Seed(input.Sources,input.Fixtures);
        Assert.Equal(2,manifest.Elements.Length);
        Assert.Single(manifest.Elements[0].Fixtures);Assert.Empty(manifest.Elements[1].Fixtures);
        Assert.NotEqual(manifest.Elements[0].Id,manifest.Elements[1].Id);
        var result=ElementAudit.Analyze(input.Sources,input.Fixtures,manifest);
        Assert.True(result.LinksCurrent);Assert.False(result.CompletionProven);
        Assert.Equal(2,result.PendingModeReviews);Assert.Equal(2,result.PendingProcessReviews);
    }
    [Fact]
    public void UnresolvedSpecificationsAreRetainedIndividually()
    {
        var input=Inputs();
        var specs=RequirementDiscovery.Todo("<a id=\"element-001\"></a>\nOne\n<a id=\"element-002\"></a>\nTwo");
        var sources=input.Sources.Concat(specs).ToArray();
        var manifest=ElementAudit.Seed(sources,input.Fixtures);
        Assert.Equal(specs,manifest.UnresolvedSources);
        var missing=manifest with {UnresolvedSources=[specs[0]]};
        Assert.False(ElementAudit.Analyze(sources,input.Fixtures,missing).LinksCurrent);
    }
    [Fact]
    public void MissingCatalogueOrFixtureAndChangedConfigurationInvalidateMapping()
    {
        var input=Inputs();var manifest=ElementAudit.Seed(input.Sources,input.Fixtures);
        Assert.False(ElementAudit.Analyze(input.Sources,input.Fixtures,
            manifest with {Elements=[manifest.Elements[0]]}).LinksCurrent);
        var withoutFixture=manifest.Elements[0] with {Fixtures=[]};
        Assert.False(ElementAudit.Analyze(input.Sources,input.Fixtures,
            manifest with {Elements=[withoutFixture,manifest.Elements[1]]}).LinksCurrent);
        var changed=input.Fixtures[0] with {Source=input.Fixtures[0].Source with
            {Hash=RequirementDiscovery.Hash("different configuration")}};
        Assert.False(ElementAudit.Analyze([input.Sources[0],input.Sources[1],changed.Source],
            [changed],manifest).LinksCurrent);
    }
    [Fact]
    public void DuplicateOrWrongFixtureOwnershipRejects()
    {
        var input=Inputs();var manifest=ElementAudit.Seed(input.Sources,input.Fixtures);
        var wrong=manifest.Elements[1] with {Fixtures=manifest.Elements[0].Fixtures};
        Assert.Throws<InvalidDataException>(()=>ElementAudit.Analyze(input.Sources,input.Fixtures,
            manifest with {Elements=[manifest.Elements[0],wrong]}));
        Assert.Throws<InvalidDataException>(()=>ElementAudit.Analyze(input.Sources,input.Fixtures,
            manifest with {Elements=[manifest.Elements[0],manifest.Elements[0]]}));
        Assert.Throws<InvalidDataException>(()=>ElementAudit.Seed(input.Sources,
            [input.Fixtures[0] with {Catalogue=new(RequirementOrigin.Catalogue,new("missing"))}]));
    }
    [Fact]
    public void CatalogueEntriesCannotBeHiddenInUnresolvedSpecifications()
    {
        var input=Inputs();var manifest=ElementAudit.Seed(input.Sources,input.Fixtures);
        Assert.Throws<InvalidDataException>(()=>ElementAudit.Analyze(input.Sources,input.Fixtures,
            manifest with {UnresolvedSources=[input.Sources[1]]}));
    }
    [Theory]
    [InlineData(ReviewState.Reviewed)]
    [InlineData((ReviewState)999)]
    public void UnsupportedReviewClaimsReject(ReviewState state)
    {
        var input=Inputs();var manifest=ElementAudit.Seed(input.Sources,input.Fixtures);
        var changed=manifest.Elements[0] with {Modes=state};
        Assert.Throws<InvalidDataException>(()=>ElementAudit.Analyze(input.Sources,input.Fixtures,
            manifest with {Elements=[changed,manifest.Elements[1]]}));
        changed=manifest.Elements[0] with {Processes=state};
        Assert.Throws<InvalidDataException>(()=>ElementAudit.Analyze(input.Sources,input.Fixtures,
            manifest with {Elements=[changed,manifest.Elements[1]]}));
    }
    [Fact]
    public void JsonRetainsTypedIdentitiesAndRequiresFixtureBindings()
    {
        var input=Inputs();var manifest=ElementAudit.Seed(input.Sources,input.Fixtures);
        var json=JsonSerializer.Serialize(manifest,CoverageJson.Options);
        var decoded=JsonSerializer.Deserialize<ElementManifest>(json,CoverageJson.Options)!;
        Assert.True(ElementAudit.Analyze(input.Sources,input.Fixtures,decoded).LinksCurrent);
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<ElementManifest>("{}",CoverageJson.Options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<FixtureRequirement>(
            JsonSerializer.Serialize(input.Fixtures[0].Source,CoverageJson.Options),CoverageJson.Options));
    }
}
