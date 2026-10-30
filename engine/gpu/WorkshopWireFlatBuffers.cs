using CuriousContraptions.Abstractions;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Google.FlatBuffers;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

/// <summary>The runtime Workshop codec: commands, responses, compiled installations and diagnostics are FlatBuffers of
/// <c>schemas/workshop_wire.fbs</c>. Authoring and saves belong to the main-only authoring codec. Every buffer is size-prefixed and carries
/// its root's identifier; each is decoded once, and a buffer that is not exactly one verified root of the expected kind is a
/// <see cref="WireFaultException"/> naming why. Nothing else decodes these messages.</summary>
public static partial class WorkshopWireFlatBuffers
{

    public const string CommandIdentifier = "CCWC";
    public const string ResponseIdentifier = "CCWR";
    private const int IdentifierOffset = FlatBufferConstants.SizePrefixLength + sizeof(int);
    private const int FrameHeaderBytes = IdentifierOffset + FlatBufferConstants.FileIdentifierLength;

    /// <summary>The one decode boundary: the size prefix must cover the buffer exactly, the root identifier must be the expected one
    /// and the FlatBuffers verifier must admit the root. Returns the buffer positioned at its root.</summary>
    internal static ByteBuffer OpenFrame(ReadOnlySpan<byte> bytes, string identifier, VerifyTableAction verify)
    {
        if (bytes.Length < FlatBufferConstants.SizePrefixLength)
            throw new WireFaultException(WireFaultReason.Truncated, $"A {identifier} buffer is shorter than its size prefix.");
        var declared = (long)BinaryPrimitives.ReadUInt32LittleEndian(bytes) + FlatBufferConstants.SizePrefixLength;
        if (declared > bytes.Length)
            throw new WireFaultException(WireFaultReason.Truncated, $"A {identifier} buffer holds {bytes.Length} of its {declared} bytes.");
        if (declared < bytes.Length)
            throw new WireFaultException(WireFaultReason.TrailingBytes, $"A {identifier} buffer has {bytes.Length - declared} bytes after its {declared}.");
        if (bytes.Length < FrameHeaderBytes || !IsIdentifier(bytes[IdentifierOffset..FrameHeaderBytes], identifier))
            throw new WireFaultException(WireFaultReason.UnknownIdentifier, $"The buffer is not a {identifier} root.");
        var buffer = new ByteBuffer(bytes.ToArray());
        bool verified;
        // The vendored verifier's own offset arithmetic (Convert.ToUInt32 of a hostile signed offset) overflows on some corrupt
        // buffers instead of answering false: that is its refusal too, and only here.
        try { verified = new Verifier(buffer).SetMaxTables(TableVisitBudget(bytes.Length)).VerifyBuffer(identifier, true, verify); }
        catch (OverflowException failure)
        { throw new WireFaultException(WireFaultReason.VerifierRejected, $"The FlatBuffers verifier refused a {identifier} buffer.", failure); }
        if (!verified)
            throw new WireFaultException(WireFaultReason.VerifierRejected, $"The FlatBuffers verifier refused a {identifier} buffer.");
        var fieldCount = identifier switch { CommandIdentifier => 12, ResponseIdentifier => 29, _ => 0 };
        if (fieldCount != 0)
        {
            var root = checked(FlatBufferConstants.SizePrefixLength + buffer.GetInt(FlatBufferConstants.SizePrefixLength));
            var vtable = checked(root - buffer.GetInt(root));
            if (buffer.GetUshort(vtable) > 4 + fieldCount * 2)
                throw new WireFaultException(WireFaultReason.OutOfRange, "Runtime record contains undefined fields.");
        }
        return ByteBufferUtil.RemoveSizePrefix(buffer);
    }


    // Runtime roots contain compiled capability rows and nested presentation descriptors; authoring roots
    // contain instance/parameter vectors. No current path nests more than two table vectors.
    // Shared vectors are visited repeatedly: bounding unique tables by bytes would be incorrect.
    // Each offset vector has at most w four-byte entries, so 3+w+w*w bounds visits.
    // Leave one representable rejecting visit for the vendored increment-before-check counter.
    private static int TableVisitBudget(int bytes)
    {
        var words = (long)bytes / sizeof(int);
        return (int)Math.Min(int.MaxValue - 1L, checked(3L + words + words * words));
    }

    private static bool IsIdentifier(ReadOnlySpan<byte> bytes, string identifier)
    {
        for (var i = 0; i < FlatBufferConstants.FileIdentifierLength; i++)
            if (bytes[i] != identifier[i]) return false;
        return true;
    }

    private static RuntimeSessionId RequiredSession(byte[]? bytes) => bytes is { Length: RuntimeSessionId.ByteLength }
        ? RuntimeSessionId.Read(bytes)
        : throw new WireFaultException(WireFaultReason.MissingField, "A Workshop message requires its 16-byte runtime session.");

    private static byte[] RuntimeSessionBytes(RuntimeSessionId session)
    {
        var bytes = new byte[RuntimeSessionId.ByteLength]; session.WriteTo(bytes); return bytes;
    }

    private static VectorOffset Session(FlatBufferBuilder builder, RuntimeSessionId session)
    {
        var bytes = new byte[RuntimeSessionId.ByteLength];
        session.WriteTo(bytes);
        return Wire.WorkshopResponse.CreateSessionVectorBlock(builder, bytes);
    }

    public static byte[] EncodeCommand(WorkshopCommand command)
    {
        command.Session.Validate();
        var hasScene = command.Kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.Construct or WorkshopCommandKind.ConfigureCadence;
        command.Scene?.Validate();
        command.Electrical?.Validate();
        if (!Enum.IsDefined(command.Kind) || !Enum.IsDefined(command.RevisionKind) ||
            (command.RevisionKind == ExpectedRevisionKind.Any && command.Revision.Value != 0) ||
            (command.Kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Save && command.RevisionKind != ExpectedRevisionKind.Exact) ||
            command.Sequence.Value == 0 || hasScene != (command.Scene is not null) ||
            (command.Kind == WorkshopCommandKind.ConfigureElectrical) != command.Electrical.HasValue ||
            (command.Kind == WorkshopCommandKind.CutRope) != command.Cut.HasValue || command.Cut is { Value: 0 } ||
            (command.Kind == WorkshopCommandKind.Initialize
                ? command.Cadence.Value != 0 || command.Projection.Value != 0
                : command.Cadence.Value == 0 || command.Projection.Value == 0) ||
            (command.Kind == WorkshopCommandKind.Cancel) != command.Target.HasValue ||
            (command.Target is { } target && (target.Sequence.Value == 0 || target.Epoch.Value == 0 || target.Cadence.Value == 0 || target.Projection.Value == 0)))
            throw new ArgumentException("Unsupported Workshop command.");

        var builder = new FlatBufferBuilder(512);
        var session = Session(builder, command.Session);
        Offset<CompiledSceneRecord>? scene = command.Scene is { } built ? EncodeCompiledScene(builder, built) : null;

        Wire.WorkshopCommand.StartWorkshopCommand(builder);
        Wire.WorkshopCommand.AddSequence(builder, command.Sequence.Value);
        Wire.WorkshopCommand.AddKind(builder, (CommandKind)command.Kind);
        Wire.WorkshopCommand.AddEpoch(builder, command.Epoch.Value);
        Wire.WorkshopCommand.AddRevision(builder, command.Revision.Value);
        Wire.WorkshopCommand.AddRevisionKind(builder, (uint)command.RevisionKind);
        Wire.WorkshopCommand.AddSession(builder, session);
        Wire.WorkshopCommand.AddCadence(builder, command.Cadence.Value);
        Wire.WorkshopCommand.AddProjection(builder, command.Projection.Value);
        if (command.Cut is { } cut) Wire.WorkshopCommand.AddCutSpan(builder, cut.Value);
        if (command.Electrical is { } electrical)
            Wire.WorkshopCommand.AddElectrical(builder, ElectricalControl.CreateElectricalControl(builder, electrical.Owner.Value, (byte)electrical.Enabled));
        if (command.Target is { } identity)
            Wire.WorkshopCommand.AddTarget(builder, CommandIdentityRecord.CreateCommandIdentityRecord(builder,
                identity.Sequence.Value, identity.Epoch.Value, identity.Revision.Value, identity.Cadence.Value, identity.Projection.Value));
        if (scene is { } record) Wire.WorkshopCommand.AddScene(builder, record);
        builder.FinishSizePrefixed(Wire.WorkshopCommand.EndWorkshopCommand(builder).Value, CommandIdentifier);
        return builder.SizedByteArray();
    }

    public static WorkshopCommand DecodeCommand(ReadOnlySpan<byte> bytes)
    {
        var fb = Wire.WorkshopCommand.GetRootAsWorkshopCommand(OpenFrame(bytes, CommandIdentifier, WorkshopCommandVerify.Verify));
        var session = RequiredSession(fb.GetSessionArray());

        var revisionKind = (ExpectedRevisionKind)fb.RevisionKind;
        if (!Enum.IsDefined(revisionKind) ||
            (revisionKind == ExpectedRevisionKind.Any && fb.Revision != 0))
            throw new ArgumentException("Unsupported expected revision.");

        var kind = (WorkshopCommandKind)fb.Kind;
        if (!Enum.IsDefined(kind))
            throw new WireFaultException(WireFaultReason.UnknownCommand, $"The command names kind {(uint)fb.Kind}, which this game does not have.");
        var sequence = new CommandSequence(fb.Sequence);
        var cadence = new CadenceRevision(fb.Cadence);
        var projection = new ProjectionEpoch(fb.Projection);
        var hasScene = kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.Construct or WorkshopCommandKind.ConfigureCadence;
        if ((kind == WorkshopCommandKind.Initialize ? cadence.Value != 0 || projection.Value != 0 : cadence.Value == 0 || projection.Value == 0) ||
            sequence.Value == 0 ||
            (kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Save && revisionKind != ExpectedRevisionKind.Exact) ||
            hasScene != fb.Scene.HasValue ||
            (kind == WorkshopCommandKind.ConfigureElectrical) != fb.Electrical.HasValue ||
            (kind == WorkshopCommandKind.CutRope) != fb.CutSpan.HasValue || fb.CutSpan == 0 ||
            (kind == WorkshopCommandKind.Cancel) != fb.Target.HasValue)
            throw new ArgumentException("Unsupported Workshop command shape.");

        WorkshopCommandIdentity? target = null;
        if (fb.Target is { } t)
        {
            target = new WorkshopCommandIdentity(new(t.Sequence), new(t.Epoch), new(t.Revision), new(t.Cadence), new(t.Projection));
            if (target.Value.Sequence.Value == 0 || target.Value.Epoch.Value == 0 || target.Value.Cadence.Value == 0 || target.Value.Projection.Value == 0)
                throw new ArgumentException("Cancel target must have a complete original identity.");
        }

        WorkshopElectricalControl? electrical = null;
        if (fb.Electrical is { } e)
        {
            electrical = new WorkshopElectricalControl(new GpuBodyId(e.Owner), (ElectricalEnable)e.Enabled);
            electrical.Value.Validate();
        }

        var scene = fb.Scene is { } record ? DecodeCompiledScene(record) : null;
        return new WorkshopCommand(sequence, kind, new(fb.Epoch), new(fb.Revision), scene,
            target, revisionKind, session, cadence, projection, electrical, fb.CutSpan is { } cut ? new RopeSpanId(cut) : null);
    }

    public static byte[] ReadIdentity(WorkshopResponse response)
    {
        var identity = new byte[WorkshopClockWire.ReceiptBytes];
        WorkshopClockWire.WriteReceipt(response, identity);
        return identity;
    }

    public static byte[] EncodeResponse(WorkshopResponse response) => EncodeResponse(response, new FlatBufferBuilder(4096));

    /// <summary>Encodes into a caller-owned builder (cleared first), so a per-tick publisher reuses one builder's storage.</summary>
    public static byte[] EncodeResponse(WorkshopResponse response, FlatBufferBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (!Enum.IsDefined(response.Kind) || !Enum.IsDefined(response.Phase) ||
            !Enum.IsDefined(response.Result.Outcome) || !Enum.IsDefined(response.Result.Reason) ||
            (response.Kind == WorkshopResponseKind.Acknowledgement) != (response.Sequence.Value != 0))
            throw new ArgumentException("Unsupported Workshop response.");
        response.Session.Validate();
        if ((response.Kind == WorkshopResponseKind.Read) != (response.Publication.Value != 0))
            throw new ArgumentException("Invalid physical publication identity.");
        ValidateMemory(response);
        WorkshopMessages.ValidateCapture(response.Read);
        WorkshopMessages.ValidateScheduleIdentity(response);
        if (response.Scene is { } ownerConstruction) WorkshopMessages.ValidatePresentationOwners(response.Read, ownerConstruction);
        WorkshopMessages.ValidatePhysicalRead(response.Read);

        builder.Clear();
        var read = response.Read;
        Offset<CompiledSceneRecord>? constructionOffset = response.Scene is { } construction
            ? EncodeCompiledScene(builder, construction) : null;
        var ropeOffsets = new Offset<RopeSpanReadRecord>[read.Presentation.Ropes.Length];
        for (var i = 0; i < ropeOffsets.Length; i++)
        {
            var rope = read.Presentation.Ropes[i];
            RopeSpanReadRecord.StartRopeSpanReadRecord(builder);
            RopeSpanReadRecord.AddId(builder, rope.Id.Value); RopeSpanReadRecord.AddRoute(builder, rope.Route.Value);
            RopeSpanReadRecord.AddOrder(builder, rope.Order);
            RopeSpanReadRecord.AddBodyA(builder, rope.BodyA.Value); RopeSpanReadRecord.AddBodyB(builder, rope.BodyB.Value);
            RopeSpanReadRecord.AddAttachmentA(builder, Vec3f.CreateVec3f(builder, rope.AttachmentA.X, rope.AttachmentA.Y, rope.AttachmentA.Z));
            RopeSpanReadRecord.AddAttachmentB(builder, Vec3f.CreateVec3f(builder, rope.AttachmentB.X, rope.AttachmentB.Y, rope.AttachmentB.Z));
            RopeSpanReadRecord.AddMaximumLength(builder, rope.MaximumLength); RopeSpanReadRecord.AddCurrentLength(builder, rope.CurrentLength);
            RopeSpanReadRecord.AddIntegrity(builder, (uint)rope.Integrity); RopeSpanReadRecord.AddState(builder, (uint)rope.State);
            RopeSpanReadRecord.AddCutSpan(builder, rope.CutSpan.Value);
            ropeOffsets[i] = RopeSpanReadRecord.EndRopeSpanReadRecord(builder);
        }
        var ropesVector = Wire.WorkshopResponse.CreateRopesVector(builder, ropeOffsets);
        VectorOffset? pulleyOffset = null;
        if (!read.Presentation.Pulleys.IsEmpty)
        {
            var pulleyOffsets = new Offset<Wire.PulleyWheelReadRecord>[read.Presentation.Pulleys.Length];
            for (var i = 0; i < read.Presentation.Pulleys.Length; i++)
            {
                var p = read.Presentation.Pulleys[i];
                pulleyOffsets[i] = Wire.PulleyWheelReadRecord.CreatePulleyWheelReadRecord(builder, p.Pulley.Value, p.Travel, p.Angle);
            }
            pulleyOffset = Wire.WorkshopResponse.CreatePulleyWheelsVector(builder, pulleyOffsets);
        }
        VectorOffset? logicOffset = null;
        if (!read.Presentation.Logic.IsEmpty)
        {
            Wire.WorkshopResponse.StartElectricalLogicVector(builder, read.Presentation.Logic.Length);
            for(var i=read.Presentation.Logic.Length-1;i>=0;i--)
            {
                var logic=read.Presentation.Logic[i];
                ElectricalLogicRecord.CreateElectricalLogicRecord(builder,logic.Owner.Value,(byte)logic.Inputs,logic.Supplied,logic.Active);
            }
            logicOffset=builder.EndVector();
        }
        VectorOffset? actuatorOffset = null;
        if (!read.Presentation.Actuators.IsEmpty)
        {
            Wire.WorkshopResponse.StartActuatorDrivesVector(builder, read.Presentation.Actuators.Length);
            for (var i = read.Presentation.Actuators.Length - 1; i >= 0; i--)
            {
                var a = read.Presentation.Actuators[i];
                ActuatorDriveRecord.CreateActuatorDriveRecord(builder, a.Joint.Value, a.Owner.Value, a.SupplyClosed, a.MovingContact,
                    a.Coordinate, a.Speed, a.Lower, a.Upper);
            }
            actuatorOffset = builder.EndVector();
        }
        VectorOffset? loadOffset = null;
        if (!read.Presentation.Loads.IsEmpty)
        {
            Wire.WorkshopResponse.StartContactLoadsVector(builder, read.Presentation.Loads.Length);
            for (var i = read.Presentation.Loads.Length - 1; i >= 0; i--)
            {
                var l = read.Presentation.Loads[i];
                ContactLoadRecord.CreateContactLoadRecord(builder, l.Sensor.Value, l.Frame.Value, l.Region.Value, l.MeasuredMass, (uint)l.Phase);
            }
            loadOffset = builder.EndVector();
        }
        var session = Session(builder, response.Session);
        VectorOffset? motion = read.Motion is { } committed ? Wire.WorkshopResponse.CreateMotionVectorBlock(builder, committed.Bytes.ToArray()) : null;

        VectorOffset? electricalOffset = null;
        if (read.Electrical.Count > 0)
        {
            Wire.WorkshopResponse.StartElectricalVector(builder, read.Electrical.Count);
            for (var i = read.Electrical.Count - 1; i >= 0; i--)
            {
                var e = read.Electrical[i];
                ElectricalSourceRecord.CreateElectricalSourceRecord(builder, e.Id.Value, e.Owner.Value, e.Remaining.Value, e.Debit.Value, (byte)e.Enabled, e.Available);
            }
            electricalOffset = builder.EndVector();
        }

        VectorOffset? contactWorksOffset = null;
        if (read.ContactWorks.Count > 0)
        {
            Wire.WorkshopResponse.StartContactWorksVector(builder, read.ContactWorks.Count);
            for (var i = read.ContactWorks.Count - 1; i >= 0; i--)
            {
                var w = read.ContactWorks[i];
                ContactWorkStoreRecord.CreateContactWorkStoreRecord(builder, w.Id.Value, w.Owner.Value, w.OccurrenceCount, w.RemainingEnergy.Value, w.SuppliedEnergy.Value);
            }
            contactWorksOffset = builder.EndVector();
        }

        VectorOffset? bodiesOffset = null;
        if (read.Bodies.Count > 0)
        {
            Wire.WorkshopResponse.StartBodiesVector(builder, read.Bodies.Count);
            for (var i = read.Bodies.Count - 1; i >= 0; i--)
            {
                var b = read.Bodies[i];
                BodyRead.CreateBodyRead(builder, b.Body.Id.Value,
                    b.Body.Cell.X, b.Body.Cell.Y, b.Body.Cell.Z,
                    b.Body.Local.X, b.Body.Local.Y, b.Body.Local.Z,
                    b.Rotation.X, b.Rotation.Y, b.Rotation.Z, b.Rotation.W,
                    b.LocalCentreOfMass.X, b.LocalCentreOfMass.Y, b.LocalCentreOfMass.Z,
                    b.Body.Velocity.X, b.Body.Velocity.Y, b.Body.Velocity.Z,
                    b.AngularVelocity.X, b.AngularVelocity.Y, b.AngularVelocity.Z);
            }
            bodiesOffset = builder.EndVector();
        }

        VectorOffset? capturesOffset = null;
        if (read.Captures.Count > 0)
        {
            Wire.WorkshopResponse.StartCapturesVector(builder, read.Captures.Count);
            for (var i = read.Captures.Count - 1; i >= 0; i--)
            {
                var c = read.Captures[i];
                CaptureLatchRead.CreateCaptureLatchRead(builder, c.Sensor.Value, c.Target.Value, (uint)c.Phase, c.EventOrdinal, c.EventPhase);
            }
            capturesOffset = builder.EndVector();
        }

        VectorOffset? activationsOffset = null;
        if (read.Activations.Count > 0)
        {
            Wire.WorkshopResponse.StartActivationsVector(builder, read.Activations.Count);
            for (var i = read.Activations.Count - 1; i >= 0; i--)
            {
                var a = read.Activations[i];
                ActivationRecord.CreateActivationRecord(builder, a.Node.Value, a.Owner.Value, a.Source.Value, a.Body.Value, a.Collider.Value,
                    (uint)a.Phase, a.EventOrdinal, (float)a.EventPhase, (float)a.ApproachSpeed.Value, (uint)a.Kind, a.Emitter.Value,
                    a.CauseOrdinal, (float)a.CausePhase);
            }
            activationsOffset = builder.EndVector();
        }

        VectorOffset? timersOffset = null;
        if (read.Timers.Count > 0)
        {
            Wire.WorkshopResponse.StartTimersVector(builder, read.Timers.Count);
            for (var i = read.Timers.Count - 1; i >= 0; i--)
            {
                var t = read.Timers[i];
                TimerRecord.CreateTimerRecord(builder, t.Node.Value, (uint)t.Phase, t.StartedTick, t.DueTick, (uint)t.Input.Kind,
                    t.Input.Time.Ordinal, (float)t.Input.Time.Phase, (float)t.Input.Cause.ApproachSpeed.Value, t.Input.Emitter.Value,
                    t.Input.Cause.Source.Value, t.Input.Cause.Body.Value, t.Input.Cause.Collider.Value, t.Input.Cause.Time.Ordinal,
                    (float)t.Input.Cause.Time.Phase);
            }
            timersOffset = builder.EndVector();
        }

        // Committed counter and clock memory rows (Story 9R.1 D3)
        VectorOffset? memoryOffset = null;
        if (read.Memory.Count > 0)
        {
            Wire.WorkshopResponse.StartActivationMemoryVector(builder, read.Memory.Count);
            for (var i = read.Memory.Count - 1; i >= 0; i--)
            {
                var m = read.Memory[i];
                ActivationMemoryRecord.CreateActivationMemoryRecord(builder, m.Node.Value, (uint)m.Kind, m.Count,
                    m.Last.Ordinal, (float)m.Last.Phase, (uint)m.Phase, m.DueTick);
            }
            memoryOffset = builder.EndVector();
        }

        Wire.WorkshopResponse.StartWorkshopResponse(builder);
        if (logicOffset.HasValue) Wire.WorkshopResponse.AddElectricalLogic(builder,logicOffset.Value);
        Wire.WorkshopResponse.AddRopes(builder, ropesVector);
        if (pulleyOffset.HasValue) Wire.WorkshopResponse.AddPulleyWheels(builder, pulleyOffset.Value);
        if (actuatorOffset.HasValue) Wire.WorkshopResponse.AddActuatorDrives(builder, actuatorOffset.Value);
        if (loadOffset.HasValue) Wire.WorkshopResponse.AddContactLoads(builder, loadOffset.Value);
        if (constructionOffset.HasValue) Wire.WorkshopResponse.AddScene(builder, constructionOffset.Value);
        Wire.WorkshopResponse.AddSequence(builder, response.Sequence.Value);
        Wire.WorkshopResponse.AddKind(builder, (ResponseKind)response.Kind);
        Wire.WorkshopResponse.AddOutcome(builder, (CommandOutcome)response.Result.Outcome);
        Wire.WorkshopResponse.AddReason(builder, (byte)response.Result.Reason);
        Wire.WorkshopResponse.AddPhase(builder, (SimulationPhase)response.Phase);
        Wire.WorkshopResponse.AddEpoch(builder, read.Epoch.Value);
        Wire.WorkshopResponse.AddTick(builder, read.Tick.Value);
        Wire.WorkshopResponse.AddRevision(builder, read.Revision.Value);
        Wire.WorkshopResponse.AddPublication(builder, response.Publication.Value);
        Wire.WorkshopResponse.AddMasterGeneration(builder, response.MasterGeneration.Value);
        Wire.WorkshopResponse.AddCadence(builder, response.Cadence.Value);
        Wire.WorkshopResponse.AddProjection(builder, response.Projection.Value);
        Wire.WorkshopResponse.AddSession(builder, session);
        if (read.Capture is { } capture)
            Wire.WorkshopResponse.AddCapture(builder, ClockStamp.CreateClockStamp(builder, capture.Time.Value, capture.Uncertainty.Value, (byte)capture.Domain));
        if (bodiesOffset.HasValue) Wire.WorkshopResponse.AddBodies(builder, bodiesOffset.Value);
        if (capturesOffset.HasValue) Wire.WorkshopResponse.AddCaptures(builder, capturesOffset.Value);
        if (activationsOffset.HasValue) Wire.WorkshopResponse.AddActivations(builder, activationsOffset.Value);
        if (timersOffset.HasValue) Wire.WorkshopResponse.AddTimers(builder, timersOffset.Value);
        if (contactWorksOffset.HasValue) Wire.WorkshopResponse.AddContactWorks(builder, contactWorksOffset.Value);
        if (electricalOffset.HasValue) Wire.WorkshopResponse.AddElectrical(builder, electricalOffset.Value);
        if (memoryOffset.HasValue) Wire.WorkshopResponse.AddActivationMemory(builder, memoryOffset.Value);
        if (motion.HasValue) Wire.WorkshopResponse.AddMotion(builder, motion.Value);
        if (response.Memory is { } observed)
            Wire.WorkshopResponse.AddMemory(builder, MemoryObservationRecord.CreateMemoryObservationRecord(builder, observed.UsedBefore,
                observed.UsedAfter, observed.DurationTicks, observed.Frequency, observed.AllocatedBefore, observed.AllocatedAfter,
                observed.CollectionsBefore, observed.CollectionsAfter));
        builder.FinishSizePrefixed(Wire.WorkshopResponse.EndWorkshopResponse(builder).Value, ResponseIdentifier);
        return builder.SizedByteArray();
    }

    public static WorkshopResponse DecodeResponse(ReadOnlySpan<byte> bytes)
    {
        var fb = Wire.WorkshopResponse.GetRootAsWorkshopResponse(OpenFrame(bytes, ResponseIdentifier, WorkshopResponseVerify.Verify));
        var session = RequiredSession(fb.GetSessionArray());

        var sequence = new CommandSequence(fb.Sequence);
        var kind = (WorkshopResponseKind)fb.Kind;
        var outcome = (WorkshopCommandOutcome)fb.Outcome;
        var reason = (WorkshopRejection)fb.Reason;
        var phase = (WorkshopSimulationPhase)fb.Phase;
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(outcome) || !Enum.IsDefined(reason) || !Enum.IsDefined(phase) ||
            (kind == WorkshopResponseKind.Acknowledgement) != (sequence.Value != 0) ||
            (kind == WorkshopResponseKind.Read) != (fb.Publication != 0))
            throw new ArgumentException("Invalid response discriminant or publication.");

        var epoch = new SimulationEpoch(fb.Epoch);
        var tick = new SimulationTick(fb.Tick);
        var master = new ClockGeneration(fb.MasterGeneration);

        WorkshopClockStamp? capture = null;
        if (fb.Capture is { } c)
            capture = new WorkshopClockStamp((WorkshopClockDomain)c.Domain, master, new MonotonicNanoseconds(c.Time), new TimestampUncertainty(c.Uncertainty));

        var bodyValues = new PhysicsBodyRead[fb.BodiesLength];
        for (var i = 0; i < bodyValues.Length; i++)
        {
            var b = fb.Bodies(i)!.Value;
            var body = new CanonicalBody(new GpuBodyId(b.Id), epoch.Value, tick.Value,
                new CellOrigin(b.Cell.X, b.Cell.Y, b.Cell.Z), new LocalPosition(b.Local.X, b.Local.Y, b.Local.Z),
                new LinearVelocity(b.Velocity.X, b.Velocity.Y, b.Velocity.Z));
            bodyValues[i] = new PhysicsBodyRead(body, new CanonicalRotation(b.Rotation.X, b.Rotation.Y, b.Rotation.Z, b.Rotation.W),
                new AngularVelocity(b.AngularVelocity.X, b.AngularVelocity.Y, b.AngularVelocity.Z),
                new MetreVector(b.Com.X, b.Com.Y, b.Com.Z));
        }
        var bodies = new PhysicsBodyReadSet(bodyValues);

        var latches = new CaptureLatch[fb.CapturesLength];
        for (var i = 0; i < latches.Length; i++)
        {
            var cl = fb.Captures(i)!.Value;
            latches[i] = new CaptureLatch(new GpuSensorId(cl.Sensor), new GpuBodyId(cl.Target), (CaptureLatchPhase)cl.Phase, cl.EventOrdinal, cl.EventPhase);
        }

        var actValues = new ActivationLatch[fb.ActivationsLength];
        for (var i = 0; i < actValues.Length; i++)
        {
            var a = fb.Activations(i)!.Value;
            actValues[i] = new ActivationLatch(new ActivationNodeId(a.Node), new GpuBodyId(a.Owner), (ActivationPhase)a.Phase,
                new GpuEventSourceId(a.Source), new GpuBodyId(a.Body), new GpuColliderId(a.Collider), a.EventOrdinal, a.EventPhase,
                new ContactSpeed(a.ApproachSpeed), (ActivationOccurrenceKind)a.Kind, new ActivationNodeId(a.Emitter),
                a.CauseOrdinal, a.CausePhase);
        }

        var timerValues = new ActivationTimerState[fb.TimersLength];
        for (var i = 0; i < timerValues.Length; i++)
        {
            var t = fb.Timers(i)!.Value;
            var timerPhase = (ActivationTimerPhase)t.Phase;
            ActivationOccurrence input = default;
            if (timerPhase != ActivationTimerPhase.Ready)
                input = new ActivationOccurrence((ActivationOccurrenceKind)t.InputKind, new ActivationNodeId(t.InputEmitter),
                    new ActivationTime(t.InputOrdinal, t.InputPhase),
                    new ActivationCause(new GpuEventSourceId(t.CauseSource), new GpuBodyId(t.CauseBody), new GpuColliderId(t.CauseCollider),
                        new ActivationTime(t.CauseOrdinal, t.CausePhase), new ContactSpeed(t.InputApproachSpeed)));
            timerValues[i] = new ActivationTimerState(new ActivationNodeId(t.Node), timerPhase, t.StartedTick, t.DueTick, input);
        }

        // The record's own vector lengths (Story 9R.5b: never narrowed; a byte cast here truncated past 255 occurrences).
        var stores = new ContactWorkRead[fb.ContactWorksLength];
        for (var i = 0; i < stores.Length; i++)
        {
            var s = fb.ContactWorks(i)!.Value;
            stores[i] = new ContactWorkRead(new GpuContactWorkId(s.Id), new GpuBodyId(s.Owner), s.OccurrenceCount, new Joules(s.RemainingEnergy), new Joules(s.SuppliedEnergy));
        }

        var sources = new ElectricalSourceRead[fb.ElectricalLength];
        for (var i = 0; i < sources.Length; i++)
        {
            var e = fb.Electrical(i)!.Value;
            sources[i] = new ElectricalSourceRead(new ElectricalSourceId(e.Id), new GpuBodyId(e.Owner), new Joules(e.Remaining), new Joules(e.Debit), (ElectricalEnable)e.Enabled);
        }

        var ropeValues = new RopeSpanRead[fb.RopesLength];
        for (var i = 0; i < ropeValues.Length; i++)
        {
            var row = Required(fb.Ropes(i), "rope observation");
            var a = Required(row.AttachmentA, "rope attachment A"); var b = Required(row.AttachmentB, "rope attachment B");
            ropeValues[i] = new(new(row.Id), new(row.Route), row.Order, new(row.BodyA), new(row.BodyB),
                new(a.X, a.Y, a.Z), new(b.X, b.Y, b.Z), row.MaximumLength, row.CurrentLength,
                (RopeIntegrity)row.Integrity, (RopeState)row.State, new(row.CutSpan));
        }
        var pulleyValues = new PulleyWheelRead[fb.PulleyWheelsLength];
        for (var i = 0; i < pulleyValues.Length; i++)
        {
            var row = Required(fb.PulleyWheels(i), "pulley observation");
            pulleyValues[i] = new(new(row.Pulley), row.Travel, row.Angle);
        }
        var logicValues=new ElectricalLogicRead[fb.ElectricalLogicLength];
        for(var i=0;i<logicValues.Length;i++)
        {
            var logic=fb.ElectricalLogic(i)!.Value;
            logicValues[i]=new(new(logic.Owner),(ElectricalInputState)logic.Inputs,logic.Supplied,logic.Active);
        }
        var actuatorValues = new ActuatorDriveRead[fb.ActuatorDrivesLength];
        for (var i = 0; i < actuatorValues.Length; i++)
        {
            var a = fb.ActuatorDrives(i)!.Value;
            actuatorValues[i] = new(new(a.Joint), new(a.Owner), a.SupplyClosed, a.MovingContact, a.Coordinate, a.Speed, a.Lower, a.Upper);
        }
        var loadValues = new ContactLoadSensorRead[fb.ContactLoadsLength];
        for (var i = 0; i < loadValues.Length; i++)
        {
            var l = fb.ContactLoads(i)!.Value;
            loadValues[i] = new(new(l.Sensor), new(l.Frame), new(l.Region), l.MeasuredMass, (ContactLoadPhase)l.Phase);
        }
        var memoryValues = new ActivationMemory[fb.ActivationMemoryLength];
        for (var i = 0; i < memoryValues.Length; i++)
        {
            var m = fb.ActivationMemory(i)!.Value;
            memoryValues[i] = new ActivationMemory(new ActivationNodeId(m.Node), (ActivationNodeKind)m.Kind, m.Count,
                new ActivationTime(m.LastOrdinal, m.LastPhase), (OscillatorPhase)m.Phase, m.DueTick);
        }

        // A committed read past admission carries its motion; admission's absent motion stays absent (nothing is fabricated).
        var motionBytes = fb.GetMotionArray();
        if (motionBytes is null && tick.Value != 0)
            throw new WireFaultException(WireFaultReason.MissingField, $"The committed read at tick {tick.Value} has no motion.");
        var motion = motionBytes is null ? null : PhysicsMotionRead.Decode(motionBytes, bodies, tick);

        var read = new WorkshopRead(epoch, tick, bodies, new AuthorityRevision(fb.Revision), capture, new PhysicsCaptureRead(latches), motion,
            new PhysicsActivationRead(actValues), new PhysicsTimerRead(timerValues), new PhysicsContactWorkRead(stores),
            new PhysicsElectricalRead(sources), new PhysicsMemoryRead(memoryValues), new PhysicsPresentationRead(actuatorValues, loadValues, logicValues, ropeValues, pulleyValues));
        WorkshopMessages.ValidatePhysicalRead(read);
        WorkshopMessages.ValidateCapture(read);

        WorkshopMemoryMeasurement? memory = fb.Memory is { } r
            ? new(r.UsedBefore, r.UsedAfter, r.DurationTicks, r.Frequency, r.AllocatedBefore, r.AllocatedAfter, r.CollectionsBefore, r.CollectionsAfter)
            : null;
        var response = new WorkshopResponse(sequence, kind, new WorkshopCommandResult(outcome, reason), phase, read, session,
            new PublicationSequence(fb.Publication), new CadenceRevision(fb.Cadence), master, new ProjectionEpoch(fb.Projection),
            new PulseOrdinal(tick.Value), memory, fb.Scene is { } construction ? DecodeCompiledScene(construction) : null);
        WorkshopMessages.ValidateScheduleIdentity(response);
        if (response.Scene is { } ownerConstruction) WorkshopMessages.ValidatePresentationOwners(read, ownerConstruction);
        ValidateMemory(response);
        return response;
    }

    /// <summary>A memory measurement rides only on an applied acknowledgement (the memory query's), and is a valid measurement.</summary>
    private static void ValidateMemory(WorkshopResponse response)
    {
        if (response.Memory is not { } memory) return;
        if (response.Kind != WorkshopResponseKind.Acknowledgement || response.Result.Outcome != WorkshopCommandOutcome.Applied)
            throw new ArgumentException("Only an applied acknowledgement carries a memory measurement.");
        memory.Validate();
    }

    internal static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new WireFaultException(WireFaultReason.MissingField, $"Missing {field}.");
}
