using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum WorkshopCommandKind : uint { Initialize, Construct, Run, Reset, Dispose, Cancel, ConfigureCadence, Pause, Resume, Step, Save }
public enum WorkshopResponseKind : uint { Acknowledgement, Read }
public enum WorkshopWireVersion : uint { GenericMechanical = 8 }
public enum ExpectedRevisionKind : uint { Any = 1, Exact = 2 }
public readonly record struct WorkshopCommand(CommandSequence Sequence, WorkshopCommandKind Kind, SimulationEpoch Epoch, AuthorityRevision Revision, WorkshopConstruction? Construction, WorkshopCommandIdentity? Target = null, ExpectedRevisionKind RevisionKind = ExpectedRevisionKind.Exact, RuntimeSessionId Session = default,
    CadenceRevision Cadence = default, ProjectionEpoch Projection = default, WorkshopCadenceSettings? Settings = null);
public readonly record struct WorkshopCommandIdentity(CommandSequence Sequence, SimulationEpoch Epoch, AuthorityRevision Revision, CadenceRevision Cadence, ProjectionEpoch Projection);
public readonly record struct WorkshopResponse(CommandSequence Sequence, WorkshopResponseKind Kind,
    WorkshopCommandResult Result, WorkshopSimulationPhase Phase, WorkshopRead Read, RuntimeSessionId Session = default, PublicationSequence Publication = default,
    CadenceRevision Cadence = default, ClockGeneration MasterGeneration = default,
    ProjectionEpoch Projection = default, PulseOrdinal SourcePulse = default);

/// <summary>Named, bounded external boundary; unknown schemas, enums, lengths and padding reject.</summary>
public static class WorkshopWire
{
    public const int CommandHeaderBytes = 72;
    public const int ConstructionBytes = 288;
    public const int ResponseBytes = 512 + PhysicsMotionRead.ByteLength;
    public static bool Matches(WorkshopCommand command, SimulationEpoch epoch, AuthorityRevision revision,
        RuntimeSessionId session, CadenceRevision cadence, ProjectionEpoch projection) =>
        command.Session == session && command.Epoch == epoch && command.Cadence == cadence && command.Projection == projection &&
        (command.RevisionKind == ExpectedRevisionKind.Any || command.Revision == revision);

    public static byte[] Encode(WorkshopCommand command)
    {
        command.Session.Validate();
        var hasSettings = command.Kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.ConfigureCadence;
        command.Settings?.Validate();
        if (!Enum.IsDefined(command.Kind) || !Enum.IsDefined(command.RevisionKind) ||
            (command.RevisionKind == ExpectedRevisionKind.Any && command.Revision.Value != 0) ||
            (command.Kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Save && command.RevisionKind != ExpectedRevisionKind.Exact) ||
            command.Sequence.Value == 0 || hasSettings != command.Settings.HasValue ||
            (command.Kind == WorkshopCommandKind.Initialize
                ? command.Cadence.Value != 0 || command.Projection.Value != 0
                : command.Cadence.Value == 0 || command.Projection.Value == 0) ||
            (command.Kind == WorkshopCommandKind.Construct) != command.Construction.HasValue ||
            (command.Kind == WorkshopCommandKind.Cancel) != command.Target.HasValue ||
            (command.Target is { } target && (target.Sequence.Value == 0 || target.Epoch.Value == 0 || target.Cadence.Value == 0 || target.Projection.Value == 0)))
            throw new ArgumentException("Unsupported Workshop command.");
        var bytes = new byte[CommandHeaderBytes + (command.Construction.HasValue ? ConstructionBytes : command.Target.HasValue ? 40 : hasSettings ? WorkshopCadenceWire.ByteLength : 0)];
        var data = bytes.AsSpan();
        BinaryPrimitives.WriteUInt64LittleEndian(data, command.Sequence.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[8..], (uint)command.Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(data[12..], (uint)WorkshopWireVersion.GenericMechanical);
        BinaryPrimitives.WriteUInt64LittleEndian(data[16..], command.Epoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..], command.Revision.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[32..], (uint)command.RevisionKind);
        WriteSession(data[40..56], command.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(data[56..], command.Cadence.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[64..], command.Projection.Value);
        if (command.Settings is { } settings) WorkshopCadenceWire.Write(settings, data[CommandHeaderBytes..]);
        if (command.Construction is { } construction) WriteConstruction(data[CommandHeaderBytes..], construction);
        if (command.Target is { } identity)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(data[72..], identity.Sequence.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[80..], identity.Epoch.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[88..], identity.Revision.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[96..], identity.Cadence.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[104..], identity.Projection.Value);
        }
        return bytes;
    }

    public static WorkshopCommand DecodeCommand(ReadOnlySpan<byte> data)
    {
        if (data.Length < CommandHeaderBytes ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..]) != (uint)WorkshopWireVersion.GenericMechanical)
            throw new ArgumentException("Unsupported Workshop command schema.");
        var revisionKind = (ExpectedRevisionKind)BinaryPrimitives.ReadUInt32LittleEndian(data[32..]);
        if (!Enum.IsDefined(revisionKind) || BinaryPrimitives.ReadUInt32LittleEndian(data[36..]) != 0 ||
            (revisionKind == ExpectedRevisionKind.Any && BinaryPrimitives.ReadUInt64LittleEndian(data[24..]) != 0))
            throw new ArgumentException("Unsupported expected revision.");
        var kind = (WorkshopCommandKind)BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        var sequence = new CommandSequence(BinaryPrimitives.ReadUInt64LittleEndian(data));
        var cadence = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(data[56..]));
        var projection = new ProjectionEpoch(BinaryPrimitives.ReadUInt64LittleEndian(data[64..]));
        var hasSettings = kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.ConfigureCadence;
        if ((kind == WorkshopCommandKind.Initialize ? cadence.Value != 0 || projection.Value != 0 : cadence.Value == 0 || projection.Value == 0) ||
            !Enum.IsDefined(kind) || sequence.Value == 0 ||
            (kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Save && revisionKind != ExpectedRevisionKind.Exact) ||
            data.Length != CommandHeaderBytes + (kind == WorkshopCommandKind.Construct ? ConstructionBytes : kind == WorkshopCommandKind.Cancel ? 40 : hasSettings ? WorkshopCadenceWire.ByteLength : 0))
            throw new ArgumentException("Unsupported Workshop command shape.");
        return new(sequence, kind, new(BinaryPrimitives.ReadUInt64LittleEndian(data[16..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])), kind == WorkshopCommandKind.Construct ? ReadConstruction(data[CommandHeaderBytes..]) : null,
            kind == WorkshopCommandKind.Cancel ? ReadTarget(data[CommandHeaderBytes..]) : null, revisionKind, ReadSession(data[40..56]), cadence, projection,
            hasSettings ? WorkshopCadenceWire.Read(data[CommandHeaderBytes..]) : null);
    }

    private static WorkshopCommandIdentity ReadTarget(ReadOnlySpan<byte> data)
    {
        var identity = new WorkshopCommandIdentity(new(BinaryPrimitives.ReadUInt64LittleEndian(data)),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[8..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[16..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[32..])));
        if (identity.Sequence.Value == 0 || identity.Epoch.Value == 0 || identity.Cadence.Value == 0 || identity.Projection.Value == 0)
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
        response.Session.Validate();
        if ((response.Kind == WorkshopResponseKind.Read) != (response.Publication.Value != 0))
            throw new ArgumentException("Invalid physical publication identity.");
        ValidateCapture(response.Read);
        ValidateScheduleIdentity(response);
        if (response.Read.Ball is { } validatedBody)
        {
            validatedBody.Validate();
            if (validatedBody.Epoch != response.Read.Epoch.Value || validatedBody.Tick != response.Read.Tick.Value)
                throw new ArgumentException("Read body does not match its envelope.");
        }
        ValidatePhysicalRead(response.Read);
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
        BinaryPrimitives.WriteUInt32LittleEndian(data[52..], (uint)WorkshopWireVersion.GenericMechanical);
        WriteSession(data[64..80], response.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(data[104..], response.Publication.Value);
        if (response.Read.Capture is { } capture)
        {
            data[56] = (byte)capture.Domain; data[57] = 1;
            BinaryPrimitives.WriteUInt64LittleEndian(data[80..], capture.Generation.Value);
            BinaryPrimitives.WriteInt64LittleEndian(data[88..], capture.Time.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[96..], capture.Uncertainty.Value);
        }
        if (response.Read.Ball is { } body) body.Write(data[112..192]);
        BinaryPrimitives.WriteUInt64LittleEndian(data[192..], response.Cadence.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[200..], response.MasterGeneration.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[208..], response.Projection.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[216..], response.SourcePulse.Value);
        if (response.Read.Rotation is { } rotation)
        {
            Write(data[224..], rotation.X); Write(data[226..], rotation.Y);
            Write(data[228..], rotation.Z); Write(data[230..], rotation.W);
            Write(data[232..], response.Read.Angular.X); Write(data[234..], response.Read.Angular.Y); Write(data[236..], response.Read.Angular.Z);
        }
        if (response.Read.Motion is { } motion) motion.Bytes.CopyTo(data[512..]);
        BinaryPrimitives.WriteUInt32LittleEndian(data[240..], response.Read.Captures.Count);
        for (var i = 0; i < response.Read.Captures.Count; i++)
        {
            var latch = response.Read.Captures[i]; var output = data.Slice(256 + i * 32, 32);
            BinaryPrimitives.WriteUInt64LittleEndian(output, latch.Sensor.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(output[8..], (uint)latch.Phase);
            BinaryPrimitives.WriteUInt32LittleEndian(output[12..], latch.EventOrdinal);
            Write(output[16..], latch.EventPhase);
        }
    }

    public static WorkshopResponse DecodeResponse(ReadOnlySpan<byte> data)
    {
        if (data.Length != ResponseBytes || !Zero(data.Slice(58, 6)) || BinaryPrimitives.ReadUInt32LittleEndian(data[52..]) != (uint)WorkshopWireVersion.GenericMechanical)
            throw new ArgumentException("Unsupported Workshop response shape.");
        var sequence = new CommandSequence(BinaryPrimitives.ReadUInt64LittleEndian(data));
        var kind = (WorkshopResponseKind)BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        var outcome = (WorkshopCommandOutcome)BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        var reason = (WorkshopRejection)BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
        var phase = (WorkshopSimulationPhase)BinaryPrimitives.ReadUInt32LittleEndian(data[20..]);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[48..]);
        var session = ReadSession(data[64..80]);
        var publication = new PublicationSequence(BinaryPrimitives.ReadUInt64LittleEndian(data[104..]));
        if ((kind == WorkshopResponseKind.Read) != (publication.Value != 0) || data[57] > 1)
            throw new ArgumentException("Invalid capture/publication tag.");
        WorkshopClockStamp? capture = data[57] == 0 ? null :
            new((WorkshopClockDomain)data[56], new(BinaryPrimitives.ReadUInt64LittleEndian(data[80..])),
                new(BinaryPrimitives.ReadInt64LittleEndian(data[88..])), new(BinaryPrimitives.ReadUInt64LittleEndian(data[96..])));
        if (capture is null && (data[56] != 0 || !Zero(data.Slice(80, 24))))
            throw new ArgumentException("Absent capture has a nonzero payload.");
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(outcome) || !Enum.IsDefined(reason) ||
            !Enum.IsDefined(phase) || count > 1 ||
            (kind == WorkshopResponseKind.Acknowledgement) != (sequence.Value != 0))
            throw new ArgumentException("Unsupported Workshop response discriminant.");
        var read = new WorkshopRead(new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[32..])),
            count == 0 ? null : CanonicalBody.Decode(data[112..192]), new(BinaryPrimitives.ReadUInt64LittleEndian(data[40..])), capture);
        if (count == 0 && !Zero(data[112..192])) throw new ArgumentException("Empty read has nonzero payload.");
        if (read.Ball is { } body && (body.Epoch != read.Epoch.Value || body.Tick != read.Tick.Value))
            throw new ArgumentException("Read body does not match its envelope.");
        if (!Zero(data[238..240]) || !Zero(data[244..256])) throw new ArgumentException("Nonzero physical read padding.");
        var captureCount = BinaryPrimitives.ReadUInt32LittleEndian(data[240..]);
        if (captureCount > PhysicsSceneDeclaration.SensorCapacity ||
            !Zero(data[(256 + checked((int)captureCount) * 32)..512])) throw new ArgumentException("Invalid capture read capacity.");
        Span<CaptureLatch> latches = stackalloc CaptureLatch[PhysicsSceneDeclaration.SensorCapacity];
        for (var i = 0; i < captureCount; i++)
        {
            var input = data.Slice(256 + i * 32, 32);
            var latch = new CaptureLatch(new(BinaryPrimitives.ReadUInt64LittleEndian(input)),
                (CaptureLatchPhase)BinaryPrimitives.ReadUInt32LittleEndian(input[8..]),
                BinaryPrimitives.ReadUInt32LittleEndian(input[12..]), Read(input[16..]));
            latch.Validate();
            if (!Zero(input[18..])) throw new ArgumentException("Nonzero capture padding.");
            latches[i] = latch;
        }
        read = read with
        {
            Rotation = count == 0 ? null : new CanonicalRotation(Read(data[224..]), Read(data[226..]), Read(data[228..]), Read(data[230..])),
            Angular = new(Read(data[232..]), Read(data[234..]), Read(data[236..])),
            Captures = new(latches[..checked((int)captureCount)]),
            Motion = PhysicsMotionRead.Decode(data[512..], read.Ball, read.Tick)
        };
        if (count == 0 && !Zero(data[224..240])) throw new ArgumentException("Absent body has orientation data.");
        ValidatePhysicalRead(read);
        ValidateCapture(read);
        var response = new WorkshopResponse(sequence, kind, new(outcome, reason), phase, read, session, publication,
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[192..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[200..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[208..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[216..])));
        ValidateScheduleIdentity(response);
        return response;
    }

    private static void ValidatePhysicalRead(WorkshopRead read)
    {
        if (read.Motion is { } motion) motion.ValidateBinding(read.Ball, read.Tick);
        else if (read.Tick.Value != 0) throw new ArgumentException("Committed motion description is required.");
        if (read.Ball.HasValue != read.Rotation.HasValue) throw new ArgumentException("Body orientation is required.");
        read.Rotation?.Validate();
        PhysicsDeclarationBounds.Vector(read.Angular.X, read.Angular.Y, read.Angular.Z, (Half)64);
        if (!read.Ball.HasValue && !PhysicsDeclarationBounds.Zero(read.Angular.X, read.Angular.Y, read.Angular.Z))
            throw new ArgumentException("Absent body has angular motion.");
        for (var i = 0; i < read.Captures.Count; i++)
        {
            var latch = read.Captures[i]; latch.Validate();
            if (latch.EventOrdinal > WorkshopCadenceSettings.PhysicalOrdinalLimit)
                throw new ArgumentException("Capture time exceeds the world duration.");
        }
    }

    private static void ValidateScheduleIdentity(WorkshopResponse response)
    {
        if (response.Read.Epoch.Value == 0)
        {
            if (response.Cadence.Value != 0 || response.Projection.Value != 0 || response.MasterGeneration.Value != 0 || response.SourcePulse.Value != 0)
                throw new ArgumentException("Uninstalled response contains a schedule identity.");
        }
        else if (response.Cadence.Value == 0 || response.Projection.Value == 0 || response.MasterGeneration.Value == 0 ||
            response.Read.Capture?.Generation != response.MasterGeneration || response.SourcePulse.Value != response.Read.Tick.Value)
            throw new ArgumentException("Physical response does not bind its installed master schedule.");
    }

    private static void ValidateCapture(WorkshopRead read)
    {
        if (read.Capture is { } capture)
        {
            capture.Validate();
            if (capture.Domain != WorkshopClockDomain.SimulationMonotonic || read.Epoch.Value == 0)
                throw new ArgumentException("Physical read requires its simulation native clock.");
        }
        else if (read.Epoch.Value != 0 || read.Tick.Value != 0 || read.Revision.Value != 0 || read.Ball is not null)
            throw new ArgumentException("An installed physical read requires its original capture.");
    }

    internal static void WriteSession(Span<byte> data, RuntimeSessionId session)
    {
        session.Validate();
        BinaryPrimitives.WriteUInt64LittleEndian(data, session.Low);
        BinaryPrimitives.WriteUInt64LittleEndian(data[8..], session.High);
    }
    internal static RuntimeSessionId ReadSession(ReadOnlySpan<byte> data)
    {
        var session = new RuntimeSessionId(BinaryPrimitives.ReadUInt64LittleEndian(data),
            BinaryPrimitives.ReadUInt64LittleEndian(data[8..]));
        session.Validate(); return session;
    }

    internal static void WriteConstruction(Span<byte> data, WorkshopConstruction construction)
    {
        if (data.Length != ConstructionBytes) throw new ArgumentException("Invalid construction width.");
        construction.Validate();
        data.Clear();
        WorkshopCadenceWire.Write(construction.Settings, data[128..160]);
        BinaryPrimitives.WriteUInt64LittleEndian(data, construction.Revision.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[8..], construction.Ball is null ? 0u : 1u);
        if (construction.Receiver is { } receiver)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(data[160..], 1);
            new CanonicalBody(receiver.Id, 0, 0, receiver.Cell, receiver.Local, default).Write(data[176..256]);
            Write(data[256..], receiver.Rotation.X); Write(data[258..], receiver.Rotation.Y);
            Write(data[260..], receiver.Rotation.Z); Write(data[262..], receiver.Rotation.W);
            Write(data[264..], receiver.Capture.Margin.Value); Write(data[266..], receiver.Capture.SpeedLimit.Value);
            Write(data[268..], receiver.Capture.Dwell.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(data[272..], (uint)receiver.Capture.Participation);
        }
        if (construction.Ball is not { } ball) return;
        new CanonicalBody(ball.Id, 0, 0, ball.Cell, ball.Local, default).Write(data.Slice(16, CanonicalBody.ByteLength));
        Write(data[96..], ball.Rotation.X); Write(data[98..], ball.Rotation.Y);
        Write(data[100..], ball.Rotation.Z); Write(data[102..], ball.Rotation.W);
        Write(data[104..], ball.Material.Radius.Value); Write(data[106..], ball.Material.Mass.Value);
        Write(data[108..], ball.Material.Bounce.Value); Write(data[110..], ball.Material.Drag.Value);
        Write(data[112..], ball.Material.Buoyancy.Value);
    }

    internal static WorkshopConstruction ReadConstruction(ReadOnlySpan<byte> data)
    {
        if (data.Length != ConstructionBytes) throw new ArgumentException("Invalid construction width.");
        var revision = new ConstructionRevision(BinaryPrimitives.ReadUInt64LittleEndian(data));
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        if (count > 1 || !Zero(data[12..16]) || !Zero(data[114..128]))
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
        else if (!Zero(data[16..128])) throw new ArgumentException("Empty construction has nonzero payload.");
        var receiverCount = BinaryPrimitives.ReadUInt32LittleEndian(data[160..]);
        if (receiverCount > 1 || !Zero(data[164..176]) || !Zero(data[270..272]) || !Zero(data[276..288]))
            throw new ArgumentException("Invalid Receiver declaration shape.");
        WorkshopReceiver? receiver = null;
        if (receiverCount != 0)
        {
            var body = CanonicalBody.Decode(data[176..256]);
            if (body.Epoch != 0 || body.Tick != 0 || !HalfBits.IsPositiveZero(body.Velocity))
                throw new ArgumentException("Authored Receiver contains live state.");
            receiver = new(body.Id, body.Cell, body.Local,
                new(Read(data[256..]), Read(data[258..]), Read(data[260..]), Read(data[262..])),
                new(new(Read(data[264..])), new(Read(data[266..])), new(Read(data[268..])),
                    (SensorParticipation)BinaryPrimitives.ReadUInt32LittleEndian(data[272..])));
        }
        else if (!Zero(data[176..288])) throw new ArgumentException("Absent Receiver contains payload.");
        var construction = new WorkshopConstruction(revision, ball, WorkshopCadenceWire.Read(data[128..160]), receiver);
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
