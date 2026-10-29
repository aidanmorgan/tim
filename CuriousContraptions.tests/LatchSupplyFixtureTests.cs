using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9.4c: the CAT-037 T21 supply-switching fixture (Battery → State Latch contact → Powered Gate, Impact switch A on
/// SetIn and B on ResetIn, lamp on the latch level) proves CAT-005 N17/N19 and the CAT-051 mid-stroke interruption (facts 14, 15,
/// 19) on the production WorkshopSimulation. Supply changes only through the latch contact or the in-run battery enable; no wire
/// is edited during Run (CAT-005 Q6) and the return spring is the only closing law (CAT-051 Q1).</summary>
public sealed class LatchSupplyFixtureTests
{
    private const ulong Battery = 1, Latch = 2, Gate = 3, SwitchA = 4, BallA = 5, SwitchB = 6, BallB = 7, Lamp = 8, Payload = 9;
    // Measured on this engine (spec D3): the committed latch of tick T closes or opens the route consumed by physics tick T+1.
    // The frozen T14 "On at T+2" came from the deleted legacy command queue; flagged for owner confirmation.
    private const int SupplyLagTicks = 1;
    private const float Upper = 1.55f, ClosedTolerance = 1e-3f, BallRadius = .34f, BladeHalfThickness = .06f, BladeHalfHeight = .72f,
        BoreRadius = .65f, TiltDegrees = -15f;
    private const double GateX = 8, GateY = 3;
    private static readonly (float Sin, float Cos) Tilt = MathF.SinCos(TiltDegrees * MathF.PI / 180f);

    private static WorkshopConnection Power(ulong source, ulong target) =>
        new(new(source), WorkshopSocket.Supply, new(target), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopConnection Signal(ulong source, ulong target, WorkshopSocket input) =>
        new(new(source), WorkshopSocket.ActivationOut, new(target), input, WorkshopConnectionDomain.Activation);
    private static readonly WorkshopConnection[] FiveConnections =
    [
        Power(Battery, Latch), Power(Latch, Gate), Signal(SwitchA, Latch, WorkshopSocket.SetIn), Signal(SwitchB, Latch, WorkshopSocket.ResetIn),
        Signal(Latch, Lamp, WorkshopSocket.ActivationIn)
    ];

    /// <summary>The one fixture builder (spec D5/D7). Ball B's drop height times Switch B's strike; null leaves Switch B unstruck.
    /// The payload basketball starts in the tilted bore's upstream mouth and rests against the closed blade.</summary>
    internal static WorkshopConstruction Machine(double? ballBHeight, bool payload = false, ElectricalEnable battery = ElectricalEnable.Enabled)
    {
        var half = TiltDegrees * Math.PI / 360;
        var parts = new List<IWorkshopInstance>
        {
            PlacedParts.Battery(new(Battery), -8, 3, 4, enabled: battery),
            PlacedParts.Latch(new(Latch), -4, 3, 4, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("powered_gate"), new(Gate), (float)(GateX), (float)(GateY), 0, 0, 0, (float)(Math.Sin(half)), (float)(Math.Cos(half))),
            WorkshopInput.Generic(new PartId("switch"), new(SwitchA), 0, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("ball"), new(BallA), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("switch"), new(SwitchB), 0, 1, -6, 0, 0, 0, 1),
            PlacedParts.Lamp(new(Lamp), -4, 6, 4, 0, 0, 0, 1),
        };
        if (ballBHeight is { } height) parts.Add(WorkshopInput.Generic(new PartId("ball"), new(BallB), 0, (float)(height), -6, 0, 0, 0, 1));
        if (payload)
        {
            const double lx = -.45, ly = -.27;
            parts.Add(WorkshopInput.Generic(new PartId("ball"), new(Payload),
                (float)(GateX + Tilt.Cos * lx - Tilt.Sin * ly), (float)(GateY + Tilt.Sin * lx + Tilt.Cos * ly), 0, 0, 0, 0, 1));
        }
        return new(new(2), WorkshopCadenceSettings.Default(), new(parts.ToArray()), Connections: new(FiveConnections));
    }

    private sealed record Trace(WorkshopRead Built, List<WorkshopRead> Reads, WorkshopRead Reset, WorkshopConstruction Authored,
        List<WorkshopConstruction> DuringRun, WorkshopConstruction AfterReset, List<WorkshopRead> Replay);

    /// <summary>Construct, Run, the phases (each applies its in-run battery controls, then advances), Reset, and optionally a second
    /// Run of `replay` ticks — all on the production WorkshopSimulation. Records the authored construction after every tick.</summary>
    private static async Task<Trace> Run(WorkshopConstruction construction, int replay, params (WorkshopElectricalControl[] Controls, int Ticks)[] phases)
    {
        await using var device = new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), CompiledSceneFixture.Compile(new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty)),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(CompiledSceneFixture.Compile(construction))).Outcome);
        var built = simulation.Committed; var authored = construction;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        var reads = new List<WorkshopRead>(); var during = new List<WorkshopConstruction>();
        foreach (var (controls, ticks) in phases)
        {
            foreach (var control in controls) Assert.Equal(WorkshopCommandOutcome.Applied, simulation.ConfigureElectrical(control).Outcome);
            for (var tick = 0; tick < ticks; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
                reads.Add(simulation.Committed); during.Add(construction);
                Assert.Equal(construction.Revision, simulation.Scene.Revision);
            }
        }
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        var reset = simulation.Committed; var after = construction;
        var again = new List<WorkshopRead>();
        if (replay > 0)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            for (var tick = 0; tick < replay; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
                again.Add(simulation.Committed);
            }
        }
        return new(built, reads, reset, authored, during, after, again);
    }
    private static Task<Trace> Run(WorkshopConstruction construction, int ticks) => Run(construction, 0, ([], ticks));

    private static ActivationLatch Activation(WorkshopRead read, ulong owner) =>
        Enumerable.Range(0, read.Activations.Count).Select(i => read.Activations[i]).Single(row => row.Owner.Value == owner);
    private static bool Latched(WorkshopRead read, ulong owner) => Activation(read, owner).Phase == ActivationPhase.Latched;
    private static ActuatorDriveRead Drive(WorkshopRead read) => read.Presentation.Actuators.ToArray().Single(row => row.Owner.Value == Gate);
    private static ElectricalSourceRead Source(WorkshopRead read) =>
        Enumerable.Range(0, read.Electrical.Count).Select(i => read.Electrical[i]).Single(row => row.Owner.Value == Battery);
    /// <summary>Index of the first committed read in which the owner's activation is Latched (the strike tick).</summary>
    private static int Strike(List<WorkshopRead> reads, ulong owner)
    {
        var index = reads.FindIndex(read => Latched(read, owner));
        Assert.True(index > 0, $"switch {owner} must be struck during the run");
        return index;
    }

    /// <summary>The payload ball in the gate's frame: x along the bore (+ downstream), y along the blade's travel.</summary>
    private static (float X, float Y, float Speed) Ball(WorkshopRead read)
    {
        var body = Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i].Body).Single(row => row.Id == new GpuBodyId(Payload));
        var dx = (body.Cell.X + (float)body.Local.X) / 16f - (float)GateX;
        var dy = (body.Cell.Y + (float)body.Local.Y) / 16f - (float)GateY;
        float vx = (float)body.Velocity.X, vy = (float)body.Velocity.Y, vz = (float)body.Velocity.Z;
        return (Tilt.Cos * dx + Tilt.Sin * dy, -Tilt.Sin * dx + Tilt.Cos * dy, MathF.Sqrt(vx * vx + vy * vy + vz * vz));
    }
    /// <summary>Depth of the ball inside the blade box (centred at opening x on the travel axis).</summary>
    private static float BladePenetration((float X, float Y, float Speed) ball, float opening)
    {
        var dx = MathF.Max(MathF.Abs(ball.X) - BladeHalfThickness, 0f); var dy = MathF.Max(MathF.Abs(ball.Y - opening) - BladeHalfHeight, 0f);
        return BallRadius - MathF.Sqrt(dx * dx + dy * dy);
    }
    /// <summary>Mechanical energy of the 1 kg blade on its −15° frame: KE + ½·14·x² + m·g·cos(15°)·x (spec D2).</summary>
    private static float Energy(ActuatorDriveRead drive) =>
        .5f * drive.Speed * drive.Speed + .5f * 14f * drive.Coordinate * drive.Coordinate + 9.81f * Tilt.Cos * drive.Coordinate;
    /// <summary>CAT-051 Q3 (C-Gate): with nothing in the blade's way it never reads Blocked: not at the closed or open stop, not in
    /// powered travel, not in the spring return, and not at a mid-stroke apex where its speed passes through zero.</summary>
    private static void AssertNeverBlocked(List<WorkshopRead> reads) =>
        Assert.All(reads, read => Assert.False(Drive(read).Blocked, $"nothing obstructs the blade, tick {read.Tick.Value}: {Drive(read)}"));

    private static int ClosedBy(List<WorkshopRead> reads, int from)
    {
        var closed = reads.FindIndex(from, read => Drive(read).Coordinate <= ClosedTolerance);
        Assert.True(closed > from && closed - from <= 240, $"blade closes within 240 ticks of tick {reads[from].Tick.Value} (closed index {closed})");
        Assert.All(reads.Skip(closed), read => Assert.True(Drive(read).Coordinate <= ClosedTolerance, $"stays closed, tick {read.Tick.Value}: {Drive(read).Coordinate}"));
        return closed;
    }

    [Fact]
    public async Task SetClosesRouteOneTickAfterStrikeWithoutTopologyChange()
    {
        var trace = await Run(Machine(null), 240);
        var reads = trace.Reads; var t = Strike(reads, SwitchA);
        Assert.All(reads.Take(t), read =>
        {
            Assert.False(Latched(read, Latch)); Assert.False(Latched(read, Lamp));
            Assert.False(Drive(read).SupplyClosed); Assert.True(Drive(read).Coordinate <= ClosedTolerance);
        });
        // The latch and its lamp set at the strike's own boundary; the route the strike's physics consumed was still open.
        Assert.True(Latched(reads[t], Latch)); Assert.True(Latched(reads[t], Lamp));
        Assert.False(Drive(reads[t]).SupplyClosed);
        Assert.True(Drive(reads[t]).Coordinate <= ClosedTolerance);
        // Both sides of the lag edge: a 0-lag or 2-lag engine fails here.
        Assert.True(Drive(reads[t + SupplyLagTicks]).SupplyClosed);
        Assert.False(Drive(reads[t + SupplyLagTicks - 1]).SupplyClosed);
        Assert.True(Drive(reads[t + SupplyLagTicks]).Coordinate > Drive(reads[t]).Coordinate, "first opening tick is the first supplied tick");
        Assert.All(reads.Skip(t + SupplyLagTicks), read => Assert.True(Drive(read).SupplyClosed));
        Assert.True(reads.Max(read => Drive(read).Coordinate) >= 1.50f);
        AssertNeverBlocked(reads);
        // N19: the supply change is the latch contact; the authored construction and its five wires never change during Run.
        Assert.All(trace.DuringRun, construction => Assert.Equal(trace.Authored, construction));
        Assert.Equal(FiveConnections, trace.Authored.Connections.ToArray());
        Assert.Equal(trace.Authored.Revision, trace.DuringRun[^1].Revision);
        Assert.Equal(trace.Authored, trace.AfterReset);
    }

    [Fact]
    public async Task ResetWhenOpenClearsDownstreamAndSpringCloses()
    {
        var reads = (await Run(Machine(16), 480)).Reads; var reset = Strike(reads, SwitchB);
        Assert.True(Drive(reads[reset - 1]).Coordinate >= Upper - ClosedTolerance && Drive(reads[reset]).Coordinate >= Upper - ClosedTolerance,
            $"guard: Switch B must strike with the blade fully open, opening {Drive(reads[reset]).Coordinate}");
        Assert.True(Latched(reads[reset - 1], Latch) && Latched(reads[reset - 1], Lamp));
        Assert.True(Drive(reads[reset]).SupplyClosed);
        // N17: from the strike's boundary the latch and lamp clear; from the next tick the gate is unsupplied for the rest of the run.
        Assert.All(reads.Skip(reset), read => { Assert.False(Latched(read, Latch)); Assert.False(Latched(read, Lamp)); });
        Assert.All(reads.Skip(reset + SupplyLagTicks), read => Assert.False(Drive(read).SupplyClosed));
        AssertSpringDamperReturn(reads, reset);
        AssertNeverBlocked(reads);
    }

    [Fact]
    public async Task MidStrokeResetStopsDriveAndReturnsClosed()
    {
        var reads = (await Run(Machine(6), 480)).Reads; var cut = Strike(reads, SwitchB);
        var opening = Drive(reads[cut]).Coordinate;
        Assert.True(opening > .05f && opening < 1.50f, $"guard: mid-stroke strike required, opening {opening}");
        Assert.True(Drive(reads[cut]).SupplyClosed && Drive(reads[cut]).Speed > 1f, "the drive was opening the blade at the cut");
        Assert.All(reads.Skip(cut + SupplyLagTicks), read => Assert.False(Drive(read).SupplyClosed));
        var reverse = reads.FindIndex(cut, read => Drive(read).Speed <= 0f);
        Assert.True(reverse > cut + SupplyLagTicks && reverse - cut <= 60, $"the blade coasts and reverses (index {reverse})");
        for (var i = cut; i < reverse; i++)
            Assert.True(Drive(reads[i + 1]).Speed < Drive(reads[i]).Speed, $"opening speed strictly decreases, tick {reads[i + 1].Tick.Value}");
        AssertSpringDamperReturn(reads, cut);
        // The apex (between ticks reverse - 1 and reverse) passes through zero speed mid-stroke with nothing touching the blade.
        AssertNeverBlocked(reads);
    }

    /// <summary>Facts 15 and 19: every unsupplied tick follows the declared spring-damper (a = −14·x − 4·v − g·cos 15° for the
    /// 1 kg blade, away from the closed stop), its energy never rises by more than ε_E = 1e-3 J per tick until it is closed, and
    /// it closes within 240 ticks and stays closed.</summary>
    private static void AssertSpringDamperReturn(List<WorkshopRead> reads, int cut)
    {
        var closed = ClosedBy(reads, cut); var worst = 0f;
        for (var i = cut + SupplyLagTicks - 1; i < closed; i++)
        {
            var (before, after) = (Drive(reads[i]), Drive(reads[i + 1]));
            Assert.True(Energy(after) <= Energy(before) + 1e-3f,
                $"fact 15: energy {Energy(before)} -> {Energy(after)} at tick {reads[i + 1].Tick.Value}");
            if (after.Coordinate <= .02f) continue;
            var measured = (after.Speed - before.Speed) * 120f;
            var expected = -14f * (before.Coordinate + after.Coordinate) / 2f - 4f * (before.Speed + after.Speed) / 2f - 9.81f * Tilt.Cos;
            worst = MathF.Max(worst, MathF.Abs(measured - expected));
        }
        Assert.True(worst <= .5f, $"spring-damper law residual {worst} m/s²"); // measured 0.13 (Reset when open), 0.09 (mid-stroke)
    }

    [Fact]
    public async Task NoSwitchBCompletesOpeningHoldsAndPassesTheBall()
    {
        var reads = (await Run(Machine(null, payload: true), 360)).Reads; var set = Strike(reads, SwitchA);
        Assert.All(reads.Take(set), read => Assert.True(Ball(read).X <= -.39f, $"the closed blade holds the ball upstream, x {Ball(read).X}"));
        var open = reads.FindIndex(read => Drive(read).Coordinate >= Upper - ClosedTolerance);
        Assert.True(open > set, "the opening completes");
        Assert.All(reads.Skip(open), read => Assert.True(Drive(read).Coordinate >= Upper - ClosedTolerance && Drive(read).SupplyClosed, "and holds Open"));
        Assert.All(reads.Skip(set), read => Assert.True(Latched(read, Latch)));
        Assert.True(Ball(reads[^1]).X > .49f, $"the ball leaves through the downstream mouth, x {Ball(reads[^1]).X}");
    }

    [Fact]
    public async Task AuthoredDisabledBatteryNeverOpensAfterSet()
    {
        var reads = (await Run(Machine(null, battery: ElectricalEnable.Disabled), 240)).Reads; var set = Strike(reads, SwitchA);
        Assert.All(reads.Skip(set), read => { Assert.True(Latched(read, Latch)); Assert.True(Latched(read, Lamp)); });
        Assert.All(reads, read =>
        {
            Assert.Equal(ElectricalEnable.Disabled, Source(read).Enabled);
            Assert.False(Drive(read).SupplyClosed);
            Assert.True(Drive(read).Coordinate <= ClosedTolerance, $"gate never opens, tick {read.Tick.Value}: {Drive(read).Coordinate}");
        });
        AssertNeverBlocked(reads);
    }

    [Fact]
    public async Task SupplyOffOnKeepsMemoryAndRepowersWithoutNewSet()
    {
        var trace = await Run(Machine(null), 0, ([], 120), ([new(new(Battery), ElectricalEnable.Disabled)], 40),
            ([new(new(Battery), ElectricalEnable.Enabled)], 60));
        var reads = trace.Reads; var set = Strike(reads, SwitchA);
        var (on, off, restored) = (reads.Take(120).ToList(), reads.Skip(120).Take(40).ToList(), reads.Skip(160).ToList());
        Assert.True(Drive(on[^1]).SupplyClosed && Drive(on[^1]).Coordinate > .5f);
        var memory = Activation(on[^1], Latch); var strike = Activation(on[^1], SwitchA);
        Assert.Equal(ActivationPhase.Latched, memory.Phase);
        // Tick-exact: the first tick after Disable is unsupplied, the first after Enable is supplied; no new Set is needed.
        Assert.All(off, read => Assert.False(Drive(read).SupplyClosed));
        Assert.All(restored, read => Assert.True(Drive(read).SupplyClosed));
        Assert.All(off.Concat(restored), read =>
        {
            Assert.Equal(memory, Activation(read, Latch)); Assert.Equal(strike, Activation(read, SwitchA));
            Assert.True(Latched(read, Lamp));
        });
        Assert.True(Drive(off[^1]).Coordinate < Drive(off[0]).Coordinate - .2f, "the unsupplied blade falls");
        Assert.True(Drive(restored[^1]).Coordinate > Drive(restored[0]).Coordinate + .2f, "the repowered blade reopens");
        Assert.True(set < 120);
    }

    [Fact]
    public async Task BallUpstreamAtCutStaysUpstream()
    {
        var reads = (await Run(Machine(5, payload: true), 360)).Reads; var cut = Strike(reads, SwitchB);
        var opening = Drive(reads[cut]).Coordinate;
        Assert.True(opening > .05f && opening < 1.50f, $"guard: mid-stroke strike required, opening {opening}");
        Assert.True(Ball(reads[cut]).X <= -.39f, $"guard: the ball is short of the passage at the cut, x {Ball(reads[cut]).X}");
        Assert.All(reads, read => Assert.True(Ball(read).X < -BladeHalfThickness, $"the ball never reaches the blade plane, x {Ball(read).X}"));
        ClosedBy(reads, cut);
        var rest = Ball(reads[^1]);
        Assert.True(rest.X <= -.39f && rest.Speed <= 1e-3f, $"the ball rests upstream against the closed blade, x {rest.X} speed {rest.Speed}");
        // A ball pressing on the closed blade's face is contact at the closed stop, which is not Blocked.
        Assert.True(Drive(reads[^1]).MovingContact, "the resting ball touches the closed blade");
        AssertNeverBlocked(reads);
    }

    [Fact]
    public async Task BallPartlyThroughStopsClosingBladeUncrushed()
    {
        var reads = (await Run(Machine(9, payload: true), 360)).Reads; var cut = Strike(reads, SwitchB);
        var opening = Drive(reads[cut]).Coordinate; var atCut = Ball(reads[cut]);
        Assert.True(opening > .05f && opening < 1.50f, $"guard: mid-stroke strike required, opening {opening}");
        Assert.True(atCut.X > -(BallRadius + BladeHalfThickness) && atCut.X < 0f, $"guard: the ball is partly through the blade plane, x {atCut.X}");
        var falling = reads.FindIndex(cut, read => Drive(read).Speed < -.5f);
        var landed = reads.FindIndex(falling, read => MathF.Abs(Drive(read).Speed) <= .05f);
        Assert.True(falling > cut && landed > falling, $"the blade falls and stops (falling {falling}, landed {landed})");
        Assert.True(Drive(reads[landed]).Coordinate > .05f, $"fact 14: the blade stops on the ball, opening {Drive(reads[landed]).Coordinate}");
        foreach (var read in reads.Skip(landed))
        {
            var ball = Ball(read); var drive = Drive(read);
            Assert.True(drive.Coordinate > .05f && !drive.SupplyClosed, $"blade held open by the ball, tick {read.Tick.Value}: {drive.Coordinate}");
            Assert.True(ball.Speed <= .5f, $"ball not ejected, tick {read.Tick.Value}: {ball.Speed}");
            Assert.True(MathF.Abs(ball.X) < BallRadius + BladeHalfThickness, $"ball stays in the passage, tick {read.Tick.Value}: {ball.X}");
            Assert.True(BladePenetration(ball, drive.Coordinate) <= .006f, $"temporary penetration ≤ 5% of the blade, tick {read.Tick.Value}");
        }
        var final = Drive(reads[^1]); var rest = Ball(reads[^1]);
        Assert.True(MathF.Abs(final.Speed) <= 1e-3f && rest.Speed <= 1e-3f, $"trapped at rest: blade {final.Speed}, ball {rest.Speed}");
        // Fact 14 "State Blocked" (C-Gate, CAT-051 Q3): from the first tick at the envelope's rest speed the blade reads Blocked on
        // every tick, although a constant ~1.1e-5 m/s commit-point residual remains (game-grade rest: the pose holds; no rest
        // threshold zeroes velocity). The ball is the contact that holds it.
        var resting = reads.FindIndex(landed, read => MathF.Abs(Drive(read).Speed) < ActuatorDriveRead.RestSpeed);
        Assert.True(resting >= landed, "the blade comes to rest on the ball");
        Assert.All(reads.Take(resting), read => Assert.False(Drive(read).Blocked, $"not blocked before rest, tick {read.Tick.Value}"));
        Assert.All(reads.Skip(resting), read =>
        {
            Assert.True(Drive(read).MovingContact, $"the ball touches the resting blade, tick {read.Tick.Value}");
            Assert.True(Drive(read).Blocked, $"fact 14: Blocked on every tick after rest, tick {read.Tick.Value}: {Drive(read)}");
        });
        Assert.True(MathF.Abs(final.Coordinate - Drive(reads[^61]).Coordinate) <= 1e-5f, "the blade holds its opening");
        Assert.True(BladePenetration(rest, final.Coordinate) <= 5e-4f, $"resting blade penetration {BladePenetration(rest, final.Coordinate)}");
        Assert.True(-(BoreRadius - BallRadius) - rest.Y <= 5e-4f, $"resting tube penetration {-(BoreRadius - BallRadius) - rest.Y}");
    }

    [Fact]
    public async Task RunResetRestoresLatchOffBladeClosedAuthoredBattery()
    {
        var trace = await Run(Machine(null), 150, ([], 150), ([new(new(Battery), ElectricalEnable.Disabled)], 5));
        var reads = trace.Reads;
        // Non-vacuous: the run set the latch, opened the blade and ended with the battery disabled in Run.
        Assert.True(Latched(reads[^1], Latch) && Latched(reads[^1], Lamp) && reads.Max(read => Drive(read).Coordinate) > 1f);
        Assert.Equal(ElectricalEnable.Disabled, Source(reads[^1]).Enabled);
        Assert.False(Latched(trace.Reset, Latch)); Assert.False(Latched(trace.Reset, Lamp)); Assert.False(Latched(trace.Reset, SwitchA));
        Assert.Equal(ElectricalEnable.Enabled, Source(trace.Reset).Enabled);
        Assert.True(Drive(trace.Reset).Coordinate <= ClosedTolerance && !Drive(trace.Reset).SupplyClosed);
        NoFixedCapacityTests.AssertSameState(trace.Built, trace.Reset, "latch fixture Reset");
        Assert.Equal(trace.Built.Presentation.Actuators.ToArray(), trace.Reset.Presentation.Actuators.ToArray());
        Assert.Equal(trace.Built.Presentation.Logic.ToArray(), trace.Reset.Presentation.Logic.ToArray());
        Assert.Equal(trace.Built.Presentation.Loads.ToArray(), trace.Reset.Presentation.Loads.ToArray());
        Assert.Equal(trace.Authored, trace.AfterReset);
        // Reset re-enables the authored battery, and the next run again sets the latch and opens the blade.
        Assert.Contains(trace.Replay, read => Latched(read, Latch) && Latched(read, Lamp) && Drive(read).Coordinate > 1f);
        Assert.All(trace.Replay, read => Assert.Equal(ElectricalEnable.Enabled, Source(read).Enabled));
    }

    [Fact]
    public async Task SaveLoadPreservesFiveSocketsAndReplaysIdentically()
    {
        var construction = Machine(9, payload: true);
        var saved = new WorkshopSavedConstruction(construction, new(Payload + 1));
        var bytes = WorkshopSaveCodec.Encode(saved); var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, loaded);
        Assert.Equal(FiveConnections, loaded.Construction.Connections.ToArray());
        var original = await Run(construction, 300); var replay = await Run(loaded.Construction, 300);
        Assert.Contains(original.Reads, read => Latched(read, SwitchB) && Drive(read).Coordinate > .05f); // the replay covers the cut
        Assert.Contains(replay.Reads, read => Latched(read, SwitchB) && Drive(read).Coordinate > .05f);
        Assert.Contains(replay.Reads, read => Latched(read, Latch) && Latched(read, Lamp));
        NoFixedCapacityTests.AssertSameState(replay.Built, replay.Reset, "loaded latch fixture Reset");
    }
}
