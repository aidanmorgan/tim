using System;
using Google.FlatBuffers;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static class WorkshopWireFlatBuffers
{
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

        VectorOffset? sessionOffset = null;
        if (command.Session != default)
        {
            var sessionBytes = new byte[16];
            WorkshopWire.WriteSession(sessionBytes, command.Session);
            sessionOffset = CuriousContraptions.Wire.WorkshopCommand.CreateSessionVectorBlock(builder, sessionBytes);
        }

        VectorOffset? constructionOffset = null;
        Offset<CuriousContraptions.Wire.ConstructionRecord>? crOffset = null;
        if (command.Construction is { } construction)
        {
            crOffset = EncodeConstructionRecord(builder, construction);
            var constructionBytes = new byte[WorkshopWire.ConstructionBytes(construction)];
            WorkshopWire.WriteConstruction(constructionBytes, construction);
            constructionOffset = CuriousContraptions.Wire.WorkshopCommand.CreateConstructionVectorBlock(builder, constructionBytes);
        }

        CuriousContraptions.Wire.WorkshopCommand.StartWorkshopCommand(builder);
        CuriousContraptions.Wire.WorkshopCommand.AddSequence(builder, command.Sequence.Value);
        CuriousContraptions.Wire.WorkshopCommand.AddKind(builder, (CuriousContraptions.Wire.CommandKind)command.Kind);
        CuriousContraptions.Wire.WorkshopCommand.AddVersion(builder, (uint)WorkshopWireVersion.GenericMechanical);
        CuriousContraptions.Wire.WorkshopCommand.AddEpoch(builder, command.Epoch.Value);
        CuriousContraptions.Wire.WorkshopCommand.AddRevision(builder, command.Revision.Value);
        CuriousContraptions.Wire.WorkshopCommand.AddRevisionKind(builder, (uint)command.RevisionKind);
        if (sessionOffset.HasValue) CuriousContraptions.Wire.WorkshopCommand.AddSession(builder, sessionOffset.Value);
        CuriousContraptions.Wire.WorkshopCommand.AddCadence(builder, command.Cadence.Value);
        CuriousContraptions.Wire.WorkshopCommand.AddProjection(builder, command.Projection.Value);

        if (command.Electrical is { } electrical)
        {
            var electOffset = CuriousContraptions.Wire.ElectricalControl.CreateElectricalControl(
                builder,
                electrical.Owner.Value,
                (byte)electrical.Enabled);
            CuriousContraptions.Wire.WorkshopCommand.AddElectrical(builder, electOffset);
        }

        if (command.Settings is { } settings)
        {
            var setOffset = CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(
                builder,
                (uint)WorkshopSettingsVersion.SharedMaster,
                (uint)settings.Simulation,
                (uint)settings.Physical,
                (uint)settings.Animation,
                (uint)settings.Presentation,
                settings.PresentationRate.Numerator,
                settings.PresentationRate.Denominator);
            CuriousContraptions.Wire.WorkshopCommand.AddSettings(builder, setOffset);
        }

        if (command.Target is { } identity)
        {
            var targetOffset = CuriousContraptions.Wire.CommandIdentityRecord.CreateCommandIdentityRecord(
                builder,
                identity.Sequence.Value,
                identity.Epoch.Value,
                identity.Revision.Value,
                identity.Cadence.Value,
                identity.Projection.Value);
            CuriousContraptions.Wire.WorkshopCommand.AddTarget(builder, targetOffset);
        }

        if (constructionOffset.HasValue)
        {
            CuriousContraptions.Wire.WorkshopCommand.AddConstruction(builder, constructionOffset.Value);
        }
        if (crOffset.HasValue)
        {
            CuriousContraptions.Wire.WorkshopCommand.AddConstructionRecord(builder, crOffset.Value);
        }

        var end = CuriousContraptions.Wire.WorkshopCommand.EndWorkshopCommand(builder);
        builder.Finish(end.Value);
        return builder.SizedByteArray();
    }

    // Decode boundary: a malformed buffer is always rejected with ArgumentException. The FlatBuffers reader itself
    // signals offsets and lengths that run outside the buffer with these runtime exceptions; they are translated
    // here, once, with the reader's exception kept as the cause. WorkshopWireDecodeBoundaryTests fuzzes this.
    public static WorkshopCommand DecodeCommand(ReadOnlySpan<byte> bytes)
    {
        try { return DecodeCommandRecord(bytes); }
        catch (Exception error) when (error is OverflowException or IndexOutOfRangeException)
        { throw new ArgumentException("Malformed Workshop command buffer.", nameof(bytes), error); }
    }

    private static WorkshopCommand DecodeCommandRecord(ReadOnlySpan<byte> bytes)
    {
        var bb = new ByteBuffer(bytes.ToArray());
        var fb = CuriousContraptions.Wire.WorkshopCommand.GetRootAsWorkshopCommand(bb);

        if (fb.Version != (uint)WorkshopWireVersion.GenericMechanical)
            throw new ArgumentException("Unsupported Workshop command schema.");

        var revisionKind = (ExpectedRevisionKind)fb.RevisionKind;
        if (!Enum.IsDefined(revisionKind) ||
            (revisionKind == ExpectedRevisionKind.Any && fb.Revision != 0))
            throw new ArgumentException("Unsupported expected revision.");

        var kind = (WorkshopCommandKind)fb.Kind;
        var sequence = new CommandSequence(fb.Sequence);
        var epoch = new SimulationEpoch(fb.Epoch);
        var revision = new AuthorityRevision(fb.Revision);
        var cadence = new CadenceRevision(fb.Cadence);
        var projection = new ProjectionEpoch(fb.Projection);

        var sessionBytes = fb.GetSessionArray();
        var session = sessionBytes is { Length: 16 } ? WorkshopWire.ReadSession(sessionBytes) : default;

        var hasSettings = kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.ConfigureCadence;
        if ((kind == WorkshopCommandKind.Initialize ? cadence.Value != 0 || projection.Value != 0 : cadence.Value == 0 || projection.Value == 0) ||
            !Enum.IsDefined(kind) || sequence.Value == 0 ||
            (kind is WorkshopCommandKind.Construct or WorkshopCommandKind.Save && revisionKind != ExpectedRevisionKind.Exact) ||
            hasSettings != fb.Settings.HasValue ||
            (kind == WorkshopCommandKind.ConfigureElectrical) != fb.Electrical.HasValue ||
            (kind == WorkshopCommandKind.Construct) != (fb.ConstructionRecord.HasValue || fb.ConstructionLength > 0) ||
            (kind == WorkshopCommandKind.Cancel) != fb.Target.HasValue)
            throw new ArgumentException("Unsupported Workshop command shape.");

        WorkshopConstruction? construction = null;
        if (fb.ConstructionRecord.HasValue)
        {
            construction = DecodeConstructionRecord(fb.ConstructionRecord.Value);
        }
        else if (fb.ConstructionLength > 0)
        {
            construction = WorkshopWire.ReadConstruction(fb.GetConstructionArray());
        }

        WorkshopCommandIdentity? target = null;
        if (fb.Target is { } t)
        {
            target = new WorkshopCommandIdentity(
                new CommandSequence(t.Sequence),
                new SimulationEpoch(t.Epoch),
                new AuthorityRevision(t.Revision),
                new CadenceRevision(t.Cadence),
                new ProjectionEpoch(t.Projection));
            if (target.Value.Sequence.Value == 0 || target.Value.Epoch.Value == 0 || target.Value.Cadence.Value == 0 || target.Value.Projection.Value == 0)
                throw new ArgumentException("Cancel target must have a complete original identity.");
        }

        WorkshopCadenceSettings? settings = null;
        if (fb.Settings is { } s)
        {
            if (s.Version != (uint)WorkshopSettingsVersion.SharedMaster)
                throw new ArgumentException("Unsupported settings shape.");
            settings = new WorkshopCadenceSettings(
                (SimulationCadence)s.Simulation,
                (PhysicalStepProfile)s.Physical,
                (AnimationCadence)s.Animation,
                (PresentationCadence)s.Presentation,
                new PulseRate(s.PresentationRateNumerator, s.PresentationRateDenominator));
            settings.Value.Validate();
        }

        WorkshopElectricalControl? electrical = null;
        if (fb.Electrical is { } e)
        {
            electrical = new WorkshopElectricalControl(
                new GpuBodyId(e.Owner),
                (CuriousContraptions.Gpu.ElectricalEnable)e.Enabled);
            electrical.Value.Validate();
        }

        return new WorkshopCommand(
            sequence,
            kind,
            epoch,
            revision,
            construction,
            target,
            revisionKind,
            session,
            cadence,
            projection,
            settings,
            electrical);
    }

    public static byte[] EncodeResponse(WorkshopResponse response)
    {
        if (!Enum.IsDefined(response.Kind) || !Enum.IsDefined(response.Phase) ||
            !Enum.IsDefined(response.Result.Outcome) || !Enum.IsDefined(response.Result.Reason) ||
            (response.Kind == WorkshopResponseKind.Acknowledgement) != (response.Sequence.Value != 0))
            throw new ArgumentException("Unsupported Workshop response.");
        response.Session.Validate();
        if ((response.Kind == WorkshopResponseKind.Read) != (response.Publication.Value != 0))
            throw new ArgumentException("Invalid physical publication identity.");
        WorkshopWire.ValidateCapture(response.Read);
        WorkshopWire.ValidateScheduleIdentity(response);
        WorkshopWire.ValidatePhysicalRead(response.Read, requireMotion: false);

        var builder = new FlatBufferBuilder(4096);

        // 1. Session vector
        VectorOffset? sessionOffset = null;
        if (response.Session != default)
        {
            var sessionBytes = new byte[16];
            WorkshopWire.WriteSession(sessionBytes, response.Session);
            sessionOffset = CuriousContraptions.Wire.WorkshopResponse.CreateSessionVectorBlock(builder, sessionBytes);
        }

        // 2. Electrical vector
        VectorOffset? electricalOffset = null;
        if (response.Read.Electrical.Count > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartElectricalVector(builder, response.Read.Electrical.Count);
            for (var i = response.Read.Electrical.Count - 1; i >= 0; i--)
            {
                var e = response.Read.Electrical[i];
                CuriousContraptions.Wire.ElectricalSourceRecord.CreateElectricalSourceRecord(
                    builder,
                    e.Id.Value,
                    e.Owner.Value,
                    e.Remaining.Value,
                    e.Debit.Value,
                    (byte)e.Enabled,
                    e.Available);
            }
            electricalOffset = builder.EndVector();
        }

        // 3. ContactWorks vector
        VectorOffset? contactWorksOffset = null;
        if (response.Read.ContactWorks.Count > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartContactWorksVector(builder, response.Read.ContactWorks.Count);
            for (var i = response.Read.ContactWorks.Count - 1; i >= 0; i--)
            {
                var w = response.Read.ContactWorks[i];
                CuriousContraptions.Wire.ContactWorkStoreRecord.CreateContactWorkStoreRecord(
                    builder,
                    w.Id.Value,
                    w.Owner.Value,
                    w.OccurrenceCount,
                    w.RemainingEnergy.Value,
                    w.SuppliedEnergy.Value);
            }
            contactWorksOffset = builder.EndVector();
        }

        // 4. Occurrences vector
        VectorOffset? occurrencesOffset = null;
        if (response.Read.ContactWorks.OccurrenceCount > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartOccurrencesVector(builder, response.Read.ContactWorks.OccurrenceCount);
            for (var i = response.Read.ContactWorks.OccurrenceCount - 1; i >= 0; i--)
            {
                var occ = response.Read.ContactWorks.Occurrence(i);
                CuriousContraptions.Wire.ContactWorkOccurrenceRecord.CreateContactWorkOccurrenceRecord(
                    builder,
                    occ.Work.Value,
                    occ.Collider.Value,
                    occ.Target.Value,
                    occ.Sequence,
                    occ.EventOrdinal,
                    (float)occ.EventPhase,
                    occ.ApproachSpeed.Value,
                    occ.Debit.Value,
                    (byte)occ.Effect);
            }
            occurrencesOffset = builder.EndVector();
        }

        // 5. Bodies vector
        VectorOffset? bodiesOffset = null;
        if (response.Read.Bodies.Count > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartBodiesVector(builder, response.Read.Bodies.Count);
            for (var i = response.Read.Bodies.Count - 1; i >= 0; i--)
            {
                var b = response.Read.Bodies[i];
                CuriousContraptions.Wire.BodyRead.CreateBodyRead(
                    builder,
                    b.Body.Id.Value,
                    b.Body.Cell.X, b.Body.Cell.Y, b.Body.Cell.Z,
                    b.Body.Local.X, b.Body.Local.Y, b.Body.Local.Z,
                    b.Rotation.X, b.Rotation.Y, b.Rotation.Z, b.Rotation.W,
                    (float)b.LocalCentreOfMass.X, (float)b.LocalCentreOfMass.Y, (float)b.LocalCentreOfMass.Z,
                    b.Body.Velocity.X, b.Body.Velocity.Y, b.Body.Velocity.Z,
                    b.AngularVelocity.X, b.AngularVelocity.Y, b.AngularVelocity.Z);
            }
            bodiesOffset = builder.EndVector();
        }

        // 6. Captures vector
        VectorOffset? capturesOffset = null;
        if (response.Read.Captures.Count > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartCapturesVector(builder, response.Read.Captures.Count);
            for (var i = response.Read.Captures.Count - 1; i >= 0; i--)
            {
                var c = response.Read.Captures[i];
                CuriousContraptions.Wire.CaptureLatchRead.CreateCaptureLatchRead(
                    builder,
                    c.Sensor.Value,
                    (uint)c.Phase,
                    c.EventOrdinal,
                    (float)c.EventPhase);
            }
            capturesOffset = builder.EndVector();
        }

        // 7. Activations vector
        VectorOffset? activationsOffset = null;
        if (response.Read.Activations.Count > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartActivationsVector(builder, response.Read.Activations.Count);
            for (var i = response.Read.Activations.Count - 1; i >= 0; i--)
            {
                var a = response.Read.Activations[i];
                CuriousContraptions.Wire.ActivationRecord.CreateActivationRecord(
                    builder,
                    a.Node.Value,
                    a.Owner.Value,
                    a.Source.Value,
                    a.Body.Value,
                    a.Collider.Value,
                    (uint)a.Phase,
                    a.EventOrdinal,
                    (float)a.EventPhase,
                    (float)a.ApproachSpeed.Value,
                    (uint)a.Kind,
                    a.Emitter.Value,
                    a.CauseOrdinal,
                    (float)a.CausePhase);
            }
            activationsOffset = builder.EndVector();
        }

        // 8. Timers vector
        VectorOffset? timersOffset = null;
        if (response.Read.Timers.Count > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartTimersVector(builder, response.Read.Timers.Count);
            for (var i = response.Read.Timers.Count - 1; i >= 0; i--)
            {
                var t = response.Read.Timers[i];
                CuriousContraptions.Wire.TimerRecord.CreateTimerRecord(
                    builder,
                    t.Node.Value,
                    (uint)t.Phase,
                    t.StartedTick,
                    t.DueTick,
                    (uint)t.Input.Kind,
                    t.Input.Time.Ordinal,
                    (float)t.Input.Time.Phase,
                    (float)t.Input.Cause.ApproachSpeed.Value,
                    t.Input.Emitter.Value,
                    t.Input.Cause.Source.Value,
                    t.Input.Cause.Body.Value,
                    t.Input.Cause.Collider.Value,
                    t.Input.Cause.Time.Ordinal,
                    (float)t.Input.Cause.Time.Phase);
            }
            timersOffset = builder.EndVector();
        }

        // 9. Committed counter and clock memory rows (Story 9R.1 D3)
        VectorOffset? memoryOffset = null;
        if (response.Read.Memory.Count > 0)
        {
            CuriousContraptions.Wire.WorkshopResponse.StartActivationMemoryVector(builder, response.Read.Memory.Count);
            for (var i = response.Read.Memory.Count - 1; i >= 0; i--)
            {
                var m = response.Read.Memory[i];
                CuriousContraptions.Wire.ActivationMemoryRecord.CreateActivationMemoryRecord(builder, m.Node.Value, (uint)m.Kind, m.Count,
                    m.Last.Ordinal, (float)m.Last.Phase, (uint)m.Phase, m.DueTick);
            }
            memoryOffset = builder.EndVector();
        }

        // Start WorkshopResponse
        CuriousContraptions.Wire.WorkshopResponse.StartWorkshopResponse(builder);
        CuriousContraptions.Wire.WorkshopResponse.AddSequence(builder, response.Sequence.Value);
        CuriousContraptions.Wire.WorkshopResponse.AddKind(builder, (CuriousContraptions.Wire.ResponseKind)response.Kind);
        CuriousContraptions.Wire.WorkshopResponse.AddOutcome(builder, (CuriousContraptions.Wire.CommandOutcome)response.Result.Outcome);
        CuriousContraptions.Wire.WorkshopResponse.AddReason(builder, (byte)response.Result.Reason);
        CuriousContraptions.Wire.WorkshopResponse.AddPhase(builder, (CuriousContraptions.Wire.SimulationPhase)response.Phase);
        CuriousContraptions.Wire.WorkshopResponse.AddVersion(builder, (uint)WorkshopWireVersion.GenericMechanical);
        CuriousContraptions.Wire.WorkshopResponse.AddEpoch(builder, response.Read.Epoch.Value);
        CuriousContraptions.Wire.WorkshopResponse.AddTick(builder, response.Read.Tick.Value);
        CuriousContraptions.Wire.WorkshopResponse.AddRevision(builder, response.Read.Revision.Value);
        CuriousContraptions.Wire.WorkshopResponse.AddPublication(builder, response.Publication.Value);
        CuriousContraptions.Wire.WorkshopResponse.AddMasterGeneration(builder, response.MasterGeneration.Value);
        CuriousContraptions.Wire.WorkshopResponse.AddCadence(builder, response.Cadence.Value);
        CuriousContraptions.Wire.WorkshopResponse.AddProjection(builder, response.Projection.Value);

        if (sessionOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddSession(builder, sessionOffset.Value);
        if (response.Read.Capture is { } capture)
        {
            var clockOffset = CuriousContraptions.Wire.ClockStamp.CreateClockStamp(
                builder,
                capture.Time.Value,
                capture.Uncertainty.Value,
                (byte)capture.Domain);
            CuriousContraptions.Wire.WorkshopResponse.AddCapture(builder, clockOffset);
        }

        if (bodiesOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddBodies(builder, bodiesOffset.Value);
        if (capturesOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddCaptures(builder, capturesOffset.Value);
        if (activationsOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddActivations(builder, activationsOffset.Value);
        if (timersOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddTimers(builder, timersOffset.Value);
        if (contactWorksOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddContactWorks(builder, contactWorksOffset.Value);
        if (occurrencesOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddOccurrences(builder, occurrencesOffset.Value);
        if (electricalOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddElectrical(builder, electricalOffset.Value);
        if (memoryOffset.HasValue) CuriousContraptions.Wire.WorkshopResponse.AddActivationMemory(builder, memoryOffset.Value);

        var end = CuriousContraptions.Wire.WorkshopResponse.EndWorkshopResponse(builder);
        CuriousContraptions.Wire.WorkshopResponse.FinishWorkshopResponseBuffer(builder, end);
        return builder.SizedByteArray();
    }

    public static WorkshopResponse DecodeResponse(ReadOnlySpan<byte> bytes)
    {
        try { return DecodeResponseRecord(bytes); }
        catch (Exception error) when (error is OverflowException or IndexOutOfRangeException)
        { throw new ArgumentException("Malformed Workshop response buffer.", nameof(bytes), error); }
    }

    private static WorkshopResponse DecodeResponseRecord(ReadOnlySpan<byte> bytes)
    {
        var bb = new ByteBuffer(bytes.ToArray());
        var fb = CuriousContraptions.Wire.WorkshopResponse.GetRootAsWorkshopResponse(bb);

        if (fb.Version != (uint)WorkshopWireVersion.GenericMechanical)
            throw new ArgumentException("Unsupported Workshop response schema.");

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
        var revision = new AuthorityRevision(fb.Revision);
        var publication = new PublicationSequence(fb.Publication);
        var master = new ClockGeneration(fb.MasterGeneration);
        var cadence = new CadenceRevision(fb.Cadence);
        var projection = new ProjectionEpoch(fb.Projection);

        var sessionBytes = fb.GetSessionArray();
        var session = sessionBytes is { Length: 16 } ? WorkshopWire.ReadSession(sessionBytes) : default;

        WorkshopClockStamp? capture = null;
        if (fb.Capture is { } c)
        {
            capture = new WorkshopClockStamp(
                (WorkshopClockDomain)c.Domain,
                master,
                new MonotonicNanoseconds(c.Time),
                new TimestampUncertainty(c.Uncertainty));
        }

        var bodyCount = fb.BodiesLength;
        var bodyValues = new PhysicsBodyRead[bodyCount];
        for (var i = 0; i < bodyCount; i++)
        {
            var b = fb.Bodies(i)!.Value;
            var body = new CanonicalBody(
                new GpuBodyId(b.Id), epoch.Value, tick.Value,
                new CellOrigin(b.Cell.X, b.Cell.Y, b.Cell.Z),
                new LocalPosition(b.Local.X, b.Local.Y, b.Local.Z),
                new LinearVelocity(b.Velocity.X, b.Velocity.Y, b.Velocity.Z));
            bodyValues[i] = new PhysicsBodyRead(
                body,
                new CanonicalRotation(b.Rotation.X, b.Rotation.Y, b.Rotation.Z, b.Rotation.W),
                new AngularVelocity(b.AngularVelocity.X, b.AngularVelocity.Y, b.AngularVelocity.Z),
                new MetreVector((Half)b.Com.X, (Half)b.Com.Y, (Half)b.Com.Z));
        }
        var bodies = new PhysicsBodyReadSet(bodyValues);

        var captureCount = fb.CapturesLength;
        var latches = new CaptureLatch[captureCount];
        for (var i = 0; i < captureCount; i++)
        {
            var cl = fb.Captures(i)!.Value;
            latches[i] = new CaptureLatch(
                new GpuSensorId(cl.Sensor),
                (CaptureLatchPhase)cl.Phase,
                cl.EventOrdinal,
                (Half)cl.EventPhase);
        }
        var captures = new PhysicsCaptureRead(latches);

        var actCount = fb.ActivationsLength;
        var actValues = new ActivationLatch[actCount];
        for (var i = 0; i < actCount; i++)
        {
            var a = fb.Activations(i)!.Value;
            actValues[i] = new ActivationLatch(
                new ActivationNodeId(a.Node),
                new GpuBodyId(a.Owner),
                (ActivationPhase)a.Phase,
                new GpuEventSourceId(a.Source),
                new GpuBodyId(a.Body),
                new GpuColliderId(a.Collider),
                a.EventOrdinal,
                (Half)a.EventPhase,
                new LinearSpeed((Half)a.ApproachSpeed),
                (ActivationOccurrenceKind)a.Kind,
                new ActivationNodeId(a.Emitter),
                a.CauseOrdinal,
                (Half)a.CausePhase);
        }
        var activations = new PhysicsActivationRead(actValues);

        var timerCount = fb.TimersLength;
        var timerValues = new ActivationTimerState[timerCount];
        for (var i = 0; i < timerCount; i++)
        {
            var t = fb.Timers(i)!.Value;
            var tPhase = (ActivationTimerPhase)t.Phase;
            ActivationOccurrence input = default;
            if (tPhase != ActivationTimerPhase.Ready)
            {
                input = new ActivationOccurrence(
                    (ActivationOccurrenceKind)t.InputKind,
                    new ActivationNodeId(t.InputEmitter),
                    new ActivationTime(t.InputOrdinal, (Half)t.InputPhase),
                    new ActivationCause(
                        new GpuEventSourceId(t.CauseSource),
                        new GpuBodyId(t.CauseBody),
                        new GpuColliderId(t.CauseCollider),
                        new ActivationTime(t.CauseOrdinal, (Half)t.CausePhase),
                        new LinearSpeed((Half)t.InputApproachSpeed)));
            }
            timerValues[i] = new ActivationTimerState(
                new ActivationNodeId(t.Node),
                tPhase,
                t.StartedTick,
                t.DueTick,
                input);
        }
        var timers = new PhysicsTimerRead(timerValues);

        // The record's own vector lengths (Story 9R.5b: never narrowed; a byte cast here truncated past 255 occurrences).
        var storeCount = fb.ContactWorksLength;
        var occCount = fb.OccurrencesLength;
        var stores = new ContactWorkRead[storeCount];
        for (var i = 0; i < storeCount; i++)
        {
            var s = fb.ContactWorks(i)!.Value;
            stores[i] = new ContactWorkRead(
                new GpuContactWorkId(s.Id),
                new GpuBodyId(s.Owner),
                s.OccurrenceCount,
                new Joules(s.RemainingEnergy),
                new Joules(s.SuppliedEnergy));
        }
        var occurrences = new ContactWorkOccurrence[occCount];
        for (var i = 0; i < occCount; i++)
        {
            var occ = fb.Occurrences(i)!.Value;
            occurrences[i] = new ContactWorkOccurrence(
                new ContactWorkSlot(occ.Work),
                new ColliderSlot(occ.Collider),
                new GpuBodyId(occ.Target),
                occ.Sequence,
                occ.EventOrdinal,
                (Half)occ.EventPhase,
                new ContactSpeed(occ.ApproachSpeed),
                new Joules(occ.Debit),
                (ContactWorkEffect)occ.Effect);
        }
        var contactWorks = new PhysicsContactWorkRead(stores, occurrences);

        var electricalCount = fb.ElectricalLength;
        var sources = new ElectricalSourceRead[electricalCount];
        for (var i = 0; i < electricalCount; i++)
        {
            var e = fb.Electrical(i)!.Value;
            sources[i] = new ElectricalSourceRead(
                new ElectricalSourceId(e.Id),
                new GpuBodyId(e.Owner),
                new Joules(e.Remaining),
                new Joules(e.Debit),
                (CuriousContraptions.Gpu.ElectricalEnable)e.Enabled);
        }
        var electrical = new PhysicsElectricalRead(sources);

        var memoryCount = fb.ActivationMemoryLength;
        var memoryValues = new ActivationMemory[memoryCount];
        for (var i = 0; i < memoryCount; i++)
        {
            var m = fb.ActivationMemory(i)!.Value;
            memoryValues[i] = new ActivationMemory(new ActivationNodeId(m.Node), (ActivationNodeKind)m.Kind, m.Count,
                new ActivationTime(m.LastOrdinal, (Half)m.LastPhase), (OscillatorPhase)m.Phase, m.DueTick);
        }
        var memory = new PhysicsMemoryRead(memoryValues);

        PhysicsMotionRead? motion = tick.Value == 0
            ? PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength(bodies.Count)], bodies, tick)
            : null;
        var read = new WorkshopRead(
            epoch,
            tick,
            bodies,
            revision,
            capture,
            captures,
            motion,
            activations,
            timers,
            contactWorks,
            electrical,
            memory);

        WorkshopWire.ValidatePhysicalRead(read, requireMotion: false);
        WorkshopWire.ValidateCapture(read);

        var response = new WorkshopResponse(
            sequence,
            kind,
            new WorkshopCommandResult(outcome, reason),
            phase,
            read,
            session,
            publication,
            cadence,
            master,
            projection,
            new PulseOrdinal(tick.Value));

        WorkshopWire.ValidateScheduleIdentity(response);
        return response;
    }

    public static Offset<CuriousContraptions.Wire.ConstructionRecord> EncodeConstructionRecord(FlatBufferBuilder builder, WorkshopConstruction construction, bool validate = true)
    {
        if (validate) construction.Validate();

        Offset<CuriousContraptions.Wire.PuzzleRecord>? puzzleOffset = null;
        if (construction.Puzzle.Id != WorkshopPuzzleId.Free)
        {
            var puzzle = construction.Puzzle;
            var invPartOffset = puzzle.InventoryPart.Value != null ? builder.CreateString(puzzle.InventoryPart.Value) : default;

            var assistanceBytes = new byte[198];
            WorkshopPuzzleWire.WriteProfile(assistanceBytes.AsSpan(0, 66), puzzle.BallAssistance);
            WorkshopPuzzleWire.WriteProfile(assistanceBytes.AsSpan(66, 66), puzzle.ReceiverAssistance);
            WorkshopPuzzleWire.WriteProfile(assistanceBytes.AsSpan(132, 66), puzzle.RampAssistance);
            var assistVec = CuriousContraptions.Wire.PuzzleRecord.CreateRawAssistanceVector(builder, assistanceBytes);

            var goalBody = puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.SourceNode.Value : puzzle.Goal.Body.Value;
            var goalTarget = puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.TargetNode.Value : puzzle.Goal.Target.Value;

            CuriousContraptions.Wire.PuzzleRecord.StartPuzzleRecord(builder);
            CuriousContraptions.Wire.PuzzleRecord.AddId(builder, (uint)puzzle.Id);
            CuriousContraptions.Wire.PuzzleRecord.AddPlacement(builder, (uint)puzzle.Placement);
            CuriousContraptions.Wire.PuzzleRecord.AddPrecision(builder, (float)puzzle.Precision.Value);
            if (puzzle.InventoryPart.Value != null) CuriousContraptions.Wire.PuzzleRecord.AddInventoryPart(builder, invPartOffset);
            CuriousContraptions.Wire.PuzzleRecord.AddInventoryCount(builder, puzzle.InventoryCount);
            CuriousContraptions.Wire.PuzzleRecord.AddGoalKind(builder, (uint)puzzle.Goal.Kind);
            CuriousContraptions.Wire.PuzzleRecord.AddGoalBody(builder, goalBody);
            CuriousContraptions.Wire.PuzzleRecord.AddGoalTarget(builder, goalTarget);
            CuriousContraptions.Wire.PuzzleRecord.AddGoalEventSource(builder, puzzle.Goal.EventSource.Value);
            CuriousContraptions.Wire.PuzzleRecord.AddGoalDelay(builder, (float)puzzle.Goal.MinimumDelay.Value);
            CuriousContraptions.Wire.PuzzleRecord.AddRawAssistance(builder, assistVec);
            puzzleOffset = CuriousContraptions.Wire.PuzzleRecord.EndPuzzleRecord(builder);
        }

        var instanceOffsets = new Offset<CuriousContraptions.Wire.InstanceRecord>[construction.Instances.Count];
        for (var i = 0; i < construction.Instances.Count; i++)
        {
            var instance = construction.Instances[i];
            var partIdOffset = builder.CreateString(instance.PartId.Value);

            CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
            CuriousContraptions.Wire.InstanceRecord.AddId(builder, instance.Id.Value);
            CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, partIdOffset);
            CuriousContraptions.Wire.InstanceRecord.AddLocked(builder, instance.Locked);
            CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, instance.Cell.X, instance.Cell.Y, instance.Cell.Z));
            CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, instance.Local.X, instance.Local.Y, instance.Local.Z));
            CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W));

            switch (instance)
            {
                case WorkshopBall ball:
                    CuriousContraptions.Wire.InstanceRecord.AddRadius(builder, (float)ball.Material.Radius.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddMass(builder, (float)ball.Material.Mass.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddBounce(builder, (float)ball.Material.Bounce.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddDrag(builder, (float)ball.Material.Drag.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddBuoyancy(builder, (float)ball.Material.Buoyancy.Value);
                    break;
                case WorkshopReceiver receiver:
                    CuriousContraptions.Wire.InstanceRecord.AddMargin(builder, (float)receiver.Capture.Margin.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddSpeedLimit(builder, (float)receiver.Capture.SpeedLimit.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddDwell(builder, (float)receiver.Capture.Dwell.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddParticipation(builder, (uint)receiver.Capture.Participation);
                    CuriousContraptions.Wire.InstanceRecord.AddForceMin(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, (float)receiver.ForceRegion.Minimum.X, (float)receiver.ForceRegion.Minimum.Y, (float)receiver.ForceRegion.Minimum.Z));
                    CuriousContraptions.Wire.InstanceRecord.AddForceMax(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, (float)receiver.ForceRegion.Maximum.X, (float)receiver.ForceRegion.Maximum.Y, (float)receiver.ForceRegion.Maximum.Z));
                    CuriousContraptions.Wire.InstanceRecord.AddSupportHeight(builder, (float)receiver.ForceRegion.SupportHeight.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddSupportMargin(builder, (float)receiver.ForceRegion.SupportMargin.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddMaxAcceleration(builder, (float)receiver.ForceRegion.MaximumAcceleration.Value);
                    break;
                case WorkshopRamp ramp:
                    CuriousContraptions.Wire.InstanceRecord.AddLength(builder, (float)ramp.Dimensions.Length.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddWidth(builder, (float)ramp.Dimensions.Width.Value);
                    break;
                case WorkshopWall wall:
                    CuriousContraptions.Wire.InstanceRecord.AddWidth(builder, (float)wall.Dimensions.Width.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddHeight(builder, (float)wall.Dimensions.Height.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddThickness(builder, (float)wall.Dimensions.Thickness.Value);
                    break;
                case WorkshopSwitch trigger:
                    CuriousContraptions.Wire.InstanceRecord.AddThreshold(builder, (float)trigger.Trigger.Threshold.Value);
                    break;
                case WorkshopDelay delay:
                    CuriousContraptions.Wire.InstanceRecord.AddDuration(builder, (float)delay.Duration.Seconds.Value);
                    break;
                case WorkshopSpringboard spring:
                    CuriousContraptions.Wire.InstanceRecord.AddStiffness(builder, spring.Settings.Stiffness);
                    CuriousContraptions.Wire.InstanceRecord.AddDamping(builder, spring.Settings.Damping);
                    CuriousContraptions.Wire.InstanceRecord.AddInitialCompression(builder, spring.Settings.InitialCompression);
                    break;
                case WorkshopBattery battery:
                    CuriousContraptions.Wire.InstanceRecord.AddCapacity(builder, battery.Settings.Capacity.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddMaximumPower(builder, battery.Settings.MaximumPower.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddInitialFraction(builder, battery.Settings.InitialFraction);
                    CuriousContraptions.Wire.InstanceRecord.AddEnabled(builder, (byte)battery.Settings.Enabled);
                    break;
                case WorkshopBumper bumper:
                    CuriousContraptions.Wire.InstanceRecord.AddStrength(builder, bumper.Work.Strength.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddReferenceMass(builder, bumper.Work.ReferenceMass.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddPreload(builder, bumper.Work.Preload.Value);
                    break;
                case WorkshopDomino domino:
                    CuriousContraptions.Wire.InstanceRecord.AddHalfExtents(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, (float)domino.Material.HalfExtents.X, (float)domino.Material.HalfExtents.Y, (float)domino.Material.HalfExtents.Z));
                    CuriousContraptions.Wire.InstanceRecord.AddMass(builder, (float)domino.Material.Mass.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddBounce(builder, (float)domino.Material.Bounce.Value);
                    CuriousContraptions.Wire.InstanceRecord.AddFriction(builder, (float)domino.Material.Friction.Value);
                    break;
                case WorkshopPipe pipe:
                    CuriousContraptions.Wire.InstanceRecord.AddLength(builder, pipe.Length.Metres);
                    break;
                case WorkshopLamp:
                case WorkshopLatch:
                case IWorkshopDeclaredColliders:
                    break;
                case WorkshopPressurePlate plate:
                    CuriousContraptions.Wire.InstanceRecord.AddThreshold(builder, plate.MinimumMass);
                    break;
                case WorkshopCounter counter:
                    CuriousContraptions.Wire.InstanceRecord.AddThreshold(builder, (float)counter.TargetCount);
                    break;
                case WorkshopClock clock:
                    CuriousContraptions.Wire.InstanceRecord.AddThreshold(builder, clock.IntervalSeconds);
                    break;
                default:
                    throw new ArgumentException("Unsupported instance declaration.");
            }

            instanceOffsets[i] = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        }
        var instancesVector = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, instanceOffsets);

        VectorOffset? connectionsVector = null;
        if (construction.Connections.Count > 0)
        {
            CuriousContraptions.Wire.ConstructionRecord.StartConnectionsVector(builder, construction.Connections.Count);
            for (var i = construction.Connections.Count - 1; i >= 0; i--)
            {
                var link = construction.Connections[i];
                CuriousContraptions.Wire.ConnectionRecord.CreateConnectionRecord(
                    builder,
                    link.Source.Value,
                    link.Target.Value,
                    (uint)link.Domain,
                    (uint)link.Output,
                    (uint)link.Input);
            }
            connectionsVector = builder.EndVector();
        }

        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, construction.Revision.Value);
        var settingsOffset = CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(
            builder,
            (uint)WorkshopSettingsVersion.SharedMaster,
            (uint)construction.Settings.Simulation,
            (uint)construction.Settings.Physical,
            (uint)construction.Settings.Animation,
            (uint)construction.Settings.Presentation,
            construction.Settings.PresentationRate.Numerator,
            construction.Settings.PresentationRate.Denominator);
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, settingsOffset);
        if (puzzleOffset.HasValue) CuriousContraptions.Wire.ConstructionRecord.AddPuzzle(builder, puzzleOffset.Value);
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instancesVector);
        if (connectionsVector.HasValue) CuriousContraptions.Wire.ConstructionRecord.AddConnections(builder, connectionsVector.Value);
        return CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
    }

    // A float compares with int.MaxValue as 2^31, so the exclusive f32 bound below is the exact Int32 range (Story 9.4b).
    private static int WholeCount(float value, GpuBodyId id) =>
        float.IsFinite(value) && value == MathF.Round(value) && value >= -2147483648f && value < 2147483648f
            ? (int)value
            : throw new ArgumentException($"Counter {id.Value} target count '{value:R}' must be a whole number within Int32.");

    public static WorkshopConstruction DecodeConstructionRecord(CuriousContraptions.Wire.ConstructionRecord cr)
    {
        var revision = new ConstructionRevision(cr.Revision);
        if (revision.Value == 0) throw new ArgumentException("Construction revision must be nonzero.");

        if (!cr.Settings.HasValue) throw new ArgumentException("Missing settings record.");
        var s = cr.Settings.Value;
        if (s.Version != (uint)WorkshopSettingsVersion.SharedMaster) throw new ArgumentException("Unsupported settings version.");
        var settings = new WorkshopCadenceSettings(
            (SimulationCadence)s.Simulation,
            (PhysicalStepProfile)s.Physical,
            (AnimationCadence)s.Animation,
            (PresentationCadence)s.Presentation,
            new PulseRate(s.PresentationRateNumerator, s.PresentationRateDenominator));

        WorkshopPuzzle puzzle = default;
        if (cr.Puzzle.HasValue)
        {
            var p = cr.Puzzle.Value;
            var pId = (WorkshopPuzzleId)p.Id;
            if (pId != WorkshopPuzzleId.Free)
            {
                var kind = (WorkshopGoalKind)p.GoalKind;
                var sensor = new GpuSensorId(p.GoalEventSource);
                var goal = kind == WorkshopGoalKind.ActivatedAfter
                    ? new WorkshopGoal(kind, default, default, sensor, new(p.GoalBody), new(p.GoalTarget), new((Half)p.GoalDelay))
                    : new WorkshopGoal(kind, new(p.GoalBody), new(p.GoalTarget), sensor, MinimumDelay: new((Half)p.GoalDelay));
                goal.Validate();

                var assistLength = p.RawAssistanceLength;
                var assistBytes = new byte[assistLength];
                for (var i = 0; i < assistLength; i++) assistBytes[i] = p.RawAssistance(i);
                var ballAssistance = assistLength >= 66 ? WorkshopPuzzleWire.ReadProfile(assistBytes.AsSpan(0, 66)) : default;
                var receiverAssistance = assistLength >= 132 ? WorkshopPuzzleWire.ReadProfile(assistBytes.AsSpan(66, 66)) : default;
                var rampAssistance = assistLength >= 198 ? WorkshopPuzzleWire.ReadProfile(assistBytes.AsSpan(132, 66)) : default;

                puzzle = new WorkshopPuzzle(
                    pId,
                    (WorkshopPlacementMode)p.Placement,
                    new((Half)p.Precision),
                    p.InventoryPart != null ? new PartId(p.InventoryPart) : default,
                    p.InventoryCount,
                    goal,
                    ballAssistance,
                    receiverAssistance,
                    rampAssistance);
            }
        }

        // No instance or connection limit (Story 9R.5b): the record's own vectors are the population.
        var instCount = cr.InstancesLength;
        var instances = new IWorkshopInstance[instCount];
        for (var i = 0; i < instCount; i++)
        {
            var inst = cr.Instances(i)!.Value;
            if (!inst.Cell.HasValue || !inst.Local.HasValue || !inst.Rotation.HasValue || inst.PartId == null)
                throw new ArgumentException("Invalid authored instance flags.");
            var id = new GpuBodyId(inst.Id);
            var partId = new PartId(inst.PartId);
            var locked = inst.Locked;
            var cell = new CellOrigin(inst.Cell.Value.X, inst.Cell.Value.Y, inst.Cell.Value.Z);
            var local = new LocalPosition(inst.Local.Value.X, inst.Local.Value.Y, inst.Local.Value.Z);
            var rotation = new CanonicalRotation(inst.Rotation.Value.X, inst.Rotation.Value.Y, inst.Rotation.Value.Z, inst.Rotation.Value.W);
            rotation.Validate();

            instances[i] = partId.Value switch
            {
                "ball" or "bowling" => new WorkshopBall(id, partId, cell, local, rotation,
                    BallMaterial.For(partId) with {
                        Radius = new((Half)inst.Radius),
                        Mass = new((Half)inst.Mass),
                        Bounce = new((Half)inst.Bounce),
                        Drag = new((Half)inst.Drag),
                        Buoyancy = new((Half)inst.Buoyancy)
                    }, locked),
                "basket" => new WorkshopReceiver(id, cell, local, rotation,
                    new(new((Half)inst.Margin), new((Half)inst.SpeedLimit), new((Half)inst.Dwell), (SensorParticipation)inst.Participation),
                    locked)
                {
                    ForceRegion = new(
                        new MetreVector((Half)(inst.ForceMin?.X ?? throw new ArgumentException("Missing force min.")), (Half)inst.ForceMin.Value.Y, (Half)inst.ForceMin.Value.Z),
                        new MetreVector((Half)(inst.ForceMax?.X ?? throw new ArgumentException("Missing force max.")), (Half)inst.ForceMax.Value.Y, (Half)inst.ForceMax.Value.Z),
                        new((Half)inst.SupportHeight),
                        new((Half)inst.SupportMargin),
                        new((Half)inst.MaxAcceleration))
                },
                "ramp" => new WorkshopRamp(id, cell, local, rotation,
                    new(new((Half)inst.Length), new((Half)inst.Width)), locked),
                "wall" => new WorkshopWall(id, cell, local, rotation,
                    new(new((Half)inst.Width), new((Half)inst.Height), new((Half)inst.Thickness)), locked),
                "switch" => new WorkshopSwitch(id, cell, local, rotation,
                    new ContactTriggerSettings(new LinearSpeed((Half)inst.Threshold)), locked),
                "delay" => new WorkshopDelay(id, cell, local, rotation,
                    new DelayDuration(new DurationSeconds((Half)inst.Duration)), locked),
                "spring" => new WorkshopSpringboard(id, cell, local, rotation,
                    new SpringboardSettings(inst.Stiffness, inst.Damping, inst.InitialCompression), locked),
                "battery" => new WorkshopBattery(id, cell, local, rotation,
                    new ElectricalSourceSettings(new Joules(inst.Capacity), new Watts(inst.MaximumPower), inst.InitialFraction, (ElectricalEnable)inst.Enabled), locked),
                "bumper" => new WorkshopBumper(id, cell, local, rotation,
                    new BumperWork(new ContactSpeed(inst.Strength), new WorkMass(inst.ReferenceMass), new Joules(inst.Preload)), locked),
                "domino" => new WorkshopDomino(id, cell, local, rotation,
                    new DominoMaterial(new MetreVector((Half)(inst.HalfExtents?.X ?? throw new ArgumentException("Missing half extents.")), (Half)inst.HalfExtents.Value.Y, (Half)inst.HalfExtents.Value.Z),
                        new((Half)inst.Mass), new((Half)inst.Bounce), new((Half)inst.Friction)), locked),
                "lamp" => new WorkshopLamp(id, cell, local, rotation, locked),
                "latch" => new WorkshopLatch(id, cell, local, rotation, locked),
                // A record without its length decodes the schema's 0, which validation rejects: no default replaces saved data.
                "pipe" => new WorkshopPipe(id, cell, local, rotation, new(inst.Length), locked),
                "pipe_bend_45" or "pipe_bend_90" or "funnel" or "powered_gate" => new WorkshopGenericInstance(partId, id, cell, local, rotation,
                    WorkshopCanonicalColliders.For(partId), locked),
                "ball_detector" => new WorkshopBallDetector(id, cell, local, rotation,
                    WorkshopCanonicalColliders.BallDetectorColliders, locked),
                // Declared settings are decoded as written; construction validation rejects a missing (zero) or
                // out-of-range value, and a target count must be a whole number. No default replaces saved data.
                "pressure_plate" => new WorkshopPressurePlate(id, cell, local, rotation, inst.Threshold, locked),
                "counter" => new WorkshopCounter(id, cell, local, rotation, WholeCount(inst.Threshold, id), locked),
                "clock" => new WorkshopClock(id, cell, local, rotation, inst.Threshold, locked),
                _ => throw new ArgumentException("Unsupported instance kind or parameters.")
            };
        }

        var linkCount = cr.ConnectionsLength;
        var links = new WorkshopConnection[linkCount];
        for (var i = 0; i < linkCount; i++)
        {
            var conn = cr.Connections(i)!.Value;
            links[i] = new(new(conn.Source), (WorkshopSocket)conn.FromSocket, new(conn.Target), (WorkshopSocket)conn.ToSocket, (WorkshopConnectionDomain)conn.Domain);
        }

        var construction = new WorkshopConstruction(revision, settings, new(instances), puzzle, new(links));
        construction.Validate();
        return construction;
    }
}
