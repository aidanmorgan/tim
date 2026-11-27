using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using CuriousContraptions.Gpu;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
}
