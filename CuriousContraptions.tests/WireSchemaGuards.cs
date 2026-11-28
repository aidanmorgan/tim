using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Wire = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5c architecture guards over the one wire schema file (owner decision 24p): it is the only schema file and names its roots
/// by schema data (S1); every declaration that references a declaration marked <c>authoring</c> is itself marked, and the
/// runtime roots are unmarked, so no runtime root reaches a marked declaration (S9); and no compile of the worker
/// runtime (Core, Simulation, Animation.Worker) references a marked declaration, a generated companion of one, or the main
/// assembly's authoring codecs, apart from the one save-frame verification Core performs for the main assembly (S10). Each
/// rule is one function that the real-tree guard and its synthetic controls both call.
/// </summary>
public sealed class WireSchemaGuards
{
    private const string OneSchema = "workshop_wire.fbs";
    private const string RootIdentifierEnum = "WireRootIdentifier";

    private static readonly string Root = twodog.Engine.ResolveProjectDir();

    private static WireSchemaText RealSchema() => WireSchemaText.Read(Path.Combine(Root, "schemas", OneSchema));

    // ----- S1: one schema file, one root type, one identifier, roots named by schema data --------------------------------

    [Fact]
    public void TheSchemaIsOneFileWithOneRootTypeAndNoInclude()
    {
        var real = Directory.GetFiles(Path.Combine(Root, "schemas"))
            .ToDictionary(path => Path.GetFileName(path), path => File.ReadAllText(path), StringComparer.Ordinal);
        Assert.Contains("README.md", real.Keys);
        Assert.Empty(SchemaFileViolations(real));

        // Control: a second schema file, and an include, are each refused by name.
        var second = new Dictionary<string, string>(real, StringComparer.Ordinal) { ["s5c_second.fbs"] = real[OneSchema] };
        Assert.Equal(["schemas/s5c_second.fbs is a second schema file"], SchemaFileViolations(second));
        var included = new Dictionary<string, string>(real, StringComparer.Ordinal) { [OneSchema] = "include \"other.fbs\";\n" + real[OneSchema] };
        Assert.Equal(["schemas/workshop_wire.fbs: line 1: 'include' is not in the wire schema grammar"], SchemaFileViolations(included));
        // Boundary: a root identifier member that names no declared table, and a file identifier other than its root's member
        // (synthetic identifiers: "TEST" is 0x54534554 read little-endian).
        var admitted = Synthetic("enum WireRootIdentifier : uint32 { SavedConstruction = 0x54534554 }\n");
        Assert.Empty(SchemaFileViolations(new Dictionary<string, string> { [OneSchema] = admitted }));
        var undeclared = Synthetic("enum WireRootIdentifier : uint32 { SavedConstruction = 0x54534554, Ghost = 0x54534555 }\n");
        Assert.Equal(["WireRootIdentifier.Ghost names no declared table"], SchemaFileViolations(new Dictionary<string, string> { [OneSchema] = undeclared }));
        var mismatched = Synthetic("enum WireRootIdentifier : uint32 { SavedConstruction = 0x54534555 }\n");
        Assert.Equal(["file_identifier \"TEST\" is not WireRootIdentifier.SavedConstruction (\"UEST\")"],
            SchemaFileViolations(new Dictionary<string, string> { [OneSchema] = mismatched }));
        // Control: a root identifier enum of another underlying type is refused.
        var narrow = Synthetic("enum WireRootIdentifier : uint8 { SavedConstruction = 0x54 }\n");
        Assert.Equal(["the schema declares no enum WireRootIdentifier : uint32"], SchemaFileViolations(new Dictionary<string, string> { [OneSchema] = narrow }));
    }

    /// <summary>S1: the files of schemas/ by name. Exactly one schema file, read by the one schema reader (which refuses an
    /// include and a second root_type or file_identifier); every WireRootIdentifier member names a declared table, and the
    /// file_identifier is its root_type's member converted by the one conversion.</summary>
    internal static List<string> SchemaFileViolations(IReadOnlyDictionary<string, string> schemaDirectory)
    {
        var violations = schemaDirectory.Keys.Where(name => name.EndsWith(".fbs", StringComparison.Ordinal) && name != OneSchema)
            .Order(StringComparer.Ordinal).Select(name => $"schemas/{name} is a second schema file").ToList();
        if (!schemaDirectory.TryGetValue(OneSchema, out var text))
        {
            violations.Add($"schemas/{OneSchema} is missing");
            return violations;
        }
        WireSchemaText schema;
        try { schema = WireSchemaText.Parse(text); }
        catch (FormatException refused)
        {
            violations.Add($"schemas/{OneSchema}: {refused.Message}");
            return violations;
        }
        violations.AddRange(RootIdentifierViolations(schema));
        return violations;
    }

    private static IEnumerable<string> RootIdentifierViolations(WireSchemaText schema)
    {
        if (!schema.Declares(RootIdentifierEnum) || schema[RootIdentifierEnum] is not { Kind: WireDeclarationKind.Enum, Underlying: WireScalar.UInt32 } roots)
        {
            yield return $"the schema declares no enum {RootIdentifierEnum} : uint32";
            yield break;
        }
        foreach (var member in roots.Members.Where(member => !schema.Declares(member.Name) || schema[member.Name].Kind != WireDeclarationKind.Table))
            yield return $"{RootIdentifierEnum}.{member.Name} names no declared table";
        if (roots.Members.FirstOrDefault(member => member.Name == schema.RootType) is not { } root)
        {
            yield return $"root_type {schema.RootType} has no {RootIdentifierEnum} member";
            yield break;
        }
        var expected = WorkshopWireFlatBuffers.FileIdentifier((CuriousContraptions.Wire.WireRootIdentifier)root.Value);
        if (schema.FileIdentifier != expected)
            yield return $"file_identifier \"{schema.FileIdentifier}\" is not {RootIdentifierEnum}.{root.Name} (\"{expected}\")";
    }

    // ----- S9: no runtime root reaches an authoring declaration ----------------------------------------------------------

    // One rule, closed under reference: every declaration that references a marked one is itself marked. The runtime roots
    // (the WireRootIdentifier members other than the save) are unmarked, so no field path from one reaches a marked
    // declaration, and a marked type reaches code only through a marked declaration (S10's stated limit relies on this).
    // Opaque byte vectors are not visible to it.
    [Fact]
    public void NoRuntimeRootReachesAnAuthoringDeclaration()
    {
        var real = RealSchema();
        Assert.True(real["SavedConstruction"].Authoring);
        Assert.True(real["InstanceRecord"].Authoring);
        Assert.Equal(["WorkshopCommand", "WorkshopObservationBatch", "WorkshopResponse"], RuntimeRoots(real).Order(StringComparer.Ordinal));
        // Boundary: the real file, whose runtime roots and marked declarations share the unmarked struct Vec3f, is admitted.
        Assert.Contains(real["WorkshopResponse"].Fields, field => field.Name == "bodies" && field.Type is { Name: "BodyRead", Vector: true });
        Assert.Contains(real["BodyRead"].Fields, field => field.Type.Name == "Vec3f");
        Assert.Contains(real["InstanceRecord"].Fields, field => field.Type.Name == "Vec3f");
        Assert.Empty(UnmarkedReferenceViolations(real));

        // Controls: a runtime root whose field names a marked table, an unmarked table holding a marked field, and one holding a
        // vector of them, are each refused, naming the field.
        var root = WireSchemaText.Parse(Synthetic(RootsEnum + MarkedRecords + "table WorkshopCommand { sequence: uint64; scene: ConstructionRecord; }\n"));
        Assert.Equal(["WorkshopCommand.scene names the authoring declaration ConstructionRecord, but WorkshopCommand is not marked authoring"], UnmarkedReferenceViolations(root));
        var direct = WireSchemaText.Parse(Synthetic(RootsEnum + MarkedRecords + "table Holder { record: InstanceRecord; }\n"));
        Assert.Equal(["Holder.record names the authoring declaration InstanceRecord, but Holder is not marked authoring"], UnmarkedReferenceViolations(direct));
        var vector = WireSchemaText.Parse(Synthetic(RootsEnum + MarkedRecords + "table Holder { records: [InstanceRecord]; }\n"));
        Assert.Equal(["Holder.records names the authoring declaration InstanceRecord, but Holder is not marked authoring"], UnmarkedReferenceViolations(vector));
    }

    /// <summary>S9's one rule: every field (a vector's element type included) of an unmarked declaration whose type is a marked
    /// declaration.</summary>
    internal static List<string> UnmarkedReferenceViolations(WireSchemaText schema) =>
        schema.Declarations.Where(declaration => !declaration.Authoring)
            .SelectMany(declaration => declaration.Fields
                .Where(field => field.Type is { Kind: WireTypeKind.Declared, Name: { } name } && schema[name].Authoring)
                .Select(field => $"{declaration.Name}.{field.Name} names the authoring declaration {field.Type.Name}, but {declaration.Name} is not marked authoring"))
            .ToList();

    private static IEnumerable<string> RuntimeRoots(WireSchemaText schema) =>
        schema[RootIdentifierEnum].Members.Select(member => member.Name).Where(name => schema.Declares(name) && !schema[name].Authoring);

    private const string RootsEnum = "enum WireRootIdentifier : uint32 { WorkshopCommand = 1, SavedConstruction = 0x54534554 }\n";
    private const string MarkedRecords = "table InstanceRecord (authoring) { id: uint64; }\n" +
        "table ConstructionRecord (authoring) { instances: [InstanceRecord]; }\n" +
        "table SavedConstruction (authoring) { construction: ConstructionRecord; }\n";

    /// <summary>A complete schema text of the closed grammar around the given declarations; the save is its root.</summary>
    private static string Synthetic(string declarations)
    {
        var body = declarations.Contains("table SavedConstruction", StringComparison.Ordinal)
            ? declarations
            : declarations + "table SavedConstruction (authoring) { next_body_id: uint64; }\n";
        return "namespace CuriousContraptions.Wire;\nattribute \"authoring\";\n" + body + "root_type SavedConstruction;\nfile_identifier \"TEST\";\n";
    }

    // ----- S10: no worker compile references an authoring declaration ---------------------------------------------------

    private static readonly string[] WorkerProjects = ["CuriousContraptions.Core", "CuriousContraptions.Simulation", "CuriousContraptions.Animation.Worker"];
    private static readonly string[] MainOnlyAuthoringTypes =
        ["CuriousContraptions.Gpu.WorkshopAuthoringFlatBuffers", "CuriousContraptions.Gpu.WorkshopSaveCodec", "CuriousContraptions.Gpu.WorkshopSavePrevalidation"];
    private const string AdmittedType = "CuriousContraptions.Wire.SavedConstructionVerify";
    private const string AdmittedProject = "CuriousContraptions.Core";
    private const string AdmittedFile = "CuriousContraptions.Core/CoreCommandCodec.cs";
    private const string AdmittedMethod = "ValidateSaveFrame";

    /// <summary>The types no worker compile may bind: every declaration marked authoring and its generated companions (the
    /// pinned compiler writes a <c>&lt;Name&gt;Verify</c> class for a table and nothing else for a struct; S5c task 1(a)), and the
    /// main assembly's authoring codecs.</summary>
    private static HashSet<string> ForbiddenTypes(WireSchemaText schema) => schema.Declarations.Where(declaration => declaration.Authoring)
        .SelectMany(declaration => declaration.Kind == WireDeclarationKind.Table ? [declaration.Name, declaration.Name + "Verify"] : new[] { declaration.Name })
        .Select(name => $"{WireSchemaText.Namespace}.{name}").Concat(MainOnlyAuthoringTypes).ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void NoWorkerCompileReferencesAnAuthoringDeclaration()
    {
        var forbidden = ForbiddenTypes(RealSchema());
        Assert.Contains(AdmittedType, forbidden);
        var compiles = CSharpStaticGateTests.MatchingCompiles().Where(compile => WorkerProjects.Contains(compile.ProjectName, StringComparer.Ordinal))
            .Select(compile => (compile.ProjectName, Generated(compile))).ToArray();
        Assert.Equal(WorkerProjects.Order(StringComparer.Ordinal), compiles.Select(compile => compile.ProjectName).Distinct().Order(StringComparer.Ordinal));

        var result = AuthoringReferences(compiles, forbidden);

        Assert.True(result.Violations.Count == 0, string.Join("\n", result.Violations));
        // The walk sees the one admitted site, deduplicated across every replayed compile of Core.
        Assert.Single(result.Admitted);
    }

    public enum SyntheticWorkerCase
    {
        AdmittedSite, SimulationInstanceRecord, SecondSiteInTheAdmittedMethod, SecondCoreSite, CoreInstanceRecordVerify, AliasedMarkedType,
        CompileError, UnnamedTargetTypedCreation, UnnamedLambdaParameter, ContainingTypeArgument, AdmittedSiteOutsideCore,
        UnnamedFunctionPointer, UnnamedFunctionPointerReturn,
    }

    /// <summary>S10's rule on synthetic compiles against the real generated wire assembly. Boundary: the admitted site and the
    /// enum member WireRootIdentifier.SavedConstruction are admitted. Each control is refused, naming project, file and type.</summary>
    [Theory]
    [InlineData(SyntheticWorkerCase.AdmittedSite, null)]
    [InlineData(SyntheticWorkerCase.SimulationInstanceRecord, "CuriousContraptions.Simulation: /s10/Simulation/Probe.cs:1: references CuriousContraptions.Wire.InstanceRecord")]
    [InlineData(SyntheticWorkerCase.SecondSiteInTheAdmittedMethod, "a second site of CuriousContraptions.Wire.SavedConstructionVerify")]
    [InlineData(SyntheticWorkerCase.SecondCoreSite, "CuriousContraptions.Core: /s10/CuriousContraptions.Core/CoreCommandCodec.cs:4: references CuriousContraptions.Wire.SavedConstructionVerify")]
    [InlineData(SyntheticWorkerCase.CoreInstanceRecordVerify, "CuriousContraptions.Core: /s10/Core/Probe.cs:1: references CuriousContraptions.Wire.InstanceRecordVerify")]
    [InlineData(SyntheticWorkerCase.AliasedMarkedType, "CuriousContraptions.Core: /s10/Core/Probe.cs:2: references CuriousContraptions.Wire.PuzzleRecord")]
    [InlineData(SyntheticWorkerCase.CompileError, "CuriousContraptions.Core: /s10/Core/Probe.cs:1: CS0029")]
    [InlineData(SyntheticWorkerCase.UnnamedTargetTypedCreation, "CuriousContraptions.Core: /s10/Core/Probe.cs:1: references CuriousContraptions.Wire.InstanceRecord")]
    [InlineData(SyntheticWorkerCase.UnnamedLambdaParameter, "CuriousContraptions.Core: /s10/Core/Probe.cs:1: references CuriousContraptions.Wire.InstanceRecord")]
    [InlineData(SyntheticWorkerCase.ContainingTypeArgument, "CuriousContraptions.Core: /s10/Core/Probe.cs:1: references CuriousContraptions.Wire.InstanceRecord")]
    [InlineData(SyntheticWorkerCase.AdmittedSiteOutsideCore, "CuriousContraptions.Simulation: /s10/CuriousContraptions.Core/CoreCommandCodec.cs:3: references CuriousContraptions.Wire.SavedConstructionVerify")]
    [InlineData(SyntheticWorkerCase.UnnamedFunctionPointer, "CuriousContraptions.Core: /s10/Core/Probe.cs:1: references CuriousContraptions.Wire.InstanceRecord")]
    [InlineData(SyntheticWorkerCase.UnnamedFunctionPointerReturn, "CuriousContraptions.Core: /s10/Core/Probe.cs:1: references CuriousContraptions.Wire.InstanceRecord")]
    public void TheWorkerReferenceRuleRefusesEachForbiddenBinding(SyntheticWorkerCase which, string? expected)
    {
        const string admitted = "class CoreCommandCodec\n{\n    void ValidateSaveFrame(Google.FlatBuffers.Verifier v) => CuriousContraptions.Wire.SavedConstructionVerify.Verify(v, 0);\n" +
            "    uint Root() => (uint)CuriousContraptions.Wire.WireRootIdentifier.SavedConstruction;\n}\n";
        var core = new List<(string Path, string Text)> { ("/s10/CuriousContraptions.Core/CoreCommandCodec.cs", admitted) };
        var project = "CuriousContraptions.Core";
        switch (which)
        {
            case SyntheticWorkerCase.SimulationInstanceRecord:
                project = "CuriousContraptions.Simulation";
                core = [("/s10/Simulation/Probe.cs", "class Probe { CuriousContraptions.Wire.InstanceRecord record; }")];
                break;
            case SyntheticWorkerCase.SecondSiteInTheAdmittedMethod:
                core[0] = (core[0].Path, admitted.Replace("Verify(v, 0);", "Verify(v, 0) && CuriousContraptions.Wire.SavedConstructionVerify.Verify(v, 1);", StringComparison.Ordinal)
                    .Replace("void ValidateSaveFrame", "bool ValidateSaveFrame", StringComparison.Ordinal));
                break;
            case SyntheticWorkerCase.SecondCoreSite:
                core[0] = (core[0].Path, admitted.Replace("    uint Root()", "    bool Other(Google.FlatBuffers.Verifier v) => CuriousContraptions.Wire.SavedConstructionVerify.Verify(v, 0);\n    uint Root()", StringComparison.Ordinal));
                break;
            case SyntheticWorkerCase.CoreInstanceRecordVerify:
                core.Add(("/s10/Core/Probe.cs", "class Probe { bool Check(Google.FlatBuffers.Verifier v) => CuriousContraptions.Wire.InstanceRecordVerify.Verify(v, 0); }"));
                break;
            case SyntheticWorkerCase.AliasedMarkedType:
                core.Add(("/s10/Core/Probe.cs", "using Puzzle = CuriousContraptions.Wire.PuzzleRecord;\nclass Probe { Puzzle record; }"));
                break;
            case SyntheticWorkerCase.CompileError:
                core.Add(("/s10/Core/Probe.cs", "class Probe { int value = \"text\"; }"));
                break;
            case SyntheticWorkerCase.UnnamedTargetTypedCreation:
                core.Add(("/s10/Core/Probe.cs", "class Probe { void Run() { Helper.Use(new()); } }"));
                break;
            case SyntheticWorkerCase.UnnamedLambdaParameter:
                core.Add(("/s10/Core/Probe.cs", "class Probe { void Run() { Helper.Each(record => record.PartId); } }"));
                break;
            case SyntheticWorkerCase.ContainingTypeArgument:
                core.Add(("/s10/Core/Probe.cs", "class Probe { void Run() { Helper.Hold(new()); } }"));
                break;
            case SyntheticWorkerCase.AdmittedSiteOutsideCore:
                project = "CuriousContraptions.Simulation";
                break;
            case SyntheticWorkerCase.UnnamedFunctionPointer:
                core.Add(("/s10/Core/Probe.cs", "unsafe class Probe { void Run() { var p = Helper.Pointer(); } }"));
                break;
            case SyntheticWorkerCase.UnnamedFunctionPointerReturn:
                core.Add(("/s10/Core/Probe.cs", "unsafe class Probe { void Run() { var p = Helper.Returning(); } }"));
                break;
        }
        var compilation = CSharpCompilation.Create(project, core.Select(source => CSharpSyntaxTree.ParseText(source.Text, path: source.Path)),
            [.. SyntheticReferences.Value, SyntheticHelper.Value], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        var result = AuthoringReferences([(project, compilation)], ForbiddenTypes(RealSchema()));

        if (expected is null)
        {
            Assert.Empty(result.Violations);
            Assert.Single(result.Admitted);
        }
        else Assert.Contains(result.Violations, violation => violation.Contains(expected, StringComparison.Ordinal));
    }

    private static readonly Lazy<MetadataReference[]> SyntheticReferences = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(CuriousContraptions.Wire.WireRootIdentifier).Assembly.Location)
            .DistinctBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToArray());

    // A referenced assembly whose members take or return InstanceRecord directly, through a delegate, as the argument of a
    // containing type or in a function-pointer signature, so a probe can bind the type without naming it.
    private static readonly Lazy<MetadataReference> SyntheticHelper = new(() => CSharpCompilation.Create("S10Helper",
        [CSharpSyntaxTree.ParseText("public static class Helper { public static void Use(CuriousContraptions.Wire.InstanceRecord record) { } " +
            "public static void Each(System.Func<CuriousContraptions.Wire.InstanceRecord, string> read) { } " +
            "public static void Hold(Outer<CuriousContraptions.Wire.InstanceRecord>.Inner inner) { } " +
            "public static unsafe delegate*<CuriousContraptions.Wire.InstanceRecord, void> Pointer() => null; " +
            "public static unsafe delegate*<CuriousContraptions.Wire.InstanceRecord> Returning() => null; } " +
            "public class Outer<T> { public class Inner { } }")],
        SyntheticReferences.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)).ToMetadataReference());

    internal sealed record ReferenceResult(List<string> Violations, HashSet<(string Path, TextSpan Span)> Admitted);

    /// <summary>S10: every type symbol each compile binds (a simple name's type: type syntax, member access, object creation,
    /// <c>typeof</c>, <c>nameof</c>, attribute arguments, generic arguments, aliases and <c>var</c>; and every expression's type and
    /// converted type, so an unnamed one is seen too), generated trees included, checked against the forbidden types; XML
    /// documentation crefs are not inspected. Stated limit: a marked type bound only through a called member's signature (an
    /// optional or <c>params</c> parameter with no argument expression) is not walked; it is unreachable while every declaration
    /// that references a marked one is itself marked, which S9 enforces
    /// (<see cref="NoRuntimeRootReachesAnAuthoringDeclaration"/>), because worker declarations then name such a
    /// type at their own declaration, and no other referenced assembly exposes one. A compile whose model reports an error is
    /// refused, so a binding gap cannot hide a reference. The one admitted site is counted once per source location across
    /// compiles; a second distinct location is refused.</summary>
    internal static ReferenceResult AuthoringReferences(IEnumerable<(string Project, CSharpCompilation Compilation)> compiles, IReadOnlySet<string> forbidden)
    {
        var result = new ReferenceResult([], []);
        foreach (var (project, compilation) in compiles)
        {
            foreach (var error in compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && !diagnostic.IsWarningAsError))
                result.Violations.Add($"{project}: {Where(error.Location)}: {error.Id} {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}");
            foreach (var tree in compilation.SyntaxTrees)
                CheckTree(project, compilation.GetSemanticModel(tree), forbidden, result);
        }
        if (result.Admitted.Count > 1)
            result.Violations.Add($"a second site of {AdmittedType}: " + string.Join(", ", result.Admitted.Select(site => $"{site.Path} {site.Span}")));
        return result;
    }

    private static void CheckTree(string project, SemanticModel model, IReadOnlySet<string> forbidden, ReferenceResult result)
    {
        foreach (var node in model.SyntaxTree.GetRoot().DescendantNodes())
        foreach (var type in BoundTypes(model, node).Select(type => type.OriginalDefinition.ToDisplayString()).Where(forbidden.Contains).Distinct())
        {
            if (type == AdmittedType && IsAdmittedSite(project, node)) result.Admitted.Add((model.SyntaxTree.FilePath, node.Span));
            else result.Violations.Add($"{project}: {Where(node.GetLocation())}: references {type}");
        }
    }

    // The types a node binds: a simple name's type symbol (type syntax, aliases, var, typeof, nameof, generic and attribute
    // arguments), and an expression's type and converted type unless the expression names a type or namespace, so a type that
    // is never named is seen too (a target-typed creation, a lambda parameter, a value passed straight on).
    private static IEnumerable<ITypeSymbol> BoundTypes(SemanticModel model, SyntaxNode node)
    {
        if (node is SimpleNameSyntax name)
        {
            var info = model.GetSymbolInfo(name);
            foreach (var type in TypesOf(info.Symbol ?? info.CandidateSymbols.FirstOrDefault())) yield return type;
        }
        if (node is not ExpressionSyntax expression || model.GetSymbolInfo(expression).Symbol is ITypeSymbol or INamespaceSymbol) yield break;
        var typeInfo = model.GetTypeInfo(expression);
        foreach (var type in TypesOf(typeInfo.Type).Concat(TypesOf(typeInfo.ConvertedType))) yield return type;
    }

    // The one admitted site: ValidateSaveFrame in Core's CoreCommandCodec.cs, in a Core compile only.
    private static bool IsAdmittedSite(string project, SyntaxNode name) =>
        project == AdmittedProject &&
        name.SyntaxTree.FilePath.Replace('\\', '/').EndsWith("/" + AdmittedFile, StringComparison.Ordinal) &&
        name.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText == AdmittedMethod;

    // The closed decomposition of every type kind: a type, then recursively the parts it is built from: a named type's type
    // arguments and containing type, an array's or pointer's element type, and a function pointer's return and parameter
    // types. Every other type (a type parameter, dynamic, an error type) is a leaf.
    private static IEnumerable<ITypeSymbol> TypesOf(ISymbol? symbol) => symbol switch
    {
        INamedTypeSymbol named => named.TypeArguments.SelectMany(TypesOf).Concat(TypesOf(named.ContainingType)).Prepend(named),
        IArrayTypeSymbol array => TypesOf(array.ElementType).Prepend(array),
        IPointerTypeSymbol pointer => TypesOf(pointer.PointedAtType).Prepend(pointer),
        IFunctionPointerTypeSymbol function => function.Signature.Parameters.Select(parameter => parameter.Type)
            .Prepend(function.Signature.ReturnType).SelectMany(TypesOf).Prepend(function),
        ITypeSymbol type => [type],
        _ => [],
    };

    private static string Where(Location location)
    {
        var span = location.GetLineSpan();
        return $"{span.Path}:{span.StartLinePosition.Line + 1}";
    }

    /// <summary>The replayed compile with its source generators run, as the compiler ran them: the generators from the
    /// command line's analyzer references, its parse options, analyzer configuration files and additional files.</summary>
    private static CSharpCompilation Generated(CSharpStaticGateTests.MatchingCompile compile)
    {
        var loader = new AnalyzerLoader();
        var generators = compile.Arguments.AnalyzerReferences
            .Select(reference => new AnalyzerFileReference(compile.Resolve(reference.FilePath), loader))
            .SelectMany(reference => reference.GetGenerators(LanguageNames.CSharp)).ToArray();
        var configs = compile.Arguments.AnalyzerConfigPaths.Select(path => compile.Resolve(path))
            .Select(path => AnalyzerConfig.Parse(SourceText.From(File.ReadAllText(path)), path)).ToList();
        var additional = compile.Arguments.AdditionalFiles.Select(file => (AdditionalText)new AdditionalFile(compile.Resolve(file.Path))).ToImmutableArray();
        var driver = CSharpGeneratorDriver.Create(generators, additional, compile.Arguments.ParseOptions,
            new ConfigOptionsProvider(AnalyzerConfigSet.Create(configs)));
        driver.RunGeneratorsAndUpdateCompilation(compile.Compilation, out var output, out _);
        return (CSharpCompilation)output;
    }

    private sealed class AdditionalFile(string path) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(File.ReadAllText(Path));
    }

    private sealed class ConfigOptionsProvider(AnalyzerConfigSet set) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new ConfigOptions(set.GlobalConfigOptions.AnalyzerOptions);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => For(tree.FilePath);
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => For(textFile.Path);

        private ConfigOptions For(string path) =>
            new(set.GlobalConfigOptions.AnalyzerOptions.SetItems(set.GetOptionsForSourcePath(path).AnalyzerOptions));
    }

    private sealed class ConfigOptions(ImmutableDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
        public override IEnumerable<string> Keys => values.Keys;
    }

    /// <summary>Loads each analyzer assembly from its path into one context of its own; Roslyn and the framework resolve to the
    /// ones this test already runs.</summary>
    private sealed class AnalyzerLoader() : AssemblyLoadContext("S10 analyzers"), IAnalyzerAssemblyLoader
    {
        private readonly Dictionary<string, string> _dependencies = new(StringComparer.Ordinal);

        public void AddDependencyLocation(string fullPath) => _dependencies[System.IO.Path.GetFileNameWithoutExtension(fullPath)] = fullPath;

        public Assembly LoadFromPath(string fullPath) =>
            Assemblies.FirstOrDefault(assembly => assembly.Location == fullPath) ?? LoadFromAssemblyPath(fullPath);

        protected override Assembly? Load(AssemblyName assemblyName) =>
            assemblyName.Name is { } name && _dependencies.TryGetValue(name, out var path) && !name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
                ? LoadFromPath(path)
                : null;
    }

    // ----- S2: every flatc invocation reads only the one schema file, and TypeScript generates once per graph -----------

    private const string AbstractionsProject = "CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj";
    private const string VendoredProject = "CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj";
    private const string CompilerTargets = "tools/runtime/FlatcCompiler.targets";
    private static readonly string[] GraphLogs = ["playtest.binlog", "solution.binlog", "web.binlog"];

    [Fact]
    public void EveryCompilerRunReadsOnlyTheOneSchemaAndTypeScriptGeneratesOncePerGraph()
    {
        var directory = Environment.GetEnvironmentVariable("CSHARP_STATIC_GATE_BINLOGS") is { Length: > 0 } logs
            ? logs
            : throw new InvalidOperationException("CSHARP_STATIC_GATE_BINLOGS is required. Capture the five matching compiler-proof logs described in docs/delivery-workflow.md#local-test-authority.");
        var schema = Path.Combine(Root, "schemas", OneSchema);
        foreach (var log in GraphLogs)
        {
            var path = Path.Combine(directory, log);
            Assert.True(File.Exists(path), $"{log}: missing (every gate build's binary log is required)");
            Assert.Empty(CompilerGraphViolations(log, ReadCompilerRuns(path), schema));
        }

        var checkout = Path.Combine(Path.GetTempPath(), "checkout's with $space");
        var canonical = Path.Combine(checkout, "schemas", OneSchema);
        var foreign = Path.Combine(Path.GetTempPath(), "different checkout", "schemas", OneSchema);
        var csharpPaths = ControlCompilerPaths(VendoredProject, canonical);
        var paths = ControlCompilerPaths(AbstractionsProject, canonical);
        var csharp = CaptureExec(VendoredProject, "GenerateWireAccessors", FlatcCommand(csharpPaths, canonical, BuildWork.CSharp), 5, csharpPaths)!;
        var generation = FlatcCommand(paths, canonical, BuildWork.TypeScript);
        var ts = CaptureExec(AbstractionsProject, "GenerateWireTypeScript", generation, 8, paths)!;
        var runtime = CaptureExec(AbstractionsProject, "BuildRuntimeTypeScript", RuntimeCommand(paths.RuntimeOutput), 13, paths)!;
        var extra = CaptureExec(AbstractionsProject, "UnexpectedTarget", generation, 14, paths)!;
        var control = new CompilerRuns([csharp, ts, runtime],
            [new(BuildWork.Clean, 1, 3), new(BuildWork.Stamp, 4, 6), new(BuildWork.CSharp, 4, 6), new(BuildWork.TypeScript, 7, 11),
                new(BuildWork.Runtime, 12, 15), new(BuildWork.Reconcile, 16, 18)]);
        Assert.Empty(CompilerGraphViolations("control", control, canonical));
        var restores = ReplayRestoreControl();
        var restore = Assert.Single(restores.Invocations);
        Assert.Equal(BuildWork.Restore, restore.Work);
        Assert.Equal(3, restore.At);
        Assert.Equal(new WriterInterval(BuildWork.Restore, 1, 5), Assert.Single(restores.Writers));
        Assert.Empty(RestoreViolations("canonical restore control", restores));
        Assert.Contains(CompilerGraphViolations("missing restore interval control", restores with { Writers = [] }, canonical),
            finding => finding.Contains("active npm restore Execs", StringComparison.Ordinal));
        var missingOwner = ReplayRestoreControl(includeOwner: false);
        Assert.Equal(BuildWork.Restore, Assert.Single(missingOwner.Invocations).Work);
        Assert.Empty(missingOwner.Writers);
        var missingOwnerViolations = CompilerGraphViolations("missing restore owner control", missingOwner, canonical);
        Assert.Contains(missingOwnerViolations, finding => finding.Contains("unexpected npm restore owner", StringComparison.Ordinal));
        Assert.Contains(missingOwnerViolations, finding => finding.Contains("active npm restore Execs", StringComparison.Ordinal));
        var wrongOptions = ReplayRestoreControl("npm ci --no-fund");
        Assert.Single(wrongOptions.Invocations);
        Assert.Single(wrongOptions.Writers);
        Assert.Contains(CompilerGraphViolations("incorrect restore options control", wrongOptions, canonical),
            finding => finding.Contains("unexpected npm restore owner or command", StringComparison.Ordinal));
        var wrongExecutable = ReplayRestoreControl("echo ci --ignore-scripts --no-audit --no-fund");
        Assert.Equal(BuildWork.Restore, Assert.Single(wrongExecutable.Invocations).Work);
        Assert.Single(wrongExecutable.Writers);
        Assert.Contains(CompilerGraphViolations("wrong restore executable control", wrongExecutable, canonical),
            finding => finding.Contains("unexpected npm restore owner or command", StringComparison.Ordinal));
        var extraArgument = ReplayRestoreControl("npm ci --ignore-scripts --no-audit --no-fund ''");
        Assert.Contains(RestoreViolations("extra empty restore argument", extraArgument),
            finding => finding.Contains("unexpected npm restore owner or command", StringComparison.Ordinal));

        var foreignInstall = ReplayRestoreControl("npm install --ignore-scripts", ownerTarget: "UnexpectedTarget");
        Assert.Equal(BuildWork.Restore, Assert.Single(foreignInstall.Invocations).Work);
        Assert.Empty(foreignInstall.Writers);
        Assert.Contains(CompilerGraphViolations("unowned npm install control", foreignInstall, canonical),
            finding => finding.Contains("unexpected npm restore owner or command", StringComparison.Ordinal));
        var foreignAdd = ReplayRestoreControl("npm add flatbuffers", ownerTarget: "UnexpectedTarget");
        Assert.Equal(BuildWork.Restore, Assert.Single(foreignAdd.Invocations).Work);
        Assert.Empty(foreignAdd.Writers);
        Assert.Contains(RestoreViolations("unowned npm add control", foreignAdd),
            finding => finding.Contains("npm add flatbuffers", StringComparison.Ordinal));
        var foreignAssigned = ReplayRestoreControl("CI=true npm ci", ownerTarget: "UnexpectedTarget");
        Assert.Equal(BuildWork.Restore, Assert.Single(foreignAssigned.Invocations).Work);
        Assert.Empty(foreignAssigned.Writers);
        Assert.Contains(RestoreViolations("unowned assigned npm control", foreignAssigned),
            finding => finding.Contains("CI=true npm ci", StringComparison.Ordinal));
        var assigned = ReplayRestoreControl("CI=true npm ci --ignore-scripts --no-audit --no-fund");
        Assert.Contains(RestoreViolations("assigned command in canonical owner", assigned),
            finding => finding.Contains("unexpected npm restore owner or command", StringComparison.Ordinal));

        string[] globalOptionsWithValues =
        [
            "--global --prefix \"foreign install root\" --loglevel=quiet", "--omit dev", "--fetch-retries 1", "--maxsockets 2",
            "--script-shell /bin/sh", "--logs-dir \"logs with space\""
        ];
        foreach (var globalOption in globalOptionsWithValues)
        foreach (var subcommand in new[] { "install", "ci" })
        {
            var globalOptions = ReplayRestoreControl($"npm {globalOption} {subcommand}", ownerTarget: "UnexpectedTarget");
            Assert.Equal(BuildWork.Restore, Assert.Single(globalOptions.Invocations).Work);
            Assert.Empty(globalOptions.Writers);
            Assert.Contains(RestoreViolations("globally configured npm control", globalOptions),
                finding => finding.Contains("unexpected npm restore owner or command", StringComparison.Ordinal));
        }
        AssertCompilerReplayControls(canonical);
        var activeRestore = new CompilerRuns([.. control.Invocations, .. restores.Invocations], [.. control.Writers, .. restores.Writers]);
        Assert.Contains(CompilerGraphViolations("restore overlap control", activeRestore, canonical),
            finding => finding.Contains("Restore overlaps", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("two-schema control", control with
        {
            Invocations = [csharp, ts with { Arguments = [.. ts.Arguments, foreign] }, runtime]
        }, canonical), finding => finding.Contains("2 schema inputs", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("foreign-schema control", control with
        {
            Invocations = [csharp, ts with { Arguments = [.. ts.Arguments[..^1], foreign] }, runtime]
        }, canonical), finding => finding.Contains(foreign, StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("extra-owner control", control with
        {
            Invocations = [.. control.Invocations, extra]
        }, canonical), finding => finding.Contains("unexpected flatc owner", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("zero-generator control", control with { Invocations = [csharp, runtime] }, canonical),
            finding => finding.Contains("started 0 times", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("two-generator control", control with { Invocations = [.. control.Invocations, ts] }, canonical),
            finding => finding.Contains("started 2 times", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("early-runtime control", control with
        {
            Invocations = [csharp, ts, runtime with { At = 10 }]
        }, canonical), finding => finding.Contains("before TypeScript generation completed", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("overlap control", control with { Writers = [.. control.Writers, new(BuildWork.Clean, 9, 13)] }, canonical),
            finding => finding.Contains("overlaps", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("reconcile overlap control", control with
        {
            Writers = [.. control.Writers, new(BuildWork.Reconcile, 14, 17)]
        }, canonical), finding => finding.Contains("Reconcile overlaps", StringComparison.Ordinal));
        var foreignRuntime = CaptureExec(AbstractionsProject, "UnexpectedTarget", "node /foreign/node_modules/typescript/bin/tsc -p tsconfig.runtime.json", 14)!;
        Assert.Contains(CompilerGraphViolations("foreign compiler control", control with
        {
            Invocations = [.. control.Invocations, foreignRuntime]
        }, canonical), finding => finding.Contains("unexpected runtime tsc", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("two runtime compilers control", control with
        {
            Invocations = [.. control.Invocations, runtime]
        }, canonical), finding => finding.Contains("runtime tsc started 2 times", StringComparison.Ordinal));
    }

    private static void AssertCompilerReplayControls(string schema)
    {
        var paths = ControlCompilerPaths(AbstractionsProject, schema);
        var runtime = RuntimeCommand(paths.RuntimeOutput);
        var generation = FlatcCommand(paths, schema, BuildWork.TypeScript);
        var replay = ReplayGenerationControl(schema, generation, runtime);
        Assert.Equal(new[] { BuildWork.CSharp, BuildWork.TypeScript, BuildWork.Runtime, BuildWork.Reconcile },
            replay.Writers.Select(writer => writer.Work));
        Assert.Empty(CompilerGraphViolations("executing and skipped compiler visits", replay, schema));

        var wrongTypeScript = ReplayGenerationControl(schema, generation.Replace("--gen-object-api ", "", StringComparison.Ordinal), runtime);
        Assert.Contains(CompilerGraphViolations("noncanonical flatc arguments", wrongTypeScript, schema),
            finding => finding.Contains("unexpected flatc owner or arguments", StringComparison.Ordinal));
        var wrongRuntime = ReplayGenerationControl(schema, generation, runtime.Replace("node ", "node.exe ", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("noncanonical tsc executable", wrongRuntime, schema),
            finding => finding.Contains("unexpected runtime tsc invocation", StringComparison.Ordinal));

        var rawSchema = generation.Replace(QuoteShellPath(schema), $"\"{schema}\"", StringComparison.Ordinal);
        var rawOutput = runtime.Replace(QuoteShellPath(paths.RuntimeOutput), $"\"{paths.RuntimeOutput}\"", StringComparison.Ordinal);
        var expanded = ReplayGenerationControl(schema, rawSchema, rawOutput);
        Assert.Equal(replay.Invocations.Select(invocation => string.Join(" ", invocation.Arguments)),
            expanded.Invocations.Select(invocation => string.Join(" ", invocation.Arguments)));
        Assert.Contains(CompilerGraphViolations("unprotected compiler paths", expanded, schema),
            finding => finding.Contains("unexpected flatc owner or arguments", StringComparison.Ordinal));
        Assert.Contains(CompilerGraphViolations("unprotected compiler paths", expanded, schema),
            finding => finding.Contains("unexpected runtime tsc invocation", StringComparison.Ordinal));

        var foreign = paths with { IntermediateOutput = Path.Combine(Path.GetTempPath(), "foreign intermediate") };
        var foreignGeneration = ReplayGenerationControl(schema, FlatcCommand(foreign, schema, BuildWork.TypeScript), runtime);
        Assert.Contains(CompilerGraphViolations("same-name foreign flatc output", foreignGeneration, schema),
            finding => finding.Contains("unexpected flatc owner or arguments", StringComparison.Ordinal));
        var csharp = ControlCompilerPaths(VendoredProject, schema) with { IntermediateOutput = foreign.IntermediateOutput };
        var foreignCSharp = ReplayGenerationControl(schema, generation, runtime, csharpCommand: FlatcCommand(csharp, schema, BuildWork.CSharp));
        Assert.Contains(CompilerGraphViolations("same-name foreign C# output", foreignCSharp, schema),
            finding => finding.Contains("unexpected flatc owner or arguments", StringComparison.Ordinal));
        var foreignRuntime = ReplayGenerationControl(schema, generation, RuntimeCommand(Path.Combine(foreign.IntermediateOutput, ".runtime-build")));
        Assert.Contains(CompilerGraphViolations("same-name foreign tsc output", foreignRuntime, schema),
            finding => finding.Contains("unexpected runtime tsc invocation", StringComparison.Ordinal));
        var foreignCompiler = paths with { Flatc = Path.Combine(foreign.IntermediateOutput, "flatc") };
        var wrongCompiler = ReplayGenerationControl(schema, FlatcCommand(foreignCompiler, schema, BuildWork.TypeScript), runtime);
        Assert.Contains(CompilerGraphViolations("same-name foreign flatc executable", wrongCompiler, schema),
            finding => finding.Contains("unexpected flatc owner or arguments", StringComparison.Ordinal));
        foreach (var after in new[] { BuildWork.TypeScript, BuildWork.Runtime })
        {
            var late = ReplayGenerationControl(schema, generation, runtime, restoreAfter: after);
            Assert.Contains(RestoreViolations("late canonical restore", late),
                finding => finding.Contains("must finish before TypeScript generation and runtime tsc", StringComparison.Ordinal));
        }
    }

    private static CompilerPaths ControlCompilerPaths(string project, string schema)
    {
        var checkout = Path.GetDirectoryName(Path.GetDirectoryName(schema))!;
        return new(Path.Combine(checkout, project), "obj/Debug/net10.0", Path.Combine(checkout, "pinned compiler", "flatc"),
            Path.Combine(checkout, ".runtime-build"));
    }

    private static string FlatcCommand(CompilerPaths paths, string schema, BuildWork work)
    {
        var switches = work == BuildWork.CSharp ? "--csharp" : "--ts --gen-object-api";
        return $"{QuoteShellPath(paths.Flatc)} {switches} -o {QuoteShellPath(FlatcOutput(paths, work))} {QuoteShellPath(schema)}";
    }

    private static string RuntimeCommand(string output) =>
        $"node ./node_modules/typescript/bin/tsc -p tsconfig.runtime.json --outDir {QuoteShellPath(output)}";

    private static string QuoteShellPath(string path) => "'" + path.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";

    private static CompilerRuns ReplayGenerationControl(string schema, string generation, string runtime, string? csharpCommand = null,
        BuildWork? restoreAfter = null) => ReplayCompilerRuns(raise =>
    {
        var csharp = ControlCompilerPaths(VendoredProject, schema);
        var typescript = ControlCompilerPaths(AbstractionsProject, schema);
        ReplayCompilerVisit(raise, VendoredProject, "GenerateWireAccessors", 1,
            csharpCommand ?? FlatcCommand(csharp, schema, BuildWork.CSharp), csharp);
        ReplayCompilerVisit(raise, AbstractionsProject, "GenerateWireTypeScript", 2, generation, typescript);
        if (restoreAfter == BuildWork.TypeScript)
            ReplayCompilerVisit(raise, AbstractionsProject, "RestoreRuntimeTypeScript", 5, string.Join(' ', RuntimeRestoreCommand));
        ReplayCompilerVisit(raise, AbstractionsProject, "BuildRuntimeTypeScript", 3, runtime, typescript);
        ReplayCompilerVisit(raise, AbstractionsProject, "ReconcileCompiledWireTypeScript", 4, command: null);
        if (restoreAfter == BuildWork.Runtime)
            ReplayCompilerVisit(raise, AbstractionsProject, "RestoreRuntimeTypeScript", 5, string.Join(' ', RuntimeRestoreCommand));
    });

    private static void ReplayCompilerVisit(Action<BuildEventArgs> raise, string project, string target, int context, string? command,
        CompilerPaths? paths = null)
    {
        var active = new BuildEventContext(submissionId: 0, nodeId: 1, evaluationId: context, projectInstanceId: context,
            projectContextId: context, targetId: 1, taskId: 1);
        if (paths is not null)
        {
            raise(new ProjectEvaluationFinishedEventArgs
            {
                BuildEventContext = active,
                ProjectFile = paths.ProjectFile,
                Properties = new Hashtable
                {
                    ["IntermediateOutputPath"] = paths.IntermediateOutput,
                    ["FlatcPath"] = paths.Flatc,
                    ["_RuntimeTypeScriptOutput"] = paths.RuntimeOutput
                }
            });
            raise(new ProjectStartedEventArgs("", "", paths.ProjectFile, target, Array.Empty<DictionaryEntry>(), Array.Empty<DictionaryEntry>())
                { BuildEventContext = active });
        }
        raise(new TargetStartedEventArgs("", null, target, project, null) { BuildEventContext = active });
        if (command is not null)
        {
            var reuse = new BuildEventContext(nodeId: 1, targetId: 1, projectContextId: context + 100, taskId: 1);
            raise(new TargetStartedEventArgs("", null, target, project, null) { BuildEventContext = reuse });
            raise(new TaskCommandLineEventArgs(command, "Exec", MessageImportance.Low) { BuildEventContext = active });
            raise(new TargetFinishedEventArgs("", null, target, project, null, true) { BuildEventContext = reuse });
        }
        raise(new TargetFinishedEventArgs("", null, target, project, null, true) { BuildEventContext = active });
    }

    private static readonly string[] RuntimeRestoreCommand = ["npm", "ci", "--ignore-scripts", "--no-audit", "--no-fund"];

    private static CompilerRuns ReplayRestoreControl(string command = "npm ci --ignore-scripts --no-audit --no-fund",
        bool includeOwner = true, string ownerTarget = "RestoreRuntimeTypeScript") => ReplayCompilerRuns(raise =>
    {
        const string target = "RestoreRuntimeTypeScript";
        var active = new BuildEventContext(nodeId: 1, targetId: 2, projectContextId: 3, taskId: 4);
        var skipped = new BuildEventContext(nodeId: 1, targetId: 2, projectContextId: 5, taskId: 4);
        if (includeOwner)
            raise(new TargetStartedEventArgs("", null, ownerTarget, AbstractionsProject, null) { BuildEventContext = active });
        raise(new TargetStartedEventArgs("", null, target, AbstractionsProject, null) { BuildEventContext = skipped });
        raise(new TaskCommandLineEventArgs(command, "Exec", MessageImportance.Low)
            { BuildEventContext = active });
        raise(new TargetFinishedEventArgs("", null, target, AbstractionsProject, null, true) { BuildEventContext = skipped });
        raise(new TargetFinishedEventArgs("", null, ownerTarget, AbstractionsProject, null, true) { BuildEventContext = active });
    });

    private enum BuildWork { CSharp, TypeScript, Runtime, Stamp, Clean, Reconcile, Restore }

    private sealed record CompilerPaths(string ProjectFile, string IntermediateOutput, string Flatc, string RuntimeOutput);
    private sealed record CompilerInvocation(BuildWork? Work, string Project, string Target, string[] Arguments, int At, CompilerPaths? Paths, bool UnprotectedDollar);
    private sealed record WriterInterval(BuildWork Work, int Start, int End);
    private sealed record CompilerRuns(List<CompilerInvocation> Invocations, List<WriterInterval> Writers);

    private static List<string> RestoreViolations(string log, CompilerRuns runs)
    {
        var violations = new List<string>();
        var activeRestores = runs.Invocations.Where(invocation => invocation.Work == BuildWork.Restore).ToArray();
        foreach (var restore in activeRestores)
            if (WorkOf(restore.Project, restore.Target) != BuildWork.Restore ||
                !restore.Arguments.SequenceEqual(RuntimeRestoreCommand, StringComparer.Ordinal))
                violations.Add($"{log}: unexpected npm restore owner or command in {restore.Project}:{restore.Target} [{string.Join(" ", restore.Arguments)}]");
        var restoreWriters = runs.Writers.Where(writer => writer.Work == BuildWork.Restore).ToArray();
        if (activeRestores.Length != restoreWriters.Length ||
            activeRestores.Any(restore => restoreWriters.Count(writer => writer.Start < restore.At && restore.At < writer.End) != 1))
            violations.Add($"{log}: {activeRestores.Length} active npm restore Execs must each have one writer interval (found {restoreWriters.Length})");
        foreach (var restore in restoreWriters)
            if (runs.Invocations.Any(invocation => invocation.Work is BuildWork.TypeScript or BuildWork.Runtime && invocation.At <= restore.End))
                violations.Add($"{log}: npm restore must finish before TypeScript generation and runtime tsc");
        return violations;
    }

    private static List<string> CompilerGraphViolations(string log, CompilerRuns runs, string expectedSchema)
    {
        var violations = RestoreViolations(log, runs);
        var typescript = runs.Invocations.Count(invocation => invocation.Work == BuildWork.TypeScript);
        if (typescript != 1)
            violations.Add($"{log}: GenerateWireTypeScript in {AbstractionsProject} started {typescript} times, not exactly once");
        if (!runs.Invocations.Exists(invocation => invocation.Work == BuildWork.CSharp))
            violations.Add($"{log}: no C# schema compiler invocation in {VendoredProject}");
        foreach (var invocation in runs.Invocations.Where(invocation => invocation.Work is not (BuildWork.Runtime or BuildWork.Restore)))
        {
            var owner = $"{invocation.Project}:{invocation.Target}";
            if (!HasCanonicalFlatcArguments(invocation))
                violations.Add($"{log}: unexpected flatc owner or arguments {owner}");
            if (invocation.Work is { } work && runs.Writers.Count(writer => OwnsInvocation(writer, work, invocation.At)) != 1)
                violations.Add($"{log}: {owner} did not run within one matching generation interval");
            var schemas = invocation.Arguments.Where(argument => argument.EndsWith(".fbs", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (schemas.Length != 1 || !Path.IsPathFullyQualified(schemas[0]) ||
                !string.Equals(Path.GetFullPath(schemas[0]), Path.GetFullPath(expectedSchema), StringComparison.Ordinal))
                violations.Add($"{log}: {owner} passed {schemas.Length} schema inputs [{string.Join(", ", schemas)}], expected only {expectedSchema}");
        }

        var generation = runs.Writers.FirstOrDefault(writer => writer.Work == BuildWork.TypeScript &&
            runs.Invocations.Exists(invocation => invocation.Work == BuildWork.TypeScript && OwnsInvocation(writer, BuildWork.TypeScript, invocation.At)));
        var runtimeCompiles = runs.Invocations.Where(invocation => invocation.Work == BuildWork.Runtime).ToArray();
        if (runtimeCompiles.Length != 1)
            violations.Add($"{log}: runtime tsc started {runtimeCompiles.Length} times, expected exactly one compile");
        foreach (var invocation in runtimeCompiles)
        {
            if (WorkOf(invocation.Project, invocation.Target) != BuildWork.Runtime ||
                !HasCanonicalRuntimeArguments(invocation))
                violations.Add($"{log}: unexpected runtime tsc invocation in {invocation.Project}:{invocation.Target}");
            if (generation is null || invocation.At <= generation.End)
                violations.Add($"{log}: runtime tsc ran before TypeScript generation completed or generation did not run");
            var compile = runs.Writers.Find(writer => OwnsInvocation(writer, BuildWork.Runtime, invocation.At));
            if (compile is null)
                violations.Add($"{log}: runtime tsc did not run within the Abstractions compile interval");
            else if (!runs.Writers.Exists(writer => writer.Work == BuildWork.Reconcile && writer.Start > compile.End))
                violations.Add($"{log}: no emitted wire reconciliation interval after runtime compile");
        }
        int previousEnd = -1;
        // C# generation writes a disjoint intermediate tree. Its invocation still needs its own interval.
        foreach (var writer in runs.Writers.Where(writer => writer.Work != BuildWork.CSharp).OrderBy(writer => writer.Start))
        {
            if (writer.Start < previousEnd)
                violations.Add($"{log}: {writer.Work} overlaps a wire writer");
            previousEnd = Math.Max(previousEnd, writer.End);
        }
        return violations;
    }

    private static bool OwnsInvocation(WriterInterval writer, BuildWork work, int at) =>
        writer.Work == work && writer.Start < at && at < writer.End;

    private static bool HasCanonicalFlatcArguments(CompilerInvocation invocation)
    {
        if (invocation.UnprotectedDollar || invocation.Paths is not { } paths)
            return false;
        return invocation.Work switch
        {
            BuildWork.CSharp when invocation.Arguments is [var compiler, "--csharp", "-o", var destination, _] =>
                SamePath(compiler, paths.Flatc) && SamePath(destination, FlatcOutput(paths, BuildWork.CSharp)),
            BuildWork.TypeScript when invocation.Arguments is [var compiler, "--ts", "--gen-object-api", "-o", var destination, _] =>
                SamePath(compiler, paths.Flatc) && SamePath(destination, FlatcOutput(paths, BuildWork.TypeScript)),
            _ => false,
        };
    }

    private static string FlatcOutput(CompilerPaths paths, BuildWork work) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(paths.ProjectFile)!,
            paths.IntermediateOutput.Replace('\\', Path.DirectorySeparatorChar),
            work == BuildWork.CSharp ? "WireAccessors" : "WireTypeScriptStaging"));

    private static bool HasCanonicalRuntimeArguments(CompilerInvocation invocation) =>
        !invocation.UnprotectedDollar && invocation.Paths is { } paths &&
        invocation.Arguments is ["node", "./node_modules/typescript/bin/tsc", "-p", "tsconfig.runtime.json", "--outDir", var output] &&
        SamePath(output, paths.RuntimeOutput);

    private static bool SamePath(string actual, string expected) =>
        Path.IsPathFullyQualified(actual) && Path.IsPathFullyQualified(expected) &&
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(actual)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(expected)), StringComparison.Ordinal);

    private static BuildWork? WorkOf(string project, string target)
    {
        if (Path.IsPathFullyQualified(project))
            project = Path.GetRelativePath(Root, project);
        project = project.Replace('\\', '/');
        if (project == VendoredProject && target == "GenerateWireAccessors")
            return BuildWork.CSharp;
        if (project != AbstractionsProject)
            return null;
        return target switch
        {
            "GenerateWireTypeScript" => BuildWork.TypeScript,
            "BuildRuntimeTypeScript" => BuildWork.Runtime,
            "ReconcileCompiledWireTypeScript" => BuildWork.Reconcile,
            "StampWireTypeScriptInputs" => BuildWork.Stamp,
            "CleanWireTypeScriptInputs" => BuildWork.Clean,
            "RestoreRuntimeTypeScript" => BuildWork.Restore,
            _ => null
        };
    }

    private static CompilerInvocation? CaptureExec(string project, string target, string command, int ordinal, CompilerPaths? paths = null)
    {
        var (tokens, executableIndex, unprotectedDollar) = ShellCommandTokens(command);
        var work = WorkOf(project, target);
        var executable = executableIndex < 0 ? "" : Path.GetFileName(tokens[executableIndex].Replace('\\', '/'));
        if (work == BuildWork.Restore || executable.ToLowerInvariant() is "npm" or "npm.cmd" or "npm.exe")
            return new(BuildWork.Restore, project, target, tokens, ordinal, paths, unprotectedDollar);
        if (string.Equals(executable, "flatc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(executable, "flatc.exe", StringComparison.OrdinalIgnoreCase))
            return new(work is BuildWork.CSharp or BuildWork.TypeScript ? work : null, project, target, tokens, ordinal, paths, unprotectedDollar);
        var runsTypeScript = executable is "tsc" or "tsc.cmd" ||
            (executable is "node" or "node.exe" && executableIndex + 1 < tokens.Length &&
                Path.GetFileName(tokens[executableIndex + 1].Replace('\\', '/')) is "tsc" or "tsc.js" or "_tsc.js");
        return runsTypeScript && !tokens.Contains("--version", StringComparer.Ordinal)
            ? new(BuildWork.Runtime, project, target, tokens, ordinal, paths, unprotectedDollar) : null;
    }

    private static (string[] Tokens, int ExecutableIndex, bool UnprotectedDollar) ShellCommandTokens(string command)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        var quote = '\0';
        var wordStarted = false;
        var unprotectedDollar = false;
        var executableIndex = -1;
        for (var index = 0; index < command.Length; index++)
        {
            var character = command[index];
            if (!wordStarted && executableIndex < 0 && !char.IsWhiteSpace(character) &&
                !StartsWithShellAssignment(command.AsSpan(index)))
                executableIndex = words.Count;
            // Canonical path arguments use single quotes. Double quotes still allow shell expansion.
            if (character == '$' && quote != '\'')
                unprotectedDollar = true;
            if (character == quote)
                quote = '\0';
            else if (quote == '\0' && character is '\'' or '"')
            {
                quote = character;
                wordStarted = true;
            }
            else if (quote != '\0' || !char.IsWhiteSpace(character))
            {
                word.Append(character);
                wordStarted = true;
            }
            else if (wordStarted)
            {
                words.Add(word.ToString());
                word.Clear();
                wordStarted = false;
            }
        }
        if (wordStarted)
            words.Add(word.ToString());
        return (words.ToArray(), executableIndex, unprotectedDollar);
    }

    private static bool StartsWithShellAssignment(ReadOnlySpan<char> word)
    {
        if (!char.IsAsciiLetter(word[0]) && word[0] != '_')
            return false;
        var nameEnd = 1;
        while (nameEnd < word.Length && (char.IsAsciiLetterOrDigit(word[nameEnd]) || word[nameEnd] == '_'))
            nameEnd++;
        return nameEnd < word.Length && word[nameEnd] == '=';
    }

    private static CompilerRuns ReadCompilerRuns(string binlog) =>
        ReplayCompilerRuns(handler => CSharpStaticGateTests.ReplayBuildLog(binlog, handler));

    private static CompilerRuns ReplayCompilerRuns(Action<Action<BuildEventArgs>> replay)
    {
        var targets = new Dictionary<(int, int, int), (string Project, string Target, int Start)>();
        var evaluations = new Dictionary<(int Node, int Evaluation), CompilerPaths>();
        var projectEvaluations = new Dictionary<(int Node, int Project), int>();
        var executing = new HashSet<(int, int, int)>();
        var runs = new CompilerRuns([], []);
        int ordinal = 0;
        replay(raised =>
        {
            ordinal++;
            if (raised.BuildEventContext is not { } context)
                return;
            var key = (context.NodeId, context.ProjectContextId, context.TargetId);
            if (raised is ProjectEvaluationFinishedEventArgs evaluation && ReadCompilerPaths(evaluation) is { } paths)
                evaluations[(context.NodeId, context.EvaluationId)] = paths;
            else if (raised is ProjectStartedEventArgs)
                projectEvaluations[(context.NodeId, context.ProjectContextId)] = context.EvaluationId;
            else if (raised is TargetStartedEventArgs target)
                targets.Add(key, (target.ProjectFile ?? "", target.TargetName, ordinal));
            else if (raised is TaskCommandLineEventArgs { TaskName: "Exec" } exec)
            {
                var owner = targets.GetValueOrDefault(key);
                var evaluationId = projectEvaluations.GetValueOrDefault((context.NodeId, context.ProjectContextId), -1);
                var owningPaths = evaluations.GetValueOrDefault((context.NodeId, evaluationId));
                var invocation = CaptureExec(owner.Project ?? exec.ProjectFile ?? "", owner.Target ?? "", exec.CommandLine, ordinal, owningPaths);
                if (invocation is null)
                    return;
                runs.Invocations.Add(invocation);
                if (invocation.Work is { } work && work == WorkOf(owner.Project ?? "", owner.Target ?? ""))
                    executing.Add(key);
            }
            else if (raised is TargetFinishedEventArgs && targets.Remove(key, out var ended) &&
                WorkOf(ended.Project, ended.Target) is { } writer)
                TrackWriter(runs, writer, ended.Start, ordinal, executing.Remove(key));
        });
        return runs;
    }

    private static CompilerPaths? ReadCompilerPaths(ProjectEvaluationFinishedEventArgs evaluation)
    {
        if (evaluation.ProjectFile is not { } project || evaluation.Properties is not { } properties)
            return null;
        var paths = properties.Cast<DictionaryEntry>()
            .Where(entry => entry.Key is "IntermediateOutputPath" or "FlatcPath" or "_RuntimeTypeScriptOutput")
            .ToDictionary(entry => (string)entry.Key, entry => entry.Value as string, StringComparer.Ordinal);
        return paths.GetValueOrDefault("IntermediateOutputPath") is { Length: > 0 } intermediate &&
            paths.GetValueOrDefault("FlatcPath") is { Length: > 0 } compiler &&
            paths.GetValueOrDefault("_RuntimeTypeScriptOutput") is { Length: > 0 } runtime
            ? new(project, intermediate, compiler, runtime) : null;
    }

    private static void TrackWriter(CompilerRuns runs, BuildWork writer, int start, int end, bool invoked)
    {
        // Incremental compiler and restore target visits can have an interval without executing the command.
        if (invoked || writer is not (BuildWork.CSharp or BuildWork.TypeScript or BuildWork.Runtime or BuildWork.Restore))
            runs.Writers.Add(new(writer, start, end));
    }

    // ----- S12: the FlatBuffers TypeScript runtime is pinned once, at the root ------------------------------------------

    [Fact]
    public void TheFlatBuffersRuntimeIsPinnedAtTheRoot()
    {
        var version = Regex.Match(File.ReadAllText(Path.Combine(Root, CompilerTargets)), "<FlatcVersion>([^<]+)</FlatcVersion>").Groups[1].Value;
        Assert.Matches(@"^\d+\.\d+\.\d+$", version);
        var pins = new RootRuntimeManifests(
            File.ReadAllText(Path.Combine(Root, "package.json")),
            File.ReadAllText(Path.Combine(Root, "package-lock.json")),
            File.Exists(Path.Combine(Root, "tools", "e2e", "package.json")),
            File.Exists(Path.Combine(Root, "tools", "e2e", "package-lock.json")),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tsconfig.runtime.json"] = File.ReadAllText(Path.Combine(Root, "tsconfig.runtime.json")),
                ["tools/e2e/tsconfig.json"] = File.ReadAllText(Path.Combine(Root, "tools", "e2e", "tsconfig.json")),
            });
        Assert.Empty(RootRuntimePinViolations(pins, version));

        // Controls: a version range, a tools/e2e/package.json naming flatbuffers, a version differing from <FlatcVersion>, and a paths mapping are each refused.
        var pinned = $"\"flatbuffers\": \"{version}\"";
        Assert.Contains($"package.json: devDependencies.flatbuffers is ^{version}, not exactly {version}",
            RootRuntimePinViolations(pins with { RootManifest = pins.RootManifest.Replace(pinned, $"\"flatbuffers\": \"^{version}\"", StringComparison.Ordinal) }, version));
        Assert.Contains("tools/e2e/package.json must be absent",
            RootRuntimePinViolations(pins with { HasE2eManifest = true }, version));
        Assert.Contains("package.json: devDependencies.flatbuffers is 25.2.10, not exactly 9.9.9",
            RootRuntimePinViolations(pins, "9.9.9"));
        Assert.Contains("tools/e2e/tsconfig.json: compilerOptions.paths maps flatbuffers to [\"/x\"]",
            RootRuntimePinViolations(pins with { Tsconfigs = new(pins.Tsconfigs) { ["tools/e2e/tsconfig.json"] = """{ "compilerOptions": { "paths": { "flatbuffers": ["/x"] } } }""" } }, version));
    }

    internal sealed record RootRuntimeManifests(string RootManifest, string RootLock, bool HasE2eManifest, bool HasE2eLock, Dictionary<string, string> Tsconfigs);

    private static readonly JsonDocumentOptions PinJsonOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
    private static readonly string[] LockedSourceFields = ["resolved", "integrity"];

    internal static List<string> RootRuntimePinViolations(RootRuntimeManifests manifests, string version)
    {
        var violations = new List<string>();
        using var rootManifest = JsonDocument.Parse(manifests.RootManifest, PinJsonOptions);
        var declared = JsonText(rootManifest.RootElement, "devDependencies", "flatbuffers");
        if (declared != version) violations.Add($"package.json: devDependencies.flatbuffers is {declared ?? "absent"}, not exactly {version}");
        using var rootLock = JsonDocument.Parse(manifests.RootLock, PinJsonOptions);
        var lockedRoot = JsonText(rootLock.RootElement, "packages", "", "devDependencies", "flatbuffers");
        if (lockedRoot != version) violations.Add($"package-lock.json: root devDependencies.flatbuffers is {lockedRoot ?? "absent"}, not exactly {version}");
        var locked = JsonText(rootLock.RootElement, "packages", "node_modules/flatbuffers", "version");
        if (locked != version) violations.Add($"package-lock.json: node_modules/flatbuffers is {locked ?? "absent"}, not exactly {version}");
        foreach (var field in LockedSourceFields.Where(f => string.IsNullOrEmpty(JsonText(rootLock.RootElement, "packages", "node_modules/flatbuffers", f))))
            violations.Add($"package-lock.json: node_modules/flatbuffers has no {field}");
        if (manifests.HasE2eManifest) violations.Add("tools/e2e/package.json must be absent");
        if (manifests.HasE2eLock) violations.Add("tools/e2e/package-lock.json must be absent");
        foreach (var (path, text) in manifests.Tsconfigs.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            using var ts = JsonDocument.Parse(text, PinJsonOptions);
            if (JsonFind(ts.RootElement, "compilerOptions", "paths", "flatbuffers") is { } mapping)
                violations.Add($"{path}: compilerOptions.paths maps flatbuffers to {mapping.GetRawText()}");
        }
        return violations;
    }

    private static JsonElement? JsonFind(JsonElement element, params string[] path)
    {
        foreach (var name in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element)) return null;
        }
        return element;
    }

    private static string? JsonText(JsonElement element, params string[] path) =>
        JsonFind(element, path) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;
    // S11: these checks are limited to the retired forms named in the contract.
    private static readonly string[] RemovedSchemaNames =
    [
        "SNAPSHOT_" + "SCHEMA",
        "TheVersionlessRecords" + "DeclareExactlyTheirFields",
        "InstanceRecord" + "Fields",
        "SavedConstruction" + "Slot",
        "ConstructionInstances" + "Slot",
        "HoldsFieldBeyond" + "TheInstanceTable",
        "ValidateBinding" + "Shapes",
        "Table" + "Target",
        "COMMAND_" + "IDENTIFIER",
        "RESPONSE_" + "IDENTIFIER",
        "SAVE_" + "IDENTIFIER",
        "Command" + "Identifier",
        "Response" + "Identifier",
        "Observation" + "Identifier",
    ];

    private static readonly string[] RootIdentifiers = Enum.GetValues<Wire.WireRootIdentifier>()
        .Select(WorkshopWireFlatBuffers.FileIdentifier).ToArray();

    private static readonly Regex ForbiddenSchemaWords = new(
        @"\b(?:" + string.Join('|', RemovedSchemaNames.Concat(RootIdentifiers).Select(Regex.Escape)) + @")\b",
        RegexOptions.CultureInvariant);

    private const string SaveRejections = "CuriousContraptions.tests/WorkshopSaveRejectionTests.cs";
    private const string SchemaLayoutFile = "CuriousContraptions.tests/WireSchemaText.cs";
    private const string FrameAdapter = "tools/runtime/wire-frame.ts";
    private const string HistoricalBegin = "// historical layout: begin";
    private const string HistoricalEnd = "// historical layout: end";

    private static readonly Dictionary<string, int> HistoricalCounts = new(StringComparer.Ordinal)
    {
        ["CuriousContraptions.tests/OldLayoutSaves.cs"] = 1,
        ["CuriousContraptions.tests/DeclaredOscillatorTests.cs"] = 1,
        ["CuriousContraptions.tests/DeclaredOrientationTests.cs"] = 1,
        ["CuriousContraptions.tests/PayloadInstanceTests.cs"] = 1,
        ["CuriousContraptions.tests/ClutchGeneralityTests.cs"] = 1,
    };

    private static readonly Regex OldInstanceFieldEnum = new(@"\benum\s+Field\b", RegexOptions.CultureInvariant);
    private static readonly CSharpParseOptions[] SourceVariants =
    [
        CSharpParseOptions.Default.WithPreprocessorSymbols("DEBUG", "TRACE"),
        CSharpParseOptions.Default.WithPreprocessorSymbols("DEBUG", "TRACE", "PLAYTEST"),
    ];
    // Each recognized signature takes the table size or slot first in its declaration.
    private static readonly Dictionary<(string Name, int Arity), string> CSharpSlotParameters =
        typeof(Google.FlatBuffers.FlatBufferBuilder).GetMethods()
            .Where(method => method.Name == "StartTable" || method.Name.StartsWith("Add", StringComparison.Ordinal) &&
                method.GetParameters() is [{ ParameterType: var slot }, _, ..] && slot == typeof(int))
            .Concat(typeof(Google.FlatBuffers.Table).GetMethods().Where(method => method.Name == "__offset"))
            .Select(method => (method.Name, Parameters: method.GetParameters()))
            .ToDictionary(method => (method.Name, method.Parameters.Length), method => method.Parameters[0].Name!);

    [Fact]
    public void NoHandSchemaRestatementRemains()
    {
        var (paths, violations) = ScanSchemaSources(CSharpStaticGateTests.RepositoryFiles().Order(StringComparer.Ordinal),
            path => File.ReadAllText(Path.Combine(Root, path)));
        Assert.Contains(FrameAdapter, paths);
        Assert.Contains(SaveRejections, paths);
        foreach (var path in HistoricalCounts.Keys) Assert.Contains(path, paths);
        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void SchemaRestatementGuardRefusesTheClosedFormsAndChecksHistoricalRegions()
    {
        const string cs = "Probe.cs";
        foreach (var word in RemovedSchemaNames.Concat(RootIdentifiers))
            Assert.Equal([$"{cs}:2: hand schema name '{word}'"], RestatementViolations(cs, $"\n// {word}\n"));
        Assert.Equal([$"{SaveRejections}:1: hand instance field enum"], RestatementViolations(SaveRejections, "enum Field { Id }"));
        Assert.Empty(RestatementViolations(cs, "enum Field { Id }"));

        foreach (var call in new[]
        {
            "builder.StartTable(12)", "builder.StartTable(numfields: 12)",
            "builder.AddInt(2, value, 0)", "builder.AddInt(x: value, d: 0, o: 2)",
            "builder.AddInt(2, nullable)", "builder.AddInt(x: nullable, o: 2)",
            "builder.AddStruct(x: value, d: 0, voffset: 2)",
            "table.__offset(0xA)", "table.__offset(vtableOffset: 0xA)",
            "Table.__offset(4, position, buffer)", "Table.__offset(bb: buffer, offset: position, vtableOffset: 4)"
        })
            Assert.Equal([$"{cs}:2: literal FlatBuffers table slot or size"], RestatementViolations(cs, $"\n{call};"));
        foreach (var expression in new[] { "4 + 2 * slot", "4 + slot * 2", "slot * 2 + 4",
            "vtable + 4 + 2 * slot", "vtable + (4 + slot * 2)", "vtable + slot * 2 + 4" })
            Assert.Equal([$"{cs}:2: hand FlatBuffers vtable offset"], RestatementViolations(cs, $"\nbytes.AsSpan({expression});"));

        AssertTypeScriptCalls();

        Assert.Empty(RestatementViolations(cs, "builder.AddInt(10); builder.AddOffset(3); builder.StartTable(fieldCount); bytes.AsSpan(4 + width);"));
        foreach (var call in new[]
        {
            "builder.AddInt(x: 12)", "builder.AddInt(x: 12, d: 0, o: slot)", "builder.AddInt(x: 12, o: slot)",
            "builder.AddStruct(x: 12, d: 0, voffset: slot)", "table.__offset(vtableOffset: field)",
            "Table.__offset(offset: 12, bb: buffer, vtableOffset: field)"
        })
            Assert.Empty(RestatementViolations(cs, call + ";"));
        Assert.False(ScansSchemaSource("tools/e2e/.wire/curious-contraptions/wire.ts"));
        Assert.False(ScansSchemaSource("CuriousContraptions.FlatBuffers/FlatBufferBuilder.cs"));
        Assert.True(ScansSchemaSource("CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj"));

        var fixture = HistoricalCounts.Keys.First();
        var retired = $"{HistoricalBegin}\nbuilder.StartTable(12);\n{HistoricalEnd}\n";
        Assert.Empty(RestatementViolations(fixture, retired));
        Assert.Equal([$"{fixture}:4: literal FlatBuffers table slot or size"],
            RestatementViolations(fixture, retired + "builder.StartTable(12);"));
        Assert.Contains($"{fixture}:1: expected 1 historical layout region(s), found 2", RestatementViolations(fixture, retired + retired));
        Assert.Contains($"{fixture}:2: nested historical layout region", RestatementViolations(fixture, $"{HistoricalBegin}\n{retired}"));
        Assert.Contains($"{fixture}:1: unclosed historical layout region", RestatementViolations(fixture, HistoricalBegin));
        Assert.Contains($"{fixture}:1: historical layout end without begin", RestatementViolations(fixture, HistoricalEnd));
        Assert.Contains($"{cs}:1: historical layout marker outside a named fixture", RestatementViolations(cs, retired));
    }

    private static void AssertTypeScriptCalls()
    {
        string[] literals = ["12", "( (12) )", "0x1_2", "0B1_0", "0o1_2", "1_2", "(-0b10)", "12.0", "1.2e1"];
        var refusals = literals.SelectMany(literal => new[]
        {
            $"builder.startObject({literal})", $"builder.addFieldInt32({literal}, value, 0)", $"bb.__offset(row, {literal})"
        }).Concat([
            "builder[('startObject')](12.0)", "builder['addFieldInt32'](1e1, value, 0)", "bb['__offset'](row, (0xA))",
            "(builder.startObject)(12 as number)", "(<Start>builder.startObject)(<number>12)",
            "(builder.startObject satisfies Start)(12 satisfies number)", "builder.startObject!(12!)",
            "(builder.addFieldInt32<number>)(0b10, value, 0)", "(bb[(`__offset` as string)!])(row, (12 as number)!)",
            "bb.__offset(resolveRow(rows, index), (0xA))",
            "bb.__offset(resolveRow(rows, /* , */ index), /* slot */ ( /* inner */ 0b10 /* value */ ) /* end */)",
            "builder.startObject(// count below\n (0o12) /* end */)",
            "bb.__offset(resolveRow<Row, Key>(rows, index), (0xA))",
            "bb.__offset(resolveRow(\"(,)[;]\", index), /* slot\n follows */ (0xA))",
            "bb.__offset(row, // count below\u2028 (0xA))"
        ]).ToArray();
        string[] admitted =
        [
            "builder[('startObject')](count)", "builder['addFieldInt32'](slot, 1e1, 0)", "bb['__offset'](12.0, offset)",
            "builder.startObject(1.25); bb.__offset(row, 1e-1); builder[method](12); builder['start' + 'Object'](12)",
            "(builder.startObject)(count as number)", "(<Start>builder.startObject)(<number>count)",
            "(builder.startObject satisfies Start)(count satisfies number)", "builder.startObject!(count!)",
            "(builder.addFieldInt32<number>)(slot, value, 0)", "(bb[(`__offset` as string)!])(row, (offset as number)!)",
            "(bb[('__has_' + 'identifier')])(identifier); bb[`__has_${suffix}`](identifier)",
            "flatbuffers['SIZE_PREFIX_' + 'LENGTH']; const label = 'SIZE_PREFIX_LENGTH';",
            """const { someOtherKey = "SIZE_PREFIX_LENGTH" } = options""",
            "const { ['SIZE_PREFIX_' + 'LENGTH']: size } = flatbuffers; ({ [`__has_${suffix}`]: check } = bb)",
            "const label = { name: 'SIZE_PREFIX_LENGTH' }; import { helper } from '__has_identifier'",
            "builder.startObject(count); bb.__offset(4, offset)",
            "bb.__offset(resolveRow(rows, (2)), offset)",
            "bb.__offset(resolveRow(rows, index), /* slot */ (offset))",
            "builder.startObject((2 + 3)); builder.addFieldInt32(slot, (2), 0)",
            "bb.__offset(resolveRow<Row, Key>(rows, index), offset)",
            "bb.__offset(resolveRow(\"(,)[;]\", index), /* slot\n follows */ offset)",
            "bb.__offset(row, // count below\u2028 (offset))"
        ];
        var probes = refusals.Concat(admitted).Select((call, index) => (Path: $"probe-{index}.ts", Text: $"\n{call};")).ToArray();
        const string handwritten = "tools/e2e/handwritten-probe.ts";
        string[] frameUses =
        [
            "bb.__has_identifier(identifier)",
            "((bb[(`__has_identifier`)] as FrameCheck)!)(identifier)",
            "flatbuffers.SIZE_PREFIX_LENGTH",
            "(bb.__has_identifier).call(bb, identifier)",
            "__has_identifier.apply(bb, [identifier])",
            """bb.__has_\u0069dentifier.apply(bb, [identifier])""",
            """flatbuffers.SI\u005aE_PREFIX_LENGTH""",
            """flatbuffers[('\u0053IZE_PREFIX_LENGTH' as string)]""",
            """bb[`\u005f_has_identifier`].call(bb, identifier)""",
            "SIZE_PREFIX_LENGTH",
            """const { "\u0053IZE_PREFIX_LENGTH": boundPrefix } = flatbuffers""",
            "const { [(`__has_identifier` as string)!]: boundIdentifier } = bb",
            "({ 'SIZE_PREFIX_LENGTH': prefix } = flatbuffers)",
            """({ [('\u005f_has_identifier' as string)]: lookup } = bb)""",
            """import { '\u0053IZE_PREFIX_LENGTH' as importedPrefix } from 'flatbuffers'""",
            """export { "__has_identifier" as checkIdentifier } from 'flatbuffers'""",
            """export { prefix as "SIZE_PREFIX_LENGTH" } from 'flatbuffers'"""
        ];
        var frameAndSlot = string.Concat(frameUses.Select(use => $"\n{use};")) + "\nbuilder.startObject(12);";
        var sources = probes.Concat([
            (Path: "batch.cs", Text: "\noldBuilder.StartTable(3);"),
            (Path: handwritten, Text: frameAndSlot),
            (Path: FrameAdapter, Text: frameAndSlot),
            (Path: "tools/e2e/.wire/curious-contraptions/wire.ts", Text: frameAndSlot)
        ]).ToDictionary(source => source.Path, source => source.Text);
        var slotLine = frameUses.Length + 2;
        var expected = probes.Take(refusals.Length).Select(probe => $"{probe.Path}:2: literal FlatBuffers table slot or size")
            .Concat(Enumerable.Range(2, frameUses.Length).Select(line => $"{handwritten}:{line}: frame check outside the shared frame adapter"))
            .Concat([
                "batch.cs:2: literal FlatBuffers table slot or size",
                $"{handwritten}:{slotLine}: literal FlatBuffers table slot or size",
                $"{FrameAdapter}:{slotLine}: literal FlatBuffers table slot or size"
            ]);
        var (_, violations) = ScanSchemaSources(sources.Keys, path => sources[path]);
        Assert.Equal(expected.Order(StringComparer.Ordinal), violations.Order(StringComparer.Ordinal));
    }

    private static (string[] Paths, string[] Violations) ScanSchemaSources(IEnumerable<string> paths, Func<string, string> readSource)
    {
        var selected = paths.Where(ScansSchemaSource).ToArray();
        var sources = selected.Select(path => (Path: path, Text: readSource(path))).ToArray();
        var violations = sources.SelectMany(source => RestatementViolations(source.Path, source.Text))
            .Concat(WireConformanceTests.RunWireSourceGuard(sources.Where(source => source.Path.EndsWith(".ts", StringComparison.Ordinal)), FrameAdapter))
            .ToArray();
        return (selected, violations);
    }

    private static bool ScansSchemaSource(string path) =>
        !path.StartsWith("docs/", StringComparison.Ordinal) &&
        !path.StartsWith("_bmad-output/", StringComparison.Ordinal) &&
        !path.StartsWith("tools/e2e/.wire/", StringComparison.Ordinal) &&
        !(path.StartsWith("CuriousContraptions.FlatBuffers/", StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal)) &&
        Path.GetExtension(path) is ".cs" or ".ts" or ".csproj" or ".props" or ".targets" or ".slnx" or ".proj" or ".rsp";

    private static IEnumerable<string> RestatementViolations(string path, string text)
    {
        var source = SourceText.From(text);
        var result = new List<string>();
        void Report(int line, string reason) => result.Add($"{path}:{line + 1}: {reason}");
        var historical = HistoricalLines(path, source, Report);
        var csharp = path.EndsWith(".cs", StringComparison.Ordinal);
        var root = csharp ? CSharpSyntaxTree.ParseText(source).GetRoot() : null;
        var layout = path == SchemaLayoutFile ? root?.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .SingleOrDefault(node => node.Identifier.ValueText == nameof(WireSchemaLayout))?.Span : null;

        bool Exempt(int position) => historical[source.Lines.GetLineFromPosition(position).LineNumber] ||
            layout is { } region && region.Contains(position);
        void At(int position, string reason)
        {
            if (!Exempt(position)) Report(source.Lines.GetLineFromPosition(position).LineNumber, reason);
        }

        foreach (Match word in ForbiddenSchemaWords.Matches(text))
            At(word.Index, $"hand schema name '{word.Value}'");
        if (path == SaveRejections)
            foreach (Match field in OldInstanceFieldEnum.Matches(text)) At(field.Index, "hand instance field enum");
        if (root is not null)
        {
            ScanCSharpInvocations(root, At);
            foreach (var options in SourceVariants)
                ScanCSharpInvocations(CSharpSyntaxTree.ParseText(source, options).GetRoot(), At);
        }
        return result.Distinct(StringComparer.Ordinal);
    }

    private static bool[] HistoricalLines(string path, SourceText source, Action<int, string> report)
    {
        var exempt = new bool[source.Lines.Count];
        var expected = HistoricalCounts.GetValueOrDefault(path);
        var begin = -1;
        var count = 0;
        foreach (var line in source.Lines)
        {
            var number = line.LineNumber;
            switch (line.ToString().Trim())
            {
                case HistoricalBegin:
                    if (expected == 0) report(number, "historical layout marker outside a named fixture");
                    else if (begin >= 0) report(number, "nested historical layout region");
                    else begin = number;
                    break;
                case HistoricalEnd:
                    if (expected == 0) report(number, "historical layout marker outside a named fixture");
                    else if (begin < 0) report(number, "historical layout end without begin");
                    else
                    {
                        Array.Fill(exempt, true, begin, number - begin + 1);
                        ++count;
                        begin = -1;
                    }
                    break;
            }
        }
        if (begin >= 0) report(begin, "unclosed historical layout region");
        if (count != expected) report(0, $"expected {expected} historical layout region(s), found {count}");
        return exempt;
    }

    private static void ScanCSharpInvocations(SyntaxNode root, Action<int, string> report)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var name = invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                MemberBindingExpressionSyntax member => member.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => string.Empty
            };
            var arguments = invocation.ArgumentList.Arguments;
            if (CSharpSlotParameters.TryGetValue((name, arguments.Count), out var parameter))
            {
                var slot = arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == parameter);
                if (slot is null && arguments[0].NameColon is null) slot = arguments[0];
                if (slot is not null && IntegerLiteral(slot.Expression) is not null)
                    report(slot.Expression.SpanStart, "literal FlatBuffers table slot or size");
            }

            if (name is "__offset" or "AsSpan" or "GetUshort" or "PutUshort")
                foreach (var argument in arguments.Where(argument => HasVtableArithmetic(argument.Expression)))
                    report(argument.SpanStart, "hand FlatBuffers vtable offset");
        }
    }

    private static ExpressionSyntax Unparenthesized(ExpressionSyntax value) =>
        value is ParenthesizedExpressionSyntax parenthesized ? Unparenthesized(parenthesized.Expression) : value;

    private static decimal? IntegerLiteral(ExpressionSyntax value) => Unparenthesized(value) switch
    {
        LiteralExpressionSyntax { Token.Value: int or uint or long or ulong } literal =>
            Convert.ToDecimal(literal.Token.Value, System.Globalization.CultureInfo.InvariantCulture),
        PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryPlusExpression) => IntegerLiteral(unary.Operand),
        PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression) => -IntegerLiteral(unary.Operand),
        _ => null
    };

    private static bool IsLiteral(ExpressionSyntax value, int expected) => IntegerLiteral(value) == expected;

    private static bool Doubled(ExpressionSyntax expression) =>
        Unparenthesized(expression) is BinaryExpressionSyntax product && product.IsKind(SyntaxKind.MultiplyExpression) &&
        (IsLiteral(product.Left, 2) || IsLiteral(product.Right, 2));

    private static bool HasVtableArithmetic(ExpressionSyntax expression)
    {
        if (Unparenthesized(expression) is not BinaryExpressionSyntax sum || !sum.IsKind(SyntaxKind.AddExpression)) return false;
        if (IsLiteral(sum.Left, 4) && Doubled(sum.Right) || Doubled(sum.Left) && IsLiteral(sum.Right, 4)) return true;
        if (HasVtableArithmetic(sum.Left) || HasVtableArithmetic(sum.Right)) return true;
        // C# parses "base + 4 + 2 * slot" as "(base + 4) + 2 * slot".
        return Unparenthesized(sum.Left) is BinaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.AddExpression) &&
            (IsLiteral(prefix.Right, 4) && Doubled(sum.Right) || Doubled(prefix.Right) && IsLiteral(sum.Right, 4));
    }

}
