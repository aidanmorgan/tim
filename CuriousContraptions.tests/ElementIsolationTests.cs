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
    [InlineData("WorkshopSystemClock")]
    [InlineData("BatterySettings")]
    [InlineData("PipeDimensions")]
    [InlineData("WallLessons")]
    [InlineData("AddBatteryConfiguration")]
    [InlineData("BatterySolver")]
    [InlineData("batterySolver")]
    [InlineData("IBatterySolver")]
    [InlineData("PulleyWheelRead")]
    [InlineData("pulleyWheelRead")]
    [InlineData("CounterReached")]
    [InlineData("counterReached")]
    [InlineData("WorkshopPulley")]
    [InlineData("ApplyBatteryConfiguration")]
    [InlineData("Pulleys")]
    [InlineData("pulleys")]
    [InlineData("Batteries")]
    [InlineData("batteries")]
    [InlineData("Counters")]
    [InlineData("counters")]
    [InlineData("Dominoes")]
    [InlineData("dominoes")]
    [InlineData("BallDetectors")]
    [InlineData("ballDetectors")]
    public void PackageOwnedIdentifiersAreRejectedWithALocation(string name)
    {
        var hit = Assert.Single(Scanner().ScanCSharp("engine/probe.cs", "class " + name + " {}"));
        Assert.Equal((ElementIsolation.Rule.PackageIdentifier, name, "engine/probe.cs", 1),
            (hit.Rule, hit.Token, hit.File, hit.Line));
    }

    [Fact]
    public void ProhibitedElementUsingDirectivesAreRejected()
    {
        var source = "using CuriousContraptions.Elements.Battery;\nclass Probe {}";
        var hit = Assert.Single(Scanner().ScanCSharp("engine/probe.cs", source));
        Assert.Equal(ElementIsolation.Rule.PackageIdentifier, hit.Rule);
        Assert.Equal("CuriousContraptions.Elements.Battery", hit.Token);
        Assert.Equal(1, hit.Line);
    }

    [Fact]
    public void ProhibitedTypeScriptFixturesAreRejected()
    {
        var source = "function compileBattery() {}\nconst part = `${PartId.Battery}`;";
        var hits = Scanner().ScanJavaScript("CuriousContraptions.web/wwwroot/probe.ts", source);
        Assert.Contains(hits, hit => hit.Rule == ElementIsolation.Rule.PackageIdentifier && hit.Token == "compileBattery" && hit.Line == 1);
        Assert.Contains(hits, hit => hit.Rule == ElementIsolation.Rule.NamedPart && hit.Line == 2);
    }

    [Theory]
    [InlineData("import { Something } from '../../elements/Battery/Something.js';")]
    [InlineData("import '../../elements/Battery/battery.js';")]
    [InlineData("import { Battery } from './other.js';")]
    public void TypeScriptElementImportsAreRejected(string source)
    {
        var hits = Scanner().ScanJavaScript("CuriousContraptions.web/wwwroot/probe.ts", source);
        Assert.Contains(hits, hit => hit.Rule == ElementIsolation.Rule.PackageIdentifier);
    }

    [Theory]
    [InlineData("class Probe { void Run() => CuriousContraptions.Elements.Battery.SomeType.Run(); }", "CuriousContraptions.Elements.Battery.SomeType.Run")]
    [InlineData("class Probe { void Run() => global::CuriousContraptions.Elements.Battery.SomeType.Run(); }", "global::CuriousContraptions.Elements.Battery.SomeType.Run")]
    [InlineData("class Probe { object x = nameof(CuriousContraptions.Elements.Battery.SomeType); }", "CuriousContraptions.Elements.Battery.SomeType")]
    [InlineData("class Probe { global::CuriousContraptions.Elements.Battery.SomeType x; }", "global::CuriousContraptions.Elements.Battery.SomeType")]
    public void QualifiedElementNamespaceExpressionsAreRejected(string code, string expectedToken)
    {
        var hit = Assert.Single(Scanner().ScanCSharp("engine/probe.cs", code));
        Assert.Equal((ElementIsolation.Rule.PackageIdentifier, expectedToken, "engine/probe.cs", 1),
            (hit.Rule, hit.Token, hit.File, hit.Line));
    }

    [Fact]
    public void DynamicallyRegisteredPackageIsGuardedWithoutScannerChanges()
    {
        var gizmoEntry = TestBundle.Entries[0] with
        {
            Metadata = TestBundle.Entries[0].Metadata with
            {
                Id = "gizmo",
                ScenePath = "res://elements/Gizmo/Gizmo.tscn"
            }
        };
        var isolation = new ElementIsolation(Root, TestBundle.Entries.Append(gizmoEntry));
        var source = "class GizmoSolver {}\nclass GizmoWheelRead {}\nenum GizmoReached {}\nclass WorkshopGizmo {}\nclass ApplyGizmoConfiguration {}\nclass Probe { object Gizmos; }\n";
        var hits = isolation.ScanCSharp("engine/probe.cs", source);
        Assert.Equal(6, hits.Count(hit => hit.Rule == ElementIsolation.Rule.PackageIdentifier));
        Assert.Contains(hits, hit => hit.Token == "GizmoSolver");
        Assert.Contains(hits, hit => hit.Token == "GizmoWheelRead");
        Assert.Contains(hits, hit => hit.Token == "GizmoReached");
        Assert.Contains(hits, hit => hit.Token == "WorkshopGizmo");
        Assert.Contains(hits, hit => hit.Token == "ApplyGizmoConfiguration");
        Assert.Contains(hits, hit => hit.Token == "Gizmos");

        var usingHits = isolation.ScanCSharp("engine/probe.cs", "using CuriousContraptions.Elements.Gizmo;\nclass P {}");
        Assert.Contains(usingHits, hit => hit.Rule == ElementIsolation.Rule.PackageIdentifier && hit.Token == "CuriousContraptions.Elements.Gizmo");

        var tsHits = isolation.ScanJavaScript("CuriousContraptions.web/wwwroot/probe.ts", "function compileGizmo() {}\nconst x = `${PartId.Gizmo}`;");
        Assert.Contains(tsHits, hit => hit.Rule == ElementIsolation.Rule.PackageIdentifier && hit.Token == "compileGizmo");
        Assert.Contains(tsHits, hit => hit.Rule == ElementIsolation.Rule.NamedPart);
    }

    [Theory]
    [InlineData("class Probe { RopeWinding W = RopeWinding.CounterClockwise; }")]
    [InlineData("class Probe { bool S(Vector3 p) => SmoothClosestWall(p, Vector3.Zero, default); }")]
    [InlineData("class Probe { MonotonicNanoseconds? Wall => DisplayWall; }")]
    [InlineData("class Probe { ClockProbe Probe; }")]
    [InlineData("class Probe { ClockObservation Observation; }")]
    [InlineData("class Probe { void Check() { var Clocks = 1; } }")]
    [InlineData("class Probe { int latches; }")]
    [InlineData("class Probe { CaptureLatchPhase Phase; }")]
    [InlineData("class Probe { SimulationEventKind E = SimulationEventKind.ThresholdReached; }")]
    [InlineData("class Probe { SimulationEventKind E = SimulationEventKind.MemoryChanged; }")]
    [InlineData("class Probe { ActivationNodeKind K = ActivationNodeKind.Counter; }")]
    [InlineData("class Probe { ActivationNodeKind K = ActivationNodeKind.StateLatch; }")]
    [InlineData("class RevoluteConstraintSolver {}")]
    [InlineData("class TensileConstraintSolver {}")]
    [InlineData("class RopeGuideRead {}")]
    [InlineData("class RevoluteDriveTargetRead {}")]
    public void LegitimateGeometryCavityTimingAndCapabilityNamesRemainAllowed(string code)
    {
        Assert.Empty(Scanner().ScanCSharp("engine/probe.cs", code));
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
