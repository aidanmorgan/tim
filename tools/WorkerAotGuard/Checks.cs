using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace CuriousContraptions.WorkerAotGuard;

/// <summary>One check's outcome: each finding fails the publish (its text names the WorkerAot.targets check); notes are evidence lines.</summary>
internal sealed record CheckResult(IReadOnlyList<string> Findings, IReadOnlyList<string> Notes)
{
    public bool Passed => Findings.Count == 0;
}

/// <summary>f32x4 arithmetic counts in a disassembly: per named type, and per function of those types.</summary>
internal sealed record SimdCount(IReadOnlyDictionary<string, int> PerType, IReadOnlyDictionary<string, int> PerFunction);

/// <summary>The inspections WorkerAot.targets runs on a worker publish and on the web host's final bundle.</summary>
internal static partial class Checks
{
    /// <summary>The f32x4 lane arithmetic that counts as engine SIMD (not loads, stores, splats or lane moves).</summary>
    [GeneratedRegex(@"\bf32x4\.(add|sub|mul|div|min|max|sqrt)\b", RegexOptions.CultureInvariant)]
    private static partial Regex F32x4Arithmetic();

    /// <summary>An llvm-objdump function header line: the address, then the symbol in angle brackets.</summary>
    [GeneratedRegex(@"^[0-9a-f]+ <(.+)>:$", RegexOptions.CultureInvariant)]
    private static partial Regex FunctionHeader();

    /// <summary>(b) Each named engine assembly's AOT module is in the native module: Mono's AOT module record stores the assembly
    /// name as a NUL-terminated string (an interpreter-only dotnet.native.wasm carries none of these names).</summary>
    public static CheckResult NativeModules(string nativeFile, IReadOnlyList<string> moduleNames)
    {
        ArgumentNullException.ThrowIfNull(moduleNames);
        if (moduleNames.Count == 0) return new([$"WorkerAot (b): no engine module name was given for {nativeFile}."], []);
        var bytes = File.ReadAllBytes(nativeFile);
        var missing = moduleNames.Where(name => !ContainsName(bytes, name))
            .Select(name => $"WorkerAot (b): {nativeFile} carries no AOT module for {name}.").ToArray();
        return new(missing, missing.Length == 0 ? [$"WORKER_AOT_MODULES={string.Join(',', moduleNames)} in {nativeFile}"] : []);
    }

    /// <summary>True when <paramref name="name"/> occurs as a whole string: a NUL after it and a non-printable byte before it.</summary>
    internal static bool ContainsName(ReadOnlySpan<byte> bytes, string name)
    {
        var needle = Encoding.ASCII.GetBytes(name + "\0");
        for (var offset = 0; offset <= bytes.Length - needle.Length;)
        {
            var found = bytes[offset..].IndexOf(needle);
            if (found < 0) return false;
            var at = offset + found;
            if (at == 0 || bytes[at - 1] < 0x20) return true;
            offset = at + 1;
        }
        return false;
    }

    /// <summary>(c) Each engine assembly the native link used is the IL-stripped one: ILStrip rewrites the compiled method bodies in
    /// place (the file keeps its size), so an assembly identical to its AOT input still carries its IL.</summary>
    public static CheckResult IlStripped(IReadOnlyList<(string Stripped, string AotInput)> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        if (pairs.Count == 0) return new(["WorkerAot (c): no engine assembly was given to check for stripped IL."], []);
        var findings = new List<string>();
        var notes = new List<string>();
        foreach (var (stripped, input) in pairs)
        {
            if (!File.Exists(stripped) || !File.Exists(input))
            {
                findings.Add($"WorkerAot (c): {stripped} or its AOT input {input} is missing.");
                continue;
            }
            var after = File.ReadAllBytes(stripped);
            var before = File.ReadAllBytes(input);
            var rewritten = 0;
            for (var index = 0; index < Math.Min(after.Length, before.Length); index++)
                if (after[index] != before[index]) rewritten++;
            if (rewritten == 0 && after.Length == before.Length) findings.Add($"WorkerAot (c): {stripped} still carries its IL (identical to {input}).");
            else notes.Add(FormattableString.Invariant($"WORKER_AOT_IL_STRIPPED {Path.GetFileName(stripped)}: {rewritten} of {before.Length} bytes rewritten"));
        }
        return new(findings, notes);
    }

    /// <summary>Counts f32x4 arithmetic per function whose symbol is <paramref name="symbolPrefix"/>CuriousContraptions_{namespace}_{type}_…
    /// for one of <paramref name="types"/> (an exact type segment: SoftConstraintCoefficients is not SoftConstraint).</summary>
    public static SimdCount CountSimd(IEnumerable<string> disassembly, string symbolPrefix, IReadOnlyList<string> types)
    {
        ArgumentNullException.ThrowIfNull(disassembly);
        ArgumentNullException.ThrowIfNull(types);
        var perType = types.ToDictionary(type => type, _ => 0, StringComparer.Ordinal);
        var perFunction = new SortedDictionary<string, int>(StringComparer.Ordinal);
        string? owner = null, function = null;
        foreach (var line in disassembly)
        {
            var header = FunctionHeader().Match(line);
            if (header.Success)
            {
                function = header.Groups[1].Value;
                owner = OwnerType(function, symbolPrefix, types);
                continue;
            }
            if (owner is null || !F32x4Arithmetic().IsMatch(line)) continue;
            perType[owner]++;
            var key = $"{owner} {function}";
            perFunction[key] = perFunction.GetValueOrDefault(key) + 1;
        }
        return new(perType, perFunction);
    }

    private static string? OwnerType(string symbol, string symbolPrefix, IReadOnlyList<string> types)
    {
        var root = symbolPrefix + "CuriousContraptions_";
        if (!symbol.StartsWith(root, StringComparison.Ordinal)) return null;
        var afterNamespace = symbol.IndexOf('_', root.Length);
        if (afterNamespace < 0) return null;
        var rest = symbol[(afterNamespace + 1)..];
        return types.FirstOrDefault(type => rest.StartsWith(type + "_", StringComparison.Ordinal));
    }

    /// <summary>(d) The engine's AOT object has f32x4 arithmetic in every named physics type; the per-function counts go to
    /// <paramref name="reportFile"/>.</summary>
    public static CheckResult Simd(string objdump, string objectFile, string symbolPrefix, string reportFile, IReadOnlyList<string> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        if (!File.Exists(objdump)) return new([$"WorkerAot (d): llvm-objdump is missing at {objdump}."], []);
        if (!File.Exists(objectFile)) return new([$"WorkerAot (d): the engine AOT object {objectFile} is missing."], []);
        if (types.Count == 0) return new(["WorkerAot (d): no physics type was named for the SIMD check."], []);
        var start = new ProcessStartInfo(objdump) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-d");
        start.ArgumentList.Add(objectFile);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"{objdump} did not start.");
        var errors = process.StandardError.ReadToEndAsync();
        var count = CountSimd(Lines(process.StandardOutput), symbolPrefix, types);
        process.WaitForExit();
        if (process.ExitCode != 0) return new([FormattableString.Invariant($"WorkerAot (d): llvm-objdump exited {process.ExitCode}: {errors.Result}")], []);
        File.WriteAllLines(reportFile, count.PerFunction.Select(pair => FormattableString.Invariant($"{pair.Value} {pair.Key}")));
        var findings = count.PerType.Where(pair => pair.Value == 0)
            .Select(pair => $"WorkerAot (d): no f32x4 arithmetic in any {pair.Key} function of {objectFile}.").ToArray();
        var notes = count.PerType.Select(pair => FormattableString.Invariant($"WORKER_AOT_SIMD_INSTRUCTIONS[{pair.Key}]={pair.Value}"))
            .Append(FormattableString.Invariant($"WORKER_AOT_SIMD_INSTRUCTIONS={count.PerType.Values.Sum()} (per function: {reportFile})")).ToArray();
        return new(findings, notes);
    }

    private static IEnumerable<string> Lines(StreamReader reader)
    {
        while (reader.ReadLine() is { } line) yield return line;
    }

    /// <summary>The host bundle's copy of each verified worker file is byte-identical, and each .br/.gz sibling of the copy
    /// decompresses to it (a stale sibling would ship the previous bundle to a client that accepts compression).</summary>
    public static CheckResult HostCopies(IReadOnlyList<(string Source, string Copy)> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0) return new(["WorkerAot (host): no worker bundle file was given to check in the host bundle."], []);
        var findings = new List<string>();
        var checkedFiles = 0;
        foreach (var (source, copy) in files)
        {
            var expected = File.ReadAllBytes(source);
            if (!File.Exists(copy) || !File.ReadAllBytes(copy).AsSpan().SequenceEqual(expected))
            {
                findings.Add($"WorkerAot (host): {copy} is not the verified worker file {source}.");
                continue;
            }
            checkedFiles++;
            foreach (var suffix in Siblings.Keys)
            {
                var sibling = copy + suffix;
                if (!File.Exists(sibling)) continue;
                if (!Decompress(sibling, Siblings[suffix]).AsSpan().SequenceEqual(expected))
                    findings.Add($"WorkerAot (host): {sibling} is stale: it does not decompress to {source}.");
                checkedFiles++;
            }
        }
        return new(findings, findings.Count == 0 ? [FormattableString.Invariant($"WORKER_AOT_HOST_FILES={checkedFiles} identical to the verified worker bundles")] : []);
    }

    /// <summary>The precompressed sibling suffixes the host bundle ships.</summary>
    internal enum Compression { Brotli = 1, Gzip }

    private static readonly IReadOnlyDictionary<string, Compression> Siblings = new Dictionary<string, Compression>(StringComparer.Ordinal)
    {
        [".br"] = Compression.Brotli, [".gz"] = Compression.Gzip,
    };

    private static byte[] Decompress(string path, Compression compression)
    {
        using var input = File.OpenRead(path);
        using Stream decoded = compression switch
        {
            Compression.Brotli => new BrotliStream(input, CompressionMode.Decompress),
            Compression.Gzip => new GZipStream(input, CompressionMode.Decompress),
            _ => throw new ArgumentOutOfRangeException(nameof(compression), compression, "Undefined compression."),
        };
        using var output = new MemoryStream();
        decoded.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>Reads a pair list file: one "first|second" line per pair (the format WorkerAot.targets writes).</summary>
    public static IReadOnlyList<(string, string)> ReadPairs(string listFile) =>
        File.ReadAllLines(listFile).Where(line => line.Length > 0).Select(line =>
        {
            var parts = line.Split('|');
            return parts.Length == 2 ? (parts[0], parts[1])
                : throw new FormatException(string.Create(CultureInfo.InvariantCulture, $"{listFile}: '{line}' is not one 'first|second' pair."));
        }).ToArray();
}
