using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum WorkshopCommandKind : uint { Initialize, Construct, Run, Reset, Dispose, Cancel, ConfigureCadence, Pause, Resume, Step, Save, ConfigureElectrical }
public enum WorkshopResponseKind : uint { Acknowledgement, Read }
public enum WorkshopWireVersion : uint { GenericMechanical = 19 }
public enum ExpectedRevisionKind : uint { Any = 1, Exact = 2 }
public readonly record struct WorkshopCommand(CommandSequence Sequence, WorkshopCommandKind Kind, SimulationEpoch Epoch, AuthorityRevision Revision, WorkshopConstruction? Construction, WorkshopCommandIdentity? Target = null, ExpectedRevisionKind RevisionKind = ExpectedRevisionKind.Exact, RuntimeSessionId Session = default,
    CadenceRevision Cadence = default, ProjectionEpoch Projection = default, WorkshopCadenceSettings? Settings = null, WorkshopElectricalControl? Electrical = null);
public readonly record struct WorkshopCommandIdentity(CommandSequence Sequence, SimulationEpoch Epoch, AuthorityRevision Revision, CadenceRevision Cadence, ProjectionEpoch Projection);
public readonly record struct WorkshopResponse(CommandSequence Sequence, WorkshopResponseKind Kind,
    WorkshopCommandResult Result, WorkshopSimulationPhase Phase, WorkshopRead Read, RuntimeSessionId Session = default, PublicationSequence Publication = default,
    CadenceRevision Cadence = default, ClockGeneration MasterGeneration = default,
    ProjectionEpoch Projection = default, PulseOrdinal SourcePulse = default);

/// <summary>Named, bounded external boundary; unknown schemas, enums, lengths and padding reject.</summary>
public static class WorkshopWire
{
    public const int CommandHeaderBytes = 72;
    public const int ConstructionHeaderBytes = 320;
    public const int InstanceBytes = 160;
    public const int ConnectionsOffset = ConstructionHeaderBytes + WorkshopInstances.Capacity * InstanceBytes;
    public const int ConnectionBytes = 32;
    public const int ConstructionBytes = ConnectionsOffset + WorkshopConnections.Capacity * ConnectionBytes;
    public const int ResponseHeaderBytes = 128;
    public const int ResponseSessionOffset = 40;
    public const int ResponsePublicationOffset = 56;
    public static int[] ResponseAbi() => [ResponseBytes, ResponseSessionOffset, ResponsePublicationOffset];
    public const int ReadBodiesOffset = ResponseHeaderBytes;
    public const int ReadCapturesOffset = ReadBodiesOffset + PhysicsBodyReadSet.Capacity * PhysicsBodyWire.ByteLength;
    public const int CaptureBytes = 24;
    public const int ReadActivationsOffset = ReadCapturesOffset + PhysicsSceneDeclaration.SensorCapacity * CaptureBytes;
    public const int ReadTimersOffset = ReadActivationsOffset + WorkshopActivationWire.ByteLength;
    public const int ReadContactWorkOffset = ReadTimersOffset + WorkshopTimerWire.ByteLength;
    public const int ReadMotionOffset = ReadContactWorkOffset + WorkshopContactWorkWire.ByteLength;
    public const int ReadElectricalOffset = ReadMotionOffset + PhysicsMotionRead.ByteLength;
    public const int ResponseBytes = ReadElectricalOffset + WorkshopElectricalWire.ByteLength;
    public static bool Matches(WorkshopCommand command, SimulationEpoch epoch, AuthorityRevision revision,
        RuntimeSessionId session, CadenceRevision cadence, ProjectionEpoch projection) =>
        command.Session == session && command.Epoch == epoch && command.Cadence == cadence && command.Projection == projection &&
        (command.RevisionKind == ExpectedRevisionKind.Any || command.Revision == revision);

    public static byte[] Encode(WorkshopCommand command)
    {
        command.Session.Validate();
        var hasSettings = command.Kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.ConfigureCadence;
        command.Settings?.Validate();
        command.Electrical?.Validate();
        if (!Enum.IsDefined(command.Kind) || !Enum.IsDefined(command.RevisionKind) ||
            (command.RevisionKind == ExpectedRevisionKind.Any && command.Revision.Value != 0) ||
            (command.Kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Save && command.RevisionKind != ExpectedRevisionKind.Exact) ||
            command.Sequence.Value == 0 || hasSettings != command.Settings.HasValue ||
            (command.Kind == WorkshopCommandKind.ConfigureElectrical) != command.Electrical.HasValue ||
            (command.Kind == WorkshopCommandKind.Initialize
                ? command.Cadence.Value != 0 || command.Projection.Value != 0
                : command.Cadence.Value == 0 || command.Projection.Value == 0) ||
            (command.Kind == WorkshopCommandKind.Construct) != command.Construction.HasValue ||
            (command.Kind == WorkshopCommandKind.Cancel) != command.Target.HasValue ||
            (command.Target is { } target && (target.Sequence.Value == 0 || target.Epoch.Value == 0 || target.Cadence.Value == 0 || target.Projection.Value == 0)))
            throw new ArgumentException("Unsupported Workshop command.");
        var bytes = new byte[CommandHeaderBytes + (command.Construction.HasValue ? ConstructionBytes : command.Target.HasValue ? 40 : command.Electrical.HasValue ? 16 : hasSettings ? WorkshopCadenceWire.ByteLength : 0)];
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
        if (command.Electrical is { } electrical) electrical.Write(data[CommandHeaderBytes..]);
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
            data.Length != CommandHeaderBytes + (kind == WorkshopCommandKind.Construct ? ConstructionBytes : kind == WorkshopCommandKind.Cancel ? 40 : kind == WorkshopCommandKind.ConfigureElectrical ? 16 : hasSettings ? WorkshopCadenceWire.ByteLength : 0))
            throw new ArgumentException("Unsupported Workshop command shape.");
        return new(sequence, kind, new(BinaryPrimitives.ReadUInt64LittleEndian(data[16..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])), kind == WorkshopCommandKind.Construct ? ReadConstruction(data[CommandHeaderBytes..]) : null,
            kind == WorkshopCommandKind.Cancel ? ReadTarget(data[CommandHeaderBytes..]) : null, revisionKind, ReadSession(data[40..56]), cadence, projection,
            hasSettings ? WorkshopCadenceWire.Read(data[CommandHeaderBytes..]) : null,
            kind == WorkshopCommandKind.ConfigureElectrical ? WorkshopElectricalControl.Read(data[CommandHeaderBytes..]) : null);
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
        ValidateCapture(response.Read); ValidateScheduleIdentity(response); ValidatePhysicalRead(response.Read);
        data.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(data,response.Sequence.Value);
        data[8]=checked((byte)response.Kind); data[9]=checked((byte)response.Result.Outcome);
        data[10]=checked((byte)response.Result.Reason); data[11]=checked((byte)response.Phase);
        BinaryPrimitives.WriteUInt32LittleEndian(data[12..],(uint)WorkshopWireVersion.GenericMechanical);
        BinaryPrimitives.WriteUInt64LittleEndian(data[16..],response.Read.Epoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..],response.Read.Tick.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[32..],response.Read.Revision.Value);
        WriteSession(data[40..56],response.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(data[56..],response.Publication.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[64..],response.MasterGeneration.Value);
        if (response.Read.Capture is { } capture)
        {
            BinaryPrimitives.WriteInt64LittleEndian(data[72..],capture.Time.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[80..],capture.Uncertainty.Value);
            data[110]=checked((byte)capture.Domain); data[111]=1;
        }
        BinaryPrimitives.WriteUInt64LittleEndian(data[88..],response.Cadence.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[96..],response.Projection.Value);
        data[104]=response.Read.Bodies.Count; data[105]=response.Read.Captures.Count;
        data[106]=response.Read.Activations.Count; data[107]=response.Read.Timers.Count;
        data[112]=response.Read.Electrical.Count;
        WorkshopElectricalWire.Write(response.Read.Electrical,data[ReadElectricalOffset..]);
        data[108]=response.Read.ContactWorks.Count; data[109]=response.Read.ContactWorks.OccurrenceCount;
        for (var i=0; i<response.Read.Bodies.Count; i++)
            PhysicsBodyWire.Write(response.Read.Bodies[i],data.Slice(ReadBodiesOffset+i*PhysicsBodyWire.ByteLength,PhysicsBodyWire.ByteLength));
        for (var i=0; i<response.Read.Captures.Count; i++)
        {
            var latch=response.Read.Captures[i]; var slot=data.Slice(ReadCapturesOffset+i*CaptureBytes,CaptureBytes);
            BinaryPrimitives.WriteUInt64LittleEndian(slot,latch.Sensor.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[8..],(uint)latch.Phase);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[12..],latch.EventOrdinal);
            Write(slot[16..],latch.EventPhase);
        }
        WorkshopActivationWire.Write(response.Read.Activations,data[ReadActivationsOffset..ReadTimersOffset]);
        WorkshopTimerWire.Write(response.Read.Timers,data[ReadTimersOffset..ReadContactWorkOffset]);
        WorkshopContactWorkWire.Write(response.Read.ContactWorks,data[ReadContactWorkOffset..ReadMotionOffset]);
        if (response.Read.Motion is { } motion) motion.Bytes.CopyTo(data[ReadMotionOffset..ReadElectricalOffset]);
    }

    public static WorkshopResponse DecodeResponse(ReadOnlySpan<byte> data)
    {
        if (data.Length != ResponseBytes || !Zero(data[113..128]) ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..]) != (uint)WorkshopWireVersion.GenericMechanical)
            throw new ArgumentException("Unsupported Workshop response shape.");
        var sequence=new CommandSequence(BinaryPrimitives.ReadUInt64LittleEndian(data));
        var kind=(WorkshopResponseKind)data[8]; var outcome=(WorkshopCommandOutcome)data[9];
        var reason=(WorkshopRejection)data[10]; var phase=(WorkshopSimulationPhase)data[11];
        var session=ReadSession(data[40..56]);
        var publication=new PublicationSequence(BinaryPrimitives.ReadUInt64LittleEndian(data[56..]));
        var master=new ClockGeneration(BinaryPrimitives.ReadUInt64LittleEndian(data[64..]));
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(outcome) || !Enum.IsDefined(reason) || !Enum.IsDefined(phase) ||
            (kind == WorkshopResponseKind.Acknowledgement) != (sequence.Value != 0) ||
            (kind == WorkshopResponseKind.Read) != (publication.Value != 0) || data[111]>1)
            throw new ArgumentException("Invalid response discriminant or publication.");
        WorkshopClockStamp? capture=data[111]==0 ? null :
            new((WorkshopClockDomain)data[110],master,new(BinaryPrimitives.ReadInt64LittleEndian(data[72..])),
                new(BinaryPrimitives.ReadUInt64LittleEndian(data[80..])));
        if (capture is null && (data[110]!=0 || !Zero(data[64..88])))
            throw new ArgumentException("Absent capture has a nonzero payload.");
        var epoch=new SimulationEpoch(BinaryPrimitives.ReadUInt64LittleEndian(data[16..]));
        var tick=new SimulationTick(BinaryPrimitives.ReadUInt64LittleEndian(data[24..]));
        var bodyCount=data[104]; var captureCount=data[105];
        if (bodyCount>PhysicsBodyReadSet.Capacity || captureCount>PhysicsSceneDeclaration.SensorCapacity ||
            !Zero(data[(ReadBodiesOffset+bodyCount*PhysicsBodyWire.ByteLength)..ReadCapturesOffset]) ||
            !Zero(data[(ReadCapturesOffset+captureCount*CaptureBytes)..ReadActivationsOffset]))
            throw new ArgumentException("Invalid body or capture population/padding.");
        Span<PhysicsBodyRead> bodyValues=stackalloc PhysicsBodyRead[PhysicsBodyReadSet.Capacity];
        for (var i=0; i<bodyCount; i++)
            bodyValues[i]=PhysicsBodyWire.Read(data.Slice(ReadBodiesOffset+i*PhysicsBodyWire.ByteLength,PhysicsBodyWire.ByteLength),epoch,tick);
        var bodies=new PhysicsBodyReadSet(bodyValues[..bodyCount]);
        Span<CaptureLatch> latches=stackalloc CaptureLatch[PhysicsSceneDeclaration.SensorCapacity];
        for (var i=0; i<captureCount; i++)
        {
            var slot=data.Slice(ReadCapturesOffset+i*CaptureBytes,CaptureBytes);
            if (!Zero(slot[18..])) throw new ArgumentException("Nonzero capture padding.");
            latches[i]=new(new(BinaryPrimitives.ReadUInt64LittleEndian(slot)),
                (CaptureLatchPhase)BinaryPrimitives.ReadUInt32LittleEndian(slot[8..]),
                BinaryPrimitives.ReadUInt32LittleEndian(slot[12..]),Read(slot[16..]));
        }
        var read=new WorkshopRead(epoch,tick,bodies,new(BinaryPrimitives.ReadUInt64LittleEndian(data[32..])),capture,
            new(latches[..captureCount]),PhysicsMotionRead.Decode(data[ReadMotionOffset..ReadElectricalOffset],bodies,tick),
            WorkshopActivationWire.Read(data[ReadActivationsOffset..ReadTimersOffset],data[106]),
            WorkshopTimerWire.Read(data[ReadTimersOffset..ReadContactWorkOffset],data[107]),
            WorkshopContactWorkWire.Read(data[ReadContactWorkOffset..ReadMotionOffset],data[108],data[109]),
            WorkshopElectricalWire.Read(data[ReadElectricalOffset..],data[112]));
        ValidatePhysicalRead(read); ValidateCapture(read);
        var response=new WorkshopResponse(sequence,kind,new(outcome,reason),phase,read,session,publication,
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[88..])),master,
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[96..])),new(tick.Value));
        ValidateScheduleIdentity(response); return response;
    }

    private static void ValidatePhysicalRead(WorkshopRead read)
    {
        read.Bodies.ValidateTime(read.Epoch,read.Tick);
        for (var i=0; i<read.Bodies.Count; i++) read.Bodies[i].Validate();
        if (read.Motion is { } motion) motion.ValidateBinding(read.Bodies,read.Tick);
        else if (read.Tick.Value != 0) throw new ArgumentException("Committed motion description is required.");
        for (var i=0; i<read.Activations.Count; i++)
        {
            var value=read.Activations[i]; value.Validate();
            if (value.Phase == ActivationPhase.Latched && (!read.Bodies.TryGet(value.Body,out _) ||
                value.EventOrdinal>read.Tick.Value*8 || read.Tick.Value==0))
                throw new ArgumentException("Activation event does not belong to a running physical world.");
        }
        for (var i=0; i<read.Captures.Count; i++)
        {
            var value=read.Captures[i]; value.Validate();
            if (value.EventOrdinal>WorkshopCadenceSettings.PhysicalOrdinalLimit)
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
        else if (read.Epoch.Value != 0 || read.Tick.Value != 0 || read.Revision.Value != 0 || read.Bodies.Count != 0 || read.Captures.Count != 0 || read.Activations.Count != 0 || read.Timers.Count != 0 || read.ContactWorks.Count != 0 || read.ContactWorks.OccurrenceCount != 0 || read.Electrical.Count != 0)
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
        construction.Validate(); data.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(data, construction.Revision.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[8..], checked((uint)construction.Instances.Count));
        BinaryPrimitives.WriteUInt32LittleEndian(data[12..], checked((uint)construction.Connections.Count));
        WorkshopCadenceWire.Write(construction.Settings, data[16..48]);
        WorkshopPuzzleWire.Write(data[48..304], construction.Puzzle);
        for (var i = 0; i < construction.Instances.Count; i++)
        {
            var instance = construction.Instances[i];
            var slot = data.Slice(ConstructionHeaderBytes + i * InstanceBytes, InstanceBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(slot, (uint)instance.Kind);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[4..], instance.Locked ? 1u : 0u);
            new CanonicalBody(instance.Id, 0, 0, instance.Cell, instance.Local, default).Write(slot[16..96]);
            Write(slot[96..], instance.Rotation.X); Write(slot[98..], instance.Rotation.Y);
            Write(slot[100..], instance.Rotation.Z); Write(slot[102..], instance.Rotation.W);
            switch (instance)
            {
                case WorkshopBall ball:
                    Write(slot[104..], ball.Material.Radius.Value); Write(slot[106..], ball.Material.Mass.Value);
                    Write(slot[108..], ball.Material.Bounce.Value); Write(slot[110..], ball.Material.Drag.Value);
                    Write(slot[112..], ball.Material.Buoyancy.Value); break;
                case WorkshopReceiver receiver:
                    Write(slot[104..], receiver.Capture.Margin.Value); Write(slot[106..], receiver.Capture.SpeedLimit.Value);
                    Write(slot[108..], receiver.Capture.Dwell.Value);
                    BinaryPrimitives.WriteUInt32LittleEndian(slot[112..], (uint)receiver.Capture.Participation);
                    Write(slot[116..], receiver.ForceRegion.Minimum.X); Write(slot[118..], receiver.ForceRegion.Minimum.Y);
                    Write(slot[120..], receiver.ForceRegion.Minimum.Z); Write(slot[122..], receiver.ForceRegion.Maximum.X);
                    Write(slot[124..], receiver.ForceRegion.Maximum.Y); Write(slot[126..], receiver.ForceRegion.Maximum.Z);
                    Write(slot[128..], receiver.ForceRegion.SupportHeight.Value); Write(slot[130..], receiver.ForceRegion.SupportMargin.Value);
                    Write(slot[132..], receiver.ForceRegion.MaximumAcceleration.Value); break;
                case WorkshopRamp ramp:
                    Write(slot[104..], ramp.Dimensions.Length.Value); Write(slot[106..], ramp.Dimensions.Width.Value); break;
                case WorkshopWall wall:
                    Write(slot[104..], wall.Dimensions.Width.Value); Write(slot[106..], wall.Dimensions.Height.Value);
                    Write(slot[108..], wall.Dimensions.Thickness.Value); break;
                case WorkshopSwitch trigger:
                    Write(slot[104..], trigger.Trigger.Threshold.Value); break;
                case WorkshopDelay delay:
                    Write(slot[104..], delay.Duration.Seconds.Value); break;
                case WorkshopSpringboard spring:
                    BinaryPrimitives.WriteSingleLittleEndian(slot[104..], spring.Settings.Stiffness);
                    BinaryPrimitives.WriteSingleLittleEndian(slot[108..], spring.Settings.Damping); break;
                case WorkshopBattery battery:
                    BinaryPrimitives.WriteSingleLittleEndian(slot[104..], battery.Settings.Capacity.Value);
                    BinaryPrimitives.WriteSingleLittleEndian(slot[108..], battery.Settings.MaximumPower.Value);
                    BinaryPrimitives.WriteSingleLittleEndian(slot[112..], battery.Settings.InitialFraction);
                    slot[116] = (byte)battery.Settings.Enabled; break;
                case WorkshopBumper bumper:
                    BinaryPrimitives.WriteSingleLittleEndian(slot[104..], bumper.Work.Strength.Value); BinaryPrimitives.WriteSingleLittleEndian(slot[108..], bumper.Work.ReferenceMass.Value);
                    BinaryPrimitives.WriteSingleLittleEndian(slot[112..], bumper.Work.Preload.Value); break;
                case WorkshopDomino domino:
                    Write(slot[104..], domino.Material.HalfExtents.X); Write(slot[106..], domino.Material.HalfExtents.Y);
                    Write(slot[108..], domino.Material.HalfExtents.Z); Write(slot[110..], domino.Material.Mass.Value);
                    Write(slot[112..], domino.Material.Bounce.Value); Write(slot[114..], domino.Material.Friction.Value); break;
                case WorkshopLamp: break;
                default: throw new ArgumentException("Unsupported instance declaration.");
            }
        }
        for (var i = 0; i < construction.Connections.Count; i++)
        {
            var link = construction.Connections[i]; var slot = data.Slice(ConnectionsOffset + i * ConnectionBytes, ConnectionBytes);
            BinaryPrimitives.WriteUInt64LittleEndian(slot, link.Source.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(slot[8..], link.Target.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[16..], (uint)link.Domain);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[20..], (uint)link.Output);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[24..], (uint)link.Input);
        }
    }

    internal static WorkshopConstruction ReadConstruction(ReadOnlySpan<byte> data)
    {
        if (data.Length != ConstructionBytes) throw new ArgumentException("Invalid construction width.");
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        var linkCount = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        if (count > WorkshopInstances.Capacity || linkCount > WorkshopConnections.Capacity || !Zero(data[304..ConstructionHeaderBytes]) ||
            !Zero(data[(ConstructionHeaderBytes + checked((int)count) * InstanceBytes)..ConnectionsOffset]) ||
            !Zero(data[(ConnectionsOffset + checked((int)linkCount) * ConnectionBytes)..]))
            throw new ArgumentException("Invalid construction count or padding.");
        var instances = new IWorkshopInstance[count];
        for (var i = 0; i < instances.Length; i++)
        {
            var slot = data.Slice(ConstructionHeaderBytes + i * InstanceBytes, InstanceBytes);
            var kind = (WorkshopPartKind)BinaryPrimitives.ReadUInt32LittleEndian(slot);
            var locked = BinaryPrimitives.ReadUInt32LittleEndian(slot[4..]);
            if (locked > 1 || !Zero(slot[8..16])) throw new ArgumentException("Invalid authored instance flags.");
            var body = CanonicalBody.Decode(slot[16..96]);
            if (body.Epoch != 0 || body.Tick != 0 || !body.Velocity.IsPositiveZero)
                throw new ArgumentException("Construction contains live simulation state.");
            var rotation = new CanonicalRotation(Read(slot[96..]), Read(slot[98..]), Read(slot[100..]), Read(slot[102..]));
            instances[i] = kind switch
            {
                // Both ball kinds share the slot layout; the persisted bits must equal the kind's declared material, whose friction, bounce
                // threshold and rolling resistance are kind data rather than persisted fields.
                WorkshopPartKind.Basketball or WorkshopPartKind.BowlingBall when Zero(slot[114..]) => new WorkshopBall(body.Id, kind, body.Cell, body.Local, rotation,
                    BallMaterial.For(kind) with { Radius = new(Read(slot[104..])), Mass = new(Read(slot[106..])), Bounce = new(Read(slot[108..])),
                        Drag = new(Read(slot[110..])), Buoyancy = new(Read(slot[112..])) }, locked == 1),
                WorkshopPartKind.Receiver when Zero(slot[110..112]) && Zero(slot[134..]) => new WorkshopReceiver(body.Id, body.Cell, body.Local, rotation,
                    new(new(Read(slot[104..])), new(Read(slot[106..])), new(Read(slot[108..])),
                        (SensorParticipation)BinaryPrimitives.ReadUInt32LittleEndian(slot[112..])), locked == 1)
                    { ForceRegion = new(new(Read(slot[116..]), Read(slot[118..]), Read(slot[120..])),
                        new(Read(slot[122..]), Read(slot[124..]), Read(slot[126..])), new(Read(slot[128..])),
                        new(Read(slot[130..])), new(Read(slot[132..]))) },
                WorkshopPartKind.Ramp when Zero(slot[108..]) => new WorkshopRamp(body.Id, body.Cell, body.Local, rotation,
                    new(new(Read(slot[104..])), new(Read(slot[106..]))), locked == 1),
                WorkshopPartKind.Wall when Zero(slot[110..]) => new WorkshopWall(body.Id, body.Cell, body.Local, rotation,
                    new(new(Read(slot[104..])), new(Read(slot[106..])), new(Read(slot[108..]))), locked == 1),
                WorkshopPartKind.ImpactSwitch when Zero(slot[106..]) => new WorkshopSwitch(body.Id, body.Cell, body.Local, rotation,
                    new(new(Read(slot[104..]))), locked == 1),
                WorkshopPartKind.Delay when Zero(slot[106..]) => new WorkshopDelay(body.Id, body.Cell, body.Local, rotation,
                    new(new(Read(slot[104..]))), locked == 1),
                WorkshopPartKind.Springboard when Zero(slot[112..]) => new WorkshopSpringboard(body.Id, body.Cell, body.Local, rotation,
                    new(BinaryPrimitives.ReadSingleLittleEndian(slot[104..]), BinaryPrimitives.ReadSingleLittleEndian(slot[108..])), locked == 1),
                WorkshopPartKind.Battery when Zero(slot[117..]) => new WorkshopBattery(body.Id, body.Cell, body.Local, rotation,
                    new(new(BinaryPrimitives.ReadSingleLittleEndian(slot[104..])), new(BinaryPrimitives.ReadSingleLittleEndian(slot[108..])),
                        BinaryPrimitives.ReadSingleLittleEndian(slot[112..]), (ElectricalEnable)slot[116]), locked == 1),
                WorkshopPartKind.PinballBumper when Zero(slot[116..]) => new WorkshopBumper(body.Id, body.Cell, body.Local, rotation,
                    new(new(BinaryPrimitives.ReadSingleLittleEndian(slot[104..])), new(BinaryPrimitives.ReadSingleLittleEndian(slot[108..])), new(BinaryPrimitives.ReadSingleLittleEndian(slot[112..]))), locked == 1),
                WorkshopPartKind.Domino when Zero(slot[116..]) => new WorkshopDomino(body.Id, body.Cell, body.Local, rotation,
                    new(new(Read(slot[104..]), Read(slot[106..]), Read(slot[108..])), new(Read(slot[110..])), new(Read(slot[112..])), new(Read(slot[114..]))), locked == 1),
                WorkshopPartKind.SignalLamp when Zero(slot[104..]) => new WorkshopLamp(body.Id, body.Cell, body.Local, rotation, locked == 1),
                _ => throw new ArgumentException("Unsupported instance kind or parameters.")
            };
        }
        var links = new WorkshopConnection[linkCount];
        for (var i = 0; i < links.Length; i++)
        {
            var slot = data.Slice(ConnectionsOffset + i * ConnectionBytes, ConnectionBytes);
            if (!Zero(slot[28..])) throw new ArgumentException("Invalid connection padding.");
            links[i] = new(new(BinaryPrimitives.ReadUInt64LittleEndian(slot)), (WorkshopSocket)BinaryPrimitives.ReadUInt32LittleEndian(slot[20..]),
                new(BinaryPrimitives.ReadUInt64LittleEndian(slot[8..])), (WorkshopSocket)BinaryPrimitives.ReadUInt32LittleEndian(slot[24..]),
                (WorkshopConnectionDomain)BinaryPrimitives.ReadUInt32LittleEndian(slot[16..]));
        }
        var construction = new WorkshopConstruction(new(BinaryPrimitives.ReadUInt64LittleEndian(data)),
            WorkshopCadenceWire.Read(data[16..48]), new(instances), WorkshopPuzzleWire.Read(data[48..304]), new(links));
        construction.Validate(); return construction;
    }

    private static bool Zero(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes) if (value != 0) return false;
        return true;
    }
    private static void Write(Span<byte> data, Half value) => BinaryPrimitives.WriteUInt16LittleEndian(data, BitConverter.HalfToUInt16Bits(value));
    private static Half Read(ReadOnlySpan<byte> data) => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(data));
}
