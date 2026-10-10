using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Ownership;

public static class SourceInventory
{
    private const string AnimationProject = "CuriousContraptions.Animation/CuriousContraptions.Animation.csproj";
    private const string MainProject = "CuriousContraptions.csproj";
    private const string WebProject = "CuriousContraptions.web/CuriousContraptions.web.csproj";
    private const string TestProject = "CuriousContraptions.tests/CuriousContraptions.tests.csproj";
    private const string DiagnosticSymbol = "PLAYTEST";
    private const string WebSymbol = "TWODOG_WEB_BOOT";

    internal sealed record ProjectInputs(string[] Paths, string[] BindingPaths, string[] References, string[] Symbols,
        string[] Usings, string AssemblyName, LanguageVersion Language, NullableContextOptions Nullable,
        bool Unsafe, OutputKind Output, bool Checked, string[] ConfigPaths);

    private static ProjectInputs Resolve(string root, string project, InspectionContext context)
    {
        var diagnostics = context is InspectionContext.ProductionDiagnostic or InspectionContext.TestDiagnostic;
        var generatedDirectory = Path.Combine(root, "tools/Ownership/obj/binding", context switch
        {
            InspectionContext.AnimationRelease => "AnimationRelease",
            InspectionContext.ProductionDiagnostic => "ProductionDiagnostic",
            InspectionContext.ProductionRelease => "ProductionRelease",
            InspectionContext.TestDiagnostic => "TestDiagnostic",
            InspectionContext.TestRelease => "TestRelease",
            _ => throw new InvalidDataException("Unknown compilation context.")
        });
        if (Directory.Exists(generatedDirectory)) Directory.Delete(generatedDirectory, recursive: true);
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in new[] { "msbuild", project, "-p:Configuration=Release",
            "-target:Rebuild", "-p:BuildProjectReferences=false", "-p:EmitCompilerGeneratedFiles=true",
            "-p:CompilerGeneratedFilesOutputPath=" + generatedDirectory, "-getItem:Compile,ReferencePath,Using",
            "-getProperty:DefineConstants,AssemblyName,LangVersion,Nullable,AllowUnsafeBlocks,OutputType,CheckForOverflowUnderflow,MSBuildAllProjects,MSBuildProjectFullPath,ProjectAssetsFile,MSBuildProjectExtensionsPath", "-p:PlaytestDiagnostics=" + (diagnostics ? "true" : "false"), "-nologo" })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot resolve compilation inputs.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var stdout = output.GetAwaiter().GetResult();
        var stderr = errors.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new InvalidDataException(stdout + stderr);
        using var document = JsonDocument.Parse(stdout);
        var items = document.RootElement.GetProperty("Items");
        string[] ItemPaths(string kind) => items.GetProperty(kind).EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString() ?? throw new InvalidDataException("Missing input path."))
            .Select(path => Path.GetRelativePath(root, path)).Distinct().Order(StringComparer.Ordinal).ToArray();
        var properties = document.RootElement.GetProperty("Properties");
        string Property(string name) => properties.GetProperty(name).GetString() ?? throw new InvalidDataException("Missing compiler option.");
        if (!LanguageVersionFacts.TryParse(Property("LangVersion"), out var language))
            throw new InvalidDataException("Unsupported language version.");
        var nullable = Property("Nullable") switch
        {
            "enable" => NullableContextOptions.Enable, "disable" or "" => NullableContextOptions.Disable,
            "warnings" => NullableContextOptions.Warnings, "annotations" => NullableContextOptions.Annotations,
            _ => throw new InvalidDataException("Unsupported nullable context.")
        };
        var outputKind = Property("OutputType") switch
        {
            "Library" => OutputKind.DynamicallyLinkedLibrary, "Exe" => OutputKind.ConsoleApplication,
            "WinExe" => OutputKind.WindowsApplication,
            _ => throw new InvalidDataException("Unsupported output kind.")
        };
        var config = Property("MSBuildAllProjects").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Concat([Property("MSBuildProjectFullPath"), Property("ProjectAssetsFile"),
                Path.Combine(Property("MSBuildProjectExtensionsPath"), Path.GetFileName(project) + ".nuget.g.props"),
                Path.Combine(Property("MSBuildProjectExtensionsPath"), Path.GetFileName(project) + ".nuget.g.targets"),
                Path.Combine(root, "Directory.Build.props"), Path.Combine(root, "Directory.Build.targets"),
                Path.Combine(root, "global.json")])
            .Select(path => Path.GetRelativePath(root, Path.GetFullPath(path, root)))
            .Distinct().Order(StringComparer.Ordinal).ToArray();
        return new(ItemPaths("Compile"), (Directory.Exists(generatedDirectory)
                ? Directory.EnumerateFiles(generatedDirectory, "*.cs", SearchOption.AllDirectories) : [])
                .Select(path => Path.GetRelativePath(root, path)).Order(StringComparer.Ordinal).ToArray(), ItemPaths("ReferencePath"),
            properties.GetProperty("DefineConstants").GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries),
            items.GetProperty("Using").EnumerateArray().Select(item => item.GetProperty("Identity").GetString()!).ToArray(),
            properties.GetProperty("AssemblyName").GetString()!, language, nullable,
            bool.Parse(Property("AllowUnsafeBlocks")), outputKind, bool.Parse(Property("CheckForOverflowUnderflow")), config);
    }

    private static SourcePath[] WebLifecycleInputs(string root)
    {
        // Source/configuration reconciliation only: no web build, Clean, Publish or AppBundle write.
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in new[] { "msbuild", WebProject, "-p:Configuration=Release",
            "-p:PlaytestDiagnostics=false", "-getItem:Compile", "-getProperty:MSBuildAllProjects", "-nologo" })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot evaluate web inputs.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var text = output.GetAwaiter().GetResult();
        var error = errors.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new InvalidDataException(text + error);
        using var document = JsonDocument.Parse(text);
        var compile = document.RootElement.GetProperty("Items").GetProperty("Compile").EnumerateArray()
            .Select(item => Path.GetRelativePath(root, item.GetProperty("FullPath").GetString()!));
        var imports = document.RootElement.GetProperty("Properties").GetProperty("MSBuildAllProjects")
            .GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(path => Path.GetRelativePath(root, path));
        SourcePath[] explicitInputs =
        [
            new(WebProject), new("CuriousContraptions.web/Directory.Build.props"), new("CuriousContraptions.web/global.json"),
            new("CuriousContraptions.web/obj/project.assets.json"),
            new("CuriousContraptions.web/obj/CuriousContraptions.web.csproj.nuget.g.props"),
            new("CuriousContraptions.web/obj/CuriousContraptions.web.csproj.nuget.g.targets"),
            new(MainProject), new("Directory.Build.props"), new("Directory.Build.targets"), new("global.json")
        ];
        return explicitInputs.Concat(compile.Concat(imports).Select(path => new SourcePath(path)))
            .Distinct().OrderBy(path => path.Value, StringComparer.Ordinal).ToArray();
    }

    public static OwnershipSnapshot Capture(string root)
    {
        var animation = Resolve(root, AnimationProject, InspectionContext.AnimationRelease);
        var main = Resolve(root, MainProject, InspectionContext.ProductionDiagnostic);
        var tests = Resolve(root, TestProject, InspectionContext.TestDiagnostic);
        var releaseMain = Resolve(root, MainProject, InspectionContext.ProductionRelease);
        var releaseTests = Resolve(root, TestProject, InspectionContext.TestRelease);
        var animationCompilation = Compile(root, animation);
        if (animation.AssemblyName != "CuriousContraptions.Animation")
            throw new InvalidDataException("Animation project assembly identity differs.");
        foreach (var consumer in new[] { main, tests, releaseMain, releaseTests })
            RequireReference(consumer, animationCompilation);
        var compilation = Compile(root, main, animationCompilation);
        var testCompilation = Compile(root, tests, animationCompilation, compilation);
        var releaseCompilation = Compile(root, releaseMain, animationCompilation);
        var releaseTestCompilation = Compile(root, releaseTests, animationCompilation, releaseCompilation);
        var contexts = new[] {
            (Kind: InspectionContext.AnimationRelease, Compilation: animationCompilation, Inputs: animation),
            (Kind: InspectionContext.ProductionDiagnostic, Compilation: compilation, Inputs: main),
            (Kind: InspectionContext.TestDiagnostic, Compilation: testCompilation, Inputs: tests),
            (Kind: InspectionContext.ProductionRelease, Compilation: releaseCompilation, Inputs: releaseMain),
            (Kind: InspectionContext.TestRelease, Compilation: releaseTestCompilation, Inputs: releaseTests)
        };
        return Inspect(root, contexts, WebLifecycleInputs(root));
    }

    internal static void RequireReference(ProjectInputs inputs, CSharpCompilation dependency)
    {
        if (inputs.References.Count(path => Path.GetFileNameWithoutExtension(path) == dependency.AssemblyName) != 1)
            throw new InvalidDataException("Required source-backed assembly reference is missing or duplicated.");
    }

    internal static CSharpCompilation Compile(string root, ProjectInputs inputs, params CSharpCompilation[] dependencies)
    {
        if (inputs.Paths.Length == 0 || inputs.Paths.Distinct(StringComparer.Ordinal).Count() != inputs.Paths.Length)
            throw new InvalidDataException("Compile inputs must be present and unique.");
        foreach (var dependency in dependencies) RequireReference(inputs, dependency);
        if (dependencies.Select(item => item.AssemblyName).Distinct(StringComparer.Ordinal).Count() != dependencies.Length)
            throw new InvalidDataException("Duplicate source-backed dependency.");
        var options = new CSharpParseOptions(inputs.Language, preprocessorSymbols: inputs.Symbols);
        var source = inputs.Paths.Concat(inputs.BindingPaths).Distinct(StringComparer.Ordinal)
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, path)), options, path));
        var imports = CSharpSyntaxTree.ParseText(string.Join("\n", inputs.Usings.Select(value => "global using " + value + ";")), options);
        IEnumerable<MetadataReference> references = inputs.References
            .Where(path => !dependencies.Any(dependency => Path.GetFileNameWithoutExtension(path) == dependency.AssemblyName))
            .Select(path => MetadataReference.CreateFromFile(Path.Combine(root, path)));
        references = references.Concat(dependencies.Select(dependency => dependency.ToMetadataReference()));
        return CSharpCompilation.Create(inputs.AssemblyName, source.Append(imports), references,
            new CSharpCompilationOptions(inputs.Output, allowUnsafe: inputs.Unsafe,
                nullableContextOptions: inputs.Nullable, checkOverflow: inputs.Checked));
    }

    internal static OwnershipSnapshot Inspect(string root,
        (InspectionContext Kind, CSharpCompilation Compilation, ProjectInputs Inputs)[] contexts,
        SourcePath[] supplementalInputs)
    {
        if (contexts.Any(context => !Enum.IsDefined(context.Kind)) ||
            contexts.Select(context => context.Kind).Distinct().Count() != contexts.Length ||
            !Enum.GetValues<InspectionContext>().Order().SequenceEqual(contexts.Select(context => context.Kind).Order()))
            throw new InvalidDataException("Missing, duplicate or unknown compilation context.");
        var animation = contexts.Single(context => context.Kind == InspectionContext.AnimationRelease);
        if (animation.Compilation.AssemblyName != "CuriousContraptions.Animation")
            throw new InvalidDataException("Animation source context has the wrong assembly identity.");
        foreach (var context in contexts.Where(context => context.Kind is
            InspectionContext.ProductionRelease or InspectionContext.ProductionDiagnostic or
            InspectionContext.TestRelease or InspectionContext.TestDiagnostic))
        {
            RequireReference(context.Inputs, animation.Compilation);
            if (!context.Compilation.References.OfType<CompilationReference>()
                .Any(reference => ReferenceEquals(reference.Compilation, animation.Compilation)))
                throw new InvalidDataException("Animation dependency is not source-backed.");
        }
        var diagnostics = contexts.SelectMany(context => context.Compilation.GetDiagnostics()
            .Where(item => item.Severity == DiagnosticSeverity.Error)
            .Select(item => new BindingDiagnostic(context.Kind, new(item.Id),
                new(item.Location.SourceTree?.FilePath ?? string.Empty),
                item.Location.IsInSource ? item.Location.GetLineSpan().StartLinePosition.Line + 1 : 0,
                item.GetMessage()))).ToArray();
        // Animation currently has no conditional source branches. Revisit this Release-only union
        // when its authored conditionals/configurations change; do not infer future coverage.
        var declarations = contexts.Where(context => context.Kind is InspectionContext.ProductionDiagnostic
            or InspectionContext.AnimationRelease);
        var trees = declarations.SelectMany(context => context.Compilation.SyntaxTrees
            .Where(tree => context.Inputs.Paths.Contains(tree.FilePath, StringComparer.Ordinal))
            .Select(tree => (Tree: tree, Compilation: context.Compilation))).ToArray();
        var allTrees = contexts.SelectMany(context => context.Compilation.SyntaxTrees
            .Where(tree => tree.FilePath.Length != 0).Select(tree => (tree, context.Compilation, context.Kind))).ToArray();
        var paths = contexts.SelectMany(context => context.Inputs.Paths.Concat(context.Inputs.BindingPaths)
            .Concat(context.Inputs.ConfigPaths)).Distinct().ToArray();
        var referencePaths = contexts.SelectMany(context => context.Inputs.References)
            .Distinct().Order(StringComparer.Ordinal).ToArray();
        var symbols = contexts.Select(context => new CompilationContext(context.Kind,
            context.Compilation.SyntaxTrees.First().Options is CSharpParseOptions parse ? parse.PreprocessorSymbolNames.ToArray() : [],
            new(context.Compilation.AssemblyName!), ((CSharpParseOptions)context.Compilation.SyntaxTrees.First().Options).LanguageVersion,
            context.Compilation.Options.OutputKind, context.Compilation.Options.NullableContextOptions,
            context.Compilation.Options.AllowUnsafe, context.Compilation.Options.CheckOverflow,
            context.Inputs.Paths.Select(path => new SourcePath(path)).ToArray(),
            context.Inputs.BindingPaths.Select(path => new SourcePath(path)).ToArray(),
            context.Inputs.References.Select(path => new SourcePath(path)).ToArray()))
            .ToArray();
        var declared = new Dictionary<ISymbol, (SyntaxNode Node, StorageForm Form)>(SymbolEqualityComparer.Default);
        foreach (var (tree, ownerCompilation) in trees)
        {
            var model = ownerCompilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                ISymbol? symbol = null;
                StorageForm form = default;
                switch (node)
                {
                    case VariableDeclaratorSyntax variable when variable.Parent?.Parent is FieldDeclarationSyntax:
                        symbol = model.GetDeclaredSymbol(variable); form = StorageForm.Field; break;
                    case VariableDeclaratorSyntax variable when variable.Parent?.Parent is EventFieldDeclarationSyntax:
                        symbol = model.GetDeclaredSymbol(variable); form = StorageForm.Event; break;
                    case PropertyDeclarationSyntax property:
                        symbol = model.GetDeclaredSymbol(property); form = StorageForm.Property; break;
                    case IndexerDeclarationSyntax indexer:
                        symbol = model.GetDeclaredSymbol(indexer); form = StorageForm.Property; break;
                    case EventDeclarationSyntax eventDeclaration:
                        symbol = model.GetDeclaredSymbol(eventDeclaration); form = StorageForm.Event; break;
                    case ParameterSyntax parameter when parameter.Parent?.Parent is TypeDeclarationSyntax and not RecordDeclarationSyntax:
                        symbol = model.GetDeclaredSymbol(parameter); form = StorageForm.PrimaryCapture; break;
                }
                if (symbol is IFieldSymbol { IsConst: true }) continue;
                if (symbol is not null) declared.Add(symbol, (node, form));
            }
            foreach (var record in tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(record) is not INamedTypeSymbol type || record.ParameterList is null) continue;
                foreach (var parameter in record.ParameterList.Parameters)
                {
                    var property = type.GetMembers(parameter.Identifier.ValueText).OfType<IPropertySymbol>().SingleOrDefault();
                    if (property is not null && !declared.ContainsKey(property)) declared.Add(property, (parameter, StorageForm.RecordProperty));
                }
            }
        }
        var uses = declared.Keys.ToDictionary(symbol => symbol, _ => new List<UseSite>(), SymbolEqualityComparer.Default);
        var names = declared.Keys.GroupBy(symbol => symbol.Name).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var identities = declared.Keys.ToDictionary(Identity, symbol => symbol, StringComparer.Ordinal);
        var calls = new List<CallerEdge>();
        var contextCallers = new HashSet<CallerId>();
        foreach (var (tree, contextCompilation, contextKind) in allTrees)
        {
            var model = contextCompilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax)
                {
                    if (model.GetDeclaredSymbol(node) is { } declaredMethod) contextCallers.Add(new(Identity(declaredMethod)));
                }
                // Include method groups as conservative edges: callbacks can execute after registration.
                if (node is not (InvocationExpressionSyntax or ObjectCreationExpressionSyntax
                    or ImplicitObjectCreationExpressionSyntax or SimpleNameSyntax)) continue;
                var bound = model.GetSymbolInfo(node).Symbol;
                if (bound is IPropertySymbol property)
                {
                    foreach (var accessor in new[] { property.GetMethod, property.SetMethod }.OfType<IMethodSymbol>())
                    {
                        var propertyCaller = Site(node, model, StateAccess.Call).Caller;
                        contextCallers.Add(propertyCaller);
                        contextCallers.Add(new(Identity(accessor)));
                        calls.Add(new(contextKind, new(tree.FilePath), node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            propertyCaller, new(Identity(accessor)), accessor.IsVirtual || accessor.IsOverride
                                ? DispatchScope.VirtualFamily : DispatchScope.NamedTarget));
                    }
                }
                var called = bound as IMethodSymbol;
                if (called is null) continue;
                var caller = Site(node, model, StateAccess.Call).Caller;
                contextCallers.Add(caller);
                var scope = called.MethodKind == MethodKind.DelegateInvoke ? DispatchScope.ContextDelegates
                    : called.ContainingType.TypeKind == TypeKind.Interface ? DispatchScope.InterfaceFamily
                    : called.IsVirtual || called.IsOverride || called.IsAbstract ? DispatchScope.VirtualFamily
                    : called.Locations.Any(location => location.IsInSource) ? DispatchScope.NamedTarget
                    : DispatchScope.External;
                calls.Add(new(contextKind, new(tree.FilePath), node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    caller, new(Identity(called.OriginalDefinition)), scope));
            }

            foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (!names.TryGetValue(name.Identifier.ValueText, out var possible)) continue;
                var info = model.GetSymbolInfo(name);
                var resolved = info.Symbol?.OriginalDefinition;
                var symbol = resolved is null ? null : identities.GetValueOrDefault(Identity(resolved));
                if (symbol is not null && uses.TryGetValue(symbol, out var sites))
                    sites.Add(Site(name, model, Classify(name, model)));
                else if (symbol is null)
                    foreach (var candidate in possible) uses[candidate].Add(Site(name, model, StateAccess.Potential));
            }
            foreach (var element in tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>())
            {
                var info = model.GetSymbolInfo(element);
                if (info.Symbol?.OriginalDefinition is { } resolved && identities.TryGetValue(Identity(resolved), out var symbol)
                    && uses.TryGetValue(symbol, out var sites))
                    sites.Add(Site(element, model, Classify(element, model)));
            }
        }
        var members = declared.Select(pair =>
        {
            var symbol = pair.Key;
            var (node, form) = pair.Value;
            var type = symbol switch { IFieldSymbol field => field.Type, IPropertySymbol property => property.Type,
                IEventSymbol signal => signal.Type, IParameterSymbol captured => captured.Type,
                _ => throw new InvalidDataException("Unsupported declaration.") };
            var mutable = symbol switch
            {
                IFieldSymbol field => !field.IsReadOnly,
                IPropertySymbol property => property.SetMethod is { IsInitOnly: false },
                IEventSymbol => true,
                IParameterSymbol => true,
                _ => false
            };
            var storage = HasReferences(type, new(SymbolEqualityComparer.Default));
            var identity = symbol is IParameterSymbol parameter
                ? DocumentationCommentId.CreateDeclarationId(parameter.ContainingType) + "::" + parameter.Name
                : DocumentationCommentId.CreateDeclarationId(symbol) ?? throw new InvalidDataException("State member needs a stable declaration identity.");
            return new StateMember(new(identity), new(node.SyntaxTree.FilePath),
                node.GetLocation().GetLineSpan().StartLinePosition.Line + 1, new(type.ToDisplayString()),
                form, storage ? StorageMutability.ReferencedStorage : mutable ? StorageMutability.MutableValue : StorageMutability.ConstructionValue,
                symbol.IsStatic, uses[symbol].Distinct().OrderBy(site => site.Path.Value, StringComparer.Ordinal).ThenBy(site => site.Line).ToArray());
        }).OrderBy(member => member.Id.Value, StringComparer.Ordinal).ToArray();
        return new(paths.Concat(supplementalInputs.Select(path => path.Value))
            .Distinct().Order(StringComparer.Ordinal).Select(path => new SourceInput(new(path),
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, path)))))).ToArray(), members)
        {
            Diagnostics = diagnostics,
            References = referencePaths.Select(path => new SourceInput(new(path),
                Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, path)))))).ToArray(),
            Symbols = symbols,
            Calls = calls.Distinct().OrderBy(edge => edge.Path.Value, StringComparer.Ordinal).ThenBy(edge => edge.Line).ToArray(),
            ConservativeContextCallers = contextCallers.Concat(members.SelectMany(member => member.Uses).Select(site => site.Caller))
                .Distinct().OrderBy(caller => caller.Value, StringComparer.Ordinal).ToArray()
        };
    }

    private static string Identity(ISymbol symbol) => symbol is IParameterSymbol parameter
        ? DocumentationCommentId.CreateDeclarationId(parameter.ContainingType) + "::" + parameter.Name
        : DocumentationCommentId.CreateDeclarationId(symbol) ?? symbol.ToDisplayString();

    private static bool HasReferences(ITypeSymbol type, HashSet<ITypeSymbol> visited)
    {
        if (type.SpecialType == SpecialType.System_String || type.TypeKind == TypeKind.Enum) return false;
        if (type.IsReferenceType || type is IArrayTypeSymbol || type.IsRefLikeType || type.TypeKind == TypeKind.TypeParameter) return true;
        if (!visited.Add(type)) return false;
        return type.GetMembers().OfType<IFieldSymbol>().Any(field => !field.IsStatic && HasReferences(field.Type, visited));
    }

    private static UseSite Site(SyntaxNode node, SemanticModel model, StateAccess access)
    {
        var enclosing = model.GetEnclosingSymbol(node.SpanStart);
        return new(new(node.SyntaxTree.FilePath), node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            new(enclosing is null ? node.SyntaxTree.FilePath : DocumentationCommentId.CreateDeclarationId(enclosing) ?? enclosing.ToDisplayString()), access);
    }

    internal static StateAccess Classify(ExpressionSyntax name, SemanticModel model)
    {
        ExpressionSyntax expression = name.Parent is MemberAccessExpressionSyntax member && member.Name == name ? member : name;
        var operation = model.GetOperation(expression);
        if (operation is null) return StateAccess.Potential;
        // Roslyn removes most syntactic parentheses. Conversions and explicit parenthesized
        // operations preserve the reference/lvalue relationship, including user-defined casts.
        while (operation.Parent is IConversionOperation or IParenthesizedOperation)
            operation = operation.Parent;
        return operation.Parent switch
        {
            ICompoundAssignmentOperation assignment when assignment.Target == operation => StateAccess.ReadWrite,
            IAssignmentOperation assignment when assignment.Target == operation => StateAccess.Write,
            IIncrementOrDecrementOperation => StateAccess.ReadWrite,
            IArgumentOperation or IReturnOperation or IVariableInitializerOperation
                or IFieldInitializerOperation or IPropertyInitializerOperation => StateAccess.ReferenceEscape,
            IAssignmentOperation => StateAccess.ReferenceEscape,
            IArrayElementReferenceOperation or IConditionalOperation or ICoalesceOperation
                or IConditionalAccessOperation or IAddressOfOperation => StateAccess.ReferenceEscape,
            IInvocationOperation => StateAccess.Call,
            IPropertyReferenceOperation or IFieldReferenceOperation when operation.Type?.IsReferenceType == true
                => StateAccess.ReferenceEscape,
            _ => StateAccess.Read
        };
    }
}
