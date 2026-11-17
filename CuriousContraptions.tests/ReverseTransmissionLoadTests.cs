using System;
using System.Linq;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed partial class ReverseTransmissionTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    public async Task ADeclaredOutputLoadReactsThroughReverseToTheSource(int sign, bool stopped)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var template = TestBundle.Entry(Reverse);
        var hinge = template.Mechanical!.Hinges[0];
        if (stopped) hinge = hinge with { Travel = RevoluteTravel.Limited,
            LowerLimit = sign > 0 ? -.5f : 0f, UpperLimit = sign > 0 ? 0f : .5f };
        var entry = template with
        {
            Metadata = template.Metadata with { Id = "finite_shaft_load" },
            Footprint = new(2, 1, 0, 1),
            Mechanical = template.Mechanical with { Hinges = [hinge], Transmissions = [] },
            Ports = [template.Ports.Single(port => port.Id == PortId.DriveIn)],
            Presentation = template.Presentation with { Bodies = [], Bindings = [] }
        };
        using var declared = DeclaredPhysics.Scoped([entry, SignedSource()]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var construction = Simple(sign: sign, signedSource: true);
        construction = construction with { Instances = new(construction.Instances.Append(Part(new(entry.Metadata.Id), 4, 5f)).ToArray()),
            Connections = construction.Connections.With(Drive(3, 4)) };
        await h.Require(WorkshopCommandKind.Construct, construction);
        await h.Run(); await h.Advance(60);
        var halfway = Coordinate(h, new(3), 0).Coordinate;
        await h.Advance(60);
        var input = Coordinate(h, new(3), 0);
        var output = Coordinate(h, new(3), 1);
        var load = Coordinate(h, new(4), 0);
        if (stopped)
        {
            var sourceSlot = Array.FindIndex(h.Simulation.Scene.Physics.Revolutes.ToArray(),
                row => row.Id == h.Construction.Instances.Identities[new(2)].Hinges[0].Joint);
            var loadSlot = Array.FindIndex(h.Simulation.Scene.Physics.Revolutes.ToArray(),
                row => row.Id == h.Construction.Instances.Identities[new(4)].Hinges[0].Joint);
            var bytes = h.Device.CommittedWorld.ToArray();
            var state = PhysicsGpuAbi.ReadRevoluteState(bytes, sourceSlot);
            var loadBody = h.Construction.Instances.Identities[new(4)].Hinges[0].Body;
            Assert.True(h.Committed.Bodies.TryGet(loadBody, out var loadPose));
            var poseAngle = 2f * MathF.Atan2(loadPose.Rotation.Z, loadPose.Rotation.W);
            var stopKind = sign > 0 ? RevoluteRowKind.UpperStop : RevoluteRowKind.LowerStop;
            var reaction = System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(
                PhysicsGpuAbi.Layout(bytes).RevoluteOffset(loadSlot) + PhysicsGpuAbi.RevoluteImpulseOffset + 4 * (int)stopKind));
            // A visible index must remain within the existing one-second inactive envelope.
            Assert.True(MathF.Abs(input.Coordinate) <= .01f,
                $"Stopped chain travel input={input.Coordinate}, output={output.Coordinate}, load={load.Coordinate}; speeds={input.Speed},{output.Speed},{load.Speed}; halfInput={halfway}, loadPoseAngle={poseAngle}, sourceWork={state.Work}, stopImpulse={reaction}.");
            Assert.InRange(output.Coordinate, -.01f, .01f);
            Assert.InRange(load.Coordinate, -.01f, .01f);
            var slot = Array.FindIndex(h.Simulation.Scene.Physics.Revolutes.ToArray(),
                row => row.Id == h.Construction.Instances.Identities[new(2)].Hinges[0].Joint);
            var source = PhysicsGpuAbi.ReadRevoluteState(h.Device.CommittedWorld.ToArray(), slot);
            Assert.True(source.Supplied);
            Assert.InRange(source.Work, 0f, 20f * .01f); // Declared torque times permitted visible shaft travel.
            Assert.InRange(poseAngle, -.01f, .01f);
            Assert.True(reaction > 0f, "The stopped output must transmit an actual opposing constraint reaction.");
            var beforePause = h.Simulation.Scene.Physics.Transmissions.ToArray().Select((_, index) =>
                PhysicsGpuAbi.ReadMechanicalTransmissionState(h.Device.CommittedWorld.ToArray(), index).Impulse).ToArray();
            Assert.Contains(beforePause, impulse => MathF.Abs(impulse) > .001f);
            await h.Require(WorkshopCommandKind.Pause);
            foreach (var cadence in new[] { SimulationCadence.Hz60, SimulationCadence.Hz240 })
            {
                var refused = await h.Execute(WorkshopCommandKind.ConfigureCadence, settings: h.Construction.Settings with { Simulation = cadence });
                Assert.Equal(new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase), refused.Result);
                var retained = h.Simulation.Scene.Physics.Transmissions.ToArray().Select((_, index) =>
                    PhysicsGpuAbi.ReadMechanicalTransmissionState(h.Device.CommittedWorld.ToArray(), index).Impulse).ToArray();
                Assert.Equal(beforePause, retained); // Paused/rejected configuration cannot apply or erase a committed reaction.
            }
            await h.Require(WorkshopCommandKind.Resume); await h.Advance(24);
            Assert.InRange(Coordinate(h, new(3), 0).Coordinate, -.01f, .01f);
            await h.Reset();
            Assert.All(h.Simulation.Scene.Physics.Transmissions.ToArray().Select((_, index) =>
                PhysicsGpuAbi.ReadMechanicalTransmissionState(h.Device.CommittedWorld.ToArray(), index)),
                value => Assert.Equal(0f, value.Impulse));
            foreach (var cadence in new[] { SimulationCadence.Hz60, SimulationCadence.Hz240 })
            {
                await h.Require(WorkshopCommandKind.ConfigureCadence, settings: h.Construction.Settings with { Simulation = cadence });
                Assert.All(h.Simulation.Scene.Physics.Transmissions.ToArray().Select((_, index) =>
                    PhysicsGpuAbi.ReadMechanicalTransmissionState(h.Device.CommittedWorld.ToArray(), index)),
                    value => Assert.Equal(0f, value.Impulse));
                await h.Run(); await h.Advance((int)h.Construction.Settings.SimulationRate.Numerator);
                Assert.InRange(Coordinate(h, new(3), 0).Coordinate, -.01f, .01f);
                await h.Reset();
            }
        }
        else
        {
            Assert.True(sign * output.Coordinate > .1f, "Removing the hard stop must permit useful output travel.");
            Assert.True(-sign * input.Coordinate > .1f);
            Assert.InRange(output.Speed - load.Speed, -CoupledSpeedAllowance, CoupledSpeedAllowance);
            await AssertMovingEnergyAndSupplyLoss(h, new(2),
                [(new(2), 0, .0225f), (new(3), 0, .0128f), (new(3), 1, .0128f), (new(4), 0, .0128f)]);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task AnOutputDriveBackdrivesBothReverseShaftsAndTheUnpoweredMotor(int sign)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var template = SignedSource();
        var drive = template.Ports.Single(port => port.Id == PortId.Drive);
        var entry = template with { Metadata = template.Metadata with { Id = "supplied_shaft_load" },
            Ports = template.Ports.Where(port => port.Id != PortId.Drive).Append(
                drive with { Id = PortId.DriveIn, Direction = PortDirection.Input, Role = PortRole.DriveIn }).ToArray() };
        using var declared = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var load = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(4), 5f, 4f, 0f, 0f, 0f, 0f, 1f,
            new([new(new("speed"), sign * 6f), new(new("torque"), 20f)]));
        var source = Simple();
        var construction = source with { Instances = new(source.Instances.Append(load).ToArray()),
            Connections = new(Drive(2, 3), Drive(3, 4), Power(4)) };
        await h.Require(WorkshopCommandKind.Construct, construction);
        await h.Run(); await h.Advance(120);
        var input = Coordinate(h, new(3), 0); var output = Coordinate(h, new(3), 1);
        Assert.True(sign * input.Speed > .1f);
        Assert.True(-sign * output.Speed > .1f);
        Assert.True(-sign * Coordinate(h, new(2), 0).Speed > .1f, "Output load must transmit reaction through the passive reverser.");
        Assert.InRange(input.Speed + output.Speed, -CoupledSpeedAllowance, CoupledSpeedAllowance);
        await AssertMovingEnergyAndSupplyLoss(h, new(4),
            [(new(2), 0, .0225f), (new(3), 0, .0128f), (new(3), 1, .0128f), (new(4), 0, .0225f)]);
    }

    private static async Task AssertMovingEnergyAndSupplyLoss(WorkshopIntegrationHarness h, GpuBodyId source,
        (GpuBodyId Owner, int Hinge, float Inertia)[] shafts)
    {
        // Fixed-axis rotors start at rest: initial kinetic energy is zero. Solid-disc axial inertias
        // are independently m*r²/2: Motor .5kg/.3m and Reverse/load .25kg/.32m.
        float Energy() => shafts.Sum(shaft =>
        {
            var speed = Coordinate(h, shaft.Owner, shaft.Hinge).Speed;
            return .5f * shaft.Inertia * speed * speed;
        });
        var slot = Array.FindIndex(h.Simulation.Scene.Physics.Revolutes.ToArray(),
            row => row.Id == h.Construction.Instances.Identities[source].Hinges[0].Joint);
        float Work() => PhysicsGpuAbi.ReadRevoluteState(h.Device.CommittedWorld.ToArray(), slot).Work;
        // One fixed energy allowance from the defined 0.05rad/s visible speed mismatch at authored 6rad/s;
        // not a per-step budget or a bound fitted to solver output.
        var allowance = shafts.Sum(shaft => .5f * shaft.Inertia *
            (2f * 6f * CoupledSpeedAllowance + CoupledSpeedAllowance * CoupledSpeedAllowance));
        var drivenEnergy = Energy();
        Assert.True(drivenEnergy > .1f, "The energy check must exercise moving finite inertia.");
        Assert.True(drivenEnergy <= Work() + allowance,
            $"Moving shaft energy {drivenEnergy} exceeds supplied work {Work()} plus allowance {allowance}.");
        await h.Require(WorkshopCommandKind.ConfigureRunControl, runControl:
            h.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var workAtCut = Work();
        await h.Advance(30);
        Assert.Equal(workAtCut, Work());
        Assert.True(Energy() <= drivenEnergy + allowance,
            "The passive loaded/backdriven chain cannot create kinetic energy after supply loss.");
    }
}
