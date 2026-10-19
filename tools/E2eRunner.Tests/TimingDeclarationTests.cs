using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The typed timing declaration against the real tree (AC2 (a)-(f)), plus the audit table the reviewer checks.</summary>
public sealed partial class TimingDeclarationTests(ITestOutputHelper output)
{
    private static readonly Dictionary<SuiteId, IReadOnlyList<TimingHit>> Hits = SuiteCatalogue.All.ToDictionary(
        declaration => declaration.Id, declaration => TimingDetector.ScanSuite(TestRepository.Root, SuiteCatalogue.RelativePath(declaration.Id)));

    private static void AssertNone(IEnumerable<string> violations)
    {
        var all = violations.ToArray();
        if (all.Length > 0) Assert.Fail($"{all.Length} timing declaration violation(s):\n" + string.Join('\n', all));
    }

    [Fact]
    public void CatalogueAndSuiteFilesAreABijection()
    {
        var files = TestRepository.ListFiles("tools/e2e").Where(SuiteCatalogue.IsSuitePath).ToArray();
        var declared = SuiteCatalogue.All.Select(declaration => SuiteCatalogue.RelativePath(declaration.Id)).ToArray();
        AssertNone(files.Except(declared).Select(file => $"{file}: undeclared suite (add a SuiteId and a catalogue entry)")
            .Concat(declared.Except(files).Select(file => $"{file}: declared but missing"))
            .Concat(Enum.GetValues<SuiteId>().Where(id => SuiteCatalogue.All.Count(declaration => declaration.Id == id) != 1)
                .Select(id => $"{id}: declared {SuiteCatalogue.All.Count(declaration => declaration.Id == id)} times")));
    }

    [Fact]
    public void ParallelSuitesHaveNoTimingDetectorHit() =>
        AssertNone(SuiteCatalogue.All.Where(declaration => declaration.Timing == SuiteTiming.Parallel)
            .SelectMany(declaration => Hits[declaration.Id].Where(hit => TimingDetector.ReasonFor(hit.Rule) is not null)
                .Select(hit => $"{SuiteCatalogue.FileName(declaration.Id)} is Parallel but {hit}")));

    [Fact]
    public void NoSuiteUsesUnscannableCode() =>
        AssertNone(Hits.SelectMany(entry => entry.Value.Where(hit => hit.Rule == TimingRule.UnscannableCode)
            .Select(hit => $"{SuiteCatalogue.FileName(entry.Key)}: {hit}")));

    [Theory]
    [InlineData("tools/e2e/clock-master.test.ts")]
    [InlineData("tools/e2e/cat-017.test.ts")]
    [InlineData("tools/e2e/cat-023b.test.ts")]
    public void OwnerNamedSuitesAreSerialAlone(string path)
    {
        var declaration = SuiteCatalogue.Get(SuiteCatalogue.TryParsePath(path)!.Value);
        Assert.Equal(SuiteTiming.SerialAlone, declaration.Timing);
        Assert.Contains(declaration.Reasons, reason => reason.Reason == TimingReason.OwnerNamed);
    }

    [Fact]
    public void EverySerialAloneSuiteIsJustified()
    {
        var violations = new List<string>();
        foreach (var declaration in SuiteCatalogue.All)
        {
            var name = SuiteCatalogue.FileName(declaration.Id);
            var lines = TestRepository.Read(SuiteCatalogue.RelativePath(declaration.Id)).Split('\n').Length;
            var detected = Hits[declaration.Id].Select(hit => TimingDetector.ReasonFor(hit.Rule)).OfType<TimingReason>().ToHashSet();
            var declared = declaration.Reasons.Select(reason => reason.Reason).ToHashSet();
            if (declaration.Timing == SuiteTiming.Parallel)
            {
                if (declaration.Reasons.Count > 0) violations.Add($"{name}: Parallel with timing reasons");
                continue;
            }
            // SerialAlone is the default (a suite runs alone unless the catalogue marks it Parallel), so a reason is optional; a declared
            // reason must be justified, cite a line in the file, and (for the mechanical rules) be backed by a detector hit.
            foreach (var reason in declaration.Reasons)
            {
                if (string.IsNullOrWhiteSpace(reason.Justification)) violations.Add($"{name}: {reason.Reason} has no justification");
                if (reason.Line < 1 || reason.Line > lines) violations.Add($"{name}: {reason.Reason} cites line {reason.Line} outside the file");
            }
            violations.AddRange(declared.Where(reason => reason is not (TimingReason.AssertsWallClockRate or TimingReason.OwnerNamed))
                .Except(detected).Select(reason => $"{name}: declares {reason} but the detector finds no such rule hit"));
        }
        AssertNone(violations);
    }

    [GeneratedRegex(@"\b(selectTool|stackUnderLiftedBall|selectLevel)\s*\(")]
    private static partial Regex PlacingCall();
    [GeneratedRegex(@"['""]([^'""]*)['""]")]
    private static partial Regex StringLiteral();
    [GeneratedRegex(@"\.\s*([A-Za-z]\w*)\s*\(")]
    private static partial Regex MethodCall();

    [Fact]
    public void LiteralPlacementsAndLevelsAreDeclared()
    {
        var violations = new List<string>();
        foreach (var declaration in SuiteCatalogue.All)
        {
            var name = SuiteCatalogue.FileName(declaration.Id);
            var source = new ScannedSource(TestRepository.Read(SuiteCatalogue.RelativePath(declaration.Id)));
            foreach (Match call in PlacingCall().Matches(source.Code))
            {
                var open = call.Index + call.Length - 1;
                var close = open;
                for (var depth = 0; close < source.Code.Length; close++)
                    if (source.Code[close] == '(') depth++;
                    else if (source.Code[close] == ')' && --depth == 0) break;
                foreach (Match literal in StringLiteral().Matches(source.Original[open..close]))
                {
                    if (!source.IsCode(open + literal.Index)) continue;
                    var value = literal.Groups[1].Value;
                    if (call.Groups[1].Value == DriverVocabulary.LevelMethod)
                    {
                        if (new LevelId(value) != LevelId.FreeWorkshop && !declaration.Levels.Contains(new LevelId(value)))
                            violations.Add($"{name}:{source.LineOf(call.Index)} selects level {value}, not declared in Levels");
                    }
                    else if (!DriverVocabulary.ToolKinds.TryGetValue(value, out var part))
                        violations.Add($"{name}:{source.LineOf(call.Index)} selects unknown tool kind '{value}'");
                    else if (declaration.Scope != PlacementScope.EveryPalettePart && !declaration.Places.Contains(part))
                        violations.Add($"{name}:{source.LineOf(call.Index)} places {part}, not declared in Places");
                }
            }
            foreach (Match call in MethodCall().Matches(source.Code))
                if (DriverVocabulary.MethodAreas.TryGetValue(call.Groups[1].Value, out var area) && !declaration.Areas.Contains(area))
                    violations.Add($"{name}:{source.LineOf(call.Index)} calls {call.Groups[1].Value} ({area}), not declared in Areas");
        }
        AssertNone(violations.Distinct());
    }

    [Fact]
    public void DeclaredLevelsExist()
    {
        using var document = JsonDocument.Parse(TestRepository.Read("content/puzzles.json"));
        var levels = document.RootElement.EnumerateArray().Select(level => new LevelId(level.GetProperty("id").GetString()!)).Append(LevelId.FreeWorkshop).ToHashSet();
        AssertNone(SuiteCatalogue.All.SelectMany(declaration => declaration.Levels.Where(level => !levels.Contains(level))
            .Select(level => $"{SuiteCatalogue.FileName(declaration.Id)} declares unknown level {level}")));
    }

    [Fact]
    public void ElementPackagesMatchDirectoriesAndBundle()
    {
        var directories = Directory.EnumerateDirectories(Path.Combine(TestRepository.Root, "elements")).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal);
        Assert.Equal(directories, ImpactRules.ElementPackages.Keys.Order(StringComparer.Ordinal));
        var bundle = ImpactContext.BundleElements(TestRepository.Read(ImpactContext.BundlePath));
        Assert.NotNull(bundle);
        Assert.Equal(bundle.Select(element => element.Part).Order(), ImpactRules.ElementPackages.Values.Order());
    }

    [Fact]
    public void AuditTableCoversEverySuite()
    {
        var rows = SuiteCatalogue.All.Select(declaration =>
            $"| {SuiteCatalogue.FileName(declaration.Id)} | {declaration.Timing} | " +
            $"{string.Join("<br>", Hits[declaration.Id].Select(hit => $"{hit.Rule} {hit.File}:{hit.Line}"))} | " +
            $"{string.Join("<br>", declaration.Reasons.Select(reason => $"{reason.Reason} :{reason.Line}"))} | " +
            $"{declaration.Primary?.Value ?? "-"} | {(declaration.Scope == PlacementScope.EveryPalettePart ? "every palette part" : string.Join(",", declaration.Places.Order()))} | " +
            $"{string.Join(",", declaration.Levels.Select(level => level.Value).Order(StringComparer.Ordinal))} | {string.Join(",", declaration.Wires.Order())} | " +
            $"{string.Join(",", declaration.Areas.Order())} |").ToArray();
        output.WriteLine("| Suite | Timing | Rule hits (file:line) | Declared reasons (:line) | Primary | Places | Levels | Wires | Areas |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var row in rows) output.WriteLine(row);
        Assert.Equal(Enum.GetValues<SuiteId>().Length, rows.Length);
    }
}
