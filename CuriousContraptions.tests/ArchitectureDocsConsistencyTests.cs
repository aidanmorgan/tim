using System.Text.RegularExpressions;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.0: the architecture authority docs state the sole design. AGENTS.md links the isolation authority,
/// none of the five other authorities presents a retired design as current (the isolation doc alone names it, in its
/// Current status), and every relative link in the six docs resolves.</summary>
public sealed class ArchitectureDocsConsistencyTests(ITestOutputHelper output)
{
    private const string IsolationAuthority = "docs/architecture/element-package-isolation.md";

    /// <summary>The authorities that must not present a retired design as current.</summary>
    private static readonly string[] CurrentAuthorities =
    [
        "AGENTS.md",
        "_bmad-output/planning-artifacts/architecture.md",
        "docs/engine-contracts.md",
        "docs/general-engine-design.md",
        "docs/gpu-f32-physics.md",
    ];

    /// <summary>Retired designs, matched case-insensitively: the catalogue resources, the nonexistent pose-ring script and
    /// engine assembly, and the deleted worker binary16 accessors.</summary>
    private static readonly Regex[] RetiredPhrases =
    [
        // Story 9A.14 deleted the whole res://parts tree: the bundle is the sole catalogue and scenes live in their packages.
        new(@"res://parts/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\.tres\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"pose-ring\.js", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"CuriousContraptions\.Engine\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\bgetF16\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\bsetF16\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
    ];

    private static readonly Regex RelativeLink = new(@"\]\(([^)\s]+)\)", RegexOptions.CultureInvariant);

    private static string Root => twodog.Engine.ResolveProjectDir();

    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));

    /// <summary>Every retired phrase in <paramref name="text"/>, in order of appearance.</summary>
    public static IReadOnlyList<string> FindRetiredPhrases(string text) =>
        RetiredPhrases.SelectMany(pattern => pattern.Matches(text)).OrderBy(match => match.Index).Select(match => match.Value).ToList();

    [Fact]
    public void AgentsLinksTheIsolationAuthority() =>
        Assert.Contains($"]({IsolationAuthority})", Read("AGENTS.md"), StringComparison.Ordinal);

    [Fact]
    public void NoAuthorityPresentsRetiredDesignAsCurrent()
    {
        var hits = CurrentAuthorities
            .SelectMany(doc => FindRetiredPhrases(Read(doc)).Select(phrase => $"{doc}: {phrase}"))
            .ToList();
        foreach (var hit in hits) output.WriteLine(hit);
        Assert.Empty(hits);
    }

    [Fact]
    public void TheBanCheckRejectsAReinstatedPhrase() =>
        Assert.NotEmpty(FindRetiredPhrases("The registry loads its scenes from res://parts/scenes at startup."));

    [Fact]
    public void EveryRelativeLinkInTheAuthorityDocsResolves()
    {
        var broken = new List<string>();
        foreach (var doc in CurrentAuthorities.Append(IsolationAuthority))
        {
            var directory = Path.GetDirectoryName(Path.Combine(Root, doc))!;
            foreach (var target in RelativeLink.Matches(Read(doc)).Select(match => match.Groups[1].Value))
            {
                if (target.StartsWith('#') || target.Contains("://", StringComparison.Ordinal) ||
                    target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                    continue;
                var file = target.Split('#')[0];
                if (!File.Exists(Path.Combine(directory, file)) && !Directory.Exists(Path.Combine(directory, file)))
                    broken.Add($"{doc} -> {target}");
            }
        }
        foreach (var link in broken) output.WriteLine(link);
        Assert.Empty(broken);
    }
}
