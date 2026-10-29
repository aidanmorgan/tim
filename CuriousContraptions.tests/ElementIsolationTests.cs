using System.Xml.Linq;
using CuriousContraptions.BundleElements;

namespace CuriousContraptions.Tests;

/// <summary>Architecture guard: the production bundle build owns package isolation.</summary>
public sealed class ElementIsolationTests
{
    private static readonly string Root = twodog.Engine.ResolveProjectDir();
    private static ElementIsolation Scanner() => new(Root, TestBundle.Entries);



    [Fact]
    public void NestedConditionalMembersKeepOriginalLocations()
    {
        var source = "using P = CuriousContraptions.PartId;\nclass Probe {\n#if DEBUG\n#if PLAYTEST\nobject x = P.Battery;\n#else\nobject y = PartId.Battery;\n#endif\n#else\nobject z = CuriousContraptions.PartId.Battery;\n#endif\n}";
        var hits = Scanner().ScanCSharp("engine/nested.cs", source);
        Assert.Collection(hits.Where(hit => hit.Rule == ElementIsolation.Rule.NamedPart).Select(hit => hit.Line).Order(),
            line => Assert.Equal(5, line), line => Assert.Equal(7, line), line => Assert.Equal(10, line));
        Assert.All(hits, hit => Assert.StartsWith("Probe.", hit.Symbol, StringComparison.Ordinal));
        Assert.Empty(Scanner().ScanCSharp("engine/control.cs", source.Replace("Battery", "Value", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("DEBUG")]
    [InlineData("PLAYTEST")]
    [InlineData("DEBUG && !PLAYTEST")]
    public void ConditionalBranchesCannotHidePackageCoupling(string condition)
    {
        var source = "class Probe {\n#if " + condition +
            "\nstring Id = \"wall\";\n#elif OTHER\nobject Part = PartId.Battery;\n#else\nvoid AddBatteryConfiguration() {}\n#endif\n}";
        var hits = Scanner().ScanCSharp("engine/probe.cs", source);
        Assert.Contains(hits, hit => hit.Rule == ElementIsolation.Rule.ElementLiteral && hit.Line == 3 && hit.Symbol == "Probe.Id");
        Assert.Contains(hits, hit => hit.Rule == ElementIsolation.Rule.NamedPart && hit.Line == 5);
        Assert.Contains(hits, hit => hit.Rule == ElementIsolation.Rule.PackageIdentifier && hit.Line == 7);
        Assert.All(hits, hit => Assert.Equal("engine/probe.cs", hit.File));
        Assert.Empty(Scanner().ScanCSharp("engine/control.cs",
            "#if " + condition + "\nclass Clock { void TickClock() {} }\n#else\n// wall WorkshopBattery\nclass Counter {}\n#endif"));
    }

    [Fact]
    public void ShippingSourcesContainNoPackageCoupling() =>
        Assert.Empty(Scanner().ScanTree());

    [Theory]
    [InlineData("engine/probe.cs", true)]
    [InlineData("ui/probe.cs", true)]
    [InlineData("CuriousContraptions.Core/probe.cs", true)]
    [InlineData("CuriousContraptions.Simulation/wwwroot/worker.ts", true)]
    [InlineData("CuriousContraptions.Animation.Worker/wwwroot/worker.ts", true)]
    [InlineData("CuriousContraptions.web/wwwroot/workshop-client.ts", true)]
    [InlineData("CuriousContraptions.Simulation/wwwroot/worker.js", false)]
    [InlineData("CuriousContraptions.web/wwwroot/workshop-client.js", false)]
    [InlineData("CuriousContraptions.Core/obj/probe.cs", false)]
    [InlineData("CuriousContraptions.tests/probe.cs", false)]
    [InlineData("elements/Wall/probe.cs", false)]
    [InlineData("content/probe.cs", false)]
    [InlineData("tools/e2e/probe.ts", false)]
    [InlineData("tools/Coverage/probe.cs", false)]
    [InlineData("docs/probe.cs", false)]
    [InlineData("_bmad-output/probe.cs", false)]
    [InlineData("archives/probe.cs", false)]
    public void SourceOwnershipIncludesProductionAndExcludesNonshippingInputs(string path, bool included) =>
        Assert.Equal(included, ElementIsolation.Included(path));

    [Theory]
    [InlineData("WorkshopBattery")]
    [InlineData("WorkshopClock")]
    [InlineData("BatterySettings")]
    [InlineData("PipeDimensions")]
    [InlineData("WallLessons")]
    [InlineData("AddBatteryConfiguration")]
    public void PackageOwnedIdentifiersAreRejectedWithALocation(string name)
    {
        var hit = Assert.Single(Scanner().ScanCSharp("engine/probe.cs", "class " + name + " {}"));
        Assert.Equal((ElementIsolation.Rule.PackageIdentifier, name, "engine/probe.cs", 1),
            (hit.Rule, hit.Token, hit.File, hit.Line));
    }

    [Fact]
    public void NamedQualifiedAndAliasedIdentitiesAreRejected()
    {
        var source = "using P = CuriousContraptions.PartId; class Probe { object a = PartId.Battery; object b = CuriousContraptions.PartId.Battery; object c = global::CuriousContraptions.PartId.Battery; object d = P.Battery; }";
        Assert.Equal(4, Scanner().ScanCSharp("engine/probe.cs", source).Count(hit => hit.Rule == ElementIsolation.Rule.NamedPart));
        Assert.Contains(Scanner().ScanCSharp("engine/probe.cs", "class Probe { object x = CosmeticCurves.Battery; }"),
            hit => hit.Token == "CosmeticCurves.Battery");
    }

    [Fact]
    public void GenericCapabilitiesCommentsAndUnrelatedStringsRemainValid()
    {
        var source = "// wall WorkshopBattery\nclass Clock { void TickClock() {} } class Counter {} class Spring {} class Probe { string Text = \"unrelated\"; void Read(dynamic instance) { var x = instance.PartId.Value; } }";
        Assert.Empty(Scanner().ScanCSharp("engine/probe.cs", source));
    }

    [Fact]
    public void EscapedLiteralsAndTypeScriptTemplateExpressionsCannotHidePackages()
    {
        Assert.Contains(Scanner().ScanCSharp("engine/probe.cs", "class Probe { string Id = \"\\u0077all\"; }"),
            hit => hit.Rule == ElementIsolation.Rule.ElementLiteral && hit.Token == "wall");
        var hits = Scanner().ScanJavaScript("CuriousContraptions.web/wwwroot/probe.ts",
            "// wall\nconst a = '\\x77all'; const b = `\\u0077all`; const c = `${PartId.Battery} ${AddBatteryConfiguration()}`;");
        Assert.Equal(2, hits.Count(hit => hit.Rule == ElementIsolation.Rule.ElementLiteral));
        Assert.Contains(hits, hit => hit.Rule == ElementIsolation.Rule.NamedPart);
        Assert.Contains(hits, hit => hit.Token == "AddBatteryConfiguration");
        Assert.All(hits, hit => Assert.Equal(2, hit.Line));
    }

    [Fact]
    public void EveryNormalBundleBuildInvokesTheGateAfterSuccessfulGeneration()
    {
        var program = File.ReadAllText(Path.Combine(Root, "tools/BundleElements/Program.cs"));
        Assert.True(program.IndexOf("generated.Errors.Count", StringComparison.Ordinal) <
            program.IndexOf("isolation.ScanTree()", StringComparison.Ordinal));
        Assert.Contains("return 1;", program, StringComparison.Ordinal);
        var project = File.ReadAllText(Path.Combine(Root, "tools/BundleElements/BundleElements.csproj"));
        Assert.Contains("Name=\"RunElementBundleGenerator\" AfterTargets=\"Build\"", project, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeAssemblyOwnsCapabilitiesAndCannotReferenceAuthoring()
    {
        var runtime = typeof(CuriousContraptions.Gpu.WorkshopCompiledScene).Assembly;
        Assert.Equal("CuriousContraptions.Core", runtime.GetName().Name);
        Assert.Same(runtime, typeof(CuriousContraptions.Gpu.WorkshopSimulation).Assembly);
        Assert.Same(runtime, typeof(CuriousContraptions.Gpu.WorkshopWireFlatBuffers).Assembly);
        Assert.NotSame(runtime, typeof(CuriousContraptions.Gpu.WorkshopSceneCompiler).Assembly);
        Assert.NotSame(runtime, typeof(PartId).Assembly);
        Assert.Empty(runtime.GetManifestResourceNames());
        Assert.DoesNotContain(runtime.GetReferencedAssemblies(), reference =>
            reference.Name is "CuriousContraptions" or "CuriousContraptions.Bundle" or "BundleElements");
        Assert.DoesNotContain(runtime.GetTypes(), type =>
            type == typeof(PartId) || type == typeof(CuriousContraptions.Gpu.WorkshopConstruction));
    }

    [Theory]
    [InlineData("CuriousContraptions.Simulation")]
    [InlineData("CuriousContraptions.Animation.Worker")]
    public void WorkerProjectClosureContainsOnlyRuntimeCapabilities(string worker)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string path)
        {
            path = Path.GetFullPath(path);
            if (!visited.Add(path)) return;
            var project = System.Xml.Linq.XDocument.Load(path);
            Assert.Empty(project.Descendants("EmbeddedResource"));
            if (Path.GetFileNameWithoutExtension(path) == worker)
                foreach (var include in project.Descendants("Compile").Attributes("Include"))
                    Assert.DoesNotContain("engine/", include.Value, StringComparison.Ordinal);
            foreach (var reference in project.Descendants("ProjectReference").Attributes("Include"))
            {
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, reference.Value));
                var name = Path.GetFileNameWithoutExtension(target);
                Assert.True(name is "CuriousContraptions.Core" or "CuriousContraptions.Animation" or
                    "CuriousContraptions.Abstractions" or "CuriousContraptions.Abstractions.Mechanical" or
                    "CuriousContraptions.Abstractions.Signal" or "CuriousContraptions.Abstractions.Electrical" or
                    "CuriousContraptions.FlatBuffers", target);
                Visit(target);
            }
        }
        Visit(Path.Combine(Root, worker, worker + ".csproj"));
        Assert.Contains(Path.Combine(Root, "CuriousContraptions.Core/CuriousContraptions.Core.csproj"), visited);
    }
}
