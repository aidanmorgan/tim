using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class GasChamberWorldTests
{
    private static readonly JointFrame Origin = new(default, RigidRotation.Identity);
    private static PhysicsObject Object(PhysicsBody body, ConvexGeometry geometry) =>
        new(body, new([new(geometry, AffineTransform.Identity)]), new(1, 0, 0));
    private static SealedGasState Gas() => new(new(200, 400, 100, 1000, 100, 1e7), .002, .01, 240);
    private static (PhysicsWorld World, PhysicsBody Body, PhysicsBody Anchor, PhysicsFrameJoint Joint, AxialGasLoad Load)
        Setup(double area, double step, bool movingAnchor = false, double speed = 0, bool wall = false)
    {
        var body = new PhysicsBody(new(0), PhysicsMotionType.Dynamic, RigidPose.At(new(0, 0, .2)),
            new(0, 0, speed), default, 1, new(1, 1, 1));
        var anchor = movingAnchor
            ? new PhysicsBody(new(1), PhysicsMotionType.Dynamic, RigidPose.Identity, default, default, 2, new(1, 1, 1))
            : new PhysicsBody(new(1), PhysicsMotionType.Static, RigidPose.Identity, default, default);
        var joint = new PhysicsFrameJoint(new(0), FrameJointKind.Slider, body, Origin, anchor, Origin,
            ConnectedBodyCollision.Disabled, null, JointTravelDirection.Both);
        var objects = new List<PhysicsObject> { Object(body, new ConvexSphere(.01)), Object(anchor, new ConvexSphere(.001)) };
        if (wall) objects.Add(Object(new(new(2), PhysicsMotionType.Static, RigidPose.Identity, default, default),
            new ConvexBox(new(1, 1, .01))));
        var world = new PhysicsWorld([], objects.ToArray(), [joint], new(default, maximumStep: step));
        world.InstallGasNodes([new(new(0), anchor.Id, Gas())]);
        var load = new AxialGasLoad(new(0), joint.Id, new(Gas().Volume, joint.Travel.Error, area), 1e-9);
        world.ReplaceLoads(world.Loads with { Gas = [load] });
        return (world, body, anchor, joint, load);
    }

    [Theory]
    [InlineData(.001, .01, false)]
    [InlineData(-.001, .01, false)]
    [InlineData(.001, .002, false)]
    [InlineData(.001, .01, true)]
    public void GasDrivesSharedMotionWithFiniteEnergyAndExactReplay(double area, double step, bool movingAnchor)
    {
        var f = Setup(area, step, movingAnchor);
        var initial = f.World.Capture();
        void Run()
        {
            for (var i = 0; i < 30; i++)
            {
                f.World.Step([], [], .01);
                var gas = f.World.GasNode(new(0)).State;
                Assert.Equal(f.Load.Geometry.Volume(f.Joint.Travel.Error), gas.Volume);
                Assert.InRange(Math.Abs(gas.InternalEnergy -
                    240 * Math.Pow(.01 / gas.Volume, .5)), 0, 1e-10);
                Assert.InRange(Math.Abs(gas.InternalEnergy + f.Body.KineticEnergy + f.Anchor.KineticEnergy - 240), 0, 1e-7);
                if (movingAnchor)
                    Assert.InRange(Math.Abs(f.Body.LinearVelocity.Z + 2 * f.Anchor.LinearVelocity.Z), 0, 1e-10);
            }
        }
        Run();
        Assert.True(f.World.GasNode(new(0)).State.InternalEnergy < 240);
        Assert.Equal(Math.Sign(area), Math.Sign(f.Body.LinearVelocity.Z));
        var final = f.World.Capture();
        Assert.Throws<InvalidOperationException>(() => f.World.ReplaceLoads(f.World.Loads with { Gas = [] }));
        f.World.Restore(initial);
        Assert.Equal(initial.GasStates.ToArray(), f.World.GasNodes.ToArray());
        Run();
        Assert.Equal(final.GasStates.ToArray(), f.World.GasNodes.ToArray());
        Assert.Equal(final.BodyStates.ToArray(), f.World.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void CollisionCutsCompressionWorkAndRestoresBothDomains()
    {
        var f = Setup(.001, .08, speed: -4, wall: true);
        var initial = f.World.Capture();
        f.World.Step([], [], .08);
        Assert.NotEmpty(f.World.Impacts.ToArray());
        Assert.True(f.Body.LinearVelocity.Z > 0);
        Assert.InRange(Math.Abs(f.World.GasNode(new(0)).State.InternalEnergy + f.Body.KineticEnergy - 248), 0, 1e-7);
        var final = f.World.Capture();
        f.World.Restore(initial);
        f.World.Step([], [], .08);
        Assert.Equal(final.GasStates.ToArray(), f.World.GasNodes.ToArray());
        Assert.Equal(final.BodyStates.ToArray(), f.World.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void InvalidBindingsAndDuplicateInventoryRejectAtomically()
    {
        var f = Setup(.001, .01);
        var before = f.World.Capture();
        Assert.Throws<ArgumentException>(() => f.World.ReplaceLoads(f.World.Loads with { Gas = [f.Load, f.Load] }));
        Assert.Throws<ArgumentException>(() => f.World.ReplaceLoads(f.World.Loads with
            { Gas = [new(new(99), f.Joint.Id, f.Load.Geometry, 1e-9)] }));
        Assert.Throws<ArgumentException>(() => f.World.ReplaceLoads(f.World.Loads with
            { Gas = [new(new(0), new(99), f.Load.Geometry, 1e-9)] }));
        Assert.Throws<ArgumentException>(() => f.World.ReplaceLoads(f.World.Loads with
            { Gas = [new(new(0), f.Joint.Id, new(Gas().Volume, 0, .001), 1e-9)] }));
        Assert.Throws<ArgumentException>(() => f.World.ReplaceJoints([]));
        Assert.Throws<ArgumentNullException>(() => new AxialGasLoad(new(0), new(0), null!, 1e-9));
        Assert.Throws<ArgumentNullException>(() => f.World.ReplaceLoads(f.World.Loads with { Gas = [null!] }));
        Assert.Same(f.Load, Assert.Single(f.World.Loads.Gas));
        Assert.Equal(before.GasStates.ToArray(), f.World.GasNodes.ToArray());
        Assert.False(f.World.Loads.IsEmpty);
    }

    [Fact]
    public void UnsupportedThermodynamicMotionRollsBackEntireStep()
    {
        var f = Setup(.001, .01, speed: -2000);
        var before = f.World.Capture();
        Assert.ThrowsAny<ArgumentException>(() => f.World.Step([], [], .01));
        Assert.Equal(before.GasStates.ToArray(), f.World.GasNodes.ToArray());
        Assert.Equal(before.BodyStates.ToArray(), f.World.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time, f.World.Time);
        Assert.Equal(before.StepIndex, f.World.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle, f.World.Phase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EqualOpposedChambersHoldWithoutSpendingEitherInventory(bool reverse)
    {
        var f = Setup(.001, .01);
        f.World.InstallGasNodes([new(new(1), f.Anchor.Id, Gas())]);
        var opposite = new AxialGasLoad(new(1), f.Joint.Id, new(Gas().Volume, f.Joint.Travel.Error, -.001), 1e-9);
        AxialGasLoad[] declarations = [f.Load, opposite];
        if (reverse) Array.Reverse(declarations);
        f.World.ReplaceLoads(f.World.Loads with { Gas = declarations });
        var before = f.World.Capture();
        declarations[0] = null!;
        f.World.Step([], [], .1);
        Assert.Equal(before.BodyStates.ToArray(), f.World.Capture().BodyStates.ToArray());
        Assert.Equal(before.GasStates.ToArray(), f.World.GasNodes.ToArray());
        Assert.Equal(0, f.Body.KineticEnergy);
    }

    [Fact]
    public void InventoryWithoutMechanicalBindingSuppliesNoMotion()
    {
        var f = Setup(.001, .01);
        f.World.ReplaceLoads(f.World.Loads with { Gas = [] });
        var before = f.World.Capture();
        f.World.Step([], [], .1);
        Assert.Equal(before.BodyStates.ToArray(), f.World.Capture().BodyStates.ToArray());
        Assert.Equal(before.GasStates.ToArray(), f.World.GasNodes.ToArray());
    }

    [Theory]
    [InlineData(false, 0, .01, true)]
    [InlineData(true, 0, .01, true)]
    [InlineData(true, .5, .01, true)]
    [InlineData(true, .5, .002, true)]
    [InlineData(true, .5, .01, false)]
    [InlineData(true, .5, .002, false)]
    public void RotatedAndSpinningCarriersConserveGasAndMechanicalEnergy(bool dynamicCarrier, double spin, double step, bool supplied)
    {
        var rotation = RigidRotation.FromRotationVector(new(.3, .5, -.2));
        var offset = rotation.Apply(new(0, 0, .2));
        var omega = rotation.Apply(new(spin, 0, 0));
        var carrierVelocity = rotation.Apply(new(.4, -.2, .3));
        if (!dynamicCarrier) carrierVelocity = default;
        var body = new PhysicsBody(new(0), PhysicsMotionType.Dynamic, new(offset, rotation),
            carrierVelocity + CollisionVector.Cross(omega, offset), omega, 1, new(1, 1, 1));
        var anchor = dynamicCarrier
            ? new PhysicsBody(new(1), PhysicsMotionType.Dynamic, new(default, rotation),
                carrierVelocity, omega, 2, new(1, 1, 1))
            : new PhysicsBody(new(1), PhysicsMotionType.Static, new(default, rotation), default, default);
        var joint = new PhysicsFrameJoint(new(0), FrameJointKind.Slider, body, Origin, anchor, Origin,
            ConnectedBodyCollision.Disabled, null, JointTravelDirection.Both);
        var world = new PhysicsWorld([], [Object(body, new ConvexSphere(.01)), Object(anchor, new ConvexSphere(.001))],
            [joint], new(default, maximumStep: step));
        world.InstallGasNodes([new(new(0), anchor.Id, Gas())]);
        world.ReplaceLoads(new() { Gas = [new(new(0), joint.Id, new(Gas().Volume, joint.Travel.Error, .001), 1e-9)] });
        if (!supplied) world.ReplaceLoads(world.Loads with { Gas = [] });
        var initial = world.Capture();
        var energy = 240 + body.KineticEnergy + anchor.KineticEnergy;
        var momentum = body.LinearVelocity + (dynamicCarrier ? anchor.LinearVelocity * 2 : default);
        if (spin != 0)
        {
            var bodies = new[] { body, anchor };
            var prediction = AccelerationSolver.Predict(world.Loads,
                bodies.ToDictionary(value => value.Id, value => world.Collider(value.Id).Declaration),
                bodies, [joint], [], bodies.ToDictionary(value => value.Id, _ => default(BodyWrench)),
                step, .01, 1e-7, 1e-8, 1e-9, [], new Dictionary<PhysicsBodyId, PhysicsEnergyStoreState>(), world.Loads.Gas.ToDictionary(load => load.Node, load => load.Bind([joint], world.GasNodes.ToArray().ToDictionary(node => node.Id))));
            if (supplied)
            {
                Assert.Equal(ForcePredictionBoundary.Accuracy, prediction.Boundary);
                Assert.InRange(prediction.Duration, double.Epsilon, Math.BitDecrement(step));
            }
            var midpoint = bodies.ToDictionary(value => value.Id,
                value => prediction.Trajectories[value.Id].SampleBody(prediction.Duration / 2));
            var endpoint = bodies.ToDictionary(value => value.Id,
                value => prediction.Trajectories[value.Id].SampleBody(prediction.Duration));
            var midJoint = (PhysicsFrameJoint)joint.Rebind(midpoint);
            var endJoint = (PhysicsFrameJoint)joint.Rebind(endpoint);
            var potential = new AxialGasPotential(Gas(), joint.Travel.Error, .001);
            var effort = supplied ? potential.IntervalEffort(joint.Travel.Error, endJoint.Travel.Error) : 0;
            var terms = midJoint.Travel.Jacobian.Bind(midJoint.A, midJoint.B).Terms.ToArray();
            var gasWork = WrenchPathWork.Measure(terms.Select(term => new WrenchPathTerm(
                bodies.Single(value => value.Id == term.Body.Id), prediction.Trajectories[term.Body.Id],
                new(term.Linear * effort, term.Angular * effort))).ToArray(), prediction.Duration, 1e-12, 0);
            var totalWork = WrenchPathWork.Measure(bodies.Select(value => new WrenchPathTerm(value,
                prediction.Trajectories[value.Id], prediction.Wrenches[value.Id])).ToArray(),
                prediction.Duration, 1e-12, 0);
            var gasSignedWork = gasWork.Supplied - gasWork.Dissipated;
            var totalSignedWork = totalWork.Supplied - totalWork.Dissipated;
            Assert.InRange(Math.Abs(totalSignedWork - gasSignedWork), 0, 1e-10);
            var released = supplied ? 240 - potential.Energy(endJoint.Travel.Error) : 0;
            var kineticGain = endpoint.Values.Sum(value => value.KineticEnergy) - bodies.Sum(value => value.KineticEnergy);
            Assert.InRange(Math.Abs(kineticGain - totalSignedWork), 0, 1e-10);
            Console.WriteLine($"Captured gas work: supplied={supplied}, horizon={prediction.Duration:R}, gasMinusRelease={gasSignedWork - released:R}, constraintWork={totalSignedWork - gasSignedWork:R}, kineticMinusWork={kineticGain - totalSignedWork:R}, predictedEnergyError={kineticGain - released:R}, workErrorBound={gasWork.SuppliedErrorBound + gasWork.DissipatedErrorBound + totalWork.SuppliedErrorBound + totalWork.DissipatedErrorBound:R}");
            var beforeProjectionEnergy = endpoint.Values.Sum(value => value.KineticEnergy) +
                (supplied ? potential.Energy(endJoint.Travel.Error) : 240);
            var beforeProjectionCoordinate = endJoint.Travel.Error;
            var positions = PositionSolver.Solve(() => [endJoint],
                new(endpoint.Values, (_, _) => [], 1e-6), 1e-7);
            var afterPositionEnergy = endpoint.Values.Sum(value => value.KineticEnergy) +
                (supplied ? potential.Energy(endJoint.Travel.Error) : 240);
            var velocities = ImpulseSolver.Solve(PhysicsJoint.CollectVelocityConstraints([endJoint], 1e-7).ToArray(),
                tolerance: 1e-8);
            var afterVelocityEnergy = endpoint.Values.Sum(value => value.KineticEnergy) +
                (supplied ? potential.Energy(endJoint.Travel.Error) : 240);
            Console.WriteLine($"Endpoint correction: supplied={supplied}, horizon={prediction.Duration:R}, positionEnergy={afterPositionEnergy - beforeProjectionEnergy:R}, velocityEnergy={afterVelocityEnergy - afterPositionEnergy:R}, coordinateCorrection={endJoint.Travel.Error - beforeProjectionCoordinate:R}, positionIterations={positions.Iterations}, velocityIterations={velocities.Iterations}");
            Assert.Equal(initial.BodyStates.ToArray(), world.Capture().BodyStates.ToArray());
            Assert.Equal(initial.GasStates.ToArray(), world.GasNodes.ToArray());
        }
        void Run()
        {
            for (var i = 0; i < 20; i++)
            {
                world.Step([], [], .01);
                var error = Math.Abs(world.GasNode(new(0)).State.InternalEnergy +
                    body.KineticEnergy + anchor.KineticEnergy - energy);
                if (error > 1e-7) Console.WriteLine($"Gas energy error: supplied={supplied}, spin={spin}, maximumStep={step}, tick={i + 1}, error={error:R}");
                Assert.InRange(error, 0, 1e-7);
                if (supplied) Assert.InRange(error, 0, (i + 1) * 1e-9);
                if (dynamicCarrier)
                    Assert.InRange((body.LinearVelocity + anchor.LinearVelocity * 2 - momentum).Length, 0, 1e-9);
            }
        }
        Run();
        Assert.Equal(20UL, world.StepIndex);
        Assert.InRange(Math.Abs(world.Time - .2), 0, 1e-15);
        var final = world.Capture();
        world.Restore(initial);
        Run();
        Assert.Equal(final.GasStates.ToArray(), world.GasNodes.ToArray());
        Assert.Equal(final.BodyStates.ToArray(), world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void FailureAfterAValidSubstepRestoresInitialGasAndMotion()
    {
        var f = Setup(.001, .01, speed: -500);
        var initial = f.World.Capture();
        f.World.Step([], [], .01);
        Assert.True(f.World.GasNode(new(0)).State.InternalEnergy > 240);
        Assert.NotEqual(initial.BodyStates.ToArray(), f.World.Capture().BodyStates.ToArray());
        f.World.Restore(initial);
        Assert.ThrowsAny<ArgumentException>(() => f.World.Step([], [], .03));
        Assert.Equal(initial.GasStates.ToArray(), f.World.GasNodes.ToArray());
        Assert.Equal(initial.BodyStates.ToArray(), f.World.Capture().BodyStates.ToArray());
        Assert.Equal(initial.Time, f.World.Time);
        Assert.Equal(initial.StepIndex, f.World.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle, f.World.Phase);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.Epsilon)]
    public void InvalidWorkAllowancesReject(double tolerance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AxialGasLoad(new(0), new(0), new(Gas().Volume, 0, .001), tolerance));
    }

    [Fact]
    public void ChamberBindingUsesSuppliedCurrentInventoryAndKeepsGeometry()
    {
        var f = Setup(.001, .01);
        var nodes = f.World.GasNodes.ToArray().ToDictionary(node => node.Id);
        var first = f.Load.Bind([f.Joint], nodes);
        var depleted = AdiabaticGasDischarge.Propose(Gas(), .001).After;
        nodes[new(0)] = new(new(0), f.Anchor.Id, depleted);
        var second = f.Load.Bind([f.Joint], nodes);
        Assert.Same(depleted, second.ReferenceState);
        Assert.Same(f.Load.Geometry, second.Geometry);
        var coordinate = f.Joint.Travel.Error;
        Assert.Equal(depleted.Pressure * .001, second.IntervalEffort(coordinate, coordinate));
        Assert.True(second.IntervalEffort(coordinate, coordinate) < first.IntervalEffort(coordinate, coordinate));
        Assert.Equal(depleted.Mass, second.State(coordinate + .01).Mass);
        Assert.Equal(first.State(coordinate + .01).Volume, second.State(coordinate + .01).Volume);
        Assert.Equal(Gas(), f.World.GasNode(new(0)).State);
        Assert.Equal(Gas(), first.ReferenceState);
        nodes[new(0)] = new(new(1), f.Anchor.Id, depleted);
        Assert.Throws<ArgumentException>(() => f.Load.Bind([f.Joint], nodes));
        nodes[new(0)] = new(new(0), new(99), depleted);
        Assert.Throws<ArgumentException>(() => f.Load.Bind([f.Joint], nodes));
        nodes[new(0)] = null!;
        Assert.Throws<ArgumentException>(() => f.Load.Bind([f.Joint], nodes));
        Assert.Throws<ArgumentNullException>(() => f.Load.Bind([f.Joint], null!));
    }

    [Fact]
    public void PredictionRequiresExactGeometryBindingsAndDoesNotMutateInventory()
    {
        var f = Setup(.001, .01);
        var bodies = new[] { f.Body, f.Anchor };
        var nodes = f.World.GasNodes.ToArray().ToDictionary(node => node.Id);
        ForcePrediction Predict(Dictionary<PhysicsGasNodeId, AxialGasPotential> bindings) =>
            AccelerationSolver.Predict(f.World.Loads,
                bodies.ToDictionary(body => body.Id, body => f.World.Collider(body.Id).Declaration),
                bodies, [f.Joint], [], bodies.ToDictionary(body => body.Id, _ => default(BodyWrench)),
                .01, .01, 1e-7, 1e-8, 1e-9, [], new Dictionary<PhysicsBodyId, PhysicsEnergyStoreState>(), bindings);
        Assert.Throws<ArgumentException>(() => Predict([]));
        Assert.Throws<ArgumentNullException>(() => Predict(null!));
        Assert.Throws<ArgumentException>(() => Predict(new() { [new(0)] = null! }));
        var bound = f.Load.Bind([f.Joint], nodes);
        Assert.Throws<ArgumentException>(() => Predict(new() { [new(1)] = bound }));
        Assert.Throws<ArgumentException>(() => Predict(new()
            { [new(0)] = new(Gas(), f.Joint.Travel.Error, -.001) }));
        _ = Predict(new() { [new(0)] = bound });
        Assert.Equal(Gas(), f.World.GasNode(new(0)).State);
        f.World.Step([], [], .01);
        // The declaration has no obsolete gas state that could overwrite current inventory.
        f.World.ReplaceLoads(f.World.Loads);
        var current = f.World.GasNode(new(0)).State;
        var rebound = f.Load.Bind([f.Joint], f.World.GasNodes.ToArray().ToDictionary(node => node.Id));
        Assert.Same(current, rebound.ReferenceState);
        Assert.Same(f.Load.Geometry, rebound.Geometry);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GeometryRejectsNonfiniteScalars(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasGeometry(value, 0, .001));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasGeometry(.01, value, .001));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasGeometry(.01, 0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasGeometry(.01, 0, .001).Volume(value));
    }

    [Fact]
    public void GeometryBindingRejectsMismatchedVolumeAndUnsupportedDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasGeometry(0, 0, .001));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasGeometry(-1, 0, .001));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasGeometry(.01, 0, 0));
        var geometry = new AxialGasGeometry(.01, 0, .001);
        Assert.Throws<ArgumentOutOfRangeException>(() => geometry.Volume(-10));
        Assert.Throws<ArgumentException>(() => new AxialGasPotential(Gas(), 1, geometry));
        Assert.Throws<ArgumentNullException>(() => new AxialGasPotential(Gas(), 0, (AxialGasGeometry)null!));
        Assert.Throws<ArgumentNullException>(() => new AxialGasPotential(null!, 0, geometry));
    }
}
