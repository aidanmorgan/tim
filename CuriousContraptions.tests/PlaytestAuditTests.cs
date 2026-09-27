using System.Text.Json.Nodes;

namespace CuriousContraptions.Tests;

public class PlaytestAuditTests
{
    [Theory]
    [InlineData("2", false)]
    [InlineData("2.01", true)]
    [InlineData("null", true)]
    [InlineData("-1", true)]
    [InlineData("201", true)]
    [InlineData("\"2\"", true)]
    public void RopeResetRequiresExplicitUnchangedLength(string length, bool fails)
    {
        var before = JsonNode.Parse("""{"connections":[{"from":"weight","to":"pulley","type":"rope","fromPort":"tie","toPort":"tie","ropeLength":2}]}""")!;
        var after = before.DeepClone();
        after["connections"]![0]!["ropeLength"] = JsonNode.Parse(length);
        var errors = new List<string>();
        var gaps = new List<string>();
        Audit.CheckResetConnections(before, after, errors, gaps);
        Assert.Equal(fails, errors.Count > 0);
        Assert.Empty(gaps);
    }

    [Theory]
    [InlineData("""{"width":4,"height":2,"thickness":0.5}""", false)]
    [InlineData("""{"thickness":0.5,"height":2,"width":4}""", false)]
    [InlineData("""{"width":3,"height":2,"thickness":0.5}""", true)]
    [InlineData("""{"width":4,"height":2}""", true)]
    [InlineData("""{"width":4,"height":2,"thickness":0.5,"extra":1}""", true)]
    [InlineData("""{"width":"4","height":2,"thickness":0.5}""", true)]
    [InlineData("""{"width":null,"height":2,"thickness":0.5}""", true)]
    [InlineData("[]", true)]
    public void PropertyAuditChecksDimensionsAndRejectsMalformedValues(string properties, bool fails)
    {
        var initial = JsonNode.Parse("""{"properties":{"width":4,"height":2,"thickness":0.5}}""")!;
        var after = new JsonObject { ["properties"] = JsonNode.Parse(properties) };
        var errors = new List<string>();
        var gaps = new List<string>();
        Audit.CheckProperties(initial, after, "Reset", errors, gaps);
        Assert.Equal(fails, errors.Count > 0);
        Assert.Empty(gaps);
    }

    [Fact]
    public void MissingPropertyEvidenceIsIncompleteAndExplicitEmptyPropertiesAreValid()
    {
        var errors = new List<string>();
        var gaps = new List<string>();
        var empty = JsonNode.Parse("""{"properties":{}}""")!;
        Audit.CheckProperties(empty, empty, "Reset", errors, gaps);
        Assert.Empty(errors);
        Assert.Empty(gaps);
        Audit.CheckProperties(empty, new JsonObject(), "Reset", errors, gaps);
        Assert.NotEmpty(gaps);
        gaps.Clear();
        Audit.CheckProperties(new JsonObject(), empty, "Reset", errors, gaps);
        Assert.NotEmpty(gaps);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("\"unknown\"")]
    [InlineData("\"belt\"")]
    [InlineData("\"999\"")]
    [InlineData("4")]
    [InlineData("null")]
    public void MatchingInvalidDomainsAreRejected(string domain)
    {
        var state = JsonNode.Parse(TwoEdges)!;
        state["connections"]![0]!["type"] = JsonNode.Parse(domain);
        var result = Check(state.ToJsonString(), state.ToJsonString());
        Assert.NotEmpty(result.Errors);
        Assert.Empty(result.Gaps);
    }

    private const string TwoEdges = """
        {"connections":[
            {"from":"switch","to":"fan","type":"activation","fromPort":"activation_out","toPort":"activation_in"},
            {"from":"switch","to":"lamp","type":"activation","fromPort":"activation_out","toPort":"activation_in"}
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
            {"from":"switch","to":"lamp","type":"activation","fromPort":"activation_out","toPort":"activation_in"},
            {"from":"switch","to":"fan","type":"activation","fromPort":"activation_out","toPort":"activation_in"}
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
        Assert.Contains("Reset did not restore typed connections and rope lengths.", result.Errors);
    }

    [Theory]
    [InlineData("fromPort")]
    [InlineData("toPort")]
    public void ChangedSocketIdentityFails(string field)
    {
        const string original = """{"connections":[{"from":"battery","to":"motor","type":"electrical","fromPort":"supply","toPort":"power_in"}]}""";
        var changed = JsonNode.Parse(original)!;
        changed["connections"]![0]![field] = "other";
        Assert.NotEmpty(Check(original, changed.ToJsonString()).Errors);
        changed["connections"]![0]!.AsObject().Remove(field);
        Assert.NotEmpty(Check(original, changed.ToJsonString()).Errors);
        changed["connections"]![0]![field] = 4;
        Assert.NotEmpty(Check(original, changed.ToJsonString()).Errors);
        Assert.Empty(Check(original, original).Errors);
    }

    [Fact]
    public void MissingSocketsAreRejectedWithoutFallback()
    {
        var incomplete = JsonNode.Parse(TwoEdges)!;
        foreach (var link in incomplete["connections"]!.AsArray())
        {
            link!.AsObject().Remove("fromPort");
            link.AsObject().Remove("toPort");
        }
        Assert.NotEmpty(Check(TwoEdges, incomplete.ToJsonString()).Errors);
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
