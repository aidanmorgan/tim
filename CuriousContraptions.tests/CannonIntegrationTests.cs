using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class CannonIntegrationTests
{


    [Fact]
    public async Task StaleCannonReplacementPreservesInstalledCapabilitiesAndPresentation()
    {
        var original = Vertical();
        await using var h = await WorkshopIntegrationHarness.Building(original);
        var heldRevision = h.Committed.Revision;
        var heldScene = CompiledSceneFixture.Compile(original with { Revision = new(3) }, new(99));
        var changed = Vertical(clockInterval: 3f) with { Revision = new(h.Construction.Revision.Value + 1) };
        await h.Require(WorkshopCommandKind.Construct, changed);
        var installed = h.Simulation.Scene;
        var before = h.Committed;
        var presentation = h.Presentation;
        var samples = presentation!.Advance(AnimationTestTime.Seconds(0)).ToArray();
        var fresh = h.Command(WorkshopCommandKind.Construct, original with { Revision = new(h.Construction.Revision.Value + 1) });
        var refused = await h.Dispatch(fresh with { Revision = heldRevision, Scene = heldScene });
        Assert.Equal(WorkshopCommandOutcome.Rejected, refused.Result.Outcome);
        // The worker rejects stale authority before attempting construction-revision admission.
        Assert.Equal(WorkshopRejection.StaleGeneration, refused.Result.Reason);
        Assert.Same(installed, h.Simulation.Scene);
        Assert.Equal(before.Revision, h.Committed.Revision);
        Assert.Equal(before.Epoch, h.Committed.Epoch);
        Assert.Equal(before.PhysicalRevision, h.Committed.PhysicalRevision);
        Assert.Equal(before.EnergyStores[0], h.Committed.EnergyStores[0]);
        Assert.Equal(before.ChamberRegions[0], h.Committed.ChamberRegions[0]);
        Assert.Equal(before.FiniteWorkActuators[0], h.Committed.FiniteWorkActuators[0]);
        Assert.Same(presentation, h.Presentation);
        Assert.Equal(samples, h.Presentation!.Advance(AnimationTestTime.Seconds(0)));
        Assert.Empty(h.Records);
        await h.Run();
        await h.Advance(390);
        Assert.Equal(1u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Single(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        await h.Reset();
        await h.Require(WorkshopCommandKind.Construct, original with { Revision = new(h.Construction.Revision.Value + 1) });
        Assert.NotEqual(installed.Identity, h.Simulation.Scene.Identity);
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultChargeTakesOneAndAHalfSecondsAndPowerLossRetainsPartialEnergy(bool removePower)
    {
        await using var h = await WorkshopIntegrationHarness.Running(Vertical(clockInterval: 12f));
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
        if (removePower)
        {
            await h.Advance(90); // 30 W * .75 s = 22.5 J, independently of physical arrival.
            var partial = h.Committed.EnergyStores[0];
            Assert.InRange(partial.Energy.Value - 22.5f, -.005f, .005f);
            await h.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: h.RunControl(new(4), RunControlLane.SourceEnable,
                    RunControlValue.Of(ElectricalEnable.Disabled)));
            var source = h.Committed.Electrical[0];
            await h.Advance(1); // The acknowledged control takes effect at the next physical boundary.
            Assert.Equal(ElectricalEnable.Disabled, h.Committed.Electrical[0].Enabled);
            Assert.Equal(0f, h.Committed.Electrical[0].Debit.Value);
            Assert.Equal(source.Remaining, h.Committed.Electrical[0].Remaining);
            Assert.Equal(partial with { CommitSupplied = default, CommitReleased = default, CommitDissipated = default, Transactions = 0 },
                h.Committed.EnergyStores[0]);
            var disabled = h.Committed.Electrical[0];
            await h.Advance(179);
            Assert.Equal(partial with { CommitSupplied = default, CommitReleased = default, CommitDissipated = default, Transactions = 0 },
                h.Committed.EnergyStores[0]);
            Assert.Equal(disabled, h.Committed.Electrical[0]);
            Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
            await h.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: h.RunControl(new(4), RunControlLane.SourceEnable,
                    RunControlValue.Of(ElectricalEnable.Enabled)));
            await h.Advance(90);
            Assert.Equal(ElectricalEnable.Enabled, h.Committed.Electrical[0].Enabled);
            Assert.True(h.Committed.Electrical[0].Remaining.Value < disabled.Remaining.Value);
        }
        else
        {
            await h.Advance(179);
            Assert.True(h.Committed.EnergyStores[0].Energy.Value < 45f);
            Assert.InRange(h.Committed.EnergyStores[0].Energy.Value - 30f * 179f / 120f, -.005f, .005f);
            await h.Advance(1);
        }
        Assert.Equal(45f, h.Committed.EnergyStores[0].Energy.Value);
        Assert.InRange(h.Committed.EnergyStores[0].Supplied.Value - 45f, -.005f, .005f);
        await h.Advance(60);
        Assert.Equal(45f, h.Committed.EnergyStores[0].Energy.Value);
        Assert.Equal(0f, h.Committed.EnergyStores[0].Released.Value);
        Assert.DoesNotContain(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        await h.Reset();
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
    }

    [Fact]
    public async Task TwoPhysicalOccupantsPresentUnavailableAndResetToEmpty()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var cannon = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("cannon"), new(1), 0f, 3f, 0f,
            rotation.X, rotation.Y, rotation.Z, rotation.W);
        var first = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(2), 0f, 3f, 0f, 0f, 0f, 0f, 1f);
        var second = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(3), 0f, 3.75f, 0f, 0f, 0f, 0f, 1f);
        await using var h = await WorkshopIntegrationHarness.Running(
            new(new(2), WorkshopCadenceSettings.Default(), new(cannon, first, second)));
        await h.Advance(1);
        Assert.Equal(ChamberPhase.Ambiguous, h.Committed.ChamberRegions[0].Phase);
        Assert.Equal(2u, h.Committed.ChamberRegions[0].Occupancy);
        AssertUnavailableCue(h);
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        await h.Reset();
        Assert.Equal(ChamberPhase.Empty, h.Committed.ChamberRegions[0].Phase);
        AssertUnavailableCue(h);
    }

    private static void AssertUnavailableCue(WorkshopIntegrationHarness h)
    {
        var cue = h.Presentation!.Advance(AnimationTestTime.Seconds(h.Committed.Tick.Value / 120d))
            .Where(sample => sample.Owner == new GpuBodyId(1)).OrderBy(sample => sample.Binding)
            .Select(sample => BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits)).ToArray();
        Assert.Equal(-70f * MathF.PI / 180f, cue[1]);
        Assert.Equal(new[] { 85f / 255f, 101f / 255f, 115f / 255f }, cue[2..5]);
    }


    [Theory]
    [InlineData(5f, FiniteWorkRequestResult.Uncharged)]
    [InlineData(10f, FiniteWorkRequestResult.Empty)]
    public async Task AnExpiredAttemptDoesNotWaitForChargeOrArrival(float height, FiniteWorkRequestResult refusal)
    {
        await using var h = await WorkshopIntegrationHarness.Running(Vertical(initialHeight: height, clockInterval: 1f));
        await h.Advance(130);
        Assert.Equal(refusal, h.Committed.FiniteWorkActuators[0].LastResult);
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        await h.Advance(80); // ready before the next authored two-second clock request
        Assert.Equal(ChamberPhase.Seated, h.Committed.ChamberRegions[0].Phase);
        Assert.Equal(45f, h.Committed.EnergyStores[0].Energy.Value);
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.DoesNotContain(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        for (var tick = 0; tick < 90 && h.Committed.FiniteWorkActuators[0].OccurrenceCount == 0; tick++)
            await h.Advance(1);
        Assert.Equal(FiniteWorkRequestResult.Fired, h.Committed.FiniteWorkActuators[0].LastResult);
        Assert.Equal(1u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(2UL, Assert.Single(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased).Other);
        await h.Reset();
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
    }


    [Fact]
    public async Task HiddenReleaseRetainsOneVisibleRecoilFrameAndResetCancelsIt()
    {
        var construction = Vertical();
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var sleeve = WorkshopDeclaredPresentation.Targets(CompiledSceneFixture.Compile(construction))
            .Single(target => target.Owner == new GpuBodyId(1) && target.Binding == 5).Target;
        h.Presentation!.SetVisible(sleeve, AnimationProperty.LocalTranslation, false);
        await h.Advance(324); // after the two-second shot and its authored0.53s clip, before another request
        Assert.Single(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        Assert.DoesNotContain(h.Presentation.Advance(AnimationTestTime.Seconds(2.7)), sample => sample.Owner == new GpuBodyId(1));
        h.Presentation.SetVisible(sleeve, AnimationProperty.LocalTranslation, true);
        float Sleeve(double seconds) => BitConverter.UInt32BitsToSingle(h.Presentation.Advance(AnimationTestTime.Seconds(seconds))
            .Single(sample => sample.Owner == new GpuBodyId(1) && sample.Binding == 5).Value.CanonicalBits);
        Assert.InRange(Sleeve(2.7) - (-.22f - .16f * MathF.Tanh(1f)), -.005f, .005f);
        Assert.Equal(-.22f, Sleeve(2.71));
        Assert.Equal(-.22f, Sleeve(3));
        var count = h.Committed.FiniteWorkActuators[0].OccurrenceCount;
        await h.Reset();
        Assert.Equal(1u, count);
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(-.22f, BitConverter.UInt32BitsToSingle(h.Presentation!.Advance(AnimationTestTime.Seconds(0))
            .Single(sample => sample.Owner == new GpuBodyId(1) && sample.Binding == 5).Value.CanonicalBits));
    }


    [Fact]
    public async Task PauseRetainsTheClockRequestUntilStepAndResumeDoesNotReplayIt()
    {
        await using var h = await WorkshopIntegrationHarness.Running(Vertical());
        await h.Advance(240); // authored two-second clock boundary at the declared120Hz commit cadence
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(ChamberPhase.Seated, h.Committed.ChamberRegions[0].Phase);
        Assert.Equal(45f, h.Committed.EnergyStores[0].Energy.Value);
        await h.Require(WorkshopCommandKind.Pause);
        var pausedTick = h.Committed.Tick;
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        await h.Require(WorkshopCommandKind.Step);
        Assert.Equal(pausedTick.Value + 1, h.Committed.Tick.Value);
        Assert.Equal(1u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        float Sleeve(PhysicalTime master) => BitConverter.UInt32BitsToSingle(h.Presentation!.Advance(h.Schedule, master)
            .Single(sample => sample.Owner == new GpuBodyId(1) && sample.Binding == 5).Value.CanonicalBits);
        var first = Sleeve(AnimationTestTime.Seconds(10));
        Assert.True(first < -.22f, "The accepted step must show its actual recoil.");
        Assert.Equal(first, Sleeve(AnimationTestTime.Seconds(20))); // stopped world time, independently advancing master
        await h.Require(WorkshopCommandKind.Resume);
        await h.Advance(1);
        Assert.Equal(1u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Single(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        await h.Reset();
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
    }

    internal static WorkshopConstruction Vertical(bool powered = true, float initialHeight = 5f, float clockInterval = 2f)
    {
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var cannon = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("cannon"), new(1), 0, 3, 0, q.X, q.Y, q.Z, q.W);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(2), 0, initialHeight, 0, 0, 0, 0, 1);
        var clock = PlacedParts.Clock(new(3), -4, 3, 0, 0, 0, 0, 1, clockInterval);
        var battery = PlacedParts.Battery(new(4), -7, 3, 0);
        var wires = new List<WorkshopConnection>
        {
            new(new(4), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(3), PortId.ActivationOut, new(1), PortId.ActivationIn, WorkshopConnectionDomain.Activation)
        };
        if (powered) wires.Add(new(new(4), PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        return new(new(2), WorkshopCadenceSettings.Default(), new(cannon, ball, clock, battery), Connections: new(wires.ToArray()));
    }





    [Fact]
    public async Task OpeningTheMuzzleGateDoesNotReplayAnExpiredRequest()
    {
        var vertical = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        IWorkshopInstance Part(string kind, ulong id, Vector3 position, Quaternion rotation) =>
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new(kind), new(id), position.X, position.Y, position.Z,
                rotation.X, rotation.Y, rotation.Z, rotation.W);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(Part("cannon", 1, new(0f, 3f, 0f), vertical),
                Part("ball", 2, new(0f, 3f, 0f), Quaternion.Identity),
                PlacedParts.Clock(new(3), -4f, 3f, 0f, 0f, 0f, 0f, 1f, 2f),
                PlacedParts.Battery(new(4), -7f, 3f, 0f),
                Part("delay", 5, new(-5f, 6f, -2f), Quaternion.Identity),
                Part("latch", 6, new(3f, 5f, 2f), Quaternion.Identity),
                Part("powered_gate", 7, new(0f, 3.8f, 0f), vertical)), Connections: new(
                    new(new(4), PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                    new(new(4), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                    new(new(4), PortId.Supply, new(6), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                    new(new(3), PortId.ActivationOut, new(1), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
                    new(new(3), PortId.ActivationOut, new(5), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
                    new(new(5), PortId.ActivationOut, new(6), PortId.SetIn, WorkshopConnectionDomain.Activation),
                    new(new(6), PortId.Supply, new(7), PortId.PowerIn, WorkshopConnectionDomain.Electrical)));
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var blade = construction.Instances.Identities[new(7)].Sliders[0].Body;
        Assert.True(h.Committed.Bodies.TryGet(blade, out var closed));
        await h.Advance(300); // after the authored two-second attempt, before the one-second delay opens the gate
        Assert.Equal(FiniteWorkRequestResult.Obstructed, h.Committed.FiniteWorkActuators[0].LastResult);
        AssertUnavailableCue(h);
        Assert.Equal(45f, h.Committed.EnergyStores[0].Energy.Value);
        Assert.Equal(0f, h.Committed.EnergyStores[0].Released.Value);
        Assert.DoesNotContain(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        await h.Advance(170); // after opening, still before the clock's next four-second request
        Assert.True(h.Committed.Bodies.TryGet(blade, out var open));
        var opening = MathF.Abs((open.Body.Cell.X + open.Body.Local.X - closed.Body.Cell.X - closed.Body.Local.X) / 16f);
        Assert.True(opening > 1.1f, "The blade must actually clear its0.72m half-width plus the ball's0.35m extent.");
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(45f, h.Committed.EnergyStores[0].Energy.Value);
        Assert.DoesNotContain(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        for (var tick = 0; tick < 130 && h.Committed.FiniteWorkActuators[0].OccurrenceCount == 0; tick++)
            await h.Advance(1);
        var shot = Assert.Single(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        Assert.Equal(2UL, shot.Other);
        Assert.InRange(shot.Magnitude - 45f, -.005f, .005f);
        Assert.Equal(FiniteWorkRequestResult.Fired, h.Committed.FiniteWorkActuators[0].LastResult);
        await h.Reset();
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
    }

    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(0f, 90f, 0f)]
    [InlineData(0f, 0f, 90f)]
    [InlineData(0f, 30f, 75f)]
    [InlineData(30f, 45f, 60f)]
    public async Task SuppliedAuthoredMountLaunchesOnItsClockRequest(float xDegrees, float yDegrees, float zDegrees)
    {
        var radians = MathF.PI / 180f;
        var rotation = Quaternion.CreateFromYawPitchRoll(yDegrees * radians, xDegrees * radians, zDegrees * radians);
        var cannon = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("cannon"), new(1), 0f, 3f, 0f,
            rotation.X, rotation.Y, rotation.Z, rotation.W,
            new([new(new("capacity"), 1f), new(new("charge_power"), 100f)]));
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(2), 0f, 3f, 0f, 0f, 0f, 0f, 1f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(cannon, ball, PlacedParts.Clock(new(3), -4f, 3f, 0f, 0f, 0f, 0f, 1f, .1f),
                PlacedParts.Battery(new(4), -7f, 3f, 0f)), Connections: new(
                new(new(4), PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(new(4), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(new(3), PortId.ActivationOut, new(1), PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var charged = false;
        var seated = false;
        WorkshopRead before = h.Committed;
        for (var tick = 0; tick < 60 && h.Committed.FiniteWorkActuators[0].OccurrenceCount == 0; tick++)
        {
            before = h.Committed;
            charged |= before.EnergyStores[0].Energy.Value == 1f;
            seated |= before.ChamberRegions[0].Phase == ChamberPhase.Seated;
            await h.Advance(1);
        }
        Assert.True(charged);
        Assert.True(seated);
        var shot = Assert.Single(h.Records, record => record.Kind == SimulationEventKind.FiniteWorkReleased);
        Assert.Equal(2UL, shot.Other);
        Assert.InRange(shot.Magnitude - 1f, -.005f, .005f);
        Assert.True(before.Bodies.TryGet(new(2), out var prior));
        Assert.True(h.Committed.Bodies.TryGet(new(2), out var after));
        var change = new Vector3(after.Body.Velocity.X - prior.Body.Velocity.X,
            after.Body.Velocity.Y - prior.Body.Velocity.Y, after.Body.Velocity.Z - prior.Body.Velocity.Z);
        var axis = Vector3.Transform(Vector3.UnitX, rotation);
        Assert.True(Vector3.Dot(change, axis) > .5f, "The paid shot must produce visible forward motion along the authored barrel.");
        // A completed interval includes gravity (less than0.09m/s); a0.1m/s transverse
        // allowance excludes a visibly wrong aim without pinning the incidental trigger substep.
        Assert.InRange((change - axis * Vector3.Dot(change, axis)).Length(), 0f, .1f);
        await h.Reset();
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
    }

    [Theory]
    [InlineData(0f, 0f, 0f, false)]
    [InlineData(0f, 90f, 0f, false)]
    [InlineData(0f, 30f, 75f, false)]
    [InlineData(30f, 45f, 60f, false)]
    [InlineData(0f, 0f, 0f, true)]
    [InlineData(0f, 30f, 75f, true)]
    [InlineData(30f, 45f, 60f, true)]
    public async Task AuthoredMountAimsAlongItsBarrelAndTenMillimetreMuzzleWallRefuses(
        float xDegrees, float yDegrees, float zDegrees, bool blocked)
    {
        // The production package is compiled at each named mount. Only initial stored energy
        // and the optional obstacle are authored by this fixture before admission.
        var radians = MathF.PI / 180f;
        var rotation = Quaternion.CreateFromYawPitchRoll(yDegrees * radians, xDegrees * radians, zDegrees * radians);
        IWorkshopInstance Part(string kind, ulong id) => WorkshopInput.Generic(RuntimeTestServices.Bundle,
            new(kind), new(id), 0f, 3f, 0f, rotation.X, rotation.Y, rotation.Z, rotation.W);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(Part("cannon", 1), Part("ball", 2)));
        var compiled = WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle,
            construction, new(101, 206), new(1));
        var source = compiled.Physics;
        var bodies = source.Bodies.ToArray().ToList();
        var colliders = source.Colliders.ToArray().ToList();
        var next = source.NextIdentity;
        if (blocked)
        {
            var wall = new GpuBodyId(next++);
            bodies.Add(bodies.Single(body => body.Id == new GpuBodyId(1)) with { Id = wall, Owner = wall });
            colliders.Add(new(new(next++), wall, colliders.First(value => value.Body == new GpuBodyId(1)).Material,
                CuriousContraptions.Gpu.ColliderShapeKind.Box, new(new(.7f, 0f, 0f), CanonicalRotation.Identity),
                new(0f), new(.005f, 1f, 1f)));
        }
        var physics = RuntimeTestServices.Core.CreatePhysicsScene(source.Document, next,
            bodies.ToArray(), colliders.ToArray(), source.Materials, source.Sensors, source.Guides,
            source.Triggers, source.ContactWorks, source.OrientationSensors, source.Electrical.Sources,
            source.Electrical.Bindings, source.Prismatics, source.SupplyGraph.Nodes, source.SupplyGraph.Edges,
            source.ContactLoadSensors, source.PrismaticDrives, source.TensileSpans, source.Revolutes,
            source.CompliantSurfaces, source.RevoluteDrives, source.RopeGuides, source.Transmissions,
            source.DrivenSurfaces, source.EnergyStores.ToArray().Select(store => store with { InitialEnergy = store.Capacity }).ToArray(),
            source.ChamberRegions, source.FiniteWorkActuators);
        var scene = RuntimeTestServices.Core.CreateScene(compiled.Identity, construction.Revision, construction.Settings,
            physics, compiled.Activation, compiled.Presentation, bodies.Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(
            new CuriousContraptions.Simulation.BrowserGpuTransport(new EventRingPublication()), physics.Document);
        var profile = new WorkshopGpuProfile(construction.Settings.Simulation, construction.Settings.Physical, new(1));
        var admitted = await device.Admit(scene, new(1), profile, new(1));
        device.Commit(new(1)); device.DrainEvents();
        var actuator = Assert.Single(physics.FiniteWorkActuators.ToArray()).Id;
        var result = await device.ApplyFiniteWork(admitted.Read, new(2), new(new(1), actuator, new(2)));
        device.Commit(new(2));
        var work = result.Read.FiniteWorkActuators[0];
        Assert.Equal(blocked ? FiniteWorkRequestResult.Obstructed : FiniteWorkRequestResult.Fired, work.LastResult);
        Assert.Equal(blocked ? 0u : 1u, work.OccurrenceCount);
        Assert.True(result.Read.Bodies.TryGet(new(2), out var payload));
        var velocity = new Vector3(payload.Body.Velocity.X, payload.Body.Velocity.Y, payload.Body.Velocity.Z);
        if (blocked)
        {
            Assert.Equal(Vector3.Zero, velocity);
            Assert.Equal(45f, result.Read.EnergyStores[0].Energy.Value);
            Assert.Equal(0f, result.Read.EnergyStores[0].Released.Value);
            Assert.Equal(0UL, device.Events.WriteSequence);
        }
        else
        {
            // One centimetre of aim error over a second excludes a wrong launch axis.
            var expected = Vector3.Transform(Vector3.UnitX, rotation) * MathF.Sqrt(90f);
            Assert.InRange((velocity - expected).Length(), 0f, .01f);
            Assert.InRange(result.Read.EnergyStores[0].Released.Value - 45f, -.005f, .005f);
            Assert.Equal(1UL, device.Events.WriteSequence);
        }
        Assert.Equal(admitted.Read.Tick, result.Read.Tick);
        Assert.Equal(new PhysicalCommitRevision(1), result.Read.PhysicalRevision);
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(.05f, true)]
    [InlineData(0f, false)]
    public async Task ClockDelayLatchAndGateFeedADistinctSecondBall(float feederX, bool suppliedGate)
    {
        IWorkshopInstance Part(string kind, uint id, float x, float y, float z, float degrees = 0f)
        {
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, degrees * MathF.PI / 180f);
            return WorkshopInput.Generic(RuntimeTestServices.Bundle, new(kind), new(id), x, y, z,
                rotation.X, rotation.Y, rotation.Z, rotation.W);
        }
        var parts = new WorkshopInstances(Part("cannon", 1, 0, 3, 0, 75), Part("ball", 2, 0, 3, 0),
            Part("ball", 3, feederX, 8.8f, 0), Part("battery", 4, -4, 3, 2), Part("clock", 5, -3, 5, -2),
            Part("delay", 6, -5, 6, -2), Part("latch", 7, 3, 5, 2), Part("powered_gate", 8, feederX, 8, 0, 90));
        var wires = new List<WorkshopConnection>
        {
            new(new(4), PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(4), PortId.Supply, new(5), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(4), PortId.Supply, new(7), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(5), PortId.ActivationOut, new(1), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
            new(new(5), PortId.ActivationOut, new(6), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
            new(new(6), PortId.ActivationOut, new(7), PortId.SetIn, WorkshopConnectionDomain.Activation)
        };
        if (suppliedGate) wires.Add(new(new(7), PortId.Supply, new(8), PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), parts, Connections: new(wires.ToArray()));
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(1440); // twelve-second stall guard; authored clock/delay periods remain unchanged
        var shots = h.Records.Where(value => value.Kind == SimulationEventKind.FiniteWorkReleased).ToArray();
        Assert.Equal(suppliedGate ? 2 : 1, shots.Length);
        Assert.Equal(2UL, shots[0].Other);
        if (suppliedGate)
        {
            Assert.Equal(3UL, shots[1].Other);
            Assert.True(shots[0].Ordinal < shots[1].Ordinal);
        }
        else
        {
            Assert.True(h.Committed.Bodies.TryGet(new(3), out var held));
            var bladeId = construction.Instances.Identities[new(8)].Sliders[0].Body;
            Assert.True(h.Committed.Bodies.TryGet(bladeId, out var blade));
            var collider = Assert.Single(h.Simulation.Scene.Physics.Colliders.ToArray(), value => value.Body == bladeId);
            var rotation = new Quaternion(blade.Rotation.X, blade.Rotation.Y, blade.Rotation.Z, blade.Rotation.W);
            var localRotation = new Quaternion(collider.Pose.Rotation.X, collider.Pose.Rotation.Y, collider.Pose.Rotation.Z, collider.Pose.Rotation.W);
            var shapeRotation = Quaternion.Normalize(rotation * localRotation);
            Vector3 Position(CanonicalBody body) => new((body.Cell.X + body.Local.X) / 16f,
                (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f);
            var centre = Position(blade.Body) + Vector3.Transform(new Vector3(collider.Pose.Translation.X,
                collider.Pose.Translation.Y, collider.Pose.Translation.Z), rotation);
            var relative = Vector3.Transform(Position(held.Body) - centre, Quaternion.Conjugate(shapeRotation));
            // Relative to the actual closed blade: sphere stays over its footprint, with no visible centimetre-sized support gap.
            var normal = Vector3.Transform(Vector3.UnitX, shapeRotation);
            var ballRotation = new Quaternion(held.Rotation.X, held.Rotation.Y, held.Rotation.Z, held.Rotation.W);
            var supportGap = float.PositiveInfinity;
            foreach (var child in h.Simulation.Scene.Physics.Colliders)
            {
                if (child.Body != new GpuBodyId(3) || child.Participation == CollisionParticipation.Disabled) continue;
                var childRotation = Quaternion.Normalize(ballRotation * new Quaternion(child.Pose.Rotation.X,
                    child.Pose.Rotation.Y, child.Pose.Rotation.Z, child.Pose.Rotation.W));
                var childCentre = Position(held.Body) + Vector3.Transform(new Vector3(child.Pose.Translation.X,
                    child.Pose.Translation.Y, child.Pose.Translation.Z), ballRotation);
                var shape = new HollowPartner(child.Shape, childCentre, Vector3.Transform(Vector3.UnitX, childRotation),
                    Vector3.Transform(Vector3.UnitY, childRotation), Vector3.Transform(Vector3.UnitZ, childRotation),
                    child.Radius.Value, child.HalfExtents.X, child.HalfExtents.Y, child.HalfExtents.Z);
                var bottom = RuntimeTestServices.Core.Geometry.MaterialSupport(shape, -normal);
                supportGap = MathF.Min(supportGap, Vector3.Dot(bottom - centre, normal) - collider.HalfExtents.X);
            }
            Assert.InRange(supportGap, -.01f, .01f);
            Assert.InRange(MathF.Abs(relative.Y), 0f, collider.HalfExtents.Y);
            Assert.InRange(MathF.Abs(relative.Z), 0f, collider.HalfExtents.Z);
        }
        Assert.Equal(8, h.Simulation.Scene.Owners.Length);
        Assert.InRange(h.Committed.EnergyStores[0].Released.Value - (suppliedGate ? 90f : 45f), -.01f, .01f);
        await h.Reset();
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
    }

    [Fact]
    public async Task TwentyRunResetsAndFiveSaveLoadsRestoreOnlyAuthoredConstruction()
    {
        var construction = Vertical();
        for (var load = 0; load < 5; load++)
        {
            var saved = new WorkshopSavedConstruction(construction, new(5));
            var decoded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
                RuntimeTestServices.Bundle, SaveFixture.Elements,
                WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
            Assert.Equal(saved, decoded); // decoded typed construction, never serialized buffer identity
            construction = decoded.Construction;
            await using var h = await WorkshopIntegrationHarness.Building(construction);
            for (var reset = 0; reset < 4; reset++)
            {
                await h.Run(); await h.Advance(250);
                Assert.Equal(1u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
                Assert.InRange(h.Committed.EnergyStores[0].Released.Value - 45f, -.005f, .005f);
                await h.Reset();
                Assert.Equal(new SimulationTick(0), h.Committed.Tick);
                Assert.Equal(new PhysicalCommitRevision(0), h.Committed.PhysicalRevision);
                Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
                Assert.Null(h.Committed.FiniteWorkActuators[0].LastResult);
                Assert.Equal(default, h.Committed.FiniteWorkActuators[0].LastPayload);
                Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
                Assert.Equal(0f, h.Committed.EnergyStores[0].Released.Value);
                Assert.Equal(ChamberPhase.Empty, h.Committed.ChamberRegions[0].Phase);
                Assert.True(h.Committed.Bodies.TryGet(new(2), out var body));
                Assert.Equal(5f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f);
                Assert.Equal(Vector3.Zero, new Vector3(body.Body.Velocity.X, body.Body.Velocity.Y, body.Body.Velocity.Z));
            }
        }
    }

    [Fact]
    public async Task GravityReturnsTheSameBallForAnotherPaidShot()
    {
        await using var h = await WorkshopIntegrationHarness.Building(Vertical());
        await h.Run();
        var departed = false;
        var returned = false;
        for (var tick = 0; tick < 1440 && h.Committed.FiniteWorkActuators[0].OccurrenceCount < 2; tick++)
        {
            await h.Advance(1);
            var work = h.Committed.FiniteWorkActuators[0];
            if (work.OccurrenceCount == 1)
            {
                departed |= h.Committed.ChamberRegions[0].Phase == ChamberPhase.Empty;
                returned |= departed && h.Committed.ChamberRegions[0].Phase == ChamberPhase.Seated;
            }
        }
        Assert.True(departed, "The first launched ball must leave the chamber.");
        Assert.True(returned, "Gravity must return that ball to a seated chamber before another shot.");
        Assert.True(h.Committed.FiniteWorkActuators[0].OccurrenceCount == 2,
            $"Reload guard: actuator={h.Committed.FiniteWorkActuators[0]}, chamber={h.Committed.ChamberRegions[0]}, store={h.Committed.EnergyStores[0]}");
        Assert.Equal(1, h.Committed.Bodies.Count);
        Assert.InRange(h.Committed.EnergyStores[0].Released.Value - 90f, -.01f, .01f);
        var events = h.Records.Where(value => value.Kind == SimulationEventKind.FiniteWorkReleased).ToArray();
        Assert.Equal(2, events.Length);
        Assert.All(events, value => Assert.Equal(2UL, value.Other));
        Assert.True(events[0].Sequence < events[1].Sequence && events[0].Ordinal < events[1].Ordinal);
        await h.Reset();
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply(bool powered)
    {
        var construction = Vertical(powered);
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var ids = construction.Instances.Identities[new(1)];
        var actuator = ids.FiniteWorkActuators[new(1)];
        var store = ids.EnergyStores[new(1)];
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
        await h.Run();
        var seated = false;
        var ready = false;
        var partialCharge = false;
        var loading = false;
        WorkshopRead? shot = null;
        for (var tick = 0; tick < 360; tick++) // three seconds: generous guard after the authored two-second trigger
        {
            await h.Advance(1);
            var chamber = h.Committed.ChamberRegions[0];
            var charge = h.Committed.EnergyStores[0].Energy.Value;
            var cue = h.Presentation!.Advance(AnimationTestTime.Seconds(h.Committed.Tick.Value / 120d))
                .Where(sample => sample.Owner == new GpuBodyId(1)).OrderBy(sample => sample.Binding)
                .Select(sample => BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits)).ToArray();
            Assert.Equal(6, cue.Length);
            Assert.Equal(MathF.Max(.001f, charge / 45f), cue[0]); // defined package mapping, not an interpolated range
            if (chamber.Phase == ChamberPhase.Departing)
            {
                Assert.Equal(-70f * MathF.PI / 180f, cue[1]);
                Assert.Equal(new[] { 85f / 255f, 101f / 255f, 115f / 255f }, cue[2..5]);
            }
            if (chamber.Phase == ChamberPhase.Loading)
            {
                loading = true;
                Assert.Equal(-70f * MathF.PI / 180f, cue[1]);
                Assert.Equal(new[] { 85f / 255f, 101f / 255f, 115f / 255f }, cue[2..5]);
            }
            if (chamber.Phase == ChamberPhase.Seated && charge > 0f && charge < 45f)
            {
                partialCharge = true;
                Assert.Equal(-70f * MathF.PI / 180f, cue[1]);
                Assert.Equal(new[] { 102f / 255f, 184f / 255f, 201f / 255f }, cue[2..5]);
            }
            if (chamber.Phase == ChamberPhase.Seated && charge == 45f)
            {
                Assert.Equal(0f, cue[1]);
                Assert.Equal(new[] { 247f / 255f, 203f / 255f, 82f / 255f }, cue[2..5]);
            }
            seated |= chamber.Phase == ChamberPhase.Seated;
            ready |= chamber.Phase == ChamberPhase.Seated && h.Committed.EnergyStores[0].Energy.Value == 45f;
            if (h.Committed.FiniteWorkActuators[0].OccurrenceCount > 0) { shot = h.Committed; break; }
        }
        Assert.True(loading, "A physically arriving ball must first expose Loading.");
        Assert.True(seated, "The real falling ball must settle in the physical barrel before release.");
        if (powered)
        {
            Assert.True(ready);
            Assert.True(partialCharge);
            Assert.True(shot.HasValue, "A charged seated ball needs one accepted launch after the clock trigger.");
            var released = Assert.IsType<WorkshopRead>(shot);
            var work = released.FiniteWorkActuators[0];
            Assert.Equal(actuator, work.Id); Assert.Equal(store, work.Store);
            Assert.Equal(FiniteWorkRequestResult.Fired, work.LastResult);
            Assert.Equal(new GpuBodyId(2), work.LastPayload);
            Assert.Equal(1u, work.OccurrenceCount);
            Assert.InRange(work.LastDebit.Value - 45f, -.005f, .005f);
            Assert.True(released.Bodies.TryGet(new(2), out var ball));
            // The default store can lift its 1 kg ball several metres; the first completed
            // interval includes ordinary gravity and drag, so it is not an exact speed golden.
            Assert.InRange(ball.Body.Velocity.Y, 8f, 10f);
            var occurrence = Assert.Single(h.Records, value => value.Kind == SimulationEventKind.FiniteWorkReleased);
            Assert.Equal(actuator.Value, occurrence.Source.Value);
            Assert.Equal(2UL, occurrence.Other);
            Assert.InRange(occurrence.Magnitude - work.LastDebit.Value, -.005f, .005f);
            var eventSeconds = (occurrence.Ordinal + occurrence.Phase / (double)PhysicalTime.UnitsPerOrdinal) / PhysicalTime.Frequency;
            float Sleeve(double seconds) => BitConverter.UInt32BitsToSingle(h.Presentation!.Advance(AnimationTestTime.Seconds(seconds))
                .Single(sample => sample.Owner == new GpuBodyId(1) && sample.Binding == 5).Value.CanonicalBits);
            // A 5 mm cosmetic allowance distinguishes the visible 16 cm sleeve stroke from a missing cue.
            Assert.InRange(Sleeve(eventSeconds + .08) - (-.22f - .16f * MathF.Tanh(1f)), -.005f, .005f);
            Assert.Equal(-.22f, Sleeve(eventSeconds + .7));
            Assert.Equal(released.PhysicalRevision, h.Committed.PhysicalRevision);
            Assert.True(h.Committed.Bodies.TryGet(new(2), out var unchanged));
            Assert.Equal(ball.Body, unchanged.Body); // cosmetic evaluation cannot mutate the installed physical read
        }
        else
        {
            Assert.False(ready); Assert.False(partialCharge); Assert.Null(shot);
            Assert.Equal(-.22f, BitConverter.UInt32BitsToSingle(h.Presentation!.Advance(AnimationTestTime.Seconds(3))
                .Single(sample => sample.Owner == new GpuBodyId(1) && sample.Binding == 5).Value.CanonicalBits));
            Assert.Equal(FiniteWorkRequestResult.Uncharged, h.Committed.FiniteWorkActuators[0].LastResult);
            Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
            Assert.DoesNotContain(h.Records, value => value.Kind == SimulationEventKind.FiniteWorkReleased);
        }
        Assert.Equal(1, h.Committed.Bodies.Count); // the existing payload remains the sole dynamic body
        await h.Reset();
        Assert.Equal(0f, h.Committed.EnergyStores[0].Energy.Value);
        Assert.Equal(0u, h.Committed.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Null(h.Committed.FiniteWorkActuators[0].LastResult);
        Assert.Equal(ChamberPhase.Empty, h.Committed.ChamberRegions[0].Phase);
        Assert.True(h.Committed.Bodies.TryGet(new(2), out var restored));
        Assert.Equal(Vector3.Zero, new Vector3(restored.Body.Velocity.X, restored.Body.Velocity.Y, restored.Body.Velocity.Z));
        Assert.Equal(5f, (restored.Body.Cell.Y + restored.Body.Local.Y) / 16f);
    }
}
