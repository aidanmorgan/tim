using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ownership;

var classified = typeof(SourceInventory).GetMethod("Classify", BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new InvalidOperationException("Missing classifier.");
var results = new List<object>();
foreach (var variant in Enum.GetValues<Probe>())
{
    var syntax = CSharpSyntaxTree.ParseText(variant switch
    {
        Probe.Direct => "class Other { void Mutate() { _states[0] = default; } }",
        Probe.Cast => "class Other { void Mutate() { ((SimulationTimerState[])_states)[0] = default; } }",
        Probe.Parenthesized => "class Other { void Mutate() { (_states)[0] = default; } }",
        _ => throw new InvalidOperationException()
    });
    var name = syntax.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
        .Single(value => value.Identifier.ValueText == "_states");
    var access = (StateAccess)classified.Invoke(null, [name])!;
    var member = new StateMember(new("F:CuriousContraptions.SimulationTimers._states"),
        new("engine/SimulationTimers.cs"), 1, new("CuriousContraptions.SimulationTimerState[]"),
        StorageForm.Field, StorageMutability.ReferencedStorage, false,
        [new(new("fixture/Other.cs"), 1, new("M:Fixture.Other.Mutate"), access)]);
    var assignment = new OwnershipAssignment(member.Id, AssemblyOwner.SimulationCore, new("P0-019"),
        OwnershipRule.SimulationAuthority, OwnershipAudit.Fingerprint(member));
    var snapshot = new OwnershipSnapshot([], [member]);
    var contract = new OwnershipContract([], [assignment]);
    var accepted = false;
    string? error = null;
    try { OwnershipAudit.Validate(snapshot, contract, new HashSet<WorkId> { assignment.Migration }); accepted = true; }
    catch (InvalidDataException failure) { error = failure.Message; }
    results.Add(new { Probe = variant, Access = access, Accepted = accepted, Error = error });
}
Console.WriteLine(JsonSerializer.Serialize(results));
enum Probe { Direct, Cast, Parenthesized }
