using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class RotaryAirflowRuntimeTests
{
    [Theory]
    [InlineData(1f, 12f, 9f, .6666667f, 6f)]
    [InlineData(-1f, 12f, 9f, .6666667f, 6f)]
    [InlineData(1f, 12f, 9f, 4f, 12f)]
    [InlineData(1f, 12f, 9f, .1f, .9f)]
    [InlineData(-1f, 1f, .75f, 4f, 3f)]
    [InlineData(1f, 1f, .75f, 4f, 3f)]
    public void PaidFiniteRotorApproachesItsSignedUnloadedOperatingRegion(float sign, float flow,
        float force, float response, float expected)
    {
        var (world, rotor) = Rotor(sign, flow, force, response);
        // The low-flow case has time constant I/(c*p²)=.245/(.75/9)=2.94 s.
        // Five time constants allow its intended gradual acceleration; observe one more second.
        var settleSeconds = flow == 1 ? 15 : 10;
        var result = world.Step((settleSeconds + 1) * world.TicksPerSecond, (tick, state) =>
        {
            if (tick == 1) Assert.InRange(sign * state[rotor].Angular.X, .001f, expected * .5f);
            if (tick >= settleSeconds * world.TicksPerSecond)
                Assert.InRange(sign * state[rotor].Angular.X, expected - .1f, expected + .1f);
        });
        // A .1 rad/s region means .07 m/s at a .7 m tip; it detects sign/response/cap errors without pinning trajectory.
        Assert.InRange(sign * result[rotor].Angular.X, expected - .1f, expected + .1f);
        var read = Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World));
        Assert.True(read.Work.Value > 0);
        Assert.True(read.JetLoss.Value >= 0);
        var remaining = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(result.World)).Energy.Value;
        Assert.True(remaining < 10000);
        // This finite fixture cannot receive more than the declared source's elapsed work allowance.
        Assert.InRange(read.Work.Value, 0, force * flow * (settleSeconds + 1));
    }

    [Theory]
    [InlineData(.049f, false)]
    [InlineData(.05f, true)]
    public void InclusiveCutInControlsActualTransfer(float force, bool active)
    {
        // Identity joint frames give an exactly aligned axis: the threshold is aligned rest force,
        // not nominal source force multiplied by a rounded quarter-turn quaternion.
        var scene = new LawScene(false);
        var nozzle = scene.StaticBox(new(0, 5, 0), new(.05f));
        var support = scene.StaticBox(new(0, 5, 2), new(.05f));
        var rotor = scene.Sphere(new(0, 5, 2), .05f, 1, LawMaterial.Domino, default, default, 0, 0);
        scene.DeclareMassFrame(rotor, new(.245f));
        scene.Own(rotor, support);
        var joint = scene.Revolute(support, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity,
            -MathF.PI, MathF.PI, 0, travel: RevoluteTravel.Continuous);
        scene.RotaryCapture(support, support, joint, .6666667f,
            exclusions: [scene.ColliderOf(support), scene.ColliderOf(rotor)]);
        var store = scene.EnergyStore(nozzle, 10, 10);
        scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true), force, axis: Vector3.UnitZ);
        var result = scene.Admit().Step(360);
        Assert.Equal(active, result[rotor].Angular.Z > .01f);
        Assert.Equal(active, Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value > 0);
    }

    [Fact]
    public void DisabledSourceCannotStartTheRotorOrSpendItsStore()
    {
        var (world, rotor) = Rotor(1, 12, 9, .6666667f, enabled: false);
        var result = world.Step(1200);
        Assert.Equal(Vector3.Zero, result[rotor].Angular);
        Assert.Equal(0, Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value);
        Assert.Equal(10000, Assert.Single(PhysicsGpuAbi.ReadEnergyStores(result.World)).Energy.Value);
    }

    [Fact]
    public void PassiveRotorAcceptsBackdriveAboveTargetCapAndCoastsWithoutSourceWork()
    {
        var (world, rotor) = Rotor(1, 12, 9, .6666667f, enabled: false, spin: 20);
        var result = world.Step(1200, (tick, state) =>
        {
            if (tick == 1) Assert.True(state[rotor].Angular.X > 12, "The unloaded target cap must not clamp external backdrive.");
        });
        // Default passive I/b=.245/.6 seconds; ten seconds is a generous coast guard.
        Assert.InRange(MathF.Abs(result[rotor].Angular.X), 0, .01f);
        Assert.Equal(0, Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value);
    }

    [Theory]
    [InlineData(0, 6f)]
    [InlineData(1, 4.5f)]
    [InlineData(4, 0f)]
    public void FourActualPoseSamplesLoseOnlyOccludedWeight(int blocked, float expected)
    {
        var scene = new LawScene(false);
        var nozzle = scene.StaticBox(new(0, 5, 0), new(.05f));
        // At x=3 the cone radius is .6 m, enclosing all four .42 m samples.
        var support = scene.StaticBox(new(3, 5, 0), new(.05f));
        var rotor = AddRotor(scene, support, new(3, 5, 0));
        AirflowSample[] samples = [new(new(0, .42f, 0), .25f), new(new(0, -.42f, 0), .25f),
            new(new(0, 0, .42f), .25f), new(new(0, 0, -.42f), .25f)];
        scene.RotaryCapture(support, support, rotor.Joint, .6666667f, samples: samples,
            exclusions: [scene.ColliderOf(support), scene.ColliderOf(rotor.Body)]);
        if (blocked == 1) scene.StaticBox(new(1, 5.42f, 0), new(.05f, .1f, .1f));
        if (blocked == 4) scene.StaticBox(new(1, 5, 0), new(.05f, .6f, .6f));
        var store = scene.EnergyStore(nozzle, 10000, 10000);
        scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        var result = scene.Admit().Step(1320, (tick, state) =>
        {
            if (tick >= 1200) Assert.InRange(state[rotor.Body].Angular.X, expected - .1f, expected + .1f);
        });
        Assert.Equal(blocked != 4, Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedFiniteStorePaysRotaryAndLinearBranchesRegardlessOfDeclarationOrder(bool reverse)
    {
        var scene = new LawScene(false);
        var nozzle = scene.StaticBox(new(0, 5, 0), new(.05f));
        var support = scene.StaticBox(new(2, 5.3f, 0), new(.05f));
        var rotor = AddRotor(scene, support, new(2, 5.3f, 0));
        scene.RotaryCapture(support, support, rotor.Joint, 1,
            exclusions: [scene.ColliderOf(support), scene.ColliderOf(rotor.Body)]);
        var cargo = scene.Sphere(new(2, 4.7f, 0), .05f, 1, LawMaterial.Domino, default, default, 0, 0);
        scene.AirflowTarget(cargo);
        var store = scene.EnergyStore(nozzle, .2f, .2f);
        scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        if (reverse) scene.ReverseResourceDeclarations();
        var sawRotor = false;
        var result = scene.Admit().Step(240, (_, state) => sawRotor |= state[rotor.Body].Angular.X > .001f);
        Assert.True(sawRotor);
        Assert.True(result[cargo].Velocity.X > .001f);
        var paid = Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value;
        Assert.InRange(paid, .19f, .2f);
        Assert.InRange(Assert.Single(PhysicsGpuAbi.ReadEnergyStores(result.World)).Energy.Value, 0, .01f);
        Assert.InRange(MathF.Abs(result[rotor.Body].Angular.X), 0, .01f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FreeSupportAndNozzleReceiveConjugateReactions(bool powered)
    {
        var scene = new LawScene(false);
        var nozzle = scene.Sphere(new(0, 5, 0), .05f, 2, LawMaterial.Domino, default, default, 0, 0);
        var support = scene.Sphere(new(2, 5, 0), .05f, 2, LawMaterial.Domino, default, default, 0, 0);
        scene.DeclareMassFrame(support, new(.5f));
        var rotor = AddRotor(scene, support, new(2, 5, 0), powered ? 0 : 4);
        scene.RotaryCapture(support, support, rotor.Joint, .6666667f,
            exclusions: [scene.ColliderOf(support), scene.ColliderOf(rotor.Body)]);
        var store = scene.EnergyStore(nozzle, 100, 100);
        scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true), enabled: powered);
        var result = scene.Admit().Step(12);
        // Coaxial geometry has no orbital angular momentum. A .001 kg m²/s error would
        // visibly change this small rotor's speed by .004 rad/s; paired incidence must cancel.
        var angular = .5f * result[support].Angular.X + .245f * result[rotor.Body].Angular.X;
        Assert.InRange(angular - (powered ? 0 : .98f), -.001f, .001f);
        var momentum = 2 * result[nozzle].Velocity + 2 * result[support].Velocity + result[rotor.Body].Velocity;
        Assert.InRange(momentum.Length(), 0, .001f);
        var mechanical = result[nozzle].Velocity.LengthSquared() + result[support].Velocity.LengthSquared() +
            .5f * result[rotor.Body].Velocity.LengthSquared() + .25f * result[support].Angular.LengthSquared() +
            .1225f * result[rotor.Body].Angular.LengthSquared();
        var source = Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World));
        if (powered)
        {
            Assert.True(result[rotor.Body].Angular.X > .1f);
            Assert.True(result[support].Angular.X < 0);
            Assert.InRange(mechanical, 0, source.Work.Value + .005f);
            Assert.InRange(mechanical + source.JetLoss.Value, 0, source.Work.Value + .005f);
        }
        else
        {
            Assert.True(result[support].Angular.X > 0);
            Assert.True(mechanical < .5f * .245f * 16);
            Assert.Equal(0, source.Work.Value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovingSourceOrOccluderResamplesExposureAtCommittedPose(bool movingOccluder)
    {
        var scene = new LawScene(false);
        var nozzle = movingOccluder ? scene.StaticBox(new(0, 5, 0), new(.05f)) :
            scene.Sphere(new(0, 5, .3f), .05f, 1000, LawMaterial.Domino, new(4, 0, 0), default, 0, 0);
        var support = scene.StaticBox(new(3, 5, 0), new(.05f));
        var rotor = AddRotor(scene, support, new(3, 5, 0));
        scene.RotaryCapture(support, support, rotor.Joint, .6666667f,
            exclusions: [scene.ColliderOf(support), scene.ColliderOf(rotor.Body)]);
        if (movingOccluder) scene.Box(new(1, 5, 0), new(.05f, .6f, .6f), 1, LawMaterial.Domino,
            velocity: new(0, 2, 0), gravity: 0);
        var store = scene.EnergyStore(nozzle, 10000, 10000);
        scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        var startSpin = 0f;
        var stoppedWork = 0f;
        var result = scene.Admit().Step(1320, (tick, state) =>
        {
            if (tick == 12) startSpin = state[rotor.Body].Angular.X;
            if (tick == 1200) stoppedWork = Assert.Single(PhysicsGpuAbi.ReadAirflow(state.World)).Work.Value;
        });
        if (movingOccluder)
        {
            Assert.Equal(0, startSpin);
            Assert.InRange(result[rotor.Body].Angular.X, 5.9f, 6.1f);
        }
        else
        {
            Assert.True(startSpin > .1f);
            Assert.InRange(MathF.Abs(result[rotor.Body].Angular.X), 0, .01f);
            Assert.Equal(stoppedWork, Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoRotaryBranchesShareOneFiniteStoreAndCoastAfterDepletion(bool reverse)
    {
        var scene = new LawScene(false);
        var nozzle = scene.StaticBox(new(0, 5, 0), new(.05f));
        var support = scene.StaticBox(new(3, 5, 0), new(.05f));
        var rotor = AddRotor(scene, support, new(3, 5, 0));
        scene.RotaryCapture(support, support, rotor.Joint, 1,
            exclusions: [scene.ColliderOf(support), scene.ColliderOf(rotor.Body)]);
        const float budget = 12;
        var store = scene.EnergyStore(nozzle, budget, budget);
        var ids = new Dictionary<float, AirflowSourceId>();
        foreach (var force in reverse ? new[] { 6f, 3f } : [3f, 6f])
            ids.Add(force, scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true), force));
        var world = scene.Admit();
        LawState? depleted = null;
        var coast = world.Step(360, (tick, state) =>
        {
            if (tick == 240) depleted = new(world.Scene, (byte[])state.World.Clone(), state.Interactions);
        });
        Assert.NotNull(depleted);
        var sources = PhysicsGpuAbi.ReadAirflow(depleted.World);
        var paid = new Dictionary<AirflowSourceId, float>();
        for (var i = 0; i < sources.Length; i++) paid.Add(sources[i].Id, sources[i].Work.Value);
        foreach (var id in ids.Values) Assert.True(paid[id] > 0);
        var balance = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(depleted.World)).Balance;
        // 0.01 J is below one percent of this 12 J budget and cannot hide a missing branch or free rotor start.
        Assert.InRange(balance.Available, 0, .01f);
        Assert.InRange(paid.Values.Sum(), budget - .01f, budget);
        var after = PhysicsGpuAbi.ReadAirflow(coast.World);
        for (var i = 0; i < after.Length; i++) Assert.InRange(after[i].Work.Value - paid[after[i].Id], 0, .01f);
        Assert.InRange(MathF.Abs(coast[rotor.Body].Angular.X), 0, MathF.Abs(depleted[rotor.Body].Angular.X));
    }

    private static (GpuBodyId Body, RevoluteConstraintId Joint) AddRotor(LawScene scene, GpuBodyId support,
        Vector3 position, float spin = 0)
    {
        var body = scene.Sphere(position, .05f, 1, LawMaterial.Domino, default, new(spin, 0, 0), 0, 0);
        scene.DeclareMassFrame(body, new(.245f));
        var frame = new ConstraintFrame(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2));
        scene.Own(body, support);
        var joint = scene.Revolute(support, body, frame, frame, -MathF.PI, MathF.PI, 0, travel: RevoluteTravel.Continuous);
        return (body, joint);
    }

    private static (LawWorld World, GpuBodyId Rotor) Rotor(float sign, float flow, float force,
        float response, bool enabled = true, float spin = 0)
    {
        var scene = new LawScene(false);
        var nozzle = scene.StaticBox(new(sign > 0 ? 0 : 4, 5, 0), new(.1f));
        var support = scene.StaticBox(new(2, 5, 0), new(.1f));
        var rotor = scene.Sphere(new(2, 5, 0), .1f, 1, LawMaterial.Domino, default, new(spin, 0, 0), 0, 0);
        scene.DeclareMassFrame(rotor, new(.245f));
        var frame = new ConstraintFrame(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2));
        scene.Own(rotor, support);
        var coordinate = scene.Revolute(support, rotor, frame, frame, -MathF.PI, MathF.PI, 0,
            travel: RevoluteTravel.Continuous);
        scene.RotaryCapture(support, support, coordinate, response,
            exclusions: [scene.ColliderOf(support), scene.ColliderOf(rotor)]);
        var store = scene.EnergyStore(nozzle, 10000, 10000);
        scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true),
            force, flow, enabled: enabled, axis: sign * Vector3.UnitX);
        return (scene.Admit(), rotor);
    }
}
