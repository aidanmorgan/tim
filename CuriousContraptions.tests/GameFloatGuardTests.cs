using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CuriousContraptions.Tests;

/// <summary>Named architecture guard: semantic double state/calculation is forbidden in production game code.
/// Genuine external adapters are listed by member, never by file or containing class.</summary>
public sealed class GameFloatGuardTests
{
    // The single external-adapter inventory. These members only sample/validate/transport external values,
    // observe performance, or implement the framework's required callback signature.
    internal static readonly HashSet<string> Adapters = new(StringComparer.Ordinal)
    {
        "CuriousContraptions.Gpu.WorkshopNativeClock.FromMilliseconds",
        "CuriousContraptions.Gpu.BrowserWorkshopTransport.JsNativeMilliseconds",
        "CuriousContraptions.Gpu.BrowserWorkshopTransport.JsEventMilliseconds",
        "CuriousContraptions.Simulation.Program.NativeMilliseconds",
        "CuriousContraptions.Simulation.Program.ObservationMilliseconds",
        "CuriousContraptions.Simulation.Program.RecordObservedTickDuration",
        "Godot.Range.Value", "Godot.Range.MinValue", "Godot.Range.MaxValue", "Godot.Range.Step",
        "Godot.Range.SetValueNoSignal", "Google.FlatBuffers.FlatBufferBuilder.AddFloat",
        "CuriousContraptions.Workshop.OnLayerDepthChanged", "CuriousContraptions.Workshop.LayerDepthInput",
        "CuriousContraptions.Animation.Worker.Program.NativeMilliseconds",
        "CuriousContraptions.Animation.Worker.Program.Reply",
        "CuriousContraptions.Simulation.WorkshopTrace.RecordTickDuration",
        "CuriousContraptions.Simulation.WorkshopTrace.FlushSealed",
        "CuriousContraptions.Simulation.WorkshopTrace._durations",
        "System.Runtime.InteropServices.JavaScript.JSObject.SetProperty",
        "CuriousContraptions.Gpu.WorkshopObservationProjection.Durations",
        "CuriousContraptions.Gpu.IWorkshopObservationObject<T>.SetMilliseconds",
        "CuriousContraptions.Gpu.BrowserWorkshopObservation.BrowserObject.SetMilliseconds",
        "CuriousContraptions.Gpu.WorkshopWireFlatBuffers.EncodeDurations",
        "CuriousContraptions.Gpu.WorkshopWireFlatBuffers.DecodeDurations",
        "CuriousContraptions.Gpu.WorkshopWireFlatBuffers.ValidateDurations",
        "CuriousContraptions.Wire.CaptureObservation.Milliseconds",
        "CuriousContraptions.Wire.CaptureObservation.GetMillisecondsBytes",
        "CuriousContraptions.Wire.CaptureObservation.GetMillisecondsArray",
        "CuriousContraptions.Wire.CaptureObservation.CreateMillisecondsVector",
        "CuriousContraptions.Wire.CaptureObservation.CreateMillisecondsVectorBlock",
        "CuriousContraptions.Workshop.PrecisionInput", "CuriousContraptions.Workshop.FrameDelta", "CuriousContraptions.Workshop.ChangePrecision", "CuriousContraptions.Workshop._Process", "CuriousContraptions.Workshop._PhysicsProcess",
        "CuriousContraptions.MachineWorld._Process"
    };
    // Exact external observation locals inside mixed methods; their neighboring game expressions are still inspected.
    private static readonly HashSet<string> ObservationLocals = new(StringComparer.Ordinal)
    {
        "CuriousContraptions.Simulation.Program.RunLoopHost.Advance.tickStarted",
        "CuriousContraptions.Simulation.WorkshopTrace.Begin.durations"
    };

    private static readonly HashSet<string> ObservationParameters = new(StringComparer.Ordinal)
    {
        "CuriousContraptions.Simulation.WorkshopTrace.Record.milliseconds",
        "CuriousContraptions.Gpu.WorkshopCaptureDiagnostic..ctor.Milliseconds"
    };

    private static string Identity(ISymbol symbol) => symbol.ContainingType is { } type
        ? type.ToDisplayString() + "." + symbol.Name : symbol.Name;
    private static bool Wider(ITypeSymbol? type) => type?.SpecialType == SpecialType.System_Double ||
        type is IArrayTypeSymbol array && Wider(array.ElementType);
    private static bool Included(string path) => path.EndsWith(".cs", StringComparison.Ordinal) &&
        (path.StartsWith("engine/", StringComparison.Ordinal) || path.StartsWith("ui/", StringComparison.Ordinal) ||
         path.StartsWith("elements/", StringComparison.Ordinal) || path.StartsWith("tools/BundleElements/", StringComparison.Ordinal) ||
         path.StartsWith("CuriousContraptions.", StringComparison.Ordinal) &&
         !path.StartsWith("CuriousContraptions.tests/", StringComparison.Ordinal) &&
         !path.StartsWith("CuriousContraptions.FlatBuffers/", StringComparison.Ordinal)) &&
        !path.Split('/').Any(part => part is "obj" or "bin");

    internal static string[] Violations(CSharpCompilation compilation, SyntaxTree tree)
    {
        var model = compilation.GetSemanticModel(tree);
        var hits = new List<string>();
        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            // Type and converted type catch inferred locals, implicit promotion and widen-calculate-narrow.
            if (node is not ExpressionSyntax expression) continue;
            var info = model.GetTypeInfo(expression);
            if (!Wider(info.Type) && !Wider(info.ConvertedType)) continue;
            var declaration = node.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            var containing = declaration is FieldDeclarationSyntax field
                ? model.GetDeclaredSymbol(field.Declaration.Variables[0])
                : declaration is not null ? model.GetDeclaredSymbol(declaration) : model.GetEnclosingSymbol(node.SpanStart);
            while (containing is ILocalSymbol or IParameterSymbol) containing = containing.ContainingSymbol;
            if (containing is not null && Adapters.Contains(Identity(containing))) continue;
            if (model.GetSymbolInfo(node).Symbol is { } referenced && Adapters.Contains(Identity(referenced))) continue;
            var local = node.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault() ??
                node.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().FirstOrDefault()?.Variables.FirstOrDefault();
            if (local is not null && model.GetDeclaredSymbol(local) is ILocalSymbol variable &&
                ObservationLocals.Contains(Identity(variable.ContainingSymbol) + "." + variable.Name)) continue;
            if (model.GetSymbolInfo(node).Symbol is ILocalSymbol read &&
                ObservationLocals.Contains(Identity(read.ContainingSymbol) + "." + read.Name)) continue;
            var parameter = node.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
            if (parameter is not null && model.GetDeclaredSymbol(parameter) is IParameterSymbol declaredParameter &&
                ObservationParameters.Contains(Identity(declaredParameter.ContainingSymbol) + "." + declaredParameter.Name)) continue;
            // Roslyn also reports the contextual conversion on a member access's final name.
            // Normalize that name only; never walk through arithmetic or other argument descendants.
            var argumentValue = node is SimpleNameSyntax && node.Parent is MemberAccessExpressionSyntax member && member.Name == node
                ? member : node;
            // Only a float/integer result may widen at a named external output call. The child's
            // own type still rejects wider arithmetic before that conversion.
            if (!Wider(info.Type) && Wider(info.ConvertedType) &&
                argumentValue.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax external } &&
                model.GetSymbolInfo(external).Symbol is { } callee && Adapters.Contains(Identity(callee))) continue;
            if (!Wider(info.Type) && Wider(info.ConvertedType) && node.Parent is AssignmentExpressionSyntax output &&
                node == output.Right && model.GetSymbolInfo(output.Left).Symbol is { } property && Adapters.Contains(Identity(property))) continue;
            if (node is AssignmentExpressionSyntax assignment &&
                model.GetSymbolInfo(assignment.Left).Symbol is { } destination && Adapters.Contains(Identity(destination))) continue;
            var at = node.GetLocation().GetLineSpan().StartLinePosition;
            hits.Add($"F32-GAME {tree.FilePath}:{at.Line + 1}:{at.Character + 1}: {node}");
        }
        return hits.Distinct(StringComparer.Ordinal).ToArray();
    }

    [Fact]
    public void ProductionGameValuesHaveNoExplicitOrInferredDoubleCalculations()
    {
        var root = twodog.Engine.ResolveProjectDir();
        var expected = CSharpStaticGateTests.RepositoryFiles().Where(Included).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("engine/gpu/WorkshopConstruction.cs", expected);
        Assert.Contains("elements/Ball/BallElementDeclaration.cs", expected);
        Assert.Contains("tools/BundleElements/SceneGeometryExtractor.cs", expected);
        Assert.Contains("engine/presentation/AnimationBatch.cs", expected);
        Assert.Contains("CuriousContraptions.Animation.Worker/Program.cs", expected);
        Assert.Contains("CuriousContraptions.Abstractions/PhysicalTime.cs", expected);
        var covered = new HashSet<string>(StringComparer.Ordinal);
        var hits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var compilation in CSharpStaticGateTests.MatchingCompilations())
        foreach (var tree in compilation.SyntaxTrees)
        {
            var path = Path.GetRelativePath(root, tree.FilePath).Replace('\\', '/');
            if (!expected.Contains(path)) continue;
            covered.Add(path);
            hits.UnionWith(Violations(compilation, tree));
        }
        Assert.Empty(expected.Except(covered));
        Assert.True(hits.Count == 0, string.Join("\n", hits.Order(StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData("double x = 1;", true)]
    [InlineData("var x = 1.0 + input;", true)]
    [InlineData("var x = System.Math.Sin(input);", true)]
    [InlineData("var x = (float)((double)input * input);", true)]
    [InlineData("var x = System.Math.Min(input, 1.0);", true)]
    [InlineData("var x = System.MathF.Sin(input);", false)]
    [InlineData("var x = System.Math.Min(input, 1f);", false)]
    [InlineData("var x = System.Math.Min(1, 2);", false)]
    public void SemanticControlsCatchWideningAndAdmitFloatAndIntegerOperations(string body, bool rejected)
    {
        var tree = CSharpSyntaxTree.ParseText("class Control { void Run(float input) { " + body + " } }", path: "control.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("control", [tree], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Equal(rejected, Violations(compilation, tree).Length != 0);
    }

    [Fact]
    public void AnAdapterDoesNotExemptItsNeighboringGameMember()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace CuriousContraptions.Gpu;
            class WorkshopNativeClock {
                public static long FromMilliseconds(double value) => checked((long)(value * 1e6));
                public static float Neighbor(float value) => (float)System.Math.Sin(value);
            }
            """, path: "adapter-control.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("control", [tree], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var hits = Violations(compilation, tree);
        Assert.NotEmpty(hits);
        Assert.All(hits, hit => Assert.Contains("adapter-control.cs:4:", hit));
    }

    [Fact]
    public void WiderArithmeticInsideAnAdapterArgumentIsStillRejected()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace CuriousContraptions.Gpu;
            class WorkshopNativeClock {
                public static long FromMilliseconds(double value) => checked((long)(value * 1e6));
                public static long Consumer(float input) => FromMilliseconds((double)input * input);
            }
            """, path: "argument-control.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("control", [tree], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var hits = Violations(compilation, tree);
        Assert.Contains(hits, hit => hit.Contains("(double)input * input", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("value.Amount", false)]
    [InlineData("value.Count", false)]
    [InlineData("value.Amount * 2f", false)]
    [InlineData("value.Amount * 2.0", true)]
    [InlineData("(double)value.Amount", true)]
    public void ExternalOutputAdmitsMemberConversionsButRejectsWiderArgumentCalculations(string value, bool rejected)
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace CuriousContraptions.Gpu;
            class WorkshopNativeClock {
                public static long FromMilliseconds(double value) => checked((long)value);
            }
            class Payload { public float Amount; public uint Count; }
            class Consumer {
                public long Project(Payload value) => WorkshopNativeClock.FromMilliseconds(
            """ + value + "); }", path: "member-output-control.cs", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("control", [tree], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.DoesNotContain(compilation.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(rejected, Violations(compilation, tree).Length != 0);
    }

    private static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path));
}
