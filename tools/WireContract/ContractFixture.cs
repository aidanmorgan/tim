using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions.WireDesign;

// Design fixtures only. No runtime project references this executable.
public enum LeaseLeg : ushort { Outbound=0, Return=1 }
public enum RuntimeRole : byte { Browser=1, Simulation=2, Animation=3 }
public enum MessageKind : ushort
{
    Hello=1, Ready=2, Fault=3, Credit=4, Acknowledge=5, Commands=6,
    Results=8, PhysicalState=9, Occurrences=10, Topology=11, Queries=12,
    QueryResults=13, Construction=14, TransferSeal=15, ConstructionSave=16,
    AnimationDefinitions=17, AnimationCommands=18, AnimationResults=19,
    AnimationSamples=20, ClockProbe=21, ClockReply=22, Diagnostics=23
}
public enum AppliedPhase : ushort { ConstructionBoundary=1, BeforeTick=2, LifecycleBoundary=3 }
public enum ScheduleKind : ushort { NextBoundary=1, ExactTick=2 }
public enum CommandKind : ushort
{
    ReplaceConstruction=1, CreateEntity=2, ReplaceEntity=3, RemoveEntity=4,
    Connect=5, Disconnect=6, BinaryInput=7, ScalarInput=8, Pulse=9, Run=10,
    Pause=11, Resume=12, Step=13, Reset=14, Save=15, Cancel=16
}
public enum Admission
{
    Accepted=1, DuplicatePending=2, DuplicateResult=3, AlreadyAcknowledged=4, WrongGeneration=5,
    SequenceGap=6, Full=7, InvalidSchema=8, TooLate=9, TooFarAhead=10, ConflictingDuplicate=11
}
public readonly record struct BodyId(ulong Value);
public readonly record struct CapabilityId(ulong Value);
public readonly record struct StorageSlot(int Value);
public readonly record struct Generation(long Value);
public readonly record struct CommandSequence(ulong Value);
public readonly record struct Tick(ulong Value);
public readonly record struct FixtureCommand(CommandSequence Sequence, Generation Generation,
    ScheduleKind Schedule, Tick Requested, CommandKind Kind, CapabilityId Target, double Value);
public readonly record struct AppliedCommand(CommandSequence Sequence, Tick Tick, AppliedPhase Phase, uint Order);
public readonly record struct Binding(BodyId Id, StorageSlot Slot);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record HeaderFixture(
    [property:JsonRequired] ushort Version,
    [property:JsonRequired] MessageKind Kind,
    [property:JsonRequired] RuntimeRole Sender,
    [property:JsonRequired] RuntimeRole Recipient,
    [property:JsonRequired] uint TotalBytes,
    [property:JsonRequired] LeaseLeg Flags,
    [property:JsonRequired] long Generation,
    [property:JsonRequired] ulong Sequence);

public static class ContractFixture
{
    public const int HeaderBytes=80;
    public const int MaximumEnvelopeBytes=65536;
    public const int OrdinaryReservations=128;
    public const int LifecycleReservations=8;
    public const int MaximumEdgeEnvelopes=64;
    public const int ReservedControlEnvelopes=8;
    public const int MaximumEdgeBytes=512*1024;
    public const int MaximumTransferBytes=1024*1024;
    public const int AggregateBytes=8*1024*1024;
    public const ulong MaximumFutureTicks=1200;

    public static HeaderFixture ReadHeader(string json)
    {
        using var document=JsonDocument.Parse(json);
        if(document.RootElement.ValueKind!=JsonValueKind.Object) throw new JsonException("Object required.");
        var names=new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in document.RootElement.EnumerateObject())
            if(!names.Add(property.Name)) throw new JsonException("Duplicate field.");
        var value=JsonSerializer.Deserialize<HeaderFixture>(json) ?? throw new JsonException("Null header.");
        Validate(value);
        return value;
    }
    public static void Validate(HeaderFixture value)
    {
        if(value.Version!=1 || !Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.Sender) ||
           !Enum.IsDefined(value.Recipient) || value.Sender==value.Recipient ||
           value.TotalBytes<94 || value.TotalBytes>MaximumEnvelopeBytes ||
           !Enum.IsDefined(value.Flags) || (value.Kind==MessageKind.Credit)!=(value.Flags==LeaseLeg.Return) || value.Generation<0 || value.Sequence==0)
            throw new ArgumentException("Invalid design envelope.");
        var route=(value.Sender,value.Recipient);
        var browserToWorker=value.Sender==RuntimeRole.Browser;
        var workerToBrowser=value.Recipient==RuntimeRole.Browser;
        var allowed=value.Kind switch
        {
            MessageKind.Hello or MessageKind.ClockProbe=>browserToWorker,
            MessageKind.Ready or MessageKind.ClockReply or MessageKind.Diagnostics=>workerToBrowser,
            MessageKind.Commands or MessageKind.Queries or MessageKind.Construction=>
                route==(RuntimeRole.Browser,RuntimeRole.Simulation),
            MessageKind.Results or MessageKind.QueryResults or MessageKind.ConstructionSave=>
                route==(RuntimeRole.Simulation,RuntimeRole.Browser),
            MessageKind.PhysicalState or MessageKind.Occurrences or MessageKind.Topology=>
                value.Sender==RuntimeRole.Simulation,
            MessageKind.TransferSeal=>route==(RuntimeRole.Browser,RuntimeRole.Simulation) ||
                value.Sender==RuntimeRole.Simulation,
            MessageKind.AnimationDefinitions=>route==(RuntimeRole.Browser,RuntimeRole.Animation),
            MessageKind.AnimationCommands=>route==(RuntimeRole.Browser,RuntimeRole.Animation),
            MessageKind.AnimationResults=>route==(RuntimeRole.Animation,RuntimeRole.Browser),
            MessageKind.AnimationSamples=>route==(RuntimeRole.Animation,RuntimeRole.Browser),
            MessageKind.Fault or MessageKind.Credit or MessageKind.Acknowledge=>true,
            _=>false
        };
        if(!allowed) throw new ArgumentException("Invalid route.");
        if(value.Generation==0 && value.Kind is not (MessageKind.Hello or MessageKind.Ready or
            MessageKind.Fault or MessageKind.Credit or MessageKind.Acknowledge or MessageKind.ClockProbe or
            MessageKind.ClockReply or MessageKind.AnimationDefinitions or MessageKind.AnimationCommands or
            MessageKind.AnimationResults or MessageKind.AnimationSamples or MessageKind.Diagnostics))
            throw new ArgumentException("A world message requires a positive generation.");
    }
    public static long PhysicalBytes(uint poses,uint scalars,uint booleans,uint enums,uint compliant,uint patches,uint velocities=0)=>
        checked(HeaderBytes+57L+64L*poses+18L*scalars+9L*booleans+12L*enums+12L*compliant+48L*patches+80L*velocities);
    public static long AnimationBytes(uint samples)=>checked(HeaderBytes+37L+26L*samples);
    public static void RequireEnvelopeSize(long size)
    {
        if(size<HeaderBytes||size>MaximumEnvelopeBytes) throw new ArgumentOutOfRangeException(nameof(size));
    }
    public static Admission Schedule(FixtureCommand command,Generation generation,Tick committed)
    {
        if(command.Sequence.Value==0 || command.Generation.Value<=0 || !Enum.IsDefined(command.Kind) ||
            !Enum.IsDefined(command.Schedule) || !double.IsFinite(command.Value))
            return Admission.InvalidSchema;
        if(command.Generation!=generation)return Admission.WrongGeneration;
        if(command.Schedule==ScheduleKind.NextBoundary)
            return command.Requested.Value==0?Admission.Accepted:Admission.InvalidSchema;
        if(command.Kind is not (CommandKind.BinaryInput or CommandKind.ScalarInput or CommandKind.Pulse))
            return Admission.InvalidSchema;
        if(command.Requested.Value<=committed.Value)return Admission.TooLate;
        return command.Requested.Value-committed.Value>MaximumFutureTicks?Admission.TooFarAhead:Admission.Accepted;
    }
    public static AppliedCommand[] Due(IEnumerable<FixtureCommand> commands, Tick boundary)
    {
        var due=commands.Where(c=>c.Schedule==ScheduleKind.NextBoundary||c.Requested==boundary)
            .OrderBy(c=>c.Sequence.Value).ToArray();
        return due.Select((c,i)=>new AppliedCommand(c.Sequence,boundary,AppliedPhase.BeforeTick,checked((uint)i))).ToArray();
    }
    public static BodyId[] Canonical(IEnumerable<Binding> bindings)
    {
        var values=bindings.ToArray();
        if(values.Any(v=>v.Id.Value==0||v.Slot.Value<0) ||
           values.Select(v=>v.Id).Distinct().Count()!=values.Length ||
           values.Select(v=>v.Slot).Distinct().Count()!=values.Length)
            throw new ArgumentException("Invalid local identity map.");
        return values.Select(v=>v.Id).OrderBy(v=>v.Value).ToArray();
    }
    public static StorageSlot Resolve(IEnumerable<Binding> bindings,Generation installed,Generation requested,BodyId id)
    {
        if(installed.Value<=0||requested!=installed||id.Value==0)throw new ArgumentException("Stale identity.");
        _=Canonical(bindings);
        var matches=bindings.Where(v=>v.Id==id).ToArray();
        return matches.Length==1?matches[0].Slot:throw new ArgumentException("Removed identity.");
    }
    public static byte[] UnsignedBytes(ulong value)
    {
        var bytes=new byte[sizeof(ulong)];BinaryPrimitives.WriteUInt64LittleEndian(bytes,value);return bytes;
    }
    public static byte[] SignedBytes(long value)
    {
        var bytes=new byte[sizeof(long)];BinaryPrimitives.WriteInt64LittleEndian(bytes,value);return bytes;
    }
}
