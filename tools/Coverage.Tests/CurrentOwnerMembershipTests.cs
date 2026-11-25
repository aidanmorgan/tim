using System.Globalization;
using System.Text.RegularExpressions;
using CuriousContraptions.Coverage;
using Ownership;
using Xunit;

namespace CuriousContraptions.CoverageTests;

/// <summary>A lettered campaign level (LEVEL-069c) is a declared current owner, and no other spelling is.</summary>
public class CurrentOwnerMembershipTests
{
    private const string InvestPath = "docs/planning/invest";
    private static readonly string[] InvestDocuments =
        ["engine", "gpu-physics", "refinements", "decisions", "scope-corrections", "current-consumers", "campaign"];

    // Minimal valid engine and consumers documents, shaped like the ownership oracle's fixtures; no numbered LEVEL- heading.
    private const string Engine = """
        ## ENGINE-CORE-1
        ## Disposition of every P0 ID
        | ID | Staged at | Current disposition |
        | --- | --- | --- |
        | P0-001 | Current | Current |
        """;
    private const string Consumers = """
        ## CAT-001-I Ball
        ## Exact fixture proof children
        | Canonical owner | Current level/instance | Kind / locked | Consumer proof | Source criteria |
        | --- | --- | --- | --- | --- |
        | <a id="fix-001-001"></a>FIX-001-001 | first/ball | ball / True | CAT-001-V | FIX-001-001 |
        """;
    private static readonly string[] FixtureOwners = ["CAT-001-I", "CAT-001-V", "ENGINE-CORE-1", "FIX-001-001", "P0-001"];

    private const string ClauseLink = "**Exact source clauses:** [sequence-task-265](../requirements.md#sequence-task-265).";
    private static readonly SourceKey ClauseKey = new(RequirementOrigin.Task, new("sequence-task-265"));

    [Fact]
    public void ALetteredLevelHeadingInTheCampaignDocumentIsADeclaredOwner()
    {
        var root = CopyInvestDocuments();
        try
        {
            var unmodified = CurrentOwnerRelations.Read(root).Members;
            var campaign = Path.Combine(root.FullName, InvestPath, "campaign.md");
            var text = File.ReadAllText(campaign);
            const string nextSlot = "<a id=\"level-070\"></a>";
            Assert.Single(Regex.Matches(text, Regex.Escape(nextSlot)));
            // 'z' is outside the planned letters a to e, so no authored heading can ever coincide with it.
            File.WriteAllText(campaign, text.Replace(nextSlot, "<a id=\"level-069z\"></a>\n### LEVEL-069z\n\n" + nextSlot));
            var inserted = CurrentOwnerRelations.Read(root).Members;

            var lettered = new WorkOrderId("LEVEL-069z");
            var numbered = Enumerable.Range(1, 150)
                .Select(slot => new WorkOrderId("LEVEL-" + slot.ToString("D3", CultureInfo.InvariantCulture)));
            Assert.All(numbered, level => Assert.Contains(level, unmodified));
            Assert.DoesNotContain(lettered, unmodified);
            // Whole-set equality: an alias to LEVEL-069 or any other added or lost owner fails it.
            Assert.Equal(Sorted(unmodified.Append(lettered).Select(id => id.Value)), Sorted(inserted.Select(id => id.Value)));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void TheCampaignDocumentDeclaresExactlyThePlacedLetteredLevels()
    {
        // Owner decisions 24d to 24f: the 21 lettered levels that hold a kept authored level (campaign coverage, section 5),
        // and the 122 fixture rows of the 34 kept levels; the removed wall_return's only Bumper row is gone with its proof.
        string[] placed =
        [
            "LEVEL-003a", "LEVEL-009a", "LEVEL-011a", "LEVEL-011b", "LEVEL-011c", "LEVEL-011d", "LEVEL-012a", "LEVEL-012b",
            "LEVEL-012c", "LEVEL-014a", "LEVEL-014b", "LEVEL-014c", "LEVEL-019a", "LEVEL-019b", "LEVEL-021a", "LEVEL-022a",
            "LEVEL-023a", "LEVEL-041a", "LEVEL-051a", "LEVEL-051b", "LEVEL-051c",
        ];
        var members = CurrentOwnerMembership.Read(RepositoryRoot().FullName).Select(id => id.Value).ToArray();

        Assert.Equal(Sorted(placed), Sorted(members.Where(id => Regex.IsMatch(id, @"\ALEVEL-[0-9]{3}[a-z]\z"))));
        Assert.Equal(122, members.Count(id => id.StartsWith("FIX-", StringComparison.Ordinal)));
        Assert.DoesNotContain("FIX-015-001", members);
        Assert.DoesNotContain("CAT-015-V", members);
    }

    [Fact]
    public void TheInvestHeadingCensusEqualsTheDeclaredCount()
    {
        // The real tree declares exactly the shared count; one more declared heading counts exactly one more.
        Assert.Equal(CurrentOwnerMembership.DeclaredHeadingCount, CurrentOwnerMembership.CountDeclaredHeadings(RepositoryRoot().FullName));
        var root = CopyInvestDocuments();
        try
        {
            var campaign = Path.Combine(root.FullName, InvestPath, "campaign.md");
            const string nextSlot = "<a id=\"level-070\"></a>";
            File.WriteAllText(campaign, File.ReadAllText(campaign).Replace(nextSlot, "<a id=\"level-069z\"></a>\n### LEVEL-069z\n\n" + nextSlot));
            Assert.Equal(CurrentOwnerMembership.DeclaredHeadingCount + 1, CurrentOwnerMembership.CountDeclaredHeadings(root.FullName));
        }
        finally { root.Delete(true); }

        // A heading the census pattern counts but no owner prefix declares is refused by name, never skipped.
        var undeclared = CopyInvestDocuments();
        try
        {
            var campaign = Path.Combine(undeclared.FullName, InvestPath, "campaign.md");
            File.WriteAllText(campaign, File.ReadAllText(campaign) + "\n### GAP-18 Not a declared owner\n");
            var error = Assert.Throws<InvalidOperationException>(() => CurrentOwnerMembership.CountDeclaredHeadings(undeclared.FullName));
            Assert.Contains("GAP-18", error.Message, StringComparison.Ordinal);
        }
        finally { undeclared.Delete(true); }
    }

    [Theory]
    [InlineData("LEVEL-001a")]
    [InlineData("LEVEL-069z")]
    [InlineData("LEVEL-150a")]
    public void AWellFormedLetteredLevelIdentityIsAdmitted(string identity)
    {
        var owners = CurrentOwnerMembership.Parse(Engine, Consumers, ["## " + identity]);
        Assert.Equal(Sorted(FixtureOwners.Append(identity)), Sorted(owners.Select(id => id.Value)));
    }

    [Theory]
    [InlineData("LEVEL-069C")]
    [InlineData("LEVEL-069aa")]
    [InlineData("LEVEL-069a1")]
    [InlineData("LEVEL-69a")]
    [InlineData("LEVEL-0069a")]
    public void AMalformedLetteredLevelIdentityIsRefusedNamingIt(string identity)
    {
        var error = Assert.Throws<InvalidDataException>(() => CurrentOwnerMembership.Parse(Engine, Consumers, ["## " + identity]));
        Assert.Equal($"Invalid current owner identity '{identity}'.", error.Message);
    }

    [Theory]
    [InlineData("LEVEL-069c", null)]
    [InlineData("LEVEL-069", "Grouped role owner differs from its current heading.")]
    [InlineData("LEVEL-069aa", "Malformed grouped role owner.")]
    [InlineData("LEVEL-069aa. Original owner, all criteria and technical stage: LEVEL-069c", "Malformed grouped role owner.")]
    [InlineData("LEVEL-069c. Original owner, all criteria and technical stage: LEVEL-069aa", "Malformed grouped role owner.")]
    [InlineData("LEVEL-069c. Original owner, all criteria and technical stage: LEVEL-069c", "Malformed grouped role owner.")]
    public void AGroupedRoleUnderALetteredLevelResolvesToThatLevel(string grantedOwner, string? refusal)
    {
        var level = new WorkOrderId("LEVEL-069c");
        var members = new HashSet<WorkOrderId> { level };
        // The grant and the clause link share one line, as the parser reads a grant only on a clause line.
        var document = "## LEVEL-069c\nOriginal owner, all criteria and technical stage: " + grantedOwner + ". " + ClauseLink;
        if (refusal is not null)
        {
            Assert.Equal(refusal, Assert.Throws<InvalidDataException>(() => CurrentOwnerRelations.Parse(members, [document])).Message);
            return;
        }
        var issues = new List<CurrentInputIssue>();
        var resolved = CurrentOwnerRelations.Parse(members, [document]).ResolveProof(ClauseKey, issues);
        Assert.Empty(issues);
        Assert.Equal(level, resolved);
    }

    private static string[] Sorted(IEnumerable<string> values) => values.Order(StringComparer.Ordinal).ToArray();

    private static DirectoryInfo RepositoryRoot()
    {
        var source = new DirectoryInfo(AppContext.BaseDirectory);
        while (source is not null && !File.Exists(Path.Combine(source.FullName, InvestPath, "campaign.md"))) source = source.Parent;
        return source ?? throw new InvalidOperationException("Actual current authority missing.");
    }

    private static DirectoryInfo CopyInvestDocuments()
    {
        var source = RepositoryRoot();
        var root = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var invest = Directory.CreateDirectory(Path.Combine(root.FullName, InvestPath));
        try
        {
            foreach (var name in InvestDocuments)
                File.Copy(Path.Combine(source.FullName, InvestPath, name + ".md"), Path.Combine(invest.FullName, name + ".md"));
            return root;
        }
        catch
        {
            root.Delete(true);
            throw;
        }
    }
}
