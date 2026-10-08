using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BodyPoseReadTests
{
    [Theory]
    [InlineData(PhysicsMotionType.Static)]
    [InlineData(PhysicsMotionType.Kinematic)]
    [InlineData(PhysicsMotionType.Dynamic)]
    public void CapturedPoseRetainsTypedIdentityAndValues(PhysicsMotionType motion)
    {
        var original = new RigidPose(new(2, 3, 4), RigidRotation.Identity);
        var body = new PhysicsBody(new(7), motion, original, default, default,
            motion == PhysicsMotionType.Dynamic ? 1 : 0,
            motion == PhysicsMotionType.Dynamic ? new(1, 1, 1) : default);
        var read = new BodyPoseRead(body.Id, body.MotionType, body.Pose, RigidPose.Identity);
        body.Restore(body.Snapshot() with { Pose = RigidPose.At(new(5, 6, 7)) });
        Assert.Equal(new PhysicsBodyId(7), read.Id);
        Assert.Equal(motion, read.MotionType);
        Assert.Equal(original, read.Pose);
        Assert.NotEqual(body.Pose, read.Pose);
    }

    [Fact]
    public void UnsupportedMotionAndInvalidPoseReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BodyPoseRead(new(0), (PhysicsMotionType)99, RigidPose.Identity, RigidPose.Identity));
        Assert.Throws<ArgumentException>(() => new BodyPoseRead(new(0), PhysicsMotionType.Static, default, RigidPose.Identity));
        Assert.Throws<ArgumentException>(() => new BodyPoseRead(new(0), PhysicsMotionType.Static,
            new(new(double.NaN, 0, 0), RigidRotation.Identity), RigidPose.Identity));
    }

    [Fact]
    public void RelativePoseUsesCapturedReferenceIncludingOffsetAndRotation()
    {
        var reference = new RigidPose(new(4, 5, 6),
            RigidRotation.FromRotationVector(new(.2, -.3, .4)));
        var relative = new RigidPose(new(1, 2, 3),
            RigidRotation.FromRotationVector(new(-.4, .1, .2)));
        var read = new BodyPoseRead(new(3), PhysicsMotionType.Dynamic,
            reference.Compose(relative), reference);
        Assert.InRange((read.RelativePose.Center - relative.Center).Length, 0, 1e-12);
        Assert.InRange((read.RelativePose.Rotation.Apply(new(1, 2, 3)) -
            relative.Rotation.Apply(new(1, 2, 3))).Length, 0, 1e-12);
        Assert.Equal(reference, read.ReferencePose);
        Assert.Throws<ArgumentException>(() =>
            new BodyPoseRead(new(0), PhysicsMotionType.Static, RigidPose.Identity, default));
    }
}
