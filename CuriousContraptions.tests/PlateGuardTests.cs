using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Plate suite-first guards (9A.20, 9A.40, 9R.4, 9R.5, CAT-052). Written on the record path (base 61ded7ad) and run unchanged on
/// the declared generic instance: every plate is built through <see cref="PlacedParts.Plate"/> and every expectation reads the
/// production compile, Advance and Reset path (committed contact-load reads, activation levels, the gate's blade and the lamp), never a
/// record property. The f32 boundaries and the 0.1 s (12-tick) hold are stated literally from CAT-052 and owner decision (h).</summary>
public sealed class PlateGuardTests
{
    private const double GateHeight = 3;
    private const float ClosedTravel = .2f, OpenTravel = 1.3f;
    private const int HoldTicks = 12; // 0.1 s at 120 Hz
    private static readonly double PlateY = (double)WorkshopConstruction.WorkbenchSurface.Value + .22;
    private static readonly double Face = PlateY + .08;

    /// <summary>What rests on the plate at the start of a run.</summary>
    public enum Load { Basketball, StandingDomino, BowlingBall, FourBowlingBalls, ThreeBowlingBallsAndABasketball }

    private static IWorkshopInstance[] Bodies(Load load, double x, double z, ulong first) => load switch
    {
        Load.Basketball => [Ball(first, x, z)],
        // The .325m half-depth covers the .3m ring: crown .084+.018 above the owner,
        // plus .55m tile half-height and independent 1mm initial clearance.
        Load.StandingDomino => [PlacedParts.Domino(new(first), (float)(x), (float)(PlateY + .084 + .018 + .55 + .001), (float)(z), 0, 0, 0, 1)],
        Load.BowlingBall => [Bowling(first, x, z)],
        Load.FourBowlingBalls => [Bowling(first, x - .45, z - .45), Bowling(first + 1, x + .45, z - .45),
            Bowling(first + 2, x - .45, z + .45), Bowling(first + 3, x + .45, z + .45)],
        Load.ThreeBowlingBallsAndABasketball => [Bowling(first, x - .45, z - .45), Bowling(first + 1, x + .45, z - .45),
            Bowling(first + 2, x - .45, z + .45), Ball(first + 3, x + .45, z + .45)],
        _ => throw new ArgumentOutOfRangeException(nameof(load))
    };
    private static WorkshopGenericInstance Ball(ulong id, double x, double z) =>
        WorkshopInput.Generic(PartId.Basketball, new(id), (float)(x), (float)(Face + .345), (float)(z), 0, 0, 0, 1);
    private static WorkshopGenericInstance Bowling(ulong id, double x, double z) =>
        WorkshopInput.Generic(PartId.BowlingBall, new(id), (float)(x), (float)(Face + .285), (float)(z), 0, 0, 0, 1);
    private static WorkshopGenericInstance Gate(ulong id, double z) =>
        WorkshopInput.Generic(PartId.PoweredGate, new(id), 0, (float)(GateHeight), (float)(z), 0, 0, 0, 1);
    private static WorkshopGenericInstance Battery(ulong id, double z) => PlacedParts.Battery(new(id), 3, (float)(GateHeight), (float)(z));
    private static IWorkshopInstance Lamp(ulong id, double z) => PlacedParts.Lamp(new(id), 6, (float)(GateHeight), (float)(z), 0, 0, 0, 1);
    private static WorkshopConnection Supply(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.ActivationOut, target, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));

    /// <summary>Battery → plate → gate, plate ActivationOut → lamp, and the load on the plate (ids 5 onward).</summary>
    private static (WorkshopConstruction Bench, IWorkshopInstance Plate, WorkshopGenericInstance Gate, IWorkshopInstance Lamp) Circuit(
        float minimumMass, IWorkshopInstance[] load, double z = 0, ulong first = 1)
    {
        var gate = Gate(first, z); var battery = Battery(first + 1, z);
        var plate = PlacedParts.Plate(new(first + 2), -4, (float)(PlateY), (float)(z), (float)(minimumMass)); var lamp = Lamp(first + 3, z);
        return (Bench([gate, battery, plate, lamp, .. load], Supply(battery.Id, plate.Id), Supply(plate.Id, gate.Id), Signal(plate.Id, lamp.Id)),
            plate, gate, lamp);
    }

    private static async Task<List<WorkshopRead>> Run(WorkshopConstruction construction, int ticks) =>
        (await WorkshopIntegrationHarness.ObservePhases(construction, ([], ticks)))[0];

    private static bool Latched(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Activations.Count; i++) if (read.Activations[i].Owner == owner) return read.Activations[i].Phase == ActivationPhase.Latched;
        throw new InvalidOperationException($"No activation node for {owner.Value}.");
    }
    private static ContactLoadSensorRead LoadOf(WorkshopRead read, GpuBodyId plate)
    {
        foreach (var load in read.Presentation.Loads) if (load.Frame == plate) return load;
        throw new InvalidOperationException($"Plate {plate.Value} commits no contact-load read.");
    }
    private static float Travel(WorkshopRead read, WorkshopGenericInstance gate)
    {
        return read.Presentation.Actuators.ToArray().Single(row => row.Owner == gate.Id).Coordinate;
    }

    // The threshold matrix through the real path: the instance's minimum mass, compared inclusively in f32, decides the plate. A
    // Loaded plate is high on its first Loaded tick (no dwell), lights its lamp and opens its gate; an Underweight plate leaves both
    // dark and shut. 4 x 4 kg = 16 kg is exactly the maximum; 3 x 4 kg + 1 kg = 13 kg is under it; a 0.4 kg Domino loads the 0.1 kg minimum.
    [Theory]
    [InlineData(.5f, Load.Basketball, true)]
    [InlineData(.5f, Load.StandingDomino, false)]
    [InlineData(2f, Load.Basketball, false)]
    [InlineData(2f, Load.BowlingBall, true)]
    [InlineData(1f, Load.Basketball, true)]
    [InlineData(16f, Load.FourBowlingBalls, true)]
    [InlineData(16f, Load.ThreeBowlingBallsAndABasketball, false)]
    [InlineData(.1f, Load.StandingDomino, true)]
    public async Task TheMinimumMassDecidesTheLevelTheLampAndTheGate(float minimumMass, Load load, bool loaded)
    {
        var (bench, plate, gate, lamp) = Circuit(minimumMass, Bodies(load, -4, 0, 5));
        Assert.Equal(minimumMass, Assert.Single(WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).ContactLoadSensors.ToArray()).MinimumMass);
        var reads = await Run(bench, 3 * 120);
        Assert.Equal(ContactLoadPhase.Empty, LoadOf(reads[0], plate.Id).Phase);
        Assert.Contains(reads, read => LoadOf(read, plate.Id).Phase != ContactLoadPhase.Empty); // control: the load rests on the face
        if (!loaded)
        {
            Assert.All(reads, read => Assert.True(LoadOf(read, plate.Id).Phase != ContactLoadPhase.Loaded && !Latched(read, plate.Id) &&
                !Latched(read, lamp.Id) && Travel(read, gate) < ClosedTravel, $"{load} at {minimumMass} kg, tick {read.Tick.Value}"));
            Assert.Equal(ContactLoadPhase.Underweight, LoadOf(reads[^1], plate.Id).Phase);
            return;
        }
        var first = reads.FindIndex(read => Latched(read, plate.Id));
        Assert.True(first > 0, $"{load} at {minimumMass} kg loads the plate");
        Assert.Equal(ContactLoadPhase.Loaded, LoadOf(reads[first], plate.Id).Phase); // high on its first Loaded tick
        Assert.All(reads.Take(first), read => Assert.NotEqual(ContactLoadPhase.Loaded, LoadOf(read, plate.Id).Phase));
        Assert.All(reads.Skip(first), read => Assert.True(Latched(read, lamp.Id), $"lamp lit at tick {read.Tick.Value}"));
        Assert.True(Travel(reads[^1], gate) > OpenTravel, $"the gate opens, travel {Travel(reads[^1], gate)}");
    }

    // The 0.1 s hold through the real path: a Basketball dropped onto the plate bounces. On every tick the plate level and its lamp
    // are high exactly while n <= 11, with n the ticks since the last Loaded tick. From 2 m every rebound gap is short, so the lamp stays
    // steadily lit from the first contact; from 8 m the first rebound leaves the face long enough to be released: lit at n = 11, dark at
    // n = 12 and n = 13, then lit again on the next Loaded tick (lifting off behaves as a release).
    [Theory]
    [InlineData(2.0, false)]
    [InlineData(8.0, true)]
    public async Task ABouncingLoadIsHeldElevenTicksAndReleasedOnTheTwelfth(double drop, bool released)
    {
        var ball = WorkshopInput.Generic(PartId.Basketball, new(5), -4, (float)(Face + .345 + drop), 0, 0, 0, 0, 1);
        var (bench, plate, _, lamp) = Circuit(.5f, [ball]);
        var reads = await Run(bench, 6 * 120);
        ulong? last = null;
        var gaps = new List<ulong>(); var seen = new HashSet<ulong>();
        foreach (var read in reads)
        {
            var loaded = LoadOf(read, plate.Id).Phase == ContactLoadPhase.Loaded;
            if (loaded && last is { } previous && read.Tick.Value - previous > 1) gaps.Add(read.Tick.Value - previous - 1);
            if (loaded) last = read.Tick.Value;
            var expected = last is { } l && read.Tick.Value - l <= HoldTicks - 1;
            if (last is { } m) seen.Add(read.Tick.Value - m);
            Assert.True(expected == Latched(read, plate.Id) && expected == Latched(read, lamp.Id),
                $"tick {read.Tick.Value}: last Loaded {last}, plate {Latched(read, plate.Id)}, lamp {Latched(read, lamp.Id)}");
        }
        var trace = $"non-Loaded gaps between Loaded ticks: {string.Join(",", gaps)}";
        Assert.True(gaps.Count > 0, trace); // control: the ball left the face between contacts
        if (released)
        {
            Assert.True(gaps.Any(gap => gap >= 13), trace);
            Assert.Superset(new HashSet<ulong> { 11, 12, 13 }, seen);
        }
        else
        {
            Assert.True(gaps.All(gap => gap <= 11), trace);
            var first = reads.FindIndex(read => LoadOf(read, plate.Id).Phase == ContactLoadPhase.Loaded);
            Assert.All(reads.Skip(first), read => Assert.True(Latched(read, lamp.Id), $"steadily lit at tick {read.Tick.Value}"));
        }
    }

    // Two plates with their own lamps: loading P1 lights only L1 and opens only G1.
    [Fact]
    public async Task TwoPlatesLightAndOpenOnlyTheirOwn()
    {
        var (first, plate1, gate1, lamp1) = Circuit(.5f, [Ball(9, -4, -3)], -3, 1);
        var (second, plate2, gate2, lamp2) = Circuit(.5f, [], 3, 5);
        var bench = first with
        {
            Instances = new([.. first.Instances, .. second.Instances]),
            Connections = new([.. first.Connections, .. second.Connections])
        };
        var reads = await Run(bench, 2 * 120);
        Assert.True(Latched(reads[^1], lamp1.Id) && Travel(reads[^1], gate1) > OpenTravel, "P1 lights L1 and opens G1");
        Assert.All(reads, read => Assert.True(!Latched(read, plate2.Id) && !Latched(read, lamp2.Id) && Travel(read, gate2) < ClosedTravel &&
            LoadOf(read, plate2.Id).Phase == ContactLoadPhase.Empty, $"P2 stays Empty at tick {read.Tick.Value}"));
        Assert.Equal(ContactLoadPhase.Loaded, LoadOf(reads[^1], plate1.Id).Phase);
    }

    // Negative control: a static Wall sized to the plate face's exact footprint (1.8 m x 1.8 m, its top at the face height) carries a
    // resting Basketball, yet compiles no contact-load row and no level; the real 0.5 kg plate beside it stays Empty, dark and shut.
    [Fact]
    public async Task AStaticBoxWithThePlateFaceButNoSensorNeverActivates()
    {
        const float height = .4f;
        var wall = WorkshopInput.Generic(PartId.Wall, new(9), -4, (float)(Face - height / 2), 4, 0, 0, 0, 1,
            new([new(new("width"), 1.8f), new(new("height"), height), new(new("thickness"), 1.8f)]));
        var (bench, plate, gate, lamp) = Circuit(.5f, [wall, Ball(10, -4, 4)]);
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.DoesNotContain(scene.ContactLoadSensors.ToArray(), row => row.Frame == wall.Id);
        Assert.DoesNotContain(WorkshopActivationCompiler.Compile(bench).Nodes.ToArray(), node => node.Owner == wall.Id);
        var reads = await Run(bench, 2 * 120);
        Assert.True(reads[^1].Bodies.TryGet(new(10), out var ball) && Math.Abs((ball.Body.Cell.Y + ball.Body.Local.Y) / 16.0 - (Face + .34)) < .01,
            "control: the ball rests on the wall's top");
        Assert.All(reads, read => Assert.True(LoadOf(read, plate.Id).Phase == ContactLoadPhase.Empty && !Latched(read, plate.Id) &&
            !Latched(read, lamp.Id) && Travel(read, gate) < ClosedTravel, $"tick {read.Tick.Value}"));
    }

    private static string Row(WorkshopRead read)
    {
        var text = new System.Text.StringBuilder($"t={read.Tick.Value}");
        for (var i = 0; i < read.Activations.Count; i++) text.Append(' ').Append(read.Activations[i]);
        for (var i = 0; i < read.Memory.Count; i++) text.Append(' ').Append(read.Memory[i]);
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var load in read.Presentation.Loads) text.Append(invariant, $" {load.Frame.Value}:{load.Phase}:{BitConverter.SingleToUInt32Bits(load.MeasuredMass):x8}");
        for (var i = 0; i < read.Bodies.Count; i++)
        {
            var b = read.Bodies[i];
            text.Append(invariant, $" {b.Body.Id.Value}:{b.Body.Cell}:{BitConverter.SingleToUInt32Bits((float)b.Body.Local.X):x8},{BitConverter.SingleToUInt32Bits((float)b.Body.Local.Y):x8},{BitConverter.SingleToUInt32Bits((float)b.Body.Local.Z):x8}:{b.Rotation}");
        }
        return text.ToString();
    }

    // Lifecycle: Run then Reset restores the built state exactly (plate Empty, hold cleared, lamp dark, gate shut); a second run replays
    // tick for tick; the save codec reloads the same construction (2.0 kg kept bit for bit), which replays the same run.
    [Fact]
    public async Task RunResetReplayAndSaveLoadAreExact()
    {
        var (bench, plate, gate, lamp) = Circuit(2f, [Bowling(5, -4, 0)]);
        var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, 2 * 120);
        Assert.True(Latched(reads[^1], lamp.Id) && Travel(reads[^1], gate) > OpenTravel, "control: the run loads the plate");
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        Assert.True(LoadOf(reset, plate.Id).Phase == ContactLoadPhase.Empty && !Latched(reset, plate.Id) && !Latched(reset, lamp.Id) &&
            Travel(reset, gate) < ClosedTravel, "Reset: Empty, dark and shut");
        for (var i = 0; i < reset.Memory.Count; i++) Assert.Equal(0ul, reset.Memory[i].DueTick);
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(bench, 2 * 120);
        Assert.Equal(reads.Select(Row), replay.Select(Row));

        var saved = new WorkshopSavedConstruction(bench, new(9));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, loaded);
        Assert.Equal(BitConverter.SingleToUInt32Bits(2f), BitConverter.SingleToUInt32Bits(
            Assert.Single(WorkshopPhysicsCompiler.Compile(loaded.Construction, new(1, 2)).ContactLoadSensors.ToArray()).MinimumMass));
        var (_, reloaded, _) = await WorkshopIntegrationHarness.ObserveReset(loaded.Construction, 2 * 120);
        Assert.Equal(reads.Select(Row), reloaded.Select(Row));
    }
}
