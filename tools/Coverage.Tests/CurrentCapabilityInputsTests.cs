using CuriousContraptions.Coverage;
using Xunit;

namespace CuriousContraptions.CoverageTests;

public class CurrentCapabilityInputsTests
{
    private static readonly SourcePath BallResource = new("elements/Ball/BallElementDeclaration.cs");
    private static DirectoryInfo Root()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, CurrentCatalogue.ConsumersPath.Value))) root = root.Parent;
        return root ?? throw new InvalidOperationException("Actual current authority missing.");
    }
    [Fact]
    public void RetiredImplementationClaimsDoNotRemoveCapabilityObligations()
    {
        Assert.False(CapabilityImplementationRequirements.CurrentDeclarations.ContainsKey(EngineCapability.ElectricalPower));
        Assert.False(CapabilityImplementationRequirements.CurrentDeclarations.ContainsKey(EngineCapability.JointConstraint));
        Assert.False(CapabilityImplementationRequirements.CurrentDeclarations.ContainsKey(EngineCapability.FiniteLedger));
        Assert.Contains(EngineCapability.ElectricalPower, CapabilityCatalogueRequirements.Values[new("battery")]);
        Assert.Contains(EngineCapability.FiniteLedger, CapabilityCatalogueRequirements.Values[new("battery")]);
        Assert.NotEmpty(CapabilityImplementationRequirements.CurrentDeclarations[EngineCapability.ContactImpulse]);
        Assert.Equal(Enum.GetValues<EngineCapability>().Length, CapabilityRequirements.Dependencies.Count);
    }

    [Fact]
    public void CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles()
    {
        var root = Root();
        var sources = RequirementDiscovery.Discover(root);
        var preparation = CapabilityInputs.Prepare(root, sources);
        var expected = preparation.Expectations;
        Assert.Equal(1471, sources.Count);
        Assert.Equal(72, sources.Count(source => source.Key.Origin == RequirementOrigin.Catalogue));
        Assert.Equal(302, sources.Count(source => source.Key.Origin == RequirementOrigin.Fixture));
        Assert.Equal(104, expected.Modes.Count);
        Assert.Equal(sources.Count, expected.Classifications.Count);
        Assert.NotEmpty(preparation.Issues);
        Assert.Contains(preparation.Issues, issue => issue.Problem == CurrentInputProblem.UnknownOwner && issue.Identity == "S010-D");
        Assert.Contains(preparation.Issues, issue => issue.Problem == CurrentInputProblem.AmbiguousScope);
        Assert.Contains(preparation.Issues, issue => issue.Problem == CurrentInputProblem.MissingScope);
        Assert.Empty(expected.ImplementationEligibleOwners);
        var ball = new SourceKey(RequirementOrigin.Catalogue, new("ball"));
        var planned = new SourceKey(RequirementOrigin.Catalogue, new("electrical_nor"));
        Assert.Equal(ConsumerKind.CurrentPart, expected.Classifications[ball].Consumer);
        Assert.Equal(ConsumerKind.FutureDeclaration, expected.Classifications[planned].Consumer);
        Assert.NotEmpty(expected.RequiredSymbols[ball]);
        Assert.False(expected.RequiredSymbols.ContainsKey(planned));
        Assert.Equal(CapabilityCatalogueRequirements.Values[planned.Id], expected.RequiredCapabilities[planned]);
        Assert.Equal(new WorkOrderId("CAT-001-I"), expected.SourceOwners[ball]);
        var error = Assert.Throws<InvalidDataException>(() => CapabilityInputs.Discover(root, sources));
        Assert.Contains("Current capability inputs unresolved", error.Message);
        Assert.DoesNotContain("work-register", error.Message);
    }


    [Fact]
    public void PlannedAbsentResourcesDoNotBlockCurrentInputsButAdmittedRemovalDoes()
    {
        var original = Root();
        var target = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var plan = CurrentCatalogue.Read(original);
        var paths = new HashSet<string>(StringComparer.Ordinal)
        {
            "docs/planning/requirements.md", "content/puzzles.json", CurrentCatalogue.PartIdentityPath.Value, "docs/physics-puzzle-gap-audit.md"
        };
        foreach (var name in new[] { "engine", "gpu-physics", "refinements", "decisions", "scope-corrections", "current-consumers", "campaign" })
            paths.Add("docs/planning/invest/" + name + ".md");
        foreach (var element in plan.Elements) paths.Add(element.Declaration.Path.Value);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(original.FullName, "docs"), "*research.md"))
            paths.Add(Path.GetRelativePath(original.FullName, path));
        foreach (var resource in plan.Admitted.Values)
        {
            var closure = CapabilityInputs.ReadResourceClosure(original, resource);
            foreach (var artifact in closure.Artifacts) paths.Add(artifact.Path.Value);
            foreach (var symbol in closure.Symbols) paths.Add(symbol.Path.Value);
        }
        try
        {
            foreach (var path in paths)
            {
                var destination = Path.Combine(target.FullName, path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Path.Combine(original.FullName, path), destination);
            }
            Assert.False(Directory.Exists(Path.Combine(target.FullName, "elements/ElectricalNand")));
            var sources = RequirementDiscovery.Discover(target);
            var prepared = CapabilityInputs.Prepare(target, sources);
            Assert.Equal(1471, sources.Count);
            Assert.Equal(104, prepared.Expectations.Modes.Count);
            Assert.Equal(72, prepared.Expectations.RequiredCapabilities.Keys.Count(key => key.Origin == RequirementOrigin.Catalogue));
            var springboard = Path.Combine(target.FullName, "elements/Springboard/SpringboardElementDeclaration.cs");
            var springboardText = File.ReadAllText(springboard);
            File.WriteAllText(springboard, springboardText.Replace("Id: \"spring\"", "Id: \"ball\""));
            Assert.Contains("'ball' is declared by both", Assert.Throws<InvalidDataException>(() => CurrentCatalogue.Read(target)).Message);
            File.WriteAllText(springboard, springboardText.Replace("Id: \"spring\"", "Id: \"springboard\""));
            Assert.Contains("'springboard' is not a declared part identity",
                Assert.Throws<InvalidDataException>(() => CurrentCatalogue.Read(target)).Message);
            File.WriteAllText(springboard, springboardText);
            Assert.Equal(new SourcePath("elements/Springboard/SpringboardElementDeclaration.cs"), CurrentCatalogue.Read(target).Admitted[new("spring")]);
            File.Delete(Path.Combine(target.FullName, "elements/Ball/BallElementDeclaration.cs"));
            Assert.Contains("have no element declaration: ball.", Assert.Throws<InvalidDataException>(() => CurrentCatalogue.Read(target)).Message);
        }
        finally { if (target.Exists) target.Delete(true); }
    }

    [Fact]
    public void LinkCardinalityNeverGrantsProofAndRepeatedExplicitRolesCoalesce()
    {
        var ids = new HashSet<WorkOrderId> { new("S269"), new("S270") };
        const string link = "**Exact source clauses:** [sequence-task-265](../requirements.md#sequence-task-265).";
        var key = new SourceKey(RequirementOrigin.Task, new("sequence-task-265"));
        var single = CurrentOwnerRelations.Parse(ids, ["## S269\n" + link]);
        var issues = new List<CurrentInputIssue>();
        Assert.Null(single.ResolveProof(key, issues));
        Assert.Equal(CurrentInputProblem.UnresolvedProofRole, Assert.Single(issues).Problem);
        var grant = "## S269\nOriginal owner, all criteria and technical stage: S269. " + link;
        var explicitRole = CurrentOwnerRelations.Parse(ids, [grant, grant]);
        issues.Clear();
        Assert.Equal(new WorkOrderId("S269"), explicitRole.ResolveProof(key, issues));
        Assert.Empty(issues);
        var multiple = CurrentOwnerRelations.Parse(ids, [grant, "## S270\n" + link]);
        issues.Clear();
        Assert.Null(multiple.ResolveProof(key, issues));
        Assert.Equal(CurrentInputProblem.AmbiguousScope, Assert.Single(issues).Problem);
        Assert.Equal(2, issues[0].Candidates.Length);
        Assert.Throws<InvalidDataException>(() => CurrentOwnerRelations.Parse(ids, [grant.Replace("stage: S269", "stage: S270")]));
        Assert.Throws<InvalidDataException>(() => CurrentOwnerRelations.Parse(ids, [link]));
    }

    [Fact]
    public void AdmittedResourceClosureRejectsMissingDuplicateAndEscapingLinksAndBindsBytes()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "elements/Ball"));
        Directory.CreateDirectory(Path.Combine(directory, "engine"));
        var root = new DirectoryInfo(directory);
        var resource = Path.Combine(directory, BallResource.Value);
        var scene = Path.Combine(directory, "elements/Ball/ball.tscn");
        var script = Path.Combine(directory, "engine/MachinePart.cs");
        const string resourceText = "Id: \"ball\",\nScenePath: \"res://elements/Ball/ball.tscn\",";
        const string sceneText = "[ext_resource type=\"Script\" path=\"res://engine/MachinePart.cs\" id=\"1\"]\n[node name=\"ball\" type=\"Node3D\"]\nscript = ExtResource(\"1\")";
        try
        {
            File.WriteAllText(resource, resourceText);
            File.WriteAllText(scene, sceneText);
            File.WriteAllText(script, "public class MachinePart {}");
            var first = CapabilityInputs.ReadResourceClosure(root, BallResource);
            Assert.Equal(2, first.Artifacts.Length);
            Assert.Single(first.Symbols);
            File.WriteAllText(script, "public class MachinePart { public float Mass; }");
            Assert.NotEqual(first.Symbols[0].FileHash, CapabilityInputs.ReadResourceClosure(root, BallResource).Symbols[0].FileHash);
            File.WriteAllText(scene, sceneText + "\n# authored change");
            Assert.NotEqual(first.Artifacts[1].Hash, CapabilityInputs.ReadResourceClosure(root, BallResource).Artifacts[1].Hash);
            File.WriteAllText(resource, resourceText + "\n" + resourceText);
            Assert.Throws<InvalidDataException>(() => CapabilityInputs.ReadResourceClosure(root, BallResource));
            File.WriteAllText(resource, resourceText.Replace("elements/Ball/ball.tscn", "../outside.tscn"));
            Assert.Throws<ArgumentException>(() => CapabilityInputs.ReadResourceClosure(root, BallResource)); // SourcePath refuses it
            File.WriteAllText(resource, resourceText);
            File.Delete(scene);
            Assert.Throws<FileNotFoundException>(() => CapabilityInputs.ReadResourceClosure(root, BallResource));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(false, "ScenePath: \"res://elements/Ball/ball.tscn\",\nScenePath: \"res://elements/Ball/ball.tscn\",")]
    [InlineData(false, "ScenePath: \"res://elements/Ball/ball.svg\",")]
    [InlineData(true, "")]
    [InlineData(true, "script = ExtResource(\"unknown\")")]
    [InlineData(true, "script = ExtResource(\"1\")\nscript = ExtResource(\"1\")")]
    [InlineData(true, "script = null")]
    public void ActualResourcePropertyBindingMustResolve(bool sceneBinding, string binding)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "elements/Ball"));
        Directory.CreateDirectory(Path.Combine(directory, "engine"));
        try
        {
            File.WriteAllText(Path.Combine(directory, BallResource.Value),
                "Id: \"ball\",\n" + (sceneBinding ? "ScenePath: \"res://elements/Ball/ball.tscn\"," : binding));
            File.WriteAllText(Path.Combine(directory, "elements/Ball/ball.tscn"),
                "[ext_resource type=\"Script\" path=\"res://engine/MachinePart.cs\" id=\"1\"]\n[node name=\"ball\" type=\"Node3D\"]\n" +
                (sceneBinding ? binding : "script = ExtResource(\"1\")"));
            File.WriteAllText(Path.Combine(directory, "engine/MachinePart.cs"), "public class MachinePart {}");
            Assert.Throws<InvalidDataException>(() => CapabilityInputs.ReadResourceClosure(new(directory), BallResource));
        }
        finally { Directory.Delete(directory, true); }
    }
}
