using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

/// <summary>Contact invariants previously exercised through scene-velocity mutations,
/// now bound to the shared rigid bodies, constraints and event clock.</summary>
public class SharedContactMigrationTests
{
    private static readonly JointFrame Origin = new(default, RigidRotation.Identity);
    private static PhysicsBody Body(int id, CollisionVector center, CollisionVector velocity) =>
        new(new(id), PhysicsMotionType.Dynamic, RigidPose.At(center), velocity, default, 1, new(.1, .1, .1));
    private static PhysicsObject Object(PhysicsBody body, double restitution) =>
        new(body, new([new(new ConvexSphere(.5), AffineTransform.Identity)]), new(restitution, 0, 0));

    [Theory]
    [InlineData(false, 4d)]
    [InlineData(true, 4d)]
    [InlineData(false, (double)float.Epsilon)]
    [InlineData(true, (double)float.Epsilon)]
    [InlineData(false, 1e-20)]
    [InlineData(true, 1e-20)]
    public void RatchetAbsorbsInwardContactInEitherBodyOrder(bool headFirst, double speed)
    {
        var head = Body(0, default, default);
        var anchor = new PhysicsBody(new(1), PhysicsMotionType.Static, RigidPose.At(default), default, default);
        var payload = Body(2, new(0, 0, 1), new(0, 0, -speed));
        var joint = new PhysicsFrameJoint(new(0), FrameJointKind.Slider, head, Origin, anchor, Origin,
            ConnectedBodyCollision.Disabled, new(0, 1), JointTravelDirection.Positive);
        var contact = headFirst
            ? ImpulseConstraint.Contact(head, payload, new(0, 0, .5), new(0, 0, -1), 0, 0)
            : ImpulseConstraint.Contact(payload, head, new(0, 0, .5), new(0, 0, 1), 0, 0);
        var poses = new[] { head.Pose, payload.Pose };
        ImpulseSolver.Solve([..joint.VelocityConstraints(1e-7), contact], tolerance: speed * 1e-9);
        // Relative assertions ensure a tiny incoming impulse is not simply ignored.
        Assert.InRange(head.LinearVelocity.Length / speed, 0, 1e-8);
        Assert.InRange(payload.LinearVelocity.Length / speed, 0, 1e-8);
        Assert.InRange(Math.Abs(contact.AccumulatedImpulse / speed - 1), 0, 1e-8);
        Assert.Equal(poses, new[] { head.Pose, payload.Pose });
    }

    [Theory]
    [InlineData((double)float.Epsilon)]
    [InlineData(1e-20)]
    [InlineData(1d)]
    [InlineData(100d)]
    public void FirstImpactIsResolvedAndASeparatingContactDoesNotPullBodiesTogether(double speed)
    {
        var a = Body(0, new(-.5, 0, 0), new(speed, 0, 0));
        var b = Body(1, new(.5, 0, 0), default);
        var impact = ImpulseConstraint.Contact(a, b, default, new(-1, 0, 0), 1, 0);
        ImpulseSolver.Solve([impact], tolerance: speed * 1e-9);
        Assert.InRange(Math.Abs(a.LinearVelocity.X / speed), 0, 1e-9);
        Assert.InRange(Math.Abs(b.LinearVelocity.X / speed - 1), 0, 1e-9);
        Assert.InRange(Math.Abs(impact.AccumulatedImpulse / speed - 1), 0, 1e-9);
        var beforeA = a.Snapshot(); var beforeB = b.Snapshot();
        var separating = ImpulseConstraint.Contact(a, b, default, new(-1, 0, 0), 1, 0);
        ImpulseSolver.Solve([separating], tolerance: speed * 1e-9);
        Assert.Equal(0, separating.AccumulatedImpulse);
        Assert.Equal(beforeA, a.Snapshot()); Assert.Equal(beforeB, b.Snapshot());
    }

    [Theory]
    [InlineData(false, 0d)]
    [InlineData(true, 0d)]
    [InlineData(false, 1d)]
    [InlineData(true, 1d)]
    public void FutureImpactUsesSharedClockAndRestoresExactly(bool reversedIds, double restitution)
    {
        var a = Body(reversedIds ? 1 : 0, new(-2, 0, 0), new(2, 0, 0));
        var b = Body(reversedIds ? 0 : 1, default, default);
        var world = new PhysicsWorld([], [Object(a, restitution), Object(b, restitution)], [],
            new(default, maximumStep: 1));
        var before = world.Capture();
        void Run()
        {
            world.Step([], [], .25);
            Assert.Empty(world.Impacts.ToArray());
            Assert.Equal(2, a.LinearVelocity.X); Assert.Equal(0, b.LinearVelocity.X);
            world.Step([], [], .5);
            Assert.Contains(world.Impacts.ToArray(), hit => hit.Pair.A != hit.Pair.B);
            Assert.InRange(Math.Abs(a.LinearVelocity.X - (1 - restitution)), 0, 1e-7);
            Assert.InRange(Math.Abs(b.LinearVelocity.X - (1 + restitution)), 0, 1e-7);
            Assert.InRange(a.KineticEnergy + b.KineticEnergy, 0, 2 + 1e-8);
            Assert.InRange((b.Center - a.Center).Length, 1 - 1e-7, 2);
            Assert.Equal(.75, world.Time);
        }
        Run(); var after = world.Capture();
        world.Restore(before);
        Assert.Equal(before.BodyStates.ToArray(), world.Capture().BodyStates.ToArray());
        Assert.Equal(0, world.Time);
        Run();
        Assert.Equal(after.BodyStates.ToArray(), world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void AChangedContactNormalBuildsANewConstraint()
    {
        var body = Body(0, default, new(-2, -3, 0));
        var wall = new PhysicsBody(new(1), PhysicsMotionType.Static, RigidPose.At(default), default, default);
        var first = ImpulseConstraint.Contact(body, wall, default, new(1, 0, 0), 0, 0);
        ImpulseSolver.Solve([first]);
        Assert.Equal(new CollisionVector(0, -3, 0), body.LinearVelocity);
        var second = ImpulseConstraint.Contact(body, wall, default, new(0, 1, 0), 0, 0);
        ImpulseSolver.Solve([second]);
        Assert.Equal(default(CollisionVector), body.LinearVelocity);
        Assert.Equal(2, first.AccumulatedImpulse); Assert.Equal(3, second.AccumulatedImpulse);
    }
}
