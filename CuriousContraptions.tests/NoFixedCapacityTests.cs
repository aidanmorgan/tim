using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9R.5b (owner decision, 20 Oct 2026: "the physics body table needs to be infinite"): no physics, worker or wire
/// table has a fixed capacity, and Build sets no part limit. Every table is sized from the compiled scene at Run, so each
/// population one past a retired ceiling compiles, steps through the production engine and crosses both response codecs.</summary>
public sealed class NoFixedCapacityTests
{
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
    private static (WorkshopConstruction Bench, Action<PhysicsSceneDeclaration> Check) Population(string table)
    {
        ulong id = 0;
        GpuBodyId Next() => new(++id);
        var parts = new List<IWorkshopInstance>();
        var links = new List<WorkshopConnection>();
        void Balls(int count, double z) { for (var i = 0; i < count; i++) parts.Add(WorkshopInput.Generic(PartId.Basketball,Next(), X(i), 4, z, 0, 0, 0, 1)); }
        switch (table)
        {
            case "instances-32":
                for (var i = 0; i < 33; i++) parts.Add(WorkshopInput.Generic(PartId.Wall, Next(), X(i), 2, 4, 0, 0, 0, 1, WallFixture.Default));
                return (Bench(parts), scene => Assert.Equal(34, scene.Bodies.Length));
            case "bodies-33":
                // Seventeen Powered Gates: 35 bodies from 17 parts, below the retired part table.
                for (var i = 0; i < 17; i++)
                    parts.Add(WorkshopInput.Generic(PartId.PoweredGate, Next(), X(i), 0, 0, 0, 0, 0, 1));
                return (Bench(parts), scene => Assert.Equal(35, scene.Bodies.Length));
            case "dynamic-16":
                Balls(17, 0);
                return (Bench(parts), scene => Assert.Equal(17, Dynamic(scene)));
            case "sensors-guides-16":
                Balls(17, 0);
                parts.Add(WorkshopInput.Generic(PartId.Receiver, Next(), 0, 1, 4, 0, 0, 0, 1));
                return (Bench(parts), scene => { Assert.Equal(17, scene.Sensors.Length); Assert.Equal(17, scene.Guides.Length); });
            case "triggers-8":
                for (var i = 0; i < 9; i++) parts.Add(WorkshopInput.Generic(PartId.ImpactSwitch, Next(), X(i), 1, 0, 0, 0, 0, 1));
                return (Bench(parts), scene => Assert.Equal(9, scene.Triggers.Length));
            case "contact-work-8":
                // Nine bumpers under seventeen balls: 9 x 17 = 153 occurrence rows, past the retired 128.
                for (var i = 0; i < 9; i++) parts.Add(WorkshopInput.Generic(PartId.PinballBumper, Next(), X(i), 1, 0, 0, 0, 0, 1));
                Balls(17, 0);
                return (Bench(parts), scene => Assert.Equal(9, scene.ContactWorks.Length));
            case "electrical-8":
                // Nine Batteries, each wired to its own Powered Gate: nine sources, nine routes and nine connections.
                for (var i = 0; i < 9; i++)
                {
                    var gate = WorkshopInput.Generic(PartId.PoweredGate, Next(), X(2 * i), 0, 0, 0, 0, 0, 1);
                    var battery = WorkshopInput.Battery(Next(), X(2 * i) + 1.6, 0, 0, 0, 0, 0, 1,
                        ElectricalSourceSettings.Default with { Enabled = ElectricalEnable.Enabled });
                    parts.AddRange([gate, battery]); links.Add(Supply(battery.Id, gate.Id));
                }
                return (Bench(parts, [.. links]), scene => { Assert.Equal(9, scene.Electrical.Sources.Length); Assert.Equal(9, scene.SupplyGraph.Nodes.ToArray().Count(node=>node.Kind==SupplyNodeKind.Consumer && node.Consumer!=default)); });
            case "prismatics-16":
                for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(PartId.Springboard, Next(), X(i), 1, 0, 0, 0, 0, 1));
                return (Bench(parts), scene => Assert.Equal(17, scene.Prismatics.Length));
            case "orientation-16":
                // A Domino declares its orientation sensor only while wired: each of seventeen drives its own lamp.
                for (var i = 0; i < 17; i++)
                {
                    var tile = WorkshopInput.Domino(Next(), X(i), -0.46 + .56, 0, 0, 0, 0, 1);
                    var lamp = WorkshopInput.Lamp(Next(), X(i), 1, 3, 0, 0, 0, 1);
                    parts.AddRange([tile, lamp]); links.Add(Signal(tile.Id, lamp.Id));
                }
                return (Bench(parts, [.. links]), scene => Assert.Equal(17, scene.OrientationSensors.Length));
            case "contact-load-32":
                for (var i = 0; i < 33; i++)
                    parts.Add(WorkshopInput.PressurePlate(Next(), X(i), 1, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass));
                return (Bench(parts), scene => Assert.Equal(33, scene.ContactLoadSensors.Length));
            case "activation-timers-32":
                for (var i = 0; i < 33; i++) parts.Add(WorkshopInput.Clock(Next(), X(i), 1, 0, 0, 0, 0, 1, WorkshopClock.PlacedIntervalSeconds));
                return (Bench(parts), _ => Assert.Equal(33, WorkshopActivationCompiler.Compile(Bench(parts)).Nodes.Length));
            case "connections-8":
                for (var i = 0; i < 9; i++)
                {
                    var hit = WorkshopInput.Generic(PartId.ImpactSwitch, Next(), X(2 * i), 1, 0, 0, 0, 0, 1);
                    var lamp = WorkshopInput.Lamp(Next(), X(2 * i) + 1.6, 1, 0, 0, 0, 0, 1);
                    parts.AddRange([hit, lamp]); links.Add(Signal(hit.Id, lamp.Id));
                }
                return (Bench(parts, [.. links]), _ => Assert.Equal(9, links.Count));
            default: throw new ArgumentException($"Unknown table '{table}'.", nameof(table));
        }
    }

    public static TheoryData<string> Tables => new(
        "instances-32", "bodies-33", "dynamic-16", "sensors-guides-16", "triggers-8", "contact-work-8", "electrical-8",
        "prismatics-16", "orientation-16", "contact-load-32", "activation-timers-32", "connections-8");

    [Theory]
    [MemberData(nameof(Tables))]
    public async Task OnePastEachRetiredCeilingCompilesStepsAndCrossesBothResponseCodecs(string table)
    {
        var (bench, check) = Population(table);
        bench.Validate();
        check(WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
        var reads = await WorkshopPhysicsEngineTests.Run(bench, SteppedTicks);
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
        AssertCrossesBothCodecs(reads[^1]);
    }

    /// <summary>The committed read crosses the worker boundary intact through both response codecs.</summary>
    private static void AssertCrossesBothCodecs(WorkshopRead read)
    {
        var response = new WorkshopResponse(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, new(1, 2), new(1), new(1), read.Capture!.Value.Generation, new(1), new(read.Tick.Value));
        foreach (var (codec, decoded) in new[] { ("binary", WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response))),
            ("FlatBuffers", WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response))) })
        {
            var actual = decoded.Read;
            Assert.True(read.Bodies.HasSameBits(actual.Bodies), $"{codec}: bodies differ");
            Assert.True(read.ContactWorks.HasSameBits(actual.ContactWorks), $"{codec}: contact work differs");
            Assert.True(read.Electrical.HasSameBits(actual.Electrical), $"{codec}: electrical state differs");
            Assert.True(read.Memory.HasSameBits(actual.Memory), $"{codec}: memory rows differ");
            Assert.Equal(read.Captures.Count, actual.Captures.Count);
            Assert.Equal(read.Activations.Count, actual.Activations.Count);
            for (var i = 0; i < read.Activations.Count; i++) Assert.Equal(read.Activations[i], actual.Activations[i]);
            Assert.Equal(read.Timers.Count, actual.Timers.Count);
            for (var i = 0; i < read.Timers.Count; i++) Assert.Equal(read.Timers[i], actual.Timers[i]);
            Assert.True(read.HasSameContent(actual), $"{codec}: the decoded read differs from the committed read");
        }
    }

    // ---------- The proof bench: forty Impact Switches (Build refused the ninth), one Signal lamp, one ball ----------

    private const int Switches = 40;
    private const double SwitchY = 1, DropY = 3;

    /// <summary>Forty switches in an eight-by-five grid, a lamp wired to the last one, and a ball dropped on <paramref name="struck"/>.</summary>
    private static (WorkshopConstruction Bench, WorkshopLamp Lamp, WorkshopGenericInstance[] Switches) SwitchBench(int struck)
    {
        var switches = Enumerable.Range(0, Switches).Select(i => WorkshopInput.Generic(PartId.ImpactSwitch, new((ulong)i + 1), -6 + i % 8 * 1.6, SwitchY,
            -5 + i / 8 * 2.5, 0, 0, 0, 1)).ToArray();
        var lamp = WorkshopInput.Lamp(new(Switches + 1), 8, SwitchY, 0, 0, 0, 0, 1);
        var target = switches[struck];
        var ball = WorkshopInput.Generic(PartId.Basketball,new(Switches + 2), (target.Cell.X + (double)target.Local.X) / 16, DropY,
            (target.Cell.Z + (double)target.Local.Z) / 16, 0, 0, 0, 1);
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
        var reads = await WorkshopPhysicsEngineTests.Run(bench, LampTicks);
        Assert.False(Latched(reads[0], lamp.Id), "the lamp starts dark");
        Assert.Equal(lit, Latched(reads[^1], lamp.Id));
    }

    [Fact]
    public async Task TheFortySwitchBenchResetsExactlyAndSurvivesSaveAndLoad()
    {
        var (bench, lamp, _) = SwitchBench(Switches - 1);
        var (built, run, reset) = await RunAndReset(bench, LampTicks);
        Assert.True(Latched(run[^1], lamp.Id), "the last switch lit its lamp");
        AssertSameState(built, reset, "Reset");
        AssertCrossesBothCodecs(run[^1]);

        var saved = new WorkshopSavedConstruction(bench, new(Switches + 3));
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, reloaded);
        var (_, replay, _) = await RunAndReset(reloaded.Construction, LampTicks);
        AssertSameState(run[^1], replay[^1], "the reloaded bench's run");
    }

    // ---------- Every retired ceiling exceeded at once ----------

    /// <summary>One machine past every retired ceiling together: 17 balls and a Receiver (17 sensors and guides), 9 switches
    /// wired to 9 lamps, 9 bumpers, 9 battery-gate circuits, 17 springboards, 17 dominoes each wired to its lamp, 33 plates and 33 clocks.</summary>
    private static WorkshopConstruction Everything()
    {
        ulong id = 0;
        GpuBodyId Next() => new(++id);
        var parts = new List<IWorkshopInstance>();
        var links = new List<WorkshopConnection>();
        for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(PartId.Basketball,Next(), X(i), 4, -20, 0, 0, 0, 1));
        parts.Add(WorkshopInput.Generic(PartId.Receiver, Next(), 0, 1, -24, 0, 0, 0, 1));
        for (var i = 0; i < 9; i++)
        {
            var hit = WorkshopInput.Generic(PartId.ImpactSwitch, Next(), X(2 * i), 1, -16, 0, 0, 0, 1);
            var lamp = WorkshopInput.Lamp(Next(), X(2 * i) + 1.6, 1, -16, 0, 0, 0, 1);
            parts.AddRange([hit, lamp]); links.Add(Signal(hit.Id, lamp.Id));
        }
        for (var i = 0; i < 9; i++) parts.Add(WorkshopInput.Generic(PartId.PinballBumper, Next(), X(i), 1, -12, 0, 0, 0, 1));
        for (var i = 0; i < 9; i++)
        {
            var gate = WorkshopInput.Generic(PartId.PoweredGate, Next(), X(2 * i), 0, -8, 0, 0, 0, 1);
            var battery = WorkshopInput.Battery(Next(), X(2 * i) + 1.6, 0, -8, 0, 0, 0, 1,
                ElectricalSourceSettings.Default with { Enabled = ElectricalEnable.Enabled });
            parts.AddRange([gate, battery]); links.Add(Supply(battery.Id, gate.Id));
        }
        for (var i = 0; i < 17; i++) parts.Add(WorkshopInput.Generic(PartId.Springboard, Next(), X(i), 1, -4, 0, 0, 0, 1));
        for (var i = 0; i < 17; i++)
        {
            var tile = WorkshopInput.Domino(Next(), X(i), -0.46 + .56, 0, 0, 0, 0, 1);
            var lamp = WorkshopInput.Lamp(Next(), X(i), 1, 2, 0, 0, 0, 1);
            parts.AddRange([tile, lamp]); links.Add(Signal(tile.Id, lamp.Id));
        }
        for (var i = 0; i < 33; i++)
            parts.Add(WorkshopInput.PressurePlate(Next(), X(i), 1, 4, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass));
        for (var i = 0; i < 33; i++) parts.Add(WorkshopInput.Clock(Next(), X(i), 1, 8, 0, 0, 0, 1, WorkshopClock.PlacedIntervalSeconds));
        return Bench(parts, [.. links]);
    }

    [Fact]
    public async Task AMachinePastEveryRetiredCeilingRunsResetsExactlyAndSurvivesSaveAndLoad()
    {
        var bench = Everything();
        bench.Validate();
        Assert.Equal(17 + 1 + 18 + 9 + 18 + 17 + 34 + 33 + 33, bench.Instances.Count);
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.True(Dynamic(scene) > 16 && scene.Bodies.Length > 33 && scene.Triggers.Length > 8 && scene.ContactWorks.Length > 8 &&
            scene.Prismatics.Length > 16 && scene.Sensors.Length > 16 && scene.Guides.Length > 16 &&
            scene.OrientationSensors.Length > 16 && scene.ContactLoadSensors.Length > 32 && scene.Electrical.Sources.Length > 8,
            "every retired ceiling is exceeded");

        var (built, run, reset) = await RunAndReset(bench, SteppedTicks);
        AssertSameState(built, reset, "Reset");
        AssertCrossesBothCodecs(run[^1]);
        var saved = new WorkshopSavedConstruction(bench, new((ulong)bench.Instances.Count + 1));
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, reloaded);
        var (_, replay, _) = await RunAndReset(reloaded.Construction, SteppedTicks);
        AssertSameState(run[^1], replay[^1], "the reloaded machine's run");
    }

    /// <summary>The production Simulation on the worker's transport: Construct, Run, `ticks` Advances, Reset (Story 9R.6 reuses it).</summary>
    internal static async Task<(WorkshopRead Built, List<WorkshopRead> Run, WorkshopRead Reset)> RunAndReset(WorkshopConstruction construction, int ticks)
    {
        await using var device = new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        var built = simulation.Committed;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        var reads = new List<WorkshopRead>(ticks);
        for (var tick = 0; tick < ticks; tick++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            reads.Add(simulation.Committed);
        }
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        return (built, reads, simulation.Committed);
    }

    /// <summary>Bit-equal committed state, the world epoch aside (the Story 9R.1 snapshot fields).</summary>
    internal static void AssertSameState(WorkshopRead expected, WorkshopRead actual, string label)
    {
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Bodies.Count, actual.Bodies.Count);
        for (var i = 0; i < expected.Bodies.Count; i++)
        {
            var (e, a) = (expected.Bodies[i], actual.Bodies[i]);
            Assert.True(e.Body.Id == a.Body.Id && e.Body.Tick == a.Body.Tick && e.Body.Cell == a.Body.Cell &&
                F32Bits.Equal(e.Body.Local, a.Body.Local) && e.Body.Velocity.HasSameBits(a.Body.Velocity) &&
                e.Rotation == a.Rotation && e.AngularVelocity.HasSameBits(a.AngularVelocity), $"{label}: body {e.Body.Id.Value} differs");
        }
        Assert.Equal(expected.Captures.Count, actual.Captures.Count);
        for (var i = 0; i < expected.Captures.Count; i++) Assert.Equal(expected.Captures[i], actual.Captures[i]);
        Assert.Equal(expected.Activations.Count, actual.Activations.Count);
        for (var i = 0; i < expected.Activations.Count; i++) Assert.Equal(expected.Activations[i], actual.Activations[i]);
        Assert.Equal(expected.Timers.Count, actual.Timers.Count);
        for (var i = 0; i < expected.Timers.Count; i++) Assert.Equal(expected.Timers[i], actual.Timers[i]);
        Assert.True(expected.ContactWorks.HasSameBits(actual.ContactWorks), $"{label}: contact work differs");
        Assert.True(expected.Memory.HasSameBits(actual.Memory), $"{label}: memory rows differ");
        Assert.True(expected.Electrical.HasSameBits(actual.Electrical), $"{label}: electrical state differs");
    }
}
