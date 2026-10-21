using System;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class WorkshopWireFlatBuffersTests
{
    private static readonly RuntimeSessionId Session = new(0x1122334455667788UL, 0x8877665544332211UL);

    [Fact]
    public void Command_SimpleRunAndReset_RoundTrips()
    {
        var run = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(10), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(2), Projection: new(3));
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(run);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(run.Sequence, decoded.Sequence);
        Assert.Equal(run.Kind, decoded.Kind);
        Assert.Equal(run.Epoch, decoded.Epoch);
        Assert.Equal(run.Revision, decoded.Revision);
        Assert.Equal(run.RevisionKind, decoded.RevisionKind);
        Assert.Equal(run.Session, decoded.Session);
        Assert.Equal(run.Cadence, decoded.Cadence);
        Assert.Equal(run.Projection, decoded.Projection);
        Assert.Null(decoded.Construction);
        Assert.Null(decoded.Settings);
        Assert.Null(decoded.Target);
        Assert.Null(decoded.Electrical);
    }

    [Fact]
    public void Command_InitializeWithSettings_RoundTrips()
    {
        var settings = WorkshopCadenceSettings.Default();
        var init = new WorkshopCommand(new(1), WorkshopCommandKind.Initialize, new(1), new(1), null,
            Session: Session, Settings: settings);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(init);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(init.Sequence, decoded.Sequence);
        Assert.Equal(init.Kind, decoded.Kind);
        Assert.Equal(settings, decoded.Settings);
    }

    [Fact]
    public void Command_ConfigureCadence_RoundTrips()
    {
        var settings = new WorkshopCadenceSettings(
            SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz,
            AnimationCadence.Hz30, PresentationCadence.Hz60, new(60, 1));
        var cmd = new WorkshopCommand(new(2), WorkshopCommandKind.ConfigureCadence, new(1), new(5), null,
            Session: Session, Cadence: new(1), Projection: new(1), Settings: settings);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.Equal(settings, decoded.Settings);
    }

    [Fact]
    public void Command_ConfigureElectrical_RoundTrips()
    {
        var electrical = new WorkshopElectricalControl(new(42), ElectricalEnable.Enabled);
        var cmd = new WorkshopCommand(new(3), WorkshopCommandKind.ConfigureElectrical, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1),
            Electrical: electrical);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.Equal(electrical, decoded.Electrical);
    }

    [Fact]
    public void Command_CancelWithTarget_RoundTrips()
    {
        var target = new WorkshopCommandIdentity(new(5), new(1), new(2), new(3), new(4));
        var cmd = new WorkshopCommand(new(6), WorkshopCommandKind.Cancel, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1),
            Target: target);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.Equal(target, decoded.Target);
    }

    [Fact]
    public void Command_ConstructWithInstances_RoundTrips()
    {
        var ball = WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([ball]));
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, construction,
            Session: Session, Cadence: new(1), Projection: new(1));
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.NotNull(decoded.Construction);
        Assert.Equal(construction.Revision, decoded.Construction!.Value.Revision);
        Assert.Equal(construction.Instances.Count, decoded.Construction!.Value.Instances.Count);
        Assert.Equal(ball.Id, decoded.Construction!.Value.Instances[0].Id);
    }

    private static byte[] EncodeConstruction(WorkshopConstruction construction)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(1024);
        builder.Finish(WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, construction).Value);
        return builder.SizedByteArray();
    }

    private static WorkshopConstruction DecodeConstruction(byte[] bytes) =>
        WorkshopWireFlatBuffers.DecodeConstructionRecord(
            CuriousContraptions.Wire.ConstructionRecord.GetRootAsConstructionRecord(new Google.FlatBuffers.ByteBuffer(bytes)));

    /// <summary>One instance of every encodable kind, each with non-default declared settings where it has any.</summary>
    public static TheoryData<IWorkshopInstance> EveryInstanceKind() => new()
    {
        WorkshopInput.Basketball(new(1), -1.25, 3.5, .5, 0, 0, 0, 1) with { Locked = true },
        WorkshopInput.Ball(PartId.BowlingBall, new(1), 2, 1, -.5, 0, .7071068, 0, .7071068),
        WorkshopInput.Receiver(new(1), -.7, .55, 0, 0, 0, 0, 1) with
            { Capture = new(new((Half).05), new((Half)2.25), new((Half).24), SensorParticipation.Enabled) },
        WorkshopInput.Ramp(new(1), 1, 2, 0, 0, 0, .258819, .9659258, new(new((Half)4), new((Half)1.5))),
        WorkshopInput.Generic(PartId.Wall, new(1), -1, 4, 0, 0, 0, 0, 1, WallFixture.Values(.4f, 6, 1.5f)),
        WorkshopInput.Switch(new(1), 0, 1, 0, 0, 0, 0, 1, new(new((Half)1.25))),
        WorkshopInput.Generic(PartId.Delay, new(1), 0, 1, 0, 0, 0, 0, 1, new InstanceParameterCollection([new(new ParameterId("delay_seconds"), (float)2.5)])),
        WorkshopInput.Springboard(new(1), 0, .2, 0, 0, 0, 0, 1, new SpringboardSettings(400f, .2f, .2f)),
        WorkshopInput.Battery(new(1), 0, 1, 0, 0, 0, 0, 1, new(new(60f), new(30f), .5f, ElectricalEnable.Disabled)),
        WorkshopInput.Bumper(new(1), 0, 1, 0, 0, 0, 0, 1, BumperWork.FromCanonicalStrength(6f)),
        WorkshopInput.Domino(new(1), .1, .09, 0, 0, 0, 0, 1),
        WorkshopInput.Lamp(new(1), 3, 1, 0, 0, 0, 0, 1),
        WorkshopInput.Latch(new(1), 0, 1, 0, 0, 0, 0, 1, locked: true),
        WorkshopInput.Pipe(new(1), 0, 1, 0, 0, 0, 0, 1),
        WorkshopInput.PipeBend45(new(1), 0, 1, 0, 0, 0, 0, 1),
        WorkshopInput.PipeBend90(new(1), 0, 1, 0, 0, 0, 0, 1),
        WorkshopInput.Funnel(new(1), 0, 1, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 1, 0, 0, 0, 0, 1),
        WorkshopInput.BallDetector(new(1), 0, 1, 0, 0, 0, 0, 1),
        WorkshopInput.PressurePlate(new(1), 0, 1, 0, 0, 0, 0, 1, minimumMass: 2.25f),
        WorkshopInput.Counter(new(1), 0, 1, 0, 0, 0, 0, 1, targetCount: 7),
        WorkshopInput.Clock(new(1), 0, 1, 0, 0, 0, 0, 1, intervalSeconds: 3.5f),
    };

    // Story 9.4b: every encoded instance field survives the decoder. Re-encoding the decoded construction reproduces the
    // exact bytes, so a field the decoder dropped, defaulted or re-derived would differ.
    [Theory]
    [MemberData(nameof(EveryInstanceKind))]
    public void Construction_EveryInstanceKind_RoundTripsEveryEncodedField(IWorkshopInstance instance)
    {
        var construction = new WorkshopConstruction(new(3), WorkshopCadenceSettings.Default(), new([instance]));
        var bytes = EncodeConstruction(construction);
        var decoded = DecodeConstruction(bytes);
        Assert.Equal(bytes, EncodeConstruction(decoded));
        var actual = Assert.Single(decoded.Instances);
        Assert.Equal(instance.GetType(), actual.GetType());
        Assert.Equal(instance.PartId, actual.PartId);
        if (instance is not IWorkshopDeclaredColliders) Assert.Equal(instance, actual);
    }

    public static TheoryData<WorkshopGoalKind> EveryNamedGoalKind() => [.. Enum.GetValues<WorkshopGoalKind>()];

    // Story 9.4b: every declared goal kind keeps each goal field through the save codec.
    [Theory]
    [MemberData(nameof(EveryNamedGoalKind))]
    public void Construction_EveryGoalKind_RoundTripsEveryGoalField(WorkshopGoalKind kind)
    {
        var construction = kind switch
        {
            WorkshopGoalKind.Captured => SpringboardLessons.Create(WorkshopPuzzleId.SpringForward, new(1),
                WorkshopCadenceSettings.Default(), new(1), new(2), new(3), new((Half)1)),
            WorkshopGoalKind.ActivatedAfter => DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(),
                new(1), new(2), new(3), new((Half).5)),
            WorkshopGoalKind.None => new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
                new([WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1)])),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        Assert.Equal(kind, construction.Puzzle.Goal.Kind);
        var bytes = EncodeConstruction(construction);
        var decoded = DecodeConstruction(bytes);
        Assert.Equal(construction.Puzzle.Goal, decoded.Puzzle.Goal);
        Assert.Equal(construction.Puzzle.Id, decoded.Puzzle.Id);
        Assert.Equal(bytes, EncodeConstruction(decoded));
    }

    // The Counter's target is its own whole field (ConstructionDecodeBoundaryTests.CounterTargetIsAWholeFieldAcceptedExactlyInItsRange).
    public enum DeclaredSetting { PressurePlateMinimumMass, ClockIntervalSeconds }

    private static PartId Owner(DeclaredSetting setting) => setting switch
    {
        DeclaredSetting.PressurePlateMinimumMass => PartId.PressurePlate,
        DeclaredSetting.ClockIntervalSeconds => PartId.Clock,
        _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, "Unknown declared setting."),
    };

    // Story 9.4b: a missing, zero, non-integral or out-of-range declared setting is rejected, never replaced by a default
    // (AGENTS.md: no fallback), and the rejection names the instance and the field. Admitted values decode (positive controls).
    [Theory]
    [InlineData(DeclaredSetting.PressurePlateMinimumMass, .5f, null)]
    [InlineData(DeclaredSetting.ClockIntervalSeconds, 1f, null)]
    [InlineData(DeclaredSetting.PressurePlateMinimumMass, 0f, "Pressure plate 1 minimum mass '0'")]
    [InlineData(DeclaredSetting.PressurePlateMinimumMass, float.NaN, "Pressure plate 1 minimum mass 'NaN'")]
    [InlineData(DeclaredSetting.ClockIntervalSeconds, 0f, "Clock 1 interval seconds '0'")]
    public void Construction_DeclaredSetting_DecodesOnlyAdmittedValuesAndNamesTheRejectedField(DeclaredSetting setting, float threshold, string? rejection)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var part = builder.CreateString(Owner(setting).Value); // the record's part identity is a string at the wire boundary
        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, 1);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, part);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, 0, 16, 0));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        CuriousContraptions.Wire.InstanceRecord.AddThreshold(builder, threshold);
        var instance = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        var instances = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, [instance]);
        var settings = WorkshopCadenceSettings.Default();
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(
            builder, (uint)settings.Simulation, (uint)settings.Physical,
            (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instances);
        builder.Finish(CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder).Value);
        var bytes = builder.SizedByteArray();
        if (rejection is null)
        {
            Assert.Equal(Owner(setting), Assert.Single(DecodeConstruction(bytes).Instances).PartId);
            return;
        }
        Assert.Contains(rejection, Assert.Throws<ArgumentException>(() => DecodeConstruction(bytes)).Message);
    }

    // Story 9.4b (review-9-4 F8): a Captured goal is declared (Body = captured ball, Target = goal Receiver, EventSource =
    // its capture sensor). The saved record keeps that declaration field for field, including after a replacement Receiver.
    [Fact]
    public void Construction_CapturedGoal_EncodesDeclaredBodyTargetAndSensor()
    {
        var lesson = SpringboardLessons.Create(WorkshopPuzzleId.ReadyToRebound, new(1), WorkshopCadenceSettings.Default(),
            new(1), new(2), new(3), new((Half)1));
        var replacement = WorkshopInput.Receiver(new(4), 2, .55, 0, 0, 0, 0, 1);
        var construction = SpringboardLessons.WithReceiver(lesson, replacement);
        var goal = construction.Puzzle.Goal;
        Assert.Equal(new GpuBodyId(1), goal.Body);
        Assert.Equal(new GpuBodyId(4), goal.Target);
        var builder = new Google.FlatBuffers.FlatBufferBuilder(1024);
        builder.Finish(WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, construction).Value);
        var record = CuriousContraptions.Wire.ConstructionRecord.GetRootAsConstructionRecord(
            new Google.FlatBuffers.ByteBuffer(builder.SizedByteArray()));
        var puzzle = record.Puzzle!.Value;
        Assert.Equal((uint)WorkshopGoalKind.Captured, puzzle.GoalKind);
        Assert.Equal(1UL, puzzle.GoalBody);
        Assert.Equal(4UL, puzzle.GoalTarget);
        Assert.Equal(WorkshopPhysicsCompiler.CaptureSensor(construction.Receiver!.Value).Value, puzzle.GoalEventSource);
        Assert.Equal(goal, WorkshopWireFlatBuffers.DecodeConstructionRecord(record).Puzzle.Goal);
    }

    [Fact]
    public void Command_InvalidInputs_Throw()
    {
        var valid = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1));
        // Sequence 0
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(valid with { Sequence = default }));
        // Initialize with cadence != 0
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(
            new WorkshopCommand(new(1), WorkshopCommandKind.Initialize, new(1), new(1), null,
                Session: Session, Cadence: new(1), Settings: WorkshopCadenceSettings.Default())));
        // Decode corrupt bytes
        Assert.Equal(WireFaultReason.TrailingBytes, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(new byte[16])).Reason);
    }

    // Blind hunter 4 (Story 9.4b final round): the retired DeviceLost rejection (6) is undefined on the FlatBuffers
    // response too, so a response carrying it rejects; its neighbours remain admitted.
    [Theory]
    [InlineData((byte)5, true)]
    [InlineData((byte)6, false)]
    [InlineData((byte)7, true)]
    public void Response_RetiredDeviceLossRejectionIsUndefined(byte reason, bool admitted)
    {
        // The encoder refuses an undefined reason, so the wire byte is found by encoding the defined neighbours 5 and 7.
        byte[] Encoded(WorkshopRejection rejection) => WorkshopWireFlatBuffers.EncodeResponse(new WorkshopResponse(new(10),
            WorkshopResponseKind.Acknowledgement, new(WorkshopCommandOutcome.Rejected, rejection), WorkshopSimulationPhase.Building,
            default, Session: Session));
        var five = Encoded(WorkshopRejection.GpuAdmission);
        var seven = Encoded(WorkshopRejection.InvalidRead);
        var at = Assert.Single(Enumerable.Range(0, five.Length), index => five[index] != seven[index]);
        Assert.Throws<ArgumentException>(() => Encoded((WorkshopRejection)6));
        five[at] = reason;
        if (admitted) Assert.Equal((WorkshopRejection)reason, WorkshopWireFlatBuffers.DecodeResponse(five).Result.Reason);
        else Assert.Contains("discriminant", Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeResponse(five)).Message);
    }

    [Fact]
    public void Response_Acknowledgement_RoundTrips()
    {
        var ack = new WorkshopResponse(new(10), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Building,
            default,
            Session: Session);
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(ack);
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(encoded);
        Assert.Equal(ack.Sequence, decoded.Sequence);
        Assert.Equal(ack.Kind, decoded.Kind);
        Assert.Equal(ack.Result, decoded.Result);
        Assert.Equal(ack.Phase, decoded.Phase);
        Assert.Equal(ack.Session, decoded.Session);
    }

    [Fact]
    public void Response_PhysicalRead_RoundTrips()
    {
        var master = new ClockGeneration(1);
        var stamp = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, master, new(1_000_000), new(100));
        var canonical = new CanonicalBody(new(1), 1, 1, default, default, default);
        var body = new PhysicsBodyRead(canonical, CanonicalRotation.Identity, default, default);
        var bodies = new PhysicsBodyReadSet(new[] { body });

        var capture = CaptureLatch.Clear(new(9));
        var captures = new PhysicsCaptureRead(new[] { capture });

        var work = new ContactWorkRead(new(40), new(1), 1, new(16f), new(4f));
        var occ = new ContactWorkOccurrence(
            new(0), new(1), new(1), 1, 8, (Half)0.5f,
            new ContactSpeed(4f), new Joules(8f), ContactWorkEffect.Paid);
        var contactWorks = new PhysicsContactWorkRead(new[] { work }, new[] { occ });

        var elec = new ElectricalSourceRead(new(50), new(1), new Joules(100f), new Joules(5f), ElectricalEnable.Enabled);
        var electrical = new PhysicsElectricalRead(new[] { elec });

        var read = new WorkshopRead(new(1), new(1), bodies, new(1), stamp, captures,
            PhysicsMotionRead.Decode(WireFaultTests.MotionBytes(canonical), bodies, new(1)),
            ContactWorks: contactWorks, Electrical: electrical);

        var response = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running,
            read,
            Session: Session,
            Publication: new(1),
            Cadence: new(1),
            MasterGeneration: master,
            Projection: new(1),
            SourcePulse: new(1));

        var encoded = WorkshopWireFlatBuffers.EncodeResponse(response);
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(encoded);

        Assert.Equal(response.Sequence, decoded.Sequence);
        Assert.Equal(response.Kind, decoded.Kind);
        Assert.Equal(response.Result, decoded.Result);
        Assert.Equal(response.Phase, decoded.Phase);
        Assert.Equal(response.Session, decoded.Session);
        Assert.Equal(response.Publication, decoded.Publication);
        Assert.Equal(response.Cadence, decoded.Cadence);
        Assert.Equal(response.MasterGeneration, decoded.MasterGeneration);
        Assert.Equal(response.Projection, decoded.Projection);

        var decodedRead = decoded.Read;
        Assert.Equal(read.Epoch, decodedRead.Epoch);
        Assert.Equal(read.Tick, decodedRead.Tick);
        Assert.Equal(read.Revision, decodedRead.Revision);
        Assert.Equal(read.Capture, decodedRead.Capture);

        Assert.Equal(1, decodedRead.Bodies.Count);
        Assert.Equal(body.Body.Id, decodedRead.Bodies[0].Body.Id);

        Assert.Equal(1, decodedRead.Captures.Count);
        Assert.Equal(capture.Sensor, decodedRead.Captures[0].Sensor);

        Assert.Equal(1, decodedRead.ContactWorks.Count);
        Assert.Equal(work.RemainingEnergy, decodedRead.ContactWorks[0].RemainingEnergy);
        Assert.Equal(1, decodedRead.ContactWorks.OccurrenceCount);
        Assert.Equal(occ.Debit, decodedRead.ContactWorks.Occurrence(0).Debit);

        Assert.Equal(1, decodedRead.Electrical.Count);
        Assert.Equal(elec.Remaining, decodedRead.Electrical[0].Remaining);
        Assert.Equal(elec.Debit, decodedRead.Electrical[0].Debit);
    }
}
