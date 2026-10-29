using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The C-Lamp+Detector suite-first guards (Stories 9R.9 and 9R.16), recorded on the record path at 54ffd9f0 and unchanged
/// after the conversion: only <see cref="PlacedParts"/> builds the lamp and the detector. Every case runs the production compile,
/// worker engine and tick progression. The detector's committed passages are counted by a wired tally counter's committed memory
/// (the detector's own latch holds its first occurrence) and cross-checked by replaying the production passage tracker over the
/// committed bodies; the lamp's latch is read from committed activations.</summary>
public sealed class LampDetectorGuardTests
{
    private const int Ticks = 4 * 120;
    private static readonly double Floor = (double)WorkshopConstruction.WorkbenchSurface.Value;
    private const double BallRadius = .34, Clearance = .02;
    // The detector's crossing plane sits 0.62 m above the bench: a ball resting on the bench (centre 0.34 m up) is downstream of it
    // and never rearms, and one dropped from 3 m above its resting height rebounds (restitution 0.55 × 1) past the rearm line.
    private static readonly double Plane = Floor + .62, DropY = Floor + BallRadius + 3;
    private static readonly double Rearm = Plane + Clearance + BallRadius; // local +X down: upstream is above the plane
    private static readonly double Half45 = Math.Sqrt(.5);

    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target, WorkshopSocket input = WorkshopSocket.ActivationIn) =>
        new(source, WorkshopSocket.ActivationOut, target, input, WorkshopConnectionDomain.Activation);
    private static WorkshopConnection Power(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));

    private static ActivationLatch Node(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Activations.Count; i++) if (read.Activations[i].Owner == owner) return read.Activations[i];
        throw new InvalidOperationException($"No activation node for {owner.Value}.");
    }
    private static bool Lit(WorkshopRead read, GpuBodyId owner) => Node(read, owner).Phase == ActivationPhase.Latched;
    private static double Height(WorkshopRead read, GpuBodyId body)
    {
        Assert.True(read.Bodies.TryGet(body, out var row));
        return (row.Body.Cell.Y + (double)row.Body.Local.Y) / 16;
    }
    private static double X(WorkshopRead read, GpuBodyId body)
    {
        Assert.True(read.Bodies.TryGet(body, out var row));
        return (row.Body.Cell.X + (double)row.Body.Local.X) / 16;
    }

    /// <summary>Each distinct committed Passage occurrence of the detector, with the first tick it was committed.</summary>
    private static List<(ulong Tick, uint Ordinal)> Passages(IReadOnlyList<WorkshopRead> reads, GpuBodyId detector)
    {
        var seen = new List<(ulong Tick, uint Ordinal)>();
        foreach (var read in reads)
        {
            var node = Node(read, detector);
            if (node.Phase != ActivationPhase.Latched) continue;
            Assert.Equal(ActivationOccurrenceKind.Passage, node.Kind);
            if (seen.Count == 0 || seen[^1].Ordinal != node.EventOrdinal) seen.Add((read.Tick.Value, node.EventOrdinal));
        }
        return seen;
    }

    /// <summary>The production passage tracker replayed over the committed reads: its crossing count and the ticks it fired.</summary>
    private static (ulong Passed, List<ulong> Fired) Replay(WorkshopConstruction construction, WorkshopRead built, IReadOnlyList<WorkshopRead> reads)
    {
        var network = WorkshopActivationCompiler.Compile(construction);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var tracker = PassageSensorTracker.Create(network.PassageSensors, scene, built.Bodies);
        var outputs = new PassageSensorRead[8];
        var fired = new List<ulong>();
        foreach (var read in reads)
        {
            tracker = tracker.Advance(read.Tick, read.Bodies, Assert.IsType<PhysicsMotionRead>(read.Motion), outputs, out var count);
            for (var i = 0; i < count; i++) fired.Add(read.Tick.Value);
        }
        return ((ulong)tracker.States.Sum(state => (long)state.PassedCount), fired);
    }

    /// <summary>A detector with local +X down (forward is downward) or up, a lamp on its output, and one Basketball dropped at
    /// <paramref name="ballX"/> (the bore centre is x 0).</summary>
    private static (WorkshopConstruction Bench, GpuBodyId Detector, GpuBodyId Lamp, GpuBodyId Ball) DropBench(bool forwardDown, double ballX,
        bool counted = false)
    {
        var sign = forwardDown ? -1 : 1;
        var detector = PlacedParts.Detector(new(1), 0, (float)(Plane), 0, 0, 0, (float)(sign * Half45), (float)(Half45));
        var lamp = PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(3), (float)(ballX), (float)(DropY), 0, 0, 0, 0, 1);
        if (!counted) return (Bench([detector, lamp, ball], Signal(detector.Id, lamp.Id)), detector.Id, lamp.Id, ball.Id);
        var counter = PlacedParts.Counter(new(4), -4, 1, 0, 0, 0, 0, 1, 2);
        var second = PlacedParts.Lamp(new(5), 4, 1, 3, 0, 0, 0, 1);
        var tally = PlacedParts.Counter(Tally, -4, 1, 3, 0, 0, 0, 1, 9);
        return (Bench([detector, lamp, ball, counter, second, tally], Signal(detector.Id, lamp.Id), Signal(detector.Id, counter.Id),
            Signal(counter.Id, second.Id), Signal(detector.Id, tally.Id)), detector.Id, lamp.Id, ball.Id);
    }
    private static readonly GpuBodyId Tally = new(6);

    // ---------- 9R.9: one ball, one run: forward fires, the reverse does not, and a second forward pass after clearance fires once more ----------

    [Fact]
    public async Task OneBallFiresForwardNotInReverseAndFiresAgainAfterClearingUpstream()
    {
        var (bench, detector, lamp, ball) = DropBench(forwardDown: true, ballX: 0, counted: true);
        var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, Ticks);
        var trace = string.Join(" ", reads.Where(read => read.Tick.Value % 10 == 0).Select(read => $"{read.Tick.Value}:{Height(read, ball):F3}"));

        // Every committed passage is one input to the tally counter (target 9): its committed count and last input time.
        var network = WorkshopActivationCompiler.Compile(bench);
        var passages = new List<(ulong Tick, uint Ordinal)>();
        var count = 0U;
        foreach (var read in reads)
        {
            var row = network.Memory(read.Memory, Tally);
            Assert.True(row.Count == count || row.Count == count + 1, $"one passage at most per tick at {read.Tick.Value}");
            if (row.Count == count + 1) passages.Add((read.Tick.Value, row.Last.Ordinal));
            count = row.Count;
        }
        Assert.True(passages.Count == 2, $"two committed passages, got {passages.Count}; heights {trace}");
        var (first, second) = (passages[0], passages[1]);
        Assert.True(first.Ordinal < second.Ordinal);
        // The detector's own committed occurrence is the first passage, dark before it.
        Assert.All(reads, read => Assert.Equal(read.Tick.Value >= first.Tick, Lit(read, detector)));
        Assert.All(reads.Where(read => read.Tick.Value >= first.Tick), read =>
            Assert.Equal((ActivationOccurrenceKind.Passage, first.Ordinal), (Node(read, detector).Kind, Node(read, detector).EventOrdinal)));

        // The first crossing went through the plane, and the ball then came back up through it and cleared the rearm line by more than
        // 0.02 m beyond its radius, with no occurrence; it then fell through again.
        var between = reads.Where(read => read.Tick.Value >= first.Tick && read.Tick.Value < second.Tick).ToList();
        var below = between.FindIndex(read => Height(read, ball) < Plane);
        Assert.True(below >= 0, $"the ball went below the plane after the first passage; heights {trace}");
        var cleared = between.FindIndex(below, read => Height(read, ball) > Rearm);
        Assert.True(cleared > below, $"the reverse pass cleared the upstream rearm line at {Rearm:F3} m; heights {trace}");
        Assert.True(Height(reads.Single(read => read.Tick.Value == second.Tick), ball) < Height(between[cleared], ball));
        Assert.All(reads, read => Assert.True(Math.Abs(X(read, ball)) < .3, $"the ball stays in the bore at tick {read.Tick.Value}"));

        // Cross-check: the production passage tracker replayed over the committed bodies counts exactly two crossings at those ticks.
        var (passed, fired) = Replay(bench, built, reads);
        Assert.Equal(2UL, passed);
        Assert.Equal([first.Tick, second.Tick], fired);

        // The first-input lamp lit on the first passage and keeps that occurrence; the counter (target 2) lit its lamp on the second.
        Assert.All(reads, read => Assert.Equal(read.Tick.Value >= first.Tick, Lit(read, lamp)));
        Assert.All(reads.Where(read => read.Tick.Value >= first.Tick), read =>
        {
            var latch = Node(read, lamp);
            Assert.Equal((Node(reads[0], detector).Node, first.Ordinal, ActivationOccurrenceKind.Passage), (latch.Emitter, latch.EventOrdinal, latch.Kind));
        });
        Assert.All(reads, read => Assert.Equal(read.Tick.Value >= second.Tick, Lit(read, new(5))));

        // Reset restores the built state exactly; the reloaded save keeps its wires and replays the same run.
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        Assert.All(Enumerable.Range(0, reset.Activations.Count), i => Assert.Equal(ActivationPhase.Clear, reset.Activations[i].Phase));
        var saved = new WorkshopSavedConstruction(bench, new(7));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, loaded);
        Assert.Equal(bench.Connections.ToArray(), loaded.Construction.Connections.ToArray());
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(loaded.Construction, Ticks);
        var replayPassages = replay.Where(read => network.Memory(read.Memory, Tally).Count > 0)
            .GroupBy(read => network.Memory(read.Memory, Tally).Count).Select(group => group.First()).ToArray();
        Assert.Equal(2, replayPassages.Length);
        Assert.Equal([1U, 2U], replayPassages.Select(read => network.Memory(read.Memory, Tally).Count));
        Assert.True(replayPassages[0].Tick.Value < replayPassages[1].Tick.Value);
        Assert.All(replay, read => Assert.Equal(read.Tick.Value >= replayPassages[0].Tick.Value, Lit(read, lamp)));
        Assert.All(replay, read => Assert.Equal(read.Tick.Value >= replayPassages[1].Tick.Value, Lit(read, new(5))));
        Assert.Equal(ActivationOccurrenceKind.Passage, Node(replay[^1], detector).Kind);
        Assert.Equal(Node(replay[^1], detector).Node, Node(replay[^1], lamp).Emitter);
        Assert.Contains(replay.Where(read => read.Tick.Value > replayPassages[0].Tick.Value &&
            read.Tick.Value < replayPassages[1].Tick.Value), read => Height(read, ball) > Rearm);

    }

    // ---------- Negative controls: crossings proven by positions on both sides of the plane, yet no occurrence ----------

    [Fact]
    public async Task AGlancingCrossingOutsideTheApertureFiresNothing()
    {
        // Just clear of the collar: its 16 boxes reach 0.82 / cos(π/16) = 0.836 m from the axis at their corners (the drawn
        // 0.82 m collar, elements-visible-solid), so a ball of radius 0.34 centred 1.2 m out falls past them by 2.4 cm.
        const double ballX = -1.2;
        var (bench, detector, lamp, ball) = DropBench(forwardDown: true, ballX: ballX);
        var reads = await WorkshopIntegrationHarness.Observe(bench, Ticks);
        Assert.Contains(reads, read => Height(read, ball) > Rearm);
        Assert.Contains(reads, read => Height(read, ball) < Plane);
        Assert.All(reads, read => Assert.True(Math.Abs(X(read, ball) - ballX) < .05, $"a vertical drop at tick {read.Tick.Value}"));
        Assert.Empty(Passages(reads, detector));
        Assert.All(reads, read => Assert.False(Lit(read, lamp), $"the lamp stays dark at tick {read.Tick.Value}"));
    }

    // The outer collar is solid out to its drawn 0.82 m (elements-visible-solid): a ball dropped 0.9 m out lands on the edge of the
    // collar's upper end face (0.65–0.82 m from the axis), outside the 0.65 m aperture, and is turned aside (its x moves more than 5 cm
    // from its drop line). Its centre stays outside the aperture on every tick, no Passage fires and the lamp stays dark.
    [Fact]
    public async Task ABallStrikingTheOuterCollarFiresNothing()
    {
        const double ballX = -.9, aperture = .65;
        var (bench, detector, lamp, ball) = DropBench(forwardDown: true, ballX: ballX);
        var reads = await WorkshopIntegrationHarness.Observe(bench, Ticks);
        Assert.Contains(reads, read => Math.Abs(X(read, ball) - ballX) > .05);
        Assert.All(reads, read =>
        {
            Assert.True(read.Bodies.TryGet(ball, out var row));
            var z = (row.Body.Cell.Z + (double)row.Body.Local.Z) / 16;
            var x = X(read, ball);
            Assert.True(x * x + z * z > aperture * aperture, $"the ball's centre entered the aperture at tick {read.Tick.Value}");
        });
        Assert.Empty(Passages(reads, detector));
        Assert.All(reads, read => Assert.False(Lit(read, lamp), $"the lamp stays dark at tick {read.Tick.Value}"));
    }

    [Fact]
    public async Task AReverseCrossingThroughTheBoreFiresNothing()
    {
        var (bench, detector, lamp, ball) = DropBench(forwardDown: false, ballX: 0);
        var reads = await WorkshopIntegrationHarness.Observe(bench, Ticks);
        var below = reads.FindIndex(read => Height(read, ball) < Plane);
        Assert.True(below > 0 && Height(reads[0], ball) > Plane, "the ball fell through the bore against its arrow");
        Assert.Empty(Passages(reads, detector));
        Assert.All(reads, read => Assert.False(Lit(read, lamp), $"the lamp stays dark at tick {read.Tick.Value}"));
    }

    // ---------- 9R.16: the lamp lights on its first input and stays lit; fed by a level it follows that level; unwired it stays dark ----------

    [Fact]
    public async Task ASwitchLightsItsLampOnTheFirstInputAndItStaysLitWhileAnUnwiredLampStaysDark()
    {
        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(1), 0, 1, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1);
        var dark = PlacedParts.Lamp(new(3), 4, 1, 3, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(4), 0, 3, 0, 0, 0, 0, 1);
        var bench = Bench([hit, lamp, dark, ball], Signal(hit.Id, lamp.Id));
        var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, Ticks);
        var first = reads.FindIndex(read => Lit(read, lamp.Id));
        Assert.True(first > 0, "the switch lit its lamp");
        var occurrence = Node(reads[first], lamp.Id);
        Assert.All(reads.Skip(first), read => Assert.Equal(occurrence, Node(read, lamp.Id)));
        Assert.All(reads.Take(first), read => Assert.False(Lit(read, lamp.Id)));
        Assert.All(reads, read => Assert.False(Lit(read, dark.Id), $"the unwired lamp stays dark at tick {read.Tick.Value}"));
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        Assert.False(Lit(reset, lamp.Id));
    }

    [Fact]
    public async Task ALampOnALatchFollowsItsLevelOnAndOffWhileAFirstInputLampHolds()
    {
        var setClock = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var resetClock = PlacedParts.Clock(new(2), -3, 3, 0, 0, 0, 0, 1, 1.5f);
        var latch = PlacedParts.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -6, 0, 0);
        var lamp = PlacedParts.Lamp(new(5), 3, 0, 0, 0, 0, 0, 1);
        var sticky = PlacedParts.Lamp(new(6), 3, 3, 0, 0, 0, 0, 1);
        var bench = Bench([setClock, resetClock, latch, battery, lamp, sticky], Power(battery.Id, setClock.Id), Power(battery.Id, resetClock.Id),
            Signal(setClock.Id, latch.Id, WorkshopSocket.SetIn), Signal(resetClock.Id, latch.Id, WorkshopSocket.ResetIn),
            Signal(latch.Id, lamp.Id), Signal(setClock.Id, sticky.Id));
        var network = WorkshopActivationCompiler.Compile(bench);
        var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, 300);
        Assert.All(reads, read => Assert.Equal(network.StateLatchPhase(read.Activations, latch.Id) == SimulationLatchPhase.On, Lit(read, lamp.Id)));
        Assert.Contains(reads, read => Lit(read, lamp.Id));
        var lit = reads.FindIndex(read => Lit(read, lamp.Id));
        Assert.Contains(reads.Skip(lit), read => !Lit(read, lamp.Id)); // it went dark again with the latch
        var held = reads.FindIndex(read => Lit(read, sticky.Id));
        Assert.True(held >= 0);
        Assert.All(reads.Skip(held), read => Assert.True(Lit(read, sticky.Id), $"the first-input lamp holds at tick {read.Tick.Value}"));
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
    }

    [Fact]
    public async Task ALampOnAPlateFollowsItsLevelOnAndOff()
    {
        // A Basketball rests on a plate tilted 30° and then rolls off (the PressurePlateTests removal bench).
        var half = Math.PI / 12;
        const double plateHeight = 2;
        var plate = PlacedParts.Plate(new(3), -4, (float)(plateHeight), 0, .5f, 0, 0, (float)(Math.Sin(half)), (float)(Math.Cos(half)));
        var rotation = new System.Numerics.Quaternion(0, 0, (float)Math.Sin(half), (float)Math.Cos(half));
        // At local x=-.2 the ring centreline is .1 m away. Its y=.084,
        // tube .018 and ball union reach .35 give the conservative contact
        // height .084+sqrt((.35+.018)^2-.1^2)=.4382 m; .448 m clears it.
        var drop = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(-.2f, .08f + .368f, 0), rotation);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(4), (float)(-4 + drop.X), (float)(plateHeight + drop.Y), (float)(drop.Z), 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(5), 6, 3, 0, 0, 0, 0, 1);
        var reads = await WorkshopIntegrationHarness.Observe(Bench([plate, ball, lamp], Signal(plate.Id, lamp.Id)), Ticks);
        Assert.All(reads, read => Assert.Equal(Lit(read, plate.Id), Lit(read, lamp.Id)));
        var lit = reads.FindIndex(read => Lit(read, lamp.Id));
        Assert.True(lit > 0, "the plate's load lit the lamp");
        Assert.Contains(reads.Skip(lit), read => !Lit(read, lamp.Id));
    }

    [Fact]
    public void ALampThatFollowsALevelRefusesASecondSignalAndALevelLightsNoOtherInput()
    {
        var latch = PlacedParts.Latch(new(1), 0, 0, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(2), 3, 0, 0, 0, 0, 0, 1);
        var counter = PlacedParts.Counter(new(3), -3, 0, 0, 0, 0, 0, 1, 3);
        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(4), 0, 3, 0, 0, 0, 0, 1);
        var other = PlacedParts.Latch(new(5), 0, -3, 0, 0, 0, 0, 1);
        var detector = PlacedParts.Detector(new(6), 6, 3, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(latch, lamp, counter, hit, other, detector);
        var level = Signal(latch.Id, lamp.Id);
        new WorkshopConnections(level).Validate(instances);
        new WorkshopConnections(Signal(hit.Id, lamp.Id), Signal(detector.Id, lamp.Id)).Validate(instances); // pulses may share a lamp
        static void Refused(uint ordinal, string message, Action edit)
        {
            var refusal = Assert.Throws<WorkshopEditRejectedException>(edit);
            Assert.Equal((ordinal, message), ((uint)refusal.Reason, refusal.Message));
        }
        const string one = "A lamp that follows a level output takes no other signal.";
        Refused(8, one, () => new WorkshopConnections(level, Signal(hit.Id, lamp.Id)).Validate(instances));
        Refused(8, one, () => new WorkshopConnections(level, Signal(detector.Id, lamp.Id)).Validate(instances));
        Refused(8, one, () => new WorkshopConnections(level, Signal(other.Id, lamp.Id)).Validate(instances));
        Refused(7, "A level output can only light a Signal lamp.", () => new WorkshopConnections(Signal(latch.Id, counter.Id)).Validate(instances));
        // The save boundary refuses the same shape atomically, and a valid level-fed lamp's wire survives Save/Load.
        var valid = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), instances, Connections: new(level));
        Refused(8, one, () => WorkshopSaveCodec.Encode(new(valid with { Connections = new(level, Signal(hit.Id, lamp.Id)) }, new(7))));
        Assert.Equal(level, Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new(valid, new(7))))
            .Construction.Connections.ToArray()));
    }
}
