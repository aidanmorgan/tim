using CuriousContraptions.WorkshopWireGeneration;

namespace CuriousContraptions.Tests;

public sealed class WorkshopWireGenerationTests
{
    [Fact]
    public void GeneratedAccessorsHaveOneSupportedSurface()
    {
        var output = GeneratedAccessors.SelectAccessors("indexed\n#if ENABLE_SPAN_T\nspan\n#else\nsegment\n#endif\ncopy");
        Assert.Contains("indexed", output);
        Assert.Contains("segment", output);
        Assert.Contains("copy", output);
        Assert.DoesNotContain("span", output);
        Assert.DoesNotContain("#", output);
        foreach (var file in new[] { "WorkshopWireGenerated.cs", "WorkshopAuthoringGenerated.cs" })
        {
            var generated = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "engine/gpu", file));
            Assert.DoesNotContain("ENABLE_SPAN_T", generated);
            Assert.DoesNotContain("#if", generated);
        }
    }

    [Theory]
    [InlineData("#if UNKNOWN")]
    [InlineData("#if ENABLE_SPAN_T\n#if ENABLE_SPAN_T")]
    [InlineData("#if ENABLE_SPAN_T\n#else")]
    [InlineData("#else")]
    [InlineData("#endif")]
    public void MalformedInputDoesNotReplaceExistingOutput(string directive)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            var output = Path.Combine(directory, "result.txt");
            File.WriteAllText(output, "previous supported output");
            File.WriteAllText(Path.Combine(directory, "Input.cs"), directive);
            Assert.Throws<ArgumentException>(() => GeneratedAccessors.Write(directory, output));
            Assert.Equal("previous supported output", File.ReadAllText(output));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void PinnedGenerationJoinsTypesInOneNamespaceAndRefusesAnotherRuntimeStamp()
    {
        const string type = "namespace CuriousContraptions.Wire\n{\nusing global::System;\npublic struct Example { public void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_25_2_10(); } }\n}";
        var output = GeneratedAccessors.Join([type]);
        Assert.Contains("public struct Example", output);
        Assert.Equal(1, output.Split("namespace CuriousContraptions.Wire").Length - 1);
        var foreign = type.Replace("25_2_10", "25_12_19", StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => GeneratedAccessors.Join([foreign]));
        Assert.Throws<ArgumentException>(() => GeneratedAccessors.Join([type, foreign]));
    }
}
