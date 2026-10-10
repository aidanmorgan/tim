using System.Text.Json;
using Ownership;

try
{
// CLI and JSON are external boundaries; selector strings never enter domain logic.
var operation = args.Length == 0 ? throw new ArgumentException("Supply a supported operation.") : args[0] switch
{
    "--capture" => Operation.Capture,
    "--members" => Operation.Members,
    "--audit" => Operation.Audit,
    "--oracles" => Operation.Oracles,
    _ => throw new ArgumentException("Unsupported operation.")
};
if (operation == Operation.Oracles)
{
    Console.WriteLine(JsonSerializer.Serialize(OwnershipOracles.Run(args.Length > 1 ? Path.GetFullPath(args[1]) : null), OwnershipAudit.Json));
    return 0;
}
if (args.Length < 2) throw new ArgumentException("Supply the workspace root.");
var root = Path.GetFullPath(args[1]);
if (operation == Operation.Audit && args.Length != 3) throw new ArgumentException("Supply the ownership contract path.");
var tasks = operation == Operation.Audit ? CurrentOwnerMembership.Read(root) : null;
var current = SourceInventory.Capture(root);
switch (operation)
{
    case Operation.Capture:
        Console.WriteLine(JsonSerializer.Serialize(current, OwnershipAudit.Json)); break;
    case Operation.Members:
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            current.Sources, current.Diagnostics, current.References, current.Symbols,
            CallCount = current.Calls.Length, ContextCallerCount = current.ConservativeContextCallers.Length,
            Members = current.Members.Select(member => new
            {
                member.Id, member.Path, member.Line, member.Type, member.Form, member.Mutability,
                member.Static, MemberSha256 = OwnershipAudit.Fingerprint(member),
                UseCount = member.Uses.Length
            })
        }, OwnershipAudit.Json)); break;
    case Operation.Audit:
        var contract = OwnershipAudit.Read(root, args[2]);
        OwnershipAudit.Validate(current, contract, tasks!);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            SourceCount = current.Sources.Length, MemberCount = current.Members.Length,
            UseCount = current.Members.Sum(member => member.Uses.Length), ContractCurrent = true,
            RuntimeQualified = false, BindingErrors = current.Diagnostics.Length,
            CallCount = current.Calls.Length, ContextCallerCount = current.ConservativeContextCallers.Length
        }, OwnershipAudit.Json)); break;
    default: throw new InvalidOperationException("Unsupported operation.");
}
return 0;
}
catch (Exception failure) when (failure is ArgumentException or InvalidDataException or JsonException or IOException)
{
    Console.Error.WriteLine(failure.Message);
    return 1;
}
internal enum Operation { Capture, Members, Audit, Oracles }
