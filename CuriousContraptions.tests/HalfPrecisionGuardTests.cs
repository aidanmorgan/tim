using System.Text.RegularExpressions;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.44 architecture guard (D8): every game value is f32, so no half-precision type, codec, accessor, schema lane or
/// shader type remains in the game's code. It scans every C#, TypeScript, JavaScript, FlatBuffers schema and shader source in the
/// repository (the worker sources included) with the story's inventory patterns: the whole-word type and lane names, the codec and
/// accessor names, and the shape of a hand-written half-precision decoder. Prose (Markdown, JSON content) is out of scope.</summary>
public sealed class HalfPrecisionGuardTests(ITestOutputHelper output)
{
    private static readonly Regex Words = new(@"\b(?:Half|HalfBits|Float16|float16|getF16|setF16|binary16|f16)\b", RegexOptions.CultureInvariant); // guard control
    private static readonly Regex Accessors = new( // guard control
        @"ReadF16|WriteF16|ReadHalf|WriteHalf|HalfToUInt16Bits|UInt16BitsToHalf|HalfToInt16Bits|Int16BitsToHalf|decodeFloat16|encodeFloat16|" + // guard control
        @"getFloat16|setFloat16|Float16Array|DragBits", RegexOptions.CultureInvariant); // guard control
    /// <summary>A hand-written half-precision decoder extracts the 5-bit exponent from bits 10–14.</summary>
    private static readonly Regex DecoderShape = new(@">>>?\s*10\)?\s*&\s*(?:31|0x1[fF])\b", RegexOptions.CultureInvariant);

    /// <summary>OQ1: a campaign title that uses the ordinary word is removed token by token before the line is matched.</summary>
    private static readonly Regex CampaignTitles = new(@"Half the Battle|Half-life of the party", RegexOptions.CultureInvariant); // guard control

    private static readonly string[] Extensions = [".cs", ".ts", ".js", ".mjs", ".fbs", ".wgsl", ".glsl", ".gdshader", ".shader"];

    /// <summary>Planning records (historical evidence), archives, and build or package output.</summary>
    private static readonly string[] ExcludedDirectories = ["_bmad-output", "archives", "node_modules", "bin", "obj", "AppBundle", ".godot", ".git"];

    /// <summary>The only exempt lines: this guard's own patterns and positive controls, each marked at its end.</summary>
    private const string GuardFile = "CuriousContraptions.tests/HalfPrecisionGuardTests.cs";
    private const string ControlMarker = "// guard control";

    private static string Root => twodog.Engine.ResolveProjectDir();

    internal static bool Violates(string line)
    {
        var code = CampaignTitles.Replace(line, "");
        return Words.IsMatch(code) || Accessors.IsMatch(code) || DecoderShape.IsMatch(code);
    }

    private static IEnumerable<string> Sources()
    {
        var pending = new Stack<string>([Root]);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory))
                if (!ExcludedDirectories.Contains(Path.GetFileName(child), StringComparer.Ordinal)) pending.Push(child);
            foreach (var file in Directory.EnumerateFiles(directory))
                if (Extensions.Contains(Path.GetExtension(file), StringComparer.Ordinal))
                    yield return Path.GetRelativePath(Root, file).Replace('\\', '/');
        }
    }

    [Fact]
    public void GuardNoHalfPrecisionTypeCodecOrLaneRemainsInCodeOrSchemas()
    {
        var sources = Sources().ToList();
        Assert.Contains("engine/gpu/PhysicsGpuAbi.cs", sources);
        Assert.Contains("schemas/workshop_wire.fbs", sources);
        Assert.Contains("CuriousContraptions.web/wwwroot/workshop-client.js", sources);
        Assert.Contains("CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs", sources);
        Assert.Contains("CuriousContraptions.Simulation/wwwroot/worker.js", sources);
        Assert.Contains("tools/e2e/cat-062b.test.ts", sources);
        var hits = new List<string>();
        var exempt = 0;
        foreach (var source in sources)
        {
            var lines = File.ReadAllLines(Path.Combine(Root, source));
            for (var i = 0; i < lines.Length; i++)
            {
                if (!Violates(lines[i])) continue;
                if (source == GuardFile && lines[i].TrimEnd().EndsWith(ControlMarker, StringComparison.Ordinal)) { exempt++; continue; }
                hits.Add($"{source}:{i + 1}: {lines[i].Trim()}");
            }
        }
        output.WriteLine($"{sources.Count} sources scanned, {exempt} marked guard-control lines");
        Assert.True(exempt > 0, "The guard's own marked controls must be seen, or the scan did not read this file.");
        Assert.True(hits.Count == 0, "half-precision names remain:\n" + string.Join('\n', hits));
    }

    // Controls: each pattern catches the forms it exists to forbid, and the f32 names, ordinary words and campaign titles pass.
    [Theory]
    [InlineData("    public readonly record struct Mass(Half Value);", true)] // guard control
    [InlineData("        var drag = (Half)value;", true)] // guard control
    [InlineData("    private static float ReadF16(ReadOnlySpan<byte> span, int offset) =>", true)] // guard control
    [InlineData("        b.Drag = drag; b.DragBits = bits;", true)] // guard control
    [InlineData("    const value = decodeFloat16(bits);", true)] // guard control
    [InlineData("    const bits = encodeFloat16(value);", true)] // guard control
    [InlineData("    raw_assistance: [float16];", true)] // guard control
    [InlineData("        if (!HalfBits.Equal(a, b)) return false;", true)] // guard control
    [InlineData("        BinaryPrimitives.WriteUInt16LittleEndian(span, BitConverter.HalfToUInt16Bits(value));", true)] // guard control
    [InlineData("        var bits = BitConverter.HalfToInt16Bits(value);", true)] // guard control
    [InlineData("        var value = BitConverter.Int16BitsToHalf(bits);", true)] // guard control
    [InlineData("    const value = view.getFloat16(56, true);", true)] // guard control
    [InlineData("    view.setFloat16(56, value, true);", true)] // guard control
    [InlineData("    const lanes = new Float16Array(buffer);", true)] // guard control
    [InlineData("enable f16;", true)] // guard control
    [InlineData("    var<storage> lanes: array<f16>;", true)] // guard control
    [InlineData("    const sign=bits&0x8000?-1:1,e=(bits>>>10)&31,m=bits&1023;", true)] // guard control
    [InlineData("    const e = (bits >> 10) & 0x1f;", true)] // guard control
    [InlineData("    public readonly record struct Mass(float Value);", false)]
    [InlineData("        var extents = collider.HalfExtents; var angle = threshold.CosineHalfAngle;", false)]
    [InlineData("    // half the shape's extent, in half-precision terms", false)]
    [InlineData("    const value = view.getFloat32(56, true); const property = view.getUint16(136, true);", false)]
    [InlineData("    const title = \"Half the Battle\";", false)] // guard control
    [InlineData("    const title = \"Half the Battle\"; var x = (Half)y;", true)] // guard control
    public void TheGuardCatchesEachHalfPrecisionFormAndPassesF32Code(string line, bool violates) =>
        Assert.Equal(violates, Violates(line));
}
