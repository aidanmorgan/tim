using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum WorkshopCommandKind : uint { Initialize, Construct, Run, Reset, Dispose, Cancel }
public enum WorkshopResponseKind : uint { Acknowledgement, Read }
public enum WorkshopWireVersion : uint { CanonicalHalf = 3 }
public enum ExpectedRevisionKind : uint { Any = 1, Exact = 2 }
public readonly record struct WorkshopCommand(CommandSequence Sequence, WorkshopCommandKind Kind, SimulationEpoch Epoch, AuthorityRevision Revision, WorkshopConstruction? Construction, WorkshopCommandIdentity? Target = null, ExpectedRevisionKind RevisionKind = ExpectedRevisionKind.Exact);
public readonly record struct WorkshopCommandIdentity(CommandSequence Sequence, SimulationEpoch Epoch, AuthorityRevision Revision);
public readonly record struct WorkshopResponse(CommandSequence Sequence, WorkshopResponseKind Kind,
    WorkshopCommandResult Result, WorkshopSimulationPhase Phase, WorkshopRead Read);

/// <summary>Named, bounded external boundary; unknown schemas, enums, lengths and padding reject.</summary>
public static class WorkshopWire
{
    public const int CommandHeaderBytes = 40;
    public const int ConstructionBytes = 128;
    public const int ResponseBytes = 144;
    public static bool Matches(WorkshopCommand command, SimulationEpoch epoch, AuthorityRevision revision) =>
        command.Epoch == epoch && (command.RevisionKind == ExpectedRevisionKind.Any || command.Revision == revision);

    public static byte[] Encode(WorkshopCommand command)
    {
        if (!Enum.IsDefined(command.Kind) || !Enum.IsDefined(command.RevisionKind) ||
            (command.RevisionKind == ExpectedRevisionKind.Any && command.Revision.Value != 0) ||
            (command.Kind == WorkshopCommandKind.Construct && command.RevisionKind != ExpectedRevisionKind.Exact) ||
            command.Sequence.Value == 0 ||
            (command.Kind == WorkshopCommandKind.Construct) != command.Construction.HasValue ||
            (command.Kind == WorkshopCommandKind.Cancel) != command.Target.HasValue ||
            (command.Target is { } target && (target.Sequence.Value == 0 || target.Epoch.Value == 0)))
            throw new ArgumentException("Unsupported Workshop command.");
        var bytes = new byte[CommandHeaderBytes + (command.Construction.HasValue ? ConstructionBytes : command.Target.HasValue ? 24 : 0)];
        var data = bytes.AsSpan();
        BinaryPrimitives.WriteUInt64LittleEndian(data, command.Sequence.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[8..], (uint)command.Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(data[12..], (uint)WorkshopWireVersion.CanonicalHalf);
        BinaryPrimitives.WriteUInt64LittleEndian(data[16..], command.Epoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..], command.Revision.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[32..], (uint)command.RevisionKind);
        if (command.Construction is { } construction) WriteConstruction(data[CommandHeaderBytes..], construction);
        if (command.Target is { } identity)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(data[40..], identity.Sequence.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[48..], identity.Epoch.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[56..], identity.Revision.Value);
        }
        return bytes;
    }

    public static WorkshopCommand DecodeCommand(ReadOnlySpan<byte> data)
    {
        if (data.Length < CommandHeaderBytes ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..]) != (uint)WorkshopWireVersion.CanonicalHalf)
            throw new ArgumentException("Unsupported Workshop command schema.");
        var revisionKind = (ExpectedRevisionKind)BinaryPrimitives.ReadUInt32LittleEndian(data[32..]);
        if (!Enum.IsDefined(revisionKind) || BinaryPrimitives.ReadUInt32LittleEndian(data[36..]) != 0 ||
            (revisionKind == ExpectedRevisionKind.Any && BinaryPrimitives.ReadUInt64LittleEndian(data[24..]) != 0))
            throw new ArgumentException("Unsupported expected revision.");
        var kind = (WorkshopCommandKind)BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        var sequence = new CommandSequence(BinaryPrimitives.ReadUInt64LittleEndian(data));
        if (!Enum.IsDefined(kind) || sequence.Value == 0 ||
            (kind == WorkshopCommandKind.Construct && revisionKind != ExpectedRevisionKind.Exact) ||
            data.Length != CommandHeaderBytes + (kind == WorkshopCommandKind.Construct ? ConstructionBytes : kind == WorkshopCommandKind.Cancel ? 24 : 0))
            throw new ArgumentException("Unsupported Workshop command shape.");
        return new(sequence, kind, new(BinaryPrimitives.ReadUInt64LittleEndian(data[16..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])), kind == WorkshopCommandKind.Construct ? ReadConstruction(data[CommandHeaderBytes..]) : null,
            kind == WorkshopCommandKind.Cancel ? ReadTarget(data[CommandHeaderBytes..]) : null, revisionKind);
    }

    private static WorkshopCommandIdentity ReadTarget(ReadOnlySpan<byte> data)
    {
        var identity = new WorkshopCommandIdentity(new(BinaryPrimitives.ReadUInt64LittleEndian(data)),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[8..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[16..])));
        if (identity.Sequence.Value == 0 || identity.Epoch.Value == 0)
            throw new ArgumentException("Cancel target must have a complete original identity.");
        return identity;
    }

    public static byte[] Encode(WorkshopResponse response)
    {
        var bytes = new byte[ResponseBytes];
        Write(response, bytes);
        return bytes;
    }

    public static void Write(WorkshopResponse response, Span<byte> data)
    {
        if (data.Length != ResponseBytes) throw new ArgumentException("Invalid reserved response size.");
        if (!Enum.IsDefined(response.Kind) || !Enum.IsDefined(response.Phase) ||
            !Enum.IsDefined(response.Result.Outcome) || !Enum.IsDefined(response.Result.Reason) ||
            (response.Kind == WorkshopResponseKind.Acknowledgement) != (response.Sequence.Value != 0))
            throw new ArgumentException("Unsupported Workshop response.");
        data.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(data, response.Sequence.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[8..], (uint)response.Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(data[12..], (uint)response.Result.Outcome);
        BinaryPrimitives.WriteUInt32LittleEndian(data[16..], (uint)response.Result.Reason);
        BinaryPrimitives.WriteUInt32LittleEndian(data[20..], (uint)response.Phase);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..], response.Read.Epoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[32..], response.Read.Tick.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[40..], response.Read.Revision.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[48..], response.Read.Ball is null ? 0u : 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(data[52..], (uint)WorkshopWireVersion.CanonicalHalf);
        if (response.Read.Ball is { } body) body.Write(data[64..]);
    }

    public static WorkshopResponse DecodeResponse(ReadOnlySpan<byte> data)
    {
        if (data.Length != ResponseBytes || !Zero(data.Slice(56, 8)) || BinaryPrimitives.ReadUInt32LittleEndian(data[52..]) != (uint)WorkshopWireVersion.CanonicalHalf)
            throw new ArgumentException("Unsupported Workshop response shape.");
        var sequence = new CommandSequence(BinaryPrimitives.ReadUInt64LittleEndian(data));
        var kind = (WorkshopResponseKind)BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        var outcome = (WorkshopCommandOutcome)BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        var reason = (WorkshopRejection)BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
        var phase = (WorkshopSimulationPhase)BinaryPrimitives.ReadUInt32LittleEndian(data[20..]);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[48..]);
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(outcome) || !Enum.IsDefined(reason) ||
            !Enum.IsDefined(phase) || count > 1 ||
            (kind == WorkshopResponseKind.Acknowledgement) != (sequence.Value != 0))
            throw new ArgumentException("Unsupported Workshop response discriminant.");
        var read = new WorkshopRead(new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[32..])),
            count == 0 ? null : CanonicalBody.Decode(data[64..]), new(BinaryPrimitives.ReadUInt64LittleEndian(data[40..])));
        if (count == 0 && !Zero(data[64..])) throw new ArgumentException("Empty read has nonzero payload.");
        if (read.Ball is { } body && (body.Epoch != read.Epoch.Value || body.Tick != read.Tick.Value))
            throw new ArgumentException("Read body does not match its envelope.");
        return new(sequence, kind, new(outcome, reason), phase, read);
    }

    private static void WriteConstruction(Span<byte> data, WorkshopConstruction construction)
    {
        construction.Validate();
        BinaryPrimitives.WriteUInt64LittleEndian(data, construction.Revision.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[8..], construction.Ball is null ? 0u : 1u);
        if (construction.Ball is not { } ball) return;
        new CanonicalBody(ball.Id, 0, 0, ball.Cell, ball.Local, default).Write(data.Slice(16, CanonicalBody.ByteLength));
        Write(data[96..], ball.Rotation.X); Write(data[98..], ball.Rotation.Y);
        Write(data[100..], ball.Rotation.Z); Write(data[102..], ball.Rotation.W);
        Write(data[104..], ball.Material.Radius.Value); Write(data[106..], ball.Material.Mass.Value);
        Write(data[108..], ball.Material.Bounce.Value); Write(data[110..], ball.Material.Drag.Value);
        Write(data[112..], ball.Material.Buoyancy.Value);
    }

    private static WorkshopConstruction ReadConstruction(ReadOnlySpan<byte> data)
    {
        var revision = new ConstructionRevision(BinaryPrimitives.ReadUInt64LittleEndian(data));
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        if (count > 1 || !Zero(data[12..16]) || !Zero(data[114..]))
            throw new ArgumentException("Unsupported construction shape.");
        WorkshopBall? ball = null;
        if (count == 1)
        {
            var body = CanonicalBody.Decode(data[16..96]);
            if (body.Epoch != 0 || body.Tick != 0 || !HalfBits.IsPositiveZero(body.Velocity))
                throw new ArgumentException("Construction contains live simulation state.");
            ball = new(body.Id, body.Cell, body.Local,
                new(Read(data[96..]), Read(data[98..]), Read(data[100..]), Read(data[102..])),
                new(new(Read(data[104..])), new(Read(data[106..])), new(Read(data[108..])),
                    new(Read(data[110..])), new(Read(data[112..]))));
        }
        else if (!Zero(data[16..])) throw new ArgumentException("Empty construction has nonzero payload.");
        var construction = new WorkshopConstruction(revision, ball);
        construction.Validate();
        return construction;
    }
    private static bool Zero(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes) if (value != 0) return false;
        return true;
    }
    private static void Write(Span<byte> data, Half value) => BinaryPrimitives.WriteUInt16LittleEndian(data, BitConverter.HalfToUInt16Bits(value));
    private static Half Read(ReadOnlySpan<byte> data) => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(data));
}
