using System.Text.Json;

namespace Ownership;

internal static class CurrentOwnerMembershipOracles
{
    private enum Attack { ReversedRange, OverflowRange, UnknownSyntax, MissingTable, MalformedHeading, MissingCatalogueOwner, MalformedFixture, MissingFixtureAnchor, DuplicateFixtureTable, DuplicateFixture, ConflictingFixture, EmptyDocument }
    public static object Run(string? root = null)
    {
        const string engine = """
            ## ENGINE-CORE-1
            ## Disposition of every P0 ID
            | ID | Staged at | Current disposition |
            | --- | --- | --- |
            | P0-001–003 | Current | Current |
            | P0-022 / P0-023 | Current | Current |
            """;
        const string consumers = """
            ## CAT-001-I Ball
            Source criteria CAT-001-D, S010-D and S999-I are prose, not owner declarations.
            ## Exact fixture proof children
            | Canonical owner | Current level/instance | Kind / locked | Consumer proof | Source criteria |
            | --- | --- | --- | --- | --- |
            | <a id="fix-001-001"></a>FIX-001-001 | first/ball | ball / True | CAT-001-V | FIX-001-001 |
            """;
        const string scope = "## S269 Current owner\n### LAW-FIELD-I Grouped owner\nGrouped LAW-FIELD-F criterion only.\n## ANIM-1\n## MOBILE-02\n## MOBILE-03\n## MOBILE-05\n## MOBILE-06\n## OPT-SLEEP\n## LEVEL-001\n## ELEMENT-n Generic placeholder";
        var owners = CurrentOwnerMembership.Parse(engine, consumers, [scope, scope]);
        var expected = new WorkId[] { new("ENGINE-CORE-1"), new("P0-001"), new("P0-002"), new("P0-003"), new("P0-022"), new("P0-023"), new("CAT-001-I"), new("CAT-001-V"), new("FIX-001-001"), new("S269"), new("LAW-FIELD-I"), new("ANIM-1"), new("MOBILE-02"), new("MOBILE-03"), new("MOBILE-05"), new("MOBILE-06"), new("OPT-SLEEP"), new("LEVEL-001") };
        if (!owners.SetEquals(expected)) throw new InvalidOperationException("Current owner membership differs from explicit declarations.");
        var rejected = new List<Attack>();
        foreach (var attack in Enum.GetValues<Attack>())
        {
            var engineInput = engine;
            var consumerInput = consumers;
            var scopeInput = scope;
            switch (attack)
            {
                case Attack.ReversedRange: engineInput = engine.Replace("001–003", "003–001"); break;
                case Attack.OverflowRange: engineInput = engine.Replace("001–003", "001–1000"); break;
                case Attack.UnknownSyntax: engineInput = engine.Replace("001–003", "001..003"); break;
                case Attack.MissingTable: engineInput = "## ENGINE-CORE-1"; break;
                case Attack.MalformedHeading: scopeInput = "## S269-? Invalid"; break;
                case Attack.MissingCatalogueOwner: consumerInput = consumers.Replace("## CAT-001-I Ball", "## CAT-002-I Ball"); break;
                case Attack.MalformedFixture: consumerInput = consumers.Replace("id=\"fix-001-001\"", "id=\"fix-001-002\""); break;
                case Attack.MissingFixtureAnchor: consumerInput = consumers.Replace("<a id=\"fix-001-001\"></a>", ""); break;
                case Attack.DuplicateFixtureTable: consumerInput = consumers + "\n## Exact fixture proof children"; break;
                case Attack.DuplicateFixture: consumerInput = consumers + "\n" + consumers.Split('\n').Last(); break;
                case Attack.ConflictingFixture: consumerInput = "## CAT-002-I Other\n" + consumers + "\n" + consumers.Split('\n').Last().Replace("CAT-001-V", "CAT-002-V"); break;
                case Attack.EmptyDocument: scopeInput = ""; break;
                default: throw new InvalidOperationException("Missing current owner rejection control.");
            }
            try { _ = CurrentOwnerMembership.Parse(engineInput, consumerInput, [scopeInput]); }
            catch (InvalidDataException) { rejected.Add(attack); }
            if (!rejected.Contains(attack)) throw new InvalidOperationException($"Current owner boundary accepted {attack}.");
        }
        var source = new SourceInput(new("engine/presentation/AnimationBatch.cs"), new string('a', 64));
        var member = new StateMember(new("F:CuriousContraptions.Presentation.AnimationBatch._free"), source.Path, 4,
            new("System.Double[]"), StorageForm.Field, StorageMutability.ReferencedStorage, false, []);
        var assignment = new OwnershipAssignment(member.Id, AssemblyOwner.AnimationKernel, new("P0-022"),
            OwnershipRule.AnimationAuthority, OwnershipAudit.Fingerprint(member));
        var snapshot = new OwnershipSnapshot([source], [member]);
        var contract = new OwnershipContract([source], [assignment]);
        var unchanged = JsonSerializer.Serialize(contract, OwnershipAudit.Json);
        OwnershipAudit.Validate(snapshot, contract, owners);
        var absentOwnerRejected = false;
        try { OwnershipAudit.Validate(snapshot, contract with { Assignments = [assignment with { Migration = new("S010-D") }] }, owners); }
        catch (InvalidDataException) { absentOwnerRejected = true; }
        if (!absentOwnerRejected || JsonSerializer.Serialize(contract, OwnershipAudit.Json) != unchanged)
            throw new InvalidOperationException("Membership changed assignments or admitted an undeclared historical owner.");
        var actualHeadingCount = 0;
        if (root is not null)
        {
            var actual = CurrentOwnerMembership.Read(root);
            foreach (var name in new[] { "engine", "gpu-physics", "refinements", "decisions", "scope-corrections", "current-consumers", "campaign" })
            {
                var document = File.ReadAllText(Path.Combine(root, "docs/planning/invest", name + ".md"));
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(document, @"(?m)^#{2,3} ([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*)(?=\s|$)"))
                {
                    if (!match.Groups[1].Value.Any(char.IsDigit) && !match.Groups[1].Value.Contains('-')) continue;
                    if (!actual.Contains(new(match.Groups[1].Value))) throw new InvalidOperationException("Actual declared heading owner omitted: " + match.Groups[1].Value);
                    actualHeadingCount++;
                }
            }
            if (actualHeadingCount != 543 || actual.Contains(new("ELEMENT-n")) || actual.Contains(new("S010-D")))
                throw new InvalidOperationException("Actual authority heading census or non-owner rejection changed.");
        }
        return new { ActualHeadingCount = actualHeadingCount, Positive = true, RepeatedDeclarationsCoalesced = true, NegativeCount = rejected.Count,
            Rejected = rejected, AbsentHistoricalOwnerRejected = absentOwnerRejected, AssignmentsUnchanged = true };
    }
}

