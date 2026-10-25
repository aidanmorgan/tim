using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.32 suite-first guards (CAT-023 §5), written on the record path and kept byte-identical across the generic
/// conversion: every bench is built through <see cref="PlacedParts.Domino"/> and run on the production compile and the shipped worker
/// engine, tick by tick.</summary>
public sealed class DominoGuardTests
{
    private const int Ticks = 1200;
    private static readonly double Floor = (double)WorkshopConstruction.WorkbenchSurface.Value, Upright = Floor + .55;
    private static readonly GpuBodyId End = new(2), Lamp = new(3);

    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.ActivationOut, target, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));

    /// <summary>domino_effect with its four inventory tiles at x = -4, -3, -2, -1 (ids 4-7), optionally without one, end wired to the lamp.</summary>
    private static WorkshopConstruction Lesson(float precision, bool wired = true, int skip = -1)
    {
        var result = PuzzleCatalogue.Create(new("domino_effect"), new(2), WorkshopCadenceSettings.Default(), new(1), new(precision));
        for (var i = 0; i < 4; i++)
            if (i != skip) result = result.WithInstance(PlacedParts.Domino(new((ulong)(4 + i)), -4 + i, Upright + .01, 0, 0, 0, 0, 1));
        return wired ? result with { Connections = new(Signal(End, Lamp)) } : result;
    }

    private static PhysicsBodyRead Find(WorkshopRead read, GpuBodyId id)
    {
        for (var i = 0; i < read.Bodies.Count; i++) if (read.Bodies[i].Body.Id == id) return read.Bodies[i];
        throw new ArgumentException($"Body {id.Value} is not read.", nameof(id));
    }

    /// <summary>Tilt of the tile's local +Y axis from world up, in degrees.</summary>
    private static double Tilt(PhysicsBodyRead body) =>
        Math.Acos(Math.Clamp(1 - 2 * (body.Rotation.X * body.Rotation.X + body.Rotation.Z * body.Rotation.Z), -1, 1)) * 180 / Math.PI;

    /// <summary>Total rotation from an upright (identity) admitted pose, in degrees: the angle the orientation sensor measures.</summary>
    private static double Turned(PhysicsBodyRead body) => 2 * Math.Acos(Math.Min(1, Math.Abs((double)body.Rotation.W))) * 180 / Math.PI;

    private static ActivationLatch LatchOf(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Activations.Count; i++) if (read.Activations[i].Owner == owner) return read.Activations[i];
        throw new ArgumentException($"No node for {owner.Value}.", nameof(owner));
    }

    private static bool HasNode(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Activations.Count; i++) if (read.Activations[i].Owner == owner) return true;
        return false;
    }

    private static int FirstTick(List<WorkshopRead> run, Func<WorkshopRead, bool> predicate) => run.FindIndex(read => predicate(read));

    [Fact]
    public void TheCompiledTileIsTheFormerDominoMaterialBitForBit()
    {
        Assert.Equal((BitConverter.SingleToUInt32Bits(.125f), BitConverter.SingleToUInt32Bits(.55f), BitConverter.SingleToUInt32Bits(.325f)),
            (BitConverter.SingleToUInt32Bits(LawTile.HalfExtents.X), BitConverter.SingleToUInt32Bits(LawTile.HalfExtents.Y),
                BitConverter.SingleToUInt32Bits(LawTile.HalfExtents.Z)));
        Assert.Equal(BitConverter.SingleToUInt32Bits(.4f), BitConverter.SingleToUInt32Bits(LawTile.Mass));
        Assert.Equal(new LawMaterial(.05f, .1f, .6f, 0f), LawTile.Material);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.45f)]
    [InlineData(1f)]
    public async Task TheWiredLessonCascadeTopplesByContactFiresOnceAtFortyFiveDegreesAndSolvesOnce(float precision)
    {
        var lesson = Lesson((float)precision);
        lesson.Validate();
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(lesson, Ticks);
        // The chain topples in order, each tile starting to turn only after the one before it: real contact, not a shared push.
        var order = new GpuBodyId[] { new(4), new(5), new(6), new(7), End };
        var starts = order.Select(tile => FirstTick(run, read => Tilt(Find(read, tile)) > 1)).ToArray();
        for (var i = 0; i < order.Length; i++)
        {
            Assert.True(starts[i] >= 0, $"tile {order[i].Value} never moves");
            Assert.True(run.Max(read => Tilt(Find(read, order[i]))) > 60, $"tile {order[i].Value} topples");
            if (i > 0) Assert.True(starts[i] > starts[i - 1], $"tile {order[i].Value} starts at tick {starts[i]}, before tile {order[i - 1].Value} ({starts[i - 1]})");
        }
        // One orientation occurrence: the end tile latches once, past 45° from its admitted pose, and never changes after.
        Assert.Equal(ActivationPhase.Clear, LatchOf(built, End).Phase);
        var fired = FirstTick(run, read => LatchOf(read, End).Phase == ActivationPhase.Latched);
        Assert.True(fired > 0, "the end tile fires");
        var latch = LatchOf(run[fired], End);
        Assert.Equal(ActivationOccurrenceKind.Orientation, latch.Kind);
        Assert.True(Turned(Find(run[fired - 1], End)) < 45.2 && Turned(Find(run[fired], End)) > 44.8,
            $"fires crossing 45° ({Turned(Find(run[fired - 1], End)):F3}° then {Turned(Find(run[fired], End)):F3}°)");
        Assert.All(run.Skip(fired), read => Assert.Equal(latch, LatchOf(read, End)));
        Assert.All(run.Take(fired), read => Assert.Equal(ActivationPhase.Clear, LatchOf(read, End).Phase));
        // The lamp carries that one occurrence, and the ActivatedAfter goal is first met at that tick.
        Assert.Equal(latch.EventOrdinal, LatchOf(run[fired], Lamp).EventOrdinal);
        Assert.Equal(ActivationPhase.Latched, LatchOf(run[^1], Lamp).Phase);
        Assert.Equal(fired, FirstTick(run, read => WorkshopGoalEvaluator.Occurrence(lesson.Puzzle.Goal, read.Captures, read.Activations) is not null));
        Assert.True(LaunchLessonSolveTests.Solved(lesson, run));
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
    }

    [Fact]
    public async Task AGapBreaksTheChainSoTheEndStandsAndTheLampStaysDark()
    {
        var gap = Lesson(.45f, skip: 2);
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(gap, Ticks);
        // Control: the chain runs up to the gap.
        Assert.True(run.Max(read => Tilt(Find(read, new(4)))) > 60 && run.Max(read => Tilt(Find(read, new(5)))) > 60, "the chain runs to the gap");
        Assert.True(run.Max(read => Tilt(Find(read, new(7)))) < 5, "the tile beyond the gap stands");
        Assert.True(run.Max(read => Tilt(Find(read, End))) < 5, "the end tile stands");
        Assert.All(run, read => Assert.Equal(ActivationPhase.Clear, LatchOf(read, End).Phase));
        Assert.All(run, read => Assert.Equal(ActivationPhase.Clear, LatchOf(read, Lamp).Phase));
        Assert.False(LaunchLessonSolveTests.Solved(gap, run));
    }

    [Fact]
    public async Task AnUnwiredEndCompilesNoSensorOrNodeAndFiresNothingWhenItFalls()
    {
        var unwired = Lesson(.45f, wired: false);
        Assert.Empty(WorkshopPhysicsCompiler.Compile(unwired, new(1, 2)).OrientationSensors.ToArray());
        Assert.Equal([Lamp], WorkshopActivationCompiler.Compile(unwired).Nodes.ToArray().Select(node => node.Owner));
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(unwired, Ticks);
        Assert.True(run.Max(read => Tilt(Find(read, End))) > 60, "the end tile still falls");
        Assert.All(run, read => Assert.False(HasNode(read, End)));
        Assert.All(run, read => Assert.Equal(ActivationPhase.Clear, LatchOf(read, Lamp).Phase));
        Assert.False(LaunchLessonSolveTests.Solved(unwired, run));
    }

    [Fact]
    public async Task AnUprightWiredTileStandsAndNeverFires()
    {
        var bench = Bench([PlacedParts.Domino(new(1), 0, Upright, 0, 0, 0, 0, 1), PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1)], Signal(new(1), new(2)));
        Assert.Single(WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).OrientationSensors.ToArray());
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(bench, 480);
        Assert.All(run, read => Assert.Equal(ActivationPhase.Clear, LatchOf(read, new(1)).Phase));
        Assert.True(run.Max(read => Tilt(Find(read, new(1)))) < 1, "the tile stands");
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(30, true)]
    public async Task ATileTenDegreesOffUprightRocksBackSilentlyAndThirtyDegreesTopplesAndFires(int degrees, bool topples)
    {
        var radians = degrees * Math.PI / 180;
        var bench = Bench([PlacedParts.Domino(new(1), 0, Floor + .55 * Math.Cos(radians) + .125 * Math.Sin(radians) + .005, 0,
            0, 0, Math.Sin(radians / 2), Math.Cos(radians / 2)), PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1)], Signal(new(1), new(2)));
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(bench, 480);
        if (topples)
        {
            Assert.True(run.Max(read => Tilt(Find(read, new(1)))) > 60, "the tile topples");
            var fired = FirstTick(run, read => LatchOf(read, new(1)).Phase == ActivationPhase.Latched);
            Assert.True(fired > 0);
            Assert.All(run.Skip(fired), read => Assert.Equal(LatchOf(run[fired], new(1)), LatchOf(read, new(1))));
        }
        else
        {
            Assert.All(run, read => Assert.Equal(ActivationPhase.Clear, LatchOf(read, new(1)).Phase));
            Assert.True(Tilt(Find(run[^1], new(1))) < 2, "the tile rocks back upright");
        }
    }

    [Fact]
    public void AnActivationInputIntoADominoIsRefusedAtTheEditAndSaveBoundaries()
    {
        var into = Bench([PlacedParts.Lamp(new(1), 3, 1, 0, 0, 0, 0, 1), PlacedParts.Domino(new(2), 0, Upright, 0, 0, 0, 0, 1)], Signal(new(1), new(2)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, into.Validate);
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var bytes = SaveFixture.Finish(builder, WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, into, validate: false), 3);
        SaveFixture.Refused(WorkshopSaveRejection.InvalidConnection, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
    }

    [Fact]
    public async Task TheHighestAndLowestPlacementsAreAdmittedAndOneBeyondIsRefused()
    {
        foreach (var y in new[] { 64d, -64d })
        {
            var bench = Bench([PlacedParts.Domino(new(1), 0, y, 0, 0, 0, 0, 1)]);
            bench.Validate();
            Assert.Single(WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).Bodies.ToArray(), body => body.Motion == RigidMotionKind.Dynamic);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => PlacedParts.Domino(new(1), 0, 64.0001, 0, 0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlacedParts.Domino(new(1), 0, -64.0001, 0, 0, 0, 0, 1));
        // The highest tile is a free dynamic body: it falls.
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(Bench([PlacedParts.Domino(new(1), 0, 64, 0, 0, 0, 0, 1)]), 60);
        var top = Find(run[^1], new(1));
        Assert.True((top.Body.Cell.Y + top.Body.Local.Y) / 16f < 63.5f && top.Body.Velocity.Y < 0, "the highest tile falls");
    }

    /// <summary>Run, Reset and Run again in one simulation: Reset restores every tile upright and rearms the sensor, so the second run
    /// replays the first tick for tick, its one occurrence included.</summary>
    [Fact]
    public async Task ResetRestoresUprightTilesAndRearmsTheSensorSoTheSecondRunReplaysTheFirst()
    {
        var lesson = Lesson(.45f);
        await using var device = new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(lesson)).Outcome);
        var built = simulation.Committed;
        async Task<List<WorkshopRead>> RunOnce()
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            var reads = new List<WorkshopRead>();
            for (var tick = 0; tick < 900; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
                reads.Add(simulation.Committed);
            }
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
            return reads;
        }
        var first = await RunOnce();
        NoFixedCapacityTests.AssertSameState(built, simulation.Committed, "Reset");
        Assert.Equal(ActivationPhase.Clear, LatchOf(simulation.Committed, End).Phase);
        Assert.All(new GpuBodyId[] { new(4), new(5), new(6), new(7), End }, tile => Assert.True(Tilt(Find(simulation.Committed, tile)) < .01));
        var second = await RunOnce();
        Assert.True(FirstTick(first, read => LatchOf(read, End).Phase == ActivationPhase.Latched) > 0, "the first run fires");
        for (var tick = 0; tick < first.Count; tick++) NoFixedCapacityTests.AssertSameState(first[tick], second[tick], $"second run tick {tick + 1}");
    }

    [Fact]
    public async Task SaveAndLoadReplayTheWiredCascadeIdentically()
    {
        var lesson = Lesson(.45f);
        var save = new WorkshopSavedConstruction(lesson, new(8));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(save));
        Assert.Equal(save, loaded);
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(lesson, 900);
        var (_, replay, _) = await NoFixedCapacityTests.RunAndReset(loaded.Construction, 900);
        for (var tick = 0; tick < run.Count; tick++) NoFixedCapacityTests.AssertSameState(run[tick], replay[tick], $"reloaded tick {tick + 1}");
        Assert.True(LaunchLessonSolveTests.Solved(loaded.Construction, replay));
    }
}
