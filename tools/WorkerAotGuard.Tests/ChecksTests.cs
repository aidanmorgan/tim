using System.IO.Compression;
using System.Text;
using Xunit;

namespace CuriousContraptions.WorkerAotGuard.Tests;

public sealed class ChecksTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("worker-aot-guard-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    // A native module whose data segment holds Mono AOT module names the way the AOT module record stores them.
    private static byte[] NativeModule(params string[] names) =>
        [0x00, 0x61, 0x73, 0x6d, .. names.SelectMany(name => (byte[])[0x01, .. Encoding.ASCII.GetBytes(name), 0x00]), 0x7f];

    [Fact]
    public void NativeModuleCarryingEveryEngineModulePasses()
    {
        var native = Write("dotnet.native.wasm", NativeModule("CuriousContraptions.Simulation", "CuriousContraptions.FlatBuffers"));

        var result = Checks.NativeModules(native, ["CuriousContraptions.Simulation", "CuriousContraptions.FlatBuffers"]);

        Assert.True(result.Passed);
    }

    [Fact]
    public void NativeModuleMissingAnEngineModuleIsACheckBFinding()
    {
        var native = Write("dotnet.native.wasm", NativeModule("CuriousContraptions.Simulation"));

        var result = Checks.NativeModules(native, ["CuriousContraptions.Simulation", "CuriousContraptions.FlatBuffers"]);

        Assert.Equal([$"WorkerAot (b): {native} carries no AOT module for CuriousContraptions.FlatBuffers."], result.Findings);
    }

    [Fact]
    public void ANameOnlyPresentAsAPrefixOfAnotherModuleIsNotFound()
    {
        var native = Write("dotnet.native.wasm", NativeModule("CuriousContraptions.Animation.Worker"));

        Assert.False(Checks.NativeModules(native, ["CuriousContraptions.Animation"]).Passed);
    }

    [Fact]
    public void ANameOnlyPresentAsASuffixOfAnotherStringIsNotFound()
    {
        var native = Write("dotnet.native.wasm", [0x01, .. Encoding.ASCII.GetBytes("XCuriousContraptions.Simulation"), 0x00]);

        Assert.False(Checks.NativeModules(native, ["CuriousContraptions.Simulation"]).Passed);
    }

    [Fact]
    public void NoModuleNamesIsAFinding()
    {
        var native = Write("dotnet.native.wasm", NativeModule("CuriousContraptions.Simulation"));

        Assert.False(Checks.NativeModules(native, []).Passed);
    }

    [Fact]
    public void AnAssemblyIdenticalToItsAotInputIsACheckCFinding()
    {
        var input = Write("input.dll", [1, 2, 3, 4]);
        var stripped = Write("stripped.dll", [1, 2, 3, 4]);

        var result = Checks.IlStripped([(stripped, input)]);

        Assert.Equal([$"WorkerAot (c): {stripped} still carries its IL (identical to {input})."], result.Findings);
    }

    [Fact]
    public void AnAssemblyWithRewrittenBodiesPasses()
    {
        var input = Write("input.dll", [1, 2, 3, 4]);
        var stripped = Write("stripped.dll", [1, 0, 0, 4]);

        var result = Checks.IlStripped([(stripped, input)]);

        Assert.Equal(["WORKER_AOT_IL_STRIPPED stripped.dll: 2 of 4 bytes rewritten"], result.Notes);
    }

    [Fact]
    public void AMissingStrippedAssemblyIsAFinding()
    {
        var input = Write("input.dll", [1, 2, 3, 4]);

        Assert.False(Checks.IlStripped([(Path.Combine(_directory, "absent.dll"), input)]).Passed);
    }

    private static readonly string[] Disassembly =
    [
        "00000010 <Asm_CuriousContraptions_Gpu_SoftConstraint_Solve4_Vector128>:",
        "      f32x4.mul",
        "      f32x4.add",
        "      f32x4.min",
        "      v128.load",
        "00000020 <Asm_CuriousContraptions_Gpu_SoftConstraintCoefficients_FromSpring>:",
        "      f32x4.mul",
        "00000030 <Asm_CuriousContraptions_Simulation_WorkshopPhysicsEngine_Step>:",
        "      f32x4.splat",
        "      f32x4.sqrt",
        "00000040 <Other_CuriousContraptions_Simulation_WorkshopPhysicsEngine_Step>:",
        "      f32x4.add",
    ];

    [Fact]
    public void SimdIsCountedPerExactNamedTypeOnly()
    {
        var count = Checks.CountSimd(Disassembly, "Asm_", ["SoftConstraint", "WorkshopPhysicsEngine"]);

        Assert.Equal(new Dictionary<string, int>(StringComparer.Ordinal) { ["SoftConstraint"] = 3, ["WorkshopPhysicsEngine"] = 1 }, count.PerType);
    }

    [Fact]
    public void SimdIsReportedPerFunction()
    {
        var count = Checks.CountSimd(Disassembly, "Asm_", ["SoftConstraint"]);

        Assert.Equal(3, count.PerFunction["SoftConstraint Asm_CuriousContraptions_Gpu_SoftConstraint_Solve4_Vector128"]);
    }

    [Fact]
    public void ANamedTypeWithoutArithmeticCountsZero()
    {
        var count = Checks.CountSimd(Disassembly, "Asm_", ["PrismaticConstraintSolver"]);

        Assert.Equal(0, count.PerType["PrismaticConstraintSolver"]);
    }

    [Fact]
    public void AMissingObjdumpIsACheckDFinding()
    {
        var objectFile = Write("engine.dll.o", [0]);

        var result = Checks.Simd(Path.Combine(_directory, "llvm-objdump"), objectFile, "Asm_", Path.Combine(_directory, "report.txt"), ["SoftConstraint"]);

        Assert.StartsWith("WorkerAot (d): llvm-objdump is missing", Assert.Single(result.Findings), StringComparison.Ordinal);
    }

    private static byte[] Compress(byte[] bytes, bool brotli)
    {
        using var output = new MemoryStream();
        using (Stream encoder = brotli ? new BrotliStream(output, CompressionLevel.Fastest) : new GZipStream(output, CompressionLevel.Fastest))
            encoder.Write(bytes);
        return output.ToArray();
    }

    private (string Source, string Copy) Bundle(byte[] source, byte[] copy)
    {
        Directory.CreateDirectory(Path.Combine(_directory, "host"));
        return (Write("dotnet.native.wasm", source), Write(Path.Combine("host", "dotnet.native.wasm"), copy));
    }

    [Fact]
    public void AnIdenticalHostCopyWithFreshSiblingsPasses()
    {
        var (source, copy) = Bundle([1, 2, 3], [1, 2, 3]);
        File.WriteAllBytes(copy + ".br", Compress([1, 2, 3], brotli: true));
        File.WriteAllBytes(copy + ".gz", Compress([1, 2, 3], brotli: false));

        var result = Checks.HostCopies([(source, copy)]);

        Assert.Equal(["WORKER_AOT_HOST_FILES=3 identical to the verified worker bundles"], result.Notes);
    }

    [Fact]
    public void AStaleBrotliSiblingIsAFinding()
    {
        var (source, copy) = Bundle([1, 2, 3], [1, 2, 3]);
        File.WriteAllBytes(copy + ".br", Compress([9, 9], brotli: true));

        var result = Checks.HostCopies([(source, copy)]);

        Assert.Equal([$"WorkerAot (host): {copy}.br is stale: it does not decompress to {source}."], result.Findings);
    }

    [Fact]
    public void AStaleGzipSiblingIsAFinding()
    {
        var (source, copy) = Bundle([1, 2, 3], [1, 2, 3]);
        File.WriteAllBytes(copy + ".gz", Compress([9, 9], brotli: false));

        Assert.False(Checks.HostCopies([(source, copy)]).Passed);
    }

    [Fact]
    public void AHostCopyThatDiffersIsAFinding()
    {
        var (source, copy) = Bundle([1, 2, 3], [1, 2, 4]);

        Assert.Equal([$"WorkerAot (host): {copy} is not the verified worker file {source}."], Checks.HostCopies([(source, copy)]).Findings);
    }

    [Fact]
    public void AMalformedPairLineIsRejected()
    {
        var list = Write("pairs.txt", Encoding.UTF8.GetBytes("a|b|c\n"));

        Assert.Throws<FormatException>(() => Checks.ReadPairs(list));
    }
}
