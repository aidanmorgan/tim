using System.Text.Json;
using CuriousContraptions.Coverage;
using Xunit;

namespace CuriousContraptions.CoverageTests;

public class InventoryTests
{
    private const string FirstAnchor="<a id=\"element-001\"></a>\nFirst requirement\n";
    private const string SecondAnchor="<a id=\"element-002\"></a>\nSecond requirement\n";
    private static SourceRequirement Source()=>Assert.Single(RequirementDiscovery.Todo(FirstAnchor));

    [Fact]
    public void DiscoveryDoesNotMergeSimilarTitlesOrCountNavigationAnchors()
    {
        var rows=RequirementDiscovery.Todo(FirstAnchor+SecondAnchor+
            "<a id=\"thermal-elements\"></a>\nNavigation");
        Assert.Equal(2,rows.Count);
        Assert.NotEqual(rows[0].Key,rows[1].Key);
        Assert.All(rows,row=>Assert.Equal(RequirementOrigin.Element,row.Key.Origin));
    }
    [Fact]
    public void DuplicateAndEmptyAnchorsReject()
    {
        Assert.Throws<InvalidDataException>(()=>RequirementDiscovery.Todo(FirstAnchor+FirstAnchor));
        Assert.Throws<InvalidDataException>(()=>RequirementDiscovery.Todo("<a id=\"element-001\"></a>"));
    }
    [Fact]
    public void FixturesRemainIndividualAndAuthoredChangesInvalidateEvidence()
    {
        const string json="""
            [{"id":"lesson","parts":[
              {"id":"a","kind":"ball","locked":true,"position":[0,1,0]},
              {"id":"b","kind":"ball","locked":true,"position":[0,2,0]},
              {"id":"c","kind":"ramp","locked":false,"position":[0,3,0]}]}]
            """;
        var rows=RequirementDiscovery.Fixtures(json);
        Assert.Equal(2,rows.Count);Assert.NotEqual(rows[0].Source.Key,rows[1].Source.Key);
        var changed=RequirementDiscovery.Fixtures(json.Replace("[0,1,0]","[0,4,0]"));
        Assert.NotEqual(rows[0].Source.Hash,changed[0].Source.Hash);
        Assert.Equal(rows[1],changed[1]);
    }
    [Fact]
    public void MissingOrphanedAndChangedRecordsCannotPassCurrentInventoryCheck()
    {
        var source=Source();var inventory=CoverageAudit.Seed([source]);
        var missing=CoverageAudit.Analyze([source],new([]));
        Assert.Equal(1,missing.Missing);Assert.False(missing.SourceInventoryCurrent);
        var orphan=CoverageAudit.Analyze([],inventory);
        Assert.Equal(1,orphan.Orphaned);Assert.False(orphan.SourceInventoryCurrent);
        var changed=CoverageAudit.Analyze([source with {Hash=RequirementDiscovery.Hash("changed")}],inventory);
        Assert.Equal(1,changed.Changed);Assert.False(changed.SourceInventoryCurrent);
    }
    [Fact]
    public void CompleteInventoryAndClaimedProofNeverImplyElementCompletion()
    {
        var source=Source();var inventory=CoverageAudit.Seed([source]);
        var seeded=CoverageAudit.Analyze([source],inventory);
        Assert.True(seeded.SourceInventoryCurrent);Assert.Equal(1,seeded.Unreviewed);
        Assert.False(seeded.CompletionProven);
        var claimed=inventory.Records[0] with
        {
            Mapping=ReviewState.Reviewed,Implementation=ImplementationState.Present,
            Correctness=EvidenceState.Recorded,RealUi=EvidenceState.Recorded,
            Performance=EvidenceState.Recorded,Publication=PublicationState.Pushed
        };
        Assert.False(CoverageAudit.Analyze([source],new([claimed])).CompletionProven);
    }
    [Fact]
    public void DuplicateAndUndefinedInternalStatesReject()
    {
        var source=Source();var record=CoverageAudit.Seed([source]).Records[0];
        Assert.Throws<InvalidDataException>(()=>CoverageAudit.Analyze([source],new([record,record])));
        Assert.Throws<InvalidDataException>(()=>CoverageAudit.Analyze([source],
            new([record with {Mapping=(ReviewState)999}])));
        Assert.Throws<InvalidDataException>(()=>CoverageAudit.Seed([source with
            {Key=new((RequirementOrigin)999,source.Key.Id)}]));
    }
    [Fact]
    public void AllCanonicalEnumsRoundTripAndRejectCasingNumbersAndUndefinedValues()
    {
        Check<RequirementOrigin>();Check<ReviewState>();Check<ImplementationState>();
        Check<EvidenceState>();Check<PublicationState>();
        static void Check<T>() where T:struct,Enum
        {
            foreach(var value in Enum.GetValues<T>())
            {
                var json=JsonSerializer.Serialize(value,CoverageJson.Options);
                Assert.Equal(value,JsonSerializer.Deserialize<T>(json,CoverageJson.Options));
                Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<T>(json.ToLowerInvariant(),CoverageJson.Options));
            }
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<T>("999",CoverageJson.Options));
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<T>("\"unsupported\"",CoverageJson.Options));
            Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((T)Enum.ToObject(typeof(T),999),CoverageJson.Options));
        }
    }
    [Fact]
    public void RequiredFieldsUnknownMembersAndInvalidIdentitiesRejectAtBoundary()
    {
        var inventory=CoverageAudit.Seed([Source()]);
        var json=JsonSerializer.Serialize(inventory,CoverageJson.Options);
        Assert.Equal(inventory.Records,JsonSerializer.Deserialize<CoverageInventory>(json,CoverageJson.Options)!.Records);
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CoverageInventory>("{}",CoverageJson.Options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CoverageInventory>(
            "{\"Records\":[],\"unknown\":true}",CoverageJson.Options));
        Assert.Throws<ArgumentException>(()=>new RequirementId("../bad identity"));
        Assert.Throws<ArgumentException>(()=>new ContentHash("short"));
    }
}
