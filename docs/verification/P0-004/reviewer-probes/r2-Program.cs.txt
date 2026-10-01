using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ownership;

var classify = typeof(SourceInventory).GetMethod("Classify", BindingFlags.NonPublic | BindingFlags.Static)!;
var results = new List<object>();
foreach (var probe in Enum.GetValues<Probe>())
{
    var body = probe switch
    {
        Probe.NestedCast => "((int[])(object)(_states))[0] = 1;",
        Probe.AsCast => "(_states as int[])![0] = 1;",
        Probe.Conditional => "(true ? _states : new int[1])[0] = 1;",
        Probe.Coalesce => "(_states ?? new int[1])[0] = 1;",
        Probe.LocalFunction => "void Change() { (_states)[0] = 1; } Change();",
        Probe.Deconstruct => "var (values, count) = (_states, 1); values[0] = count;",
        Probe.ReadLength => "var size = _states.Length;",
        Probe.ReturnAlias => "return (object)_states;",
        Probe.CollectionMutator => "System.Array.Clear((_states));",
        _ => throw new InvalidOperationException()
    };
    var tree = CSharpSyntaxTree.ParseText("class Fixture { readonly int[] _states = new int[1]; object Mutate() { " + body + " return null!; } }");
    var compilation = CSharpCompilation.Create("Probe", [tree],
        [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    var errors = compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).ToArray();
    if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors.Select(x => x.GetMessage())));
    var syntax = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Single(x => x.Identifier.ValueText == "_states");
    var access = (StateAccess)classify.Invoke(null, [syntax, compilation.GetSemanticModel(tree)])!;
    foreach (var authorization in Enum.GetValues<Authorization>())
    {
        var caller = authorization == Authorization.Known
            ? new CallerId("M:CuriousContraptions.SimulationTimers.CaptureCheckpoint")
            : new CallerId("M:CuriousContraptions.SimulationTimers.UnreviewedMutation");
        var member = new StateMember(new("F:CuriousContraptions.SimulationTimers._states"),
            new("engine/SimulationTimers.cs"), 1, new("System.Int32[]"), StorageForm.Field,
            StorageMutability.ReferencedStorage, false, [new(new("fixture/Flow.cs"), 1, caller, access)]);
        var assignment = new OwnershipAssignment(member.Id, AssemblyOwner.SimulationCore, new("P0-019"),
            OwnershipRule.SimulationAuthority, OwnershipAudit.Fingerprint(member));
        var accepted = false;
        try { OwnershipAudit.Validate(new([], [member]), new([], [assignment]), new HashSet<WorkId> { assignment.Migration }); accepted = true; }
        catch (InvalidDataException) { }
        if (accepted != (authorization == Authorization.Known)) throw new InvalidOperationException("Incorrect reference authorization.");
        results.Add(new { Probe = probe, Authorization = authorization, Access = access, Accepted = accepted });
    }
}
Console.WriteLine(JsonSerializer.Serialize(results));
enum Probe { NestedCast, AsCast, Conditional, Coalesce, LocalFunction, Deconstruct, ReadLength, ReturnAlias, CollectionMutator }
enum Authorization { Known, Unknown }
