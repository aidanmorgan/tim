using System.Security.Cryptography;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Lamp+Detector (Stories 9A.36 and 9A.37): the Signal lamp and the Ball detector are generic instances whose nodes are
/// selected by their packages' declared capabilities: the lamp's indicator input and the detector's passage aperture. Each compile
/// law is one shared helper (<see cref="WorkshopActivationCompiler.IndicatorKind"/>, <see cref="WorkshopActivationCompiler.PassageSensorFor"/>)
/// that the production compile calls, so a synthetic declaration (a second level input, another aperture) is observable through
/// it. Lamp and detector saves written by the record-path encoder at 54ffd9f0 load as the same generic instances at both decode
/// boundaries; since 9A.32's slot renumbering, one holding a counter is refused with the typed reason.</summary>
public sealed class DeclaredIndicatorPassageTests
{
    private static readonly double H = Math.Sqrt(.5);
    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target, WorkshopSocket input = WorkshopSocket.ActivationIn) =>
        new(source, WorkshopSocket.ActivationOut, target, input, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));
    private static ActivationNodeDeclaration Node(ActivationNetwork network, GpuBodyId owner) =>
        Assert.Single(network.Nodes.ToArray(), node => node.Owner == owner);

    // ---------- selection by declaration ----------

    // The placed lamp and detector are their packages' generic instances with no parameters; the compiler selects their nodes from
    // the declarations: an unwired or pulse-fed lamp holds its first input, a level-fed one follows the level, and the detector compiles
    // one passage sensor from its aperture. Control: a Wall (a generic instance with no signal capability) compiles no node.
    [Fact]
    public void TheLampAndDetectorCompileTheNodesTheirDeclarationsSelect()
    {
        var lamp = PlacedParts.Lamp(new(1), 3, 1, 0, 0, 0, 0, 1);
        var fed = PlacedParts.Lamp(new(2), 3, 4, 0, 0, 0, 0, 1);
        var level = PlacedParts.Lamp(new(3), 3, 7, 0, 0, 0, 0, 1);
        var detector = PlacedParts.Detector(new(4), -3, 1, 0, 0, 0, -H, H);
        var latch = PlacedParts.Latch(new(5), -6, 1, 0, 0, 0, 0, 1);
        var wall = WorkshopInput.Generic(PartId.Wall, new(6), 9, 1, 0, 0, 0, 0, 1);
        foreach (var part in new[] { lamp, fed, level, detector })
            Assert.Empty(Assert.IsType<WorkshopGenericInstance>(part).Parameters);
        var bench = Bench([lamp, fed, level, detector, latch, wall], Signal(detector.Id, fed.Id), Signal(latch.Id, level.Id));
        var network = WorkshopActivationCompiler.Compile(bench);
        Assert.Equal(ActivationNodeKind.StickyFirstInput, Node(network, lamp.Id).Kind);
        Assert.Equal(ActivationNodeKind.StickyFirstInput, Node(network, fed.Id).Kind);
        Assert.Equal(ActivationNodeKind.LevelIndicator, Node(network, level.Id).Kind);
        var sensor = Assert.Single(network.PassageSensors.ToArray());
        Assert.Equal((ActivationNodeKind.PassageSource, sensor.Id), (Node(network, detector.Id).Kind, Node(network, detector.Id).Passage));
        Assert.Equal(WorkshopActivationCompiler.PassageSensorFor(detector, DeclaredPhysics.Of(PartId.BallDetector).Signal!.Passage!.Value), sensor);
        Assert.Equal((detector.Id, detector.Cell, detector.Local, detector.Rotation), (sensor.Frame, sensor.Cell, sensor.Local, sensor.Rotation));
        Assert.DoesNotContain(network.Nodes.ToArray(), node => node.Owner == wall.Id);
    }

    // The indicator follows a level only through its declared input. On the lamp's own ActivationIn a latch makes it follow; the same
    // wiring read through a synthetic indicator on SetIn (a second level-admitting input) holds the first input instead.
    [Fact]
    public void AnIndicatorFollowsALevelOnlyOnItsDeclaredInput()
    {
        var lamp = PlacedParts.Lamp(new(1), 3, 1, 0, 0, 0, 0, 1);
        var latch = PlacedParts.Latch(new(2), -3, 1, 0, 0, 0, 0, 1);
        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(3), 0, 4, 0, 0, 0, 0, 1);
        var level = Bench([lamp, latch, hit], Signal(latch.Id, lamp.Id));
        Assert.Equal(ActivationNodeKind.LevelIndicator, WorkshopActivationCompiler.IndicatorKind(lamp.Id, new(PortSocket.ActivationIn), level));
        Assert.Equal(ActivationNodeKind.StickyFirstInput, WorkshopActivationCompiler.IndicatorKind(lamp.Id, new(PortSocket.SetIn), level));
        Assert.Equal(ActivationNodeKind.StickyFirstInput, WorkshopActivationCompiler.IndicatorKind(hit.Id, new(PortSocket.ActivationIn), level));
        var pulse = Bench([lamp, latch, hit], Signal(hit.Id, lamp.Id));
        Assert.Equal(ActivationNodeKind.StickyFirstInput, WorkshopActivationCompiler.IndicatorKind(lamp.Id, new(PortSocket.ActivationIn), pulse));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopActivationCompiler.IndicatorKind(lamp.Id, new((PortSocket)255), level));
    }

    // ---------- the aperture law (production tracker, compiled sensor) ----------

    /// <summary>Forward crossings of the sensor by one Basketball moving along local +X at height `y` above the bore axis, from `from`
    /// to `to` in 1 cm steps (the detector frame is at (0, 3, 0), unrotated).</summary>
    private static int Crossings(PassageSensorDeclaration sensor, float from, float to, float y)
    {
        var detector = PlacedParts.Detector(new(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = new GpuBodyId(2);
        var colliders = WorkshopPhysicsCompiler.Compile(Bench([detector, WorkshopInput.Generic(PartId.Basketball, ball, from, 3 + y, 0, 0, 0, 0, 1)]),
            new(1, 2)).Colliders.ToArray();
        PhysicsBodyReadSet At(float x)
        {
            var px = WorkshopInput.Position(x); var py = WorkshopInput.Position(3 + y);
            return new([
                new PhysicsBodyRead(new CanonicalBody(ball, 0, 0, new(px.Cell, py.Cell, 0), new(px.Local, py.Local, 0f), default), CanonicalRotation.Identity, default, default),
                new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
            ]);
        }
        var tracker = PassageSensorTracker.Create([sensor with { Frame = detector.Id }], colliders, At(from));
        var outputs = new PassageSensorRead[4];
        var fired = 0;
        var steps = (int)MathF.Round((to - from) / .01f);
        for (var step = 1; step <= steps; step++)
        {
            tracker = tracker.Advance(new SimulationTick((ulong)step), At(from + step * .01f), null, outputs, out var count);
            fired += count;
        }
        return fired;
    }

    private static PassageSensorDeclaration Shipped => WorkshopActivationCompiler.PassageSensorFor(PlacedParts.Detector(new(10), 0, 3, 0, 0, 0, 0, 1),
        DeclaredPhysics.Of(PartId.BallDetector).Signal!.Passage!.Value);

    // CAT-002 boundary: a centre crossing just inside the 0.65 m bore counts once, just outside counts nothing.
    [Theory]
    [InlineData(.64f, 1)]
    [InlineData(.66f, 0)]
    public void ACrossingJustInsideTheBoreCountsAndJustOutsideDoesNot(float offset, int expected) =>
        Assert.Equal(expected, Crossings(Shipped, -1f, 1f, offset));

    // A synthetic aperture compiles its own radius and clearance (narrowed to the binary16 lanes), and the tracker obeys them: a crossing
    // 0.5 m off the axis counts in the 0.65 m bore but not in a 0.3 m one; a ball starting 0.6 m upstream is armed by the 0.02 m
    // clearance (0.36 m line) but not by a 0.5 m one (0.84 m line).
    [Fact]
    public void ASyntheticApertureCompilesItsOwnRadiusAndClearance()
    {
        var detector = PlacedParts.Detector(new(10), 0, 3, 0, 0, 0, 0, 1);
        var narrow = WorkshopActivationCompiler.PassageSensorFor(detector, new SignalPassageDescriptor(.3f, .5f));
        Assert.Equal((WorkshopPhysicsCompiler.PassageSensor(detector.Id), detector.Id, (Half).3f, (Half).5f),
            (narrow.Id, narrow.Frame, narrow.Radius.Value, narrow.RearmClearance.Value));
        Assert.Equal(1, Crossings(Shipped, -1f, 1f, .5f));
        Assert.Equal(0, Crossings(Shipped with { Radius = narrow.Radius }, -1f, 1f, .5f));
        Assert.Equal(1, Crossings(Shipped, -.6f, 1f, 0f));
        Assert.Equal(0, Crossings(Shipped with { RearmClearance = narrow.RearmClearance }, -.6f, 1f, 0f));
        Assert.Equal(1, Crossings(Shipped with { RearmClearance = narrow.RearmClearance }, -.9f, 1f, 0f));
    }

    // ---------- D6: record-path saves load as the generic instances; forged records are refused at both boundaries ----------

    // Written by the production save encoder at 54ffd9f0 on the record path (evidence-c-lamp-detector/baseline/capture/old-saves; the
    // sha256 below are that directory's old-saves.sha256): a lamp, a locked rotated detector, and a detector wired to a lamp and to a
    // counter that lights a second lamp, beside one Basketball.
    private const string LampSave = "vAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAJAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABQAAAAQADwAMAAsAAAAIAAUAAQAEAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAQAAAAAQAAAAAAAAAAAAAABAAAAGxhbXAAAAAA";
    private const string DetectorSave = "zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAJAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAEAAAAUAAAAEABAADQAMAAvACAAFAAEABAAAAAAAAAAAAAAAPQENb/0BDU/AAAAAAAAAAAAAAAAKAAAABAAAADs////AAAAARAAAAABAAAAAAAAAAAAAAANAAAAYmFsbF9kZXRlY3RvcgAAAA==";
    private const string WiredSave = "rAIAABQAAABDQ1dTAAAAAAgAFgAIAAQACAAAACAAAAAJAAAAAAAAAAAAAAAAAA4AMAAkAAwAAAAIAAQADgAAACwAAACMAAAAAgAAAAEAAAACAAAAAgAAADwAAAABAAAAAgAAAAAAAAAAAAAAAwAAAAEAAAAAAAAAAgAAAAAAAAABAAAAAQAAAAQAAAAAAAAAAQAAAAAAAAAEAAAAAAAAAAEAAAABAAAABAAAAAAAAAAEAAAAAAAAAAUAAAAAAAAAAQAAAAEAAAAEAAAAAAAAAAUAAACMAQAAMAEAANwAAABsAAAABAAAAJj+//8AAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAQAAAABAAAAAwAAAAEAAAAAUAAAAAAAAAAAAAAAQAAABsYW1wAAAeAEAANAAwAAAAJAAYAAgAAAAAAAAAAAAAAAAABAAeAAAAAgAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAADA////EAAAAAAAAAAQAAAABAAAAAAAAAAAAAAABwAAAGNvdW50ZXIAIABAADAALAAAACAAFAAEAAAAAAAAAAAAAAAAAAAAPAAgAAAAAAAAAAAAAAAAAAAAAACAPwAAAAC4HqU9AAAAAAAAAAAuAAAAAAAAABgAAAADAAAAAAAAAAAAAAAEAAAAAAAAAAQAAABiYWxsAAAAALj///8AAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAQAAAABAAAAAAAAAAEAAAAAIAAAAAAAAAAAAAAAQAAABsYW1wAAAAABAAPAAwACwAAAAgABQABAAQAAAAAAAAAAAAAAD0BDW/9AQ1PwAAAADD9eC+AAAAAAAAAAADAAAAAAAAABAAAAABAAAAAAAAAAAAAAANAAAAYmFsbF9kZXRlY3RvcgAAAA==";

    private static WorkshopConstruction WiredBench()
    {
        var floor = (double)WorkshopConstruction.WorkbenchSurface.Value;
        var detector = PlacedParts.Detector(new(1), 0, floor + .62, 0, 0, 0, -H, H);
        var counter = PlacedParts.Counter(new(4), -4, 1, 0, 0, 0, 0, 1, 2);
        return Bench([detector, PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(PartId.Basketball, new(3), 0, floor + .34 + 3, 0, 0, 0, 0, 1), counter, PlacedParts.Lamp(new(5), 4, 1, 3, 0, 0, 0, 1)],
            Signal(detector.Id, new(2)), Signal(detector.Id, counter.Id), Signal(counter.Id, new(5)));
    }

    public static TheoryData<string, string, string> RecordPathSaves() => new()
    {
        { "lamp", LampSave, "3b45959bcf734fed9ead0993ee4fb6ceab90fc0ad09f81ac2934a954decd45d0" },
        { "detector", DetectorSave, "bec0bf607746462b4cdeeaeaa16132c7f8c29ab52d2193072e8547f7db722cf9" }
    };

    // 9A.32 removed four InstanceRecord fields and renumbered the later slots, so the wired save's counter target count (slot 12 at
    // 54ffd9f0) now lies beyond the table (10 fields at 9A.32, 8 since C-Memory). C-Memory made the counter a generic instance, so its
    // record decodes as a counter without its declared target_count: the save is refused with the typed reason at both decode
    // boundaries, never misread (it was a missing typed target count before C-Memory).
    [Fact]
    public void TheWiredRecordPathSaveIsRefusedAfterTheSlotRenumbering()
    {
        var bytes = Convert.FromBase64String(WiredSave);
        Assert.Equal("d3f44efad3fd9ad2855109333bc5b7caa2806ac7c7215b38001f79f95e81c2fe", Convert.ToHexStringLower(SHA256.HashData(bytes)));
        Assert.Equal("This save cannot be loaded: Invalid declared parameters.",
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes)).Message);
        var counter = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(3)!.Value;
        Assert.Equal("counter", counter.PartId);
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(counter)).Reason);
    }

    [Theory]
    [MemberData(nameof(RecordPathSaves))]
    public void ARecordPathSaveLoadsAsTheGenericInstancesAtBothBoundaries(string name, string base64, string sha256)
    {
        var bytes = Convert.FromBase64String(base64);
        Assert.Equal(sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        var expected = name switch
        {
            "lamp" => Bench([PlacedParts.Lamp(new(1), 0, 1, 0, 0, 0, 0, 1)]),
            "detector" => Bench([PlacedParts.Detector(new(1), 2.5, 1, -1.25, 0, 0, -H, H, locked: true)]),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(new WorkshopSavedConstruction(expected, new(9)), loaded);
        var records = WorkshopSaveCodec.Open(bytes).Construction!.Value;
        for (var i = 0; i < records.InstancesLength; i++)
            Assert.Equal(expected.Instances[i], WorkshopWireFlatBuffers.MapInstance(records.Instances(i)!.Value));
    }

    // The saved and loaded wired bench runs: the ball passes the detector twice, so the first lamp is lit and the counter (target 2) lit
    // the second. (Until 9A.32 this loaded the record-path save; that save is now refused above, so the bench is saved by the current
    // encoder.)
    [Fact]
    public async Task TheSavedAndLoadedWiredBenchPassesTwice()
    {
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(WiredBench(), new(9)))).Construction;
        Assert.Equal(WiredBench(), loaded);
        var (_, reads, _) = await NoFixedCapacityTests.RunAndReset(loaded, 4 * 120);
        bool Lit(GpuBodyId owner) => Enumerable.Range(0, reads[^1].Activations.Count)
            .Any(i => reads[^1].Activations[i].Owner == owner && reads[^1].Activations[i].Phase == ActivationPhase.Latched);
        Assert.True(Lit(new(2)) && Lit(new(5)));
    }

    // Reset is exact in one simulation: the passage cursor, the detector's pulse, the lamp's latch and the counter all return to the
    // built state, so the same run replays tick for tick, both passes included (a cursor left unarmed would miss the second run's
    // first pass and the counter's lamp would stay dark).
    [Fact]
    public async Task ResetRestoresThePassageCursorPulseLatchAndCountInOneSimulation()
    {
        await using var device = new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(WiredBench())).Outcome);
        var built = simulation.Committed;
        async Task<List<WorkshopRead>> Run()
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            var reads = new List<WorkshopRead>();
            for (var tick = 0; tick < 4 * 120; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
                reads.Add(simulation.Committed);
            }
            return reads;
        }
        static int FirstLit(List<WorkshopRead> reads, GpuBodyId owner) => reads.FindIndex(read => Enumerable.Range(0, read.Activations.Count)
            .Any(i => read.Activations[i].Owner == owner && read.Activations[i].Phase == ActivationPhase.Latched));
        var first = await Run();
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        var reset = simulation.Committed;
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        Assert.True(built.Memory.HasSameBits(reset.Memory), "the counter's committed memory is cleared");
        Assert.Equal(Enumerable.Range(0, built.Activations.Count).Select(i => built.Activations[i]),
            Enumerable.Range(0, reset.Activations.Count).Select(i => reset.Activations[i]));
        var second = await Run();
        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            NoFixedCapacityTests.AssertSameState(first[i], second[i], $"replay tick {i}");
            Assert.True(first[i].Memory.HasSameBits(second[i].Memory), $"memory at replay tick {i}");
            Assert.Equal(Enumerable.Range(0, first[i].Activations.Count).Select(a => first[i].Activations[a]),
                Enumerable.Range(0, second[i].Activations.Count).Select(a => second[i].Activations[a]));
        }
        var (lamp, counted) = (FirstLit(second, new(2)), FirstLit(second, new(5)));
        Assert.True(lamp > 0 && counted > lamp, $"both passes after Reset: first at read {lamp}, second at read {counted}");
        Assert.Equal((FirstLit(first, new(2)), FirstLit(first, new(5))), (lamp, counted));
    }

    // A lamp or detector record carries no typed field and no parameter: each is refused OutOfRange by the save pre-validation and by
    // the command decoder's instance mapping. (On the record path the command boundary admitted a typed field.) Control: the bare record
    // loads as the generic instance at both.
    [Theory]
    [InlineData("lamp")]
    [InlineData("ball_detector")]
    public void ARecordCarryingATypedFieldOrAParameterIsRefusedAtBothBoundaries(string part)
    {
        void Refused(byte[] bytes, string what)
        {
            var save = Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
            Assert.True(save.Reason == WorkshopSaveRejection.OutOfRange, $"{part} {what}: {save.Reason}");
            var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        }
        foreach (var field in Enum.GetValues<TypedField>())
            Refused(PayloadInstanceTests.GenericSave(part, field), field.ToString());
        Refused(PayloadInstanceTests.GenericSave(part, null, [("radius", .5f)]), "parameter");
        var bare = PayloadInstanceTests.GenericSave(part, null);
        var loaded = Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, bare).Construction.Instances));
        Assert.Equal((new PartId(part), 0), (loaded.PartId, loaded.Parameters.Count));
        Assert.Equal(loaded, WorkshopWireFlatBuffers.MapInstance(WorkshopSaveCodec.Open(bare).Construction!.Value.Instances(0)!.Value));
    }

    // D5: the two level-wiring refusals keep their ordinals and display text under their capability names.
    [Fact]
    public void TheLevelWiringRefusalsKeepTheirOrdinalsAndText()
    {
        Assert.Equal((7u, 8u), ((uint)WorkshopEditRejection.LevelDrivesLevelInputOnly, (uint)WorkshopEditRejection.LevelInputFollowsOneLevel));
        var latch = PlacedParts.Latch(new(1), 0, 0, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(2), 3, 0, 0, 0, 0, 0, 1);
        var counter = PlacedParts.Counter(new(3), -3, 0, 0, 0, 0, 0, 1, 3);
        var instances = new WorkshopInstances(latch, lamp, counter);
        Assert.Equal("A level output can only light a Signal lamp.", EditRefusal.Is(WorkshopEditRejection.LevelDrivesLevelInputOnly,
            () => new WorkshopConnections(Signal(latch.Id, counter.Id)).Validate(instances)).Message);
        Assert.Equal("A lamp that follows a level output takes no other signal.", EditRefusal.Is(WorkshopEditRejection.LevelInputFollowsOneLevel,
            () => new WorkshopConnections(Signal(latch.Id, lamp.Id), Signal(counter.Id, lamp.Id)).Validate(instances)).Message);
    }
}
