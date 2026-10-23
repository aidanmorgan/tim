using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Google.FlatBuffers;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

/// <summary>The one Workshop codec (Story 9A.2): commands, responses (acknowledgements, physical reads and schedule endpoints) and
/// the construction record of saves are FlatBuffers of <c>schemas/workshop_wire.fbs</c>. Every buffer is size-prefixed and carries
/// its root's identifier; each is decoded once, and a buffer that is not exactly one verified root of the expected kind is a
/// <see cref="WireFaultException"/> naming why. Nothing else decodes these messages.</summary>
public static class WorkshopWireFlatBuffers
{

    /// <summary>Content uses the same current record constructor as wire input, after strict content/package admission.</summary>
    internal static IWorkshopInstance MapAuthoredInstance(AuthoredInstanceTemplate template, GpuBodyId id)
    {
        var builder = new FlatBufferBuilder(512);
        var part = builder.CreateString(template.Part.Value);
        var parameterOffsets = template.Parameters.Select(pair =>
            ParameterRecord.CreateParameterRecord(builder, builder.CreateString(pair.Key.Value), pair.Value)).ToArray();
        var parameters = InstanceRecord.CreateParametersVector(builder, parameterOffsets);
        var values = template.Scalars;
        InstanceRecord.StartInstanceRecord(builder);
        InstanceRecord.AddId(builder, id.Value);
        InstanceRecord.AddPartId(builder, part);
        InstanceRecord.AddLocked(builder, template.Locked);
        InstanceRecord.AddCell(builder, Vec3i.CreateVec3i(builder, template.Cell.X, template.Cell.Y, template.Cell.Z));
        InstanceRecord.AddLocal(builder, Vec3f.CreateVec3f(builder, template.Local.X, template.Local.Y, template.Local.Z));
        InstanceRecord.AddRotation(builder, Quat4f.CreateQuat4f(builder, template.Rotation.X, template.Rotation.Y, template.Rotation.Z, template.Rotation.W));
        InstanceRecord.AddMass(builder, values[AuthoredScalar.Mass]);
        InstanceRecord.AddBounce(builder, values[AuthoredScalar.Bounce]);
        InstanceRecord.AddLength(builder, values[AuthoredScalar.Length]);
        InstanceRecord.AddThreshold(builder, values[AuthoredScalar.Threshold]);
        InstanceRecord.AddHalfExtents(builder, Vec3f.CreateVec3f(builder, template.HalfExtents.X, template.HalfExtents.Y, template.HalfExtents.Z));
        InstanceRecord.AddFriction(builder, values[AuthoredScalar.Friction]);
        InstanceRecord.AddTargetCount(builder, template.TargetCount);
        InstanceRecord.AddParameters(builder, parameters);
        builder.Finish(InstanceRecord.EndInstanceRecord(builder).Value);
        return MapInstance(InstanceRecord.GetRootAsInstanceRecord(new ByteBuffer(builder.SizedByteArray())));
    }
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
        try { verified = new Verifier(buffer).VerifyBuffer(identifier, true, verify); }
        catch (OverflowException failure)
        { throw new WireFaultException(WireFaultReason.VerifierRejected, $"The FlatBuffers verifier refused a {identifier} buffer.", failure); }
        if (!verified)
            throw new WireFaultException(WireFaultReason.VerifierRejected, $"The FlatBuffers verifier refused a {identifier} buffer.");
        return ByteBufferUtil.RemoveSizePrefix(buffer);
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

    private static VectorOffset Session(FlatBufferBuilder builder, RuntimeSessionId session)
    {
        var bytes = new byte[RuntimeSessionId.ByteLength];
        session.WriteTo(bytes);
        return Wire.WorkshopResponse.CreateSessionVectorBlock(builder, bytes);
    }

    public static byte[] EncodeCommand(WorkshopCommand command)
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

        var builder = new FlatBufferBuilder(1024);
        var session = Session(builder, command.Session);
        Offset<ConstructionRecord>? construction = command.Construction is { } built ? EncodeConstructionRecord(builder, built) : null;

        Wire.WorkshopCommand.StartWorkshopCommand(builder);
        Wire.WorkshopCommand.AddSequence(builder, command.Sequence.Value);
        Wire.WorkshopCommand.AddKind(builder, (CommandKind)command.Kind);
        Wire.WorkshopCommand.AddEpoch(builder, command.Epoch.Value);
        Wire.WorkshopCommand.AddRevision(builder, command.Revision.Value);
        Wire.WorkshopCommand.AddRevisionKind(builder, (uint)command.RevisionKind);
        Wire.WorkshopCommand.AddSession(builder, session);
        Wire.WorkshopCommand.AddCadence(builder, command.Cadence.Value);
        Wire.WorkshopCommand.AddProjection(builder, command.Projection.Value);
        if (command.Electrical is { } electrical)
            Wire.WorkshopCommand.AddElectrical(builder, ElectricalControl.CreateElectricalControl(builder, electrical.Owner.Value, (byte)electrical.Enabled));
        if (command.Settings is { } settings)
            Wire.WorkshopCommand.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder,
                (uint)settings.Simulation, (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation,
                settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        if (command.Target is { } identity)
            Wire.WorkshopCommand.AddTarget(builder, CommandIdentityRecord.CreateCommandIdentityRecord(builder,
                identity.Sequence.Value, identity.Epoch.Value, identity.Revision.Value, identity.Cadence.Value, identity.Projection.Value));
        if (construction is { } record) Wire.WorkshopCommand.AddConstructionRecord(builder, record);
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
        var hasSettings = kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.ConfigureCadence;
        if ((kind == WorkshopCommandKind.Initialize ? cadence.Value != 0 || projection.Value != 0 : cadence.Value == 0 || projection.Value == 0) ||
            sequence.Value == 0 ||
            (kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Save && revisionKind != ExpectedRevisionKind.Exact) ||
            hasSettings != fb.Settings.HasValue ||
            (kind == WorkshopCommandKind.ConfigureElectrical) != fb.Electrical.HasValue ||
            (kind == WorkshopCommandKind.Construct) != fb.ConstructionRecord.HasValue ||
            (kind == WorkshopCommandKind.Cancel) != fb.Target.HasValue)
            throw new ArgumentException("Unsupported Workshop command shape.");

        WorkshopCommandIdentity? target = null;
        if (fb.Target is { } t)
        {
            target = new WorkshopCommandIdentity(new(t.Sequence), new(t.Epoch), new(t.Revision), new(t.Cadence), new(t.Projection));
            if (target.Value.Sequence.Value == 0 || target.Value.Epoch.Value == 0 || target.Value.Cadence.Value == 0 || target.Value.Projection.Value == 0)
                throw new ArgumentException("Cancel target must have a complete original identity.");
        }

        WorkshopCadenceSettings? settings = null;
        if (fb.Settings is { } s)
        {
            settings = new WorkshopCadenceSettings((SimulationCadence)s.Simulation, (PhysicalStepProfile)s.Physical,
                (AnimationCadence)s.Animation, (PresentationCadence)s.Presentation,
                new PulseRate(s.PresentationRateNumerator, s.PresentationRateDenominator));
            settings.Value.Validate();
        }

        WorkshopElectricalControl? electrical = null;
        if (fb.Electrical is { } e)
        {
            electrical = new WorkshopElectricalControl(new GpuBodyId(e.Owner), (ElectricalEnable)e.Enabled);
            electrical.Value.Validate();
        }

        return new WorkshopCommand(sequence, kind, new(fb.Epoch), new(fb.Revision),
            fb.ConstructionRecord is { } record ? DecodeConstructionRecord(record) : null,
            target, revisionKind, session, cadence, projection, settings, electrical);
    }

    /// <summary>The 24-byte receipt identity of a physical read (its session, then its publication): the Simulation worker hands
    /// it to the browser with the read, so no script parses a buffer to acknowledge it.</summary>
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
        if (response.Construction is { } ownerConstruction) WorkshopMessages.ValidatePresentationOwners(response.Read, ownerConstruction);
        WorkshopMessages.ValidatePhysicalRead(response.Read);

        builder.Clear();
        var read = response.Read;
        Offset<ConstructionRecord>? constructionOffset = response.Construction is { } construction
            ? EncodeConstructionRecord(builder, construction) : null;
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
                ContactLoadRecord.CreateContactLoadRecord(builder, l.Sensor.Value, l.Frame.Value, l.Face.Value, l.MeasuredMass, (uint)l.Phase);
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

        VectorOffset? occurrencesOffset = null;
        if (read.ContactWorks.OccurrenceCount > 0)
        {
            Wire.WorkshopResponse.StartOccurrencesVector(builder, read.ContactWorks.OccurrenceCount);
            for (var i = read.ContactWorks.OccurrenceCount - 1; i >= 0; i--)
            {
                var occ = read.ContactWorks.Occurrence(i);
                ContactWorkOccurrenceRecord.CreateContactWorkOccurrenceRecord(builder, occ.Work.Value, occ.Collider.Value, occ.Target.Value,
                    occ.Sequence, occ.EventOrdinal, (float)occ.EventPhase, occ.ApproachSpeed.Value, occ.Debit.Value, (byte)occ.Effect);
            }
            occurrencesOffset = builder.EndVector();
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
                    (float)b.LocalCentreOfMass.X, (float)b.LocalCentreOfMass.Y, (float)b.LocalCentreOfMass.Z,
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
                CaptureLatchRead.CreateCaptureLatchRead(builder, c.Sensor.Value, (uint)c.Phase, c.EventOrdinal, (float)c.EventPhase);
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
        if (actuatorOffset.HasValue) Wire.WorkshopResponse.AddActuatorDrives(builder, actuatorOffset.Value);
        if (loadOffset.HasValue) Wire.WorkshopResponse.AddContactLoads(builder, loadOffset.Value);
        if (constructionOffset.HasValue) Wire.WorkshopResponse.AddConstruction(builder, constructionOffset.Value);
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
        if (occurrencesOffset.HasValue) Wire.WorkshopResponse.AddOccurrences(builder, occurrencesOffset.Value);
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
                new MetreVector((Half)b.Com.X, (Half)b.Com.Y, (Half)b.Com.Z));
        }
        var bodies = new PhysicsBodyReadSet(bodyValues);

        var latches = new CaptureLatch[fb.CapturesLength];
        for (var i = 0; i < latches.Length; i++)
        {
            var cl = fb.Captures(i)!.Value;
            latches[i] = new CaptureLatch(new GpuSensorId(cl.Sensor), (CaptureLatchPhase)cl.Phase, cl.EventOrdinal, (Half)cl.EventPhase);
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
        var occurrences = new ContactWorkOccurrence[fb.OccurrencesLength];
        for (var i = 0; i < occurrences.Length; i++)
        {
            var occ = fb.Occurrences(i)!.Value;
            occurrences[i] = new ContactWorkOccurrence(new ContactWorkSlot(occ.Work), new ColliderSlot(occ.Collider), new GpuBodyId(occ.Target),
                occ.Sequence, occ.EventOrdinal, (Half)occ.EventPhase, new ContactSpeed(occ.ApproachSpeed), new Joules(occ.Debit), (ContactWorkEffect)occ.Effect);
        }

        var sources = new ElectricalSourceRead[fb.ElectricalLength];
        for (var i = 0; i < sources.Length; i++)
        {
            var e = fb.Electrical(i)!.Value;
            sources[i] = new ElectricalSourceRead(new ElectricalSourceId(e.Id), new GpuBodyId(e.Owner), new Joules(e.Remaining), new Joules(e.Debit), (ElectricalEnable)e.Enabled);
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
            loadValues[i] = new(new(l.Sensor), new(l.Frame), new(l.Face), l.MeasuredMass, (ContactLoadPhase)l.Phase);
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
            new PhysicsActivationRead(actValues), new PhysicsTimerRead(timerValues), new PhysicsContactWorkRead(stores, occurrences),
            new PhysicsElectricalRead(sources), new PhysicsMemoryRead(memoryValues), new PhysicsPresentationRead(actuatorValues, loadValues, logicValues));
        WorkshopMessages.ValidatePhysicalRead(read);
        WorkshopMessages.ValidateCapture(read);

        WorkshopMemoryMeasurement? memory = fb.Memory is { } r
            ? new(r.UsedBefore, r.UsedAfter, r.DurationTicks, r.Frequency, r.AllocatedBefore, r.AllocatedAfter, r.CollectionsBefore, r.CollectionsAfter)
            : null;
        var response = new WorkshopResponse(sequence, kind, new WorkshopCommandResult(outcome, reason), phase, read, session,
            new PublicationSequence(fb.Publication), new CadenceRevision(fb.Cadence), master, new ProjectionEpoch(fb.Projection),
            new PulseOrdinal(tick.Value), memory, fb.Construction is { } construction ? DecodeConstructionRecord(construction) : null);
        WorkshopMessages.ValidateScheduleIdentity(response);
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

    public static Offset<ConstructionRecord> EncodeConstructionRecord(FlatBufferBuilder builder, WorkshopConstruction construction, bool validate = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (validate) construction.Validate();

        Offset<PuzzleRecord>? puzzleOffset = null;
        if (construction.Puzzle.Id.IsAuthored)
        {
            var puzzle = construction.Puzzle;
            var puzzleIdOffset = builder.CreateString(puzzle.Id.Value);
            var invPartOffset = puzzle.InventoryPart.Value != null ? builder.CreateString(puzzle.InventoryPart.Value) : default;

            var assistanceBytes = new byte[WorkshopPuzzleWire.AssistanceBytes];
            WorkshopPuzzleWire.WriteProfile(assistanceBytes.AsSpan(0, WorkshopPuzzleWire.ProfileBytes), puzzle.PrimaryAssistance);
            WorkshopPuzzleWire.WriteProfile(assistanceBytes.AsSpan(WorkshopPuzzleWire.ProfileBytes, WorkshopPuzzleWire.ProfileBytes), puzzle.TargetAssistance);
            WorkshopPuzzleWire.WriteProfile(assistanceBytes.AsSpan(2 * WorkshopPuzzleWire.ProfileBytes, WorkshopPuzzleWire.ProfileBytes), puzzle.PlacementAssistance);
            var assistVec = PuzzleRecord.CreateRawAssistanceVector(builder, assistanceBytes);

            var goalBody = puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.SourceNode.Value : puzzle.Goal.Body.Value;
            var goalTarget = puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.TargetNode.Value : puzzle.Goal.Target.Value;

            PuzzleRecord.StartPuzzleRecord(builder);
            PuzzleRecord.AddId(builder, puzzleIdOffset);
            PuzzleRecord.AddPlacement(builder, (uint)puzzle.Placement);
            PuzzleRecord.AddPrecision(builder, (float)puzzle.Precision.Value);
            if (puzzle.InventoryPart.Value != null) PuzzleRecord.AddInventoryPart(builder, invPartOffset);
            PuzzleRecord.AddInventoryCount(builder, puzzle.InventoryCount);
            PuzzleRecord.AddGoalKind(builder, (uint)puzzle.Goal.Kind);
            PuzzleRecord.AddGoalBody(builder, goalBody);
            PuzzleRecord.AddGoalTarget(builder, goalTarget);
            PuzzleRecord.AddGoalEventSource(builder, puzzle.Goal.EventSource.Value);
            PuzzleRecord.AddGoalDelay(builder, (float)puzzle.Goal.MinimumDuration.Value);
            PuzzleRecord.AddRawAssistance(builder, assistVec);
            puzzleOffset = PuzzleRecord.EndPuzzleRecord(builder);
        }

        var instanceOffsets = new Offset<InstanceRecord>[construction.Instances.Count];
        for (var i = 0; i < construction.Instances.Count; i++)
        {
            var instance = construction.Instances[i];
            var partIdOffset = builder.CreateString(instance.PartId.Value);
            VectorOffset? parameterVector = null;
            if (instance is WorkshopGenericInstance generic)
            {
                var parameters = generic.Parameters.Select(pair =>
                    ParameterRecord.CreateParameterRecord(builder, builder.CreateString(pair.Key.Value), pair.Value)).ToArray();
                parameterVector = InstanceRecord.CreateParametersVector(builder, parameters);
            }

            InstanceRecord.StartInstanceRecord(builder);
            if (parameterVector.HasValue) InstanceRecord.AddParameters(builder, parameterVector.Value);
            InstanceRecord.AddId(builder, instance.Id.Value);
            InstanceRecord.AddPartId(builder, partIdOffset);
            InstanceRecord.AddLocked(builder, instance.Locked);
            InstanceRecord.AddCell(builder, Vec3i.CreateVec3i(builder, instance.Cell.X, instance.Cell.Y, instance.Cell.Z));
            InstanceRecord.AddLocal(builder, Vec3f.CreateVec3f(builder, instance.Local.X, instance.Local.Y, instance.Local.Z));
            InstanceRecord.AddRotation(builder, Quat4f.CreateQuat4f(builder, instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W));

            switch (instance)
            {
                case WorkshopDomino domino:
                    InstanceRecord.AddHalfExtents(builder, Vec3f.CreateVec3f(builder, (float)domino.Material.HalfExtents.X, (float)domino.Material.HalfExtents.Y, (float)domino.Material.HalfExtents.Z));
                    InstanceRecord.AddMass(builder, (float)domino.Material.Mass.Value);
                    InstanceRecord.AddBounce(builder, (float)domino.Material.Bounce.Value);
                    InstanceRecord.AddFriction(builder, (float)domino.Material.Friction.Value);
                    break;
                case WorkshopPipe pipe:
                    InstanceRecord.AddLength(builder, pipe.Length.Metres);
                    break;
                case WorkshopLamp:
                case WorkshopLatch:
                case IWorkshopDeclaredColliders:
                    break;
                case WorkshopPressurePlate plate:
                    InstanceRecord.AddThreshold(builder, plate.MinimumMass);
                    break;
                case WorkshopCounter counter:
                    InstanceRecord.AddTargetCount(builder, checked((uint)counter.TargetCount));
                    break;
                case WorkshopClock clock:
                    InstanceRecord.AddThreshold(builder, clock.IntervalSeconds);
                    break;
                default:
                    throw new ArgumentException("Unsupported instance declaration.");
            }

            instanceOffsets[i] = InstanceRecord.EndInstanceRecord(builder);
        }
        var instancesVector = ConstructionRecord.CreateInstancesVector(builder, instanceOffsets);

        VectorOffset? connectionsVector = null;
        if (construction.Connections.Count > 0)
        {
            ConstructionRecord.StartConnectionsVector(builder, construction.Connections.Count);
            for (var i = construction.Connections.Count - 1; i >= 0; i--)
            {
                var link = construction.Connections[i];
                ConnectionRecord.CreateConnectionRecord(builder, link.Source.Value, link.Target.Value, (uint)link.Domain, (uint)link.Output, (uint)link.Input);
            }
            connectionsVector = builder.EndVector();
        }

        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, construction.Revision.Value);
        ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder,
            (uint)construction.Settings.Simulation, (uint)construction.Settings.Physical, (uint)construction.Settings.Animation,
            (uint)construction.Settings.Presentation, construction.Settings.PresentationRate.Numerator, construction.Settings.PresentationRate.Denominator));
        if (puzzleOffset.HasValue) ConstructionRecord.AddPuzzle(builder, puzzleOffset.Value);
        ConstructionRecord.AddInstances(builder, instancesVector);
        if (connectionsVector.HasValue) ConstructionRecord.AddConnections(builder, connectionsVector.Value);
        return ConstructionRecord.EndConstructionRecord(builder);
    }

    /// <summary>Decodes and validates a construction record (the Construct command's): the domain rules reject an inadmissible one.</summary>
    public static WorkshopConstruction DecodeConstructionRecord(ConstructionRecord cr)
    {
        var construction = MapConstructionRecord(cr);
        construction.Validate();
        return construction;
    }

    /// <summary>The record's values as a construction, without validating them (a save's pre-validation has already admitted them;
    /// a command's are validated by <see cref="DecodeConstructionRecord"/>). An absent required record or field is a
    /// <see cref="WireFaultException"/>; nothing is defaulted.</summary>
    internal static WorkshopConstruction MapConstructionRecord(ConstructionRecord cr)
    {
        var s = cr.Settings ?? throw new WireFaultException(WireFaultReason.MissingField, "A construction record requires its cadence settings.");
        var settings = new WorkshopCadenceSettings((SimulationCadence)s.Simulation, (PhysicalStepProfile)s.Physical,
            (AnimationCadence)s.Animation, (PresentationCadence)s.Presentation,
            new PulseRate(s.PresentationRateNumerator, s.PresentationRateDenominator));

        WorkshopPuzzle puzzle = default;
        if (cr.Puzzle is { } p)
        {
            var kind = (WorkshopGoalKind)p.GoalKind;
            var sensor = new GpuSensorId(p.GoalEventSource);
            var goal = kind == WorkshopGoalKind.ActivatedAfter
                ? new WorkshopGoal(kind, default, default, sensor, new(p.GoalBody), new(p.GoalTarget), new((Half)p.GoalDelay))
                : new WorkshopGoal(kind, new(p.GoalBody), new(p.GoalTarget), sensor, MinimumDuration: new((Half)p.GoalDelay));
            var assistance = p.GetRawAssistanceArray();
            if (assistance is not { Length: WorkshopPuzzleWire.AssistanceBytes })
                throw new WireFaultException(WireFaultReason.MissingField, "An authored puzzle requires its three assistance profiles.");
            puzzle = new WorkshopPuzzle(new PuzzleId(p.Id ?? throw new WireFaultException(WireFaultReason.MissingField, "Missing puzzle identity.")), (WorkshopPlacementMode)p.Placement, new((Half)p.Precision),
                p.InventoryPart != null ? new PartId(p.InventoryPart) : default, p.InventoryCount, goal,
                WorkshopPuzzleWire.ReadProfile(assistance.AsSpan(0, WorkshopPuzzleWire.ProfileBytes)),
                WorkshopPuzzleWire.ReadProfile(assistance.AsSpan(WorkshopPuzzleWire.ProfileBytes, WorkshopPuzzleWire.ProfileBytes)),
                WorkshopPuzzleWire.ReadProfile(assistance.AsSpan(2 * WorkshopPuzzleWire.ProfileBytes, WorkshopPuzzleWire.ProfileBytes)));
        }

        // No instance or connection limit (Story 9R.5b): the record's own vectors are the population.
        var instances = new IWorkshopInstance[cr.InstancesLength];
        for (var i = 0; i < instances.Length; i++) instances[i] = MapInstance(cr.Instances(i)!.Value);

        var links = new WorkshopConnection[cr.ConnectionsLength];
        for (var i = 0; i < links.Length; i++)
        {
            var conn = cr.Connections(i)!.Value;
            links[i] = new(new(conn.Source), (WorkshopSocket)conn.FromSocket, new(conn.Target), (WorkshopSocket)conn.ToSocket, (WorkshopConnectionDomain)conn.Domain);
        }

        return new WorkshopConstruction(new(cr.Revision), settings, new(instances), puzzle, new(links));
    }

    private static Vec3f Required(Vec3f? value, string field) =>
        value ?? throw new WireFaultException(WireFaultReason.MissingField, $"An instance record requires its {field}.");

    internal static IWorkshopInstance MapInstance(InstanceRecord inst)
    {
        if (string.IsNullOrEmpty(inst.PartId)) throw new WireFaultException(WireFaultReason.MissingField, "An instance record requires its element.");
        var c = inst.Cell ?? throw new WireFaultException(WireFaultReason.MissingField, "An instance record requires its cell.");
        var l = Required(inst.Local, "local position");
        var r = inst.Rotation ?? throw new WireFaultException(WireFaultReason.MissingField, "An instance record requires its rotation.");
        var id = new GpuBodyId(inst.Id);
        var partId = new PartId(inst.PartId);
        var locked = inst.Locked;
        var cell = new CellOrigin(c.X, c.Y, c.Z);
        var local = new LocalPosition(l.X, l.Y, l.Z);
        var rotation = new CanonicalRotation(r.X, r.Y, r.Z, r.W);

        IWorkshopInstance result = partId.Value switch
        {
            "domino" => MapDomino(inst, id, cell, local, rotation, locked),
            "lamp" => new WorkshopLamp(id, cell, local, rotation, locked),
            "latch" => new WorkshopLatch(id, cell, local, rotation, locked),
            // A record without its length decodes the schema's 0, which validation rejects: no default replaces saved data.
            "pipe" => new WorkshopPipe(id, cell, local, rotation, new(inst.Length), locked),
            "ball_detector" => new WorkshopBallDetector(id, cell, local, rotation, locked),
            // Declared settings are decoded as written; construction validation rejects a missing (zero) or out-of-range value.
            "pressure_plate" => new WorkshopPressurePlate(id, cell, local, rotation, inst.Threshold, locked),
            "counter" => new WorkshopCounter(id, cell, local, rotation, TargetCount(inst), locked),
            "clock" => new WorkshopClock(id, cell, local, rotation, inst.Threshold, locked),
            // Every other element is a generic instance of its declared colliders (Story 9A.15: no per-id list); an id with neither
            // a record nor colliders is no element this decoder knows, refused naming it.
            _ => DeclaredPhysics.Contains(partId) && DeclaredPhysics.Of(partId).Colliders.Count > 0
                ? new WorkshopGenericInstance(partId, id, cell, local, rotation, locked) { Parameters = DecodeParameters(inst, partId) }
                : throw new WireFaultException(WireFaultReason.UnknownElement, $"Instance {inst.Id} names unknown physical element '{partId}'.")
        };
        if (result is WorkshopGenericInstance && CarriesTypedField(inst))
            throw new WireFaultException(WireFaultReason.OutOfRange, "Generic instance cannot carry a typed record field.");
        if (result is not WorkshopGenericInstance && inst.ParametersLength != 0)
            throw new WireFaultException(WireFaultReason.OutOfRange, "Typed instance cannot carry generic parameters.");
        return result;
    }

    /// <summary>Whether a record holds any typed record field: a nonzero bit pattern in any scalar or the target count, or a
    /// nonzero half extent. A generic instance carries only its declared parameters, so this refuses it (the save pre-validation
    /// applies the same rule).</summary>
    internal static bool CarriesTypedField(InstanceRecord r)
    {
        static bool Set(float value) => BitConverter.SingleToUInt32Bits(value) != 0;
        var extents = r.HalfExtents;
        return Set(r.Mass) || Set(r.Bounce) || Set(r.Length) || Set(r.Threshold) || Set(r.Friction) || r.TargetCount != 0 ||
            (extents is { } h && (Set(h.X) || Set(h.Y) || Set(h.Z)));
    }

    internal static InstanceParameterCollection DecodeParameters(InstanceRecord record, PartId part)
    {
        try
        {
            var values = new List<KeyValuePair<ParameterId, float>>();
            for (var i = 0; i < record.ParametersLength; i++)
            {
                var parameter = record.Parameters(i) ?? throw new WireFaultException(WireFaultReason.MissingField, "Missing parameter.");
                values.Add(new(new(parameter.Key), parameter.Value));
            }
            var result = new InstanceParameterCollection(values);
            result.Validate(part);
            return result;
        }
        catch (ArgumentException error)
        { throw new WireFaultException(WireFaultReason.OutOfRange, "Invalid declared parameters.", error); }
    }

    /// <summary>The Counter's whole target count: absent is a missing field and one beyond Int32 is out of range, never coerced;
    /// its admitted range is the Counter's own rule.</summary>
    private static int TargetCount(InstanceRecord inst) => inst.TargetCount switch
    {
        0 => throw new WireFaultException(WireFaultReason.MissingField, $"Counter {inst.Id} has no target count."),
        > int.MaxValue => throw new WireFaultException(WireFaultReason.OutOfRange, $"Counter {inst.Id} target count {inst.TargetCount} is not an Int32."),
        var count => (int)count,
    };

    private static WorkshopDomino MapDomino(InstanceRecord inst, GpuBodyId id, CellOrigin cell, LocalPosition local, CanonicalRotation rotation, bool locked)
    {
        var extents = Required(inst.HalfExtents, "half extents");
        return new WorkshopDomino(id, cell, local, rotation,
            new DominoMaterial(new MetreVector((Half)extents.X, (Half)extents.Y, (Half)extents.Z),
                new((Half)inst.Mass), new((Half)inst.Bounce), new((Half)inst.Friction)), locked);
    }
}
