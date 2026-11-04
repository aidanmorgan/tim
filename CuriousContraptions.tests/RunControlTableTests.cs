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
