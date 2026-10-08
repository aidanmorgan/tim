using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ownership;

var root=Path.GetFullPath(args[0]);
// Reuse the current ownership resolver's actual compiler options and generated-source closure.
var resolve=typeof(SourceInventory).GetMethod("Resolve",BindingFlags.Static|BindingFlags.NonPublic)!;
if (args.Length != 2 || !Enum.TryParse<InspectionContext>(args[1], out var context) ||
    context is not (InspectionContext.ProductionRelease or InspectionContext.ProductionDiagnostic))
    throw new ArgumentException("Specify a supported production compiler context.");
var input=resolve.Invoke(null,[root,"CuriousContraptions.csproj",context])!;
T Read<T>(CompilerInput property)
{
    // Explicit reflection boundary into the existing resolver; selectors remain enum-typed.
    var member=property switch
    {
        CompilerInput.Language=>"Language",
        CompilerInput.Symbols=>"Symbols",
        CompilerInput.Paths=>"Paths",
        CompilerInput.BindingPaths=>"BindingPaths",
        CompilerInput.Usings=>"Usings",
        CompilerInput.AssemblyName=>"AssemblyName",
        CompilerInput.References=>"References",
        CompilerInput.Output=>"Output",
        CompilerInput.Unsafe=>"Unsafe",
        CompilerInput.Nullable=>"Nullable",
        CompilerInput.Checked=>"Checked",
        _=>throw new ArgumentOutOfRangeException(nameof(property))
    };
    return (T)input.GetType().GetProperty(member)!.GetValue(input)!;
}
var options=new CSharpParseOptions(Read<LanguageVersion>(CompilerInput.Language),preprocessorSymbols:Read<string[]>(CompilerInput.Symbols));
var paths=Read<string[]>(CompilerInput.Paths);
var trees=paths.Concat(Read<string[]>(CompilerInput.BindingPaths)).Select(path=>CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root,path)),options,path)).ToArray();
var imports=CSharpSyntaxTree.ParseText(string.Join("\n",Read<string[]>(CompilerInput.Usings).Select(value=>"global using "+value+";")),options);
var compilation=CSharpCompilation.Create(Read<string>(CompilerInput.AssemblyName),trees.Append(imports),
    Read<string[]>(CompilerInput.References).Select(path=>MetadataReference.CreateFromFile(Path.Combine(root,path))),
    new CSharpCompilationOptions(Read<OutputKind>(CompilerInput.Output),allowUnsafe:Read<bool>(CompilerInput.Unsafe),
        nullableContextOptions:Read<NullableContextOptions>(CompilerInput.Nullable),checkOverflow:Read<bool>(CompilerInput.Checked)));
var errors=compilation.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToArray();
if(errors.Length!=0)throw new InvalidDataException(string.Join("\n",errors.Select(d=>d.ToString())));
var authored=paths.Select(path=>new SourcePath(path)).ToHashSet();
var edges=new HashSet<Edge>();
foreach(var tree in trees)
{
    var source=new SourcePath(tree.FilePath);
    if(!authored.Contains(source))continue;
    var model=compilation.GetSemanticModel(tree);
    foreach(var node in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
    {
        var symbol=model.GetSymbolInfo(node).Symbol;
        if(symbol is IAliasSymbol alias)symbol=alias.Target;
        var type=symbol as INamedTypeSymbol??symbol?.ContainingType;
        if(type is null)continue;
        foreach(var target in type.DeclaringSyntaxReferences)
        {
            var destination=new SourcePath(target.SyntaxTree.FilePath);
            if(destination!=source&&authored.Contains(destination))
                edges.Add(new(source,destination,new(type.ToDisplayString()),node.GetLocation().GetLineSpan().StartLinePosition.Line+1));
        }
    }
}
// Actual serialized roots plus explicit host/diagnostic entry points. Resource paths
// are external identities; preserve them as SourcePath after this file-format boundary.
var resources = new HashSet<SourcePath>();
var resourceQueue = new Queue<SourcePath>(Directory.EnumerateFiles(Path.Combine(root, "parts/catalog"), "*.tres")
    .Select(path => new SourcePath(Path.GetRelativePath(root, path))));
resourceQueue.Enqueue(new("project.godot"));
while (resourceQueue.TryDequeue(out var resource))
{
    if (!resources.Add(resource)) continue;
    var path = Path.Combine(root, resource.Value);
    if (!File.Exists(path)) throw new FileNotFoundException("Linked resource is absent.", path);
    if (resource.Value.EndsWith(".cs", StringComparison.Ordinal)) continue;
    foreach (Match match in Regex.Matches(File.ReadAllText(path), "res://([^\\\"]+)"))
        resourceQueue.Enqueue(new(match.Groups[1].Value));
}
var seeds = resources.Where(authored.Contains).ToHashSet();
foreach (var path in new[] { "CuriousContraptions.web/TwoDogWebBoot.cs", "diagnostics/BackendQualificationDiagnostics.cs" })
    if (authored.Contains(new(path))) seeds.Add(new(path));
var reached=new HashSet<SourcePath>(seeds);var witnesses=new List<Edge>();var queue=new Queue<SourcePath>(seeds.OrderBy(path=>path.Value,StringComparer.Ordinal));
while(queue.TryDequeue(out var source))
    foreach(var edge in edges.Where(edge=>edge.Source==source).OrderBy(edge=>edge.Destination.Value,StringComparer.Ordinal).ThenBy(edge=>edge.Line))
        if(reached.Add(edge.Destination)){witnesses.Add(edge);queue.Enqueue(edge.Destination);}
Console.WriteLine(JsonSerializer.Serialize(new{Context=context,BindingErrors=errors.Length,Resources=resources.OrderBy(path=>path.Value).ToArray(),Seeds=seeds.OrderBy(path=>path.Value).ToArray(),
    Required=reached.OrderBy(path=>path.Value).ToArray(),Witnesses=witnesses,
    NotReachedByStaticReferences=authored.Except(reached).OrderBy(path=>path.Value).ToArray()},new JsonSerializerOptions{WriteIndented=true}));
internal sealed record Edge(SourcePath Source,SourcePath Destination,TypeId Type,int Line);

internal enum CompilerInput { Language, Symbols, Paths, BindingPaths, Usings, AssemblyName, References, Output, Unsafe, Nullable, Checked }
