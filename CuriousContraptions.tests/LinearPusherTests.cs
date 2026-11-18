using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class LinearPusherTests
{
    private static readonly GpuBodyId Pusher = new(1), Power = new(2), Positive = new(3), Negative = new(4), Gate = new(5);
    private static readonly PortId Extend = new("ExtendIn"), Retract = new("RetractIn"), Extended = new("ExtendedOut");
    private static ActuatorDriveRead Read(WorkshopRead read) => read.Presentation.Actuators.ToArray().Single(row => row.Owner == Pusher);
    private static PrismaticMotorRead Motor(WorkshopRead read) => Read(read).Motor!.Value;

    [Fact]
    public void PackageInstallationRoundTripsItsDriveExtrusionAndPhaseBinding()
    {
        var scene = CompiledSceneFixture.Compile(Machine(true, true, false));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, scene,
            Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Scene!;
        decoded.Validate();
        var drive = decoded.Physics.PrismaticDrives.ToArray().Single(row => row.Selection == PrismaticTargetSelection.Directional);
        Assert.Equal(60f, drive.MaxPower);
        Assert.Single(decoded.Physics.Colliders.ToArray(), row => row.Extrusion.HasValue);
        Assert.Equal(3, decoded.Presentation.Count(row => row.Declaration.Driver == PresentationDriver.CommittedPrismaticPhase));
        var world = PhysicsGpuAbi.Admission(decoded.Physics, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        _ = PhysicsGpuAbi.ValidateCandidate(world, world, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
    }

    [Theory]
    [InlineData(false, true, false, PrismaticDrivePhase.Unpowered)]
    [InlineData(true, false, false, PrismaticDrivePhase.Holding)]
    [InlineData(true, true, true, PrismaticDrivePhase.Conflict)]
    [InlineData(true, false, true, PrismaticDrivePhase.Holding)]
    public async Task IndependentElectricalInputsHoldWithoutDeliveringWork(bool power, bool positive, bool negative, PrismaticDrivePhase phase)
    {
        var reads = await WorkshopIntegrationHarness.Observe(Machine(power, positive, negative), 120);
        Assert.Equal(0UL, reads[0].Tick.Value);
        Assert.Equal(PrismaticDrivePhase.Unpowered, Motor(reads[0]).Phase); // Admission has not advanced the controller.
        Assert.All(reads.Skip(1), read =>
        {
            Assert.Equal(phase, Motor(read).Phase);
            Assert.Equal(0f, Motor(read).Work);
            Assert.InRange(Read(read).Coordinate, -.005f, .005f); // A held head must not visibly leave its authored home.
        });
    }

    [Fact]
    public async Task ExtensionReportsMotorWorkAndEndpointPowerOnTheNextElectricalTick()
    {
        var reads = await WorkshopIntegrationHarness.Observe(Machine(true, true, false), 300);
        var reached = reads.FindIndex(read => Read(read).Coordinate >= 1f - PhysicalTolerance.ActuatorStopSlopMetres);
        Assert.True(reached > 0, "configured one-metre stroke must finish within the generous 2.5-second guard");
        var powered = reads.FindIndex(read => read.Presentation.Actuators.ToArray().Single(row => row.Owner == Gate).SupplyClosed);
        Assert.Equal(reached + 1, powered); // Declared next-electrical-tick causality, not an incidental arrival deadline.
        Assert.Equal(PrismaticDrivePhase.Holding, Motor(reads[^1]).Phase);
        Assert.True(Motor(reads[^1]).Work > 0f);
        Assert.All(reads, read => Assert.InRange(Motor(read).TickWork, 0f, 40f * 1.5f / 120f + 1e-6f));
    }

    [Fact]
    public async Task BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval()
    {
        GpuBodyId lowerConsumer = new(20);
        var machine = Machine(true, false, false);
        machine = machine with
        {
            Instances = new(machine.Instances.Append(WorkshopInput.Generic(RuntimeTestServices.Bundle,
                new("powered_gate"), lowerConsumer, 10, 3, 0, 0, 0, 0, 1)).ToArray()),
            Connections = new(machine.Connections.Append(new WorkshopConnection(Pusher, new("RetractedOut"),
                lowerConsumer, PortId.PowerIn, WorkshopConnectionDomain.Electrical)).ToArray())
        };
        var phases = await WorkshopIntegrationHarness.ObservePhases(machine,
            ([], 2),
            ([new(Power, RunControlValue.Of(ElectricalEnable.Disabled))], 2),
            ([new(Power, RunControlValue.Of(ElectricalEnable.Enabled))], 2),
            ([new(Positive, RunControlValue.Of(ElectricalEnable.Enabled))], 300),
            ([new(Power, RunControlValue.Of(ElectricalEnable.Disabled))], 2),
            ([new(Power, RunControlValue.Of(ElectricalEnable.Enabled))], 2),
            ([new(Positive, RunControlValue.Of(ElectricalEnable.Disabled)),
              new(Negative, RunControlValue.Of(ElectricalEnable.Enabled))], 300));
        var previous = 0f;
        for (var phase = 0; phase < phases.Count; phase++)
        foreach (var read in phases[phase])
        {
            var supplied = phase is not (1 or 4);
            var actuators = read.Presentation.Actuators.ToArray();
            Assert.Equal(supplied && previous <= PhysicalTolerance.ActuatorStopSlopMetres,
                actuators.Single(row => row.Owner == lowerConsumer).SupplyClosed);
            Assert.Equal(supplied && previous >= 1f - PhysicalTolerance.ActuatorStopSlopMetres,
                actuators.Single(row => row.Owner == Gate).SupplyClosed);
            previous = Read(read).Coordinate;
        }
        Assert.True(Read(phases[3][^1]).Coordinate >= 1f - PhysicalTolerance.ActuatorStopSlopMetres);
        Assert.True(Read(phases[6][^1]).Coordinate <= PhysicalTolerance.ActuatorStopSlopMetres);
        Assert.Contains(phases[3], read => Read(read).Coordinate is > .2f and < .8f);
        Assert.Contains(phases[6], read => Read(read).Coordinate is > .2f and < .8f);
    }

    [Fact]
    public async Task LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt()
    {
        var phases = await WorkshopIntegrationHarness.ObservePhases(Machine(true, true, false),
            ([], 45),
            ([new(Power, RunControlValue.Of(ElectricalEnable.Disabled))], 120),
            ([new(Power, RunControlValue.Of(ElectricalEnable.Enabled)),
              new(Positive, RunControlValue.Of(ElectricalEnable.Disabled)),
              new(Negative, RunControlValue.Of(ElectricalEnable.Enabled))], 180));
        var interrupted = Read(phases[0][^1]).Coordinate;
        Assert.InRange(interrupted, .1f, .9f);
        Assert.All(phases[1], read =>
        {
            Assert.Equal(PrismaticDrivePhase.Unpowered, Motor(read).Phase);
            Assert.InRange(Read(read).Coordinate, interrupted - .02f, interrupted + .02f);
            Assert.Equal(0f, Motor(read).TickWork);
        });
        Assert.InRange(Read(phases[2][^1]).Coordinate, -.005f, .005f);
        Assert.Equal(PrismaticDrivePhase.Holding, Motor(phases[2][^1]).Phase);
    }

    [Fact]
    public async Task ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot()
    {
        async Task<List<WorkshopRead>> Run(float wallZ)
        {
            var machine = Machine(true, true, false);
            var wall = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(6),
                1.7f, 3f, wallZ, 0, 0, 0, 1,
                new([new(new("width"), .4f), new(new("height"), 2f), new(new("thickness"), .4f)]));
            return await WorkshopIntegrationHarness.Observe(machine with
                { Instances = new(machine.Instances.Append(wall).ToArray()) }, 360);
        }
        var blocked = await Run(0f);
        var clear = await Run(1f);
        // Wall near face 1.5 m minus 25 cm head radius and .85 m rest centre gives q=.4 m.
        Assert.InRange(Read(blocked[^1]).Coordinate, .38f, .42f);
        Assert.Equal(PrismaticDrivePhase.Blocked, Motor(blocked[^1]).Phase);
        Assert.InRange(Read(clear[^1]).Coordinate, .98f, 1.02f);
        Assert.Equal(PrismaticDrivePhase.Holding, Motor(clear[^1]).Phase);
        var settledWork = Motor(blocked[240]).Work;
        Assert.InRange(Motor(blocked[^1]).Work - settledWork, 0f, .005f); // A stationary obstruction must not create useful mechanical energy.
    }

    [Fact]
    public async Task SaveLoadPauseStepAndResetPreserveCommandsParametersAndAuthoredState()
    {
        var authored = new WorkshopSavedConstruction(Machine(true, true, false), new(6));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands,
            RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, authored));
        Assert.Equal(authored, loaded); // Canonical authored fields and connections, not serialized bytes.
        await using var harness = await WorkshopIntegrationHarness.Building(loaded.Construction);
        var initial = Read(harness.Committed);
        await harness.Run();
        await harness.Advance(45);
        Assert.True(Read(harness.Committed).Coordinate > .1f);
        await harness.Require(WorkshopCommandKind.Pause);
        var pausedTick = harness.Committed.Tick.Value;
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(pausedTick + 1, harness.Committed.Tick.Value);
        await harness.Require(WorkshopCommandKind.Resume);
        await harness.Advance(30);
        Assert.True(Motor(harness.Committed).Work > 0f);
        await harness.Reset();
        Assert.Equal(initial.Coordinate, Read(harness.Committed).Coordinate);
        Assert.Equal(initial.Lower, Read(harness.Committed).Lower);
        Assert.Equal(initial.Upper, Read(harness.Committed).Upper);
        Assert.Equal(0f, Motor(harness.Committed).Work);
        Assert.Equal(PrismaticDrivePhase.Unpowered, Motor(harness.Committed).Phase);
        await harness.Run();
        await harness.Advance(240);
        Assert.InRange(Read(harness.Committed).Coordinate, .98f, 1.02f);
    }

    [Fact]
    public async Task HeadTransfersLoadThroughCargoToARopeAndDisconnectingTheRopeReleasesTravel()
    {
        const float cargoX = 1.15f + .15932827f + 8f * .04808397f;
        const float tieY = 2.7f + .5771f * (.453070f + 8f * .136734f);
        static float X(WorkshopRead read)
        {
            Assert.True(read.Bodies.TryGet(new(6), out var found));
            var body = found.Body;
            var rotation = found.Rotation;
            var offset = Vector3.Transform(new(0f, tieY - 2.7f, 0f),
                new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));
            return (body.Cell.X + body.Local.X) / 16f + offset.X;
        }
        WorkshopConstruction Fixture(bool connected)
        {
            var machine = Machine(true, true, false);
            var pusher = (WorkshopGenericInstance)machine.Instances[0];
            pusher = pusher with { Parameters = new([new(new("stroke"), 1.5f), new(new("speed"), .5f),
                new(new("acceleration"), 2f), new(new("force"), 100f)]) };
            var cargo = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("weight"), new(6),
                cargoX, 2.7f, 0f, 0, 0, 0, 1, new([new(new("mass"), 8f)]));
            var bench = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(7),
                4.1f, 2.5f, 0f, 0, 0, 0, 1,
                new([new(new("width"), 6f), new(new("height"), .4f), new(new("thickness"), 2f)]));
            var anchor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("rope_anchor"), new(8),
                cargoX - 4f, tieY, -.18f, 0, 0, 0, 1);
            return machine with
            {
                Instances = new(machine.Instances.Select(part => part.Id == Pusher ? pusher : part)
                    .Append(cargo).Append(bench).Append(anchor).ToArray()),
                Connections = connected ? machine.Connections.With(new(new(8), PortId.Tie, new(6), PortId.Tie,
                    WorkshopConnectionDomain.Rope, 4.4f)) : machine.Connections
            };
        }
        var tied = await WorkshopIntegrationHarness.Observe(Fixture(true), 960);
        var free = await WorkshopIntegrationHarness.Observe(Fixture(false), 960);
        Assert.True(tied.Max(X) > cargoX + .1f, "The head must physically transfer load into the tethered cargo.");
        Assert.Contains(tied, read => read.Presentation.Ropes[0].State == RopeState.Taut);
        Assert.InRange(tied[^1].Presentation.Ropes[0].CurrentLength, 4.38f, 4.42f);
        Assert.True(X(free[^1]) > cargoX + .8f, "Removing only the rope must release useful cargo travel.");
        Assert.True(X(free[^1]) > X(tied[^1]) + .3f,
            $"The rope must restrain its actual Tie attachment: free={X(free[^1])}, tied={X(tied[^1])}.");
    }

    [Theory]
    [InlineData("stroke", .25f)] [InlineData("stroke", 3f)]
    [InlineData("speed", .25f)] [InlineData("speed", 4f)]
    [InlineData("acceleration", 1f)] [InlineData("acceleration", 30f)]
    [InlineData("force", 1f)] [InlineData("force", 100f)]
    public async Task ParameterEndpointsCompileAndProduceUsefulMotion(string key, float value)
    {
        var machine = Machine(true, true, false);
        var original = (WorkshopGenericInstance)machine.Instances[0];
        var parameters = new InstanceParameterCollection(original.Parameters.Select(p => p.Key == new ParameterId(key) ? new KeyValuePair<ParameterId, float>(p.Key, value) : p).ToArray());
        var changed = original with { Parameters = parameters };
        machine = machine with { Instances = new(machine.Instances.Select(p => p.Id == Pusher ? changed : p).ToArray()) };
        var compiled = CompiledSceneFixture.Compile(machine);
        var drive = compiled.Physics.PrismaticDrives.ToArray().Single(d => d.Selection == PrismaticTargetSelection.Directional);
        Assert.Equal(parameters[new("speed")], drive.MaxSpeed);
        Assert.Equal(parameters[new("acceleration")], drive.StoppingAcceleration);
        Assert.Equal(parameters[new("force")], drive.MaxEffort);
        Assert.Equal(parameters[new("speed")] * parameters[new("force")], drive.MaxPower);
        var reads = await WorkshopIntegrationHarness.Observe(machine, 240);
        Assert.Equal(parameters[new("stroke")], Read(reads[^1]).Upper);
        Assert.True(Read(reads[^1]).Coordinate > .1f, "Every admitted endpoint must produce at least 10 cm of useful stroke within two seconds.");
        Assert.True(Motor(reads[^1]).Work > 0f);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task OneSourceCanSupplyBothPowerAndCommandWithAnUnwiredCommandControl(bool commandConnected)
    {
        var machine = Machine(true, false, false);
        var wires = new List<WorkshopConnection> { new(Power, PortId.Supply, Pusher, PortId.PowerIn, WorkshopConnectionDomain.Electrical) };
        if (commandConnected) wires.Add(new(Power, PortId.Supply, Pusher, Extend, WorkshopConnectionDomain.Electrical));
        machine = machine with { Connections = new(wires.ToArray()) };
        var reads = await WorkshopIntegrationHarness.Observe(machine, 240);
        Assert.True(Read(reads[^1]).SupplyClosed);
        if (commandConnected)
        {
            Assert.True(Read(reads[^1]).Coordinate > .5f);
            Assert.True(Motor(reads[^1]).Work > 0f);
        }
        else
        {
            Assert.All(reads, r => Assert.InRange(Read(r).Coordinate, -.005f, .005f));
            Assert.Equal(0f, Motor(reads[^1]).Work);
        }
    }

    private static Vector3 Position(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var found));
        return new((found.Body.Cell.X + found.Body.Local.X) / 16f,
            (found.Body.Cell.Y + found.Body.Local.Y) / 16f, (found.Body.Cell.Z + found.Body.Local.Z) / 16f);
    }

    private static WorkshopConstruction CargoMachine(Quaternion rotation, float lateral, float force, bool vertical)
    {
        var machine = Machine(true, true, false);
        var origin = new Vector3(0, 3, 0);
        Vector3 World(Vector3 local) => origin + Vector3.Transform(local, rotation);
        var pusher = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("linear_pusher"), Pusher,
            origin.X, origin.Y, origin.Z, rotation.X, rotation.Y, rotation.Z, rotation.W,
            new([new(new("stroke"), 2.4f), new(new("speed"), 1.5f), new(new("acceleration"), 12f), new(new("force"), force)]));
        // Head radius .25 m, basketball outer envelope .35 m. The vertical initial gap is 1 cm.
        var cargoPosition = World(new(vertical ? 1.46f : 1.55f, 0f, lateral));
        var cargo = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(6),
            cargoPosition.X, cargoPosition.Y, cargoPosition.Z, 0, 0, 0, 1);
        var instances = new List<IWorkshopInstance> { pusher, cargo,
            PlacedParts.Battery(Power, 10, 3, 10), PlacedParts.Battery(Positive, 12, 3, 10),
            PlacedParts.Battery(Negative, 14, 3, 10, enabled: ElectricalEnable.Disabled) };
        if (!vertical)
        {
            // Bench starts at local x=1.2 m, beyond the initial head and base; its top supports the .35 m cargo envelope.
            var bench = World(new(3.2f, -.55f, .5f));
            instances.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(7),
                bench.X, bench.Y, bench.Z, rotation.X, rotation.Y, rotation.Z, rotation.W,
                new([new(new("width"), 4f), new(new("height"), .4f), new(new("thickness"), 2f)])));
        }
        return machine with { Instances = new(instances.ToArray()),
            Connections = new(machine.Connections.Where(link => link.Target == Pusher).ToArray()) };
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(90f)]
    [InlineData(180f)]
    public async Task RotatedSphericalHeadPushesCargoAndLateralClearanceMisses(float degrees)
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, degrees * MathF.PI / 180f);
        var axis = Vector3.Transform(Vector3.UnitX, rotation);
        var hit = await WorkshopIntegrationHarness.Observe(CargoMachine(rotation, 0f, 40f, false), 720);
        var miss = await WorkshopIntegrationHarness.Observe(CargoMachine(rotation, 1f, 40f, false), 720);
        float Advance(List<WorkshopRead> reads) => Vector3.Dot(Position(reads[^1], new(6)) - Position(reads[0], new(6)), axis);
        Assert.True(Advance(hit) > 1f, "The actual spherical head must move cargo by a useful metre in its rotated direction.");
        Assert.InRange(MathF.Abs(Advance(miss)), 0f, .02f); // No axial push across 1 m lateral clearance over six seconds.
        Assert.True(Motor(hit[^1]).Work > 0f);
    }

    [Theory]
    [InlineData(1f, false)]
    [InlineData(40f, true)]
    public async Task VerticalPackageLiftsCargoOnlyWithSufficientDeclaredForce(float force, bool lifts)
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var reads = await WorkshopIntegrationHarness.Observe(CargoMachine(rotation, 0f, force, true), 720);
        var initial = Position(reads[0], new(6)).Y;
        var lift = reads.Max(read => Position(read, new(6)).Y) - initial;
        if (lifts) Assert.True(lift > .5f, "40 N must lift the actual cargo more than half a metre within six seconds.");
        else Assert.InRange(lift, 0f, .02f); // 1 N cannot lift a 1 kg payload against 9.81 m/s² gravity.
    }

    [Fact]
    public async Task CargoTransferResetRestoresEveryAuthoredBodyAndSemanticReplay()
    {
        var authored = CargoMachine(Quaternion.Identity, 0f, 40f, false);
        var saved = new WorkshopSavedConstruction(authored, new(8));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, loaded);
        await using var harness = await WorkshopIntegrationHarness.Building(loaded.Construction);
        var initial = harness.Committed;
        for (var replay = 0; replay < 2; replay++)
        {
            await harness.Run(); await harness.Advance(480);
            Assert.True(Position(harness.Committed, new(6)).X > Position(initial, new(6)).X + 1f);
            Assert.True(Motor(harness.Committed).Work > 0f);
            Assert.True(Read(harness.Committed).Coordinate >= 2.4f - PhysicalTolerance.ActuatorStopSlopMetres);
            await harness.Reset();
            Assert.Equal(authored, harness.Construction); // All decoded poses, parameters, connections, settings and puzzle state.
            Assert.Equal(initial.Bodies.Count, harness.Committed.Bodies.Count);
            for (var index = 0; index < initial.Bodies.Count; index++)
            {
                var before = initial.Bodies[index];
                Assert.True(harness.Committed.Bodies.TryGet(before.Body.Id, out var after));
                Assert.Equal(before.Body.Cell, after.Body.Cell); Assert.Equal(before.Body.Local, after.Body.Local);
                Assert.Equal(before.Rotation, after.Rotation); Assert.Equal(before.Body.Velocity, after.Body.Velocity);
                Assert.Equal(before.AngularVelocity, after.AngularVelocity); Assert.Equal(before.LocalCentreOfMass, after.LocalCentreOfMass);
            }
            var head = authored.Instances.Identities[Pusher].Sliders[0].Body;
            Assert.Equal(Position(initial, head), Position(harness.Committed, head));
            Assert.Equal(0f, Motor(harness.Committed).Work);
            Assert.Equal(0f, Read(harness.Committed).Coordinate);
        }
    }

    internal static WorkshopConstruction Machine(bool power, bool positive, bool negative)
    {
        var pusher = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("linear_pusher"), Pusher, 0, 3, 0, 0, 0, 0, 1,
            new([new(new("stroke"), 1f), new(new("speed"), 1.5f), new(new("acceleration"), 12f), new(new("force"), 40f)]));
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("powered_gate"), Gate, 6, 3, 0, 0, 0, 0, 1);
        return new(new(2), WorkshopCadenceSettings.Default(),
            new(pusher, PlacedParts.Battery(Power, -3, 3, 0, enabled: power ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
                PlacedParts.Battery(Positive, -5, 3, 0, enabled: positive ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
                PlacedParts.Battery(Negative, -7, 3, 0, enabled: negative ? ElectricalEnable.Enabled : ElectricalEnable.Disabled), gate),
            Connections: new(
                new(Power, PortId.Supply, Pusher, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(Positive, PortId.Supply, Pusher, Extend, WorkshopConnectionDomain.Electrical),
                new(Negative, PortId.Supply, Pusher, Retract, WorkshopConnectionDomain.Electrical),
                new(Pusher, Extended, Gate, PortId.PowerIn, WorkshopConnectionDomain.Electrical)));
    }
}
