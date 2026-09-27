using System.Text.Json.Nodes;

namespace CuriousContraptions.Tests;

public class PlaytestAuditTests
{
    private const string TwoEdges = """
        {"connections":[
            {"from":"switch","to":"fan","type":"power"},
            {"from":"switch","to":"lamp","type":"power"}
        ]}
        """;

    private static (List<string> Errors, List<string> Gaps) Check(string before, string after)
    {
        var errors = new List<string>();
        var gaps = new List<string>();
        Audit.CheckResetConnections(JsonNode.Parse(before)!, JsonNode.Parse(after)!, errors, gaps);
        return (errors, gaps);
    }

    [Theory]
    [InlineData(TwoEdges, TwoEdges)]
    [InlineData("{\"connections\":[]}", "{\"connections\":[]}")]
    [InlineData(TwoEdges, """
        {"connections":[
            {"from":"switch","to":"lamp","type":"power"},
            {"from":"switch","to":"fan","type":"power"}
        ]}
        """)]
    public void ResetPreservesConnectionsRegardlessOfArrayOrder(string before, string after)
    {
        var result = Check(before, after);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Gaps);
    }

    [Theory]
    [InlineData("from", "different-source")]
    [InlineData("to", "different-target")]
    [InlineData("type", "belt")]
    public void ChangedConnectionIdentityFails(string field, string value)
    {
        var changed = JsonNode.Parse(TwoEdges)!;
        changed["connections"]![0]![field] = value;
        var result = Check(TwoEdges, changed.ToJsonString());
        Assert.Contains("Reset did not restore directed, typed connections.", result.Errors);
    }

    [Fact]
    public void ReversingAnEdgeFails()
    {
        var changed = JsonNode.Parse(TwoEdges)!;
        changed["connections"]![0]!["from"] = "fan";
        changed["connections"]![0]!["to"] = "switch";
        Assert.NotEmpty(Check(TwoEdges, changed.ToJsonString()).Errors);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"connections\":null}")]
    public void MissingEvidenceIsIncompleteNotAnEmptyGraph(string missing)
    {
        Assert.NotEmpty(Check(TwoEdges, missing).Gaps);
        Assert.NotEmpty(Check(missing, TwoEdges).Gaps);
    }

    [Fact]
    public void MissingExtraAndDuplicateEdgesFail()
    {
        var changed = JsonNode.Parse(TwoEdges)!;
        changed["connections"]!.AsArray().RemoveAt(0);
        Assert.NotEmpty(Check(TwoEdges, changed.ToJsonString()).Errors);
        Assert.NotEmpty(Check(changed.ToJsonString(), TwoEdges).Errors);
        changed = JsonNode.Parse(TwoEdges)!;
        changed["connections"]!.AsArray().Add(changed["connections"]![0]!.DeepClone());
        Assert.Contains("Reset contains a duplicate connection.", Check(TwoEdges, changed.ToJsonString()).Errors);
    }

    [Theory]
    [InlineData("{\"connections\":{}}")]
    [InlineData("{\"connections\":[null]}")]
    [InlineData("{\"connections\":[{\"from\":7,\"to\":\"fan\",\"type\":\"power\"}]}")]
    [InlineData("{\"connections\":[{\"from\":\"switch\",\"to\":\"fan\"}]}")]
    public void MalformedEvidenceFailsWithoutThrowing(string malformed)
    {
        Assert.NotEmpty(Check(TwoEdges, malformed).Errors);
        Assert.NotEmpty(Check(malformed, TwoEdges).Errors);
    }
}
