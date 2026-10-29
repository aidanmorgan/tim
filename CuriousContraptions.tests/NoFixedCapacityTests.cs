using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9R.5b (owner decision, 20 Oct 2026: "the physics body table needs to be infinite"): no physics, worker or wire
/// table has a fixed capacity, and Build sets no part limit. Every table is sized from the compiled scene at Run, so each
/// population one past a retired ceiling compiles, steps through the production engine and crosses the current response codec.</summary>
public sealed class NoFixedCapacityTests
{
    public enum PopulationKind { Instances, Bodies, DynamicBodies, CapturePolicies, Triggers, ContactWork, Electrical, Prismatics, Orientations, ContactLoad, ActivationTimers, Connections }

    private const int SteppedTicks = 30;
    private const int LampTicks = 2 * 120;

    private static WorkshopConstruction Bench(IReadOnlyList<IWorkshopInstance> instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new([.. instances]), Connections: new(links));
    private static WorkshopConnection Signal(GpuBodyId a, GpuBodyId b) =>
        new(a, WorkshopSocket.ActivationOut, b, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConnection Supply(GpuBodyId a, GpuBodyId b) =>
        new(a, WorkshopSocket.Supply, b, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static int Dynamic(PhysicsSceneDeclaration scene) => scene.Bodies.ToArray().Count(b => b.Motion == RigidMotionKind.Dynamic);

    /// <summary>One row of parts along x, 1.6 m apart, so no moving part starts inside a neighbour.</summary>
    private static double X(int i) => -25.6 + i * 1.6;

    /// <summary>A population one past each retired ceiling (base c5336092 values in the names).</summary>
    private static (WorkshopConstruction Bench, Action<PhysicsSceneDeclaration> Check) Population(PopulationKind table)
    {
        ulong id = 0;
        GpuBodyId Next() => new(++id);
        var parts = new List<IWorkshopInstance>();
        var links = new List<WorkshopConnection>();
        void Balls(int count, double z) { for (var i = 0; i < count; i++) parts.Add(WorkshopInput.Generic(new PartId("ball"),Next(), (float)(X(i)), 4, (float)(z), 0, 0, 0, 1)); }
        switch (table)
        {
            case PopulationKind.Instances:
                for (var i = 0; i < 33; i++) parts.Add(WorkshopInput.Generic(new PartId("wall"), Next(), (float)(X(i)), 2, 4, 0, 0, 0, 1, WallFixture.Default));
                parts.Add(WorkshopInput.Generic(new PartId("ball"), Next(), 0, 4, 0, 0, 0, 0, 1));
                return (Bench(parts), scene => Assert.Equal(35, scene.Bodies.Length));
            case PopulationKind.Bodies:
                // Seventeen Powered Gates: 35 bodies from 17 parts, below the retired part table.
                for (var i = 0; i < 17; i++)
                    parts.Add(WorkshopInput.Generic(new PartId("powered_gate"), Next(), (float)(X(i)), 0, 0, 0, 0, 0, 1));
                return (Bench(parts), scene => Assert.Equal(35, scene.Bodies.Length));
            case PopulationKind.DynamicBodies:
                Balls(17, 0);
                return (Bench(parts), scene => Assert.Equal(17, Dynamic(scene)));
            case PopulationKind.CapturePolicies:
                Balls(17, 0);
                for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(new PartId("basket"), Next(), (float)(X(i)), 1, 4, 0, 0, 0, 1));
                return (Bench(parts), scene => { Assert.Equal(17, scene.Sensors.Length); Assert.Equal(17, scene.Guides.Length); });
            case PopulationKind.Triggers:
                for (var i = 0; i < 9; i++) parts.Add(WorkshopInput.Generic(new PartId("switch"), Next(), (float)(X(i)), 1, 0, 0, 0, 0, 1));
                return (Bench(parts), scene => Assert.Equal(9, scene.Triggers.Length));
            case PopulationKind.ContactWork:
                // Nine owner policies and seventeen potential payloads; no Cartesian occurrence reservation.
                for (var i = 0; i < 9; i++) parts.Add(WorkshopInput.Generic(new PartId("bumper"), Next(), (float)(X(i)), 1, 0, 0, 0, 0, 1));
                Balls(17, 0);
                return (Bench(parts), scene => Assert.Equal(9, scene.ContactWorks.Length));
            case PopulationKind.Electrical:
                // Nine Batteries, each wired to its own Powered Gate: nine sources, nine routes and nine connections.
                for (var i = 0; i < 9; i++)
                {
                    var gate = WorkshopInput.Generic(new PartId("powered_gate"), Next(), (float)(X(2 * i)), 0, 0, 0, 0, 0, 1);
                    var battery = PlacedParts.Battery(Next(), (float)(X(2 * i) + 1.6), 0, 0, enabled: ElectricalEnable.Enabled);
                    parts.AddRange([gate, battery]); links.Add(Supply(battery.Id, gate.Id));
                }
                return (Bench(parts, [.. links]), scene => { Assert.Equal(9, scene.Electrical.Sources.Length); Assert.Equal(9, scene.SupplyGraph.Nodes.ToArray().Count(node=>node.Kind==SupplyNodeKind.Consumer && !node.Consumers.IsDefaultOrEmpty)); });
            case PopulationKind.Prismatics:
                for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(new PartId("spring"), Next(), (float)(X(i)), 1, 0, 0, 0, 0, 1));
                return (Bench(parts), scene => Assert.Equal(17, scene.Prismatics.Length));
            case PopulationKind.Orientations:
                // A Domino declares its orientation sensor only while wired: each of seventeen drives its own lamp.
                for (var i = 0; i < 17; i++)
                {
                    var tile = PlacedParts.Domino(Next(), (float)(X(i)), (float)(-0.46 + .56), 0, 0, 0, 0, 1);
                    var lamp = PlacedParts.Lamp(Next(), (float)(X(i)), 1, 3, 0, 0, 0, 1);
                    parts.AddRange([tile, lamp]); links.Add(Signal(tile.Id, lamp.Id));
                }
                return (Bench(parts, [.. links]), scene => Assert.Equal(17, scene.OrientationSensors.Length));
            case PopulationKind.ContactLoad:
                for (var i = 0; i < 33; i++)
                    parts.Add(PlacedParts.Plate(Next(), (float)(X(i)), 1, 0, .5f));
                return (Bench(parts), scene => Assert.Equal(33, scene.ContactLoadSensors.Length));
            case PopulationKind.ActivationTimers:
                for (var i = 0; i < 33; i++) parts.Add(PlacedParts.Clock(Next(), (float)(X(i)), 1, 0, 0, 0, 0, 1, 1f));
                return (Bench(parts), _ => Assert.Equal(33, WorkshopActivationCompiler.Compile(Bench(parts)).Nodes.Length));
            case PopulationKind.Connections:
                for (var i = 0; i < 9; i++)
                {
                    var hit = WorkshopInput.Generic(new PartId("switch"), Next(), (float)(X(2 * i)), 1, 0, 0, 0, 0, 1);
                    var lamp = PlacedParts.Lamp(Next(), (float)(X(2 * i) + 1.6), 1, 0, 0, 0, 0, 1);
                    parts.AddRange([hit, lamp]); links.Add(Signal(hit.Id, lamp.Id));
                }
                return (Bench(parts, [.. links]), _ => Assert.Equal(9, links.Count));
            default: throw new ArgumentException($"Unknown table '{table}'.", nameof(table));
        }
    }

    public static TheoryData<PopulationKind> Tables => new(
        PopulationKind.Instances, PopulationKind.Bodies, PopulationKind.DynamicBodies, PopulationKind.CapturePolicies, PopulationKind.Triggers, PopulationKind.ContactWork, PopulationKind.Electrical,
        PopulationKind.Prismatics, PopulationKind.Orientations, PopulationKind.ContactLoad, PopulationKind.ActivationTimers, PopulationKind.Connections);

    [Theory]
    [MemberData(nameof(Tables))]
    public async Task OnePastEachRetiredCeilingCompilesStepsAndCrossesResponseCodec(PopulationKind table)
    {
        var (bench, check) = Population(table);
        bench.Validate();
        check(WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
        var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, SteppedTicks);
        if (table == PopulationKind.Instances)
        {
            Assert.True(reads[^1].Bodies.TryGet(new(34), out var moved));
            Assert.True(moved.Body.Velocity.Y < -.1f && (moved.Body.Cell.Y + moved.Body.Local.Y) / 16f < 4,
                "The beyond-capacity scene must actually move a payload before Reset.");
        }
        AssertSameState(built, reset, "beyond-capacity Reset");
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
        AssertCrossesResponseCodec(reads[^1]);
    }

    /// <summary>The committed read crosses the worker boundary intact through the current response codec.</summary>
    private static void AssertCrossesResponseCodec(WorkshopRead read)
    {
        var response = new WorkshopResponse(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, new(1, 2), new(1), new(1), read.Capture!.Value.Generation, new(1), new(read.Tick.Value));
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response));
        var actual = decoded.Read;
        AssertSameState(read, actual, "decoded committed read");
        Assert.Equal(read.Epoch, actual.Epoch);
        Assert.Equal(read.Revision, actual.Revision);
        Assert.Equal(read.Capture, actual.Capture);
        Assert.Equal(read.Presentation.Actuators.ToArray(), actual.Presentation.Actuators.ToArray());
        Assert.Equal(read.Presentation.Loads.ToArray(), actual.Presentation.Loads.ToArray());
        Assert.Equal(read.Presentation.Logic.ToArray(), actual.Presentation.Logic.ToArray());

    }

    // ---------- The proof bench: forty Impact Switches (Build refused the ninth), one Signal lamp, one ball ----------

    private const int Switches = 40;
    private const double SwitchY = 1, DropY = 3;

    /// <summary>Forty switches in an eight-by-five grid, a lamp wired to the last one, and a ball dropped on <paramref name="struck"/>.</summary>
    private static (WorkshopConstruction Bench, IWorkshopInstance Lamp, WorkshopGenericInstance[] Switches) SwitchBench(int struck)
    {
        var switches = Enumerable.Range(0, Switches).Select(i => WorkshopInput.Generic(new PartId("switch"), new((ulong)i + 1), (float)(-6 + i % 8 * 1.6), (float)(SwitchY),
            (float)(-5 + i / 8 * 2.5), 0, 0, 0, 1)).ToArray();
        var lamp = PlacedParts.Lamp(new(Switches + 1), 8, (float)(SwitchY), 0, 0, 0, 0, 1);
        var target = switches[struck];
        var ball = WorkshopInput.Generic(new PartId("ball"),new(Switches + 2), (float)((target.Cell.X + (double)target.Local.X) / 16), (float)(DropY),
            (float)((target.Cell.Z + (double)target.Local.Z) / 16), 0, 0, 0, 1);
        return (Bench([.. switches, lamp, ball], Signal(switches[^1].Id, lamp.Id)), lamp, switches);
    }

    private static bool Latched(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Activations.Count; i++)
            if (read.Activations[i].Owner == owner) return read.Activations[i].Phase == ActivationPhase.Latched;
        throw new InvalidOperationException($"No activation node for {owner.Value}.");
    }

    [Fact]
    public void FortyImpactSwitchesBuildAndCompileFortyTriggers()
    {
        var (bench, _, _) = SwitchBench(Switches - 1);
        bench.Validate();
        Assert.Equal(Switches + 2, bench.Instances.Count);
        Assert.Equal(Switches, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).Triggers.Length);
    }

    [Theory]
    [InlineData(Switches - 1, true)]
    [InlineData(0, false)]
    public async Task TheLastOfFortySwitchesFiresItsOwnLampAndAnUnwiredSwitchDoesNot(int struck, bool lit)
    {
        var (bench, lamp, _) = SwitchBench(struck);
        var reads = await WorkshopIntegrationHarness.Observe(bench, LampTicks);
        Assert.False(Latched(reads[0], lamp.Id), "the lamp starts dark");
        Assert.Equal(lit, Latched(reads[^1], lamp.Id));
    }

    [Fact]
    public async Task TheFortySwitchBenchResetsExactlyAndSurvivesSaveAndLoad()
    {
        var (bench, lamp, _) = SwitchBench(Switches - 1);
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, LampTicks);
        Assert.True(Latched(run[^1], lamp.Id), "the last switch lit its lamp");
        AssertSameState(built, reset, "Reset");
        AssertCrossesResponseCodec(run[^1]);

        var saved = new WorkshopSavedConstruction(bench, new(Switches + 3));
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, reloaded);
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(reloaded.Construction, LampTicks);
        Assert.False(Latched(replay[0], lamp.Id));
        Assert.True(Latched(replay[^1], lamp.Id), "the loaded last switch lights its own lamp");
    }

    // ---------- Every retired ceiling exceeded at once ----------

    /// <summary>One machine past every retired ceiling together: 17 balls and 17 Receivers (one sensor and guide policy per owner), 9 switches
    /// wired to 9 lamps, 9 bumpers, 9 battery-gate circuits, 17 springboards, 17 dominoes each wired to its lamp, 33 plates and 33 clocks.</summary>
    private static WorkshopConstruction Everything()
    {
        ulong id = 0;
        GpuBodyId Next() => new(++id);
        var parts = new List<IWorkshopInstance>();
        var links = new List<WorkshopConnection>();
        for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(new PartId("ball"),Next(), (float)(X(i)), 4, -20, 0, 0, 0, 1));
        for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(new PartId("basket"), Next(), (float)(X(i)), 1, -24, 0, 0, 0, 1));
        for (var i = 0; i < 9; i++)
        {
            var hit = WorkshopInput.Generic(new PartId("switch"), Next(), (float)(X(2 * i)), 1, -16, 0, 0, 0, 1);
            var lamp = PlacedParts.Lamp(Next(), (float)(X(2 * i) + 1.6), 1, -16, 0, 0, 0, 1);
            parts.AddRange([hit, lamp]); links.Add(Signal(hit.Id, lamp.Id));
        }
        for (var i = 0; i < 9; i++) parts.Add(WorkshopInput.Generic(new PartId("bumper"), Next(), (float)(X(i)), 1, -12, 0, 0, 0, 1));
        for (var i = 0; i < 9; i++)
        {
            var gate = WorkshopInput.Generic(new PartId("powered_gate"), Next(), (float)(X(2 * i)), 0, -8, 0, 0, 0, 1);
            var battery = PlacedParts.Battery(Next(), (float)(X(2 * i) + 1.6), 0, -8, enabled: ElectricalEnable.Enabled);
            parts.AddRange([gate, battery]); links.Add(Supply(battery.Id, gate.Id));
        }
        for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(new PartId("spring"), Next(), (float)(X(i)), 1, -4, 0, 0, 0, 1));
        for (var i = 0; i < 17; i++)
        {
            var tile = PlacedParts.Domino(Next(), (float)(X(i)), (float)(-0.46 + .56), 0, 0, 0, 0, 1);
            var lamp = PlacedParts.Lamp(Next(), (float)(X(i)), 1, 2, 0, 0, 0, 1);
            parts.AddRange([tile, lamp]); links.Add(Signal(tile.Id, lamp.Id));
        }
        for (var i = 0; i < 33; i++)
            parts.Add(PlacedParts.Plate(Next(), (float)(X(i)), 1, 4, .5f));
        for (var i = 0; i < 33; i++) parts.Add(PlacedParts.Clock(Next(), (float)(X(i)), 1, 8, 0, 0, 0, 1, 1f));
        return Bench(parts, [.. links]);
    }

    [Fact]
    public async Task AMachinePastEveryRetiredCeilingRunsResetsExactlyAndSurvivesSaveAndLoad()
    {
        var bench = Everything();
        bench.Validate();
        Assert.Equal(17 + 17 + 18 + 9 + 18 + 17 + 34 + 33 + 33, bench.Instances.Count);
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.True(Dynamic(scene) > 16 && scene.Bodies.Length > 33 && scene.Triggers.Length > 8 && scene.ContactWorks.Length > 8 &&
            scene.Prismatics.Length > 16 && scene.Sensors.Length > 16 && scene.Guides.Length > 16 &&
            scene.OrientationSensors.Length > 16 && scene.ContactLoadSensors.Length > 32 && scene.Electrical.Sources.Length > 8,
            "every retired ceiling is exceeded");

        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, SteppedTicks);
        AssertSameState(built, reset, "Reset");
        AssertCrossesResponseCodec(run[^1]);
        var saved = new WorkshopSavedConstruction(bench, new((ulong)bench.Instances.Count + 1));
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, reloaded);
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(reloaded.Construction, SteppedTicks);
        Assert.Equal(built.Bodies.Count, replay[^1].Bodies.Count);
        for (ulong id = 1; id <= 17; id++)
        {
            Assert.True(replay[^1].Bodies.TryGet(new(id), out var ball));
            Assert.True(ball.Body.Velocity.Y < -.1f && (ball.Body.Cell.Y + ball.Body.Local.Y) / 16f < 4,
                $"loaded ball {id} falls despite all populations exceeding their former limits");
        }
        AssertCrossesResponseCodec(replay[^1]);
    }

    /// <summary>The production Simulation on the worker's transport: Construct, Run, `ticks` Advances, Reset (Story 9R.6 reuses it).</summary>
    /// <summary>Equal observable committed fields, the world epoch aside (the Story 9R.1 snapshot fields).</summary>
    internal static void AssertSameState(WorkshopRead expected, WorkshopRead actual, string label)
    {
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Bodies.Count, actual.Bodies.Count);
        for (var i = 0; i < expected.Bodies.Count; i++)
        {
            var (e, a) = (expected.Bodies[i], actual.Bodies[i]);
            Assert.True(e.Body.Id == a.Body.Id && e.Body.Tick == a.Body.Tick && e.Body.Cell == a.Body.Cell &&
                e.Body.Local == a.Body.Local && e.Body.Velocity == a.Body.Velocity &&
                e.Rotation == a.Rotation && e.AngularVelocity == a.AngularVelocity, $"{label}: body {e.Body.Id.Value} differs");
        }
        Assert.Equal(expected.Captures.Count, actual.Captures.Count);
        for (var i = 0; i < expected.Captures.Count; i++) Assert.Equal(expected.Captures[i], actual.Captures[i]);
        Assert.Equal(expected.Activations.Count, actual.Activations.Count);
        for (var i = 0; i < expected.Activations.Count; i++) Assert.Equal(expected.Activations[i], actual.Activations[i]);
        Assert.Equal(expected.Timers.Count, actual.Timers.Count);
        for (var i = 0; i < expected.Timers.Count; i++) Assert.Equal(expected.Timers[i], actual.Timers[i]);
        Assert.Equal(expected.ContactWorks.Count, actual.ContactWorks.Count);
        for (var i = 0; i < expected.ContactWorks.Count; i++) Assert.Equal(expected.ContactWorks[i], actual.ContactWorks[i]);
        Assert.Equal(expected.Memory.Count, actual.Memory.Count);
        for (var i = 0; i < expected.Memory.Count; i++) Assert.Equal(expected.Memory[i], actual.Memory[i]);
        Assert.Equal(expected.Electrical.Count, actual.Electrical.Count);
        for (var i = 0; i < expected.Electrical.Count; i++) Assert.Equal(expected.Electrical[i], actual.Electrical[i]);
    }
}
