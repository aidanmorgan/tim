using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed partial class AirflowRuntimeTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MovingSolidOcclusionIsResampledBeforeTransfer(bool moving)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(2, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var blocker = scene.Box(new(1, 5, 0), new(.1f), 1, LawMaterial.Domino,
            velocity: moving ? Vector3.UnitZ : Vector3.Zero, gravity: 0);
        var store = scene.EnergyStore(frame, 10, 10);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        scene.AirflowTarget(cargo);
        var observedClearTransfer = false;
        scene.Admit().Step(60, (_, state) =>
        {
            var work = Assert.Single(PhysicsGpuAbi.ReadAirflow(state.World)).Work.Value;
            if (state[blocker].Position.Z < .09f) Assert.Equal(0f, work);
            if (state[blocker].Position.Z > .2f && work > 0) observedClearTransfer = true;
            if (!moving) Assert.Equal(0f, state[cargo].Velocity.X);
        });
        Assert.Equal(moving, observedClearTransfer);
    }

    [Theory]
    [InlineData(12f)]
    [InlineData(13f)]
    public void ReceiverAtOrAboveFlowSpeedCannotConsumeSourceWork(float speed)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, new(speed, 0, 0), default, 0, 0);
        var store = scene.EnergyStore(frame, 10, 10);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        scene.AirflowTarget(cargo);
        var result = scene.Admit().Step(1);
        Assert.Equal(speed, result[cargo].Velocity.X);
        Assert.Equal(0f, Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value);
        Assert.Equal(10f, PhysicsGpuAbi.ReadEnergyStores(result.World)[0].Energy.Value);
    }

    [Fact]
    public void RejectedBoundedContactTrialsCannotAccumulateIndependentWindingWork()
    {
        static LawState Run(bool transfer)
        {
            var scene = new LawScene(false);
            var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
            var head = scene.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: 0);
            var shaftBody = scene.Box(new(2, 5, 0), new(.1f), .25f, LawMaterial.Domino, gravity: 0);
            var slider = scene.Prismatic(frame, head, ConstraintFrame.Identity, ConstraintFrame.Identity,
                -.8f, 0f, 160f, 0f, ConnectedCollision.Disabled);
            var shaft = scene.Revolute(frame, shaftBody, new(new(2, 0, 0), Quaternion.Identity),
                ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0, travel: RevoluteTravel.Continuous);
            scene.RevoluteDrive(shaft, -2, 20);
            scene.Retain(frame, shaft, slider, .1f);
            var nozzle = scene.StaticBox(new(0, 10, 0), new(.1f));
            var cargo = scene.Sphere(new(1, 10, 0), .1f, .01f, LawMaterial.Domino, default, default, 0, 0);
            scene.StaticBox(new(1.4f, 10, 0), new(.1f));
            var store = scene.EnergyStore(nozzle, 10, 10);
            scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true), transfer ? 40 : 0);
            scene.AirflowTarget(cargo);
            return scene.Admit().Step(30, supplied: true);
        }
        var control = Run(false);
        var loaded = Run(true);
        var ordinary = Assert.Single(PhysicsGpuAbi.ReadRetentions(control.World));
        var replayed = Assert.Single(PhysicsGpuAbi.ReadRetentions(loaded.World));
        Assert.True(ordinary.Potential > .05f);
        // 40 N / .01 kg exceeds the global transfer bound, so the accepted search necessarily rejects
        // larger trial impulses. Independent winding must still accrue only one ordinary interval per substep.
        const float workAllowance = .005f; // Existing mechanical-work allowance: less than a 1 mm lift of the .5 kg head.
        foreach (var delta in new[] { replayed.Potential - ordinary.Potential,
            replayed.WindingWork - ordinary.WindingWork, replayed.PositiveWindingTransfer - ordinary.PositiveWindingTransfer,
            replayed.ContactWork - ordinary.ContactWork, replayed.AcceptedElasticEnergy - ordinary.AcceptedElasticEnergy,
            replayed.ReleasedElasticEnergy - ordinary.ReleasedElasticEnergy })
            Assert.InRange(delta, -workAllowance, workAllowance);
        Assert.InRange(PhysicsGpuAbi.ReadRevoluteState(loaded.World, 0).Work -
            PhysicsGpuAbi.ReadRevoluteState(control.World, 0).Work, -workAllowance, workAllowance);
        var flow = Assert.Single(PhysicsGpuAbi.ReadAirflow(loaded.World));
        Assert.True(flow.Work.Value > 0);
        Assert.InRange(10 - PhysicsGpuAbi.ReadEnergyStores(loaded.World)[0].Energy.Value - flow.Work.Value, -.0001f, .0001f);
        Assert.Equal(0f, Assert.Single(PhysicsGpuAbi.ReadAirflow(control.World)).Work.Value);
    }



    [Fact]
    public void SourceAndReceiverPopulationsGrowAndResetWithoutRetainingOldOwners()
    {
        var engine = new CuriousContraptions.Simulation.WorkshopPhysicsEngine();
        var generation = 0;
        int[] regrowth = [3, 0, 19, 2, 0];
        foreach (var count in Enumerable.Range(0, 18).Concat(regrowth))
        {
            var scene = new LawScene(false);
            // Different admitted identities on regrowth expose retained owner slots, without private-array introspection.
            for (var padding = 0; padding < generation; padding++)
                scene.StaticBox(new(-20, 2 + padding, 0), new(.1f));
            generation++;
            var owners = new List<GpuBodyId>();
            for (var i = 0; i < count; i++)
            {
                var frame = scene.StaticBox(new(0, 2 + i, 0), new(.1f));
                var cargo = scene.Sphere(new(1, 2 + i, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
                var store = scene.EnergyStore(frame, 1, 1);
                scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true), .1f, 4);
                scene.AirflowTarget(cargo);
                owners.Add(frame);
            }
            var world = scene.Admit();
            var result = world.Step(1, engine: engine);
            var flows = PhysicsGpuAbi.ReadAirflow(result.World);
            Assert.Equal(count, flows.Length);
            Assert.Equal(count, world.Scene.AirflowReceivers.Length);
            Assert.Equal(count, PhysicsGpuAbi.ReadEnergyStores(result.World).Length);
            Assert.Equal(owners.OrderBy(owner => owner.Value), flows.Select(flow => flow.Owner).OrderBy(owner => owner.Value));
            Assert.All(flows, flow => Assert.True(flow.Work.Value > 0));
        }
    }



    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand(bool blocked)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        if (blocked) scene.StaticBox(new(.5f, 5.1f, 0), new(.025f));
        var store = scene.EnergyStore(frame, 10, 10);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        scene.AirflowTarget(cargo, new(new(0, .1f, 0), .25f), new(new(0, -.1f, 0), .25f),
            new(new(0, 0, .1f), .25f), new(new(0, 0, -.1f), .25f));
        var result = scene.Admit().Step(1);
        var work = Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value;
        // Initial demands are 108 W and 81 W. Slip over 1/120 s lowers either by less than 1%;
        // this discriminates the missing quarter without pinning the incidental rotated-sample trajectory.
        var ideal = (blocked ? 81f : 108f) / 120;
        Assert.InRange(work, ideal * .99f, ideal);
        Assert.InRange(12 * result[cargo].Velocity.X - work, -.00001f, .00001f);
        Assert.Equal(blocked, MathF.Abs(result[cargo].Angular.Z) > .001f);
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OpposingJetsUseOneCoupledVelocity(bool opposed)
    {
        var scene = new LawScene(false);
        var a = scene.StaticBox(new(0, 5, 0), new(.1f));
        var b = scene.StaticBox(new(2, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var firstStore = scene.EnergyStore(a, 10, 10);
        var secondStore = scene.EnergyStore(b, 10, 10);
        scene.Airflow(a, new(ContinuousWorkInputKind.Store, firstStore, default, default, 0, true));
        scene.Airflow(b, new(ContinuousWorkInputKind.Store, secondStore, default, default, 0, true),
            force: opposed ? 9 : 0, axis: -Vector3.UnitX);
        scene.AirflowTarget(cargo);
        var result = scene.Admit().Step(1);
        if (opposed)
        {
            Assert.InRange(result[cargo].Velocity.Length(), 0, .001f); // Less than 1 mm drift over a one-second puzzle window.
            var flows = PhysicsGpuAbi.ReadAirflow(result.World);
            foreach (var flow in flows)
            {
                Assert.InRange(flow.Work.Value, .89f, .91f); // Rated 108 W over 1/120 s; 1% distinguishes missing payment.
                Assert.InRange(flow.JetLoss.Value - flow.Work.Value, -.001f, .001f); // A millijoule cannot create meaningful receiver travel.
            }
        }
        else Assert.InRange(result[cargo].Velocity.X, .074f, .076f);
    }

    [Fact]
    public void MovingNozzleReceivesPairedForceAndMoment()
    {
        var scene = new LawScene(false);
        var frame = scene.Box(new(0, 5, 0), new(.2f), 2, LawMaterial.Domino, gravity: 0);
        var cargo = scene.Sphere(new(1, 5.05f, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var store = scene.EnergyStore(frame, 10, 10);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        scene.AirflowTarget(cargo);
        var result = scene.Admit().Step(1);
        Assert.True(result[frame].Velocity.X < 0);
        Assert.True(result[cargo].Velocity.X > 0);
        Assert.True(result[frame].Angular.Z > 0);
        // About the initial nozzle origin: no external force or torque, with cube inertia 2m h²/3.
        var momentum = 2 * result[frame].Velocity + result[cargo].Velocity;
        var origin = new Vector3(0, 5, 0);
        var angular = (2f * 2f * .2f * .2f / 3f) * result[frame].Angular +
            Vector3.Cross(result[frame].Position - origin, 2 * result[frame].Velocity) +
            Vector3.Cross(result[cargo].Position - origin, result[cargo].Velocity);
        Assert.InRange(momentum.Length(), 0, .001f); // 1 mN s permits less than 1 mm/s common drift.
        Assert.InRange(angular.Length(), 0, .0001f); // This inertia converts 0.1 mN m s to under 2 mrad/s spurious rotation.
        var flow = Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World));
        Assert.True(flow.Work.Value > 0 && flow.JetLoss.Value > 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnlyExactInternalCollidersAreExemptFromOcclusion(bool sameOwner)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        if (sameOwner) scene.ChildCollider(frame, ColliderShapeKind.Box, new(.5f, 0, 0), 0, new(.1f));
        else scene.StaticBox(new(.5f, 5, 0), new(.1f));
        var store = scene.EnergyStore(frame, 10, 10);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        scene.AirflowTarget(cargo);
        var result = scene.Admit().Step(1);
        Assert.Equal(Vector3.Zero, result[cargo].Velocity);
        Assert.Equal(0f, Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value);
        Assert.Equal(10f, PhysicsGpuAbi.ReadEnergyStores(result.World)[0].Energy.Value);
    }

    [Fact]
    public void LightPayloadUsesGlobalTransferBoundWithoutThrottlingIndependentJet()
    {
        var scene = new LawScene(false);
        var a = scene.StaticBox(new(0, 3, 0), new(.1f));
        var b = scene.StaticBox(new(0, 7, 0), new(.1f));
        var light = scene.Sphere(new(1, 3, 0), .1f, .01f, LawMaterial.Domino, default, default, 0, 0);
        var heavy = scene.Sphere(new(1, 7, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var firstStore = scene.EnergyStore(a, 10, 10); var secondStore = scene.EnergyStore(b, 10, 10);
        scene.Airflow(a, new(ContinuousWorkInputKind.Store, firstStore, default, default, 0, true));
        scene.Airflow(b, new(ContinuousWorkInputKind.Store, secondStore, default, default, 0, true));
        scene.AirflowTarget(light); scene.AirflowTarget(heavy);
        var result = scene.Admit().Step(1);
        // 64 m/s² over 1/120 s; a historical 16 m/s² transfer cap would be visibly four times weaker.
        Assert.InRange(result[light].Velocity.X, .53f, .534f);
        Assert.InRange(result[heavy].Velocity.X, .074f, .076f);
        var flows = PhysicsGpuAbi.ReadAirflow(result.World);
        Assert.InRange(flows[0].Work.Value - 12 * .01f * result[light].Velocity.X, -.00001f, .00001f);
        Assert.InRange(flows[1].Work.Value, .89f, .91f);
    }
}
