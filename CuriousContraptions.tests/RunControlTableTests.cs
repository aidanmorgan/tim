using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

/// <summary>engine-run-control-table: the one typed run-control path. Run compiles one row (id, owner, lane, lane index) for each
/// declared RunControl parameter of an unlocked part; one ConfigureRunControl command carries (owner, id, typed value). Every case runs
/// the one codec, or the real encode → router → executor → simulation → engine path of the shared harness.</summary>
public sealed class RunControlTableTests
{
    private static readonly GpuBodyId Gate = new(1), Source = new(3), Ball = new(5), LockedSource = new(8);
    private static readonly RuntimeSessionId Session = new(101, 206);

    /// <summary>A Powered Gate supplied by an unlocked Battery, a ball and a locked Battery: two declared stores, one run control.</summary>
    private static WorkshopConstruction Bench(ElectricalEnable enabled = ElectricalEnable.Enabled) => new(new(2), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(new PartId("powered_gate"), Gate, 0, 3, 0, 0, 0, 0, 1),
            PlacedParts.Battery(Source, 3, 3, 0, enabled: enabled),
            WorkshopInput.Generic(new PartId("ball"), Ball, -3, 3, 0, 0, 0, 0, 1),
            PlacedParts.Battery(LockedSource, 6, 3, 0) with { Locked = true }),
        Connections: new(new WorkshopConnection(Source, WorkshopSocket.Supply, Gate, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));

    private static WorkshopCompiledScene Compiled() => WorkshopSceneCompiler.Compile(Bench(), new(101, 206), new(7));

    private static ElectricalEnable Enable(WorkshopRead read, GpuBodyId owner) =>
        Enumerable.Range(0, read.Electrical.Count).Select(i => read.Electrical[i]).Single(row => row.Owner == owner).Enabled;

    private static WorkshopCommand Construct(WorkshopCompiledScene scene) =>
        new(new(1), WorkshopCommandKind.Construct, new(1), new(1), scene, Session: Session, Cadence: new(1), Projection: new(1));

    // Run compiles exactly the declared run controls of unlocked parts: the Battery's enable is its store's SourceEnable lane, at the
    // index of the source the Battery owns. The locked Battery compiles a source but no row; the gate and the ball declare none.
    [Fact]
    public void RunCompilesOneRowPerDeclaredRunControlOfAnUnlockedPart()
    {
        var scene = Compiled();
        Assert.Equal(2, scene.Physics.Electrical.Sources.Length);
        var row = Assert.Single(scene.RunControls);
        Assert.Equal((Source, RunControlLane.SourceEnable), (row.Owner, row.Lane));
        Assert.NotEqual(0ul, row.Id.Value);
        Assert.Equal(Source, scene.Physics.Electrical.Sources[row.LaneIndex].Owner);
    }

    // A compiled table is admitted only when every row resolves: a unique nonzero id, an installed owner, a defined lane, at most one
    // row per lane slot, and a SourceEnable index naming a source its owner owns. The compiled row itself is admitted (the control).
    [Fact]
    public void TheCompiledTableRefusesRowsThatDoNotResolve()
    {
        var scene = Compiled();
        var row = Assert.Single(scene.RunControls);
        var locked = Array.FindIndex(scene.Physics.Electrical.Sources.ToArray(), source => source.Owner == LockedSource);
        WorkshopCompiledScene With(params RunControlRow[] rows) => new(scene.Identity, scene.Revision, scene.Settings, scene.Physics,
            scene.Activation, scene.Presentation, scene.Owners, rows);
        Assert.Equal([row, row with { Id = new(row.Id.Value + 1), Owner = LockedSource, LaneIndex = locked }],
            With(row, row with { Id = new(row.Id.Value + 1), Owner = LockedSource, LaneIndex = locked }).RunControls);
        foreach (var rows in new RunControlRow[][]
        {
            [row with { Id = new(0) }],
            [row, row with { Owner = LockedSource, LaneIndex = locked }],
            [row with { Owner = new(99) }],
            [row with { Owner = Gate }],
            [row with { Lane = (RunControlLane)7 }],
            [row with { LaneIndex = 2 }],
            [row with { LaneIndex = -1 }],
            [row, row with { Id = new(row.Id.Value + 1) }],
        })
            Assert.Throws<ArgumentException>(() => With(rows));
    }

    // The command and the compiled table cross the one codec unchanged: owner, id and every value kind; every field of two rows whose
    // ids, owners and lane indices all differ, in order.
    [Fact]
    public void TheCommandAndTheCompiledTableRoundTripEveryField()
    {
        foreach (var value in new[] { RunControlValue.Of(ElectricalEnable.Enabled), RunControlValue.Of(ElectricalEnable.Disabled), RunControlValue.Of(-2.5f) })
        {
            var control = new WorkshopRunControl(new(42), new(7), value);
            var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(Configure(new(3), control)));
            Assert.Equal((WorkshopCommandKind.ConfigureRunControl, (WorkshopRunControl?)control), (decoded.Kind, decoded.RunControl));
        }
        var compiled = Compiled();
        var row = Assert.Single(compiled.RunControls);
        var locked = Array.FindIndex(compiled.Physics.Electrical.Sources.ToArray(), source => source.Owner == LockedSource);
        RunControlRow[] rows = [row, row with { Id = new(row.Id.Value + 1), Owner = LockedSource, LaneIndex = locked }];
        Assert.NotEqual(rows[0].LaneIndex, rows[1].LaneIndex);
        var scene = new WorkshopCompiledScene(compiled.Identity, compiled.Revision, compiled.Settings, compiled.Physics,
            compiled.Activation, compiled.Presentation, compiled.Owners, rows);
        Assert.Equal(rows, WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(Construct(scene))).Scene!.RunControls);
    }

    // An undefined lane in a compiled table is a typed wire fault the router refuses before the executor; nothing is installed and the
    // same valid Construct is still the next reliable request.
    [Fact]
    public async Task AnUndefinedLaneInTheCompiledTableIsATypedWireFault()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var construct = harness.Command(WorkshopCommandKind.Construct, Bench());
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(construct);
        var buffer = new ByteBuffer(bytes);
        var root = new Table(4 + buffer.GetInt(4), buffer);
        var record = new Table(root.__indirect(root.bb_pos + root.__offset(4 + 11 * 2)), buffer);
        var rows = record.__vector(record.__offset(4 + 28 * 2));
        Assert.Equal((byte)RunControlLane.SourceEnable, bytes[rows + 20]);
        bytes[rows + 20] = 7;
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => harness.Router.Dispatch(bytes)).Reason);
        // Nothing was installed, and the same valid Construct is still the next reliable request.
        Assert.Empty(harness.Simulation.Scene.RunControls);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(construct)).Result.Outcome);
        Assert.Single(harness.Simulation.Scene.RunControls);
    }

    // An undefined value kind or enable byte, a value whose unused field is not zero, a non-finite scalar, a zero owner or row id, and
    // the retired command kind 11 (the deleted electrical control) are typed wire faults the router refuses before the executor.
    // Nothing changes: the next valid control is still the next reliable request, and it applies on the next committed step.
    [Fact]
    public async Task UndefinedRunControlBytesAreTypedWireFaultsThatLeaveTheRouterUnchanged()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Bench());
        var valid = harness.Command(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(Source, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var (owner, id) = (Source.Value, valid.RunControl!.Value.Control.Value);
        const uint configure = (uint)WorkshopCommandKind.ConfigureRunControl;
        const byte enable = (byte)RunControlValueKind.Enable, scalar = (byte)RunControlValueKind.Scalar;
        foreach (var (kind, rowOwner, rowId, valueKind, enabled, value, reason) in new (uint, ulong, ulong, byte, byte, float, WireFaultReason)[]
        {
            (configure, owner, id, 0, 1, 0f, WireFaultReason.OutOfRange),
            (configure, owner, id, 9, 1, 0f, WireFaultReason.OutOfRange),
            (configure, owner, id, enable, 2, 0f, WireFaultReason.OutOfRange),
            (configure, owner, id, enable, 0, 1f, WireFaultReason.OutOfRange),
            (configure, owner, id, scalar, 1, 0f, WireFaultReason.OutOfRange),
            (configure, owner, id, scalar, 0, float.NaN, WireFaultReason.OutOfRange),
            (configure, owner, id, scalar, 0, float.PositiveInfinity, WireFaultReason.OutOfRange),
            (configure, 0, id, enable, 0, 0f, WireFaultReason.OutOfRange),
            (configure, owner, 0, enable, 0, 0f, WireFaultReason.OutOfRange),
            (11, owner, id, enable, 0, 0f, WireFaultReason.UnknownCommand),
        })
            Assert.Equal(reason, Assert.Throws<WireFaultException>(() =>
                harness.Router.Dispatch(Raw(valid, kind, rowOwner, rowId, valueKind, enabled, value))).Reason);
        Assert.Equal(ElectricalEnable.Enabled, Enable(harness.Committed, Source));
        var applied = (await harness.Dispatch(valid)).Result;
        Assert.Equal((WorkshopCommandOutcome.Applied, WorkshopRejection.None), (applied.Outcome, applied.Reason));
        Assert.Equal(ElectricalEnable.Disabled, Enable((await harness.Advance(1))[0], Source));
    }

    // In a run, a control naming an id the table does not have, or carrying a value kind its lane does not take (a scalar for an
    // enable), is refused with its typed reason and changes nothing: no control is held and the source keeps its authored enable.
    [Fact]
    public async Task AnUnknownIdOrAMismatchedValueKindIsRefusedWithItsTypedReasonAndChangesNothing()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Bench());
        var row = Assert.Single(harness.Simulation.Scene.RunControls);
        foreach (var (control, reason) in new (WorkshopRunControl, WorkshopRejection)[]
        {
            (new(Source, new(row.Id.Value + 1), RunControlValue.Of(ElectricalEnable.Disabled)), WorkshopRejection.UnknownRunControl),
            (new(Source, row.Id, RunControlValue.Of(0f)), WorkshopRejection.RunControlKindMismatch),
        })
        {
            var refused = (await harness.Execute(WorkshopCommandKind.ConfigureRunControl, runControl: control)).Result;
            Assert.Equal((WorkshopCommandOutcome.Rejected, reason), (refused.Outcome, refused.Reason));
        }
        Assert.Null(harness.Simulation.HeldRunControl(row.Id));
        Assert.All(await harness.Advance(2), read => Assert.Equal(ElectricalEnable.Enabled, Enable(read, Source)));
    }

    // The committed run-control read of an owner's lane is that lane's committed value: the authored enable until a committed step
    // applies the control, in both directions. A locked owner (no row) reads its committed slot too; an owner with no slot is refused.
    [Theory]
    [InlineData(ElectricalEnable.Enabled, ElectricalEnable.Disabled)]
    [InlineData(ElectricalEnable.Disabled, ElectricalEnable.Enabled)]
    public async Task TheCommittedRunControlReadFollowsTheCommittedLane(ElectricalEnable authored, ElectricalEnable control)
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Bench(authored));
        var row = Assert.Single(harness.Simulation.Scene.RunControls);
        Assert.Equal(RunControlValue.Of(authored), harness.Committed.RunControl(Source, RunControlLane.SourceEnable));
        Assert.Equal(RunControlValue.Of(ElectricalEnable.Enabled), harness.Committed.RunControl(LockedSource, RunControlLane.SourceEnable));
        Assert.Throws<ArgumentException>(() => harness.Committed.RunControl(Gate, RunControlLane.SourceEnable));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl: new(Source, row.Id, RunControlValue.Of(control)));
        Assert.Equal(RunControlValue.Of(authored), harness.Committed.RunControl(Source, RunControlLane.SourceEnable));
        Assert.Equal(RunControlValue.Of(control), (await harness.Advance(1))[0].RunControl(Source, RunControlLane.SourceEnable));
    }

    // ---- the RevoluteDriveTarget lane (engine-revolute-drive-target) ---------------------------------------------------------------

    private static readonly GpuBodyId LockedMotor = new(11), Supply = new(12), Motor = new(13);

    private static WorkshopGenericInstance MotorAt(GpuBodyId id, float x, float speed) =>
        WorkshopInput.Generic(new PartId("motor"), id, x, 4, 0, 0, 0, 0, 1, new([new(new("speed"), speed), new(new("torque"), 20f)]));

    /// <summary>A locked Motor at speed 4, a Battery and an unlocked Motor at speed 6 that the Battery supplies. The locked Motor is placed
    /// first, so its hinge is the lower identity and the unlocked Motor's drive is not the first of the drive table.</summary>
    private static WorkshopConstruction DriveBench(ElectricalEnable enabled = ElectricalEnable.Enabled) => new(new(2), WorkshopCadenceSettings.Default(), new(
            MotorAt(LockedMotor, -4, 4f) with { Locked = true },
            PlacedParts.Battery(Supply, 3, 4, 0, enabled: enabled),
            MotorAt(Motor, 0, 6f)),
        Connections: new(new WorkshopConnection(Supply, WorkshopSocket.Supply, Motor, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));

    private static WorkshopCompiledScene DriveScene() => WorkshopSceneCompiler.Compile(DriveBench(), new(101, 206), new(7));

    /// <summary>The position in the compiled drive table of the drive on the hinge <paramref name="owner"/> carries.</summary>
    private static int DriveIndex(PhysicsSceneDeclaration physics, GpuBodyId owner)
    {
        var revolutes = physics.Revolutes.ToArray();
        return Array.FindIndex(physics.RevoluteDrives.ToArray(), drive => revolutes.Single(joint => joint.Id == drive.Joint).BodyA == owner);
    }

    private static float Target(WorkshopRead read, GpuBodyId owner) => read.RunControl(owner, RunControlLane.RevoluteDriveTarget).Scalar;

    // Run compiles a RevoluteDriveTarget row for the unlocked Motor's speed with the declared parameter's bounds [0, 20] rad/s, at the
    // index of its drive in the compiled drive table (here the second: the locked Motor's hinge is the lower identity). The locked Motor
    // compiles its drive but no row; the Battery's SourceEnable row carries zero bounds.
    [Fact]
    public void RunCompilesADriveTargetRowWithItsParameterBoundsAtItsDriveIndex()
    {
        var scene = DriveScene();
        Assert.Equal(2, scene.Physics.RevoluteDrives.Length);
        var drive = Assert.Single(scene.RunControls, row => row.Lane == RunControlLane.RevoluteDriveTarget);
        Assert.Equal((Motor, 0f, 20f), (drive.Owner, drive.Minimum, drive.Maximum));
        Assert.Equal(1, DriveIndex(scene.Physics, Motor));
        Assert.Equal(DriveIndex(scene.Physics, Motor), drive.LaneIndex);
        var enable = Assert.Single(scene.RunControls, row => row.Lane == RunControlLane.SourceEnable);
        Assert.Equal((Supply, 0f, 0f), (enable.Owner, enable.Minimum, enable.Maximum));
        Assert.DoesNotContain(scene.RunControls, row => row.Owner == LockedMotor);
    }

    // A compiled RevoluteDriveTarget row resolves only to a drive its owner's hinge carries, and carries finite ordered bounds within the
    // ±128 rad/s angular envelope; an Enable row carries zero bounds. Each refused row has an admitted neighbour: the envelope limits
    // themselves, a single-value range, and the locked Motor's own slot under its own owner.
    [Fact]
    public void TheCompiledTableRefusesDriveTargetRowsWithInvalidBoundsOrSlots()
    {
        var scene = DriveScene();
        var drive = Assert.Single(scene.RunControls, row => row.Lane == RunControlLane.RevoluteDriveTarget);
        var enable = Assert.Single(scene.RunControls, row => row.Lane == RunControlLane.SourceEnable);
        var locked = DriveIndex(scene.Physics, LockedMotor);
        WorkshopCompiledScene With(params RunControlRow[] rows) => new(scene.Identity, scene.Revision, scene.Settings, scene.Physics,
            scene.Activation, scene.Presentation, scene.Owners, rows);
        foreach (var admitted in new[]
        {
            drive with { Minimum = -AngularVelocity.MaximumSpeed, Maximum = AngularVelocity.MaximumSpeed },
            drive with { Minimum = 5f, Maximum = 5f },
            drive with { Owner = LockedMotor, LaneIndex = locked },
        })
            Assert.Equal([enable, admitted], With(enable, admitted).RunControls);
        foreach (var refused in new[]
        {
            enable with { Maximum = 1f },
            enable with { Minimum = -1f },
            drive with { Minimum = float.NaN },
            drive with { Maximum = float.PositiveInfinity },
            drive with { Minimum = 3f, Maximum = 2f },
            drive with { Maximum = MathF.BitIncrement(AngularVelocity.MaximumSpeed) },
            drive with { Minimum = MathF.BitDecrement(-AngularVelocity.MaximumSpeed) },
            drive with { LaneIndex = locked },
            drive with { LaneIndex = 2 },
            drive with { LaneIndex = -1 },
            drive with { Owner = Supply },
        })
            Assert.Throws<ArgumentException>(() => With(enable, refused));
    }

    // The bounded rows cross the compiled-scene codec unchanged, every field of each; the committed drive read crosses the response
    // wire with each owner and its committed target, in drive-table order (the shared harness decodes every read it serves). A drive
    // read row naming no owner or carrying a non-finite or over-envelope target is refused at construction, which is the decoder's.
    [Fact]
    public async Task TheBoundedRowsAndTheCommittedDriveReadRoundTripEveryField()
    {
        var compiled = DriveScene();
        Assert.Equal(compiled.RunControls,
            WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(Construct(compiled))).Scene!.RunControls);

        await using var harness = await WorkshopIntegrationHarness.Running(DriveBench());
        var read = harness.Committed;
        var physics = harness.Simulation.Scene.Physics;
        Assert.Equal(2, read.DriveTargets.Count);
        Assert.Equal((LockedMotor, 4f), (read.DriveTargets[DriveIndex(physics, LockedMotor)].Owner, read.DriveTargets[DriveIndex(physics, LockedMotor)].TargetSpeed));
        Assert.Equal((Motor, 6f), (read.DriveTargets[DriveIndex(physics, Motor)].Owner, read.DriveTargets[DriveIndex(physics, Motor)].TargetSpeed));
        Assert.Equal(4f, Target(read, LockedMotor));
        foreach (var invalid in new RevoluteDriveTargetRead[] { new(default, 1f), new(Motor, float.NaN), new(Motor, MathF.BitIncrement(AngularVelocity.MaximumSpeed)) })
            Assert.Throws<ArgumentException>(() => new PhysicsDriveTargetRead([invalid]));
    }

    // The speed control admits its declared bounds inclusively (0 and 20 rad/s, each committed by the next step) and refuses a value one
    // step beyond either, and −1 and 20.001, with RunControlOutOfRange: nothing is held and the committed target is unchanged. An Enable
    // value on the speed row is refused as a kind mismatch. The worker never clamps.
    [Fact]
    public async Task ASpeedAtItsBoundsIsAppliedAndOneBeyondIsRefusedOutOfRangeChangingNothing()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(DriveBench());
        var row = Assert.Single(harness.Simulation.Scene.RunControls, row => row.Lane == RunControlLane.RevoluteDriveTarget);
        foreach (var (value, reason) in new (RunControlValue, WorkshopRejection)[]
        {
            (RunControlValue.Of(-1f), WorkshopRejection.RunControlOutOfRange),
            (RunControlValue.Of(MathF.BitDecrement(0f)), WorkshopRejection.RunControlOutOfRange),
            (RunControlValue.Of(MathF.BitIncrement(20f)), WorkshopRejection.RunControlOutOfRange),
            (RunControlValue.Of(20.001f), WorkshopRejection.RunControlOutOfRange),
            (RunControlValue.Of(ElectricalEnable.Enabled), WorkshopRejection.RunControlKindMismatch),
        })
        {
            var refused = (await harness.Execute(WorkshopCommandKind.ConfigureRunControl, runControl: new(Motor, row.Id, value))).Result;
            Assert.Equal((WorkshopCommandOutcome.Rejected, reason), (refused.Outcome, refused.Reason));
            Assert.Null(harness.Simulation.HeldRunControl(row.Id));
        }
        Assert.Equal(6f, Target((await harness.Advance(1))[0], Motor));
        foreach (var bound in new[] { 0f, 20f })
        {
            await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl: new(Motor, row.Id, RunControlValue.Of(bound)));
            Assert.Equal(bound, Target((await harness.Advance(1))[0], Motor));
        }
    }

    // Two lanes controlled before one step apply on the same committed step: the Battery's enable and the Motor's speed. At the default
    // 120 Hz cadence every tick crosses an electrical phase, so the enable applies on that step too. A later control on the speed alone
    // leaves the enable as committed.
    [Fact]
    public async Task ControlsOnTwoLanesApplyOnTheSameCommittedStepAndALaterOneLeavesTheOtherLane()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(DriveBench(ElectricalEnable.Disabled));
        Assert.Equal(SimulationCadence.Hz120, harness.Simulation.Scene.Settings.Simulation);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(Supply, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(Motor, RunControlLane.RevoluteDriveTarget, RunControlValue.Of(12f)));
        Assert.Equal((ElectricalEnable.Disabled, 6f), (Enable(harness.Committed, Supply), Target(harness.Committed, Motor)));

        var both = (await harness.Advance(1))[0];
        Assert.Equal((ElectricalEnable.Enabled, 12f), (Enable(both, Supply), Target(both, Motor)));

        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(Motor, RunControlLane.RevoluteDriveTarget, RunControlValue.Of(8f)));
        var later = (await harness.Advance(1))[0];
        Assert.Equal((ElectricalEnable.Enabled, 8f, 4f), (Enable(later, Supply), Target(later, Motor), Target(later, LockedMotor)));
    }

    // No electrical phase gates a drive target. At 240 Hz a tick is two 480 Hz substeps and an electrical phase falls every fourth
    // substep, so the second tick after Run crosses none. Controls sent before it: the speed is committed on that tick, while the enable
    // waits for the third tick, the next phase.
    [Fact]
    public async Task ADriveTargetAppliesOnATickWithoutAnElectricalPhase()
    {
        var bench = DriveBench(ElectricalEnable.Disabled);
        await using var harness = await WorkshopIntegrationHarness.Running(bench with { Settings = bench.Settings with { Simulation = SimulationCadence.Hz240 } });
        await harness.Advance(1);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(Supply, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(Motor, RunControlLane.RevoluteDriveTarget, RunControlValue.Of(12f)));
        var noPhase = (await harness.Advance(1))[0];
        Assert.Equal((ElectricalEnable.Disabled, 12f), (Enable(noPhase, Supply), Target(noPhase, Motor)));
        var phase = (await harness.Advance(1))[0];
        Assert.Equal((ElectricalEnable.Enabled, 12f), (Enable(phase, Supply), Target(phase, Motor)));
    }

    // The engine maps each driven revolute to its drive by rank among the driven ones, never by revolute index. An undriven Impact
    // Lever hinge sorts first, so the supplied Motor's drive index (0) differs from its revolute index (1); a second, unsupplied Motor
    // follows. Setting the first Motor to 12 commits 12 on its own drive only, the second keeps its 6, and the first Motor's shaft
    // reaches 12 within 2 % (MotorTests.SpeedAllowance) once its ramp has passed.
    [Fact]
    public async Task TheEngineMapsEachDriveTargetByDriveRankPastAnUndrivenHinge()
    {
        GpuBodyId lever = new(21), first = new(22), source = new(23), second = new(24);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
                WorkshopInput.Generic(new PartId("impact_lever"), lever, -8, 4, 0, 0, 0, 0, 1),
                MotorAt(first, 0, 6f), PlacedParts.Battery(source, 3, 4, 0), MotorAt(second, 8, 6f)),
            Connections: new(new WorkshopConnection(source, WorkshopSocket.Supply, first, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var physics = harness.Simulation.Scene.Physics;
        var revolutes = physics.Revolutes.ToArray();
        Assert.Equal((3, 2), (revolutes.Length, physics.RevoluteDrives.Length));
        Assert.Equal(lever, revolutes[0].BodyA);
        Assert.Equal((first, 0), (revolutes[1].BodyA, DriveIndex(physics, first)));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(first, RunControlLane.RevoluteDriveTarget, RunControlValue.Of(12f)));
        var reads = await harness.Advance(30);
        Assert.All(reads, read => Assert.Equal((12f, 6f), (Target(read, first), Target(read, second))));
        var rotor = construction.Instances.Identities[first].Hinges[0].Body;
        Assert.True(reads[^1].Bodies.TryGet(rotor, out var body));
        Assert.True(MathF.Abs(body.AngularVelocity.Z - 12f) <= .02f * 12f, $"The first Motor's shaft spins at {body.AngularVelocity.Z} rad/s");
    }

    private static WorkshopCommand Configure(CommandSequence sequence, WorkshopRunControl control) =>
        new(sequence, WorkshopCommandKind.ConfigureRunControl, new(1), default, null, RevisionKind: ExpectedRevisionKind.Any,
            Session: Session, Cadence: new(1), Projection: new(1), RunControl: control);

    /// <summary>The command <paramref name="command"/> rebuilt with a raw kind and raw run-control owner, row id and value fields.</summary>
    private static byte[] Raw(WorkshopCommand command, uint kind, ulong owner, ulong control, byte valueKind, byte enabled, float scalar)
    {
        var builder = new FlatBufferBuilder(128);
        var session = new byte[RuntimeSessionId.ByteLength]; command.Session.WriteTo(session);
        var vector = W.WorkshopCommand.CreateSessionVectorBlock(builder, session);
        W.WorkshopCommand.StartWorkshopCommand(builder);
        W.WorkshopCommand.AddSequence(builder, command.Sequence.Value);
        W.WorkshopCommand.AddKind(builder, (W.CommandKind)kind);
        W.WorkshopCommand.AddEpoch(builder, command.Epoch.Value);
        W.WorkshopCommand.AddRevisionKind(builder, (uint)command.RevisionKind);
        W.WorkshopCommand.AddRevision(builder, command.Revision.Value);
        W.WorkshopCommand.AddSession(builder, vector);
        W.WorkshopCommand.AddCadence(builder, command.Cadence.Value);
        W.WorkshopCommand.AddProjection(builder, command.Projection.Value);
        W.WorkshopCommand.AddRunControl(builder, W.RunControlCommandRecord.CreateRunControlCommandRecord(builder, owner, control, scalar,
            valueKind, (W.ElectricalEnable)enabled));
        builder.FinishSizePrefixed(W.WorkshopCommand.EndWorkshopCommand(builder).Value, WorkshopWireFlatBuffers.CommandIdentifier);
        return builder.SizedByteArray();
    }
}
