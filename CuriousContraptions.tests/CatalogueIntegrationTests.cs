using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class CatalogueIntegrationTests
{
    public enum GateKind { Both, Nand, Nor, Or, Xor }
    public static TheoryData<GateKind, SimulationCadence> GateCadences()
    {
        var data = new TheoryData<GateKind, SimulationCadence>();
        foreach (var gate in Enum.GetValues<GateKind>())
            foreach (var cadence in new[] { SimulationCadence.Hz60, SimulationCadence.Hz120, SimulationCadence.Hz240 })
                data.Add(gate, cadence);
        return data;
    }

    [Theory]
    [MemberData(nameof(GateCadences))]
    public async Task SuppliedLogicControlsTheDownstreamBladeAndRestoresAfterResetAndFreshLoad(GateKind kind, SimulationCadence cadence)
    {
        foreach (var supply in new[] { true, false })
        {
            var bench = kind switch
            {
                GateKind.Both => BothGateTests.Machine(ElectricalInputState.Both, supply),
                GateKind.Nand => ElectricalNandTests.SwitchMachine(ElectricalInputState.FirstOnly, supply),
                GateKind.Nor => ElectricalNorTests.SwitchMachine(ElectricalInputState.Neither, supply),
                GateKind.Or => ElectricalOrTests.SwitchMachine(ElectricalInputState.FirstOnly, supply),
                GateKind.Xor => ElectricalXorTests.SwitchMachine(ElectricalInputState.FirstOnly, supply),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            bench = bench with { Settings = bench.Settings with { Simulation = cadence } };
            var gate = kind == GateKind.Both ? 4UL : 8UL;
            var consumer = kind == GateKind.Both ? 5UL : 9UL;
            var rate = (int)bench.Settings.SimulationRate.Numerator;
            var saved = new WorkshopSavedConstruction(bench, new(10));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved, loaded);
            foreach (var construction in new[] { bench, loaded.Construction })
            {
                await using var harness = await WorkshopIntegrationHarness.Building(construction);
                var built = harness.Committed;
                await harness.Run();
                await harness.Advance(3 * rate);
                var logic = harness.Committed.Presentation.Logic.ToArray().Single(row => row.Owner.Value == gate);
                var blade = harness.Committed.Presentation.Actuators.ToArray().Single(row => row.Owner.Value == consumer);
                Assert.Equal(supply, logic.Supplied);
                Assert.Equal(supply, logic.Active);
                Assert.Equal(supply, blade.SupplyClosed);
                Assert.True(supply ? blade.Coordinate > 1.0f : blade.Coordinate < .2f,
                    "Only separately supplied logic may open the downstream blade.");
                var outputLamp = Assert.Single(harness.Presentation!.Advance(harness.Schedule,
                    PhysicalTime.FromNanoseconds(harness.Time.Nanoseconds)), sample => sample.Owner.Value == gate && sample.Binding == 6);
                Assert.True(supply ? outputLamp.Value.ColourChannel.Value > .9f : outputLamp.Value.ColourChannel.Value < .4f,
                    "The output lamp visibly distinguishes supplied output from off.");
                await harness.Reset();
                NoFixedCapacityTests.AssertSameState(built, harness.Committed, "logic Reset");
                Assert.Equal(bench, harness.Construction);
            }
        }
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task AClockCountsTwoPulsesThenTheSuppliedCounterOpensItsGate(SimulationCadence cadence)
    {
        foreach (var clockPowered in new[] { true, false })
        {
            var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default() with { Simulation = cadence },
                new(PlacedParts.Battery(new(1), -6, 3, 0),
                    PlacedParts.Clock(new(2), -3, 3, 0, 0, 0, 0, 1, .5f),
                    PlacedParts.Counter(new(3), 0, 3, 0, 0, 0, 0, 1, 2),
                    WorkshopInput.Generic(PartId.PoweredGate, new(4), 5, 3, 0, 0, 0, 0, 1)),
                Connections: new(Power(1, 2), Power(1, 3), Signal(2, 3), Power(3, 4)));
            if (!clockPowered) bench = bench with { Connections = bench.Connections.Without(Power(1, 2)) };
            var rate = (int)bench.Settings.SimulationRate.Numerator;
            var saved = new WorkshopSavedConstruction(bench, new(5));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved, loaded);
            foreach (var construction in new[] { bench, loaded.Construction })
            {
                await using var harness = await WorkshopIntegrationHarness.Building(construction);
                var built = harness.Committed;
                await harness.Run();
                await harness.Advance(rate / 2 - 1);
                Assert.Equal(0U, Memory(harness.Committed, 3).Count);
                await harness.Advance(1);
                Assert.Equal(clockPowered ? 1U : 0U, Memory(harness.Committed, 3).Count);
                Assert.False(Blade(harness.Committed, 4).SupplyClosed);
                await harness.Advance(rate / 2);
                Assert.Equal(clockPowered ? 2U : 0U, Memory(harness.Committed, 3).Count);
                await harness.Advance(rate);
                Assert.Equal(clockPowered, Blade(harness.Committed, 4).SupplyClosed);
                Assert.True(clockPowered ? Blade(harness.Committed, 4).Coordinate > 1 : Blade(harness.Committed, 4).Coordinate < .2f);
                await harness.Reset();
                NoFixedCapacityTests.AssertSameState(built, harness.Committed, "clock-counter Reset");
            }
        }
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task ADelayedImpactHoldsASuppliedContactForItsAuthoredDuration(SimulationCadence cadence)
    {
        foreach (var supply in new[] { true, false })
        {
            var bench = HoldTimerTests.Construction(.5f, supply, delayed: true);
            bench = bench with { Settings = bench.Settings with { Simulation = cadence } };
            var rate = (int)bench.Settings.SimulationRate.Numerator;
            var saved = new WorkshopSavedConstruction(bench, new(7));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved, loaded);
            foreach (var construction in new[] { bench, loaded.Construction })
            {
                var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(construction, 3 * rate);
                var start = reads.First(read => HoldTimerTests.State(read).Phase == ActivationTimerPhase.Counting);
                var state = HoldTimerTests.State(start);
                Assert.Equal((ulong)(rate / 2), state.DueTick - state.StartedTick);
                var holding = reads.Where(read => read.Tick.Value >= state.StartedTick && read.Tick.Value < state.DueTick).ToArray();
                Assert.Equal(rate / 2, holding.Length);
                Assert.All(holding, read => Assert.Equal(supply, Blade(read, 5).SupplyClosed));
                Assert.All(reads.Where(read => read.Tick.Value >= state.DueTick), read => Assert.False(Blade(read, 5).SupplyClosed));
                Assert.Equal(supply, reads.Any(read => Blade(read, 5).Coordinate > .2f));
                NoFixedCapacityTests.AssertSameState(built, reset, "hold Reset");
            }
        }
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task AWeightLoadsItsPlateAndOnlyTheWiredSupplyOpensTheGate(SimulationCadence cadence)
    {
        foreach (var onPlate in new[] { true, false })
        {
            var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default() with { Simulation = cadence },
                new(WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, 0, 1),
                    PlacedParts.Battery(new(2), 3, 3, 0), PlacedParts.Plate(new(3), -4, -.24f, 0, .5f),
                    WeightTests.Placed(4, onPlate ? -4 : -7, .2, 0, 4)),
                Connections: new(Power(2, 3), Power(3, 1)));
            var rate = (int)bench.Settings.SimulationRate.Numerator;
            var saved = new WorkshopSavedConstruction(bench, new(5));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved, loaded);
            foreach (var construction in new[] { bench, loaded.Construction })
            {
                var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(construction, 3 * rate);
                Assert.Equal(onPlate, Blade(reads[^1], 1).SupplyClosed);
                Assert.True(onPlate ? Blade(reads[^1], 1).Coordinate > 1 : Blade(reads[^1], 1).Coordinate < .2f);
                NoFixedCapacityTests.AssertSameState(built, reset, "weight-plate Reset");
            }
        }
    }


    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task AnImpactSetsTheSuppliedLatchAndLightsItsLampWhileTheDisconnectedControlStaysOff(SimulationCadence cadence)
    {
        foreach (var connected in new[] { true, false })
        {
            var bench = LatchSupplyFixtureTests.Machine(null);
            bench = bench with { Settings = bench.Settings with { Simulation = cadence } };
            if (!connected) bench = bench with { Connections = bench.Connections.Without(
                new WorkshopConnection(new(4), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.SetIn, WorkshopConnectionDomain.Activation)) };
            var rate = (int)bench.Settings.SimulationRate.Numerator;
            var saved = new WorkshopSavedConstruction(bench, new(10));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved, loaded);
            foreach (var construction in new[] { bench, loaded.Construction })
            {
                await using var harness = await WorkshopIntegrationHarness.Building(construction);
                var built = harness.Committed;
                await harness.Run();
                await harness.Advance(3 * rate);
                Assert.Contains(harness.Records, row => row.Kind == SimulationEventKind.TriggerActivated && row.Source == 4 && row.Other == 5);
                Assert.Equal(connected, Blade(harness.Committed, 3).SupplyClosed);
                Assert.True(connected ? Blade(harness.Committed, 3).Coordinate > 1 : Blade(harness.Committed, 3).Coordinate < .2f);
                var lamp = Enumerable.Range(0, harness.Committed.Activations.Count).Select(i => harness.Committed.Activations[i]).Single(row => row.Owner.Value == 8);
                Assert.Equal(connected ? ActivationPhase.Latched : ActivationPhase.Clear, lamp.Phase);
                await harness.Reset();
                NoFixedCapacityTests.AssertSameState(built, harness.Committed, "latch Reset");
            }
        }
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task BothPayloadsReachTheirReceiverRegardlessOfPlacementOrderAndResetFromMotion(SimulationCadence cadence)
    {
        foreach (var payload in new[] { PartId.Basketball, PartId.BowlingBall })
        foreach (var reverse in new[] { false, true })
        foreach (var aligned in new[] { true, false })
        {
            IWorkshopInstance[] parts =
            [
                WorkshopInput.Generic(payload, new(1), aligned ? 0 : 2, 2, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1)
            ];
            if (reverse) Array.Reverse(parts);
            var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default() with { Simulation = cadence }, new(parts));
            var rate = (int)bench.Settings.SimulationRate.Numerator;
            var saved = new WorkshopSavedConstruction(bench, new(3));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved, loaded);
            foreach (var construction in new[] { bench, loaded.Construction })
            {
                await using var harness = await WorkshopIntegrationHarness.Building(construction);
                var built = harness.Committed;
                await harness.Run();
                await harness.Advance(rate / 10);
                Assert.True(harness.Committed.Bodies.TryGet(new(1), out var moving));
                Assert.True(moving.Body.Velocity.Y < -.1f, "The payload is moving before Reset.");
                await harness.Reset();
                NoFixedCapacityTests.AssertSameState(built, harness.Committed, "moving payload Reset");
                await harness.Run();
                await harness.Advance(3 * rate);
                Assert.Equal(aligned, harness.Records.Any(row => row.Kind == SimulationEventKind.Captured && row.Other == 1));
                if (!aligned) Assert.DoesNotContain(harness.Records, row => row.Kind == SimulationEventKind.Captured);
                await harness.Reset();
                NoFixedCapacityTests.AssertSameState(built, harness.Committed, "payload Reset");
            }
        }
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task AFunnelGuidesAnOffsetBallThroughTheDetectorAndLightsItsLamp(SimulationCadence cadence)
    {
        foreach (var guided in new[] { true, false })
        {
            const float turn = -.70710677f, w = .70710677f;
            var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default() with { Simulation = cadence },
                new(WorkshopInput.Generic(PartId.Funnel, new(1), 0, 5, 0, 0, 0, turn, w),
                    WorkshopInput.Generic(PartId.BallDetector, new(2), 0, 3.5f, 0, 0, 0, turn, w),
                    WorkshopInput.Generic(PartId.SignalLamp, new(3), 4, 3, 0, 0, 0, 0, 1),
                    WorkshopInput.Generic(PartId.Basketball, new(4), .8f, 6.5f, 0, 0, 0, 0, 1)),
                Connections: new(Signal(2, 3)));
            if (!guided) bench = bench.WithoutInstance(new(1));
            var rate = (int)bench.Settings.SimulationRate.Numerator;
            var saved = new WorkshopSavedConstruction(bench, new(5));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved, loaded);
            foreach (var construction in new[] { bench, loaded.Construction })
            {
                await using var harness = await WorkshopIntegrationHarness.Building(construction);
                var built = harness.Committed;
                await harness.Run();
                await harness.Advance(4 * rate);
                Assert.Equal(guided, harness.Records.Any(row => row.Kind == SimulationEventKind.Passage && row.Source == 2 && row.Other == 4));
                var lamp = Enumerable.Range(0, harness.Committed.Activations.Count).Select(i => harness.Committed.Activations[i]).Single(row => row.Owner.Value == 3);
                Assert.Equal(guided ? ActivationPhase.Latched : ActivationPhase.Clear, lamp.Phase);
                await harness.Reset();
                NoFixedCapacityTests.AssertSameState(built, harness.Committed, "funnel detector Reset");
            }
        }
    }

    [Fact]
    public void EveryAdmittedPartHasAnExecutedNamedInteraction()
    {
        // LaunchLessonSolveTests: routing/impact/toppling/receiver goals and one-cause controls.
        PartId[] lessonInteractions = [PartId.Ramp, PartId.Springboard, PartId.Domino, PartId.PinballBumper,
            PartId.Wall, PartId.Delay, PartId.Pipe, PartId.PipeBend45, PartId.PipeBend90, PartId.Receiver];
        // The methods above: capture, supplied logic, physical activation, intentional timing/counts and direct load.
        PartId[] interactions = [PartId.Basketball, PartId.BowlingBall, PartId.Funnel, PartId.BallDetector,
            PartId.SignalLamp, PartId.Battery, PartId.ImpactSwitch, PartId.PoweredGate, PartId.Latch,
            PartId.Clock, PartId.Counter, PartId.PressurePlate, new("weight"), new("hold_timer"),
            new("both_gate"), new("electrical_nand"), new("electrical_nor"), new("electrical_or"), new("electrical_xor")];
        var expected = lessonInteractions.Concat(interactions).Order().ToArray();
        var actual = PartRegistry.Read(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(),
            "elements/elements.bundle.json"))).Select(row => new PartId(row.Metadata.Id)).Order().ToArray();
        Assert.Equal(actual, expected);
    }

    private static WorkshopConnection Power(ulong source, ulong target) =>
        new(new(source), WorkshopSocket.Supply, new(target), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopConnection Signal(ulong source, ulong target) =>
        new(new(source), WorkshopSocket.ActivationOut, new(target), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static ActuatorDriveRead Blade(WorkshopRead read, ulong owner) =>
        read.Presentation.Actuators.ToArray().Single(row => row.Owner.Value == owner);
    private static ActivationMemory Memory(WorkshopRead read, ulong owner) =>
        Enumerable.Range(0, read.Memory.Count).Select(i => read.Memory[i]).Single(row => row.Node.Value == owner);
}
